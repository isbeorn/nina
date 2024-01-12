#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NINA.Core.Utility.Converters {

    public class MinusOneToBaseValueConverter : IMultiValueConverter {

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            if (value[0] == DependencyProperty.UnsetValue) {
                value[0] = -1;
            }

            var param = new object[] { value[0].ToString(), null };
            var parsed = (bool)value[0].GetType()
                .GetMethod("TryParse", new[] { typeof(string), value[0].GetType().MakeByRefType() })
                .Invoke(null, param);
            if (parsed) {
                if (param[1].Equals(System.Convert.ChangeType(-1, param[1].GetType()))) {
                    if (value[1].ToString() == "-1") {
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
            var param = new object[] { value.ToString(), null };
            var parsed = (bool)targetType[0]
                .GetMethod("TryParse", new[] { typeof(string), targetType[0].MakeByRefType() })
                .Invoke(null, param);
            if (parsed) {
                return new object[] { param[1] };
            }

            return new object[] { -1 };
        }
    }
}