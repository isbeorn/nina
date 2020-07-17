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

    public class Sun : Body {

        public Sun(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        public override double Radius {
            get {
                return 696342; // https://de.wikipedia.org/wiki/Sonnenradius
            }
        }

        protected override string Name {
            get {
                return "Sun";
            }
        }

        protected override NOVAS.Body BodyNumber {
            get {
                return NOVAS.Body.Sun;
            }
        }
    }
}