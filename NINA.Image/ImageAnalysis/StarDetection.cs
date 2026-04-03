#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
            // Measure shape/flux out to 1.5x the detected star radius so the aperture includes
            // most of the stellar profile wings without expanding too far into local background
            // or neighboring sources.
            private const double MeasurementRadiusScale = 1.5d;

            // Use 0.5 px radial bins for the azimuthally averaged profile to keep enough
            // sub-pixel resolution for the FWHM half-maximum crossing while still smoothing
            // pixel-grid noise.
            private const double RadialProfileBinWidth = 0.5d;
            private const int CentroidMaxIterations = 3;
            private const double CentroidConvergencePixels = 0.01d;

            public double Radius { get; init; }
            public Rectangle Rectangle { get; init; }
            public double MeanBrightness { get; set; }
            public double SurroundingMean { get; set; }
            public double MaxPixelValue { get; set; }
            public Point Position { get;set; }
            public double HFR { get; private set; }
            public double FWHM { get; private set; }
            public double Eccentricity { get; private set; }
            public double Average { get; private set; }

            public void Calculate(List<PixelData> pixelData) {
                this.HFR = double.NaN;
                this.FWHM = double.NaN;
                this.Eccentricity = double.NaN;
                this.Average = double.NaN;

                if (pixelData.Count == 0) {
                    return;
                }

                var centroidRadius = GetMeasurementRadius(this.Position, pixelData);
                var centroid = CalculateCentroid(pixelData, this.Position, centroidRadius, iterate: true);
                if (centroid.TotalFlux <= 0) {
                    return;
                }

                this.Position = centroid.Center;

                var measurementRadius = GetMeasurementRadius(this.Position, pixelData);
                var radialSamples = CollectRadialSamples(pixelData, this.Position, measurementRadius);
                if (radialSamples.Count == 0) {
                    return;
                }

                var totalFlux = radialSamples.Sum(sample => sample.PositiveFlux);
                if (totalFlux <= 0) {
                    return;
                }

                var positiveSampleCount = radialSamples.Count(sample => sample.PositiveFlux > 0);
                this.Average = positiveSampleCount > 0 ? totalFlux / positiveSampleCount : double.NaN;
                this.HFR = CalculateHalfFluxRadius(radialSamples, totalFlux);
                this.FWHM = CalculateFwhm(radialSamples);
                this.Eccentricity = CalculateEccentricity(radialSamples, this.Position);
            }

            internal bool InsideCircle(double x, double y, double centerX, double centerY, double radius) {
                return (Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2) <= Math.Pow(radius, 2));
            }

            private double GetMeasurementRadius(Point center, List<PixelData> pixelData) {
                var minX = pixelData.Min(p => p.PosX);
                var maxX = pixelData.Max(p => p.PosX);
                var minY = pixelData.Min(p => p.PosY);
                var maxY = pixelData.Max(p => p.PosY);
                var availableRadius = Math.Min(
                    Math.Min(center.X - minX, maxX - center.X),
                    Math.Min(center.Y - minY, maxY - center.Y)) - 0.5d;

                var requestedRadius = Math.Max(this.Radius * MeasurementRadiusScale, 2d);
                return Math.Max(1d, Math.Min(requestedRadius, availableRadius));
            }

            private (Point Center, double TotalFlux) CalculateCentroid(List<PixelData> pixelData, Point center, double radius, bool iterate) {
                // Windowed centroiding with a circular Gaussian weight follows the standard
                // weighted-moment source-extraction approach used in SExtractor. See
                // Bertin & Arnouts (1996), https://doi.org/10.1051/aas:1996164 and
                // https://sextractor.readthedocs.io/en/latest/PositionWin.html
                var sigma = GetWindowSigma(radius);
                var currentCenter = center;
                var maxIterations = iterate ? CentroidMaxIterations : 1;
                var lastTotalFlux = 0d;

                for (var iteration = 0; iteration < maxIterations; iteration++) {
                    double sumWeightedFlux = 0;
                    double sumX = 0;
                    double sumY = 0;
                    double sumFlux = 0;

                    foreach (var data in pixelData) {
                        var dx = data.PosX - currentCenter.X;
                        var dy = data.PosY - currentCenter.Y;
                        var distanceSquared = dx * dx + dy * dy;
                        if (distanceSquared > radius * radius) {
                            continue;
                        }

                        var flux = data.Value - SurroundingMean;
                        if (flux <= 0) {
                            continue;
                        }

                        var window = Math.Exp(-0.5d * distanceSquared / (sigma * sigma));
                        var weightedFlux = flux * window;
                        sumWeightedFlux += weightedFlux;
                        sumX += data.PosX * weightedFlux;
                        sumY += data.PosY * weightedFlux;
                        sumFlux += flux;
                    }

                    if (sumWeightedFlux <= 0) {
                        return (currentCenter, 0);
                    }

                    var refinedCenter = new Point((float)(sumX / sumWeightedFlux), (float)(sumY / sumWeightedFlux));
                    lastTotalFlux = sumFlux;
                    if (!iterate || refinedCenter.DistanceTo(currentCenter) <= CentroidConvergencePixels) {
                        return (refinedCenter, lastTotalFlux);
                    }

                    currentCenter = refinedCenter;
                }

                return (currentCenter, lastTotalFlux);
            }

            private double GetWindowSigma(double radius) {
                return Math.Max(radius / 2d, 1d);
            }

            private List<RadialSample> CollectRadialSamples(List<PixelData> pixelData, Point center, double radius) {
                var samples = new List<RadialSample>();

                foreach (var data in pixelData) {
                    var distance = Math.Sqrt(Math.Pow(data.PosX - center.X, 2.0d) + Math.Pow(data.PosY - center.Y, 2.0d));
                    if (distance > radius) {
                        continue;
                    }

                    var flux = data.Value - SurroundingMean;
                    samples.Add(new RadialSample(distance, flux, Math.Max(flux, 0), data.PosX, data.PosY));
                }

                return samples;
            }

            private double CalculateHalfFluxRadius(List<RadialSample> samples, double totalFlux) {
                // Curve-of-growth HFR: sort background-subtracted flux by radius and interpolate
                // the radius at which the enclosed flux reaches 50% of the total enclosed flux.
                // This follows the standard half-light / half-flux radius definition rather than
                // the older first-moment HFD approximation. For autofocus/HFD context, see:
                // Larry Weber & Steve Brady, "Fast Auto-Focus Method and Software for CCD-based
                // Telescopes" (CCDWare), http://www.ccdware.com/Files/ITS%20Paper.pdf
                var targetFlux = totalFlux / 2d;
                var cumulativeFlux = 0d;
                var previousFlux = 0d;
                var previousRadius = 0d;

                foreach (var sample in samples.OrderBy(s => s.Distance)) {
                    if (sample.PositiveFlux <= 0) {
                        continue;
                    }

                    cumulativeFlux += sample.PositiveFlux;
                    if (cumulativeFlux < targetFlux) {
                        previousFlux = cumulativeFlux;
                        previousRadius = sample.Distance;
                        continue;
                    }

                    if (cumulativeFlux == previousFlux || sample.Distance <= previousRadius) {
                        return sample.Distance;
                    }

                    var fraction = (targetFlux - previousFlux) / (cumulativeFlux - previousFlux);
                    return previousRadius + fraction * (sample.Distance - previousRadius);
                }

                return samples.Max(sample => sample.Distance);
            }

            private double CalculateFwhm(List<RadialSample> samples) {
                // FWHM is measured from the azimuthally averaged, background-subtracted radial
                // profile and linearly interpolated at the half-maximum crossing. This is a
                // standard stellar profile width measure; compare the open Photutils radial
                // profile documentation, which treats background-subtracted radial profiles and
                // FWHM estimation from them:
                // https://photutils.readthedocs.io/en/stable/user_guide/profiles.html
                // https://photutils.readthedocs.io/en/stable/api/photutils.profiles.RadialProfile.html
                var peakFlux = MaxPixelValue - SurroundingMean;
                if (peakFlux <= 0) {
                    return double.NaN;
                }

                var halfMaximum = peakFlux / 2d;
                var maxDistance = samples.Max(sample => sample.Distance);
                var binCount = (int)Math.Ceiling(maxDistance / RadialProfileBinWidth) + 1;
                var sums = new double[binCount];
                var distanceSums = new double[binCount];
                var counts = new int[binCount];

                foreach (var sample in samples) {
                    var index = Math.Min((int)Math.Floor(sample.Distance / RadialProfileBinWidth), binCount - 1);
                    sums[index] += sample.RawFlux;
                    distanceSums[index] += sample.Distance;
                    counts[index]++;
                }

                var previousRadius = 0d;
                var previousValue = peakFlux;

                for (var i = 0; i < binCount; i++) {
                    if (counts[i] == 0) {
                        continue;
                    }

                    var radius = distanceSums[i] / counts[i];
                    var value = sums[i] / counts[i];
                    if (i > 0 && value > previousValue) {
                        value = previousValue;
                    }

                    if (value <= halfMaximum) {
                        if (Math.Abs(value - previousValue) < double.Epsilon || radius <= previousRadius) {
                            return 2d * radius;
                        }

                        var fraction = (halfMaximum - previousValue) / (value - previousValue);
                        var halfMaxRadius = previousRadius + fraction * (radius - previousRadius);
                        return 2d * halfMaxRadius;
                    }

                    previousRadius = radius;
                    previousValue = value;
                }

                return double.NaN;
            }

            private double CalculateEccentricity(List<RadialSample> samples, Point center) {
                // Eccentricity from background-subtracted, flux-weighted second central moments
                // within the local measurement aperture:
                // e = sqrt(1 - lambda_minor / lambda_major), where the lambdas are the
                // covariance-matrix eigenvalues of the source profile. This is the standard
                // moment-based shape formalism used in source extraction; see
                // Bertin & Arnouts (1996), SExtractor, A&AS 117, 393,
                // https://doi.org/10.1051/aas:1996164
                double momentXX = 0;
                double momentYY = 0;
                double momentXY = 0;
                double totalFlux = 0;

                foreach (var sample in samples) {
                    if (sample.PositiveFlux <= 0) {
                        continue;
                    }

                    var dx = sample.PosX - center.X;
                    var dy = sample.PosY - center.Y;
                    totalFlux += sample.PositiveFlux;
                    momentXX += sample.PositiveFlux * dx * dx;
                    momentYY += sample.PositiveFlux * dy * dy;
                    momentXY += sample.PositiveFlux * dx * dy;
                }

                if (totalFlux <= 0) {
                    return double.NaN;
                }

                momentXX /= totalFlux;
                momentYY /= totalFlux;
                momentXY /= totalFlux;

                var trace = momentXX + momentYY;
                var determinantTerm = (momentXX - momentYY) * (momentXX - momentYY) + 4d * momentXY * momentXY;
                var root = Math.Sqrt(Math.Max(0d, determinantTerm));
                var major = (trace + root) / 2d;
                var minor = (trace - root) / 2d;

                if (major <= 0) {
                    return double.NaN;
                }

                minor = Math.Max(0d, minor);
                return Math.Sqrt(Math.Max(0d, 1d - (minor / major)));
            }

            public DetectedStar ToDetectedStar() {
                return new DetectedStar() {
                    HFR = HFR,
                    FWHM = FWHM,
                    Eccentricity = Eccentricity,
                    Position = Position,
                    AverageBrightness = Average,
                    MaxBrightness = MaxPixelValue,
                    Background = SurroundingMean,
                    BoundingBox = Rectangle
                };
            }
        }

        public record PixelData (int PosX, int PosY, double Value);
        private record RadialSample(double Distance, double RawFlux, double PositiveFlux, int PosX, int PosY);

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
                        var validHfrValues = result.StarList.Select(star => star.HFR).Where(hfr => !double.IsNaN(hfr)).ToList();
                        result.DetectedStars = detectedStars;

                        if (validHfrValues.Count > 0) {
                            var mean = validHfrValues.Average();
                            var stdDev = 0d;
                            if (validHfrValues.Count > 1) {
                                stdDev = Math.Sqrt(validHfrValues.Sum(hfr => (hfr - mean) * (hfr - mean)) / (validHfrValues.Count - 1));
                            }

                            var validFwhmValues = result.StarList.Select(star => star.FWHM).Where(fwhm => !double.IsNaN(fwhm)).ToList();
                            var averageFwhm = validFwhmValues.Count > 0 ? validFwhmValues.Average() : double.NaN;
                            var validEccentricities = result.StarList.Select(star => star.Eccentricity).Where(ecc => !double.IsNaN(ecc)).ToList();
                            var averageEccentricity = validEccentricities.Count > 0 ? validEccentricities.Average() : double.NaN;

                            Logger.Info($"Average HFR: {mean}, Average FWHM: {averageFwhm}, Average Eccentricity: {averageEccentricity}, HFR σ: {stdDev}, Detected Stars {detectedStars}, Sensitivity {p.Sensitivity}, ResizeFactor: {Math.Round(state._resizefactor, 2)}");

                            result.AverageHFR = mean;
                            result.AverageFWHM = averageFwhm;
                            result.AverageEccentricity = averageEccentricity;
                            result.HFRStdDev = stdDev;
                        }
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
                    var backgroundPixelValues = new List<double>();
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
                            } else { //We're in the larger surrounding holed rectangle, providing local background
                                backgroundPixelValues.Add(pixelValue);
                            }

                            ushort value = pixelValue;
                            var pd = new PixelData(PosX: x, PosY: y, Value: value);
                            pixelDataList.Add(pd);
                        }
                    }

                    if (starPixelCount == 0) {
                        continue;
                    }

                    s.MeanBrightness = starPixelSum / (double)starPixelCount;
                    if (backgroundPixelValues.Count == 0) {
                        continue;
                    }

                    var backgroundStats = EstimateBackground(backgroundPixelValues);
                    double largeRectMean = backgroundStats.Background;
                    s.SurroundingMean = largeRectMean;
                    double largeRectStdev = backgroundStats.Sigma;
                    int minimumNumberOfPixels = (int)Math.Ceiling(Math.Max(state._originalBitmapSource.PixelWidth, state._originalBitmapSource.PixelHeight) / 1000d);

                    if (s.MeanBrightness >= largeRectMean + Math.Min(0.1 * largeRectMean, largeRectStdev) && innerStarPixelValues.Count(pv => pv > largeRectMean + 1.5 * largeRectStdev) > minimumNumberOfPixels) {
                        s.Calculate(pixelDataList);
                        //It's a local maximum, and has enough bright pixels, so likely to be a star.                        
                        if (s.Position.X > (s.Rectangle.X + 1) && s.Position.Y > (s.Rectangle.Y + 1) && s.Position.X < (s.Rectangle.X + s.Rectangle.Width - 2) && s.Position.Y < (s.Rectangle.Y + s.Rectangle.Height - 2)) {
                            // Only add star when centroid is not touching the rectangle edges
                            sumRadius += s.Radius;
                            sumSquares += s.Radius * s.Radius;
                            starList.Add(s);
                        }
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

        private (double Background, double Sigma) EstimateBackground(List<double> pixelValues) {
            // Robust local sky estimate from a sigma-clipped median. Sigma clipping is standard
            // practice for astronomical background estimation in the presence of source pixels
            // and outliers; see the Astropy CCD Reduction Guide:
            // https://www.astropy.org/ccd-reduction-and-photometry-guide/
            const int maxIterations = 3;
            const double sigmaThreshold = 3d;

            var values = pixelValues.ToList();
            if (values.Count == 0) {
                return (0d, 0d);
            }

            for (var iteration = 0; iteration < maxIterations && values.Count > 1; iteration++) {
                values.Sort();
                var median = MedianFromSorted(values);
                var deviations = new List<double>(values.Count);
                for (var index = 0; index < values.Count; index++) {
                    deviations.Add(Math.Abs(values[index] - median));
                }
                deviations.Sort();
                var mad = MedianFromSorted(deviations);
                var sigma = mad > 0 ? 1.4826d * mad : StandardDeviation(values, median);
                if (sigma <= 0) {
                    return (median, 0d);
                }

                var clipped = new List<double>(values.Count);
                for (var index = 0; index < values.Count; index++) {
                    var value = values[index];
                    if (Math.Abs(value - median) <= sigmaThreshold * sigma) {
                        clipped.Add(value);
                    }
                }
                if (clipped.Count == values.Count || clipped.Count == 0) {
                    return (median, sigma);
                }

                values = clipped;
            }

            values.Sort();
            var finalMedian = MedianFromSorted(values);
            return (finalMedian, StandardDeviation(values, finalMedian));
        }

        private double MedianFromSorted(List<double> values) {
            if (values.Count == 0) {
                return 0d;
            }

            var middle = values.Count / 2;
            if (values.Count % 2 == 0) {
                return (values[middle - 1] + values[middle]) / 2d;
            }

            return values[middle];
        }

        private double StandardDeviation(List<double> values, double mean) {
            if (values.Count == 0) {
                return 0d;
            }

            double sumSquares = 0d;
            for (var index = 0; index < values.Count; index++) {
                var delta = values[index] - mean;
                sumSquares += delta * delta;
            }

            return Math.Sqrt(sumSquares / values.Count);
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
            analysis.FWHM = result.AverageFWHM;
            analysis.Eccentricity = result.AverageEccentricity;
            analysis.HFRStDev = result.HFRStdDev;
            analysis.DetectedStars = result.DetectedStars;
            analysis.StarList = result.StarList;
        }
    }
}
