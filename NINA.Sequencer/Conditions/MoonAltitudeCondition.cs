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
using NINA.Profile.Interfaces;
using NINA.Astrometry;
using System;
using System.ComponentModel.Composition;
using static NINA.Sequencer.Utility.ItemUtility;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Core.Enum;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_MoonAltitudeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_MoonAltitudeCondition_Description")]
    [ExportMetadata("Icon", "WaningGibbousMoonSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MoonAltitudeCondition : LoopForSunMoonAltitudeBase {

        [ImportingConstructor]
        public MoonAltitudeCondition(IProfileService profileService) : base(profileService, useCustomHorizon: false) {
           InterruptReason = "Moon is outside of the specified altitude range";
        }

        private MoonAltitudeCondition(MoonAltitudeCondition cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new MoonAltitudeCondition(this) {
                Data = Data.Clone()
            };
        }

        //h = -0.583 degrees -: Moon's upper limb touches the horizon; atmospheric refraction accounted for
        public override void CalculateExpectedTime() {
            Data.Coordinates.Coordinates = CalculateMoonRADec(Data.Observer);
            Data.CurrentAltitude = AstroUtil.GetMoonAltitude(DateTime.Now, Data.Observer);
            CalculateExpectedTimeCommon(Data, -.583, until: true, 60, AstroUtil.GetMoonAltitude);
        }
    }
}