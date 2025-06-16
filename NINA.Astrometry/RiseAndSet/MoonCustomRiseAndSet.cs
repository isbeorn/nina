using NINA.Astrometry.Body;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Astrometry.RiseAndSet {
    public class MoonCustomRiseAndSet : RiseAndSetEvent {

        public MoonCustomRiseAndSet(DateTime date, double latitude, double longitude, double elevation, double moonAltitude) : base(date, latitude, longitude, elevation) {
            MoonAltitude = moonAltitude;
        }

        public double MoonAltitude { get; }

        protected override double AdjustAltitude(BasicBody body) {
            return body.Altitude - MoonAltitude;
        }

        protected override BasicBody GetBody(DateTime date) {
            return new Moon(date, Latitude, Longitude, Elevation);
        }
    }
}
