#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using System;

namespace NINA.Core.Utility {

    public static class StarMeasurementUnitConverter {

        public static bool TryConvert(
            double value,
            StarMeasurementUnit sourceUnit,
            StarMeasurementUnit targetUnit,
            double arcsecPerPixel,
            out double convertedValue) {
            convertedValue = double.NaN;

            if (double.IsNaN(value) || double.IsInfinity(value)) {
                return false;
            }

            if (sourceUnit == targetUnit) {
                convertedValue = value;
                return true;
            }

            if (double.IsNaN(arcsecPerPixel) || double.IsInfinity(arcsecPerPixel) || arcsecPerPixel <= 0) {
                return false;
            }

            switch (sourceUnit) {
                case StarMeasurementUnit.Pixels when targetUnit == StarMeasurementUnit.Arcseconds:
                    convertedValue = value * arcsecPerPixel;
                    return true;
                case StarMeasurementUnit.Arcseconds when targetUnit == StarMeasurementUnit.Pixels:
                    convertedValue = value / arcsecPerPixel;
                    return true;
                default:
                    return false;
            }
        }
    }
}
