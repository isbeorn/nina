using NINA.Astrometry.Body;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Astrometry.RiseAndSet {
    public class SunCustomRiseAndSet : RiseAndSetEvent {
        public SunCustomRiseAndSet(DateTime date, double latitude, double longitude, double sunAltitude) : base(date, latitude, longitude) {
            SunAltitude = sunAltitude;
        }

        private double SunAltitude { get; }

        protected override double AdjustAltitude(BasicBody body) {
            /* Readjust altitude based on earth radius and refraction */
            var zenithDistance = 90d - SunAltitude;
            var location = new NOVAS.OnSurface() {
                Latitude = Latitude,
                Longitude = Longitude
            };
            var refraction = NOVAS.Refract(ref location, NOVAS.RefractionOption.StandardRefraction, zenithDistance);
            var altitude = body.Altitude - AstroUtil.ToDegree(Earth.Radius) / body.Distance + AstroUtil.ToDegree(body.Radius) / body.Distance + refraction;
            return altitude - SunAltitude;
        }

        protected override BasicBody GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude);
        }
    }
}
