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

using NINA.Utility.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility.SkySurvey {

    internal class SkyAtlasSkySurvey : ISkySurvey {

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            byte[] arr = new byte[width * height];
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = 30;
            }

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256, arr, width);

            bitmap.Freeze();

            return new SkySurveyImage {
                Name = name,
                Source = nameof(SkyAtlasSkySurvey),
                Image = bitmap,
                FoVHeight = fieldOfView,
                FoVWidth = ((double)width / height) * fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }
    }
}