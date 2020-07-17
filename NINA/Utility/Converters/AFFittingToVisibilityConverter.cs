#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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