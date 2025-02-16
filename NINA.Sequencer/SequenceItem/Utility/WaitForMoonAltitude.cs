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
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using System.Runtime.Serialization;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForMoonAltitude_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForMoonAltitude_Description")]
    [ExportMetadata("Icon", "WaningGibbousMoonSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]
    public partial class WaitForMoonAltitude : WaitForAltitudeBase, IValidatable {

        private ISymbolBrokerVM symbolBroker;

        [ImportingConstructor]
        public WaitForMoonAltitude(IProfileService profileService, ISymbolBrokerVM symbolBroker) : base(profileService, useCustomHorizon: false) {
            Name = Name;
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (OffsetExpression.Definition.Length == 0) {
                OffsetExpression.Definition = Data.Offset.ToString();
            }
        }

        private WaitForMoonAltitude(WaitForMoonAltitude cloneMe) : this(cloneMe.ProfileService, cloneMe.symbolBroker) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(WaitForMoonAltitude clone) {
            clone.Data = Data.Clone();
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
                    return Data.CurrentAltitude > Data.Offset;
                default:
                    return Data.CurrentAltitude <= Data.Offset;
            }
        }

        [IsExpression("symbolBroker", Default = 30, Range = [-90, 90], Proxy = "Data.Offset", HasValidator = true, JsonIgnore = true)]
        private double offset;

        partial void OffsetExpressionValidator(Expression expr) {
            if (expr.Error == null) {
                Data.Offset = expr.Value;
            }
        }

        private DateTimeOffset lastCalculation = DateTimeOffset.MinValue;
        private double lastCalculationOffset = double.NaN;
        private ComparisonOperatorEnum lastCalculationComparator = ComparisonOperatorEnum.EQUALS;

        // See MoonAltitudeCondition for explanation of the constant
        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = AstroUtil.GetMoonAltitude(DateTime.Now, Data.Observer) + .583;

            if (!MustWait()) {
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
            // The MoonRiseAndSet already models refraction and moon disk size
            var customRiseAndSet = new MoonCustomRiseAndSet(NighttimeCalculator.GetReferenceDate(time), Data.Observer.Latitude, Data.Observer.Longitude, Data.Offset);
            AsyncContext.Run(customRiseAndSet.Calculate);
            return (Data.Comparator == ComparisonOperatorEnum.GREATER_THAN ? customRiseAndSet.Set : customRiseAndSet?.Rise) ?? DateTime.MaxValue;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForMoonAltitude)}, TargetAltitude: {Data.TargetAltitude}, Comparator: {Data.Comparator}, CurrentAltitude: {Data.CurrentAltitude}";
        }

        public bool Validate() {
            CalculateExpectedTime();
            Expression.ValidateExpressions(Issues, OffsetExpression);
            return Issues.Count == 0;
        }
    }
}