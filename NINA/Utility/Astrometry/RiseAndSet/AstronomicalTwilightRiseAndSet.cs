using System;

namespace NINA.Utility.Astrometry {
    public class AstronomicalTwilightRiseAndSet : RiseAndSetEvent {

        public AstronomicalTwilightRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        private double AstronomicalTwilightDegree {
            get {
                //http://aa.usno.navy.mil/faq/docs/RST_defs.php #Paragraph Astronomical twilight
                return -18;
            }
        }

        protected override double AdjustAltitude(Body body) {
            return body.Altitude - AstronomicalTwilightDegree;
        }

        protected override Body GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude);
        }
    }
}