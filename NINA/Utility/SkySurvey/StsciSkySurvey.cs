using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NINA.Utility.Astrometry;

namespace NINA.Utility.SkySurvey {

    internal class StsciSkySurvey : ISkySurvey {
        private const string Url = "https://archive.stsci.edu/cgi-bin/dss_search?format=GIF&r={0}&d={1}&e=J2000&h={2}&w={3}&v=1";

        public async Task<SkySurveyImage> GetImage(Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress) {
            var degrees = Astrometry.Astrometry.ArcminToDegree(fieldOfView);
            var maxSingleDegrees = 1.5;

            if (fieldOfView > 240) {
                throw new NotImplementedException();
            } else if (degrees > maxSingleDegrees) {
                var nrOfRequiredFrames = Math.Pow(Math.Ceiling(degrees / maxSingleDegrees), 2);
                if (nrOfRequiredFrames == 4) {
                    var p1Coords = new Coordinates(
                            coordinates.RADegrees + Astrometry.Astrometry.ArcminToDegree(fieldOfView / 4d),
                            coordinates.Dec + Astrometry.Astrometry.ArcminToDegree(fieldOfView / 4d),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees);
                    var p1FoV = fieldOfView / 2d;

                    var img1 = GetSingleImage(p1Coords, p1FoV, ct);

                    var p2Coords = new Coordinates(
                            coordinates.RADegrees - Astrometry.Astrometry.ArcminToDegree(fieldOfView / 4d),
                            coordinates.Dec + Astrometry.Astrometry.ArcminToDegree(fieldOfView / 4d),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees);
                    var p2FoV = fieldOfView / 2d;

                    var img2 = GetSingleImage(p2Coords, p2FoV, ct);

                    var p3Coords = new Coordinates(
                            coordinates.RADegrees + Astrometry.Astrometry.ArcminToDegree(fieldOfView / 4d),
                            coordinates.Dec - Astrometry.Astrometry.ArcminToDegree(fieldOfView / 4d),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees);
                    var p3FoV = fieldOfView / 2d;

                    var img3 = GetSingleImage(p3Coords, p3FoV, ct);

                    var p4Coords = new Coordinates(
                            coordinates.RADegrees - Astrometry.Astrometry.ArcminToDegree(fieldOfView / 4d),
                            coordinates.Dec - Astrometry.Astrometry.ArcminToDegree(fieldOfView / 4d),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees);
                    var p4FoV = fieldOfView / 2d;

                    var img4 = GetSingleImage(p4Coords, p4FoV, ct);

                    await Task.WhenAll(img1, img2, img3, img4);

                    // Gets the size of the images (I assume each image has the same size)
                    int imageWidth = img1.Result.PixelWidth;
                    int imageHeight = img1.Result.PixelHeight;

                    // Draws the images into a DrawingVisual component
                    DrawingVisual drawingVisual = new DrawingVisual();
                    using (DrawingContext drawingContext = drawingVisual.RenderOpen()) {
                        drawingContext.DrawImage(img1.Result, new Rect(0, 0, imageWidth, imageHeight));
                        drawingContext.DrawImage(img2.Result, new Rect(imageWidth, 0, imageWidth, imageHeight));
                        drawingContext.DrawImage(img3.Result, new Rect(0, imageHeight, imageWidth, imageHeight));
                        drawingContext.DrawImage(img4.Result, new Rect(imageWidth, imageHeight, imageWidth, imageHeight));
                    }

                    // Converts the Visual (DrawingVisual) into a BitmapSource
                    RenderTargetBitmap bmp = new RenderTargetBitmap(imageWidth * 2, imageHeight * 2, 96, 96, PixelFormats.Pbgra32);
                    bmp.Render(drawingVisual);

                    return new SkySurveyImage() {
                        Image = bmp,
                        FoVHeight = fieldOfView,
                        FoVWidth = fieldOfView,
                        Rotation = 180
                    };
                } else if (nrOfRequiredFrames == 9) {
                    return null;
                }

                //var url = string.Format(Url, p.Coordinates.RADegrees, p.Coordinates.Dec, fieldOfView, p.Height);

                return null;
            } else {
                var image = await GetSingleImage(coordinates, fieldOfView, ct);
                return new SkySurveyImage() {
                    Image = image,
                    FoVHeight = fieldOfView,
                    FoVWidth = fieldOfView,
                    Rotation = 180
                };
            }
        }

        private string GetUrl(Coordinates coordinates, double fieldOfView) {
            return string.Format(Url, coordinates.RADegrees, coordinates.Dec, fieldOfView, fieldOfView);
        }

        private async Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fieldOfView, CancellationToken ct) {
            var url = GetUrl(coordinates, fieldOfView);
            return await Utility.HttpGetImage(ct, url);
        }
    }
}