#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.Body;
using System;

namespace NINA.Astrometry.RiseAndSet {

    public class MoonRiseAndSet : MoonCustomRiseAndSet {

        [Obsolete("Use method with elevation parameter instead")]
        public MoonRiseAndSet(DateTime date, double latitude, double longitude) : this(date, latitude, longitude, elevation: 0) { }
        public MoonRiseAndSet(DateTime date, double latitude, double longitude, double elevation) : base(date, latitude, longitude, elevation, -AstroUtil.MoonUpperLimbApparentHorizonAltitude) {
        }

        protected override double AdjustAltitude(BasicBody body) {
            // NOVAS already includes parallax in the topocentric center altitude and distance.
            // The apparent upper limb reaches the horizon at 34 arcminutes of refraction plus
            // the Moon's angular radius. The inherited MoonAltitude retains its legacy value.
            // Reference: https://aa.usno.navy.mil/faq/RST_defs
            var angularRadius = AstroUtil.ToDegree(Math.Asin(body.Radius / body.Distance));
            return body.Altitude + 34.0 / 60.0 + angularRadius;
        }
    }
}