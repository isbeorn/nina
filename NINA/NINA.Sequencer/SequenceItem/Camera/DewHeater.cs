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

namespace NINA.Sequencer.SequenceItem.Camera {

    [ExportMetadata("Name", "Lbl_SequenceItem_Camera_DewHeater_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Camera_DewHeater_Description")]
    [ExportMetadata("Icon", "AntiDropsSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class DewHeater : SequenceItem, IValidatable {

        [ImportingConstructor]
        public DewHeater(ICameraMediator cameraMediator) {
            this.cameraMediator = cameraMediator;
        }

        private DewHeater(DewHeater cloneMe) : this(cloneMe.cameraMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new DewHeater(this) {
                OnOff = OnOff
            };
        }

        private ICameraMediator cameraMediator;

        private bool onOff = false;

        [JsonProperty]
        public bool OnOff {
            get => onOff;
            set {
                onOff = value;
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

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            cameraMediator.SetDewHeater(OnOff);
            return Task.CompletedTask;
        }

        public bool Validate() {
            var i = new List<string>();
            var info = cameraMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else if (!info.HasDewHeater) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Validation_Camera_DewHeater_NotPresent"]);
            }

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(DewHeater)}, Heater: {(OnOff ? "On" : "Off")}";
        }
    }
}