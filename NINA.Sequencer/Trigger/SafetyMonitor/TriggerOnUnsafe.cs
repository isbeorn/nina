using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.SafetyMonitor;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger.SafetyMonitor {
    [ExportMetadata("Name", "Lbl_SequenceTrigger_TriggerOnUnsafe_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_TriggerOnUnsafe_Description")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_SafetyMonitor")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TriggerOnUnsafe : SequenceTrigger, IValidatable {
        private IList<string> issues = new List<string>();
        private readonly ISafetyMonitorMediator safetyMonitorMediator;
        private readonly IApplicationResourceDictionary resourceDictionary;
        private readonly object triggerLock = new object();
        private bool isExpanded = true;
        private bool shouldTrigger;
        private bool triggerIsRunning;
        private bool hasSeenConnected;
        private TriggerExecutionPhase triggerExecutionPhase = TriggerExecutionPhase.None;
        private CancellationTokenSource afterWaitForSafeCancellation;
        private bool restartAfterWaitForSafe;

        private enum TriggerExecutionPhase {
            None,
            BeforeWaitForSafe,
            WaitUntilSafe,
            AfterWaitForSafe
        }

        [ImportingConstructor]
        public TriggerOnUnsafe(ISafetyMonitorMediator safetyMonitorMediator, IApplicationResourceDictionary resourceDictionary) {
            this.safetyMonitorMediator = safetyMonitorMediator;
            this.resourceDictionary = resourceDictionary;
            BeforeWaitForSafe = new SequentialContainer() { Name = Loc.Instance["Lbl_SequenceTrigger_TriggerOnUnsafe_BeforeWaitingUntilSafe_Description"], IsExpanded = true }.AddMetaData(resourceDictionary) as SequentialContainer;
            AfterWaitForSafe = new SequentialContainer() { Name = Loc.Instance["Lbl_SequenceTrigger_TriggerOnUnsafe_AfterWaitingUntilSafe_Description"], IsExpanded = true }.AddMetaData(resourceDictionary) as SequentialContainer;
            WaitUntilSafe = new WaitUntilSafe(safetyMonitorMediator).AddMetaData(resourceDictionary) as WaitUntilSafe;
        }

        private TriggerOnUnsafe(TriggerOnUnsafe cloneMe) : this(cloneMe.safetyMonitorMediator, cloneMe.resourceDictionary) {
            CopyMetaData(cloneMe);
            BeforeWaitForSafe = (SequentialContainer)cloneMe.BeforeWaitForSafe.Clone();
            AfterWaitForSafe = (SequentialContainer)cloneMe.AfterWaitForSafe.Clone();
            WaitUntilSafe = (WaitUntilSafe)cloneMe.WaitUntilSafe.Clone();
            IsExpanded = cloneMe.IsExpanded;
        }


        public bool Validate() {
            List<string> i = new List<string>();

            BeforeWaitForSafe.Validate();
            AfterWaitForSafe.Validate();
            i.AddRange(BeforeWaitForSafe.Issues);
            i.AddRange(AfterWaitForSafe.Issues);

            Issues = i;
            return true;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            return ShouldRunUnsafeTrigger();
        }

        public override bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem) {
            return ShouldRunUnsafeTrigger();
        }

        private bool ShouldRunUnsafeTrigger() {
            var info = safetyMonitorMediator.GetInfo();
            lock (triggerLock) {
                if (info.Connected) {
                    hasSeenConnected = true;
                    if (info.IsSafe) {
                        shouldTrigger = false;
                        return false;
                    }
                } else if (!hasSeenConnected) {
                    shouldTrigger = false;
                    return false;
                }

                shouldTrigger = true;
                return shouldTrigger && !triggerIsRunning;
            }
        }

        [JsonProperty]
        public SequentialContainer BeforeWaitForSafe {
            get;
            set;
        }

        [JsonProperty]
        public SequentialContainer AfterWaitForSafe {
            get;
            set;
        }

        [JsonProperty]
        public bool IsExpanded {
            get => isExpanded;
            set {
                isExpanded = value;
                RaisePropertyChanged();
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public WaitUntilSafe WaitUntilSafe { get; private set; }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            ISequenceContainer originalTriggerRunnerParent = TriggerRunner.Parent;
            ISequenceContainer originalBeforeWaitForSafeParent = BeforeWaitForSafe.Parent;
            ISequenceContainer originalAfterWaitForSafeParent = AfterWaitForSafe.Parent;
            ISequenceContainer runtimeParent = ItemUtility.CreateTriggerRunnerContext(context ?? Parent);
            bool restoreCollapsedAfterExecution = !IsExpanded;

            lock (triggerLock) {
                shouldTrigger = false;
                triggerIsRunning = true;
            }

            IsExpanded = true;

            try {
                // Run the unsafe instruction sets against an isolated context proxy. That keeps
                // target/root data available for inherited instructions, but prevents the nested
                // runner from climbing back into the live ancestor trigger set while it executes.
                if (!ReferenceEquals(TriggerRunner.Parent, runtimeParent)) {
                    TriggerRunner.AttachNewParent(runtimeParent);
                }

                BeforeWaitForSafe.ResetProgress();
                WaitUntilSafe.ResetProgress();
                AfterWaitForSafe.ResetProgress();
                await ConfigureTriggerRunner();

                Logger.Info("Unsafe conditions detected, running Trigger On Unsafe");
                await RunTriggerRunnerGuarded(progress, token);
            } finally {
                lock (triggerLock) {
                    if (!ReferenceEquals(TriggerRunner.Parent, originalTriggerRunnerParent)) {
                        TriggerRunner.AttachNewParent(originalTriggerRunnerParent);
                    }

                    BeforeWaitForSafe.AttachNewParent(originalBeforeWaitForSafeParent);
                    AfterWaitForSafe.AttachNewParent(originalAfterWaitForSafeParent);
                    BeforeWaitForSafe.ResetProgress();
                    WaitUntilSafe.ResetProgress();
                    AfterWaitForSafe.ResetProgress();

                    triggerIsRunning = false;
                    triggerExecutionPhase = TriggerExecutionPhase.None;
                    afterWaitForSafeCancellation = null;
                    restartAfterWaitForSafe = false;
                    shouldTrigger = IsUnsafe();
                }

                if (restoreCollapsedAfterExecution) {
                    IsExpanded = false;
                }
            }
        }

        private async Task ConfigureTriggerRunner() {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess()) {
                await dispatcher.InvokeAsync(ConfigureTriggerRunnerCore, System.Windows.Threading.DispatcherPriority.Normal);
            } else {
                ConfigureTriggerRunnerCore();
            }
        }

        private void ConfigureTriggerRunnerCore() {
            foreach (var item in TriggerRunner.GetItemsSnapshot()) {
                TriggerRunner.Remove(item);
            }

            TriggerRunner.Add(BeforeWaitForSafe);
            TriggerRunner.Add(WaitUntilSafe);
            TriggerRunner.Add(AfterWaitForSafe);
        }

        private async Task RunTriggerRunnerGuarded(IProgress<ApplicationStatus> progress, CancellationToken token) {
            while (true) {
                ResetTriggerRunnerSteps();
                ClearAfterWaitForSafeRestart();

                await RunTriggerRunnerStep(BeforeWaitForSafe, TriggerExecutionPhase.BeforeWaitForSafe, progress, token);
                await RunTriggerRunnerStep(WaitUntilSafe, TriggerExecutionPhase.WaitUntilSafe, progress, token);

                bool afterWaitForSafeCompleted = await RunAfterWaitForSafeGuarded(progress, token);
                if (afterWaitForSafeCompleted && !ConsumeAfterWaitForSafeRestart() && !IsUnsafe()) {
                    return;
                }

                Logger.Info("Unsafe conditions detected while running Trigger On Unsafe after-safety instructions, restarting Trigger On Unsafe");
            }
        }

        private void ResetTriggerRunnerSteps() {
            BeforeWaitForSafe.ResetProgress();
            WaitUntilSafe.ResetProgress();
            AfterWaitForSafe.ResetProgress();
        }

        private async Task RunTriggerRunnerStep(ISequenceItem item, TriggerExecutionPhase phase, IProgress<ApplicationStatus> progress, CancellationToken token) {
            SetTriggerExecutionPhase(phase);
            try {
                await item.Run(progress, token);
            } finally {
                ClearTriggerExecutionPhase(phase);
            }
        }

        private async Task<bool> RunAfterWaitForSafeGuarded(IProgress<ApplicationStatus> progress, CancellationToken token) {
            using (var guardCts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                SetAfterWaitForSafeGuard(guardCts);
                try {
                    if (IsUnsafe()) {
                        RequestAfterWaitForSafeRestart();
                        return false;
                    }

                    await AfterWaitForSafe.Run(progress, guardCts.Token);

                    if (IsUnsafe()) {
                        RequestAfterWaitForSafeRestart();
                        return false;
                    }

                    return true;
                } catch (OperationCanceledException) when (!token.IsCancellationRequested && ConsumeAfterWaitForSafeRestart()) {
                    return false;
                } finally {
                    ClearAfterWaitForSafeGuard(guardCts);
                }
            }
        }

        private void SetTriggerExecutionPhase(TriggerExecutionPhase phase) {
            lock (triggerLock) {
                triggerExecutionPhase = phase;
            }
        }

        private void ClearTriggerExecutionPhase(TriggerExecutionPhase phase) {
            lock (triggerLock) {
                if (triggerExecutionPhase == phase) {
                    triggerExecutionPhase = TriggerExecutionPhase.None;
                }
            }
        }

        private void SetAfterWaitForSafeGuard(CancellationTokenSource cancellationTokenSource) {
            lock (triggerLock) {
                triggerExecutionPhase = TriggerExecutionPhase.AfterWaitForSafe;
                afterWaitForSafeCancellation = cancellationTokenSource;
            }
        }

        private void ClearAfterWaitForSafeGuard(CancellationTokenSource cancellationTokenSource) {
            lock (triggerLock) {
                if (ReferenceEquals(afterWaitForSafeCancellation, cancellationTokenSource)) {
                    afterWaitForSafeCancellation = null;
                }

                if (triggerExecutionPhase == TriggerExecutionPhase.AfterWaitForSafe) {
                    triggerExecutionPhase = TriggerExecutionPhase.None;
                }
            }
        }

        private void RequestAfterWaitForSafeRestart() {
            lock (triggerLock) {
                shouldTrigger = true;
                restartAfterWaitForSafe = true;
            }
        }

        private void ClearAfterWaitForSafeRestart() {
            lock (triggerLock) {
                restartAfterWaitForSafe = false;
            }
        }

        private bool ConsumeAfterWaitForSafeRestart() {
            lock (triggerLock) {
                bool restart = restartAfterWaitForSafe;
                restartAfterWaitForSafe = false;
                return restart;
            }
        }

        public override void AfterParentChanged() {
            AttachInstructionSetsToContext(Parent);

            if (Parent == null) {
                SequenceBlockTeardown();
            } else if (Parent.Status == SequenceEntityStatus.RUNNING) {
                SequenceBlockInitialize();
            }
        }

        private void AttachInstructionSetsToContext(ISequenceContainer parent) {
            ISequenceContainer instructionSetParent = ItemUtility.CreateTriggerRunnerContext(parent);
            BeforeWaitForSafe.AttachNewParent(instructionSetParent);
            AfterWaitForSafe.AttachNewParent(instructionSetParent);
        }

        public override void SequenceBlockInitialize() {
            SequenceBlockTeardown();

            lock (triggerLock) {
                shouldTrigger = false;
                triggerIsRunning = false;
                hasSeenConnected = safetyMonitorMediator.GetInfo().Connected;
            }

            safetyMonitorMediator.Connected += SafetyMonitorMediator_Connected;
            safetyMonitorMediator.IsSafeChanged += SafetyMonitorMediator_IsSafeChanged;
            safetyMonitorMediator.Disconnected += SafetyMonitorMediator_Disconnected;

            QueueIfUnsafe();
        }

        public override void SequenceBlockTeardown() {
            safetyMonitorMediator.Connected -= SafetyMonitorMediator_Connected;
            safetyMonitorMediator.IsSafeChanged -= SafetyMonitorMediator_IsSafeChanged;
            safetyMonitorMediator.Disconnected -= SafetyMonitorMediator_Disconnected;

            lock (triggerLock) {
                shouldTrigger = false;
                triggerIsRunning = false;
                hasSeenConnected = false;
            }
        }

        private Task SafetyMonitorMediator_Connected(object sender, EventArgs e) {
            lock (triggerLock) {
                hasSeenConnected = true;
            }

            QueueIfUnsafe();
            return Task.CompletedTask;
        }

        private void SafetyMonitorMediator_IsSafeChanged(object sender, IsSafeEventArgs e) {
            if (!e.IsSafe) {
                QueueIfUnsafe();
            }
        }

        private Task SafetyMonitorMediator_Disconnected(object sender, EventArgs e) {
            QueueIfUnsafe();
            return Task.CompletedTask;
        }

        private void QueueIfUnsafe() {
            if (!IsUnsafe() || !IsActive()) {
                return;
            }

            CancellationTokenSource afterWaitForSafeCancellationToCancel = null;
            bool shouldSkipRunningItems;
            lock (triggerLock) {
                bool wasAlreadyQueued = shouldTrigger;
                shouldTrigger = true;
                if (triggerIsRunning && triggerExecutionPhase == TriggerExecutionPhase.AfterWaitForSafe) {
                    restartAfterWaitForSafe = true;
                    afterWaitForSafeCancellationToCancel = afterWaitForSafeCancellation;
                }

                shouldSkipRunningItems = !wasAlreadyQueued && !triggerIsRunning;
            }

            try {
                afterWaitForSafeCancellationToCancel?.Cancel();
            } catch { }

            if (shouldSkipRunningItems) {
                ItemUtility.GetRootContainer(Parent)?.InterruptAndResetCurrentRunningItems();
            }
        }

        private bool IsUnsafe() {
            var info = safetyMonitorMediator.GetInfo();
            lock (triggerLock) {
                if (info.Connected) {
                    hasSeenConnected = true;
                    return !info.IsSafe;
                }

                return hasSeenConnected;
            }
        }

        private bool IsActive() {
            return Parent != null
                && ItemUtility.IsInRootContainer(Parent)
                && Parent.Status == SequenceEntityStatus.RUNNING
                && Status != SequenceEntityStatus.DISABLED;
        }

        public override object Clone() {
            return new TriggerOnUnsafe(this);
        }

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        public override string ToString() {
            return $"Trigger: {nameof(TriggerOnUnsafe)}";
        }
    }
}
