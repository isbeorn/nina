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
using NINA.Core.Utility;
using NINA.Core.Enum;
using NINA.Sequencer.Utility;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_AboveHorizonCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_AboveHorizonCondition_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AboveHorizonCondition : LoopForAltitudeBase {

        private bool hasDsoParent;

        [ImportingConstructor]
        public AboveHorizonCondition(IProfileService profileService) : base(profileService, useCustomHorizon: true) {
            this.DateTime = new SystemDateTime();
            Data.Offset = 0;
            InterruptReason = "Target is below horizon";
        }

        private AboveHorizonCondition(AboveHorizonCondition cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        public ICustomDateTime DateTime { get; set; }

        public override object Clone() {
            return new AboveHorizonCondition(this) {
                Data = Data.Clone()
            };
        }

        private void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            CalculateExpectedTime();
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
            RunWatchdogIfInsideSequenceRoot();
        }

        public override string ToString() {
            return $"Condition: {nameof(AboveHorizonCondition)}";
        }

         public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            CalculateExpectedTime();
            var targetAltitude = Data.GetTargetAltitudeWithHorizon(DateTime.Now);
            var check = Data.CurrentAltitude >= targetAltitude;

            if (!check && IsActive()) {
                Logger.Info($"{nameof(AboveHorizonCondition)} finished. Current / Target: {Data.CurrentAltitude}° / {targetAltitude}°");
            }
            return check;
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            TopocentricCoordinates altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), time);
            return altaz.Altitude.Degree;
        }

        public override void CalculateExpectedTime() {
            CalculateExpectedTime(DateTime.Now);
        }

        public void CalculateExpectedTime(DateTime time) {
            Data.CurrentAltitude = GetCurrentAltitude(time, Data.Observer);
            CalculateExpectedTimeCommon(Data, until: false, 90, GetCurrentAltitude);
        }
    }
}
