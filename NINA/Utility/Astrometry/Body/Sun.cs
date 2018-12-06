using System;

namespace NINA.Utility.Astrometry {
    public class Sun : Body {

        public Sun(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        public override double Radius {
            get {
                return 696342; // https://de.wikipedia.org/wiki/Sonnenradius
            }
        }

        protected override string Name {
            get {
                return "Sun";
            }
        }

        protected override NOVAS.Body BodyNumber {
            get {
                return NOVAS.Body.Sun;
            }
        }
    }
}