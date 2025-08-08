#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using Accord.Statistics.Visualizations;
using NINA.Image.ImageData;
using NINA.Core.Utility.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.WPF.Base.Exceptions;
using System.Windows.Media.Imaging;

namespace NINA.WPF.Base.SkySurvey {

    internal class NASASkySurvey : ISkySurvey {
        private const string Url = "https://skyview.gsfc.nasa.gov/current/cgi/runquery.pl?Survey=dss2r&Position={0},{1}&Size={2}&Pixels={3}&Return=JPG";

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 2;
            fieldOfView = Math.Round(fieldOfView, 2);
            var pixels = Math.Ceiling(Math.Min(AstroUtil.ArcminToArcsec(fieldOfView) / arcSecPerPixel, 5000));

            BitmapSource image;

            try {
                var request = new HttpDownloadImageRequest(
                    Url,
                    coordinates.RADegrees,
                    coordinates.Dec,
                    AstroUtil.ArcminToDegree(fieldOfView),
                    pixels
                );

                image = await request.Request(ct, progress);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new SkySurveyUnavailableException(ex.Message);
            }

            image.Freeze();

            using (var bmp = Image.ImageAnalysis.ImageUtility.BitmapFromSource(image, System.Drawing.Imaging.PixelFormat.Format8bppIndexed)) {
                bmp.Palette = Image.ImageAnalysis.ImageUtility.GetGrayScalePalette();
                Accord.Imaging.ImageStatistics stats = new Accord.Imaging.ImageStatistics(bmp);
                Histogram gray = stats.GrayWithoutBlack;
                new Accord.Imaging.Filters.BrightnessCorrection(Math.Min(115 - gray.Median, 0)).ApplyInPlace(bmp);
                new Accord.Imaging.Filters.ContrastCorrection((int)Math.Round(115 - gray.StdDev * 2)).ApplyInPlace(bmp);
                image = Image.ImageAnalysis.ImageUtility.ConvertBitmap(bmp, System.Windows.Media.PixelFormats.Gray8);
                image.Freeze();
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