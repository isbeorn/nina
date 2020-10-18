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
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.SkySurvey {

    internal class NASASkySurvey : ISkySurvey {
        private const string Url = "https://skyview.gsfc.nasa.gov/current/cgi/runquery.pl?Survey=dss2r&Position={0},{1}&Size={2}&Pixels={3}&Return=JPG";

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 2;
            fieldOfView = Math.Round(fieldOfView, 2);
            var pixels = Math.Ceiling(Math.Min(Astrometry.Astrometry.ArcminToArcsec(fieldOfView) / arcSecPerPixel, 5000));

            var request = new Http.HttpDownloadImageRequest(
               Url,
               coordinates.RADegrees,
               coordinates.Dec,
               Astrometry.Astrometry.ArcminToDegree(fieldOfView),
               pixels
           );
            var image = await request.Request(ct, progress);
            image.Freeze();

            using (var bmp = NINA.Utility.ImageAnalysis.ImageUtility.BitmapFromSource(image, System.Drawing.Imaging.PixelFormat.Format8bppIndexed)) {
                bmp.Palette = ImageAnalysis.ImageUtility.GetGrayScalePalette();
                ImageStatistics stats = new ImageStatistics(bmp);
                Histogram gray = stats.GrayWithoutBlack;
                new Accord.Imaging.Filters.BrightnessCorrection(Math.Min(115 - gray.Median, 0)).ApplyInPlace(bmp);
                new Accord.Imaging.Filters.ContrastCorrection((int)Math.Round(115 - gray.StdDev * 2)).ApplyInPlace(bmp);
                image = ImageAnalysis.ImageUtility.ConvertBitmap(bmp, System.Windows.Media.PixelFormats.Gray8);
            }

            return new SkySurveyImage() {
                Image = image,
                Name = name,
                Source = nameof(NASASkySurvey),
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }
    }
}