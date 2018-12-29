#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using AForge;
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

namespace NINA.Utility {

    internal class BahtinovAnalysis {

        public BahtinovAnalysis(BitmapSource source, System.Windows.Media.Color backgroundColor) {
            originalSource = source;
            this.backgroundColor = backgroundColor;
        }

        private System.Windows.Media.Color backgroundColor;

        private BitmapSource originalSource;
        private Bitmap convertedSource;

        public BahtinovImage GrabBahtinov() {
            var bahtinovImage = new BahtinovImage();

            if (originalSource.Format != System.Windows.Media.PixelFormats.Gray8) {
                if (originalSource.Format != System.Windows.Media.PixelFormats.Gray16) {
                    var imgToConvert = ImageAnalysis.BitmapFromSource(originalSource, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    convertedSource = new Grayscale(0.2125, 0.7154, 0.0721).Apply(imgToConvert);
                } else {
                    convertedSource = ImageAnalysis.Convert16BppTo8Bpp(originalSource);
                }
            } else {
                convertedSource = ImageAnalysis.BitmapFromSource(originalSource, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            }

            Bitmap bahtinovedBitmap = new Bitmap(convertedSource.Width, convertedSource.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Graphics graphics = Graphics.FromImage(bahtinovedBitmap);
            graphics.DrawImage(convertedSource, 0, 0);

            /* Apply filters and detection*/
            CannyEdgeDetector filter = new CannyEdgeDetector();
            filter.GaussianSize = 10;
            filter.ApplyInPlace(convertedSource);

            HoughLineTransformation lineTransform = new HoughLineTransformation();
            lineTransform.ProcessImage(convertedSource);

            HoughLine[] lines = lineTransform.GetMostIntensiveLines(6);

            var focusEllipsePen = new System.Drawing.Pen(System.Drawing.Brushes.Green, 1);
            var intersectEllipsePen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 1);
            var mediaColor = backgroundColor;
            var drawingColor = System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
            var linePen = new System.Drawing.Pen(drawingColor, 1);

            List<Line> bahtinovLines = new List<Line>();
            foreach (HoughLine line in lines) {
                var k = TranslateHughLineToLine(line, bahtinovedBitmap.Width, bahtinovedBitmap.Height);
                bahtinovLines.Add(k);
            }

            float x1, x2, y1, y2;

            if (bahtinovLines.Count == 6) {
                var orderedPoints = bahtinovLines.OrderBy(x => 1.0d / x.Slope).ToList();
                var threeLines = new List<Line>();

                for (var i = 0; i < orderedPoints.Count(); i += 2) {
                    var l1 = orderedPoints[i];
                    var l2 = orderedPoints[i + 1];

                    var inter = (l1.Intercept + l2.Intercept) / 2.0f;
                    var slope = (l1.Slope + l2.Slope) / 2.0f;
                    var centerLine = Line.FromSlopeIntercept(slope, inter);
                    threeLines.Add(centerLine);

                    x1 = 0;
                    x2 = convertedSource.Width;
                    y1 = double.IsInfinity(centerLine.Slope) ? centerLine.Intercept : centerLine.Slope + centerLine.Intercept;
                    y2 = double.IsInfinity(centerLine.Slope) ? centerLine.Intercept : (centerLine.Slope * (convertedSource.Width) + centerLine.Intercept);

                    graphics.DrawLine(
                        linePen,
                        new PointF(x1, y1),
                        new PointF(x2, y2));
                }

                /* Intersect outer bahtinov lines */
                var intersection = threeLines[0].GetIntersectionWith(threeLines[2]);
                if (intersection.HasValue) {
                    /* get orthogonale to center line through intersection */
                    var centerBahtinovLine = threeLines[1];
                    var orthogonalSlope = -1.0f / centerBahtinovLine.Slope;
                    var orthogonalIntercept = intersection.Value.Y - orthogonalSlope * intersection.Value.X;

                    var orthogonalCenter = Line.FromSlopeIntercept(orthogonalSlope, orthogonalIntercept);
                    var intersection2 = centerBahtinovLine.GetIntersectionWith(orthogonalCenter);
                    if (intersection2.HasValue && !double.IsInfinity(intersection2.Value.X)) {
                        x1 = intersection.Value.X;
                        y1 = intersection.Value.Y;
                        x2 = intersection2.Value.X;
                        y2 = intersection2.Value.Y;

                        bahtinovImage.Distance = intersection.Value.DistanceTo(intersection2.Value);

                        var t = bahtinovImage.Distance * 4 / bahtinovImage.Distance;
                        var x3 = (float)((1 - t) * x1 + t * x2);
                        var y3 = (float)((1 - t) * y1 + t * y2);

                        var r = 10;
                        graphics.DrawEllipse(
                            intersectEllipsePen,
                            new RectangleF(x3 - r, y3 - r, 2 * r, 2 * r));
                        graphics.DrawEllipse(
                            focusEllipsePen,
                            new RectangleF(x2 - r, y2 - r, 2 * r, 2 * r));

                        graphics.DrawLine(
                            intersectEllipsePen,
                            new PointF(x3, y3),
                            new PointF(x2, y2));
                    }
                }
            }

            var img = ImageAnalysis.ConvertBitmap(bahtinovedBitmap, System.Windows.Media.PixelFormats.Bgr24);
            convertedSource.Dispose();
            bahtinovedBitmap.Dispose();
            img.Freeze();
            bahtinovImage.Image = img;
            return bahtinovImage;
        }

        private Line TranslateHughLineToLine(HoughLine line, int width, int height) {
            // get line's radius and theta values
            int r = line.Radius;
            double t = line.Theta;

            // check if line is in lower part of the image
            if (r < 0) {
                t += 180;
                r = -r;
            }

            // convert degrees to radians
            t = (t / 180) * Math.PI;

            // get image centers (all coordinate are measured relative to center)
            int w2 = width / 2;
            int h2 = height / 2;

            double x0 = 0, x1 = 0, y0 = 0, y1 = 0;

            if (line.Theta != 0) {
                // none-vertical line
                x0 = -w2; // most left point
                x1 = w2;  // most right point

                // calculate corresponding y values
                y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);
            } else {
                // vertical line
                x0 = line.Radius;
                x1 = line.Radius;

                y0 = h2;
                y1 = -h2;
            }

            return
                Line.FromPoints(
                    new IntPoint((int)x0 + w2, h2 - (int)y0),
                    new IntPoint((int)x1 + w2, h2 - (int)y1)
                );
        }
    }

    public class BahtinovImage {
        public BitmapSource Image { get; set; }
        public double Distance { get; set; }
    }

    internal class ImageAnalysis {
        private static System.Drawing.Pen ELLIPSEPEN = new System.Drawing.Pen(System.Drawing.Brushes.LightYellow, 1);
        private static SolidBrush TEXTBRUSH = new SolidBrush(System.Drawing.Color.Yellow);
        private static System.Drawing.FontFamily FONTFAMILY = new System.Drawing.FontFamily("Arial");
        private static Font FONT = new Font(FONTFAMILY, 32, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);

        public ImageAnalysis(BitmapSource source, ImageArray iarr) {
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

        public async Task DetectStarsAsync(IProgress<ApplicationStatus> progress, CancellationToken token) {
            _token = token;
            await Task.Run(() => DetectStars(progress));
        }

        public void DetectStars(IProgress<ApplicationStatus> progress) {
            try {
                using (MyStopWatch.Measure()) {
                    Stopwatch overall = Stopwatch.StartNew();
                    progress?.Report(new ApplicationStatus() { Status = "Preparing image for star detection" });

                    Stopwatch sw = Stopwatch.StartNew();

                    _bitmapToAnalyze = Convert16BppTo8Bpp(_originalBitmapSource);

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
                using (var bmp = Convert16BppTo8Bpp(_originalBitmapSource)) {
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
                        var img = ConvertBitmap(newBitmap, System.Windows.Media.PixelFormats.Bgr24);

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

        public static ColorRemapping16bpp GetColorRemappingFilter(IImageStatistics statistics, double targetHistogramMeanPct, double shadowsClipping) {
            ushort[] map = GetStretchMap(statistics, targetHistogramMeanPct, shadowsClipping);

            var filter = new ColorRemapping16bpp(map);

            return filter;
        }

        /// <summary>
        /// Adjusts x for a given midToneBalance
        /// </summary>
        /// <param name="midToneBalance"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static double MidtonesTransferFunction(double midToneBalance, double x) {
            if (x > 0) {
                if (x < 1) {
                    return (midToneBalance - 1) * x / ((2 * midToneBalance - 1) * x - midToneBalance);
                }
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Converts a value from range [0;65535] to [0;1]
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static double NormalizeUShort(double val) {
            return val / (double)ushort.MaxValue;
        }

        /// <summary>
        /// Converts a value from range [0;1] to [0;65535]
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static ushort DenormalizeUShort(double val) {
            return (ushort)(val * ushort.MaxValue);
        }

        private static ushort[] GetStretchMap(IImageStatistics statistics, double targetHistogramMedianPercent, double shadowsClipping) {
            ushort[] map = new ushort[ushort.MaxValue + 1];

            var normalizedMedian = NormalizeUShort(statistics.Median);
            var normalizedMAD = NormalizeUShort(statistics.MedianAbsoluteDeviation);

            var scaleFactor = 1.4826; // see https://en.wikipedia.org/wiki/Median_absolute_deviation
            var zero = normalizedMedian + shadowsClipping * normalizedMAD * scaleFactor;

            var mtf = MidtonesTransferFunction(targetHistogramMedianPercent, normalizedMedian - zero);

            for (int i = 0; i < map.Length; i++) {
                double value = NormalizeUShort(i);

                map[i] = DenormalizeUShort(MidtonesTransferFunction(mtf, value - zero));
            }

            return map;
        }

        public static BitmapSource ConvertBitmap(System.Drawing.Bitmap bitmap, System.Windows.Media.PixelFormat pf) {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, pf, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        public static Bitmap BitmapFromSource(BitmapSource source) {
            return BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
        }

        public static Bitmap BitmapFromSource(BitmapSource source, System.Drawing.Imaging.PixelFormat pf) {
            Bitmap bmp = new Bitmap(
                    source.PixelWidth,
                    source.PixelHeight,
                    pf);
            BitmapData data = bmp.LockBits(
                    new Rectangle(System.Drawing.Point.Empty, bmp.Size),
                    ImageLockMode.WriteOnly,
                    pf);
            source.CopyPixels(
                    Int32Rect.Empty,
                    data.Scan0,
                    data.Height * data.Stride,
                    data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public static Bitmap Convert16BppTo8Bpp(BitmapSource source) {
            using (var bmp = BitmapFromSource(source)) {
                return AForge.Imaging.Image.Convert16bppTo8bpp(bmp);
            }
        }

        public static BitmapSource Convert16BppTo8BppSource(BitmapSource source) {
            FormatConvertedBitmap s = new FormatConvertedBitmap();
            s.BeginInit();
            s.Source = source;
            s.DestinationFormat = System.Windows.Media.PixelFormats.Gray8;
            s.EndInit();
            s.Freeze();
            return s;
        }

        public static BitmapSource CreateSourceFromArray(ImageArray arr, System.Windows.Media.PixelFormat pf) {
            //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
            int stride = (arr.Statistics.Width * pf.BitsPerPixel + 7) / 8;
            double dpi = 96;

            BitmapSource source = BitmapSource.Create(arr.Statistics.Width, arr.Statistics.Height, dpi, dpi, pf, null, arr.FlatArray, stride);
            source.Freeze();
            return source;
        }

        public static BitmapSource Debayer(BitmapSource source, System.Drawing.Imaging.PixelFormat pf) {
            using (MyStopWatch.Measure()) {
                if (pf == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale) {
                    source = Convert16BppTo8BppSource(source);
                } else if (pf == System.Drawing.Imaging.PixelFormat.Format8bppIndexed) {
                } else {
                    throw new NotSupportedException();
                }
                using (var bmp = BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format8bppIndexed)) {
                    using (var debayeredBmp = Debayer(bmp)) {
                        var newSource = ConvertBitmap(debayeredBmp, PixelFormats.Rgb24);
                        newSource.Freeze();
                        return newSource;
                    }
                }
            }
        }

        public static Bitmap Debayer(Bitmap bmp) {
            using (MyStopWatch.Measure()) {
                var filter = new BayerFilter();
                filter.BayerPattern = new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } };
                var debayered = filter.Apply(bmp);
                return debayered;
            }
        }
    }

    public class ColorRemapping16bpp : ColorRemapping {
        private ushort[] _grayMap16;

        public ushort[] GrayMap16 {
            get { return _grayMap16; }
            set {
                // check the map
                if ((value == null) || (value.Length != 65536)) {
                    throw new ArgumentException("A map should be array with 65536 value.");
                }

                _grayMap16 = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRemapping16bpp"/> class.
        /// </summary>
        /// <param name="grayMap">Gray map.</param>
        /// <remarks>This constructor is supposed for 16bit grayscale images.</remarks>
        public ColorRemapping16bpp(ushort[] grayMap) : base() {
            FormatTranslations[System.Drawing.Imaging.PixelFormat.Format16bppGrayScale] = System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;
            GrayMap16 = grayMap;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// <param name="image">Source image data.</param>
        /// <param name="rect"> Image rectangle for processing by the filter.</param>
        protected override unsafe void ProcessFilter(UnmanagedImage image, Rectangle rect) {
            if (image.PixelFormat != System.Drawing.Imaging.PixelFormat.Format16bppGrayScale) {
                throw new UnsupportedImageFormatException("Source pixel format is not supported by the routine.");
            }

            // processing start and stop X,Y positions
            int stopX = rect.Width;
            int stopY = rect.Height;

            // do the job
            ushort* ptr = (ushort*)image.ImageData.ToPointer();

            // grayscale image
            for (int y = 0; y < stopY; y++) {
                for (int x = 0; x < stopX; x++, ptr++) {
                    // gray
                    *ptr = GrayMap16[*ptr];
                }
            }
        }
    }
}