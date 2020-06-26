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
            if (string.IsNullOrEmpty(value?.ToString())) return 0;

            int decimals;
            switch (value) {
                case double doubleValue when parameter is null:
                    return doubleValue * 100d;

                case double doubleValue when parameter is string stringValue:
                    return Math.Round(doubleValue * 100d, int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimals) ? decimals : 0);

                case decimal decimalValue when parameter is null:
                    return decimalValue * 100m;

                case decimal decimalValue when parameter is string stringValue:
                    return Math.Round(decimalValue * 100m, int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimals) ? decimals : 0);

                default:
                    return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (string.IsNullOrEmpty(value?.ToString())) return 0;

            var trimmedValue = value.ToString().TrimEnd('%');

            int decimals;
            switch (targetType) {
                case Type doubleType when doubleType == typeof(double) && parameter is null:
                    return double.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultDouble) ? resultDouble / 100d : value;

                case Type doubleType when doubleType == typeof(double) && parameter is string stringValue:
                    int.TryParse(stringValue, out decimals);
                    return double.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultDoubleRoundedToDecimals)
                        ? Math.Round(resultDoubleRoundedToDecimals / 100d, decimals + 2)
                        : value;

                case Type decimalType when decimalType == typeof(decimal) && parameter is null:
                    return decimal.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultDecimal) ? resultDecimal / 100m : value;

                case Type decimalType when decimalType == typeof(decimal) && parameter is string stringValue:
                    int.TryParse(stringValue, out decimals);
                    return decimal.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultDecimalRoundedToDecimals)
                        ? Math.Round(resultDecimalRoundedToDecimals / 100m, decimals + 2)
                        : value;

                default:
                    return value;
            }
        }
    }
}