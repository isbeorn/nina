#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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