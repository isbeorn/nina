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
using NINA.Core.Enum;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Locale;
using Nito.AsyncEx;

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

        private DateTimeOffset lastCalculation = DateTimeOffset.MinValue;
        private double lastCalculationOffset = double.NaN;
        private ComparisonOperatorEnum lastCalculationComparator = ComparisonOperatorEnum.EQUALS;

        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = AstroUtil.GetMoonAltitude(DateTime.Now, Data.Observer);
            if (!Check(null, null, true)) {
                Data.ExpectedDateTime = DateTime.Now;
                Data.ExpectedTime = Loc.Instance["LblNow"];
                lastCalculation = DateTimeOffset.MinValue;
            } else {
                var referenceDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
                // Only calculate every day or when the parameters have changed
                if (Data.ExpectedDateTime == DateTime.MinValue || lastCalculation < referenceDate || lastCalculationOffset != GetDataOffset() || lastCalculationComparator != Data.Comparator) {
                    Data.ExpectedDateTime = CalculateExpectedDateTime(referenceDate);
                    if (Data.ExpectedDateTime < DateTime.Now) {
                        Data.ExpectedDateTime = CalculateExpectedDateTime(referenceDate.AddDays(1));
                    }

                    if (Data.ExpectedDateTime == DateTime.MaxValue) {
                        Data.ExpectedTime = "--";
                    }
                    lastCalculation = referenceDate;
                    lastCalculationOffset = GetDataOffset();
                    lastCalculationComparator = Data.Comparator;
                }
            }
        }

        protected override double GetDataOffset() {
            // Moonrise/Moonset calculations are a special case where we adjust for the upper limp of the Moon touching the horizon including atmospheric refraction.
            return Data.Offset != 0 ? Data.Offset : -AstroUtil.MoonUpperLimbApparentHorizonAltitude;
        }

        private DateTime CalculateExpectedDateTime(DateTime time) {
            // The MoonRiseAndSet already models refraction and moon disk size
            var customRiseAndSet = new MoonCustomRiseAndSet(NighttimeCalculator.GetReferenceDate(time), Data.Observer.Latitude, Data.Observer.Longitude, Data.Observer.Elevation, GetDataOffset());
            AsyncContext.Run(customRiseAndSet.Calculate);
            return (Data.Comparator == ComparisonOperatorEnum.GREATER_THAN || Data.Comparator == ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL ? customRiseAndSet.Rise : customRiseAndSet.Set) ?? DateTime.MaxValue;
        }
    }
}