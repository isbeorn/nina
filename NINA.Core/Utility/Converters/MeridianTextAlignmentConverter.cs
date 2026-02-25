#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OxyPlot;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Core.Utility.Converters {

    public class MeridianTextAlignmentConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values == null || values.Length < 2) {
                return OxyPlot.HorizontalAlignment.Center;
            }

            if (values[0] is double nowTime && values[1] is double meridianTime) {
                if (double.IsNaN(nowTime) || double.IsNaN(meridianTime)) {
                    return OxyPlot.HorizontalAlignment.Center;
                }

                // Calculate distance between Now and meridian
                double distance = Math.Abs(nowTime - meridianTime);
                double oneAndHalfHours = 0.0625; // ~1.5 hours

                // If they're far apart (> 1.5 hours), center text on meridian
                if (distance > oneAndHalfHours) {
                    return OxyPlot.HorizontalAlignment.Center;
                }

                // They're close (< 1.5 hours), position to avoid Now line
                double signedDistance = nowTime - meridianTime;

                if (signedDistance > 0) {
                    // Now is to the RIGHT of meridian
                    // Use Right alignment so text ends at the Now line
                    return OxyPlot.HorizontalAlignment.Right;
                } else {
                    // Now is to the LEFT of meridian
                    // Use Left alignment so text starts after the Now line
                    return OxyPlot.HorizontalAlignment.Left;
                }
            }

            return OxyPlot.HorizontalAlignment.Center;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
