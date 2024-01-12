#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry.Interfaces;
using System;

namespace NINA.Astrometry {

    public class TwilightCalculator : ITwilightCalculator {

        public TimeSpan GetTwilightDuration(DateTime date, double latitude, double longitude) {
            var nightRise = AstroUtil.GetNightTimes(date, latitude, longitude).Rise;
            var sunRiseAndSet = AstroUtil.GetSunRiseAndSet(date, latitude, longitude);
            if (nightRise == null) {
                return sunRiseAndSet.Rise - sunRiseAndSet.Set ?? TimeSpan.Zero;
            }            

            return sunRiseAndSet.Rise - nightRise ?? TimeSpan.Zero;
        }
    }
}