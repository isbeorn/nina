using NINA.Utility;
using NINA.Utility.Astrometry;
using System.Windows;

namespace NINA.Model {

    internal class Star : BaseINPC {
        private Point position;
        private double radius;

        public Star(int id, string name, Coordinates coords, double mag) {
            Id = id;
            Mag = mag;
            Name = name;
            Coords = coords;
        }

        public int Id { get; }

        public string Name { get; }

        public Coordinates Coords { get; }

        public double Mag { get; }

        public double Radius {
            get => radius;
            set {
                radius = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TextPadding));
            }
        }

        public double TextPadding => Position.Y + Radius;

        public Point Position {
            get => position;
            set {
                position = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TextPadding));
            }
        }
    }
}