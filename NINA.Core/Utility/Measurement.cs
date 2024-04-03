using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Utility {
    public class Measurement {
        private List<Measurement> subMeasurements;
        public Measurement(string name) {
            Name = name;
            StartTime = DateTimeOffset.UtcNow;
            EndTime = DateTimeOffset.UtcNow;
            subMeasurements = new List<Measurement>();
        }

        public Measurement Start() {
            StartTime = DateTimeOffset.UtcNow;
            return this;
        }

        public Measurement Stop() {
            EndTime = DateTimeOffset.UtcNow;
            return this;
        }

        public string Name { get; }
        public TimeSpan Duration => EndTime - StartTime;
        public DateTimeOffset StartTime { get; private set; }
        public DateTimeOffset EndTime { get; private set; }
        public IReadOnlyCollection<Measurement> SubMeasurements { get => subMeasurements.AsReadOnly(); }

        public Measurement AddSubMeasurement(Measurement measurement) {
            subMeasurements.Add(measurement);
            return this;
        }

        public override string ToString() {
            return $"{Name} - {Duration}";
        }
    }
}
