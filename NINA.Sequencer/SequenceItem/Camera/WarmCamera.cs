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

namespace NINA.Sequencer.SequenceItem.Camera {

    [ExportMetadata("Name", "Lbl_SequenceItem_Camera_WarmCamera_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Camera_WarmCamera_Description")]
    [ExportMetadata("Icon", "Fire_NoFill_SVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WarmCamera : SequenceItem, IValidatable {

        [ImportingConstructor]
        public WarmCamera(ICameraMediator cameraMediator) {
            this.cameraMediator = cameraMediator;
        }

        private WarmCamera(WarmCamera cloneMe) : this(cloneMe.cameraMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WarmCamera(this) {
                Duration = Duration
            };
        }

        private ICameraMediator cameraMediator;

        [JsonProperty]
        public double Duration { get; set; } = 0;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return cameraMediator.WarmCamera(TimeSpan.FromMinutes(Duration), progress, token);
        }

        public bool Validate() {
            var i = new List<string>();
            var info = cameraMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else if (!info.CanSetTemperature) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Validation_CameraCannotSetTemperature"]);
            }

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override TimeSpan GetEstimatedDuration() {
            return Duration > 0 ? TimeSpan.FromMinutes(Duration) : TimeSpan.FromMinutes(1);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WarmCamera)}, Duration: {Duration}";
        }
    }
}