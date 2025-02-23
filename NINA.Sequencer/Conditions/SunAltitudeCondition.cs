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
using NINA.Sequencer.Generators;
using System.Runtime.Serialization;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_SunAltitudeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_SunAltitudeCondition_Description")]
    [ExportMetadata("Icon", "SunriseSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]   
    
    public partial class SunAltitudeCondition : LoopForSunMoonAltitudeBase {

        [ImportingConstructor]
        public SunAltitudeCondition(IProfileService profileService) : base(profileService, useCustomHorizon: false) {
            InterruptReason = "Sun is outside of the specified range";
        }
        private SunAltitudeCondition(SunAltitudeCondition cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(SunAltitudeCondition clone) {
            clone.Data = Data.Clone();
        }

        [OnDeserialized]
        public new void OnDeserialized(StreamingContext context) {
            base.OnDeserialized(context);
            if (OffsetExpression.Definition.Length == 0) {
                OffsetExpression.Definition = Data.Offset.ToString();
            }
        }

        [IsExpression(Default = 0, Range = [-90, 90], Proxy = "Data.Offset", HasValidator = true, JsonIgnore = true)]
        private double offset;

        partial void OffsetExpressionValidator(Expression expr) {
            if (expr.Error == null) {
                Data.Offset = expr.Value;
            }
        }

        private DateTimeOffset lastCalculation = DateTimeOffset.MinValue;
        private double lastCalculationOffset = double.NaN;
        private ComparisonOperatorEnum lastCalculationComparator = ComparisonOperatorEnum.EQUALS;

        //h = 0 degrees: Center of Sun's disk touches a mathematical horizon
        //h = -0.25 degrees: Sun's upper limb touches a mathematical horizon
        //h = -0.583 degrees: Center of Sun's disk touches the horizon; atmospheric refraction accounted for
        //h = -0.833 degrees: Sun's upper limb touches the horizon; atmospheric refraction accounted for
        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = AstroUtil.CalculateAltitudeForStandardRefraction(AstroUtil.GetSunAltitude(DateTime.Now, Data.Observer) + AstroUtil.ArcminToDegree(0.25), Data.Observer.Latitude, Data.Observer.Longitude);

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
            // The SunRiseAndSet already models refraction and sun disk size
            var customRiseAndSet = new SunCustomRiseAndSet(NighttimeCalculator.GetReferenceDate(time), Data.Observer.Latitude, Data.Observer.Longitude, Data.Offset);
            AsyncContext.Run(customRiseAndSet.Calculate);
            return (Data.Comparator == ComparisonOperatorEnum.GREATER_THAN || Data.Comparator == ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL ? customRiseAndSet.Rise : customRiseAndSet.Set) ?? DateTime.MaxValue;
        }
    }
}