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
using NINA.Utility.Notification;
using System;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    public class ImageArray : IImageArray {
        public ushort[] FlatArray;

        /// <summary>
        /// Contains RAW DSLR Data if available
        /// </summary>
        public byte[] RAWData { get; set; }

        /// <summary>
        /// Contains the type of DSLR data (e.g. cr2)
        /// </summary>
        public string RAWType { get; set; }

        public IImageStatistics Statistics { get; set; }

        private ImageArray() {
        }

        /// <summary>
        /// Factory to create an ImageArray object out of a 2d or 3d array
        /// </summary>
        /// <param name="input">A 2d or 3d image array (only 2d supported right now)</param>
        /// <param name="bitDepth">bit depth of each pixel</param>
        /// <param name="isBayered">Flag to indicate if the image is bayer matrix encoded</param>
        /// <param name="calculateStatistics">If false no statistics will be determined (fast mode)</param>
        /// <param name="histogramResolution">Resolution of the histogram</param>
        /// <returns></returns>
        public static async Task<ImageArray> CreateInstance(Array input, int bitDepth, bool isBayered, bool calculateStatistics, int histogramResolution) {
            ImageArray imgArray = new ImageArray();
            int width = input.GetLength(0);
            int height = input.GetLength(1);
            imgArray.Statistics = new ImageStatistics(width, height, bitDepth, isBayered, histogramResolution);
            await Task.Run(() => imgArray.FlipAndConvert(input));

            if (calculateStatistics) {
                await imgArray.Statistics.Calculate(imgArray.FlatArray);
            }

            return imgArray;
        }

        /// <summary>
        /// Factory to create an ImageArray object out of a one dimensional flat array
        /// </summary>
        /// <param name="input">Image Array in a flat array representation</param>
        /// <param name="width">Image data width (to be able to translate the 1d array back to 2d)</param>
        /// <param name="height">Image data width (to be able to translate the 1d array back to 2d)</param>
        /// <param name="bitDepth">bit depth of each pixel</param>
        /// <param name="isBayered">Flag to indicate if the image is bayer matrix encoded</param>
        /// <param name="calculateStatistics">If false no statistics will be determined (fast mode)</param>
        /// <param name="histogramResolution">Resolution of the histogram</param>
        /// <returns></returns>
        public static async Task<ImageArray> CreateInstance(ushort[] input, int width, int height, int bitDepth, bool isBayered, bool calculateStatistics, int histogramResolution) {
            ImageArray imgArray = new ImageArray();
            imgArray.FlatArray = input;
            imgArray.Statistics = new ImageStatistics(width, height, bitDepth, isBayered, histogramResolution);
            if (calculateStatistics) {
                await imgArray.Statistics.Calculate(imgArray.FlatArray);
            }

            return imgArray;
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