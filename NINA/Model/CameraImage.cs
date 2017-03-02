using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model {
    class CameraImage {

    }

    public class ImageArray {
        public const ushort HistogramResolution = 1000;

        public Array SourceArray;
        public ushort[] FlatArray;
        public ImageStatistics Statistics { get; set; }

        private ImageArray() {
            Statistics = new ImageStatistics();
        }


        public static async Task<ImageArray> CreateInstance(Array input) {
            ImageArray imgArray = new ImageArray();

            await Task.Run(() => imgArray.FlipAndConvert(input));
            await Task.Run(() => imgArray.CalculateStatistics());

            return imgArray;
        }

        private void CalculateStatistics() {
            
            /*Calculate StDev and Min/Max Values for Stretch */
            double average = this.FlatArray.Average(x => x);
            double sumOfSquaresOfDifferences = this.FlatArray.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / this.FlatArray.Length);
            ushort min = 0, max = 0;
            double factor = 2.5;

            if (average - factor * sd < 0) {
                min = 0;
            }
            else {
                min = (ushort)(average - factor * sd);
            }

            if (average + factor * sd > ushort.MaxValue) {
                max = ushort.MaxValue;
            }
            else {
                max = (ushort)(average + factor * sd);
            }

            this.Statistics.StDev = sd;
            this.Statistics.Mean = average;
            this.Statistics.MinNormalizationValue = min;
            this.Statistics.MaxNormalizationValue = max;
        }

        private void FlipAndConvert(Array input) {
            Int32[,] arr = (Int32[,])input;
            this.SourceArray = arr;
            int width = arr.GetLength(0);
            int height = arr.GetLength(1);

            this.Statistics.Width = width;
            this.Statistics.Height = height;
            ushort[] flatArray = new ushort[arr.Length];
            ushort value, histogramkey;
            SortedDictionary<ushort, int> histogram = new SortedDictionary<ushort, int>();
            unsafe
            {
                fixed (Int32* ptr = arr) {
                    int idx = 0, row = 0;
                    for (int i = 0; i < arr.Length; i++) {
                        value = (ushort)ptr[i];




                        idx = ((i % height) * width) + row;
                        if ((i % (height)) == (height - 1)) row++;

                        histogramkey = Convert.ToUInt16(Math.Round(((double)ImageArray.HistogramResolution / ushort.MaxValue) * value));
                        if (histogram.ContainsKey(histogramkey)) {
                            histogram[histogramkey] += 1;
                        }
                        else {
                            histogram.Add(histogramkey, 1);
                        }

                        ushort b = value;
                        flatArray[idx] = b;


                    }
                }
            }

            this.Statistics.Histogram = histogram;
            this.FlatArray = flatArray;
        }
   
    }

    public class ImageStatistics {
        public int Width;
        public int Height;
        public double StDev { get; set; }
        public double Mean { get; set; }
        public ushort MinNormalizationValue { get; set; }
        public ushort MaxNormalizationValue { get; set; }
        public SortedDictionary<ushort, int> Histogram { get; set; }
    }

}
