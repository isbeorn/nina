#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces;
using System;
using System.Windows;
using System.Windows.Data;

namespace NINA.Equipment.Converter {

    public class ShutterStatusToVisibilityConverter : IValueConverter {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be Visibility");
            Visibility result;
            if (value == null || ((ShutterState)value) == ShutterState.ShutterNone) {
                result = System.Windows.Visibility.Collapsed;
            } else {
                result = System.Windows.Visibility.Visible;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }

        #endregion IValueConverter Members
    }
}