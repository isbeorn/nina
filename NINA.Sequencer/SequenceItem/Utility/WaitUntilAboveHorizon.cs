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
using NINA.Profile.Interfaces;
using NINA.Sequencer.Validations;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Core.Utility;
using static NINA.Sequencer.Utility.ItemUtility;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitUntilAboveHorizon_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitUntilAboveHorizon_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitUntilAboveHorizon : WaitForAltitudeBase, IValidatable {

        private bool hasDsoParent;
   
        [ImportingConstructor]
        public WaitUntilAboveHorizon(IProfileService profileService) : base(profileService, useCustomHorizon: true) {
            ProfileService = profileService;
        }

        private WaitUntilAboveHorizon(WaitUntilAboveHorizon cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WaitUntilAboveHorizon(this) {
                Data = Data.Clone()
            };
        }


        [JsonProperty]
        public bool HasDsoParent {
            get => hasDsoParent;
            set {
                hasDsoParent = value;
                RaisePropertyChanged();
            }
        }

        public int UpdateInterval { get; set; } = 1;

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            do {
                Data.SetTargetAltitudeWithHorizon();

                var altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(Data.Latitude), Angle.ByDegree(Data.Longitude));
                Data.CurrentAltitude = altaz.Altitude.Degree;
                progress?.Report(new ApplicationStatus() {
                    Status = string.Format(Loc.Instance["Lbl_SequenceItem_Utility_WaitUntilAboveHorizon_Progress"], Math.Round(Data.CurrentAltitude, 2), Math.Round(Data.TargetAltitude, 2))
                });

                if (Data.CurrentAltitude > Data.GetTargetAltitudeWithHorizon(DateTime.Now)) {
                    Logger.Info("WaitUntilAboveHorizon finished: " + Data.CurrentAltitude + " > " + Data.TargetAltitude + " Offset = " + Data.Offset);
                    break;
                } else {
                    _ = await CoreUtil.Delay(TimeSpan.FromSeconds(UpdateInterval), token);
                }
            } while (true);
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            var altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), time);
            return altaz.Altitude.Degree;
        }

        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = GetCurrentAltitude(DateTime.Now, Data.Observer);
            CalculateExpectedTimeCommon(Data, until: true, 90, GetCurrentAltitude);
        }


        public override void AfterParentChanged() {
            var contextCoordinates = RetrieveContextCoordinates(this.Parent);
            if (contextCoordinates != null) {
                Data.Coordinates.Coordinates = contextCoordinates.Coordinates;
                HasDsoParent = true;
            } else {
                HasDsoParent = false;
            }
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitUntilAboveHorizon)}";
        }

        public bool Validate() {
            var issues = new List<string>();
            var maxAlt = AstroUtil.GetAltitude(0, Data.Latitude, Data.Coordinates.DecDegrees);
            var minHorizonAlt = (Data.Horizon?.GetMinAltitude() ?? 0) + Data.Offset;

            if (maxAlt < minHorizonAlt) {
                issues.Add(Loc.Instance["LblUnreachableAltitudeForHorizon"]);
                Data.ExpectedTime = "--";
            } else {
                CalculateExpectedTime();
            }

            Issues = issues;
            return issues.Count == 0;
        }
    }
}