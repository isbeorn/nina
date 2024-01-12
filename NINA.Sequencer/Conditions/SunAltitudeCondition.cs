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

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_SunAltitudeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_SunAltitudeCondition_Description")]
    [ExportMetadata("Icon", "SunriseSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SunAltitudeCondition : LoopForSunMoonAltitudeBase {

        [ImportingConstructor]
        public SunAltitudeCondition(IProfileService profileService) : base(profileService, useCustomHorizon: false) {
            InterruptReason = "Sun is outside of the specified range";
        }
        private SunAltitudeCondition(SunAltitudeCondition cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SunAltitudeCondition(this) {
                Data = Data.Clone()
           };
        }

        //h = 0 degrees: Center of Sun's disk touches a mathematical horizon
        //h = -0.25 degrees: Sun's upper limb touches a mathematical horizon
        //h = -0.583 degrees: Center of Sun's disk touches the horizon; atmospheric refraction accounted for
        //h = -0.833 degrees: Sun's upper limb touches the horizon; atmospheric refraction accounted for

        public override void CalculateExpectedTime() {
            Data.Coordinates.Coordinates = CalculateSunRADec(Data.Observer);
            Data.CurrentAltitude = AstroUtil.GetSunAltitude(DateTime.Now, Data.Observer);
            CalculateExpectedTimeCommon(Data, -.833, until: true, 30, AstroUtil.GetSunAltitude);
        }
    }
}