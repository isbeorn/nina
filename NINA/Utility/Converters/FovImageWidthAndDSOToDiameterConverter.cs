#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    internal class FovImageWidthAndDSOToDiameterConverter : IMultiValueConverter {
        public const double DSO_DEFAULT_SIZE = 30.0;

        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double fov;
            double widthOrHeight;
            DeepSkyObject dso;
            if (values[0] is double) {
                fov = (double)values[0];
            } else {
                return 0;
            }

            if (values[1] is double) {
                widthOrHeight = (double)values[1];
            } else {
                return 0;
            }

            if (values[2] is DeepSkyObject) {
                dso = (DeepSkyObject)values[2];
            } else {
                return 0;
            }

            var arcSecScale = Astrometry.Astrometry.ArcminToArcsec(fov) / widthOrHeight;

            if (dso.Size != null) {
                return dso.Size / arcSecScale;
            } else {
                return DSO_DEFAULT_SIZE;
            }
        }

        public object[] ConvertBack(object value, System.Type[] targetType, object parameter, System.Globalization.CultureInfo culture) {
            return null;
        }
    }
}