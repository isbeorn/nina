#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum BayerPatternEnum {

        [Description("LblAuto")]
        Auto = 0,

        [Description("LblBayerPatternRGGB")]
        RGGB = 2,

        [Description("LblBayerPatternBGGR")]
        BGGR = 20,

        [Description("LblBayerPatternGBRG")]
        GBRG = 21,

        [Description("LblBayerPatternGRBG")]
        GRBG = 22,

        [Description("LblBayerPatternGRGB")]
        GRGB = 23,

        [Description("LblBayerPatternGBGR")]
        GBGR = 24,

        [Description("LblBayerPatternRGBG")]
        RGBG = 25,

        [Description("LblBayerPatternBGRG")]
        BGRG = 26
    }
}