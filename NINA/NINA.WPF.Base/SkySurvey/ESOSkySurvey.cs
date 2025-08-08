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
using NINA.Core.Utility.Http;
using NINA.WPF.Base.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.WPF.Base.SkySurvey {

    public class ESOSkySurvey : MosaicSkySurvey, ISkySurvey {

        public ESOSkySurvey() {
            MaxFoVPerImage = 120;
        }

        private const string Url = "http://archive.eso.org/dss/dss/image?ra={0}&dec={1}&x={2}&y={3}&mime-type=download-gif&Sky-Survey=DSS2&equinox=J2000&statsmode=VO";

        protected override Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct, int width, int height) {
            Task<BitmapSource> image;

            try {
                var request = new HttpDownloadImageRequest(
                    Url,
                    coordinates.RADegrees,
                    coordinates.Dec,
                    fovW,
                    fovH
                );

                image = request.Request(ct);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new SkySurveyUnavailableException(ex.Message);
            }

            return image;
        }
    }
}