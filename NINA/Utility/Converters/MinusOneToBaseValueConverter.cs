#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    public class MinusOneToBaseValueConverter : IMultiValueConverter {

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            if (int.TryParse(value[0] + "", out var result)) {
                if (result == -1) {
                    if(value[1].ToString() == "{DependencyProperty.UnsetValue}") {
                        value[1] = Locale.Loc.Instance["LblCamera"];
                    }
                    return "(" + value[1] + ")";
                } else {
                    return "" + value[0];
                }
            }

            return "(" + value[1] + ")";
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            if (int.TryParse(value.ToString(), out int result)) {
                return new object[] { result };
            }

            return new object[] { -1 };
        }
    }
}