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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Imaging;
using Accord.Imaging.Filters;

namespace NINA.Utility.ImageAnalysis {

    public class NoBlurCannyEdgeDetector : BaseUsingCopyPartialFilter {
        private byte lowThreshold = 20;
        private byte highThreshold = 100;

        // private format translation dictionary
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations {
            get { return formatTranslations; }
        }

        /// <summary>
        /// Low threshold.
        /// </summary>
        ///
        /// <remarks><para>Low threshold value used for hysteresis
        /// (see  <a href="http://www.pages.drexel.edu/~weg22/can_tut.html">tutorial</a>
        /// for more information).</para>
        ///
        /// <para>Default value is set to <b>20</b>.</para>
        /// </remarks>
        ///
        public byte LowThreshold {
            get { return lowThreshold; }
            set { lowThreshold = value; }
        }

        /// <summary>
        /// High threshold.
        /// </summary>
        ///
        /// <remarks><para>High threshold value used for hysteresis
        /// (see  <a href="http://www.pages.drexel.edu/~weg22/can_tut.html">tutorial</a>
        /// for more information).</para>
        ///
        /// <para>Default value is set to <b>100</b>.</para>
        /// </remarks>
        ///
        public byte HighThreshold {
            get { return highThreshold; }
            set { highThreshold = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CannyEdgeDetector"/> class.
        /// </summary>
        ///
        public NoBlurCannyEdgeDetector() {
            // initialize format translation dictionary
            formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CannyEdgeDetector"/> class.
        /// </summary>
        ///
        /// <param name="lowThreshold">Low threshold.</param>
        /// <param name="highThreshold">High threshold.</param>
        ///
        public NoBlurCannyEdgeDetector(byte lowThreshold, byte highThreshold) : this() {
            this.lowThreshold = lowThreshold;
            this.highThreshold = highThreshold;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        ///
        /// <param name="source">Source image data.</param>
        /// <param name="destination">Destination image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        ///
        protected override unsafe void ProcessFilter(UnmanagedImage source, UnmanagedImage destination, Rectangle rect) {
            // processing start and stop X,Y positions
            int startX = rect.Left + 1;
            int startY = rect.Top + 1;
            int stopX = startX + rect.Width - 2;
            int stopY = startY + rect.Height - 2;

            int width = rect.Width - 2;
            int height = rect.Height - 2;

            int dstStride = destination.Stride;
            int srcStride = source.Stride;

            int dstOffset = dstStride - rect.Width + 2;
            int srcOffset = srcStride - rect.Width + 2;

            // pixel's value and gradients
            int gx, gy;
            //
            double orientation, toAngle = 180.0 / System.Math.PI;
            float leftPixel = 0, rightPixel = 0;

            // orientation array
            byte[] orients = new byte[width * height];
            // gradients array
            float[,] gradients = new float[source.Width, source.Height];
            float maxGradient = float.NegativeInfinity;

            // do the job
            byte* src = (byte*)source.ImageData.ToPointer();
            // allign pointer
            src += srcStride * startY + startX;

            // STEP 1 - calculate magnitude and edge orientation
            int p = 0;

            // for each line
            for (int y = startY; y < stopY; y++) {
                // for each pixel
                for (int x = startX; x < stopX; x++, src++, p++) {
                    gx = src[-srcStride + 1] + src[srcStride + 1]
                       - src[-srcStride - 1] - src[srcStride - 1]
                       + 2 * (src[1] - src[-1]);

                    gy = src[-srcStride - 1] + src[-srcStride + 1]
                       - src[srcStride - 1] - src[srcStride + 1]
                       + 2 * (src[-srcStride] - src[srcStride]);

                    // get gradient value
                    gradients[x, y] = (float)Math.Sqrt(gx * gx + gy * gy);
                    if (gradients[x, y] > maxGradient)
                        maxGradient = gradients[x, y];

                    // --- get orientation
                    if (gx == 0) {
                        // can not divide by zero
                        orientation = (gy == 0) ? 0 : 90;
                    } else {
                        double div = (double)gy / gx;

                        // handle angles of the 2nd and 4th quads
                        if (div < 0) {
                            orientation = 180 - System.Math.Atan(-div) * toAngle;
                        }
                        // handle angles of the 1st and 3rd quads
                        else {
                            orientation = System.Math.Atan(div) * toAngle;
                        }

                        // get closest angle from 0, 45, 90, 135 set
                        if (orientation < 22.5)
                            orientation = 0;
                        else if (orientation < 67.5)
                            orientation = 45;
                        else if (orientation < 112.5)
                            orientation = 90;
                        else if (orientation < 157.5)
                            orientation = 135;
                        else orientation = 0;
                    }

                    // save orientation
                    orients[p] = (byte)orientation;
                }
                src += srcOffset;
            }

            // STEP 2 - suppres non maximums
            byte* dst = (byte*)destination.ImageData.ToPointer();
            // allign pointer
            dst += dstStride * startY + startX;

            p = 0;

            // for each line
            for (int y = startY; y < stopY; y++) {
                // for each pixel
                for (int x = startX; x < stopX; x++, dst++, p++) {
                    // get two adjacent pixels
                    switch (orients[p]) {
                        case 0:
                            leftPixel = gradients[x - 1, y];
                            rightPixel = gradients[x + 1, y];
                            break;

                        case 45:
                            leftPixel = gradients[x - 1, y + 1];
                            rightPixel = gradients[x + 1, y - 1];
                            break;

                        case 90:
                            leftPixel = gradients[x, y + 1];
                            rightPixel = gradients[x, y - 1];
                            break;

                        case 135:
                            leftPixel = gradients[x + 1, y + 1];
                            rightPixel = gradients[x - 1, y - 1];
                            break;
                    }
                    // compare current pixels value with adjacent pixels
                    if ((gradients[x, y] < leftPixel) || (gradients[x, y] < rightPixel)) {
                        *dst = 0;
                    } else {
                        *dst = (byte)(gradients[x, y] / maxGradient * 255);
                    }
                }
                dst += dstOffset;
            }

            // STEP 3 - hysteresis
            dst = (byte*)destination.ImageData.ToPointer();
            // allign pointer
            dst += dstStride * startY + startX;

            // for each line
            for (int y = startY; y < stopY; y++) {
                // for each pixel
                for (int x = startX; x < stopX; x++, dst++) {
                    if (*dst < highThreshold) {
                        if (*dst < lowThreshold) {
                            // non edge
                            *dst = 0;
                        } else {
                            // check 8 neighboring pixels
                            if ((dst[-1] < highThreshold) &&
                                (dst[1] < highThreshold) &&
                                (dst[-dstStride - 1] < highThreshold) &&
                                (dst[-dstStride] < highThreshold) &&
                                (dst[-dstStride + 1] < highThreshold) &&
                                (dst[dstStride - 1] < highThreshold) &&
                                (dst[dstStride] < highThreshold) &&
                                (dst[dstStride + 1] < highThreshold)) {
                                *dst = 0;
                            }
                        }
                    }
                }
                dst += dstOffset;
            }

            // STEP 4 - draw black rectangle to remove those pixels, which were not processed
            // (this needs to be done for those cases, when filter is applied "in place" -
            //  source image is modified instead of creating new copy)
            Drawing.Rectangle(destination, rect, Color.Black);

            // release blurred image
            source.Dispose();
        }
    }
}