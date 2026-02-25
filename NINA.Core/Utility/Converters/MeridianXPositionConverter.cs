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

namespace NINA.Core.Utility.Converters {

    public class MeridianXPositionConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values == null || values.Length < 2) {
                return 0.0;
            }

            if (values[0] is double nowTime && values[1] is double meridianTime) {
                if (double.IsNaN(nowTime) || double.IsNaN(meridianTime)) {
                    return meridianTime;
                }

                // Calculate distance between Now and meridian
                double distance = Math.Abs(nowTime - meridianTime);
                double oneAndHalfHours = 0.0625; // ~1.5 hours in OxyPlot datetime units (1 day = 1.0)

                // If they're far apart (> 1.5 hours), center text on meridian
                if (distance > oneAndHalfHours) {
                    return meridianTime;
                }

                // They're close (< 1.5 hours), position text to avoid Now line
                double signedDistance = nowTime - meridianTime;

                if (signedDistance > 0) {
                    // Now is to the RIGHT of meridian
                    // Position text to end just BEFORE the Now line (will use Right alignment)
                    double margin = 0.05; // Margin (~1.2 hours) to provide clearance before Now line including text width
                    return nowTime - margin;
                } else {
                    // Now is to the LEFT of meridian
                    // Position text to start just AFTER the "Now" text (will use Left alignment)
                    double margin = 0.06; // Large margin (~1.5 hours) to clear vertical "Now" label
                    return nowTime + margin;
                }
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
