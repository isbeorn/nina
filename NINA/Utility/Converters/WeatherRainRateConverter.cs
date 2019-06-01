#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    public class WeatherRainRateConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            string output;
            string hour = Locale.Loc.Instance["LblHour"];

            if ((bool)values[1]) {
                output = string.Format("{0:0.00} in/{1}", (double)values[0] / 25.4, hour);
            } else {
                output = string.Format("{0:0.00} mm/{1}", values[0], hour);
            }

            return output;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}