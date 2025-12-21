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
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [ExportMetadata("Name", "Lbl_SequenceItem_Telescope_WaitUntilTelescopeParked_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_WaitUntilTelescopeParked_Description")]
    [ExportMetadata("Icon", "ParkSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitUntilTelescopeParked : SequenceItem, IValidatable {

        [ImportingConstructor]
        public WaitUntilTelescopeParked(ITelescopeMediator telescopeMediator) {
            this.telescopeMediator = telescopeMediator;
        }

        private WaitUntilTelescopeParked(WaitUntilTelescopeParked cloneMe) : this(cloneMe.telescopeMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WaitUntilTelescopeParked(this);
        }

        private ITelescopeMediator telescopeMediator;
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Poll until telescope is parked
            while (true) {
                token.ThrowIfCancellationRequested();

                var info = telescopeMediator.GetInfo();

                if (info.AtPark) {
                    // Telescope is parked, we can continue
                    return;
                }

                // Report status while waiting
                progress?.Report(new ApplicationStatus { Status = Loc.Instance["Lbl_SequenceItem_Telescope_WaitUntilTelescopeParked_Waiting"] });

                // Wait 1 second before checking again
                await Task.Delay(1000, token);
            }
        }

        public bool Validate() {
            var i = new List<string>();
            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitUntilTelescopeParked)}";
        }
    }
}
