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
using NINA.Model.ImageData;
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
        private static System.Drawing.Pen RECTPEN = new System.Drawing.Pen(System.Drawing.Brushes.LightYellow, 2);
        private static SolidBrush TEXTBRUSH = new SolidBrush(System.Drawing.Color.Yellow);
        private static System.Drawing.FontFamily FONTFAMILY = new System.Drawing.FontFamily("Arial");
        private static Font FONT = new Font(FONTFAMILY, 32, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);

        public StarDetection(IImageData imageData) {
            _iarr = imageData.Data;
            _originalBitmapSource = imageData.Image;
            statistics = imageData.Statistics;

            _resizefactor = 1.0;
            if (imageData.Statistics.Width > _maxWidth) {
                _resizefactor = (double)_maxWidth / imageData.Statistics.Width;
            }
            _inverseResizefactor = 1.0 / _resizefactor;

            _minStarSize = (int)Math.Floor(5 * _resizefactor);
            //Prevent Hotpixels to be detected
            if (_minStarSize < 2) {
                _minStarSize = 2;
            }

            _maxStarSize = (int)Math.Ceiling(150 * _resizefactor);
        }

        public StarDetection(IImageData imageData, System.Windows.Media.PixelFormat pf) : this(imageData) {
            if (pf == System.Windows.Media.PixelFormats.Rgb48) {
                using (var source = ImageUtility.BitmapFromSource(_originalBitmapSource, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                    using (var img = new Grayscale(0.2125, 0.7154, 0.0721).Apply(source)) {
                        _originalBitmapSource = ImageUtility.ConvertBitmap(img, System.Windows.Media.PixelFormats.Gray16);
                        _originalBitmapSource.Freeze();
                    }
                }
            }
        }

        private int _maxWidth = 1552;
        private int _minStarSize;
        private int _maxStarSize;
        private double _resizefactor;
        private double _inverseResizefactor;
        private IImageArray _iarr;
        private IImageStatistics statistics;
        private BitmapSource _originalBitmapSource;
        private BlobCounter _blobCounter;
        private Bitmap _bitmapToAnalyze;
        private CancellationToken _token;
        private List<Star> _starlist = new List<Star>();
        private List<AForge.Point> _brightestStarPositions = new List<AForge.Point>();
        private int _numberOfAFStars = 0;

        public List<AForge.Point> BrightestStarPositions {
            get {
                return _brightestStarPositions;
            }
            set { 
                _brightestStarPositions = value;
            }
        }

        public int NumberOfAFStars {
            get {
                return _numberOfAFStars;
            } set {
                _numberOfAFStars = value;
            }
        }

        public int DetectedStars { get; private set; }
        public double AverageHFR { get; private set; }
        public double CropRatio { get; set; }

        public bool ignoreImageEdges = false;
        public bool IgnoreImageEdges {
            get { 
                return ignoreImageEdges;
            }
            set { 
            ignoreImageEdges = value;
            } 
        }

        private class Star {
            public double radius;
            public double HFR;
            public AForge.Point Position;
            public double meanBrightness;
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

            internal bool InsideCircle(double x, double y, double centerX, double centerY, double radius) {
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

            double sumRadius = 0;
            double sumSquares = 0;
            foreach (Blob blob in blobs) {
                _token.ThrowIfCancellationRequested();

                if (blob.Rectangle.Width > _maxStarSize
                    || blob.Rectangle.Height > _maxStarSize
                    || blob.Rectangle.Width < _minStarSize
                    || blob.Rectangle.Height < _minStarSize) {
                    continue;
                }

                // If camera cannot subSample, but crop ratio is set, ignore blobs that are too close to the edge
                if (IgnoreImageEdges
                    && (blob.Rectangle.X + blob.Rectangle.Width / 2 < (1 - CropRatio) * _bitmapToAnalyze.Width / 2 
                    || blob.Rectangle.X + blob.Rectangle.Width / 2 > _bitmapToAnalyze.Width * (1 - (1 - CropRatio) / 2)
                    || blob.Rectangle.Y + blob.Rectangle.Height / 2 < (1 - CropRatio) * _bitmapToAnalyze.Height / 2
                    || blob.Rectangle.Y + blob.Rectangle.Height / 2 > _bitmapToAnalyze.Height * (1 - (1 - CropRatio) / 2))) { 
                    continue;
                }
                var points = _blobCounter.GetBlobsEdgePoints(blob);
                AForge.Point centerpoint;
                float radius;
                var rect = new Rectangle((int)Math.Floor(blob.Rectangle.X * _inverseResizefactor), (int)Math.Floor(blob.Rectangle.Y * _inverseResizefactor), (int)Math.Ceiling(blob.Rectangle.Width * _inverseResizefactor), (int)Math.Ceiling(blob.Rectangle.Height * _inverseResizefactor));

                //Build a rectangle that encompasses the blob
                int largeRectXPos = Math.Max(rect.X - rect.Width, 0);
                int largeRectYPos = Math.Max(rect.Y - rect.Height, 0);
                int largeRectWidth = rect.Width * 3;
                if (largeRectXPos + largeRectWidth > statistics.Width) { largeRectWidth = statistics.Width - largeRectXPos; }
                int largeRectHeight = rect.Height * 3;
                if (largeRectYPos + largeRectHeight > statistics.Height) { largeRectHeight = statistics.Height - largeRectYPos; }
                var largeRect = new Rectangle(largeRectXPos, largeRectYPos, largeRectWidth, largeRectHeight);

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
                double starPixelSum = 0;
                int starPixelCount = 0;
                double largeRectPixelSum = 0;

                for (int x = largeRect.X; x < largeRect.X + largeRect.Width; x++) {
                    for (int y = largeRect.Y; y < largeRect.Y + largeRect.Height; y++) {
                        var pixelValue = _iarr.FlatArray[x + (statistics.Width * y)];
                        if (x >= s.Rectangle.X && x < s.Rectangle.X + s.Rectangle.Width && y >= s.Rectangle.Y && y < s.Rectangle.Y + s.Rectangle.Height) { //We're in the small rectangle directly surrounding the star
                            if (s.InsideCircle(x, y, s.Position.X, s.Position.Y, s.radius)) { // We're in the inner sanctum of the star
                                starPixelSum += pixelValue;
                                starPixelCount++;
                            }
                            var value = pixelValue - statistics.Mean;
                            if (value < 0) { value = 0; }
                            PixelData pd = new PixelData { PosX = x, PosY = y, value = (ushort)value };
                            s.AddPixelData(pd);
                        } else { //We're in the larger surrounding holed rectangle, providing local background
                            largeRectPixelSum += pixelValue;
                        }
                    }
                }

                s.meanBrightness = starPixelSum / (double)starPixelCount;
                double largeRectMean = largeRectPixelSum / (double)(largeRect.Height * largeRect.Width - rect.Height * rect.Width);

                if (s.meanBrightness > largeRectMean * 1.1) { //It's a local maximum, so likely to be a star. Let's add it to our star dictionary.
                    sumRadius += s.radius;
                    sumSquares += s.radius * s.radius;
                    s.CalculateHfr();
                    starlist.Add(s);
                }
            }

            //We are performing AF with only a limited number of stars
            if (NumberOfAFStars > 0) {
                //First AF exposure, let's find the brightest star positions and store them
                if (starlist.Count() != 0 && BrightestStarPositions.Count() == 0) {
                    if (starlist.Count() <= NumberOfAFStars) {
                        BrightestStarPositions = starlist.ConvertAll(s => s.Position);
                        return starlist;
                    } else { 
                        starlist = starlist.OrderByDescending(s => s.radius * 0.3 + s.meanBrightness * 0.7).Take(NumberOfAFStars).ToList<Star>();
                        BrightestStarPositions = starlist.ConvertAll(i => i.Position);
                        return starlist;
                    }
                } else { //find the closest stars to the brightest stars previously identified
                    List<Star> topStars = new List<Star>();
                    BrightestStarPositions.ForEach(p => topStars.Add(starlist.Aggregate((min, next) => min.Position.DistanceTo(p) < next.Position.DistanceTo(p) ? min : next)));
                    return topStars;
                }
            }

            //Now that we have a properly filtered star list, let's compute stats and further filter out from the mean
            if (starlist.Count > 0) {
                double avg = sumRadius / (double)starlist.Count();
                double stdev = Math.Sqrt((sumSquares - starlist.Count() * avg * avg) / starlist.Count());
                starlist = starlist.Where(s => s.radius <= avg + 1.5 * stdev && s.radius >= avg - 1.5 * stdev).ToList<Star>();
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

                        if (IgnoreImageEdges) {
                            graphics.DrawRectangle(RECTPEN, (float)(1 - CropRatio) * statistics.Width / 2, (float)(1 - CropRatio) * statistics.Height / 2, (float)CropRatio * statistics.Width, (float)CropRatio * statistics.Height);
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

            new CannyEdgeDetector(10, 80).ApplyInPlace(bmp);
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