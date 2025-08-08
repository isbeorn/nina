#region "copyright"
/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"

using System;

namespace NINA.Astrometry {
    public static class DateTimeExtensions {
        /// <summary>
        /// DateTime to Julian Date
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static double ToJD(this DateTime dateTime) {
            return AstroUtil.GetJulianDate(dateTime);
        }

        /// <summary>
        /// DateTime to Modified Julian Date as defined by the SAO in 1957
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static double ToMJD(this DateTime dateTime) {
            // Modified Julian Date, SAO 1957 definition
            return AstroUtil.GetJulianDate(dateTime) - 2400000.5;
        }

        /// <summary>
        /// DateTime to Modified Julian Date, J2000, as defined by the ESA
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static double ToMJD2000(this DateTime dateTime) {
            // Modified Julian Date, J2000, ESA
            return AstroUtil.GetJulianDate(dateTime) - 2451545.0;
        }
    }
}