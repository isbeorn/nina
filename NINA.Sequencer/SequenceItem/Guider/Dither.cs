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
using NINA.Profile.Interfaces;

namespace NINA.Sequencer.SequenceItem.Guider {

    [ExportMetadata("Name", "Lbl_SequenceItem_Guider_Dither_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Guider_Dither_Description")]
    [ExportMetadata("Icon", "DitherSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Guider")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class Dither : SequenceItem, IValidatable {
        protected IGuiderMediator guiderMediator;
        protected IProfileService profileService;

        [ImportingConstructor]
        public Dither(IGuiderMediator guiderMediator, IProfileService profileService) {
            this.guiderMediator = guiderMediator;
            this.profileService = profileService;
        }

        private Dither(Dither cloneMe) : this(cloneMe.guiderMediator, cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new Dither(this);
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            //todo missing progress
            return guiderMediator.Dither(token);
        }

        public bool Validate() {
            var i = new List<string>();
            if (!guiderMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblGuiderNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(Dither)}";
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.FromSeconds(profileService.ActiveProfile.GuiderSettings.SettleTimeout);
        }
    }
}