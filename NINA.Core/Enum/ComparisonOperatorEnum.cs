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
    public enum ComparisonOperatorEnum {

        [Description("LblComparatorEquals")]
        EQUALS,

        [Description("LblCompatatorLessThan")]
        LESS_THAN,

        [Description("LblComparatorLessThanOrEqual")]
        LESS_THAN_OR_EQUAL,

        [Description("LblComparatorGreaterThan")]
        GREATER_THAN,

        [Description("LblComparatorGreaterThanOrEqual")]
        GREATER_THAN_OR_EQUAL,

        [Description("LblComparatorNotEqual")]
        NOT_EQUAL
    }
}