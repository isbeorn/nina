using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Astrometry {

    public class TopocentricCoordinates {
        public Angle Azimuth { get; private set; }
        public Angle Altitude { get; private set; }
        public Angle Latitude { get; private set; }
        public Angle Longitude { get; private set; }

        public TopocentricCoordinates(Angle azimuth, Angle altitude, Angle latitude, Angle longitude) {
            this.Azimuth = azimuth;
            this.Altitude = altitude;
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public Coordinates Transform(Epoch epoch) {
            var now = DateTime.Now;
            var jdUTC = Astrometry.GetJulianDate(now);

            var zenithDistance = Astrometry.ToRadians(90d - Altitude.Degree);
            var deltaUT = Astrometry.DeltaUT(now);

            var raRad = 0d;
            var decRad = 0d;
            SOFA.TopocentricToCelestial("A", Azimuth.Radians, zenithDistance, jdUTC, 0d, deltaUT, Longitude.Radians, Latitude.Radians, 0d, 0d, 0d, 0d, 0d, 0d, 0d, ref raRad, ref decRad);
            var ra = Angle.ByRadians(raRad);
            var dec = Angle.ByRadians(decRad);

            var coordinates = new Coordinates(ra, dec, Epoch.J2000);
            return coordinates.Transform(epoch);
        }
    }
}