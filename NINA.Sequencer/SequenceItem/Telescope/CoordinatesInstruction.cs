using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [JsonObject(MemberSerialization.OptIn)]
    [ExpressionObject]

    public partial class CoordinatesInstruction : SequenceItem {

        public CoordinatesInstruction(CoordinatesInstruction ci) { }

        partial void AfterClone(CoordinatesInstruction clone, CoordinatesInstruction cloned) {
            clone.Coordinates = cloned.Coordinates?.Clone();
        }

        [IsExpression(Default = 0, Range = [0, 24], HasValidator = true)]
        private double ra = 0;

        protected bool Protect = false;

        //[JsonProperty]
        public InputCoordinates Coordinates { get; set; } = new InputCoordinates();


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


        protected void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            // When coordinates change, we change the decimal value
            InputCoordinates ic = (InputCoordinates)sender;
            Coordinates c = ic.Coordinates;

            if (Protect) return;

            if (e.PropertyName.StartsWith("RA")) {
                RaExpression.Definition = Math.Round(c.RA, 5).ToString();
            } else if (e.PropertyName.StartsWith("Dec")) {
                DecExpression.Definition = Math.Round(c.Dec, 5).ToString();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            throw new NotImplementedException();
        }

        public IList<string> Issues => new List<string>();
    }
}
