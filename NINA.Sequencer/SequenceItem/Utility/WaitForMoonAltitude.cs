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
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Sequencer.Validations;
using NINA.Astrometry.RiseAndSet;
using Nito.AsyncEx;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForMoonAltitude_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForMoonAltitude_Description")]
    [ExportMetadata("Icon", "WaningGibbousMoonSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForMoonAltitude : WaitForAltitudeBase, IValidatable {

        [ImportingConstructor]
        public WaitForMoonAltitude(IProfileService profileService) : base(profileService, useCustomHorizon: false) {
            Data.Offset = 0d;
            Name = Name;
        }

        private WaitForMoonAltitude(WaitForMoonAltitude cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WaitForMoonAltitude(this) {
                Data = Data.Clone()
            };
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            do {
                CalculateExpectedTime();

                if (MustWait()) {
                    progress.Report(new ApplicationStatus() {
                        Status = string.Format(Loc.Instance["Lbl_SequenceItem_Utility_WaitForMoonAltitude_Progress"],
                        Math.Round(Data.CurrentAltitude, 2),
                        AttributeHelper.GetDescription(Data.Comparator),
                        Math.Round(Data.TargetAltitude, 2))
                    });

                    await CoreUtil.Delay(TimeSpan.FromSeconds(1), token);
                } else {
                    break;
                }
            } while (true);
        }

        private bool MustWait() {
            switch (Data.Comparator) {
                case ComparisonOperatorEnum.GREATER_THAN:
                    return Data.CurrentAltitude > GetDataOffset();
                default:
                    return Data.CurrentAltitude <= GetDataOffset();
            }
        }

        private DateTimeOffset lastCalculation = DateTimeOffset.MinValue;
        private double lastCalculationOffset = double.NaN;
        private ComparisonOperatorEnum lastCalculationComparator = ComparisonOperatorEnum.EQUALS;

        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = AstroUtil.GetMoonAltitude(DateTime.Now, Data.Observer);

            if (!MustWait()) {
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

        private DateTime CalculateExpectedDateTime(DateTime time) {
            var customRiseAndSet = new MoonCustomRiseAndSet(NighttimeCalculator.GetReferenceDate(time), Data.Observer.Latitude, Data.Observer.Longitude, Data.Observer.Elevation, GetDataOffset());
            AsyncContext.Run(customRiseAndSet.Calculate);
            return (Data.Comparator == ComparisonOperatorEnum.GREATER_THAN ? customRiseAndSet.Set : customRiseAndSet?.Rise) ?? DateTime.MaxValue;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForMoonAltitude)}, TargetAltitude: {Data.TargetAltitude}, Comparator: {Data.Comparator}, CurrentAltitude: {Data.CurrentAltitude}";
        }

        public bool Validate() {
            CalculateExpectedTime();
            return true;
        }

        private double GetDataOffset() {
            // Moonrise/Moonset calculations are a special case where we adjust for the upper limp of the Moon touching the horizon including atmospheric refraction.
            return Data.Offset != 0 ? Data.Offset : -0.583;
        }
    }
}