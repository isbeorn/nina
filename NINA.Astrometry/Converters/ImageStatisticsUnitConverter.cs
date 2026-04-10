#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using NINA.Astrometry;

namespace NINA.Astrometry.Converters {

    public class ImageStatisticsUnitConverter : IMultiValueConverter {
        private const string PixelsUnit = " px";
        private const string ArcsecondsUnit = "\"";

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (!TryGetDouble(values, 0, out var measurement) || double.IsNaN(measurement)) {
                return "--";
            }

            var showArcseconds = TryGetBool(values, 1, out var displayInArcseconds) && displayInArcseconds;
            var displayValue = measurement;
            var unit = PixelsUnit;

            if (showArcseconds) {
                if (!TryGetDouble(values, 2, out var pixelSize)
                    || !TryGetDouble(values, 3, out var focalLength)
                    || pixelSize <= 0
                    || focalLength <= 0
                    || double.IsNaN(pixelSize)
                    || double.IsNaN(focalLength)) {
                    return "--";
                }

                var pixelScale = AstroUtil.ArcsecPerPixel(pixelSize, focalLength);
                if (double.IsNaN(pixelScale) || double.IsInfinity(pixelScale) || pixelScale <= 0) {
                    return "--";
                }

                displayValue *= pixelScale;
                unit = ArcsecondsUnit;
            }

            var effectiveCulture = culture ?? CultureInfo.InvariantCulture;
            return displayValue.ToString("0.00", effectiveCulture) + unit;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        private static bool TryGetDouble(object[] values, int index, out double result) {
            result = default;
            if (!TryGetValue(values, index, out var value)) {
                return false;
            }

            switch (value) {
                case double doubleValue:
                    result = doubleValue;
                    return true;
                case float floatValue:
                    result = floatValue;
                    return true;
                case decimal decimalValue:
                    result = (double)decimalValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case short shortValue:
                    result = shortValue;
                    return true;
                case byte byteValue:
                    result = byteValue;
                    return true;
                default:
                    try {
                        result = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        return true;
                    } catch {
                        return false;
                    }
            }
        }

        private static bool TryGetBool(object[] values, int index, out bool result) {
            result = default;
            if (!TryGetValue(values, index, out var value)) {
                return false;
            }

            switch (value) {
                case bool boolValue:
                    result = boolValue;
                    return true;
                default:
                    try {
                        result = System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                        return true;
                    } catch {
                        return false;
                    }
            }
        }

        private static bool TryGetValue(object[] values, int index, out object value) {
            value = null;
            if (values == null || values.Length <= index) {
                return false;
            }

            value = values[index];
            if (value == null || value == DependencyProperty.UnsetValue || value == Binding.DoNothing) {
                return false;
            }

            return true;
        }
    }
}
