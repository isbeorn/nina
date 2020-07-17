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
using Accord.Imaging.Filters;
using Accord.Math.Geometry;
using Accord.Statistics.Visualizations;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Utility.Enum;
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

        public StarDetection(IRenderedImage renderedImage, StarSensitivityEnum sensitivity, NoiseReductionEnum noiseReduction) {
            var imageData = renderedImage.RawImageData;
            imageProperties = imageData.Properties;

            // TODO: StarDetection should probably be more of a static function that returns a result type than a stateful object with awaitable methods
            //       Checking the type of rendered image is a hack until then
            _iarr = imageData.Data;
            //If image was debayered, use debayered array for star HFR and local maximum identification
            if (imageProperties.IsBayered && (renderedImage is IDebayeredImage)) {
                var debayeredImage = (IDebayeredImage)renderedImage;
                var debayeredData = debayeredImage.DebayeredData;
                if (debayeredData != null && debayeredData.Lum != null && debayeredData.Lum.Length > 0) {
                    _iarr = new ImageArray(debayeredData.Lum);
                }
            }

            _originalBitmapSource = renderedImage.Image;
            _sensitivity = sensitivity;
            _noiseReduction = noiseReduction;

            _resizefactor = 1.0;
            if (imageProperties.Width > _maxWidth) {
                if (_sensitivity == StarSensitivityEnum.Highest) {
                    _resizefactor = Math.Max(0.625, (double)_maxWidth / imageProperties.Width);
                } else {
                    _resizefactor = (double)_maxWidth / imageProperties.Width;
                }
            }
            _inverseResizefactor = 1.0 / _resizefactor;

            _minStarSize = (int)Math.Floor(5 * _resizefactor);
            //Prevent Hotpixels to be detected
            if (_minStarSize < 2) {
                _minStarSize = 2;
            }

            _maxStarSize = (int)Math.Ceiling(150 * _resizefactor);
        }

        public StarDetection(IRenderedImage renderedImage, System.Windows.Media.PixelFormat pf, StarSensitivityEnum sensitivity, NoiseReductionEnum noiseReduction)
            : this(renderedImage, sensitivity, noiseReduction) {
            if (pf == PixelFormats.Rgb48) {
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
        private ImageProperties imageProperties;
        private BitmapSource _originalBitmapSource;
        private BlobCounter _blobCounter;
        private Bitmap _bitmapToAnalyze;
        private CancellationToken _token;
        private List<Star> _starlist = new List<Star>();
        private List<Accord.Point> _brightestStarPositions = new List<Accord.Point>();
        private int _numberOfAFStars = 0;
        private StarSensitivityEnum _sensitivity = StarSensitivityEnum.Normal;
        private NoiseReductionEnum _noiseReduction = NoiseReductionEnum.None;

        public List<Accord.Point> BrightestStarPositions {
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
            }
            set {
                _numberOfAFStars = value;
            }
        }

        public int DetectedStars { get; private set; }
        public double AverageHFR { get; private set; }
        public double InnerCropRatio { get; set; }
        public bool UseROI { get; set; } = false;
        public double OuterCropRatio { get; set; }
        public double HFRStdDev { get; private set; }
        public double AverageContrast { get; private set; }
        public double ContrastStdev { get; private set; }
        public ContrastDetectionMethodEnum ContrastDetectionMethod { get; set; }

        private class Star {
            public double radius;
            public double HFR;
            public Accord.Point Position;
            public double meanBrightness;
            private List<PixelData> pixelData;
            public double Average { get; private set; } = 0;
            public double SurroundingMean { get; set; } = 0;

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
                    double outerRadius = this.radius * 1.2;
                    double sum = 0, sumDist = 0, allSum = 0;

                    double centerX = this.Position.X;
                    double centerY = this.Position.Y;

                    foreach (PixelData data in this.pixelData) {
                        double value = Math.Round(data.value - SurroundingMean);
                        if (value < 0) {
                            value = 0;
                        }
                        data.value = (ushort)Math.Round(value);

                        allSum += data.value;
                        if (InsideCircle(data.PosX, data.PosY, this.Position.X, this.Position.Y, outerRadius)) {
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

                    /* Perform initial noise reduction on full size image if necessary */
                    if (_noiseReduction != NoiseReductionEnum.None) {
                        ReduceNoise();
                    }

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
                        var s = Math.Sqrt(((from star in _starlist select star.HFR * star.HFR).Sum() - _starlist.Count() * m * m) / _starlist.Count());
                        Debug.Print("Mean HFR: " + m);
                        Debug.Print("HFR Standard Dev: " + s);
                        //todo change
                        AverageHFR = m;
                        HFRStdDev = s;
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

        public async Task MeasureContrastAsync(IProgress<ApplicationStatus> progress, CancellationToken token) {
            _token = token;
            await Task.Run(() => MeasureContrast(progress));
        }

        public void MeasureContrast(IProgress<ApplicationStatus> progress) {
            try {
                using (MyStopWatch.Measure()) {
                    Stopwatch overall = Stopwatch.StartNew();
                    progress?.Report(new ApplicationStatus() { Status = "Preparing image for contrast measurement" });

                    _bitmapToAnalyze = ImageUtility.Convert16BppTo8Bpp(_originalBitmapSource);

                    _token.ThrowIfCancellationRequested();

                    //Crop if there is ROI

                    if (UseROI && InnerCropRatio < 1) {
                        Rectangle cropRectangle = GetCropRectangle(InnerCropRatio);
                        _bitmapToAnalyze = new Crop(cropRectangle).Apply(_bitmapToAnalyze);
                    }

                    if (_noiseReduction == NoiseReductionEnum.Median) {
                        new Median().ApplyInPlace(_bitmapToAnalyze);
                    }

                    //Make sure resizing is independent of Star Sensitivity
                    _resizefactor = (double)_maxWidth / _bitmapToAnalyze.Width;
                    _inverseResizefactor = 1.0 / _resizefactor;

                    /* Resize to speed up manipulation */
                    ResizeBitmapToAnalyze();

                    progress?.Report(new ApplicationStatus() { Status = "Measuring Contrast" });

                    _token.ThrowIfCancellationRequested();

                    if (ContrastDetectionMethod == ContrastDetectionMethodEnum.Laplace) {
                        if (_noiseReduction == NoiseReductionEnum.None || _noiseReduction == NoiseReductionEnum.Median) {
                            int[,] kernel = new int[7, 7];
                            kernel = LaplacianOfGaussianKernel(7, 1.0);
                            new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        } else if (_noiseReduction == NoiseReductionEnum.Normal) {
                            int[,] kernel = new int[9, 9];
                            kernel = LaplacianOfGaussianKernel(9, 1.4);
                            new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        } else if (_noiseReduction == NoiseReductionEnum.High) {
                            int[,] kernel = new int[11, 11];
                            kernel = LaplacianOfGaussianKernel(11, 1.8);
                            new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        } else {
                            int[,] kernel = new int[13, 13];
                            kernel = LaplacianOfGaussianKernel(13, 2.2);
                            new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        }
                        //Get mean and standard dev
                        Accord.Imaging.ImageStatistics stats = new Accord.Imaging.ImageStatistics(_bitmapToAnalyze);
                        AverageContrast = stats.GrayWithoutBlack.Mean;
                        ContrastStdev = 0.01; //Stdev of convoluted image is not a measure of error - using same figure for all
                    } else if (ContrastDetectionMethod == ContrastDetectionMethodEnum.Sobel) {
                        if (_noiseReduction == NoiseReductionEnum.None || _noiseReduction == NoiseReductionEnum.Median) {
                            //Nothing to do
                        } else if (_noiseReduction == NoiseReductionEnum.Normal) {
                            _bitmapToAnalyze = new FastGaussianBlur(_bitmapToAnalyze).Process(1);
                        } else if (_noiseReduction == NoiseReductionEnum.High) {
                            _bitmapToAnalyze = new FastGaussianBlur(_bitmapToAnalyze).Process(2);
                        } else {
                            _bitmapToAnalyze = new FastGaussianBlur(_bitmapToAnalyze).Process(3);
                        }
                        int[,] kernel = {
                            {-1, -2, 0, 2, 1},
                            {-2, -4, 0, 4, 2},
                            {0, 0, 0, 0, 0},
                            {2, 4, 0, -4, -2},
                            {1, 2, 0, -2, -1}
                        };
                        new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        //Get mean and standard dev
                        Accord.Imaging.ImageStatistics stats = new Accord.Imaging.ImageStatistics(_bitmapToAnalyze);
                        AverageContrast = stats.GrayWithoutBlack.Mean;
                        ContrastStdev = 0.01; //Stdev of convoluted image is not a measure of error - using same figure for all
                    }

                    _token.ThrowIfCancellationRequested();

                    _bitmapToAnalyze.Dispose();
                    overall.Stop();
                    Debug.Print("Overall contrast detection: " + overall.Elapsed);
                    overall = null;
                }
            } catch (OperationCanceledException) {
            } finally {
                progress?.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return;
        }

        private Rectangle GetCropRectangle(double cropRatio) {
            int xcoord = (int)Math.Floor((_bitmapToAnalyze.Width - _bitmapToAnalyze.Width * cropRatio) / 2d);
            int ycoord = (int)Math.Floor((_bitmapToAnalyze.Height - _bitmapToAnalyze.Height * cropRatio) / 2d);
            int width = (int)Math.Floor(_bitmapToAnalyze.Width * cropRatio);
            int height = (int)Math.Floor(_bitmapToAnalyze.Height * cropRatio);
            return new Rectangle(xcoord, ycoord, width, height);
        }

        private double LaplacianOfGaussianFunction(double x, double y, double sigma) {
            double result = -1 * 1 / (Math.PI * Math.Pow(sigma, 4)) * (1 - ((x * x + y * y) / (2 * sigma * sigma))) * Math.Exp(-1 * (x * x + y * y) / (2 * sigma * sigma)) * 600;
            return result;
        }

        private int[,] LaplacianOfGaussianKernel(int size, double sigma) {
            int[,] LoGKernel = new int[size, size];
            int halfsize = size / 2;
            int sumKernel = 0;
            for (int x = -halfsize; x < halfsize + 1; x++) {
                for (int y = -halfsize; y < halfsize + 1; y++) {
                    int value = (int)Math.Round(LaplacianOfGaussianFunction(x, y, sigma));
                    LoGKernel[x + halfsize, y + halfsize] = value;
                    sumKernel += value;
                }
            }
            LoGKernel[halfsize, halfsize] = LoGKernel[halfsize, halfsize] - sumKernel;
            return LoGKernel;
        }

        private bool InROI(Blob blob) {
            if (OuterCropRatio == 1 && (blob.Rectangle.X + blob.Rectangle.Width / 2 > (1 - InnerCropRatio) * _bitmapToAnalyze.Width / 2
                && blob.Rectangle.X + blob.Rectangle.Width / 2 < _bitmapToAnalyze.Width * (1 - (1 - InnerCropRatio) / 2)
                && blob.Rectangle.Y + blob.Rectangle.Height / 2 > (1 - InnerCropRatio) * _bitmapToAnalyze.Height / 2
                && blob.Rectangle.Y + blob.Rectangle.Height / 2 < _bitmapToAnalyze.Height * (1 - (1 - InnerCropRatio) / 2))) {
                return true;
            }
            if (OuterCropRatio < 1 && (blob.Rectangle.X + blob.Rectangle.Width / 2 < (1 - InnerCropRatio) * _bitmapToAnalyze.Width / 2
                || blob.Rectangle.X + blob.Rectangle.Width / 2 > _bitmapToAnalyze.Width * (1 - (1 - InnerCropRatio) / 2)
                || blob.Rectangle.Y + blob.Rectangle.Height / 2 < (1 - InnerCropRatio) * _bitmapToAnalyze.Height / 2
                || blob.Rectangle.Y + blob.Rectangle.Height / 2 > _bitmapToAnalyze.Height * (1 - (1 - InnerCropRatio) / 2)) &&
                (blob.Rectangle.X + blob.Rectangle.Width / 2 > (1 - OuterCropRatio) * _bitmapToAnalyze.Width / 2
                && blob.Rectangle.X + blob.Rectangle.Width / 2 < _bitmapToAnalyze.Width * (1 - (1 - OuterCropRatio) / 2)
                && blob.Rectangle.Y + blob.Rectangle.Height / 2 > (1 - OuterCropRatio) * _bitmapToAnalyze.Height / 2
                && blob.Rectangle.Y + blob.Rectangle.Height / 2 < _bitmapToAnalyze.Height * (1 - (1 - OuterCropRatio) / 2))) {
                return true;
            }
            return false;
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

                // If camera cannot subSample, but crop ratio is set, use blobs that are either within or without the ROI
                if (UseROI && !InROI(blob)) {
                    continue;
                }

                var points = _blobCounter.GetBlobsEdgePoints(blob);
                Accord.Point centerpoint;
                float radius;
                var rect = new Rectangle((int)Math.Floor(blob.Rectangle.X * _inverseResizefactor), (int)Math.Floor(blob.Rectangle.Y * _inverseResizefactor), (int)Math.Ceiling(blob.Rectangle.Width * _inverseResizefactor), (int)Math.Ceiling(blob.Rectangle.Height * _inverseResizefactor));

                //Build a rectangle that encompasses the blob
                int largeRectXPos = Math.Max(rect.X - rect.Width, 0);
                int largeRectYPos = Math.Max(rect.Y - rect.Height, 0);
                int largeRectWidth = rect.Width * 3;
                if (largeRectXPos + largeRectWidth > imageProperties.Width) { largeRectWidth = imageProperties.Width - largeRectXPos; }
                int largeRectHeight = rect.Height * 3;
                if (largeRectYPos + largeRectHeight > imageProperties.Height) { largeRectHeight = imageProperties.Height - largeRectYPos; }
                var largeRect = new Rectangle(largeRectXPos, largeRectYPos, largeRectWidth, largeRectHeight);

                //Star is circle
                Star s;
                if (checker.IsCircle(points, out centerpoint, out radius)) {
                    s = new Star { Position = new Accord.Point(centerpoint.X * (float)_inverseResizefactor, centerpoint.Y * (float)_inverseResizefactor), radius = radius * _inverseResizefactor, Rectangle = rect };
                } else { //Star is elongated
                    var eccentricity = CalculateEccentricity(rect.Width, rect.Height);
                    //Discard highly elliptical shapes.
                    if (eccentricity > 0.8) {
                        continue;
                    }
                    s = new Star { Position = new Accord.Point(centerpoint.X * (float)_inverseResizefactor, centerpoint.Y * (float)_inverseResizefactor), radius = Math.Max(rect.Width, rect.Height) / 2, Rectangle = rect };
                }

                /* get pixeldata */
                double starPixelSum = 0;
                int starPixelCount = 0;
                double largeRectPixelSum = 0;
                double largeRectPixelSumSquares = 0;
                List<ushort> innerStarPixelValues = new List<ushort>();

                for (int x = largeRect.X; x < largeRect.X + largeRect.Width; x++) {
                    for (int y = largeRect.Y; y < largeRect.Y + largeRect.Height; y++) {
                        var pixelValue = _iarr.FlatArray[x + (imageProperties.Width * y)];
                        if (x >= s.Rectangle.X && x < s.Rectangle.X + s.Rectangle.Width && y >= s.Rectangle.Y && y < s.Rectangle.Y + s.Rectangle.Height) { //We're in the small rectangle directly surrounding the star
                            if (s.InsideCircle(x, y, s.Position.X, s.Position.Y, s.radius)) { // We're in the inner sanctum of the star
                                starPixelSum += pixelValue;
                                starPixelCount++;
                                innerStarPixelValues.Add(pixelValue);
                            }
                            ushort value = pixelValue;
                            PixelData pd = new PixelData { PosX = x, PosY = y, value = (ushort)value };
                            s.AddPixelData(pd);
                        } else { //We're in the larger surrounding holed rectangle, providing local background
                            largeRectPixelSum += pixelValue;
                            largeRectPixelSumSquares += pixelValue * pixelValue;
                        }
                    }
                }

                s.meanBrightness = starPixelSum / (double)starPixelCount;
                double largeRectPixelCount = largeRect.Height * largeRect.Width - rect.Height * rect.Width;
                double largeRectMean = largeRectPixelSum / largeRectPixelCount;
                s.SurroundingMean = largeRectMean;
                double largeRectStdev = Math.Sqrt((largeRectPixelSumSquares - largeRectPixelCount * largeRectMean * largeRectMean) / largeRectPixelCount);
                int minimumNumberOfPixels = (int)Math.Ceiling(Math.Max(_originalBitmapSource.PixelWidth, _originalBitmapSource.PixelHeight) / 1000d);

                if (s.meanBrightness >= largeRectMean + Math.Min(0.1 * largeRectMean, largeRectStdev) && innerStarPixelValues.Count(pv => pv > largeRectMean + 1.5 * largeRectStdev) > minimumNumberOfPixels) { //It's a local maximum, and has enough bright pixels, so likely to be a star. Let's add it to our star dictionary.
                    sumRadius += s.radius;
                    sumSquares += s.radius * s.radius;
                    s.CalculateHfr();
                    starlist.Add(s);
                }
            }

            // No stars could be found. Return.
            if (starlist.Count() == 0) {
                return starlist;
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
                if (_sensitivity == StarSensitivityEnum.Normal) {
                    starlist = starlist.Where(s => s.radius <= avg + 1.5 * stdev && s.radius >= avg - 1.5 * stdev).ToList<Star>();
                } else {
                    //More sensitivity means getting fainter and smaller stars, and maybe some noise, skewing the distribution towards low radius. Let's be more permissive towards the large star end.
                    starlist = starlist.Where(s => s.radius <= avg + 2 * stdev && s.radius >= avg - 1.5 * stdev).ToList<Star>();
                }
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

                        if (UseROI) {
                            graphics.DrawRectangle(RECTPEN, (float)(1 - InnerCropRatio) * imageProperties.Width / 2, (float)(1 - InnerCropRatio) * imageProperties.Height / 2, (float)InnerCropRatio * imageProperties.Width, (float)InnerCropRatio * imageProperties.Height);
                            if (OuterCropRatio < 1) {
                                graphics.DrawRectangle(RECTPEN, (float)(1 - OuterCropRatio) * imageProperties.Width / 2, (float)(1 - OuterCropRatio) * imageProperties.Height / 2, (float)OuterCropRatio * imageProperties.Width, (float)OuterCropRatio * imageProperties.Height);
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

            if (_sensitivity == StarSensitivityEnum.Normal) {
                if (_noiseReduction == NoiseReductionEnum.None || _noiseReduction == NoiseReductionEnum.Median) {
                    //Still need to apply Gaussian blur, using normal Canny
                    new CannyEdgeDetector(10, 80).ApplyInPlace(bmp);
                } else {
                    //Gaussian blur already applied, using no-blur Canny
                    new NoBlurCannyEdgeDetector(10, 80).ApplyInPlace(bmp);
                }
            } else {
                int kernelSize = (int)Math.Max(Math.Floor(Math.Max(_originalBitmapSource.PixelWidth, _originalBitmapSource.PixelHeight) * _resizefactor / 500), 3);
                //Apply blur or sharpen operation prior to applying the Canny Edge Detector
                if (_inverseResizefactor > 1.6) {
                    //Strong blur occurred while resizing, apply fairly strong Gaussian Sharpen
                    new GaussianSharpen(1.8, kernelSize).ApplyInPlace(bmp);
                } else if (_inverseResizefactor > 1) {
                    //Some blur occurred during resizing, apply Gaussian Sharpen with relative strength proportional to resize factor
                    double sigma = (_inverseResizefactor - 1) * 3;
                    new GaussianSharpen(sigma, kernelSize).ApplyInPlace(bmp);
                } else {
                    if (_noiseReduction == NoiseReductionEnum.None || _noiseReduction == NoiseReductionEnum.Median) {
                        //No resizing or gaussian blur occurred, apply weak Gaussian blur
                        new GaussianBlur(0.7, 5).ApplyInPlace(bmp);
                    } else {
                        //Gaussian blur already occurred, do nothing
                    }
                }
                _token.ThrowIfCancellationRequested();
                new NoBlurCannyEdgeDetector(10, 80).ApplyInPlace(bmp);
            }
            _token.ThrowIfCancellationRequested();
            new SISThreshold().ApplyInPlace(bmp);
            _token.ThrowIfCancellationRequested();
            new BinaryDilation3x3().ApplyInPlace(bmp);
            _token.ThrowIfCancellationRequested();

            sw.Stop();
            Debug.Print("Time for image preparation: " + sw.Elapsed);
            sw = null;
        }

        private void ResizeBitmapToAnalyze() {
            if (_bitmapToAnalyze.Width > _maxWidth) {
                var bmp = new ResizeBicubic((int)Math.Floor(_bitmapToAnalyze.Width * _resizefactor), (int)Math.Floor(_bitmapToAnalyze.Height * _resizefactor)).Apply(_bitmapToAnalyze);
                _bitmapToAnalyze.Dispose();
                _bitmapToAnalyze = bmp;
            }
        }

        private void ReduceNoise() {
            var sw = Stopwatch.StartNew();
            if (_bitmapToAnalyze.Width > _maxWidth) {
                Bitmap bmp;
                switch (_noiseReduction) {
                    case NoiseReductionEnum.High:
                        bmp = new FastGaussianBlur(_bitmapToAnalyze).Process(2);
                        break;

                    case NoiseReductionEnum.Highest:
                        bmp = new FastGaussianBlur(_bitmapToAnalyze).Process(3);
                        break;

                    case NoiseReductionEnum.Median:
                        bmp = new Median().Apply(_bitmapToAnalyze);
                        break;

                    default:
                        bmp = new FastGaussianBlur(_bitmapToAnalyze).Process(1);
                        break;
                }
                _bitmapToAnalyze.Dispose();
                _bitmapToAnalyze = bmp;
                sw.Stop();
                Debug.Print("Time for noise reduction: " + sw.Elapsed);
                sw = null;
            }
        }
    }
}