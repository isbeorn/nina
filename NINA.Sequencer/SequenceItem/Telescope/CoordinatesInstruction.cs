using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using NINA.Sequencer.Editing;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Container;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions(GenerateValidation = true)]

    public partial class CoordinatesInstruction : SequenceItem, IValidatable, ISequenceCustomPropertyEditProvider, ISequenceAttachmentStateProvider {

        public CoordinatesInstruction(ISequenceEntity e) : this() {
        }

        public CoordinatesInstruction() {
            Coordinates = new InputCoordinates();
        }

        partial void AfterClone(CoordinatesInstruction clone) {
            clone.Coordinates = Coordinates?.Clone();
        }

        protected bool Protect = false;
        private bool hydratingCoordinates;

        protected virtual bool UsesDataCoordinates => false;

        protected void BindEffectiveCoordinates() {
            if (UsesDataCoordinates && Data != null) {
                Data.Coordinates = Coordinates;
            }
        }

        [OnDeserializing]
        private void BeginCoordinateDeserialization(StreamingContext context) {
            hydratingCoordinates = true;
        }

        private bool UsesStandaloneCoordinates => !Inherited;

        protected void ResolveEffectiveCoordinates() {
            BindEffectiveCoordinates();
            if (Inherited || Protect || hydratingCoordinates) {
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

        partial void PrepareExpressionValidation() {
            ResolveEffectiveCoordinates();
        }

        private void ApplyCoordinateExpression(Expression expression, bool rightAscension) {
            if (Inherited || Protect || hydratingCoordinates) {
                return;
            }

            CoordinateExpressionSync.Apply(Coordinates, expression, rightAscension, ref Protect, ref lastRA, ref lastDec);
        }

        // Restore coordinate configuration together: the normal component notifications rewrite
        // expression definitions. Expressions still evaluate against the current runtime values.
        private void RestoreEditorCoordinates(SequenceCoordinateState state) {
            if (state.Inherited) return;
            SequenceCoordinateState.RestoreCoordinates(Coordinates, RaExpression, DecExpression, state.Value.ToCoordinates(),
                state.NegativeDec, state.RaDefinition, state.DecDefinition, ref Protect, ref lastRA, ref lastDec);
            PositionAngleExpression.Definition = state.RotationDefinition;
            Inherited = false;
        }

        private ISequenceEditSnapshot CaptureEditorCoordinates() =>
            new SequenceEditSnapshot<SequenceCoordinateState>(
                () => SequenceCoordinateState.Capture(Coordinates, RaExpression, DecExpression, PositionAngleExpression, Inherited),
                RestoreEditorCoordinates, SequenceCoordinateState.Equal, SequenceEditDetails.Coordinates);

        bool ISequenceCustomPropertyEditProvider.TryCapturePropertyState(object source, string propertyName, out ISequenceEditSnapshot snapshot) {
            snapshot = ReferenceEquals(source, Coordinates) ? CaptureEditorCoordinates() : null;
            return snapshot != null;
        }

        ISequenceEditSnapshot ISequenceAttachmentStateProvider.CaptureAttachmentState() =>
            Coordinates?.Coordinates == null ? null : CaptureEditorCoordinates();

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            var legacy = UsesDataCoordinates ? Data?.Coordinates ?? Coordinates : Coordinates;
            bool negativeZero = legacy?.NegativeDec ?? false;
            var source = legacy?.Clone() ?? new InputCoordinates();
            if (source.Coordinates.Dec == 0) {
                source.NegativeDec = negativeZero;
            }
            double legacyRa = source.Coordinates.RA;
            double legacyDec = source.Coordinates.Dec;
            bool previous = Protect;
            Protect = true;
            try {
                Coordinates = source;
                if (!Inherited && DecExpression.Definition.Length == 0 && legacyDec != 0) {
                    DecExpression.Definition = legacyDec.ToString(CultureInfo.InvariantCulture);
                }
                if (!Inherited && RaExpression.Definition.Length == 0 && legacyRa != 0) {
                    RaExpression.Definition = legacyRa.ToString(CultureInfo.InvariantCulture);
                }
            } finally {
                hydratingCoordinates = false;
                Protect = previous;
            }

            ResolveEffectiveCoordinates();
        }

        private InputCoordinates coordinates;

        /// <summary>
        /// This field is used for backwards compatibility when deserializing older sequences or saving it in >=3.3 and going backwards
        /// </summary>
        [JsonProperty]        
        public InputCoordinates Coordinates {
            get {
                return coordinates;
            }
            set {
                if (ReferenceEquals(coordinates, value)) {
                    return;
                }

                if (coordinates != null) {
                    coordinates.PropertyChanged -= Coordinates_PropertyChanged;
                }

                coordinates = value;
                if (coordinates != null) {
                    lastRA = coordinates.Coordinates?.RA ?? 0;
                    lastDec = coordinates.Coordinates?.Dec ?? 0;
                    coordinates.PropertyChanged += Coordinates_PropertyChanged;
                }

                BindEffectiveCoordinates();
                RaisePropertyChanged();
            }
        }

        public void UpdateExpressions(CoordinatesInstruction clone, CoordinatesInstruction original) {
            clone.RaExpression = new Expression(original.RaExpression, clone.Parent, clone.RaExpressionValidator);
            clone.DecExpression = new Expression(original.DecExpression, clone.Parent, clone.DecExpressionValidator);
            clone.PositionAngleExpression = new Expression(original.PositionAngleExpression, clone.Parent, clone.PositionAngleExpressionValidator);
            clone.Coordinates = original.Coordinates?.Clone();
            clone.OffsetExpression = new Expression(original.OffsetExpression, clone.Parent, clone.OffsetExpressionValidator);
        }

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

        [IsExpression(Default = 30, Range = [-90, 90], HasValidator = true )]
        public partial double Offset { get; set; }

        partial void OffsetExpressionValidator(Expression expr) {
            if (expr.Error == null && Data != null) {
                Data.Offset = expr.Value;
            }
        }

        [JsonProperty]
        public WaitLoopData Data { get; set; }

        private bool inherited;

        [JsonProperty]
        public bool Inherited {
            get => inherited;
            set {
                inherited = value;
                RaisePropertyChanged();
            }
        }

        private double lastRA;
        private double lastDec;

        protected void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            // When coordinates change, we change the decimal value
            InputCoordinates ic = (InputCoordinates)sender;
            Coordinates c = ic.Coordinates;

            if (Protect || Inherited || hydratingCoordinates || c == null) {
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
        }

        protected void ApplyInheritedCoordinates(ContextCoordinates coordinates) {
            bool previous = Protect;
            Protect = true;
            try {
                Coordinates.Coordinates = coordinates.Coordinates.Clone();
                PositionAngle = coordinates.PositionAngle;
            } finally {
                lastRA = Coordinates.Coordinates.RA;
                lastDec = Coordinates.Coordinates.Dec;
                Protect = previous;
            }
        }

        private void ClearInheritedCoordinates() {
            bool previous = Protect;
            Protect = true;
            try {
                Coordinates = new InputCoordinates();
                RaExpression.Definition = "0";
                DecExpression.Definition = "0";
                PositionAngleExpression.Definition = "0";
                lastRA = 0;
                lastDec = 0;
            } finally {
                Protect = previous;
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();

            var coordinates = ItemUtility.RetrieveContextCoordinates(this.Parent);
            if (coordinates != null) {
                Inherited = true;
                BindEffectiveCoordinates();
                ApplyInheritedCoordinates(coordinates);
            } else {
                if (Inherited) {
                    ClearInheritedCoordinates();
                }
                Inherited = false;
            }

            Validate();
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            throw new NotImplementedException();
        }
    }
}
