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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.WPF.Base.Mediator;

namespace NINA.Sequencer.SequenceItem.Dome {

    [ExportMetadata("Name", "Lbl_SequenceItem_Dome_OpenDomeShutter_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Dome_OpenDomeShutter_Description")]
    [ExportMetadata("Icon", "ObservatorySVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Dome")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class OpenDomeShutter : SequenceItem, IValidatable {

        [ImportingConstructor]
        public OpenDomeShutter(IDomeMediator domeMediator) {
            this.domeMediator = domeMediator;
        }

        private OpenDomeShutter(OpenDomeShutter cloneMe) : this(cloneMe.domeMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new OpenDomeShutter(this);
        }

        private IDomeMediator domeMediator;
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var success = await domeMediator.OpenShutter(token);

            var shutterState = domeMediator.GetInfo().ShutterStatus;
            if (!success || shutterState != Equipment.Interfaces.ShutterState.ShutterOpen) {
                throw new SequenceEntityFailedException($"Failed to open dome shutter. Current shutter state: {shutterState}");
            }
        }

        public bool Validate() {
            var i = new List<string>();
            if (!domeMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblDomeNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(OpenDomeShutter)}";
        }
    }
}