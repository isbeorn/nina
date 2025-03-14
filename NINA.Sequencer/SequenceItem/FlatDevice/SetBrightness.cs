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
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.FlatDevice {

    [ExportMetadata("Name", "Lbl_SequenceItem_FlatDevice_SetBrightness_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FlatDevice_SetBrightness_Description")]
    [ExportMetadata("Icon", "BrightnessSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_FlatDevice")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class SetBrightness : SequenceItem, IValidatable {

        [ImportingConstructor]
        public SetBrightness(IFlatDeviceMediator flatDeviceMediator) {
            this.flatDeviceMediator = flatDeviceMediator;
        }

        private SetBrightness(SetBrightness cloneMe) : this(cloneMe.flatDeviceMediator) {
            CopyMetaData(cloneMe);
        }

        private IFlatDeviceMediator flatDeviceMediator;
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        [IsExpression]
        private int brightness;

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await flatDeviceMediator.SetBrightness(Brightness, progress, token);

            var brightnessState = flatDeviceMediator.GetInfo().Brightness;
            var minBrightness = flatDeviceMediator.GetInfo().MinBrightness;
            var maxBrightness = flatDeviceMediator.GetInfo().MaxBrightness;

            // we shouldn't consider the flatdevice bringing the brightness up to to min or down to the max a failure
            if (Brightness < minBrightness && brightnessState == minBrightness) {
                return;
            }

            if (Brightness > maxBrightness && brightnessState == maxBrightness) {
                return;
            }

            if (brightnessState != Brightness) {
                throw new SequenceEntityFailedException($"Failed to set brightness. Current brightness: {brightnessState}");
            }
        }

        public bool Validate() {
            var i = new List<string>();
            var info = flatDeviceMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblFlatDeviceNotConnected"]);
            } else {
                if (!info.SupportsOnOff) {
                    i.Add(Loc.Instance["LblFlatDeviceCannotControlBrightness"]);
                }
            }

            Expression.ValidateExpressions(i, BrightnessExpression);
            
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetBrightness)}, {nameof(Brightness)}: {Brightness}";
        }
    }
}