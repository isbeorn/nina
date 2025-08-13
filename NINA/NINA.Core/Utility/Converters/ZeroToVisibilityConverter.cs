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
using System.Windows.Data;

namespace NINA.Core.Utility.Converters {

    public class ZeroToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int) {
                var integer = (int)value;
                if (integer == 0) {
                    return System.Windows.Visibility.Collapsed;
                } else {
                    return System.Windows.Visibility.Visible;
                }
            }
            if (value is double) {
                var integer = (double)value;
                if (integer == 0d) {
                    return System.Windows.Visibility.Collapsed;
                } else {
                    return System.Windows.Visibility.Visible;
                }
            }
            throw new ArgumentException("Invalid Type for Converter");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class InverseZeroToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int) {
                var integer = (int)value;
                if (integer > 0d) {
                    return System.Windows.Visibility.Collapsed;
                } else {
                    return System.Windows.Visibility.Visible;
                }
            }
            if (value is double) {
                var integer = (double)value;
                if (integer > 0d) {
                    return System.Windows.Visibility.Collapsed;
                } else {
                    return System.Windows.Visibility.Visible;
                }
            }
            throw new ArgumentException("Invalid Type for Converter");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class ZeroToBooleanTrueConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int) {
                var integer = (int)value;
                if (integer == 0) {
                    return true;
                } else {
                    return false;
                }
            }
            if (value is double) {
                var integer = (double)value;
                if (integer == 0d) {
                    return true;
                } else {
                    return false;
                }
            }
            throw new ArgumentException("Invalid Type for Converter");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}