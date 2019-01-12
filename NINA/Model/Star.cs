using NINA.Utility;
using NINA.Utility.Astrometry;
using System.Windows;

namespace NINA.Model {

    internal class Star : BaseINPC {
        private double mag;
        private string name;
        private Coordinates coords;
        private Point position;

        public Star(int id, string name, Coordinates coords, double mag) {
            Id = id;
            Mag = mag;
            Name = name;
            Coords = coords;
        }

        public int Id { get; set; }

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

        public Point Position {
            get => position;
            set {
                position = value;
                RaisePropertyChanged();
            }
        }
    }
}