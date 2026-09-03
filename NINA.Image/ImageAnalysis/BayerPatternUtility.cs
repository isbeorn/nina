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

namespace NINA.Image.ImageAnalysis {

    public static class BayerPatternUtility {

        public static SensorType ApplyOffsets(SensorType bayerPattern, int offsetX, int offsetY) {
            if (offsetX % 2 != 0) {
                bayerPattern = bayerPattern switch {
                    SensorType.RGGB => SensorType.GRBG,
                    SensorType.GRBG => SensorType.RGGB,
                    SensorType.GBRG => SensorType.BGGR,
                    SensorType.BGGR => SensorType.GBRG,
                    SensorType.GRGB => SensorType.RGBG,
                    SensorType.RGBG => SensorType.GRGB,
                    SensorType.GBGR => SensorType.BGRG,
                    SensorType.BGRG => SensorType.GBGR,
                    _ => bayerPattern
                };
            }

            if (offsetY % 2 != 0) {
                bayerPattern = bayerPattern switch {
                    SensorType.RGGB => SensorType.GBRG,
                    SensorType.GBRG => SensorType.RGGB,
                    SensorType.GRBG => SensorType.BGGR,
                    SensorType.BGGR => SensorType.GRBG,
                    SensorType.GRGB => SensorType.GBGR,
                    SensorType.GBGR => SensorType.GRGB,
                    SensorType.RGBG => SensorType.BGRG,
                    SensorType.BGRG => SensorType.RGBG,
                    _ => bayerPattern
                };
            }

            return bayerPattern;
        }
    }
}