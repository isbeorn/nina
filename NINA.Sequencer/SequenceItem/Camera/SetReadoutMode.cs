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

    [ExportMetadata("Name", "Lbl_SequenceItem_Camera_SetReadoutMode_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Camera_SetReadoutMode_Description")]
    [ExportMetadata("Icon", "CameraDetailsSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetReadoutMode : SequenceItem, IValidatable {

        [ImportingConstructor]
        public SetReadoutMode(ICameraMediator cameraMediator) {
            this.cameraMediator = cameraMediator;
        }

        private SetReadoutMode(SetReadoutMode cloneMe) : this(cloneMe.cameraMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SetReadoutMode(this) {
                Mode = Mode
            };
        }

        private ICameraMediator cameraMediator;

        [JsonProperty]
        public short Mode { get; set; } = 0;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            cameraMediator.SetReadoutModeForNormalImages(Mode);
            return Task.CompletedTask;
        }

        public bool Validate() {
            var i = new List<string>();
            var info = cameraMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else if (Mode < 0 || Mode >= info.ReadoutModes.Count()) {
                i.Add(String.Format(Loc.Instance["Lbl_SequenceItem_Validation_InvalidReadoutMode"], info.ReadoutModes.Count() - 1));
            }

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.Zero;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetReadoutMode)}, Mode: {Mode}";
        }
    }
}