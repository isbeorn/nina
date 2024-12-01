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
using Accord.Imaging.Filters;
using Accord.Math.Geometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = Accord.Point;

namespace NINA.Image.ImageAnalysis {

    public class StarDetection : IStarDetection {
        private static int _maxWidth = 1552;

        public string Name => "NINA";

        public string ContentId => this.GetType().FullName;

        private class State {
            public IImageArray _iarr;
            public ImageProperties imageProperties;
            public BitmapSource _originalBitmapSource;
            public double _resizefactor;
            public double _inverseResizefactor;
            public int _minStarSize;
            public int _maxStarSize;
        }

        private static State GetInitialState(IRenderedImage renderedImage, System.Windows.Media.PixelFormat pf, StarDetectionParams p) {
            var state = new State();
            var imageData = renderedImage.RawImageData;
            state.imageProperties = imageData.Properties;

            state._iarr = imageData.Data;
            //If image was debayered, use debayered array for star HFR and local maximum identification
            if (state.imageProperties.IsBayered && (renderedImage is IDebayeredImage)) {
                var debayeredImage = (IDebayeredImage)renderedImage;
                var debayeredData = debayeredImage.DebayeredData;
                if (debayeredData != null && debayeredData.Lum != null && debayeredData.Lum.Length > 0) {
                    state._iarr = new ImageArray(debayeredData.Lum);
                }
            }

            state._originalBitmapSource = renderedImage.Image;
            state._resizefactor = 1.0;
            if (state.imageProperties.Width > _maxWidth) {
                if (p.Sensitivity == StarSensitivityEnum.Highest) {
                    state._resizefactor = Math.Max(2 / 3d, (double)_maxWidth / state.imageProperties.Width);
                } else if (p.Sensitivity == StarSensitivityEnum.High) {
                    // When setting the sensitivity to high the algorithm will rescale smarter by adjusting the scale based on the image scale based on focal length and pixel size
                    // This will result in less resizing compared to normal which will take a bit longer, but should yield a much better detection result
                    var pixelSize = renderedImage.RawImageData.MetaData.Camera.PixelSize;
                    var fl = renderedImage.RawImageData.MetaData.Telescope.FocalLength;
                    var imageScale = Astrometry.AstroUtil.ArcsecPerPixel(pixelSize, fl);

                    if (double.IsNaN(imageScale)) {
                        Logger.Warning("Image scale could not be determined for star detection");
                        state._resizefactor = Math.Max(1 / 2d, (double)_maxWidth / state.imageProperties.Width);
                    } else {
                        state._resizefactor = 1.0;
                        if (imageScale < 0.5) {
                            state._resizefactor = 1 / 4d;
                        }
                        if (imageScale >= 0.5 && imageScale <= 1.5) {
                            state._resizefactor = 1 / 3d;
                        }
                        if (imageScale >= 1.5 && imageScale <= 2.5) {
                            state._resizefactor = 1 / 2d;
                        }
                        if (imageScale > 1.5) {
                            state._resizefactor = Math.Max(2 / 3d, (double)_maxWidth / state.imageProperties.Width);
                        }
                    }
                } else {
                    state._resizefactor = (double)_maxWidth / state.imageProperties.Width;
                }
            }
            state._inverseResizefactor = 1.0 / state._resizefactor;

            state._minStarSize = (int)Math.Floor(5 * state._resizefactor);
            //Prevent Hotpixels to be detected
            if (state._minStarSize < 2) {
                state._minStarSize = 2;
            }

            state._maxStarSize = (int)Math.Ceiling(150 * state._resizefactor);
            if (pf == PixelFormats.Rgb48) {
                using (var source = ImageUtility.BitmapFromSource(state._originalBitmapSource, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                    using (var img = new Grayscale(0.2125, 0.7154, 0.0721).Apply(source)) {
                        state._originalBitmapSource = ImageUtility.ConvertBitmap(img, System.Windows.Media.PixelFormats.Gray16);
                        state._originalBitmapSource.Freeze();
                    }
                }
            }

            Logger.Trace($"Star Detection initialized using resizeFactor: {state._resizefactor}, minimum star size: {state._minStarSize}");

            return state;
        }

        public class Star {
            public double Radius { get; init; }
            public Rectangle Rectangle { get; init; }
            public double MeanBrightness { get; set; }
            public double SurroundingMean { get; set; }
            public double MaxPixelValue { get; set; }
            public Point Position { get;set; }
            public double HFR { get; private set; }
            public double Average { get; private set; } 

            public void Calculate(List<PixelData> pixelData) {
                double hfr = 0.0d;
                if (pixelData.Count > 0) {
                    double outerRadius = this.Radius * 1.2;
                    double sum = 0, sumDist = 0, allSum = 0, sumValX = 0, sumValY = 0;

                    double centerX = this.Position.X;
                    double centerY = this.Position.Y;

                    foreach (PixelData data in pixelData) {
                        double value = Math.Round(data.Value - SurroundingMean);
                        if (value < 0) {
                            value = 0;
                        }

                        allSum += value;
                        if (InsideCircle(data.PosX, data.PosY, this.Position.X, this.Position.Y, outerRadius)) {
                            sum += value;
                            sumDist += value * Math.Sqrt(Math.Pow(data.PosX - centerX, 2.0d) + Math.Pow(data.PosY - centerY, 2.0d));
                            sumValX += (data.PosX - Rectangle.X) * value;
                            sumValY += (data.PosY - Rectangle.Y) * value;
                        }
                    }

                    if (sum > 0) {
                        hfr = sumDist / sum;
                    } else {
                        hfr = Math.Sqrt(2) * outerRadius;
                    }
                    this.Average = allSum / pixelData.Count;

                    if (sum != 0) {
                        // Update the centroid
                        double centroidX = sumValX / sum;
                        double centroidY = sumValY / sum;
                        this.Position = new Point((float)centroidX + Rectangle.X, (float)centroidY + Rectangle.Y);
                    }
                }
                this.HFR = hfr;
            }

            internal bool InsideCircle(double x, double y, double centerX, double centerY, double radius) {
                return (Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2) <= Math.Pow(radius, 2));
            }

            public DetectedStar ToDetectedStar() {
                return new DetectedStar() {
                    HFR = HFR,
                    Position = Position,
                    AverageBrightness = Average,
                    MaxBrightness = MaxPixelValue,
                    Background = SurroundingMean,
                    BoundingBox = Rectangle
                };
            }
        }

        public record PixelData (int PosX, int PosY, double Value);

        public async Task<StarDetectionResult> Detect(IRenderedImage image, PixelFormat pf, StarDetectionParams p, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var result = new StarDetectionResult();
            Bitmap bitmapToAnalyze = null;
            try {
                using (MyStopWatch.Measure()) {
                    progress?.Report(new ApplicationStatus() { Status = "Preparing image for star detection" });

                    var state = GetInitialState(image, pf, p);
                    bitmapToAnalyze = ImageUtility.Convert16BppTo8Bpp(state._originalBitmapSource);

                    token.ThrowIfCancellationRequested();

                    /* Perform initial noise reduction on full size image if necessary */
                    if (p.NoiseReduction != NoiseReductionEnum.None) {
                        bitmapToAnalyze = ReduceNoise(bitmapToAnalyze, p);
                    }

                    /* Resize to speed up manipulation */
                    bitmapToAnalyze = DetectionUtility.ResizeForDetection(bitmapToAnalyze, _maxWidth, state._resizefactor);

                    /* prepare image for structure detection */
                    PrepareForStructureDetection(bitmapToAnalyze, p, token);

                    progress?.Report(new ApplicationStatus() { Status = "Detecting structures" });

                    /* get structure info */
                    var blobCounter = DetectStructures(bitmapToAnalyze, token);

                    progress?.Report(new ApplicationStatus() { Status = "Analyzing stars" });

                    result.StarList = IdentifyStars(p, state, blobCounter, bitmapToAnalyze, result, token, out var detectedStars);

                    token.ThrowIfCancellationRequested();

                    if (result.StarList.Count > 0) {
                        var mean = (from star in result.StarList select star.HFR).Average();
                        var stdDev = 0d;
                        if (result.StarList.Count > 1) {
                            stdDev = Math.Sqrt((from star in result.StarList select (star.HFR - mean) * (star.HFR - mean)).Sum() / (result.StarList.Count - 1));
                        }

                        Logger.Info($"Average HFR: {mean}, HFR σ: {stdDev}, Detected Stars {detectedStars}, Sensitivity {p.Sensitivity}, ResizeFactor: {Math.Round(state._resizefactor, 2)}");

                        result.AverageHFR = mean;
                        result.HFRStdDev = stdDev;
                        result.DetectedStars = detectedStars;
                    }
                }
            } catch (OperationCanceledException) {
            } finally {
                progress?.Report(new ApplicationStatus() { Status = string.Empty });
                bitmapToAnalyze?.Dispose();
            }
            return result;
        }

        private bool InROI(Bitmap bitmapToAnalyze, StarDetectionParams p, Blob blob) {
            return DetectionUtility.InROI(
                new Size(width: bitmapToAnalyze.Width, height: bitmapToAnalyze.Height),
                blob: blob.Rectangle,
                outerCropRatio: p.OuterCropRatio,
                innerCropRatio: p.InnerCropRatio);
        }

        private List<DetectedStar> IdentifyStars(StarDetectionParams p, State state, BlobCounter blobCounter, Bitmap bitmapToAnalyze, StarDetectionResult result, CancellationToken token, out int detectedStars) {
            using (MyStopWatch.Measure()) {
                detectedStars = 0;
                Blob[] blobs = blobCounter.GetObjectsInformation();
                SimpleShapeChecker checker = new SimpleShapeChecker();
                List<Star> starList = new List<Star>();
                double sumRadius = 0;
                double sumSquares = 0;
                foreach (Blob blob in blobs) {
                    token.ThrowIfCancellationRequested();

                    if (blob.Rectangle.Width > state._maxStarSize
                        || blob.Rectangle.Height > state._maxStarSize
                        || blob.Rectangle.Width < state._minStarSize
                        || blob.Rectangle.Height < state._minStarSize) {
                        continue;
                    }

                    // If camera cannot subSample, but crop ratio is set, use blobs that are either within or without the ROI
                    if (p.UseROI && !InROI(bitmapToAnalyze, p, blob)) {
                        continue;
                    }

                    var points = blobCounter.GetBlobsEdgePoints(blob);
                    var rect = new Rectangle((int)Math.Floor(blob.Rectangle.X * state._inverseResizefactor), (int)Math.Floor(blob.Rectangle.Y * state._inverseResizefactor), (int)Math.Ceiling(blob.Rectangle.Width * state._inverseResizefactor), (int)Math.Ceiling(blob.Rectangle.Height * state._inverseResizefactor));

                    //Build a rectangle that encompasses the blob
                    int largeRectXPos = Math.Max(rect.X - rect.Width, 0);
                    int largeRectYPos = Math.Max(rect.Y - rect.Height, 0);
                    int largeRectWidth = rect.Width * 3;
                    if (largeRectXPos + largeRectWidth > state.imageProperties.Width) { largeRectWidth = state.imageProperties.Width - largeRectXPos; }
                    int largeRectHeight = rect.Height * 3;
                    if (largeRectYPos + largeRectHeight > state.imageProperties.Height) { largeRectHeight = state.imageProperties.Height - largeRectYPos; }
                    var largeRect = new Rectangle(largeRectXPos, largeRectYPos, largeRectWidth, largeRectHeight);

                    //Star is circle
                    Star s;
                    if (checker.IsCircle(points, out Point centerPoint, out float radius)) {
                        s = new Star { Position = new Point(centerPoint.X * (float)state._inverseResizefactor, centerPoint.Y * (float)state._inverseResizefactor), Radius = radius * state._inverseResizefactor, Rectangle = rect };
                    } else { //Star is elongated
                        var eccentricity = CalculateEccentricity(rect.Width, rect.Height);
                        //Discard highly elliptical shapes.
                        if (eccentricity > 0.8) {
                            continue;
                        }
                        s = new Star { Position = new Point(centerPoint.X * (float)state._inverseResizefactor, centerPoint.Y * (float)state._inverseResizefactor), Radius = Math.Max(rect.Width, rect.Height) / 2, Rectangle = rect };
                    }

                    /* get pixeldata */
                    double starPixelSum = 0;
                    int starPixelCount = 0;
                    double largeRectPixelSum = 0;
                    double largeRectPixelSumSquares = 0;
                    List<ushort> innerStarPixelValues = new List<ushort>();

                    var pixelDataList = new List<PixelData>();
                    for (int x = largeRect.X; x < largeRect.X + largeRect.Width; x++) {
                        for (int y = largeRect.Y; y < largeRect.Y + largeRect.Height; y++) {
                            var pixelValue = state._iarr.FlatArray[x + (state.imageProperties.Width * y)];
                            if (x >= s.Rectangle.X && x < s.Rectangle.X + s.Rectangle.Width && y >= s.Rectangle.Y && y < s.Rectangle.Y + s.Rectangle.Height) { //We're in the small rectangle directly surrounding the star
                                if (s.InsideCircle(x, y, s.Position.X, s.Position.Y, s.Radius)) { // We're in the inner sanctum of the star
                                    starPixelSum += pixelValue;
                                    starPixelCount++;
                                    innerStarPixelValues.Add(pixelValue);
                                    s.MaxPixelValue = Math.Max(s.MaxPixelValue, pixelValue);
                                }
                                ushort value = pixelValue;
                                var pd = new PixelData(PosX: x, PosY: y, Value: value);
                                pixelDataList.Add(pd);
                            } else { //We're in the larger surrounding holed rectangle, providing local background
                                largeRectPixelSum += pixelValue;
                                largeRectPixelSumSquares += pixelValue * pixelValue;
                            }
                        }
                    }

                    s.MeanBrightness = starPixelSum / (double)starPixelCount;
                    double largeRectPixelCount = largeRect.Height * largeRect.Width - rect.Height * rect.Width;
                    double largeRectMean = largeRectPixelSum / largeRectPixelCount;
                    s.SurroundingMean = largeRectMean;
                    double largeRectStdev = Math.Sqrt((largeRectPixelSumSquares - largeRectPixelCount * largeRectMean * largeRectMean) / largeRectPixelCount);
                    int minimumNumberOfPixels = (int)Math.Ceiling(Math.Max(state._originalBitmapSource.PixelWidth, state._originalBitmapSource.PixelHeight) / 1000d);

                    if (s.MeanBrightness >= largeRectMean + Math.Min(0.1 * largeRectMean, largeRectStdev) && innerStarPixelValues.Count(pv => pv > largeRectMean + 1.5 * largeRectStdev) > minimumNumberOfPixels) { //It's a local maximum, and has enough bright pixels, so likely to be a star. Let's add it to our star dictionary.
                        sumRadius += s.Radius;
                        sumSquares += s.Radius * s.Radius;
                        s.Calculate(pixelDataList);
                        starList.Add(s);
                    }
                }

                // No stars could be found. Return.
                if (starList.Count == 0) {
                    return new List<DetectedStar>();
                }

                //Now that we have a properly filtered star list, let's compute stats and further filter out from the mean
                if (starList.Count > 0) {
                    double avg = sumRadius / (double)starList.Count;
                    double stdev = Math.Sqrt((sumSquares - starList.Count * avg * avg) / starList.Count);
                    if (p.Sensitivity != StarSensitivityEnum.Highest) {
                        starList = starList.Where(s => s.Radius <= avg + 1.5 * stdev && s.Radius >= avg - 1.5 * stdev).ToList<Star>();
                    } else {
                        //More sensitivity means getting fainter and smaller stars, and maybe some noise, skewing the distribution towards low radius. Let's be more permissive towards the large star end.
                        starList = starList.Where(s => s.Radius <= avg + 2 * stdev && s.Radius >= avg - 1.5 * stdev).ToList<Star>();
                    }
                }

                // Ensure we provide the list of detected stars, even if NumberOfAF stars is used
                detectedStars = starList.Count;

                //We are performing AF with only a limited number of stars
                if (p.NumberOfAFStars > 0) {
                    //First AF exposure, let's find the brightest star positions and store them
                    if (starList.Count != 0 && (p.MatchStarPositions == null || p.MatchStarPositions.Count == 0)) {
                        if (starList.Count <= p.NumberOfAFStars) {
                            result.BrightestStarPositions = starList.ConvertAll(s => s.Position);
                        } else {
                            starList = starList.OrderByDescending(s => s.Radius * 0.3 + s.MeanBrightness * 0.7).Take(p.NumberOfAFStars).ToList<Star>();
                            result.BrightestStarPositions = starList.ConvertAll(i => i.Position);
                        }
                        return starList.Select(s => s.ToDetectedStar()).ToList();
                    } else { //find the closest stars to the brightest stars previously identified
                        List<Star> topStars = new List<Star>();
                        p.MatchStarPositions.ForEach(pos => topStars.Add(starList.Aggregate((min, next) => min.Position.DistanceTo(pos) < next.Position.DistanceTo(pos) ? min : next)));
                        return topStars.Select(s => s.ToDetectedStar()).ToList(); ;
                    }
                }
                return starList.Select(s => s.ToDetectedStar()).ToList();
            }
        }

        private double CalculateEccentricity(double width, double height) {
            var x = Math.Max(width, height);
            var y = Math.Min(width, height);
            double focus = Math.Sqrt(Math.Pow(x, 2) - Math.Pow(y, 2));
            return focus / x;
        }

        private BlobCounter DetectStructures(Bitmap bmp, CancellationToken token) {
            using (MyStopWatch.Measure()) {
                /* detect structures */
                BlobCounter blobCounter = new BlobCounter();
                blobCounter.ProcessImage(bmp);

                token.ThrowIfCancellationRequested();

                return blobCounter;
            }
        }

        private void PrepareForStructureDetection(Bitmap bmp, StarDetectionParams p, CancellationToken token) {
            using (MyStopWatch.Measure()) {
                using (MyStopWatch.Measure("PrepareForStructureDetection - CannyEdge")) {
                    if (p.NoiseReduction == NoiseReductionEnum.None || p.NoiseReduction == NoiseReductionEnum.Median) {
                        //Still need to apply Gaussian blur, using normal Canny
                        new CannyEdgeDetector(10, 80).ApplyInPlace(bmp);
                    } else {
                        //Gaussian blur already applied, using no-blur Canny
                        new NoBlurCannyEdgeDetector(10, 80).ApplyInPlace(bmp);
                    }
                }

                token.ThrowIfCancellationRequested();
                using (MyStopWatch.Measure("PrepareForStructureDetection - SISThreshold")) {
                    new SISThreshold().ApplyInPlace(bmp);
                }

                token.ThrowIfCancellationRequested();
                using (MyStopWatch.Measure("PrepareForStructureDetection - BinaryDilation3x3")) {
                    new BinaryDilation3x3().ApplyInPlace(bmp);
                }
                token.ThrowIfCancellationRequested();
            }
        }

        private Bitmap ReduceNoise(Bitmap bitmapToAnalyze, StarDetectionParams p) {
            using (MyStopWatch.Measure()) {
                if (bitmapToAnalyze.Width > _maxWidth) {
                    Bitmap bmp;
                    switch (p.NoiseReduction) {
                        case NoiseReductionEnum.High:
                            bmp = new FastGaussianBlur(bitmapToAnalyze).Process(2);
                            break;

                        case NoiseReductionEnum.Highest:
                            bmp = new FastGaussianBlur(bitmapToAnalyze).Process(3);
                            break;

                        case NoiseReductionEnum.Median:
                            bmp = new Median().Apply(bitmapToAnalyze);
                            break;

                        default:
                            bmp = new FastGaussianBlur(bitmapToAnalyze).Process(1);
                            break;
                    }
                    bitmapToAnalyze.Dispose();
                    bitmapToAnalyze = bmp;
                }
                return bitmapToAnalyze;
            }
        }

        public IStarDetectionAnalysis CreateAnalysis() {
            return new StarDetectionAnalysis();
        }

        public void UpdateAnalysis(IStarDetectionAnalysis analysis, StarDetectionParams p, StarDetectionResult result) {
            analysis.HFR = result.AverageHFR;
            analysis.HFRStDev = result.HFRStdDev;
            analysis.DetectedStars = result.DetectedStars;
            analysis.StarList = result.StarList;
        }
    }
}