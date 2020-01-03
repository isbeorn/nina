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