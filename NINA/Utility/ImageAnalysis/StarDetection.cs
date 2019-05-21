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

using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using NINA.Model;
using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility.ImageAnalysis {

    internal class StarDetection {
        private static System.Drawing.Pen ELLIPSEPEN = new System.Drawing.Pen(System.Drawing.Brushes.LightYellow, 1);
        private static SolidBrush TEXTBRUSH = new SolidBrush(System.Drawing.Color.Yellow);
        private static System.Drawing.FontFamily FONTFAMILY = new System.Drawing.FontFamily("Arial");
        private static Font FONT = new Font(FONTFAMILY, 32, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);

        public StarDetection(BitmapSource source, ImageArray iarr) {
            _iarr = iarr;
            _originalBitmapSource = source;

            _resizefactor = 1.0;
            if (iarr.Statistics.Width > _maxWidth) {
                _resizefactor = (double)_maxWidth / iarr.Statistics.Width;
            }
            _inverseResizefactor = 1.0 / _resizefactor;

            _minStarSize = (int)Math.Floor(5 * _resizefactor);
            //Prevent Hotpixels to be detected
            if (_minStarSize < 2) {
                _minStarSize = 2;
            }

            _maxStarSize = (int)Math.Ceiling(150 * _resizefactor);
        }

        public StarDetection(BitmapSource source, ImageArray iarr, System.Windows.Media.PixelFormat pf) : this(source, iarr) {
            if (pf == System.Windows.Media.PixelFormats.Rgb48) {
                Bitmap img = new Grayscale(0.2125, 0.7154, 0.0721).Apply(ImageUtility.BitmapFromSource(_originalBitmapSource, System.Drawing.Imaging.PixelFormat.Format48bppRgb));
                _originalBitmapSource = ImageUtility.ConvertBitmap(img, System.Windows.Media.PixelFormats.Gray16);
                _originalBitmapSource.Freeze();
            }
        }

        private int _maxWidth = 1552;
        private int _minStarSize;
        private int _maxStarSize;
        private double _resizefactor;
        private double _inverseResizefactor;
        private ImageArray _iarr;
        private BitmapSource _originalBitmapSource;
        private BlobCounter _blobCounter;
        private Bitmap _bitmapToAnalyze;
        private CancellationToken _token;
        private List<Star> _starlist = new List<Star>();

        public int DetectedStars { get; private set; }
        public double AverageHFR { get; private set; }

        private class Star {
            public double radius;
            public double HFR;
            public AForge.Point Position;
            private List<PixelData> pixelData;
            public double Average { get; private set; } = 0;

            public Rectangle Rectangle;

            public Star() {
                pixelData = new List<PixelData>();
            }

            public void AddPixelData(PixelData value) {
                this.pixelData.Add(value);
            }

            public void CalculateHfr() {
                double hfr = 0.0d;
                if (this.pixelData.Count > 0) {
                    double outerRadius = this.radius;
                    double sum = 0, sumDist = 0, allSum = 0;

                    int centerX = (int)Math.Floor(this.Position.X);
                    int centerY = (int)Math.Floor(this.Position.Y);

                    foreach (PixelData data in this.pixelData) {
                        allSum += data.value;
                        if (InsideCircle(data.PosX, data.PosY, this.Position.X, this.Position.Y, outerRadius)) {
                            if (data.value < 0) {
                                data.value = 0;
                            }

                            sum += data.value;
                            sumDist += data.value * Math.Sqrt(Math.Pow((double)data.PosX - (double)centerX, 2.0d) + Math.Pow((double)data.PosY - (double)centerY, 2.0d));
                        }
                    }

                    if (sum > 0) {
                        hfr = sumDist / sum;
                    } else {
                        hfr = Math.Sqrt(2) * outerRadius;
                    }
                    this.Average = allSum / this.pixelData.Count;
                }
                this.HFR = hfr;
                this.pixelData.Clear();
            }

            private bool InsideCircle(double x, double y, double centerX, double centerY, double radius) {
                return (Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2) <= Math.Pow(radius, 2));
            }
        }

        private class PixelData {
            public int PosX;
            public int PosY;
            public ushort value;

            public override string ToString() {
                return value.ToString();
            }
        }

        public async Task DetectAsync(IProgress<ApplicationStatus> progress, CancellationToken token) {
            _token = token;
            await Task.Run(() => Detect(progress));
        }

        public void Detect(IProgress<ApplicationStatus> progress) {
            try {
                using (MyStopWatch.Measure()) {
                    Stopwatch overall = Stopwatch.StartNew();
                    progress?.Report(new ApplicationStatus() { Status = "Preparing image for star detection" });

                    Stopwatch sw = Stopwatch.StartNew();

                    _bitmapToAnalyze = ImageUtility.Convert16BppTo8Bpp(_originalBitmapSource);

                    Debug.Print("Time to convert to 8bit Image: " + sw.Elapsed);

                    sw.Restart();

                    _token.ThrowIfCancellationRequested();

                    /* Resize to speed up manipulation */
                    ResizeBitmapToAnalyze();

                    /* prepare image for structure detection */
                    PrepareForStructureDetection(_bitmapToAnalyze);

                    progress?.Report(new ApplicationStatus() { Status = "Detecting structures" });

                    /* get structure info */
                    _blobCounter = DetectStructures(_bitmapToAnalyze);

                    progress?.Report(new ApplicationStatus() { Status = "Analyzing stars" });

                    _starlist = IdentifyStars();

                    _token.ThrowIfCancellationRequested();

                    if (_starlist.Count > 0) {
                        var m = (from star in _starlist select star.HFR).Average();
                        Debug.Print("Mean HFR: " + m);
                        //todo change
                        AverageHFR = m;
                        DetectedStars = _starlist.Count;
                    }

                    sw.Stop();
                    sw = null;

                    _blobCounter = null;
                    _bitmapToAnalyze.Dispose();
                    overall.Stop();
                    Debug.Print("Overall star detection: " + overall.Elapsed);
                    overall = null;
                }
            } catch (OperationCanceledException) {
            } finally {
                progress?.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return;
        }

        private List<Star> IdentifyStars() {
            Blob[] blobs = _blobCounter.GetObjectsInformation();
            SimpleShapeChecker checker = new SimpleShapeChecker();
            List<Star> starlist = new List<Star>();

            var avg = blobs.Average((x) => x.Area);
            var sum = blobs.Sum(d => (d.Area - avg) * (d.Area - avg));
            var stdev = Math.Sqrt(sum / blobs.Count());

            foreach (Blob blob in blobs) {
                _token.ThrowIfCancellationRequested();

                if (
                    blob.Area > (avg + 1.5 * stdev)
                    || blob.Rectangle.Width > _maxStarSize
                    || blob.Rectangle.Height > _maxStarSize
                    || blob.Rectangle.Width < _minStarSize
                    || blob.Rectangle.Height < _minStarSize) {
                    continue;
                }
                var points = _blobCounter.GetBlobsEdgePoints(blob);
                AForge.Point centerpoint;
                float radius;
                var rect = new Rectangle((int)Math.Floor(blob.Rectangle.X * _inverseResizefactor), (int)Math.Floor(blob.Rectangle.Y * _inverseResizefactor), (int)Math.Ceiling(blob.Rectangle.Width * _inverseResizefactor), (int)Math.Ceiling(blob.Rectangle.Height * _inverseResizefactor));
                //Star is circle
                Star s;
                if (checker.IsCircle(points, out centerpoint, out radius)) {
                    s = new Star { Position = new AForge.Point(centerpoint.X * (float)_inverseResizefactor, centerpoint.Y * (float)_inverseResizefactor), radius = radius * _inverseResizefactor, Rectangle = rect };
                } else { //Star is elongated
                    var eccentricity = CalculateEccentricity(rect.Width, rect.Height);
                    //Discard highly elliptical shapes.
                    if (eccentricity > 0.8) {
                        continue;
                    }
                    s = new Star { Position = new AForge.Point(centerpoint.X * (float)_inverseResizefactor, centerpoint.Y * (float)_inverseResizefactor), radius = Math.Max(rect.Width, rect.Height) / 2, Rectangle = rect };
                }
                /* get pixeldata */
                for (int x = s.Rectangle.X; x < s.Rectangle.X + s.Rectangle.Width; x++) {
                    for (int y = s.Rectangle.Y; y < s.Rectangle.Y + s.Rectangle.Height; y++) {
                        var value = _iarr.FlatArray[x + (_iarr.Statistics.Width * y)] - _iarr.Statistics.Mean;
                        if (value < 0) { value = 0; }
                        PixelData pd = new PixelData { PosX = x, PosY = y, value = (ushort)value };
                        s.AddPixelData(pd);
                    }
                }
                s.CalculateHfr();
                starlist.Add(s);
            }

            return starlist;
        }

        private double CalculateEccentricity(double width, double height) {
            var x = Math.Max(width, height);
            var y = Math.Min(width, height);
            double focus = Math.Sqrt(Math.Pow(x, 2) - Math.Pow(y, 2));
            return focus / x;
        }

        public BitmapSource GetAnnotatedImage() {
            using (MyStopWatch.Measure()) {
                using (var bmp = ImageUtility.Convert16BppTo8Bpp(_originalBitmapSource)) {
                    using (var newBitmap = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)) {
                        Graphics graphics = Graphics.FromImage(newBitmap);
                        graphics.DrawImage(bmp, 0, 0);

                        if (_starlist.Count > 0) {
                            int r, offset = 10;
                            float textposx, textposy;

                            var threshhold = 200;
                            if (_starlist.Count > threshhold) {
                                _starlist.Sort((item1, item2) => item2.Average.CompareTo(item1.Average));
                                _starlist = _starlist.GetRange(0, threshhold);
                            }

                            foreach (Star star in _starlist) {
                                _token.ThrowIfCancellationRequested();
                                r = (int)Math.Ceiling(star.radius);
                                textposx = star.Position.X - offset;
                                textposy = star.Position.Y - offset;
                                graphics.DrawEllipse(ELLIPSEPEN, new RectangleF(star.Rectangle.X, star.Rectangle.Y, star.Rectangle.Width, star.Rectangle.Height));
                                graphics.DrawString(star.HFR.ToString("##.##"), FONT, TEXTBRUSH, new PointF(Convert.ToSingle(textposx - 1.5 * offset), Convert.ToSingle(textposy + 2.5 * offset)));
                            }
                        }
                        var img = ImageUtility.ConvertBitmap(newBitmap, System.Windows.Media.PixelFormats.Bgr24);

                        img.Freeze();
                        return img;
                    }
                }
            }
        }

        private BlobCounter DetectStructures(Bitmap bmp) {
            var sw = Stopwatch.StartNew();

            /* detect structures */
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.ProcessImage(bmp);

            _token.ThrowIfCancellationRequested();

            sw.Stop();
            Debug.Print("Time for structure detection: " + sw.Elapsed);
            sw = null;

            return blobCounter;
        }

        private void PrepareForStructureDetection(Bitmap bmp) {
            var sw = Stopwatch.StartNew();

            new CannyEdgeDetector().ApplyInPlace(bmp);
            _token.ThrowIfCancellationRequested();
            new SISThreshold().ApplyInPlace(bmp);
            _token.ThrowIfCancellationRequested();
            new BinaryDilatation3x3().ApplyInPlace(bmp);
            _token.ThrowIfCancellationRequested();

            sw.Stop();
            Debug.Print("Time for image preparation: " + sw.Elapsed);
            sw = null;
        }

        private void ResizeBitmapToAnalyze() {
            if (_bitmapToAnalyze.Width > _maxWidth) {
                var bmp = new ResizeBicubic(_maxWidth, (int)Math.Floor(_bitmapToAnalyze.Height * _resizefactor)).Apply(_bitmapToAnalyze);
                _bitmapToAnalyze.Dispose();
                _bitmapToAnalyze = bmp;
            }
        }
    }
}