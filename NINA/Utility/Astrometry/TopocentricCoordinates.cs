#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

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