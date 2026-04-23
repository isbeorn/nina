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
        private bool shouldTrigger;
        private bool triggerIsRunning;
        private bool hasSeenConnected;

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

        [Newtonsoft.Json.JsonIgnore]
        public WaitUntilSafe WaitUntilSafe { get; private set; }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            lock (triggerLock) {
                shouldTrigger = false;
                triggerIsRunning = true;
            }

            try {
                BeforeWaitForSafe.ResetProgress();
                WaitUntilSafe.ResetProgress();
                AfterWaitForSafe.ResetProgress();
                await ConfigureTriggerRunner();

                Logger.Info("Unsafe conditions detected, running Trigger On Unsafe");
                await TriggerRunner.Run(progress, token);
            } finally {

                lock (triggerLock) {
                    BeforeWaitForSafe.AttachNewParent(null);
                    AfterWaitForSafe.AttachNewParent(null);
                    BeforeWaitForSafe.ResetProgress();
                    WaitUntilSafe.ResetProgress();
                    AfterWaitForSafe.ResetProgress();

                    triggerIsRunning = false;
                    shouldTrigger = IsUnsafe();
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

        public override void AfterParentChanged() {
            if (Parent == null) {
                SequenceBlockTeardown();
            } else if (Parent.Status == SequenceEntityStatus.RUNNING) {
                SequenceBlockInitialize();
            }
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

            bool shouldSkipRunningItems;
            lock (triggerLock) {
                bool wasAlreadyQueued = shouldTrigger;
                shouldTrigger = true;
                shouldSkipRunningItems = !wasAlreadyQueued && !triggerIsRunning;
            }

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
