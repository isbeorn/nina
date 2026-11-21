#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Model;
using System.Globalization;
using System.IO;

namespace NINA.Test {

    /// <summary>
    /// Creates horizons from horizon file content for tests
    /// </summary>
    internal static class HorizonTestData {

        /// <summary>Horizon with the same lower altitude at every azimuth</summary>
        public static CustomHorizon Flat(double altitude) {
            var value = altitude.ToString(CultureInfo.InvariantCulture);
            return FromPoints($"0 {value}\n360 {value}");
        }

        /// <summary>Horizon file content with a lower and/or an upper value (null = no value) at every azimuth</summary>
        public static CustomHorizon Dual(double? lower, double? upper) {
            string v(double? x) => x.HasValue ? x.Value.ToString(CultureInfo.InvariantCulture) : "-";
            return FromPoints($"0 {v(lower)} {v(upper)}\n360 {v(lower)} {v(upper)}");
        }

        /// <summary>Horizon from the content of a horizon file</summary>
        public static CustomHorizon FromPoints(string points) {
            return CustomHorizon.FromReader_Standard(new StringReader(points));
        }
    }
}
