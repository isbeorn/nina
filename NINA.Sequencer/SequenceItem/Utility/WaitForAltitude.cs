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
using NINA.Core.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using static NINA.Sequencer.Utility.ItemUtility;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForAltitude_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForAltitude_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForAltitude : WaitForAltitudeBase, IValidatable {
        private bool hasDsoParent;
        private string aboveOrBelow;
 
        [ImportingConstructor]
        public WaitForAltitude(IProfileService profileService) : base(profileService, useCustomHorizon: false) {
            AboveOrBelow = ">";
            Data.Offset = 30;
        }

        private WaitForAltitude(WaitForAltitude cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }
        
        public override object Clone() {
            return new WaitForAltitude(this) {
                AboveOrBelow = AboveOrBelow,
                Data = Data.Clone()
            };
        }
        
        [JsonProperty]
        public string AboveOrBelow {
            get => aboveOrBelow;
            set {
                // For backward compatibility
                if (value == ">=") value = ">";
                else if (value == "<=") {
                    value = "<";
                }

                aboveOrBelow = value;
                if (aboveOrBelow == ">") Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;
                else Data.Comparator = ComparisonOperatorEnum.LESS_THAN_OR_EQUAL;
                CalculateExpectedTime();
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            do {
                var coordinates = Data.Coordinates.Coordinates;
                var altaz = coordinates.Transform(Angle.ByDegree(Data.Latitude), Angle.ByDegree(Data.Longitude), Data.Elevation);
                progress?.Report(new ApplicationStatus() {
                    Status = string.Format(Loc.Instance["Lbl_SequenceItem_Utility_WaitForAltitude_Progress"], Math.Round(altaz.Altitude.Degree, 2), Data.TargetAltitude)
                });

                if (aboveOrBelow == ">" && altaz.Altitude.Degree >= Data.Offset) {
                    break;
                } else if (aboveOrBelow == "<" && altaz.Altitude.Degree <= Data.Offset) {
                    break;
                }

                await NINA.Core.Utility.CoreUtil.Delay(TimeSpan.FromSeconds(1), token);
            } while (true);
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            var altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), observer.Elevation, time);
            return altaz.Altitude.Degree;
        }

        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = GetCurrentAltitude(DateTime.Now, Data.Observer);
            CalculateExpectedTimeCommon(Data, until: true, 30, GetCurrentAltitude);
        }


        [JsonProperty]
        public bool HasDsoParent {
            get => hasDsoParent;
            set {
                hasDsoParent = value;
                RaisePropertyChanged();
            }
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
            return $"Category: {Category}, Item: {nameof(WaitForAltitude)}, Altitude: {AboveOrBelow}{Data.TargetAltitude}";
        }

        public bool Validate() {
            List<string> issues = new List<string>();

            double maxAlt = AstroUtil.GetAltitude(0, Data.Latitude, Data.Coordinates.DecDegrees);
            double minAlt = AstroUtil.GetAltitude(180, Data.Latitude, Data.Coordinates.DecDegrees);

            if (aboveOrBelow == ">") {
                if (maxAlt < Data.TargetAltitude) {
                    issues.Add(Loc.Instance["LblUnreachableAltitude"]);
                }
            } else {
                if (minAlt > Data.TargetAltitude) {
                    issues.Add(Loc.Instance["LblUnreachableAltitude"]);
                }
            }

            if (issues.Count == 0) {
                CalculateExpectedTime();
            }

            Issues = issues;
            return issues.Count == 0;
        }
    }
}