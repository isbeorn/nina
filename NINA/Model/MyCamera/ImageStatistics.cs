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

using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    public class ImageStatistics : BaseINPC, IImageStatistics {

        /// <summary>
        /// Create new instance of ImageStatistics
        /// Calculate() has to be called to yield all statistics.
        /// </summary>
        /// <param name="width">width of one row</param>
        /// <param name="height">height of one column</param>
        /// <param name="bitDepth">bit depth of a pixel</param>
        /// <param name="isBayered">Flag to indicate if the image is bayer matrix encoded</param>
        /// <param name="resolution">Target histogram resolution</param>
        public ImageStatistics(int width, int height, int bitDepth, bool isBayered, int resolution) {
            this.Width = width;
            this.Height = height;
            this.IsBayered = isBayered;
            this.resolution = resolution;
            this.BitDepth = bitDepth;
        }

        public int Id { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BitDepth { get; private set; }
        public double StDev { get; private set; }
        public double Mean { get; private set; }
        public int Max { get; private set; }
        public long MaxOccurrences { get; private set; }
        public int Min { get; private set; }
        public long MinOccurrences { get; private set; }
        public double ExposureTime { get; set; }
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

        private int resolution;
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
                ushort max = 0;
                ushort oldmax = max;
                long maxOccurrences = 0;
                ushort min = ushort.MaxValue;
                ushort oldmin = min;
                long minOccurrences = 0;

                Dictionary<double, int> histogram = new Dictionary<double, int>();
                ushort maxHistogramValue = (ushort)((1 << BitDepth) - 1);

                for (var i = 0; i < array.Length; i++) {
                    ushort val = array[i];
                    double histogramVal = Math.Floor(val * ((double)resolution / maxHistogramValue));

                    sum += val;
                    squareSum += (long)val * val;

                    histogram.TryGetValue(histogramVal, out var curCount);
                    histogram[histogramVal] = curCount + 1;

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

                this.Max = max;
                this.MaxOccurrences = maxOccurrences;
                this.Min = min;
                this.MinOccurrences = minOccurrences;
                this.StDev = stdev;
                this.Mean = mean;
                this.Histogram = histogram.Select(g => new OxyPlot.DataPoint(g.Key, g.Value))
                    .OrderBy(item => item.X).ToList();
            }
        }
    }
}