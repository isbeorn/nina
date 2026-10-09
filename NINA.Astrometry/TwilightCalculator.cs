#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

        [Obsolete("Use method with elevation parameter instead")]
        public TimeSpan GetTwilightDuration(DateTime date, double latitude, double longitude) {
            return GetTwilightDuration(date, latitude, longitude, 0.0);
        }

        public TimeSpan GetTwilightDuration(DateTime date, double latitude, double longitude, double elevation) {
            var sunrise = AstroUtil.GetSunRiseAndSet(date.ToUniversalTime(), latitude, longitude, elevation).Rise;
            if (!sunrise.HasValue) {
                return TimeSpan.Zero;
            }

            // Search backwards from the upcoming sunrise so a request after dawn cannot pair
            // today's sunrise with tomorrow's dawn. UTC arithmetic also avoids DST clock jumps.
            var precedingDay = sunrise.Value.AddDays(-1);
            var dawn = AstroUtil.GetNightTimes(precedingDay, latitude, longitude, elevation).Rise;
            if (dawn.HasValue && dawn.Value <= sunrise.Value) {
                return sunrise.Value - dawn.Value;
            }

            var sunset = AstroUtil.GetSunRiseAndSet(precedingDay, latitude, longitude, elevation).Set;
            if (sunset.HasValue && sunset.Value <= sunrise.Value) {
                return sunrise.Value - sunset.Value;
            }
            return TimeSpan.Zero;
        }
    }
}