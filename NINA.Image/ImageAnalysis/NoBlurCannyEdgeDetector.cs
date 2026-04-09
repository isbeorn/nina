#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using Accord.Imaging.Filters;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {
    /// <summary>
    /// Legacy no-blur Canny implementation. The extracted static overload keeps the optimized core in
    /// this file so the blurred detector can reuse it without changing the no-blur algorithm shape.
    /// </summary>
    public class NoBlurCannyEdgeDetector : BaseUsingCopyPartialFilter {
        private byte lowThreshold = 20;
        private byte highThreshold = 100;

        // private format translation dictionary
        private readonly Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations => formatTranslations;

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
            get => lowThreshold;
            set => lowThreshold = value;
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
            get => highThreshold;
            set => highThreshold = value;
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
            ProcessFilter(source, destination, rect, lowThreshold, highThreshold);
        }

        // Shared no-blur Canny core. The extracted overload keeps the original processing layout and
        // comments close to the legacy source, while the optimized implementation still parallelizes the
        // read-only stages internally.
        internal static unsafe void ProcessFilter(UnmanagedImage source, UnmanagedImage destination, Rectangle rect, byte lowThreshold, byte highThreshold) {
            Debug.Assert(rect.Width >= 2 && rect.Height >= 2, "CannyEdgeDetector expects a processing rectangle with at least 2x2 pixels.");

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

            int sourceWidth = source.Width;

            // orientation array
            byte[] orients = new byte[width * height];
            // gradients array
            // kept flattened for performance, but still indexed in row-major order
            float[] gradients = new float[sourceWidth * source.Height];
            // per-row maxima are reduced after the parallel pass to preserve the legacy global scaling
            float[] rowMaxima = new float[height];
            var parallelRows = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

            byte* srcBase = (byte*)source.ImageData.ToPointer();

            // STEP 1 - calculate magnitude and edge orientation
            // for each line
            Parallel.For(0, height, parallelRows, row => {
                int y = startY + row;
                int p = row * width;
                int gradientRow = y * sourceWidth;
                float maxGradient = float.NegativeInfinity;
                double toAngle = 180.0 / System.Math.PI;

                // do the job
                byte* src = srcBase;

                // allign pointer
                src += srcStride * startY + startX;
                src += row * (width + srcOffset);

                // for each pixel
                for (int x = startX; x < stopX; x++, src++, p++) {
                    int gx = src[-srcStride + 1] + src[srcStride + 1]
                       - src[-srcStride - 1] - src[srcStride - 1]
                       + 2 * (src[1] - src[-1]);

                    int gy = src[-srcStride - 1] + src[-srcStride + 1]
                       - src[srcStride - 1] - src[srcStride + 1]
                       + 2 * (src[-srcStride] - src[srcStride]);

                    // get gradient value
                    float gradient = (float)System.Math.Sqrt(gx * gx + gy * gy);
                    gradients[gradientRow + x] = gradient;
                    if (gradient > maxGradient) {
                        maxGradient = gradient;
                    }

                    // --- get orientation
                    double orientation;
                    if (gx == 0) {
                        // can not divide by zero
                        orientation = (gy == 0) ? 0 : 90;
                    } else {
                        double div = (double)gy / gx;

                        // handle angles of the 2nd and 4th quads
                        if (div < 0) {
                            orientation = 180 - System.Math.Atan(-div) * toAngle;
                        } else {
                            // handle angles of the 1st and 3rd quads
                            orientation = System.Math.Atan(div) * toAngle;
                        }

                        // get closest angle from 0, 45, 90, 135 set
                        if (orientation < 22.5) {
                            orientation = 0;
                        } else if (orientation < 67.5) {
                            orientation = 45;
                        } else if (orientation < 112.5) {
                            orientation = 90;
                        } else if (orientation < 157.5) {
                            orientation = 135;
                        } else {
                            orientation = 0;
                        }
                    }

                    // save orientation
                    orients[p] = (byte)orientation;
                }

                rowMaxima[row] = maxGradient;
            });

            // Reduce the per-row maxima after the parallel pass so the final normalization uses the same
            // single global maximum as the legacy serial implementation.
            float maxGradient = float.NegativeInfinity;
            for (int i = 0; i < rowMaxima.Length; i++) {
                if (rowMaxima[i] > maxGradient) {
                    maxGradient = rowMaxima[i];
                }
            }

            byte* dstBase = (byte*)destination.ImageData.ToPointer();

            // STEP 2 - suppress non maximums
            // for each line
            Parallel.For(0, height, parallelRows, row => {
                int y = startY + row;
                int p = row * width;
                int gradientRow = y * sourceWidth;
                int gradientRowAbove = gradientRow - sourceWidth;
                int gradientRowBelow = gradientRow + sourceWidth;
                float leftPixel = 0;
                float rightPixel = 0;

                // do the job
                byte* dst = dstBase;

                // allign pointer
                dst += dstStride * startY + startX;
                dst += row * (width + dstOffset);

                // for each pixel
                for (int x = startX; x < stopX; x++, dst++, p++) {
                    // get two adjacent pixels
                    switch (orients[p]) {
                        case 0:
                            leftPixel = gradients[gradientRow + x - 1];
                            rightPixel = gradients[gradientRow + x + 1];
                            break;

                        case 45:
                            leftPixel = gradients[gradientRowBelow + x - 1];
                            rightPixel = gradients[gradientRowAbove + x + 1];
                            break;

                        case 90:
                            leftPixel = gradients[gradientRowBelow + x];
                            rightPixel = gradients[gradientRowAbove + x];
                            break;

                        case 135:
                            leftPixel = gradients[gradientRowBelow + x + 1];
                            rightPixel = gradients[gradientRowAbove + x - 1];
                            break;
                    }

                    // compare current pixels value with adjacent pixels
                    float gradient = gradients[gradientRow + x];
                    if ((gradient < leftPixel) || (gradient < rightPixel)) {
                        *dst = 0;
                    } else {
                        *dst = (byte)(gradient / maxGradient * 255);
                    }
                }
            });

            // STEP 3 - hysteresis
            byte* dst = dstBase;

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
        }
    }
}
