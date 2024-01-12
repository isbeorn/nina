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

namespace NINA.Sequencer.SequenceItem.Telescope {

    [ExportMetadata("Name", "Lbl_SequenceItem_Telescope_ParkScope_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_ParkScope_Description")]
    [ExportMetadata("Icon", "ParkSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ParkScope : SequenceItem, IValidatable {

        [ImportingConstructor]
        public ParkScope(ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator) {
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
        }

        private ParkScope(ParkScope cloneMe) : this(cloneMe.telescopeMediator, cloneMe.guiderMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new ParkScope(this);
        }

        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await guiderMediator.StopGuiding(token);
            if (!await telescopeMediator.ParkTelescope(progress, token)) {
                throw new SequenceEntityFailedException("Failed to park telescope");
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
            return $"Category: {Category}, Item: {nameof(ParkScope)}";
        }
    }
}