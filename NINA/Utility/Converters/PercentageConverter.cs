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

    internal class PercentageConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (string.IsNullOrEmpty(value.ToString())) return 0;

            if (value.GetType() == typeof(double)) return (double)value * 100;

            if (value.GetType() == typeof(decimal)) return (decimal)value * 100;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (string.IsNullOrEmpty(value.ToString())) return 0;

            var trimmedValue = value.ToString().TrimEnd(new char[] { '%' });

            if (targetType == typeof(double)) {
                double result;
                if (double.TryParse(trimmedValue, out result))
                    return result / 100d;
                else
                    return value;
            }

            if (targetType == typeof(decimal)) {
                decimal result;
                if (decimal.TryParse(trimmedValue, out result))
                    return result / 100m;
                else
                    return value;
            }
            return value;
        }
    }
}
