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
using System.Windows.Data;

namespace NINA.View.Sequencer.Converter {

    public class SymbolValueDisplayConverter : IValueConverter {
        internal const string DateFormat = "yyyy-MM-dd";
        internal const string TimeFormat = "HH:mm:ss";
        internal const string DateTimeFormat = DateFormat + " " + TimeFormat;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is DateTime dateTime) {
                return dateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
            } else if (value is DateTimeOffset dateTimeOffset) {
                return dateTimeOffset.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
            } else if (value is DateOnly dateOnly) {
                return dateOnly.ToString(DateFormat, CultureInfo.InvariantCulture);
            } else if (value is TimeOnly timeOnly) {
                return timeOnly.ToString(TimeFormat, CultureInfo.InvariantCulture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
