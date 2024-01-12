#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.SafetyMonitor {

    [ExportMetadata("Name", "Lbl_SequenceItem_SafetyMonitor_WaitUntilSafe_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_SafetyMonitor_WaitUntilSafe_Description")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_SafetyMonitor")]
    [Export(typeof(ISequenceItem))]
    public class WaitUntilSafe : SequenceItem, IValidatable {
        private ISafetyMonitorMediator safetyMonitorMediator;

        [ImportingConstructor]
        public WaitUntilSafe(ISafetyMonitorMediator safetyMonitorMediator) {
            this.safetyMonitorMediator = safetyMonitorMediator;
        }

        private WaitUntilSafe(WaitUntilSafe cloneMe) : this(cloneMe.safetyMonitorMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WaitUntilSafe(this);
        }

        private bool isSafe;

        public bool IsSafe {
            get => isSafe;
            private set {
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

        public TimeSpan WaitInterval { get; set; } = TimeSpan.FromSeconds(5);

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

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitUntilSafe)}";
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var info = safetyMonitorMediator.GetInfo();
            IsSafe = info.Connected && info.IsSafe;
            while (!IsSafe) {
                progress?.Report(new ApplicationStatus() { Status = Loc.Instance["Lbl_SequenceItem_SafetyMonitor_WaitUntilSafe_Waiting"] });
                await CoreUtil.Wait(WaitInterval, token, default);

                info = safetyMonitorMediator.GetInfo();
                IsSafe = info.Connected && info.IsSafe;
            }
        }
    }
}