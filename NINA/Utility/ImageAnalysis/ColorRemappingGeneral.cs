#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using Accord.Imaging;
using Accord.Imaging.Filters;
using System;
using System.Drawing;

namespace NINA.Utility.ImageAnalysis {

    public class ColorRemappingGeneral : ColorRemapping {

        // color maps
        private ushort[] redMap;

        private ushort[] greenMap;
        private ushort[] blueMap;
        private ushort[] grayMap;

        public ushort[] RedMap16 {
            get { return redMap; }
            set {
                // check the map
                if ((value == null) || (value.Length != 65536))
                    throw new ArgumentException("A map should be array with 65536 value.");

                redMap = value;
            }
        }

        public ushort[] GreenMap16 {
            get { return greenMap; }
            set {
                // check the map
                if ((value == null) || (value.Length != 65536))
                    throw new ArgumentException("A map should be array with 65536 value.");

                greenMap = value;
            }
        }

        public ushort[] BlueMap16 {
            get { return blueMap; }
            set {
                // check the map
                if ((value == null) || (value.Length != 65536))
                    throw new ArgumentException("A map should be array with 65536 value.");

                blueMap = value;
            }
        }

        public ushort[] GrayMap16 {
            get { return grayMap; }
            set {
                // check the map
                if ((value == null) || (value.Length != 65536))
                    throw new ArgumentException("A map should be array with 65536 value.");

                grayMap = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRemapping"/> class.
        /// </summary>
        ///
        /// <remarks>Initializes the filter without any remapping. All
        /// pixel values are mapped to the same values.</remarks>
        ///
        public ColorRemappingGeneral(ushort[] redMap, ushort[] greenMap, ushort[] blueMap) {
            FormatTranslations[System.Drawing.Imaging.PixelFormat.Format48bppRgb] = System.Drawing.Imaging.PixelFormat.Format48bppRgb;
            RedMap16 = redMap;
            GreenMap16 = greenMap;
            BlueMap16 = blueMap;
        }

        public ColorRemappingGeneral(ushort[] grayMap) {
            FormatTranslations[System.Drawing.Imaging.PixelFormat.Format16bppGrayScale] = System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;
            GrayMap16 = grayMap;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        ///
        /// <param name="image">Source image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        ///
        protected override unsafe void ProcessFilter(UnmanagedImage image, Rectangle rect) {
            // processing start and stop X,Y positions
            int stopX = rect.Width;
            int stopY = rect.Height;

            // do the job
            ushort* ptr = (ushort*)image.ImageData.ToPointer();

            if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale) {
                // grayscale image
                for (int y = 0; y < stopY; y++) {
                    for (int x = 0; x < stopX; x++, ptr++) {
                        // gray
                        *ptr = grayMap[*ptr];
                    }
                }
            } else {
                // RGB image
                for (int y = 0; y < stopY; y++) {
                    for (int x = 0; x < stopX; x++, ptr += 3) {
                        // red
                        ptr[RGB.R] = redMap[ptr[RGB.R]];
                        // green
                        ptr[RGB.G] = greenMap[ptr[RGB.G]];
                        // blue
                        ptr[RGB.B] = blueMap[ptr[RGB.B]];
                    }
                }
            }
        }
    }
}