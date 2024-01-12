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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;

namespace NINA.Sequencer.SequenceItem.Guider {

    [ExportMetadata("Name", "Lbl_SequenceItem_Guider_StartGuiding_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Guider_StartGuiding_Description")]
    [ExportMetadata("Icon", "GuiderSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Guider")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StartGuiding : SequenceItem, IValidatable {
        private IGuiderMediator guiderMediator;

        [ImportingConstructor]
        public StartGuiding(IGuiderMediator guiderMediator) {
            this.guiderMediator = guiderMediator;
        }

        private StartGuiding(StartGuiding cloneMe) : this(cloneMe.guiderMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new StartGuiding(this) {
                ForceCalibration = ForceCalibration
            };
        }

        [JsonProperty]
        public bool ForceCalibration { get; set; } = false;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (!await guiderMediator.StartGuiding(ForceCalibration, progress, token)) {
                throw new SequenceEntityFailedException("Failed to start guiding");
            }
        }

        public bool Validate() {
            bool validated = true;
            var i = new List<string>();
            if (!guiderMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblGuiderNotConnected"]);
                validated = false;
            }
            if (ForceCalibration && !guiderMediator.GetInfo().CanClearCalibration) {
                i.Add(Loc.Instance["LblGuiderCannotClearCalibration"]);
            }
            Issues = i;

            return validated;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(StartGuiding)}";
        }
    }
}