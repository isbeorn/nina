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

using NINA.Utility.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.SkySurvey {

    internal class SkyServerSkySurvey : ISkySurvey {
        private const string Url = "http://skyserver.sdss.org/dr14/SkyserverWS/ImgCutout/getjpeg?ra={0}&dec={1}&width={2}&height={3}&scale={4}";

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress, int width, int height) {
            var arcSecPerPixel = 0.4;
            var targetFoVInArcSec = Astrometry.Astrometry.ArcminToArcsec(fieldOfView);
            var pixels = Math.Min(targetFoVInArcSec / arcSecPerPixel, 2048);
            if (pixels == 2048) {
                arcSecPerPixel = targetFoVInArcSec / 2048;
            }

            var request = new Http.HttpDownloadImageRequest(
                Url,
                coordinates.RADegrees,
                coordinates.Dec,
                pixels,
                pixels,
                arcSecPerPixel);
            var image = await request.Request(ct, progress);

            image.Freeze();
            return new SkySurveyImage() {
                Name = name,
                Source = nameof(SkyServerSkySurvey),
                Image = image,
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }
    }
}