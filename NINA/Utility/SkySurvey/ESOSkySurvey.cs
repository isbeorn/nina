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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.SkySurvey {

    internal class ESOSkySurvey : MosaicSkySurvey, ISkySurvey {

        public ESOSkySurvey() {
            MaxFoVPerImage = 120;
        }

        private const string Url = "http://archive.eso.org/dss/dss/image?ra={0}&dec={1}&x={2}&y={3}&mime-type=download-gif&Sky-Survey=DSS2&equinox=J2000&statsmode=VO";

        protected override Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct, int width, int height) {
            var request = new Http.HttpDownloadImageRequest(
                Url,
                coordinates.RADegrees,
                coordinates.Dec,
                fovW,
                fovH
            );
            return request.Request(ct);
        }
    }
}