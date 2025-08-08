#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Astrometry.Converters {

    public class MoonPhaseToGeometryConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            AstroUtil.MoonPhase phase = (AstroUtil.MoonPhase)value;
            if (phase == AstroUtil.MoonPhase.NewMoon) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["NewMoonSVG"];
            } else if (phase == AstroUtil.MoonPhase.FirstQuarter) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FirstQuarterMoonSVG"];
            } else if (phase == AstroUtil.MoonPhase.FullMoon) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FullMoonSVG"];
            } else if (phase == AstroUtil.MoonPhase.LastQuarter) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["LastQuarterMoonSVG"];
            } else if (phase == AstroUtil.MoonPhase.WaningCrescent) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaningCrescentMoonSVG"];
            } else if (phase == AstroUtil.MoonPhase.WaningGibbous) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaningGibbousMoonSVG"];
            } else if (phase == AstroUtil.MoonPhase.WaxingCrescent) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaxingCrescentMoonSVG"];
            } else if (phase == AstroUtil.MoonPhase.WaxingGibbous) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaxingGibbousMoonSVG"];
            } else {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}