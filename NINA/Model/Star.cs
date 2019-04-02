using NINA.Utility.Astrometry;
using System.Drawing;

namespace NINA.Model {

    internal class Star {

        public Star(int id, string name, Coordinates coords, float mag) {
            Id = id;
            Mag = mag;
            Name = name;
            Coords = coords;
        }

        public int Id { get; }

        public string Name { get; }

        public Coordinates Coords { get; }

        public float Mag { get; }

        public float Radius { get; set; }

        public float TextPadding => Position.Y + Radius;

        public PointF Position { get; set; }
    }
}