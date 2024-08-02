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
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Enum;
using Nito.AsyncEx;
using NINA.Core.Locale;

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

        private DateTimeOffset lastCalculation = DateTimeOffset.MinValue;
        private double lastCalculationOffset = double.NaN;
        private ComparisonOperatorEnum lastCalculationComparator = ComparisonOperatorEnum.EQUALS;

        //h = 0 degrees: Center of Sun's disk touches a mathematical horizon
        //h = -0.25 degrees: Sun's upper limb touches a mathematical horizon
        //h = -0.583 degrees: Center of Sun's disk touches the horizon; atmospheric refraction accounted for
        //h = -0.833 degrees: Sun's upper limb touches the horizon; atmospheric refraction accounted for

        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = AstroUtil.GetSunAltitude(DateTime.Now, Data.Observer) + .833;
            if (!Check(null, null, true)) {
                Data.ExpectedDateTime = DateTime.Now;
                Data.ExpectedTime = Loc.Instance["LblNow"];
                lastCalculation = DateTimeOffset.MinValue;
            } else {
                var referenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
                // Only calculate every day or when the parameters have changed
                if (Data.ExpectedDateTime == DateTime.MinValue || lastCalculation < referenceDate || lastCalculationOffset != Data.Offset || lastCalculationComparator != Data.Comparator) {
                    Data.ExpectedDateTime = CalculateExpectedDateTime(referenceDate);
                    if (Data.ExpectedDateTime < DateTime.Now) {
                        Data.ExpectedDateTime = CalculateExpectedDateTime(referenceDate.AddDays(1));
                    }

                    if (Data.ExpectedDateTime == DateTime.MaxValue) {
                        Data.ExpectedTime = "--";
                    }
                    lastCalculation = referenceDate;
                    lastCalculationOffset = Data.Offset;
                    lastCalculationComparator = Data.Comparator;
                }
            }
        }

        private DateTime CalculateExpectedDateTime(DateTime time) {
            var customRiseAndSet = new SunCustomRiseAndSet(NighttimeCalculator.GetReferenceDate(time), Data.Observer.Latitude, Data.Observer.Longitude, Data.Offset - .833);
            AsyncContext.Run(customRiseAndSet.Calculate);
            return (Data.Comparator == ComparisonOperatorEnum.GREATER_THAN || Data.Comparator == ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL ? customRiseAndSet.Rise : customRiseAndSet.Set) ?? DateTime.MaxValue;
        }
    }
}