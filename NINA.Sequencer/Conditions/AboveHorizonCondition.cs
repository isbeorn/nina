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
using NINA.Sequencer.Container;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Runtime.Serialization;
using static NINA.Sequencer.Utility.ItemUtility;

namespace NINA.Sequencer.Conditions {

    [ExportMetadata("Name", "Lbl_SequenceCondition_AboveHorizonCondition_Name")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_AboveHorizonCondition_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions(GenerateValidation = true)]
    public partial class AboveHorizonCondition : LoopForAltitudeBase, IValidatable, ISequenceCustomPropertyEditProvider, ISequenceAttachmentStateProvider {
        private double lastRA;
        private double lastDec;
        private Coordinates inheritedCoordinateSource;
        private bool hasDsoParent;
        private bool hydratingCoordinates;

        private bool UsesStandaloneCoordinates => !HasDsoParent;

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

        partial void AfterClone(AboveHorizonCondition clone) {
            clone.Data = Data.Clone();
            clone.Data.Coordinates = Data.Coordinates?.Clone();
        }

        [OnDeserializing]
        private void BeginCoordinateDeserialization(StreamingContext context) {
            hydratingCoordinates = true;
        }

        [OnDeserialized]
        public new void OnDeserialized(StreamingContext context) {
            double legacyRa = Data.Coordinates.Coordinates.RA;
            double legacyDec = Data.Coordinates.Coordinates.Dec;
            bool previous = Protect;
            Protect = true;
            try {
                if (!HasDsoParent && RaExpression.Definition.Length == 0 && legacyRa != 0) {
                    RaExpression.Definition = legacyRa.ToString(CultureInfo.InvariantCulture);
                }
                if (!HasDsoParent && DecExpression.Definition.Length == 0 && legacyDec != 0) {
                    DecExpression.Definition = legacyDec.ToString(CultureInfo.InvariantCulture);
                }
                if (OffsetExpression.Definition.Length == 0 && Data.Offset != OffsetExpression.Default) {
                    OffsetExpression.Definition = Data.Offset.ToString(CultureInfo.InvariantCulture);
                }
            } finally {
                hydratingCoordinates = false;
                Protect = previous;
            }

            ResolveEffectiveCoordinates();
            base.OnDeserialized(context);
        }

        [IsExpression(Default = 0, Range = [-90, 90], Proxy = "Data.Offset")]
        public partial double Offset { get; set; }

        private void Coordinates_CoordinatesChanged(object sender, EventArgs e) {
            // When coordinates change, we change the decimal value
            InputCoordinates ic = (InputCoordinates)sender;
            Coordinates c = ic.Coordinates;

            if (Protect || HasDsoParent || hydratingCoordinates || c == null) {
                return;
            }

            double ra = c.RA;
            double dec = c.Dec;
            bool previous = Protect;
            Protect = true;
            try {
                if (ra != lastRA) {
                    RaExpression.Definition = Math.Round(ra, 7).ToString(CultureInfo.InvariantCulture);
                }
                if (dec != lastDec) {
                    DecExpression.Definition = Math.Round(dec, 7).ToString(CultureInfo.InvariantCulture);
                }

                lastRA = ra;
                lastDec = dec;
            } finally {
                Protect = previous;
            }
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
            bool wasInherited = HasDsoParent;
            var context = RetrieveContextCoordinates(this.Parent);
            bool inheritedCoordinatesChanged = false;
            bool previous = Protect;
            Protect = true;
            try {
                HasDsoParent = context != null;
                if (context != null) {
                    inheritedCoordinatesChanged = ApplyInheritedCoordinates(context);
                } else if (wasInherited) {
                    RaExpression.Definition = Data.Coordinates.Coordinates.RA.ToString(CultureInfo.InvariantCulture);
                    DecExpression.Definition = Data.Coordinates.Coordinates.Dec.ToString(CultureInfo.InvariantCulture);
                }
            } finally {
                Protect = previous;
            }

            if (Data.Coordinates != null) {
                // Use the aggregate event because one coordinate change raises several property notifications.
                Data.Coordinates.CoordinatesChanged -= Coordinates_CoordinatesChanged;
                lastRA = Data.Coordinates.Coordinates.RA;
                lastDec = Data.Coordinates.Coordinates.Dec;
                Data.Coordinates.CoordinatesChanged += Coordinates_CoordinatesChanged;
            }
            Validate();
            if (inheritedCoordinatesChanged) {
                CalculateExpectedTime();
            }
            RunWatchdogIfInsideSequenceRoot();
        }

        public override string ToString() {
            return $"Condition: {nameof(AboveHorizonCondition)}, Offset = {OffsetExpression.ValueString}";
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            CalculateExpectedTime();
            var targetAltitude = Data.TargetAltitude;
            var check = Data.CurrentAltitude >= targetAltitude;

            if (!check && IsActive()) {
                Logger.Info($"{nameof(AboveHorizonCondition)} finished. Current / Target: {Data.CurrentAltitude}° / {targetAltitude}°");
            }
            return check;
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            TopocentricCoordinates altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), observer.Elevation, time);
            return altaz.Altitude.Degree;
        }

        public override void CalculateExpectedTime() {
            CalculateExpectedTime(DateTime.Now);
        }

        public void CalculateExpectedTime(DateTime time) {
            ResolveEffectiveCoordinates();
            _ = Offset; // Refresh the target consumed by the shared altitude calculator.
            Data.CalculateTargetExpectedTime(time, TargetCrossingComparison.BelowStrict);
        }

        protected bool Protect = false;

        private bool ApplyInheritedCoordinates(ContextCoordinates context) {
            var source = context.Coordinates;
            var current = Data.Coordinates.Coordinates;
            bool changed = !ReferenceEquals(source, inheritedCoordinateSource)
                || source.RA != current.RA || source.Dec != current.Dec || source.Epoch != current.Epoch;
            bool previous = Protect;
            Protect = true;
            try {
                if (changed || ReferenceEquals(current, source)) {
                    Data.Coordinates.Coordinates = source.Clone();
                }
                PositionAngle = context.PositionAngle;
                lastRA = source.RA;
                lastDec = source.Dec;
                inheritedCoordinateSource = source;
            } finally {
                Protect = previous;
            }

            return changed;
        }

        partial void PrepareExpressionValidation() {
            ResolveEffectiveCoordinates();
        }

        private void ResolveEffectiveCoordinates() {
            if (HasDsoParent || Protect || hydratingCoordinates) {
                return;
            }

            if (RaExpression.Definition.Length > 0) {
                _ = Ra;
                ApplyCoordinateExpression(RaExpression, true);
            }
            if (DecExpression.Definition.Length > 0) {
                _ = Dec;
                ApplyCoordinateExpression(DecExpression, false);
            }
        }

        private void ApplyCoordinateExpression(Expression expression, bool rightAscension) {
            if (HasDsoParent || Protect || hydratingCoordinates) {
                return;
            }

            CoordinateExpressionSync.Apply(Data.Coordinates, expression, rightAscension, ref Protect, ref lastRA, ref lastDec);
        }

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

        [IsExpression(Default = 0, Range = [0, 24], HasValidator = true, ValidateWhen = nameof(UsesStandaloneCoordinates))]
        public partial double Ra { get; set; }

        partial void RaExpressionValidator(Expression expr) {
            ApplyCoordinateExpression(expr, true);
        }

        [IsExpression(Default = 0, Range = [-90, 90], HasValidator = true, ValidateWhen = nameof(UsesStandaloneCoordinates))]
        public partial double Dec { get; set; }

        partial void DecExpressionValidator(Expression expr) {
            ApplyCoordinateExpression(expr, false);
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
