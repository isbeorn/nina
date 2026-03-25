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
                var strategy = MeridianTextPositionHelper.GetPositioningStrategy(nowTime, meridianTime);

                return strategy switch {
                    MeridianTextPositionHelper.PositioningStrategy.OffsetRight => OxyPlot.HorizontalAlignment.Right,
                    MeridianTextPositionHelper.PositioningStrategy.OffsetLeft => OxyPlot.HorizontalAlignment.Left,
                    _ => OxyPlot.HorizontalAlignment.Center
                };
            }

            return OxyPlot.HorizontalAlignment.Center;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
