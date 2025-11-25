#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OpenCvSharp;
using System.Drawing;

namespace Accord.Imaging {
    /// <summary>
    /// Image statistics calculator using OpenCV
    /// Computes various statistical measures for image pixels
    /// </summary>
    public class ImageStatistics {
        public HistogramStatistics Gray { get; private set; }
        public HistogramStatistics GrayWithoutBlack { get; private set; }

        public ImageStatistics() {
            Gray = new HistogramStatistics();
            GrayWithoutBlack = new HistogramStatistics();
        }

        public ImageStatistics(Bitmap image) {
            Mat mat = image;

            if (mat.Empty()) {
                Gray = new HistogramStatistics();
                GrayWithoutBlack = new HistogramStatistics();
                return;
            }

            // Calculate histogram for all pixels
            Mat grayMat = mat;
            if (mat.Channels() > 1) {
                grayMat = new Mat();
                Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);
            }

            // Calculate histogram
            int[] histSize = { 256 };
            Rangef[] ranges = { new Rangef(0, 256) };
            Mat hist = new Mat();
            Cv2.CalcHist(new Mat[] { grayMat }, new int[] { 0 }, null, hist, 1, histSize, ranges);

            // Calculate statistics from histogram
            double sum = 0, weightedSum = 0;
            int count = 0;
            int min = 256, max = -1;

            for (int i = 0; i < 256; i++) {
                int binCount = (int)hist.At<float>(i);
                if (binCount > 0) {
                    sum += binCount;
                    weightedSum += i * binCount;
                    count += binCount;
                    if (min > i) min = i;
                    if (max < i) max = i;
                }
            }

            double mean = count > 0 ? weightedSum / sum : 0;

            // Calculate standard deviation
            double variance = 0;
            for (int i = 0; i < 256; i++) {
                int binCount = (int)hist.At<float>(i);
                if (binCount > 0) {
                    double diff = i - mean;
                    variance += diff * diff * binCount;
                }
            }
            double stdDev = count > 0 ? System.Math.Sqrt(variance / sum) : 0;

            // Calculate median
            double median = 0;
            int medianCount = (int)(sum / 2);
            int cumulative = 0;
            for (int i = 0; i < 256; i++) {
                cumulative += (int)hist.At<float>(i);
                if (cumulative >= medianCount) {
                    median = i;
                    break;
                }
            }

            Gray = new HistogramStatistics {
                Mean = mean,
                StdDev = stdDev,
                Median = median,
                Min = min,
                Max = max,
                PixelsCount = count
            };

            // Calculate histogram without black pixels (value > 0)
            Mat mask = new Mat();
            Cv2.Threshold(grayMat, mask, 0, 255, ThresholdTypes.Binary);

            Mat histNoBlack = new Mat();
            Cv2.CalcHist(new Mat[] { grayMat }, new int[] { 0 }, mask, histNoBlack, 1, histSize, ranges);

            // Calculate statistics from histogram without black
            sum = 0;
            weightedSum = 0;
            count = 0;
            min = 256;
            max = -1;

            for (int i = 1; i < 256; i++) { // Start from 1 to exclude black
                int binCount = (int)histNoBlack.At<float>(i);
                if (binCount > 0) {
                    sum += binCount;
                    weightedSum += i * binCount;
                    count += binCount;
                    if (min > i) min = i;
                    if (max < i) max = i;
                }
            }

            double meanNoBlack = count > 0 ? weightedSum / sum : 0;

            // Calculate standard deviation without black
            variance = 0;
            for (int i = 1; i < 256; i++) {
                int binCount = (int)histNoBlack.At<float>(i);
                if (binCount > 0) {
                    double diff = i - meanNoBlack;
                    variance += diff * diff * binCount;
                }
            }
            double stdDevNoBlack = count > 0 ? System.Math.Sqrt(variance / sum) : 0;

            // Calculate median without black
            double medianNoBlack = 0;
            medianCount = (int)(sum / 2);
            cumulative = 0;
            for (int i = 1; i < 256; i++) {
                cumulative += (int)histNoBlack.At<float>(i);
                if (cumulative >= medianCount) {
                    medianNoBlack = i;
                    break;
                }
            }

            GrayWithoutBlack = new HistogramStatistics {
                Mean = meanNoBlack,
                StdDev = stdDevNoBlack,
                Median = medianNoBlack,
                Min = min,
                Max = max,
                PixelsCount = count
            };

            // Cleanup
            hist.Dispose();
            histNoBlack.Dispose();
            mask.Dispose();
            if (grayMat != mat) {
                grayMat.Dispose();
            }
        }
    }

    /// <summary>
    /// Histogram statistics - stores mean and standard deviation
    /// Inherits from Accord.Statistics.Visualizations.Histogram for API compatibility
    /// </summary>
    public class HistogramStatistics : Statistics.Visualizations.Histogram {
        public HistogramStatistics() : base(new int[256]) {
        }

        public double Mean { get; set; }
        public double StdDev { get; set; }
        public new double Median { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int PixelsCount { get; set; }
    }
}
