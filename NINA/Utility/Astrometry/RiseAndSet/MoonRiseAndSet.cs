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