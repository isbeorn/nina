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
using NINA.Sequencer.SequenceItem;
using NINA.Astrometry;
using System;
using System.ComponentModel.Composition;
using NINA.Sequencer.SequenceItem.Utility;
using static NINA.Sequencer.Utility.ItemUtility;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_AltitudeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_AltitudeCondition_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AltitudeCondition : LoopForAltitudeBase {
        
        private bool hasDsoParent;

        [ImportingConstructor]
        public AltitudeCondition(IProfileService profileService) : base(profileService, useCustomHorizon: false) {
            Data.Offset = 30;
            Data.Comparator = Core.Enum.ComparisonOperatorEnum.LESS_THAN;
        }
        private AltitudeCondition(AltitudeCondition cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new AltitudeCondition(this) {
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

        private void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            CalculateExpectedTime();
        }

        public override void AfterParentChanged() {
            var coordinates = RetrieveContextCoordinates(this.Parent)?.Coordinates;
            if (coordinates != null) {
                Data.Coordinates.Coordinates = coordinates;
                HasDsoParent = true;
            } else {
                HasDsoParent = false;
            }
            RunWatchdogIfInsideSequenceRoot();
        }
        public override string ToString() {
            return $"Condition: {nameof(AltitudeCondition)}, Altitude >= {Data.TargetAltitude}";
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (HasDsoParent) {
                var coordinates = RetrieveContextCoordinates(this.Parent)?.Coordinates;
                if (coordinates != null) {
                    Data.Coordinates.Coordinates = coordinates;
                }
            }

            CalculateExpectedTime();
            return Data.IsRising || Data.CurrentAltitude >= Data.Offset;
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            var altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), observer.Elevation, time);
            return altaz.Altitude.Degree;
        }

        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = GetCurrentAltitude(DateTime.Now, Data.Observer);
            CalculateExpectedTimeCommon(Data, until: true, 30, GetCurrentAltitude);
        }
    }
}
