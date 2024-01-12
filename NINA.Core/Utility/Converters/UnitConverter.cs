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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Core.Utility.Converters {

    public class UnitConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (string.IsNullOrEmpty(value?.ToString())) return 0;

            var parameters = GetParameters(parameter);
            string unit = parameters.Item1;
            int? decimals = parameters.Item2;

            switch (value) {
                case double doubleValue when double.IsNaN(doubleValue):
                    return "--";

                case double doubleValue when !decimals.HasValue:
                    return $"{doubleValue}{unit}";

                case double doubleValue when decimals.HasValue:
                    var format = $"F{decimals.Value}";
                    return $"{doubleValue.ToString(format)}{unit}";

                case decimal decimalValue when !decimals.HasValue:
                    return $"{decimalValue}{unit}";

                case decimal decimalValue when decimals.HasValue:
                    var format2 = $"F{decimals.Value}";
                    return $"{decimalValue.ToString(format2)}{unit}";

                default:
                    return $"{value}{unit}";
            }
        }

        private (string, int?) GetParameters(object parameter) {
            string unit = string.Empty;
            int? decimals = null;

            var input = parameter?.ToString().Split('|');
            if (input?.Length >= 1) {
                unit = input[0];
            }
            if (input?.Length == 2) {
                if (int.TryParse(input[1], out var parsed)) {
                    decimals = parsed;
                }
            }
            return (unit, decimals);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (string.IsNullOrEmpty(value?.ToString())) return 0;

            var parameters = GetParameters(parameter);
            string unit = parameters.Item1;
            int? decimals = parameters.Item2;

            var sVal = value.ToString();
            var trimmedValue = sVal;
            if (!string.IsNullOrEmpty(unit)) {
                trimmedValue = sVal.EndsWith(unit) ? sVal.Remove(sVal.LastIndexOf(unit, StringComparison.Ordinal)) : sVal;
            }

            switch (targetType) {
                case Type doubleType when doubleType == typeof(double) && trimmedValue == "--":
                    return double.NaN;

                case Type doubleType when doubleType == typeof(double) && !decimals.HasValue:
                    return double.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultDouble) ? resultDouble : value;

                case Type doubleType when doubleType == typeof(double) && decimals.HasValue:
                    return double.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultDoubleRoundedToDecimals)
                        ? Math.Round(resultDoubleRoundedToDecimals, decimals.Value)
                        : value;

                case Type decimalType when decimalType == typeof(decimal) && !decimals.HasValue:
                    return decimal.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultDecimal) ? resultDecimal : value;

                case Type decimalType when decimalType == typeof(decimal) && decimals.HasValue:
                    return decimal.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultDecimalRoundedToDecimals)
                        ? Math.Round(resultDecimalRoundedToDecimals, decimals.Value)
                        : value;

                case Type intType when intType == typeof(int):
                    return int.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultInt) ? resultInt : value;

                case Type shortType when shortType == typeof(short):
                    return short.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultshort) ? resultshort : value;

                default:
                    return value;
            }
        }
    }
}