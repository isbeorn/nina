using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            /*Calculate StDev and Min/Max Values for Stretch */
            double average = this.FlatArray.Average(x => x);
            double sumOfSquaresOfDifferences = this.FlatArray.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / this.FlatArray.Length);

            this.Statistics.StDev = sd;
            this.Statistics.Mean = average;

            double resolution = Settings.HistogramResolution;

            this.Statistics.Histogram = this.FlatArray.GroupBy(x => Math.Floor(x * (resolution / ushort.MaxValue)))
                .Select(g => new OxyPlot.DataPoint (g.Key, g.Count()))
                .OrderBy(item => item.X).ToList();
        }

        private void FlipAndConvert(Array input) {
            if (input.GetType() == typeof(Int32[,,])) {
                this.FlatArray = FlipAndConvert3d(input);
            } else {
                this.FlatArray = FlipAndConvert2d(input);
            }
        }

        private ushort[] FlipAndConvert2d(Array input) {
            Int32[,] arr = (Int32[,])input;
            int width = arr.GetLength(0);
            int height = arr.GetLength(1);

            this.Statistics.Width = width;
            this.Statistics.Height = height;
            ushort[] flatArray = new ushort[arr.Length];
            ushort value;

            unsafe
            {
                fixed (Int32* ptr = arr) {
                    int idx = 0, row = 0;
                    for (int i = 0; i < arr.Length; i++) {
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

        private ushort[] FlipAndConvert3d(Array input) {
            Notification.ShowError(Locale.Loc.Instance["LblColorSensorNotSupported"]);
            throw new NotSupportedException();
        }
    }
}
