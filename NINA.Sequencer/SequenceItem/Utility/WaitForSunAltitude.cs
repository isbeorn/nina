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
using static NINA.Sequencer.Utility.ItemUtility;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForSunAltitude_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForSunAltitude_Description")]
    [ExportMetadata("Icon", "SunriseSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForSunAltitude : WaitForAltitudeBase, IValidatable {

        [ImportingConstructor]
        public WaitForSunAltitude(IProfileService profileService) : base(profileService, useCustomHorizon: false) {
        }

        private WaitForSunAltitude(WaitForSunAltitude cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WaitForSunAltitude(this) {
                Data = Data.Clone()
            };
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            do {
                 CalculateExpectedTime();

                if (MustWait()) {
                    progress.Report(new ApplicationStatus() {
                        Status = string.Format(Loc.Instance["Lbl_SequenceItem_Utility_WaitForSunAltitude_Progress"],
                            Data.CurrentAltitude,
                            AttributeHelper.GetDescription(Data.Comparator),
                            Data.TargetAltitude)
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

        // See SunAltitudeCondition for documentation on the -.833 constant
        public override void CalculateExpectedTime() {
            Data.Coordinates.Coordinates = CalculateSunRADec(Data.Observer);
            Data.CurrentAltitude = AstroUtil.GetSunAltitude(DateTime.Now, Data.Observer);
            CalculateExpectedTimeCommon(Data, -.833, until: false, 30, AstroUtil.GetSunAltitude);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForSunAltitude)}, TargetAltitude: {Data.TargetAltitude}, Comparator: {Data.Comparator}, CurrentSunAltitude: {Data.CurrentAltitude}";
        }

        public bool Validate() {
            CalculateExpectedTime();
            return true;
        }
    }
}
