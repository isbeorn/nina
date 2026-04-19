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
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class CoordinatesInstruction : SequenceItem, IValidatable {

        public CoordinatesInstruction(ISequenceEntity e) : this() {
        }

        public CoordinatesInstruction() {
            Coordinates = new InputCoordinates();
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
                DecExpression.Definition = c.Dec.ToString(CultureInfo.InvariantCulture);
            }
            if (RaExpression.Definition.Length == 0 && c.RA != 0) {
                RaExpression.Definition = c.RA.ToString(CultureInfo.InvariantCulture);
            }
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
                    coordinates.PropertyChanged += Coordinates_PropertyChanged;
                }

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

        [IsExpression(Default = 0, Range = [0, 24], HasValidator = true)]
        public partial double Ra { get; set; }
        
        partial void RaExpressionValidator(Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputCoordinates ic = new InputCoordinates();
            Protect = true;
            ic.Coordinates.RA = expr.Value;
            Coordinates.RAHours = ic.RAHours;
            Coordinates.RAMinutes = ic.RAMinutes;
            Coordinates.RASeconds = ic.RASeconds;
            Protect = false;
        }

        [IsExpression(Default = 0, Range = [-90, 90], HasValidator = true)]
        public partial double Dec { get; set; }

        partial void DecExpressionValidator(Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputCoordinates ic = new InputCoordinates();
            Protect = true;
            ic.Coordinates.Dec = expr.Value;
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

            if (Protect) return;

            if (c.RA != lastRA) {
                RaExpression.Definition = Math.Round(c.RA, 7).ToString(CultureInfo.InvariantCulture);
            } else if (c.Dec != lastDec) {
                DecExpression.Definition = Math.Round(c.Dec, 7).ToString(CultureInfo.InvariantCulture);
            }

            lastRA = c.RA;
            lastDec = c.Dec;
        }

        protected void ApplyInheritedCoordinates(ContextCoordinates coordinates) {
            Protect = true;
            Coordinates.Coordinates = coordinates.Coordinates.Clone();
            PositionAngle = coordinates.PositionAngle;
            lastRA = Coordinates.Coordinates.RA;
            lastDec = Coordinates.Coordinates.Dec;
            Protect = false;
        }

        private void ClearInheritedCoordinates() {
            Protect = true;
            Coordinates = new InputCoordinates();
            RaExpression.Definition = "0";
            DecExpression.Definition = "0";
            PositionAngleExpression.Definition = "0";
            PositionAngle = 0;
            lastRA = 0;
            lastDec = 0;
            Protect = false;
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();

            var coordinates = ItemUtility.RetrieveContextCoordinates(this.Parent);
            if (coordinates != null) {
                ApplyInheritedCoordinates(coordinates);
                Inherited = true;
            } else {
                if (Inherited) {
                    ClearInheritedCoordinates();
                }
                Inherited = false;
            }

            RaExpression.Context = this;
            DecExpression.Context = this;
            PositionAngleExpression.Context = this;
            OffsetExpression.Context = this;
            Validate();
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            throw new NotImplementedException();
        }

        public bool Validate() {
            Expression.ValidateExpressions(Issues, RaExpression, DecExpression, PositionAngleExpression, OffsetExpression);
            RaisePropertyChanged("Issues");
            return Issues.Count == 0;
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
