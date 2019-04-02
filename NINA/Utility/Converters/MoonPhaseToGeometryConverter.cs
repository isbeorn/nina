#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    public class MoonPhaseToGeometryConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Astrometry.Astrometry.MoonPhase phase = (Astrometry.Astrometry.MoonPhase)value;
            if (phase == Astrometry.Astrometry.MoonPhase.NewMoon) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["NewMoonSVG"];
            } else if (phase == Astrometry.Astrometry.MoonPhase.FirstQuarter) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FirstQuarterMoonSVG"];
            } else if (phase == Astrometry.Astrometry.MoonPhase.FullMoon) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FullMoonSVG"];
            } else if (phase == Astrometry.Astrometry.MoonPhase.LastQuarter) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["LastQuarterMoonSVG"];
            } else if (phase == Astrometry.Astrometry.MoonPhase.WaningCrescent) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaningCrescentMoonSVG"];
            } else if (phase == Astrometry.Astrometry.MoonPhase.WaningGibbous) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaningGibbousMoonSVG"];
            } else if (phase == Astrometry.Astrometry.MoonPhase.WaxingCrescent) {
                return (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["WaxingCrescentMoonSVG"];
            } else if (phase == Astrometry.Astrometry.MoonPhase.WaxingGibbous) {
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