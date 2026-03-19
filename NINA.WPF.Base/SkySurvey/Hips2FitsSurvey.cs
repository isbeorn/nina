#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Utility.Http;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.WPF.Base.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.WPF.Base.SkySurvey {

    /// <summary>
    /// Sky Survey using the Hips2Fits service
    /// Description can be found at http://alasky.u-strasbg.fr/hips-image-services/hips2fits
    /// </summary>
    internal class Hips2FitsSurvey : ISkySurvey {

        private const string AltUrl = "https://alasky.u-strasbg.fr/hips-image-services/hips2fits?projection=STG&hips={0}&width={1}&height={2}&fov={3}&ra={4}&dec={5}&format=jpg";
        private const string Url = "https://alaskybis.u-strasbg.fr/hips-image-services/hips2fits?projection=STG&hips={0}&width={1}&height={2}&fov={3}&ra={4}&dec={5}&format=jpg";
        private const string DefaultSkyMapPath = "CDS/P/DSS2/color";

        public async Task<SkySurveyImage> GetImage(string name, string hipsSkyMapPath, Coordinates coordinates, double fieldOfView, int width, int height,
            CancellationToken ct, IProgress<int> progress) {

            fieldOfView = Math.Round(fieldOfView, 2);

            BitmapSource image;
            try {
                image = await QueryImage(Url, coordinates, fieldOfView, ct, progress, hipsSkyMapPath);
            } catch(OperationCanceledException) {
                throw;
            } catch (Exception) {
                try {
                    image = await QueryImage(AltUrl, coordinates, fieldOfView, ct, progress, hipsSkyMapPath);
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception ex) {
                    throw new SkySurveyUnavailableException(ex.Message);
                }
            }

            if (image.DpiX != 96) {
                image = ConvertBitmapTo96DPI(image);
            }
            image.Freeze();

            return new SkySurveyImage() {
                Image = image,
                Name = name,
                Source = nameof(Hips2FitsSurvey),
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }

        private async Task<BitmapSource> QueryImage(string url, Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress, string hipsSkyMapPath = DefaultSkyMapPath) {
            var request = new HttpDownloadImageRequest(
                   url,
                   Uri.EscapeDataString(hipsSkyMapPath),
                   2000,
                   2000,
                   AstroUtil.ArcminToDegree(fieldOfView),
                   coordinates.RADegrees,
                   coordinates.Dec
                );
            return await request.Request(ct, progress);
        }

        private BitmapSource ConvertBitmapTo96DPI(BitmapSource bitmapImage) {
            double dpi = 96;
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            int stride = width * bitmapImage.Format.BitsPerPixel;
            byte[] pixelData = new byte[stride * height];
            bitmapImage.CopyPixels(pixelData, stride, 0);

            return BitmapSource.Create(width, height, dpi, dpi, bitmapImage.Format, null, pixelData, stride);
        }

        public Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width, int height, CancellationToken ct, IProgress<int> progress) {
            throw new NotImplementedException();
        }
    }

}