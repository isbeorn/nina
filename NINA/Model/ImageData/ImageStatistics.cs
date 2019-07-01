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

using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NINA.Model.ImageData {

    public class ImageStatistics : BaseINPC, IImageStatistics {
        public const int HISTOGRAMRESOLUTION = 100;

        /// <summary>
        /// Create new instance of ImageStatistics
        /// Calculate() has to be called to yield all statistics.
        /// </summary>
        /// <param name="width">width of one row</param>
        /// <param name="height">height of one column</param>
        /// <param name="bitDepth">bit depth of a pixel</param>
        /// <param name="isBayered">Flag to indicate if the image is bayer matrix encoded</param>
        /// <param name="resolution">Target histogram resolution</param>
        public ImageStatistics(int width, int height, int bitDepth, bool isBayered) {
            this.Width = width;
            this.Height = height;
            this.IsBayered = isBayered;
            this.BitDepth = bitDepth;
        }

        public int Id { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BitDepth { get; private set; }
        public double StDev { get; private set; }
        public double Mean { get; private set; }
        public double Median { get; private set; }
        public double MedianAbsoluteDeviation { get; private set; }
        public int Max { get; private set; }
        public long MaxOccurrences { get; private set; }
        public int Min { get; private set; }
        public long MinOccurrences { get; private set; }
        public bool IsBayered { get; private set; }
        public List<OxyPlot.DataPoint> Histogram { get; private set; }

        private int detectedStars;

        public int DetectedStars {
            get {
                return detectedStars;
            }

            set {
                detectedStars = value;
                RaisePropertyChanged();
            }
        }

        private double hFR;

        public double HFR {
            get {
                return hFR;
            }

            set {
                hFR = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Calculate statistics out of image array.
        /// </summary>
        /// <param name="array">one dimensional image array</param>
        /// <returns></returns>
        public Task Calculate(ushort[] array) {
            return Task.Run(() => CalculateInternal(array));
        }

        private void CalculateInternal(ushort[] array) {
            using (MyStopWatch.Measure()) {
                long sum = 0;
                long squareSum = 0;
                int count = array.Count();
                ushort min = ushort.MaxValue;
                ushort oldmin = min;
                ushort max = 0;
                ushort oldmax = max;
                long maxOccurrences = 0;
                long minOccurrences = 0;

                /* Array mapping: pixel value -> total number of occurrences of that pixel value */
                int[] histogram = new int[ushort.MaxValue + 1];
                for (var i = 0; i < array.Length; i++) {
                    ushort val = array[i];

                    sum += val;
                    squareSum += (long)val * val;

                    histogram[val]++;

                    min = Math.Min(min, val);
                    if (min != oldmin) {
                        minOccurrences = 0;
                    }
                    if (val == min) {
                        minOccurrences += 1;
                    }

                    max = Math.Max(max, val);
                    if (max != oldmax) {
                        maxOccurrences = 0;
                    }
                    if (val == max) {
                        maxOccurrences += 1;
                    }

                    oldmin = min;
                    oldmax = max;
                }

                double mean = sum / (double)count;
                double variance = (squareSum - count * mean * mean) / (count);
                double stdev = Math.Sqrt(variance);

                var occurrences = 0;
                double median = 0d;
                int median1 = 0, median2 = 0;
                var medianlength = array.Length / 2.0;

                /* Determine median out of histogram array */
                for (ushort i = 0; i < ushort.MaxValue; i++) {
                    occurrences += histogram[i];
                    if (occurrences > medianlength) {
                        median1 = i;
                        median2 = i;
                        break;
                    } else if (occurrences == medianlength) {
                        median1 = i;
                        for (int j = i + 1; j <= ushort.MaxValue; j++) {
                            if (histogram[j] > 0) {
                                median2 = j;
                                break;
                            }
                        }
                        break;
                    }
                }
                median = (median1 + median2) / 2.0;

                /* Determine median Absolute Deviation out of histogram array and previously determined median
                 * As the histogram already has the values sorted and we know the median,
                 * we can determine the mad by beginning from the median and step up and down
                 * By doing so we will gain a sorted list automatically, because MAD = DetermineMedian(|xn - median|)
                 * So starting from the median will be 0 (as median - median = 0), going up and down will increment by the steps
                 */

                var medianAbsoluteDeviation = 0.0d;
                occurrences = 0;
                var idxDown = median1;
                var idxUp = median2;
                while (true) {
                    if (idxDown >= 0 && idxDown != idxUp) {
                        occurrences += histogram[idxDown] + histogram[idxUp];
                    } else {
                        occurrences += histogram[idxUp];
                    }

                    if (occurrences > medianlength) {
                        medianAbsoluteDeviation = Math.Abs(idxUp - median);
                        break;
                    }

                    idxUp++;
                    idxDown--;
                    if (idxUp > ushort.MaxValue) {
                        break;
                    }
                }

                this.Max = max;
                this.MaxOccurrences = maxOccurrences;
                this.Min = min;
                this.MinOccurrences = minOccurrences;
                this.StDev = stdev;
                this.Mean = mean;
                this.Median = median;
                this.MedianAbsoluteDeviation = medianAbsoluteDeviation;
                var maxPossibleValue = (ushort)((1 << BitDepth) - 1);
                var factor = (double)HISTOGRAMRESOLUTION / maxPossibleValue;
                this.Histogram = histogram
                    .Select((value, index) => new { Index = index, Value = value })
                    .GroupBy(
                        x => Math.Floor((double)Math.Min(maxPossibleValue, x.Index) * factor),
                        x => x.Value)
                    .Select(g => new OxyPlot.DataPoint(g.Key, g.Sum()))
                    .OrderBy(item => item.X).ToList();
            }
        }
    }
}