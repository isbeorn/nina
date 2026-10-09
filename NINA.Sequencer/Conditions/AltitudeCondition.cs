#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Sequencer.Editing;
using NINA.Astrometry;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Runtime.Serialization;
using static NINA.Sequencer.Utility.ItemUtility;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_AltitudeCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_AltitudeCondition_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions(GenerateValidation = true)]
    public partial class AltitudeCondition : LoopForAltitudeBase, IValidatable, ISequenceCustomPropertyEditProvider, ISequenceAttachmentStateProvider {
        private double lastRA;
        private double lastDec;
        private Epoch lastEpoch;
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
                    RaExpression.Definition = Math.Round(c.RA, 7).ToString(CultureInfo.InvariantCulture);
                    DecExpression.Definition = Math.Round(c.Dec, 7).ToString(CultureInfo.InvariantCulture);
                    OffsetExpression.Definition = Data.Offset.ToString(CultureInfo.InvariantCulture);
                }
            }
            if (OffsetExpression.Definition.Length == 0 && Data.Offset != OffsetExpression.Default) {
                OffsetExpression.Definition = Data.Offset.ToString(CultureInfo.InvariantCulture);
            }
        }

        [IsExpression(Default = 30, Range = [-90, 90], Proxy = "Data.Offset")]
        public partial double Offset { get; set; }

        private void Coordinates_CoordinatesChanged(object sender, EventArgs e) {
            // When coordinates change, we change the decimal value
            InputCoordinates ic = (InputCoordinates)sender;
            Coordinates c = ic.Coordinates;

            if (Protect) return;

            if (c.RA != lastRA) {
                RaExpression.Definition = Math.Round(c.RA, 7).ToString(CultureInfo.InvariantCulture);
            }
            if (c.Dec != lastDec) {
                DecExpression.Definition = Math.Round(c.Dec, 7).ToString(CultureInfo.InvariantCulture);
            }

            lastRA = c.RA;
            lastDec = c.Dec;
            lastEpoch = c.Epoch;
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
                var inheritedCoordinates = coordinates.Coordinates;
                // Ancestor attachment repeats during loading; only changed coordinate state needs a new prediction.
                if (!ReferenceEquals(Data.Coordinates.Coordinates, inheritedCoordinates)
                    || inheritedCoordinates.RA != lastRA || inheritedCoordinates.Dec != lastDec || inheritedCoordinates.Epoch != lastEpoch) {
                    Data.Coordinates.Coordinates = inheritedCoordinates;
                }
                PositionAngle = coordinates.PositionAngle;
                HasDsoParent = true;
            } else {
                HasDsoParent = false;
            }

            if (Data.Coordinates != null) {
                // Use the aggregate event because one coordinate change raises several property notifications.
                Data.Coordinates.CoordinatesChanged -= Coordinates_CoordinatesChanged;
                lastRA = Data.Coordinates.Coordinates.RA;
                lastDec = Data.Coordinates.Coordinates.Dec;
                lastEpoch = Data.Coordinates.Coordinates.Epoch;
                Data.Coordinates.CoordinatesChanged += Coordinates_CoordinatesChanged;
            }
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
            return Data.IsRising || Data.CurrentAltitude >= Data.TargetAltitude;
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            var altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), observer.Elevation, time);
            return altaz.Altitude.Degree;
        }

        public override void CalculateExpectedTime() {
            _ = Offset; // Refresh the target consumed by the shared altitude calculator.
            Data.CalculateTargetExpectedTime(DateTime.Now, TargetCrossingComparison.SettingBelow);
        }

        protected bool Protect = false;

        private void RestoreEditorCoordinates(SequenceCoordinateState state) {
            if (state.Inherited) return;
            SequenceCoordinateState.RestoreCoordinates(Data.Coordinates, RaExpression, DecExpression, state.Value.ToCoordinates(),
                state.NegativeDec, state.RaDefinition, state.DecDefinition, ref Protect, ref lastRA, ref lastDec);
            PositionAngleExpression.Definition = state.RotationDefinition;
        }

        private ISequenceEditSnapshot CaptureEditorCoordinates() =>
            new SequenceEditSnapshot<SequenceCoordinateState>(
                () => SequenceCoordinateState.Capture(Data.Coordinates, RaExpression, DecExpression, PositionAngleExpression, HasDsoParent),
                RestoreEditorCoordinates, SequenceCoordinateState.Equal, SequenceEditDetails.Coordinates);

        bool ISequenceCustomPropertyEditProvider.TryCapturePropertyState(object source, string propertyName, out ISequenceEditSnapshot snapshot) {
            snapshot = ReferenceEquals(source, Data.Coordinates) ? CaptureEditorCoordinates() : null;
            return snapshot != null;
        }

        ISequenceEditSnapshot ISequenceAttachmentStateProvider.CaptureAttachmentState() =>
            Data.Coordinates?.Coordinates == null ? null : CaptureEditorCoordinates();

        [JsonProperty(propertyName: "Coordinates")]
        private InputCoordinates DeprecatedCoordinates {
            set => coordinates = value;
        }

        private InputCoordinates coordinates = new InputCoordinates();

        [IsExpression(Default = 0, Range = [0, 24], HasValidator = true)]
        public partial double Ra { get; set; }

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
        public partial double Dec { get; set; }

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
        public partial double PositionAngle { get; set; }

        partial void PositionAngleExpressionValidator(Expression expr) {
            if (expr.Error == null) {
                expr.Value = AstroUtil.EuclidianModulus(expr.Value, 360);
            }
        }

    }
}
