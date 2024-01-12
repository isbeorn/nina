#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging.Filters;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Image.ImageAnalysis {

    public class ContrastDetection : IContrastDetection {
        private static int _maxWidth = 1552;

        public async Task<ContrastDetectionResult> Measure(IRenderedImage image, ContrastDetectionParams p, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var result = new ContrastDetectionResult();
            try {
                var state = GetInitialState(image, p);

                using (MyStopWatch.Measure()) {
                    Stopwatch overall = Stopwatch.StartNew();
                    progress?.Report(new ApplicationStatus() { Status = "Preparing image for contrast measurement" });

                    var _bitmapToAnalyze = ImageUtility.Convert16BppTo8Bpp(state._originalBitmapSource);

                    token.ThrowIfCancellationRequested();

                    //Crop if there is ROI

                    if (p.UseROI && p.InnerCropRatio < 1) {
                        Rectangle cropRectangle = DetectionUtility.GetCropRectangle(_bitmapToAnalyze, p.InnerCropRatio);
                        _bitmapToAnalyze = new Crop(cropRectangle).Apply(_bitmapToAnalyze);
                    }

                    if (p.NoiseReduction == NoiseReductionEnum.Median) {
                        new Median().ApplyInPlace(_bitmapToAnalyze);
                    }

                    //Make sure resizing is independent of Star Sensitivity
                    state._resizefactor = (double)_maxWidth / _bitmapToAnalyze.Width;
                    state._inverseResizefactor = 1.0 / state._resizefactor;

                    /* Resize to speed up manipulation */
                    _bitmapToAnalyze = DetectionUtility.ResizeForDetection(_bitmapToAnalyze, _maxWidth, state._resizefactor);

                    progress?.Report(new ApplicationStatus() { Status = "Measuring Contrast" });

                    token.ThrowIfCancellationRequested();

                    if (p.Method == ContrastDetectionMethodEnum.Laplace) {
                        if (p.NoiseReduction == NoiseReductionEnum.None || p.NoiseReduction == NoiseReductionEnum.Median) {
                            int[,] kernel = new int[7, 7];
                            kernel = DetectionUtility.LaplacianOfGaussianKernel(7, 1.0);
                            new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        } else if (p.NoiseReduction == NoiseReductionEnum.Normal) {
                            int[,] kernel = new int[9, 9];
                            kernel = DetectionUtility.LaplacianOfGaussianKernel(9, 1.4);
                            new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        } else if (p.NoiseReduction == NoiseReductionEnum.High) {
                            int[,] kernel = new int[11, 11];
                            kernel = DetectionUtility.LaplacianOfGaussianKernel(11, 1.8);
                            new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        } else {
                            int[,] kernel = new int[13, 13];
                            kernel = DetectionUtility.LaplacianOfGaussianKernel(13, 2.2);
                            new Convolution(kernel).ApplyInPlace(_bitmapToAnalyze);
                        }
                        //Get mean and standard dev
                        Accord.Imaging.ImageStatistics stats = new Accord.Imaging.ImageStatistics(_bitmapToAnalyze);
                        result.AverageContrast = stats.GrayWithoutBlack.Mean;
                        result.ContrastStdev = 0.01; //Stdev of convoluted image is not a measure of error - using same figure for all
                    } else if (p.Method == ContrastDetectionMethodEnum.Sobel) {
                        if (p.NoiseReduction == NoiseReductionEnum.None || p.NoiseReduction == NoiseReductionEnum.Median) {
                            //Nothing to do
                        } else if (p.NoiseReduction == NoiseReductionEnum.Normal) {
                            _bitmapToAnalyze = new FastGaussianBlur(_bitmapToAnalyze).Process(1);
                        } else if (p.NoiseReduction == NoiseReductionEnum.High) {
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
                        result.AverageContrast = stats.GrayWithoutBlack.Mean;
                        result.ContrastStdev = 0.01; //Stdev of convoluted image is not a measure of error - using same figure for all
                    }

                    token.ThrowIfCancellationRequested();

                    _bitmapToAnalyze.Dispose();
                    overall.Stop();
                    Debug.Print("Overall contrast detection: " + overall.Elapsed);
                    overall = null;
                }
            } catch (OperationCanceledException) {
            } finally {
                progress?.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return result;
        }

        private class State {
            public IImageArray _iarr;
            public ImageProperties imageProperties;
            public BitmapSource _originalBitmapSource;
            public double _resizefactor;
            public double _inverseResizefactor;
            public int _minStarSize;
            public int _maxStarSize;
        }

        private static State GetInitialState(IRenderedImage renderedImage, ContrastDetectionParams p) {
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
                    state._resizefactor = Math.Max(0.625, (double)_maxWidth / state.imageProperties.Width);
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
            return state;
        }
    }
}