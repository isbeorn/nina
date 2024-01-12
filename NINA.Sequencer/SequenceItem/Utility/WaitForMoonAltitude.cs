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
            CalculateExpectedTime();

            switch (Data.Comparator) {
                case ComparisonOperatorEnum.GREATER_THAN:
                    return Data.CurrentAltitude > Data.Offset;
                default:
                    return Data.CurrentAltitude <= Data.Offset;
            }
        }

        // See MoonAltitudeCondition for explanation of the constant
        public override void CalculateExpectedTime() {
            Data.Coordinates.Coordinates = CalculateMoonRADec(Data.Observer);
            Data.CurrentAltitude = AstroUtil.GetMoonAltitude(DateTime.Now, Data.Observer);
            CalculateExpectedTimeCommon(Data, -.583, until: false, 60, AstroUtil.GetMoonAltitude);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForMoonAltitude)}, TargetAltitude: {Data.TargetAltitude}, Comparator: {Data.Comparator}, CurrentAltitude: {Data.CurrentAltitude}";
        }

        public bool Validate() {
            CalculateExpectedTime();
            return true;
        }
    }
}