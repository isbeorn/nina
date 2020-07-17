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

    public class SunRiseAndSet : RiseAndSetEvent {

        public SunRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        private double SunRiseDegree {
            get {
                //http://aa.usno.navy.mil/faq/docs/RST_defs.php #Paragraph Sunrise and sunset
                return Astrometry.ArcminToDegree(-50);
            }
        }

        protected override double AdjustAltitude(Body body) {
            return body.Altitude - SunRiseDegree;
        }

        protected override Body GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude);
        }
    }
}