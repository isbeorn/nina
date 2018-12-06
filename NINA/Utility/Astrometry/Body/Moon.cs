using System;

namespace NINA.Utility.Astrometry {
    public class Moon : Body {

        public Moon(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        public override double Radius {
            get {
                return 1738; // https://de.wikipedia.org/wiki/Monddurchmesser
            }
        }

        protected override string Name {
            get {
                return "Moon";
            }
        }

        protected override NOVAS.Body BodyNumber {
            get {
                return NOVAS.Body.Moon;
            }
        }
    }
}