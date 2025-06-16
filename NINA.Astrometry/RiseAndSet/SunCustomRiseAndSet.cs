using NINA.Astrometry.Body;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Astrometry.RiseAndSet {
    public class SunCustomRiseAndSet : RiseAndSetEvent {
        public SunCustomRiseAndSet(DateTime date, double latitude, double longitude, double elevation, double sunAltitude) : base(date, latitude, longitude, elevation) {
            SunAltitude = sunAltitude;
        }

        private double SunAltitude { get; }

        protected override double AdjustAltitude(BasicBody body) {
            return body.Altitude - SunAltitude;
        }

        protected override BasicBody GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude, Elevation);
        }
    }
}
