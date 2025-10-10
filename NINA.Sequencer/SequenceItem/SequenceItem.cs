#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NINA.Sequencer.Utility;
using NINA.Core.Locale;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SequenceItem : SequenceHasChanged, ISequenceItem {

        public SequenceItem() {
        }

        public SequenceItem(SequenceItem cloneMe) {
            CopyMetaData(cloneMe);
        }

        protected void CopyMetaData(SequenceItem cloneMe) {
            Icon = cloneMe.Icon;
            Name = cloneMe.Name;
            Category = cloneMe.Category;
            Description = cloneMe.Description;
            SymbolBroker = cloneMe.SymbolBroker;
            Attempts = cloneMe.Attempts;
            ErrorBehavior = cloneMe.ErrorBehavior;
        }

        private ISymbolBroker symbolBroker;
        private string name;
        private bool showMenu;
        private SequenceEntityStatus status = SequenceEntityStatus.CREATED;
        public ICommand AddCloneToParentCommand => new GalaSoft.MvvmLight.Command.RelayCommand<object>((o) => { AddCloneToParent(); ShowMenu = false; });
        public string Category { get; set; }
        public string Description { get; set; }
        public virtual ICommand DetachCommand => new GalaSoft.MvvmLight.Command.RelayCommand(Detach);
        public GeometryGroup Icon { get; set; }
        public ICommand MoveDownCommand => new GalaSoft.MvvmLight.Command.RelayCommand(MoveDown);
        public ICommand MoveUpCommand => new GalaSoft.MvvmLight.Command.RelayCommand(MoveUp);
        public ICommand DisableEnableCommand => new GalaSoft.MvvmLight.Command.RelayCommand(() => {
            if(Status != SequenceEntityStatus.DISABLED) {
                Status = SequenceEntityStatus.DISABLED;
                ShowMenu = false;
            } else {
                Status = SequenceEntityStatus.CREATED;
            }
            
        });

        public ISymbolBroker SymbolBroker {
            get => symbolBroker;
            set {
                symbolBroker = value;
                RaisePropertyChanged();
            }
        }

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public ISequenceContainer Parent { get; private set; }

        private InstructionErrorBehavior errorBehavior = InstructionErrorBehavior.ContinueOnError;

        [JsonProperty]
        public virtual InstructionErrorBehavior ErrorBehavior {
            get => errorBehavior;
            set {
                errorBehavior = value;
                RaisePropertyChanged();
            }
        }

        private int attempts = 1;

        [JsonProperty]
        public virtual int Attempts {
            get => attempts;
            set {
                if (value > 0) {
                    attempts = value;
                    RaisePropertyChanged();
                }
            }
        }

        public virtual ICommand ResetProgressCommand => new GalaSoft.MvvmLight.Command.RelayCommand<object>((o) => { ResetProgressCascaded(); ShowMenu = false; });

        public bool ShowMenu {
            get => showMenu;
            set {
                showMenu = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ShowMenuCommand => new GalaSoft.MvvmLight.Command.RelayCommand<object>((o) => ShowMenu = !ShowMenu, (o) => Status != SequenceEntityStatus.DISABLED);

        public SequenceEntityStatus Status {
            get => status;
            set {
                status = value;
                RaisePropertyChanged();
            }
        }

        public void AddCloneToParent() {
            Parent?.Add((ISequenceItem)this.Clone());
        }

        public virtual void AfterParentChanged() {
            //Hook for behavior when parent changes
        }

        public void AttachNewParent(ISequenceContainer newParent) {
            Parent = newParent;

            AfterParentChanged();
        }

        public abstract object Clone();

        public void Detach() {
            if (!(this is ISimpleDSOContainer) || !AskHasChanged(Name)) {
                Parent?.Remove(this);
            }
        }

        public abstract Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token);

        public virtual TimeSpan GetEstimatedDuration() {
            return TimeSpan.Zero;
        }

        public virtual void MoveDown() {
            Parent?.MoveDown(this);
        }

        public virtual void MoveUp() {
            Parent?.MoveUp(this);
        }

        public virtual void ResetProgress() {
            if(this.Status != SequenceEntityStatus.DISABLED) { 
                this.Status = SequenceEntityStatus.CREATED;
            }
        }

        public virtual void ResetProgressCascaded() {
            ResetProgress();
            this.Parent?.ResetProgressCascaded();
        }

        private CancellationTokenSource localCts;

        private void RunErrorBehavior(ISequenceRootContainer root) {
            var attemptWord = Attempts != 1 ? "attempts" : "attempt";
            Status = SequenceEntityStatus.FAILED;
            switch (ErrorBehavior) {
                case InstructionErrorBehavior.AbortOnError:
                    Logger.Error($"Instruction {Name} failed after {Attempts} {attemptWord}. Error behavior is set to {ErrorBehavior}. Aborting Sequence!");
                    _ = root.Interrupt();
                    break;

                case InstructionErrorBehavior.SkipInstructionSetOnError:
                    Logger.Error($"Instruction {Name} failed after {Attempts} {attemptWord}. Error behavior is set to {ErrorBehavior}. Skipping current instruction set.");
                    _ = Parent?.Interrupt();
                    break;

                case InstructionErrorBehavior.SkipToSequenceEndInstructions:
                    Logger.Error($"Instruction {Name} failed after {Attempts} {attemptWord}. Error behavior is set to {ErrorBehavior}. Skipping to end of sequence instructions.");
                    _ = SkipToEndOfSequence(root);
                    break;

                default:
                    Logger.Error($"Instruction {Name} failed after {Attempts} {attemptWord}. Error behavior is set to {ErrorBehavior}. Continuing.");
                    break;
            }
        }

        public async Task Run(IProgress<ApplicationStatus> progress, CancellationToken token) {
            using (localCts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                if (Status == SequenceEntityStatus.CREATED) {
                    Status = SequenceEntityStatus.RUNNING;

                    var root = ItemUtility.GetRootContainer(this.Parent);

                    try {
                        Logger.Info($"Starting {this}");
                        if (this is IValidatable && !(this is ISequenceContainer)) {
                            var validatable = this as IValidatable;
                            if (!validatable.Validate()) {
                                RunErrorBehavior(root);
                                throw new SequenceEntityFailedValidationException(string.Join(", ", validatable.Issues));
                            }
                        }

                        if (root != null && !(this is ISequenceContainer)) {
                            root.AddRunningItem(this);
                        }

                        var success = false;
                        for (int i = 0; i < Attempts; i++) {
                            using var checkTokenSource = new CancellationTokenSource();
                            var checkTimeout = TimeSpan.FromMinutes(2);
                            try {
                                if(this is ISequenceContainer) {
                                    await this.Execute(progress, localCts.Token);
                                } else {
                                    var localToken = localCts.Token;
                                    var checkToken = checkTokenSource.Token;
                                    var checkTask = Task.Run(async () => {
                                        try {
                                            while (!localToken.IsCancellationRequested) {
                                                await Task.Delay(TimeSpan.FromSeconds(1), checkToken);
                                            }
                                            await Task.Delay(10, checkToken);
                                            await CoreUtil.Wait(checkTimeout, checkToken, progress, Loc.Instance["Lbl_SequenceItem_WaitingForCancellation"]);
                                        } catch { }

                                    });
                                    var executionTask = this.Execute(progress, localToken);
                                    var completedTask = await Task.WhenAny(checkTask, executionTask);
                                    if (completedTask == checkTask) {
                                        Logger.Error($"Execution for {this} did not finish after being cancelled for over {checkTimeout.Minutes} minutes! Continuing...");
                                        Notification.ShowError(string.Format(Loc.Instance["Lbl_SequenceItem_SkippingAfterFailedCancellation"], this, checkTimeout.Minutes));
                                        root?.RaiseFailureEvent(this, new SequenceEntityFailedException($"Execution for {this} did not finish after being cancelled for over {checkTimeout.Minutes} minutes!"));
                                    } else {
                                        // Ensure any exceptions from main task are observed
                                        await executionTask;
                                    }
                                    localToken.ThrowIfCancellationRequested();
                                }

                                Logger.Info($"Finishing {this}");
                                Status = SequenceEntityStatus.FINISHED;
                                success = true;
                                break;
                            } catch (SequenceItemSkippedException) {
                                throw;
                            } catch (OperationCanceledException) {
                                throw;
                            } catch (Exception ex) {
                                Logger.Error($"{this} - ", ex);
                                success = false;
                                root?.RaiseFailureEvent(this, ex);
                            } finally { 
                                try {
                                    checkTokenSource.Cancel();
                                } catch { }
                            }
                        }

                        if (!success) {
                            RunErrorBehavior(root);
                        }
                    } catch (SequenceEntityFailedException ex) {
                        Logger.Error($"Failed: {this} - " + ex.Message);
                        Status = SequenceEntityStatus.FAILED;                        
                        root?.RaiseFailureEvent(this, ex);                        
                    } catch (SequenceEntityFailedValidationException ex) {
                        Logger.Error($"Failed validation: {this} - " + ex.Message);
                        Status = SequenceEntityStatus.FAILED;
                        root?.RaiseFailureEvent(this, ex);
                    } catch (SequenceItemSkippedException) {
                        Logger.Warning($"Skipped {this}");
                        Status = SequenceEntityStatus.SKIPPED;
                    } catch (OperationCanceledException) {
                        if (token.IsCancellationRequested) {
                            Status = SequenceEntityStatus.CREATED;
                            Logger.Debug($"Cancelled {this}");
                            throw;
                        } else {
                            Status = SequenceEntityStatus.SKIPPED;
                            Logger.Debug($"Skipped {this}");
                        }
                    } finally {
                        progress?.Report(new ApplicationStatus());
                        if (root != null && !(this is ISequenceContainer)) {
                            root?.RemoveRunningItem(this);
                        }
                    }
                }
            }
        }

        private async Task<bool> SkipToEndOfSequence(ISequenceRootContainer root) {
            var startContainer = root.Items[0] as ISequenceContainer;
            var targetContainer = root.Items[1] as ISequenceContainer;
            if (startContainer.Status == SequenceEntityStatus.RUNNING) {
                await startContainer.Interrupt();
                await Task.Delay(100);
            }
            if (targetContainer.Status == SequenceEntityStatus.RUNNING) {
                await targetContainer.Interrupt();
            }
            return true;
        }

        public void Skip() {
            if (this.Status != SequenceEntityStatus.DISABLED) {
                this.Status = SequenceEntityStatus.SKIPPED;
                try {
                    localCts?.Cancel();
                } catch { }
            }
        }

        public virtual void Initialize() {
        }

        public virtual void SequenceBlockInitialize() {
        }

        public virtual void SequenceBlockStarted() {
        }

        public virtual void SequenceBlockFinished() {
        }

        public virtual void SequenceBlockTeardown() {
        }

        public virtual void Teardown() {
        }
    }
}