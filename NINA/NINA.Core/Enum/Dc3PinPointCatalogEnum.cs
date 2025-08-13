#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System.ComponentModel;

namespace NINA.Core.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Dc3PoinPointCatalogEnum {

        [Description("LblPinPointCatGscact")]
        ppGSCACT = 3,

        [Description("LblPinPointCatTycho2")]
        ppTycho2 = 4,

        [Description("LblPinPointCatUsnoA")]
        ppUSNO_A = 5,

        [Description("LblPinPointCatUcac2")]
        ppUCAC2 = 6,

        [Description("LblPinPointCatUsnoB")]
        ppUSNO_B = 7,

        [Description("LblPinPointCatUcac3")]
        ppUCAC3 = 10,

        [Description("LblPinPointCatUcac4")]
        ppUCAC4 = 11,

        [Description("LblPinPointCatAtas")]
        ppAtlas = 12,
    }
}
