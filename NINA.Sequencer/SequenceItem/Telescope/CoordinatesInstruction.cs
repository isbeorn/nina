using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
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
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class CoordinatesInstruction : SequenceItem {

        public CoordinatesInstruction(ISequenceEntity e) {
        }

        public CoordinatesInstruction() {
        }

        partial void AfterClone(CoordinatesInstruction clone) {
            clone.Coordinates = Coordinates?.Clone();
        }

        protected bool Protect = false;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            // Fix up Ra and Dec Expressions (auto-update to existing sequences)
            Coordinates c = Coordinates.Coordinates;
            if (DecExpression.Definition.Length == 0 && c.Dec != 0) {
                DecExpression.Definition = c.Dec.ToString();
            }
            if (RaExpression.Definition.Length == 0 && c.RA != 0) {
                RaExpression.Definition = c.RA.ToString();
            }
        }


        [JsonProperty(propertyName: "Coordinates")]
        private InputCoordinates DeprecatedCoordinates {
            set => coordinates = value;
        }


        private InputCoordinates coordinates = new InputCoordinates();

        [JsonIgnore]
        public InputCoordinates Coordinates {
            get {
                return coordinates;
            }
            set {
                coordinates = value;
            }
        }

        public void UpdateExpressions(CoordinatesInstruction clone, CoordinatesInstruction cloned) {
            clone.RaExpression = new Expression(RaExpression);
            clone.DecExpression = new Expression(DecExpression);
            clone.PositionAngleExpression = new Expression(PositionAngleExpression);
            clone.Coordinates = cloned.Coordinates?.Clone();
            clone.OffsetExpression = new Expression(OffsetExpression);
        }

        [IsExpression(Default = 0, Range = [0, 24], HasValidator = true)]
        private double ra = 0;

        partial void RaExpressionValidator(Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputCoordinates ic = new InputCoordinates();
            Protect = true;
            ic.Coordinates.RA = RaExpression.Value;
            Coordinates.RAHours = ic.RAHours;
            Coordinates.RAMinutes = ic.RAMinutes;
            Coordinates.RASeconds = ic.RASeconds;
            Protect = false;
        }

        [IsExpression(Default = 0, Range = [-90, 90], HasValidator = true)]
        private double dec = 0;

        partial void DecExpressionValidator(Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputCoordinates ic = new InputCoordinates();
            Protect = true;
            ic.Coordinates.Dec = DecExpression.Value;
            Coordinates.DecDegrees = ic.DecDegrees;
            Coordinates.DecMinutes = ic.DecMinutes;
            Coordinates.DecSeconds = ic.DecSeconds;
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

        [IsExpression(Default = 30, Range = [-90, 90], HasValidator = true )]
        private double offset;

        partial void OffsetExpressionValidator(Expression expr) {
            if (expr.Error == null) {
                Data.Offset = expr.Value;
            }
        }

        [JsonProperty]
        public WaitLoopData Data { get; set; }

        private bool hasDsoParent;

        [JsonProperty]
        public bool HasDsoParent {
            get => hasDsoParent;
            set {
                hasDsoParent = value;
                RaisePropertyChanged();
            }
        }

        private double lastRA;
        private double lastDec;

        protected void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
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
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();

            var coordinates = ItemUtility.RetrieveContextCoordinates(this.Parent);
            if (coordinates != null) {
                Coordinates.Coordinates = coordinates.Coordinates;
                PositionAngle = coordinates.PositionAngle;
                HasDsoParent = true;
            } else {
                HasDsoParent = false;
            }

            if (Coordinates != null) {
                Coordinates.PropertyChanged += Coordinates_PropertyChanged;
            }
            RaExpression.Context = this;
            DecExpression.Context = this;
            PositionAngleExpression.Context = this;
            OffsetExpression.Context = this;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            throw new NotImplementedException();
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get { return issues; }
            set {
                Issues = value;
            }
        }
    }
}
