using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    public class ImageArray {
        public ushort[] FlatArray;
        public ImageStatistics Statistics { get; set; }

        public bool IsBayered { get; private set; }

        private ImageArray() {
            Statistics = new ImageStatistics { };
        }

        public static async Task<ImageArray> CreateInstance(Array input, bool isBayered = false) {
            ImageArray imgArray = new ImageArray();
            imgArray.IsBayered = isBayered;
            await Task.Run(() => imgArray.FlipAndConvert(input));
            await Task.Run(() => imgArray.CalculateStatistics());

            return imgArray;
        }

        public static async Task<ImageArray> CreateInstance(ushort[] input, int width, int height, bool isBayered = false) {
            ImageArray imgArray = new ImageArray();
            imgArray.IsBayered = isBayered;
            imgArray.FlatArray = input;
            imgArray.Statistics.Width = width;
            imgArray.Statistics.Height = height;
            await Task.Run(() => imgArray.CalculateStatistics());

            return imgArray;
        }

        private void CalculateStatistics() {
            using (MyStopWatch.Measure()) {
                long sum = 0;
                long squareSum = 0;
                int count = this.FlatArray.Count();
                ushort max = 0;
                ushort oldmax = max;
                long maxOccurrences = 0;
                ushort min = ushort.MaxValue;
                ushort oldmin = min;
                long minOccurrences = 0;

                double resolution = ProfileManager.Instance.ActiveProfile.ImageSettings.HistogramResolution;
                Dictionary<double, int> histogram = new Dictionary<double, int>();

                for (var i = 0; i < this.FlatArray.Length; i++) {
                    ushort val = this.FlatArray[i];
                    double histogramVal = Math.Floor(val * (resolution / ushort.MaxValue));

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

                double mean = sum / count;
                double variance = (squareSum - count * mean * mean) / (count);
                double stdev = Math.Sqrt(variance);

                this.Statistics.Max = max;
                this.Statistics.MaxOccurrences = maxOccurrences;
                this.Statistics.Min = min;
                this.Statistics.MinOccurrences = minOccurrences;
                this.Statistics.StDev = stdev;
                this.Statistics.Mean = mean;
                this.Statistics.Histogram = histogram.Select(g => new OxyPlot.DataPoint(g.Key, g.Value))
                    .OrderBy(item => item.X).ToList();
                this.Statistics.IsBayered = IsBayered;
            }
        }

        private void FlipAndConvert(Array input) {
            if (input.GetType() == typeof(Int32[,,])) {
                this.FlatArray = FlipAndConvert3d(input);
            } else {
                this.FlatArray = FlipAndConvert2d(input);
            }
        }

        private ushort[] FlipAndConvert2d(Array input) {
            using (MyStopWatch.Measure("FlipAndConvert2d")) {
                Int32[,] arr = (Int32[,])input;
                int width = arr.GetLength(0);
                int height = arr.GetLength(1);

                this.Statistics.Width = width;
                this.Statistics.Height = height;
                int length = width * height;
                ushort[] flatArray = new ushort[length];
                ushort value;

                unsafe {
                    fixed (Int32* ptr = arr) {
                        int idx = 0, row = 0;
                        for (int i = 0; i < length; i++) {
                            value = (ushort)ptr[i];

                            idx = ((i % height) * width) + row;
                            if ((i % (height)) == (height - 1)) row++;

                            ushort b = value;
                            flatArray[idx] = b;
                        }
                    }
                }
                return flatArray;
            }
        }

        private ushort[] FlipAndConvert3d(Array input) {
            Notification.ShowError(Locale.Loc.Instance["LblColorSensorNotSupported"]);
            throw new NotSupportedException();
        }
    }
}