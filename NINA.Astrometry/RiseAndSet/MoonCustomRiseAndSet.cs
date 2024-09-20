using NINA.Astrometry.Body;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Astrometry.RiseAndSet {
    public class MoonCustomRiseAndSet : RiseAndSetEvent {

        public MoonCustomRiseAndSet(DateTime date, double latitude, double longitude, double moonAltitude) : base(date, latitude, longitude) {
            MoonAltitude = moonAltitude;
        }

        public double MoonAltitude { get; }

        protected override double AdjustAltitude(BasicBody body) {
            /* Readjust moon altitude based on earth radius and refraction */
            var horizon = 90.0 - MoonAltitude;
            var location = new NOVAS.OnSurface() {
                Latitude = Latitude,
                Longitude = Longitude
            };
            var refraction = NOVAS.Refract(ref location, NOVAS.RefractionOption.StandardRefraction, horizon); ;
            var altitude = body.Altitude - AstroUtil.ToDegree(Earth.Radius) / body.Distance + AstroUtil.ToDegree(body.Radius) / body.Distance + refraction;
            return altitude - MoonAltitude;
        }

        protected override BasicBody GetBody(DateTime date) {
            return new Moon(date, Latitude, Longitude);
        }
    }
}
