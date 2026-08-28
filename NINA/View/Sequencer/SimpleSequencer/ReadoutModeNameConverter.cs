#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using System.Windows.Data;

namespace NINA.View.SimpleSequencer {

    internal sealed class ReadoutModeNameConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 2 || values[0] is not IEnumerable<string> modes) {
                return string.Empty;
            }

            short modeIndex;
            if (values[1] is short configuredModeIndex) {
                modeIndex = configuredModeIndex;
            } else if (values.Length >= 3 && values[2] is short cameraModeIndex) {
                modeIndex = cameraModeIndex;
            } else {
                return string.Empty;
            }

            if (modeIndex < 0) {
                return string.Empty;
            }

            return modes.ElementAtOrDefault(modeIndex) ?? string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
