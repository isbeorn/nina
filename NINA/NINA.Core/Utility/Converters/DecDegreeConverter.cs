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

    public class DecDegreeConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length == 0) {
                return string.Empty;
            }
            if (values[0] == DependencyProperty.UnsetValue) {
                return string.Empty;
            }

            var isNegative = (bool)values[0];
            int degrees = (int)values[1];

            if (degrees == 0 && isNegative) {
                return "-0";
            } else {
                return degrees.ToString(CultureInfo.InvariantCulture);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            string degrees = (string)value;
            if (degrees == "-0") {
                return new object[] { true, 0 };
            } else {
                if (int.TryParse(degrees, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)) {
                    return new object[] { number < 0, number };
                } else {
                    return new object[] { true, 0 };
                }
            }
        }
    }
}