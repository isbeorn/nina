using NINA.Utility;
using NINA.Utility.Astrometry;

namespace NINA.Model {

    internal class Star : BaseINPC {
        private double mag;
        private string name;
        private Coordinates coords;

        public Star(string name, Coordinates coords, double mag) {
            Mag = mag;
            Name = name;
            Coords = coords;
        }

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        public Coordinates Coords {
            get => coords;
            set {
                coords = value;
                RaisePropertyChanged();
            }
        }

        public double Mag {
            get => mag;
            set {
                mag = value;
                RaisePropertyChanged();
            }
        }
    }
}