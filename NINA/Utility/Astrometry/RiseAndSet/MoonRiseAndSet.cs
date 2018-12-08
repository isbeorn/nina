using System;

namespace NINA.Utility.Astrometry {

    public class MoonRiseAndSet : RiseAndSetEvent {

        public MoonRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        protected override double AdjustAltitude(Body body) {
            /* Readjust moon altitude based on earth radius and refraction */
            var horizon = 90.0;
            var location = new NOVAS.OnSurface() {
                Latitude = Latitude,
                Longitude = Longitude
            };
            var refraction = NOVAS.Refract(ref location, NOVAS.RefractionOption.StandardRefraction, horizon); ;
            var altitude = body.Altitude - Astrometry.ToDegree(Earth.Radius) / body.Distance + Astrometry.ToDegree(body.Radius) / body.Distance + refraction;
            return altitude;
        }

        protected override Body GetBody(DateTime date) {
            return new Moon(date, Latitude, Longitude);
        }
    }
}