#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using System.ComponentModel;

namespace NINA.Utility.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum XISFCompressionTypeEnum {

        [Description("LblNone")]
        NONE = 0,

        [Description("LblCompressionLZ4")]
        LZ4,

        [Description("LblCompressionLZ4HC")]
        LZ4HC,

        [Description("LblCompressionZLib")]
        ZLIB
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum XISFChecksumTypeEnum {

        [Description("LblNone")]
        NONE = 0,

        [Description("LblChecksumSHA_1")]
        SHA1,

        [Description("LblChecksumSHA_256")]
        SHA256,

        [Description("LblChecksumSHA_512")]
        SHA512,

        [Description("LblChecksumSHA3_256")]
        SHA3_256,

        [Description("LblChecksumSHA3_512")]
        SHA3_512
    }
}
