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

    public class BoolToLabelConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string parameterString = parameter as string;
            if (!string.IsNullOrEmpty(parameterString)) {
                string[] parameters = parameterString.Split(new char[] { '|' });
                if (parameters.Length != 2) {
                    throw new Exception("Two Parameters required. Must be separated by |");
                }
                if ((bool)value) {
                    return Locale.Loc.Instance[parameters[0]];
                } else {
                    return Locale.Loc.Instance[parameters[1]];
                }
            } else {
                throw new Exception("Two Parameters required. Must be separated by |");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}