using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility {
    class DigitalSkySurveyInteraction {
        public DigitalSkySurveyInteraction(DigitalSkySurveyDomain dom) {
            switch (dom) {
                case DigitalSkySurveyDomain.STSCI: {
                        _sdss = new StsciDigitalSkySurvey();
                        break;
                    }
            }
        }

        private IDigitalSkySurvey _sdss;

        public async Task<BitmapSource> Download(DigitalSkySurveyParameters p, CancellationToken ct) {
            return await _sdss.GetImage(p, ct);
        }
    }

    public interface IDigitalSkySurvey {
        Task<BitmapSource> GetImage(DigitalSkySurveyParameters p, CancellationToken ct);
    }

    /*public class SkyServerDigitalSkySurvey : IDigitalSkySurvey {
        public string Url {
            get {
                return "http://skyserver.sdss.org/dr12/SkyserverWS/ImgCutout/getjpeg?ra={0}&dec={1}&width={2}&height={3}&scale={4}";
            }
        }

        public string GetUrl(DigitalSkySurveyParameters p) {
            return string.Format(
                Url,
                p.RA,
                p.Dec,
                (int)Astrometry.Astrometry.ArcminToArcsec(p.Width * p.Scale),
                (int)Astrometry.Astrometry.ArcminToArcsec(p.Height * p.Scale),
                p.Scale
            );
        }
    }*/

    public class StsciDigitalSkySurvey : IDigitalSkySurvey {
        private const string Url = "https://archive.stsci.edu/cgi-bin/dss_search?format=GIF&r={0}&d={1}&e=J2000&h={2}&w={3}&v=1";

        public async Task<BitmapSource> GetImage(DigitalSkySurveyParameters p, CancellationToken ct) {
            var degrees = Astrometry.Astrometry.ArcminToDegree(p.FoV);
            var maxSingleDegrees = 1.5;

            if (p.FoV > 240) {
                throw new NotImplementedException();
            } else if (degrees > maxSingleDegrees) {

                var nrOfRequiredFrames = Math.Pow(Math.Ceiling(degrees / maxSingleDegrees), 2);
                if (nrOfRequiredFrames == 4) {
                    var p1 = new DigitalSkySurveyParameters() {
                        Coordinates = new Coordinates(
                            p.Coordinates.RADegrees + Astrometry.Astrometry.ArcminToDegree(p.FoV / 4d),
                            p.Coordinates.Dec + Astrometry.Astrometry.ArcminToDegree(p.FoV / 4d),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees),
                        FoV = p.FoV / 2d
                    };

                    var img1 = GetSingleImage(p1, ct);

                    var p2 = new DigitalSkySurveyParameters() {
                        Coordinates = new Coordinates(
                            p.Coordinates.RADegrees - Astrometry.Astrometry.ArcminToDegree(p.FoV / 4d),
                            p.Coordinates.Dec + Astrometry.Astrometry.ArcminToDegree(p.FoV / 4d),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees),
                        FoV = p.FoV / 2d,
                    };

                    var img2 = GetSingleImage(p2, ct);

                    var p3 = new DigitalSkySurveyParameters() {
                        Coordinates = new Coordinates(
                            p.Coordinates.RADegrees + Astrometry.Astrometry.ArcminToDegree(p.FoV / 4d),
                            p.Coordinates.Dec - Astrometry.Astrometry.ArcminToDegree(p.FoV / 4d),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees),
                        FoV = p.FoV / 2d
                    };
                    var img3 = GetSingleImage(p3, ct);

                    var p4 = new DigitalSkySurveyParameters() {
                        Coordinates = new Coordinates(
                            p.Coordinates.RADegrees - Astrometry.Astrometry.ArcminToDegree(p.FoV / 4d),
                            p.Coordinates.Dec - Astrometry.Astrometry.ArcminToDegree(p.FoV / 4d),
                            Epoch.J2000,
                            Coordinates.RAType.Degrees),
                        FoV = p.FoV / 2d
                    };

                    var img4 = GetSingleImage(p4, ct);

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

                    return bmp;

                } else if (nrOfRequiredFrames == 9) {
                    return null;
                }


                //var url = string.Format(Url, p.Coordinates.RADegrees, p.Coordinates.Dec, p.FoV, p.Height);


                return null;
            } else {
                return await GetSingleImage(p, ct);
            }
        }

        private string GetUrl(DigitalSkySurveyParameters p) {
            return string.Format(Url, p.Coordinates.RADegrees, p.Coordinates.Dec, p.FoV, p.FoV);
        }

        private async Task<BitmapSource> GetSingleImage(DigitalSkySurveyParameters p, CancellationToken ct) {
            var url = GetUrl(p);
            return await Utility.HttpGetImage(ct, url);
        }
    }

    public class DigitalSkySurveyParameters {
        public double FoV { get; set; }
        public Coordinates Coordinates { get; set; }
    }

    public enum DigitalSkySurveyDomain {
        STSCI
    }
}
