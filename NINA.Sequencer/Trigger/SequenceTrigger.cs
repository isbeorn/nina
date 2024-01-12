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
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using NINA.Sequencer.Utility;

namespace NINA.Sequencer.Trigger {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SequenceTrigger : SequenceHasChanged, ISequenceTrigger {

        public SequenceTrigger() {
            TriggerRunner = new SequentialContainer();
        }

        public SequenceTrigger(SequenceTrigger cloneMe) : this() {
            CopyMetaData(cloneMe);
        }

        protected void CopyMetaData(SequenceTrigger cloneMe) {
            Icon = cloneMe.Icon;
            Name = cloneMe.Name;
            Category = cloneMe.Category;
            Description = cloneMe.Description;
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.TriggerRunner?.Items.Clear();
            this.TriggerRunner?.Conditions.Clear();
            this.TriggerRunner?.Triggers.Clear();
        }

        public string Name { get; set; }

        public virtual bool AllowMultiplePerSet => false;

        public string Description { get; set; }
        public GeometryGroup Icon { get; set; }
        public string Category { get; set; }

        private bool showMenu;

        public bool ShowMenu {
            get => showMenu;
            set {
                showMenu = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ShowMenuCommand => new GalaSoft.MvvmLight.Command.RelayCommand<object>((o) => ShowMenu = !ShowMenu, (o) => Status != SequenceEntityStatus.DISABLED);
        public ICommand DisableEnableCommand => new GalaSoft.MvvmLight.Command.RelayCommand(() => {
            if (Status != SequenceEntityStatus.DISABLED) {
                Status = SequenceEntityStatus.DISABLED;
                ShowMenu = false;
            } else {
                Status = SequenceEntityStatus.CREATED;
            }

        });

        [JsonProperty]
        public ISequenceContainer Parent { get; set; }

        [JsonProperty]
        public SequentialContainer TriggerRunner { get; protected set; }

        private SequenceEntityStatus status = SequenceEntityStatus.CREATED;

        public SequenceEntityStatus Status {
            get => status;
            set {
                status = value;
                RaisePropertyChanged();
            }
        }

        //public abstract string Description { get; }

        public async Task Run(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (this.Status == SequenceEntityStatus.DISABLED) { return; }

            Status = SequenceEntityStatus.RUNNING;

            var root = ItemUtility.GetRootContainer(this.Parent);
            try {
                Logger.Info($"Starting {this}");
                this.TriggerRunner.ResetAll();

                if (this is IValidatable && !(this is ISequenceContainer)) {
                    var validatable = this as IValidatable;
                    if (!validatable.Validate()) {
                        throw new SequenceEntityFailedValidationException(string.Join(", ", validatable.Issues));
                    }
                }

                await this.Execute(context, progress, token);
                foreach (var instruction in TriggerRunner.GetItemsSnapshot()) {
                    if (instruction.Status == SequenceEntityStatus.FAILED) {
                        throw new SequenceEntityFailedException($"{instruction} failed to exectue");
                    }
                }
                Status = SequenceEntityStatus.FINISHED;
            } catch (SequenceEntityFailedException ex) {
                Logger.Error($"Failed: {this} - " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
                root?.RaiseFailureEvent(this, ex);
            } catch (SequenceEntityFailedValidationException ex) {
                Status = SequenceEntityStatus.FAILED;
                Logger.Error($"Failed validation: {this} - " + ex.Message);
                root?.RaiseFailureEvent(this, ex);
            } catch (OperationCanceledException) {
                Status = SequenceEntityStatus.CREATED;
            } catch (Exception ex) {
                Status = SequenceEntityStatus.FAILED;
                Logger.Error(ex);
                root?.RaiseFailureEvent(this, ex);
                //Todo Error policy - e.g. Continue; Throw and cancel; Retry;
            }
        }

        public virtual void AfterParentChanged() {
        }

        public void AttachNewParent(ISequenceContainer newParent) {
            Parent = newParent;

            AfterParentChanged();
        }

        public abstract bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem);

        public virtual bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem) {
            return false;
        }

        public abstract Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token);

        public abstract object Clone();

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

        public ICommand DetachCommand => new GalaSoft.MvvmLight.Command.RelayCommand<object>((o) => Detach());

        public ICommand MoveUpCommand => null;

        public ICommand MoveDownCommand => null;

        public void Detach() {
            Parent?.Remove(this);
        }

        public void MoveUp() {
            throw new NotImplementedException();
        }

        public void MoveDown() {
            throw new NotImplementedException();
        }
    }
}