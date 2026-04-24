#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Sequencer.Trigger.Utility {

    [ExportMetadata("Name", "Lbl_SequenceTrigger_CustomTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_CustomTrigger_Description")]
    [ExportMetadata("Icon", "PuzzlePieceSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class CustomTrigger : SequenceTrigger, IValidatable {
        private readonly IApplicationResourceDictionary resourceDictionary;
        private readonly TriggerSourceParent triggerSourceParent;
        private IList<string> issues = new List<string>();
        private ISequenceTrigger triggerSource;

        [ImportingConstructor]
        public CustomTrigger(IApplicationResourceDictionary resourceDictionary) {
            this.resourceDictionary = resourceDictionary;
            triggerSourceParent = new TriggerSourceParent(this);
            TriggerRunner = CreateAreaContainer("Lbl_SequenceTrigger_CustomTrigger_Instructions_Name");
        }

        private CustomTrigger(CustomTrigger cloneMe) : this(cloneMe.resourceDictionary) {
            CopyMetaData(cloneMe);
            TriggerRunner = (SequentialContainer)cloneMe.TriggerRunner.Clone();
            TriggerSource = (ISequenceTrigger)cloneMe.TriggerSource?.Clone();
            AttachTriggerSourceToParent();
        }

        public override bool AllowMultiplePerSet => true;

        [JsonProperty]
        public ISequenceTrigger TriggerSource {
            get => triggerSource;
            set {
                if (IsInvalidTriggerSource(value)) {
                    return;
                }

                if (ReferenceEquals(triggerSource, value)) {
                    return;
                }

                if (triggerSource != null && ReferenceEquals(triggerSource.Parent, triggerSourceParent)) {
                    triggerSource.AttachNewParent(null);
                }

                triggerSource = value;
                AttachTriggerSourceToParent();
                RaisePropertyChanged();
            }
        }

        public ICommand DropIntoTriggerSourceCommand => new RelayCommand<DropIntoParameters>(DropInTriggerSource);

        public IList<string> Issues {
            get => issues;
            set {
                issues = [.. value];
                RaisePropertyChanged();
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            EnsureAreaContainers();
            AttachTriggerRunnerToContext(Parent);
            AttachTriggerSourceToParent();
        }

        public override object Clone() {
            return new CustomTrigger(this);
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            return TriggerSource != null
                && TriggerSource.Status != SequenceEntityStatus.DISABLED
                && TriggerSource.ShouldTrigger(previousItem, nextItem);
        }

        public override bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem) {
            return TriggerSource != null
                && TriggerSource.Status != SequenceEntityStatus.DISABLED
                && TriggerSource.ShouldTriggerAfter(previousItem, nextItem);
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            ISequenceContainer originalParent = TriggerRunner.Parent;
            // Execute custom instructions against an isolated context proxy instead of the live
            // ancestor chain. That keeps target/root data available, but prevents the runner from
            // walking back into the owning trigger set and re-triggering this CustomTrigger on itself.
            ISequenceContainer runtimeParent = ItemUtility.CreateTriggerRunnerContext(context ?? Parent);

            if (!ReferenceEquals(TriggerRunner.Parent, runtimeParent)) {
                TriggerRunner.AttachNewParent(runtimeParent);
            }

            try {
                TriggerRunner.ResetAll();
                await TriggerRunner.Run(progress, token);
            } finally {
                if (!ReferenceEquals(TriggerRunner.Parent, originalParent)) {
                    TriggerRunner.AttachNewParent(originalParent);
                }
            }
        }

        public override void AfterParentChanged() {
            EnsureAreaContainers();
            AttachTriggerRunnerToContext(Parent);
            AttachTriggerSourceToParent();
            Validate();
        }

        public override void Initialize() {
            TriggerSource?.Initialize();
        }

        public override void SequenceBlockInitialize() {
            triggerSourceParent.Status = Parent?.Status ?? SequenceEntityStatus.CREATED;
            TriggerSource?.SequenceBlockInitialize();
        }

        public override void SequenceBlockStarted() {
            triggerSourceParent.Status = Parent?.Status ?? SequenceEntityStatus.RUNNING;
            TriggerSource?.SequenceBlockStarted();
        }

        public override void SequenceBlockFinished() {
            TriggerSource?.SequenceBlockFinished();
        }

        public override void SequenceBlockTeardown() {
            TriggerSource?.SequenceBlockTeardown();
            triggerSourceParent.Status = SequenceEntityStatus.CREATED;
        }

        public override void Teardown() {
            TriggerSource?.Teardown();
        }

        public bool Validate() {
            List<string> i = new List<string>();
            bool valid = true;

            if (TriggerSource == null) {
                i.Add(Loc.Instance["Lbl_SequenceTrigger_CustomTrigger_NoTriggerSource_Issue"]);
                valid = false;
            } else if (TriggerSource is IValidatable validatableTrigger) {
                valid = validatableTrigger.Validate() && valid;
                i.AddRange(validatableTrigger.Issues);
            }

            if (!TriggerRunner.GetItemsSnapshot().Any()) {
                i.Add(Loc.Instance["Lbl_SequenceTrigger_CustomTrigger_NoInstructions_Issue"]);
                valid = false;
            }

            TriggerRunner.Validate();
            i.AddRange(TriggerRunner.Issues);

            Issues = i;
            return valid;
        }

        public override string ToString() {
            return $"Trigger: {nameof(CustomTrigger)}";
        }

        private void DropInTriggerSource(DropIntoParameters parameters) {
            if (parameters?.Source is not ISequenceTrigger source) {
                return;
            }

            if (IsInvalidTriggerSource(source)) {
                return;
            }

            ISequenceTrigger trigger = source.Parent != null && !parameters.Duplicate
                ? source
                : (ISequenceTrigger)source.Clone();

            if (trigger.Parent != null && !ReferenceEquals(trigger.Parent, triggerSourceParent)) {
                trigger.Parent.Remove(trigger);
            }

            TriggerSource = trigger;
            Validate();
        }

        private bool IsInvalidTriggerSource(ISequenceTrigger trigger) {
            return ReferenceEquals(trigger, this) || trigger is CustomTrigger;
        }

        private void EnsureAreaContainers() {
            TriggerRunner ??= CreateAreaContainer("Lbl_SequenceTrigger_CustomTrigger_Instructions_Name");

            if (string.IsNullOrWhiteSpace(TriggerRunner.Name)) {
                TriggerRunner.Name = Loc.Instance["Lbl_SequenceTrigger_CustomTrigger_Instructions_Name"];
            }
        }

        private void AttachTriggerRunnerToContext(ISequenceContainer parent) {
            TriggerRunner?.AttachNewParent(ItemUtility.CreateTriggerRunnerContext(parent));
        }

        private SequentialContainer CreateAreaContainer(string labelKey) {
            SequentialContainer container = new SequentialContainer() {
                Name = Loc.Instance[labelKey],
                IsExpanded = true
            };

            return container.AddMetaData(resourceDictionary) as SequentialContainer;
        }

        private void AttachTriggerSourceToParent() {
            triggerSourceParent.AttachNewParent(Parent);
            triggerSourceParent.Status = Parent?.Status ?? SequenceEntityStatus.CREATED;
            TriggerSource?.AttachNewParent(triggerSourceParent);
        }

        private void ClearTriggerSource(ISequenceTrigger trigger) {
            if (!ReferenceEquals(TriggerSource, trigger)) {
                return;
            }

            trigger.AttachNewParent(null);
            triggerSource = null;
            RaisePropertyChanged(nameof(TriggerSource));
            Validate();
        }

        private sealed class TriggerSourceParent : SequentialContainer {
            private readonly CustomTrigger owner;

            public TriggerSourceParent(CustomTrigger owner) {
                this.owner = owner;
            }

            public override bool Remove(ISequenceTrigger trigger) {
                if (ReferenceEquals(owner.TriggerSource, trigger)) {
                    owner.ClearTriggerSource(trigger);
                    return true;
                }

                return base.Remove(trigger);
            }
        }
    }
}
