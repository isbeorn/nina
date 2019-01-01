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
using System.Windows.Data;
using Point = System.Windows.Point;

namespace NINA.Utility.Converters {

    internal class MultiDegreesToImageLeftConverter : IMultiValueConverter {

        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            // take imageparameter
            // take ra target
            // return ra margin
            SkySurveyImage image;
            DeepSkyObject dso;
            if (values[0] is SkySurveyImage) {
                image = (SkySurveyImage)values[0];
            } else {
                return 0;
            }

            if (values[1] is DeepSkyObject) {
                dso = (DeepSkyObject)values[1];
            } else {
                return 0;
            }

            var imageArcsecWidth = Astrometry.Astrometry.ArcminToArcsec(image.FoVWidth) / image.Image.Width;
            var imageArcsecHeight = Astrometry.Astrometry.ArcminToArcsec(image.FoVHeight) / image.Image.Height;

            var result = dso.Coordinates.ProjectFromCenter(image.Coordinates, new Point(image.Image.Width / 2, image.Image.Height / 2), imageArcsecWidth, imageArcsecHeight, 0);

            var dsoSize = dso.Size ?? 30;
            dsoSize /= 2;
            dsoSize /= imageArcsecWidth;

            return result.X - dsoSize;
        }

        public object[] ConvertBack(object value, System.Type[] targetType, object parameter, System.Globalization.CultureInfo culture) {
            return null;
        }
    }
}