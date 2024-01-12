#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Core.Enum;
using NINA.Sequencer.Utility;
using NINA.Sequencer.SequenceItem.Utility;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_SafetyMonitorCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_SafetyMonitorCondition_Description")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    public class SafetyMonitorCondition : SequenceCondition, IValidatable {
        protected ISafetyMonitorMediator safetyMonitorMediator;

        [ImportingConstructor]
        public SafetyMonitorCondition(ISafetyMonitorMediator safetyMonitorMediator) {
            this.safetyMonitorMediator = safetyMonitorMediator;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenUnsafe, TimeSpan.FromSeconds(5));
        }

        protected SafetyMonitorCondition(SafetyMonitorCondition cloneMe) : this(cloneMe.safetyMonitorMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SafetyMonitorCondition(this);
        }

        private bool isSafe;

        public bool IsSafe {
            get => isSafe;
            protected set {
                isSafe = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate() {
            var i = new List<string>();
            var info = safetyMonitorMediator.GetInfo();

            if (!info.Connected) {
                i.Add(Loc.Instance["LblSafetyMonitorNotConnected"]);
            } else {
                IsSafe = info.IsSafe;
            }

            Issues = i;
            return i.Count == 0;
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            var info = safetyMonitorMediator.GetInfo();
            IsSafe = info.Connected && info.IsSafe; 
            if (!IsSafe && IsActive()) {
                Logger.Info($"{nameof(SafetyMonitorCondition)} finished. Status=Unsafe");
            }
            return IsSafe;
        }

        public override void AfterParentChanged() {
            if (Parent == null) {
                SequenceBlockTeardown();
            } else {
                if (Parent.Status == SequenceEntityStatus.RUNNING) {
                    SequenceBlockInitialize();
                }
            }
        }

        public override void SequenceBlockTeardown() {
            try { ConditionWatchdog?.Cancel(); } catch { }
        }

        public override void SequenceBlockInitialize() {
            ConditionWatchdog?.Start();
        }

        private async Task InterruptWhenUnsafe() {
            if (!Check(null, null)) {
                if (this.Parent != null) {
                    if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                        Logger.Info("Unsafe conditions detected - Interrupting current Instruction Set");
                        await this.Parent.Interrupt();
                    }
                }
            }
        }

        public override string ToString() {
            return $"Condition: {nameof(SafetyMonitorCondition)}";
        }
    }

    [ExportMetadata("Name", "Lbl_SequenceCondition_LoopWhileUnsafe_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_LoopWhileUnsafe_Description")]
    [ExportMetadata("Icon", "UnshieldSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    public class LoopWhileUnsafe : SafetyMonitorCondition {

        [ImportingConstructor]
        public LoopWhileUnsafe(ISafetyMonitorMediator safetyMonitorMediator) : base(safetyMonitorMediator) {
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            var info = safetyMonitorMediator.GetInfo();
            IsSafe = info.Connected && info.IsSafe;
            if (IsSafe && IsActive()) {
                Logger.Info($"{nameof(LoopWhileUnsafe)} finished. Status=Safe");
            }
            return !IsSafe;
        }

        protected LoopWhileUnsafe(LoopWhileUnsafe cloneMe) : base(cloneMe) {
        }

        public override object Clone() {
            return new LoopWhileUnsafe(this);
        }

        public override string ToString() {
            return $"Condition: {nameof(LoopWhileUnsafe)}";
        }
    }
}