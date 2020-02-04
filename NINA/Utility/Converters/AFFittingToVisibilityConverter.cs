#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

#endregion "copyright"

using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.Utility.Converters {

    internal class AFFittingToVisibilityConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            var source = (string)values[0];
            var method = (AFMethodEnum)values[1];
            var fitting = (AFCurveFittingEnum)values[2];

            if (method == AFMethodEnum.CONTRASTDETECTION) {
                if (source == "GaussianFitting") {
                    return (Application.Current.TryFindResource("ButtonBackgroundBrush") as SolidColorBrush).Color;
                }
            } else {
                if (source == "HyperbolicFitting" && (fitting == AFCurveFittingEnum.HYPERBOLIC || fitting == AFCurveFittingEnum.TRENDHYPERBOLIC)) {
                    return (Application.Current.TryFindResource("ButtonBackgroundBrush") as SolidColorBrush).Color;
                }
                if (source == "QuadraticFitting" && (fitting == AFCurveFittingEnum.PARABOLIC || fitting == AFCurveFittingEnum.TRENDPARABOLIC)) {
                    return (Application.Current.TryFindResource("ButtonBackgroundBrush") as SolidColorBrush).Color;
                }
                if (source == "Trendline" && (fitting == AFCurveFittingEnum.TRENDLINES || fitting == AFCurveFittingEnum.TRENDHYPERBOLIC || fitting == AFCurveFittingEnum.TRENDPARABOLIC)) {
                    return (Application.Current.TryFindResource("NotificationWarningBrush") as SolidColorBrush).Color;
                }
            }

            return Colors.Transparent;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}