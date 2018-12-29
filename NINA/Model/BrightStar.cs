using NINA.Utility;
using NINA.Utility.Astrometry;
using System;

namespace NINA.Model {

    public class BrightStar : BaseINPC {

        public BrightStar(string name) {
            Name = name;
        }

        private string name;

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                coordinates = value;
                RaisePropertyChanged();
            }
        }

        private double magnitude;

        public double Magnitude {
            get => magnitude;
            set {
                magnitude = value;
                RaisePropertyChanged();
            }
        }

        private double azimuth;

        public double Azimuth {
            get => azimuth;
            set {
                azimuth = value;
                RaisePropertyChanged();
            }
        }

        public void CalculateAltitude(double latitude, double longitude) {
            var start = DateTime.UtcNow;
            var siderealTime = Astrometry.GetLocalSiderealTime(start, longitude);
            var hourAngle = Astrometry.GetHourAngle(siderealTime, this.Coordinates.RA);

            var degAngle = Astrometry.HoursToDegrees(hourAngle);
            Altitude = Astrometry.GetAltitude(degAngle, latitude, this.Coordinates.Dec);
            Azimuth = Astrometry.GetAzimuth(hourAngle, Altitude, latitude, this.Coordinates.Dec);
        }

        private double _altitude;

        public double Altitude {
            get => _altitude;
            set {
                _altitude = value;
                RaisePropertyChanged();
            }
        }

        public override string ToString() {
            return $"{Name} ({Azimuth:0.00}, Alt {Altitude:0.00})";
        }
    }
}