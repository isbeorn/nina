#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Model.MyCamera {

    public enum SensorType {
        /*
         * 0-19: ASCOM definitions
         */

        //     monochrome - no bayer encoding
        Monochrome = 0,

        //     Color image without bayer encoding
        Color = 1,

        //     RGGB bayer encoding
        RGGB = 2,

        //     CMYG bayer encoding
        CMYG = 3,

        //     CMYG2 bayer encoding
        CMYG2 = 4,

        //     Camera produces Kodak TRUESENSE Bayer LRGB array images
        LRGB = 5,

        /*
         * 20-26: Non-ASCOM bayer matrix types
         */

        BGGR = 20,
        GBRG = 21,
        GRBG = 22,
        GRGB = 23,
        GBGR = 24,
        RGBG = 25,
        BGRG = 26
    }
}
