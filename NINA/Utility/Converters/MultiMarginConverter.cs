#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    internal class MultiMarginConverter : IMultiValueConverter {

        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            for (int i = 0; i < values.Length; i++) {
                if (values[i] == DependencyProperty.UnsetValue) {
                    values[i] = 0;
                }
            }

            return new Thickness(System.Convert.ToDouble(values[0]),
                                 System.Convert.ToDouble(values[1]),
                                 System.Convert.ToDouble(values[2]),
                                 System.Convert.ToDouble(values[3]));
        }

        public object[] ConvertBack(object value, System.Type[] targetType, object parameter, System.Globalization.CultureInfo culture) {
            return null;
        }
    }
}