using System;

namespace NINA.Utility.Astrometry {
    public class SunRiseAndSet : RiseAndSetEvent {

        public SunRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        private double SunRiseDegree {
            get {
                //http://aa.usno.navy.mil/faq/docs/RST_defs.php #Paragraph Sunrise and sunset
                return Astrometry.ArcminToDegree(-50);
            }
        }

        protected override double AdjustAltitude(Body body) {
            return body.Altitude - SunRiseDegree;
        }

        protected override Body GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude);
        }
    }
}