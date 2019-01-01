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
using NINA.Utility.SkySurvey;
using System.Windows;
using System.Windows.Data;

namespace NINA.Utility.Converters {

    internal class SkySurveyImageAndDSOToTopOrLeftDistanceConverter : IMultiValueConverter {

        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            SkySurveyImage image;
            DeepSkyObject dso;
            string leftOrTop;
            if (values[0] is SkySurveyImage) {
                image = (SkySurveyImage)values[0];
            } else {
                return 0.0;
            }

            if (values[1] is DeepSkyObject) {
                dso = (DeepSkyObject)values[1];
            } else {
                return 0.0;
            }

            if (values[2] is string) {
                leftOrTop = ((string)values[2]).ToUpper();
                if (!(leftOrTop == "LEFT" || leftOrTop == "TOP")) {
                    return 0.0;
                }
            } else {
                return 0.0;
            }

            var imageArcSecWidth = Astrometry.Astrometry.ArcminToArcsec(image.FoVWidth) / image.Image.Width;
            var imageArcSecHeight = Astrometry.Astrometry.ArcminToArcsec(image.FoVHeight) / image.Image.Height;

            var result = dso.Coordinates.ProjectFromCenterToXY(image.Coordinates, new Point(image.Image.Width / 2, image.Image.Height / 2), imageArcSecWidth, imageArcSecHeight, image.Rotation);

            var dsoSize = dso.Size ?? FovImageWidthAndDSOToDiameterConverter.DSO_DEFAULT_SIZE;
            dsoSize /= 2;
            if (leftOrTop == "LEFT") {
                dsoSize /= imageArcSecWidth;
                return result.X - dsoSize;
            } else {
                dsoSize /= imageArcSecHeight;
                return result.Y - dsoSize;
            }
        }

        public object[] ConvertBack(object value, System.Type[] targetType, object parameter, System.Globalization.CultureInfo culture) {
            return null;
        }
    }
}