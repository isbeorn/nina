#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.SkySurvey {

    internal class SkyServerSkySurvey : ISkySurvey {
        private const string Url = "http://skyserver.sdss.org/dr14/SkyserverWS/ImgCutout/getjpeg?ra={0}&dec={1}&width={2}&height={3}&scale={4}";

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
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
