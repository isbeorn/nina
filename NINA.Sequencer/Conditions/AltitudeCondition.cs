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
using NINA.Astrometry;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using static NINA.Sequencer.Utility.ItemUtility;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_AltitudeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_AltitudeCondition_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]
    public partial class AltitudeCondition : LoopForAltitudeBase {
        private double lastRA;
        private double lastDec;
        private bool hasDsoParent;

        [ImportingConstructor]
        public AltitudeCondition(IProfileService profileService) : base(profileService, useCustomHorizon: false) {
            Data.Offset = 30;
            Data.Comparator = Core.Enum.ComparisonOperatorEnum.LESS_THAN;
        }
        private AltitudeCondition(AltitudeCondition cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(AltitudeCondition clone) {
            clone.Data = Data.Clone();
            clone.Data.Coordinates = Data.Coordinates?.Clone();
        }

        [OnDeserialized]
        public new void OnDeserialized(StreamingContext context) {
            base.OnDeserialized(context);
            // Fix up Ra and Dec Expressions (auto-update to existing sequences)
            if (Data != null) {
                Coordinates c = Data.Coordinates.Coordinates;
                if (c.RA != 0 || c.Dec != 0) {
                    // Fix up decimals
                    RaExpression.Definition = Math.Round(c.RA, 7).ToString();
                    DecExpression.Definition = Math.Round(c.Dec, 7).ToString();
                    OffsetExpression.Definition = Data.Offset.ToString();
                }
            }
            if (OffsetExpression.Definition.Length == 0 && Data.Offset != OffsetExpression.Default) {
                OffsetExpression.Definition = Data.Offset.ToString();
            }
        }

        [IsExpression(Default = 30, Range = [-90, 90], Proxy = "Data.Offset", HasValidator = true)]
        private double offset;

        partial void OffsetExpressionValidator(Expression expr) {
            if (expr.Error == null) {
                if (Data != null) {
                    Data.Offset = expr.Value;
                }
            }
        }
        private void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            // When coordinates change, we change the decimal value
            InputCoordinates ic = (InputCoordinates)sender;
            Coordinates c = ic.Coordinates;

            if (Protect) return;

            if (c.RA != lastRA) {
                RaExpression.Definition = Math.Round(c.RA, 7).ToString();
            } else if (c.Dec != lastDec) {
                DecExpression.Definition = Math.Round(c.Dec, 7).ToString();
            }

            lastRA = c.RA;
            lastDec = c.Dec;
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
            var coordinates = RetrieveContextCoordinates(this.Parent);
            if (coordinates != null) {
                Data.Coordinates.Coordinates = coordinates.Coordinates;
                PositionAngle = coordinates.PositionAngle;
                HasDsoParent = true;
            } else {
                HasDsoParent = false;
            }

            if (Data.Coordinates != null) {
                Data.Coordinates.PropertyChanged += Coordinates_PropertyChanged;
            }
            RaExpression.Context = this;
            DecExpression.Context = this;
            PositionAngleExpression.Context = this;
            OffsetExpression.Context = this;
            Validate();
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

        protected bool Protect = false;

        [JsonProperty(propertyName: "Coordinates")]
        private InputCoordinates DeprecatedCoordinates {
            set => coordinates = value;
        }

        private InputCoordinates coordinates = new InputCoordinates();

        [IsExpression(Default = 0, Range = [0, 24], HasValidator = true)]
        private double ra = 0;

        partial void RaExpressionValidator(Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputCoordinates ic = new InputCoordinates();
            Protect = true;
            ic.Coordinates.RA = RaExpression.Value;
            Data.Coordinates.RAHours = ic.RAHours;
            Data.Coordinates.RAMinutes = ic.RAMinutes;
            Data.Coordinates.RASeconds = ic.RASeconds;
            Protect = false;
        }

        [IsExpression(Default = 0, Range = [-90, 90], HasValidator = true)]
        private double dec = 0;

        partial void DecExpressionValidator(Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputCoordinates ic = new InputCoordinates();
            Protect = true;
            ic.Coordinates.Dec = DecExpression.Value;
            Data.Coordinates.DecDegrees = ic.DecDegrees;
            Data.Coordinates.DecMinutes = ic.DecMinutes;
            Data.Coordinates.DecSeconds = ic.DecSeconds;
            Protect = false;
        }

        [JsonProperty]
        private bool usesRotation = false;
        public bool UsesRotation {
            get { return usesRotation; }
            set { usesRotation = value; }
        }

        [IsExpression(Default = 0, Range = [0, 360], HasValidator = true)]
        private double positionAngle = 0;

        partial void PositionAngleExpressionValidator(Expression expr) {
            if (expr.Error == null) {
                expr.Value = AstroUtil.EuclidianModulus(expr.Value, 360);
            }
        }

        public bool Validate() {
            Expression.ValidateExpressions(Issues, RaExpression, DecExpression, PositionAngleExpression, OffsetExpression);
            RaisePropertyChanged(nameof(Issues));
            return Issues.Count == 0;
        }
    }
}
