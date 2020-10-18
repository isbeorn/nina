#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using Accord.Statistics.Visualizations;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.SkySurvey {

    /// <summary>
    /// Sky Survey using the Hips2Fits service
    /// Description can be found at http://alasky.u-strasbg.fr/hips-image-services/hips2fits
    /// </summary>
    internal class Hips2FitsSurvey : ISkySurvey {
        private const string Url = "http://alaskybis.u-strasbg.fr/hips-image-services/hips2fits?projection=STG&hips=CDS%2FP%2FDSS2%2Fcolor&width={0}&height={1}&fov={2}&ra={3}&dec={4}&format=jpg";

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            fieldOfView = Math.Round(fieldOfView, 2);

            var request = new Http.HttpDownloadImageRequest(
               Url,
               2000,
               2000,
               Astrometry.Astrometry.ArcminToDegree(fieldOfView),
               coordinates.RADegrees,
               coordinates.Dec
           );
            var image = await request.Request(ct, progress);
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

        private BitmapSource ConvertBitmapTo96DPI(BitmapSource bitmapImage) {
            double dpi = 96;
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            int stride = width * bitmapImage.Format.BitsPerPixel;
            byte[] pixelData = new byte[stride * height];
            bitmapImage.CopyPixels(pixelData, stride, 0);

            return BitmapSource.Create(width, height, dpi, dpi, bitmapImage.Format, null, pixelData, stride);
        }
    }
}