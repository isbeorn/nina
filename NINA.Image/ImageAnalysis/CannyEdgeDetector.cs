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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {
    /// <summary>
    /// Accord bit-exact Canny implementation for NINA. The optimized path keeps the legacy arithmetic and
    /// scan-order-sensitive behavior, but splits the pipeline so only the read-only stages run in
    /// parallel while blur borders and hysteresis stay on exact legacy semantics.
    /// </summary>
    public class CannyEdgeDetector : BaseUsingCopyPartialFilter {
        private readonly GaussianBlur gaussianFilter = new GaussianBlur();
        private byte lowThreshold = 20;
        private byte highThreshold = 100;

        private readonly Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();

        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations => formatTranslations;

        public byte LowThreshold {
            get => lowThreshold;
            set => lowThreshold = value;
        }

        public byte HighThreshold {
            get => highThreshold;
            set => highThreshold = value;
        }

        public double GaussianSigma {
            get => gaussianFilter.Sigma;
            set => gaussianFilter.Sigma = value;
        }

        public int GaussianSize {
            get => gaussianFilter.Size;
            set => gaussianFilter.Size = value;
        }

        public CannyEdgeDetector() {
            formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
        }

        public CannyEdgeDetector(byte lowThreshold, byte highThreshold) : this() {
            this.lowThreshold = lowThreshold;
            this.highThreshold = highThreshold;
        }

        public CannyEdgeDetector(byte lowThreshold, byte highThreshold, double sigma) : this() {
            this.lowThreshold = lowThreshold;
            this.highThreshold = highThreshold;
            gaussianFilter.Sigma = sigma;
        }

        protected override unsafe void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData, Rectangle rect) {
            // Blur first with the local exact 8bpp implementation. It produces the same blurred pixels
            // as the legacy Accord path, but reorganizes the work so the common interior pixels can be
            // processed more cheaply before the shared Canny core consumes the result.
            using (UnmanagedImage blurredImage = ExactGaussianBlur8bpp.Apply(sourceData, gaussianFilter)) {
                CannyEdgeDetectorCore.Process(blurredImage, destinationData, rect, lowThreshold, highThreshold);
            }
        }
    }

    /// <summary>
    /// Local copy of Accord's 8bpp Gaussian convolution used by the NINA Canny wrapper.
    /// It preserves the same kernel, divisor, threshold, and clipped-edge behavior as the legacy
    /// implementation. The optimization is structural only: full-kernel interior pixels use a cheaper
    /// path, while border pixels stay on the generic clipped-kernel path because their divisor depends
    /// on which coefficients are actually in-bounds.
    /// </summary>
    internal static class ExactGaussianBlur8bpp {
        public static unsafe UnmanagedImage Apply(UnmanagedImage sourceData, GaussianBlur gaussianFilter) {
            System.Diagnostics.Debug.Assert(sourceData.PixelFormat == PixelFormat.Format8bppIndexed,
                "CannyEdgeDetector only accepts 8bpp grayscale input, so this exact blur should not see other formats.");

            if (sourceData.PixelFormat != PixelFormat.Format8bppIndexed) {
                // Defensive fallback for unexpected direct callers. The detector rejects other formats before
                // this point, so production code should stay on the exact 8bpp path above.
                return gaussianFilter.Apply(sourceData);
            }

            UnmanagedImage destinationData = UnmanagedImage.Create(sourceData.Width, sourceData.Height, sourceData.PixelFormat);

            int width = sourceData.Width;
            int height = sourceData.Height;
            int srcStride = sourceData.Stride;
            int dstStride = destinationData.Stride;

            int[,] kernel = gaussianFilter.Kernel;
            int size = kernel.GetLength(0);
            int radius = size >> 1;
            int divisor = gaussianFilter.Divisor;
            int threshold = gaussianFilter.Threshold;
            bool dynamicDivisorForEdges = gaussianFilter.DynamicDivisorForEdges;
            // Prepare data that never changes during this blur invocation. This keeps the math identical,
            // but removes repeated kernel indexing and row-address setup from every output pixel.
            int[] flatKernel = FlattenKernel(kernel);
            int[] rowOffsets = CreateRowOffsets(size, radius, srcStride);

            byte* srcBase = (byte*)sourceData.ImageData.ToPointer();
            byte* dstBase = (byte*)destinationData.ImageData.ToPointer();
            // Each blur output row reads only from the immutable source image, so rows can be processed
            // independently. Leave one logical processor free so this hot path does not monopolize the machine.
            var parallelRows = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

            // Split each row into border and interior spans. Only the interior span can assume that the
            // entire kernel window is in-bounds, which lets it skip clipping logic without changing results.
            Parallel.For(0, height, parallelRows, y => {
                byte* src = srcBase + y * srcStride;
                byte* dst = dstBase + y * dstStride;

                // Top and bottom border rows, plus tiny images, never see the full kernel window.
                // In those cases the whole row must stay on the clipped legacy path.
                if ((y < radius) || (y >= height - radius) || (width <= (radius * 2)) || (height <= (radius * 2))) {
                    for (int x = 0; x < width; x++, dst++) {
                        *dst = ComputeBorderPixel(srcBase, srcStride, x, y, width, height, kernel, size, radius, divisor, threshold, dynamicDivisorForEdges);
                    }
                    return;
                }

                for (int x = 0; x < radius; x++, dst++) {
                    *dst = ComputeBorderPixel(srcBase, srcStride, x, y, width, height, kernel, size, radius, divisor, threshold, dynamicDivisorForEdges);
                }

                for (int x = radius; x < width - radius; x++, dst++) {
                    *dst = ComputeInteriorPixel(src, x, flatKernel, rowOffsets, size, radius, divisor, threshold);
                }

                for (int x = width - radius; x < width; x++, dst++) {
                    *dst = ComputeBorderPixel(srcBase, srcStride, x, y, width, height, kernel, size, radius, divisor, threshold, dynamicDivisorForEdges);
                }
            });

            return destinationData;
        }

        // Accord stores the Gaussian kernel as a 2D array. Flatten it once so the interior loop can walk
        // coefficients with a single linear index instead of paying multidimensional-array access costs
        // for every output pixel.
        private static int[] FlattenKernel(int[,] kernel) {
            int size = kernel.GetLength(0);
            int[] flatKernel = new int[size * size];
            int index = 0;

            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    flatKernel[index++] = kernel[i, j];
                }
            }

            return flatKernel;
        }

        // Precompute each kernel row's byte offset relative to the window origin. The interior loop then
        // reuses these offsets directly instead of rebuilding row addresses for every kernel coefficient.
        private static int[] CreateRowOffsets(int size, int radius, int stride) {
            int[] rowOffsets = new int[size];

            for (int i = 0; i < size; i++) {
                rowOffsets[i] = (i - radius) * stride;
            }

            return rowOffsets;
        }

        // The caller only reaches this path when the full kernel window is inside the image bounds.
        // That lets the code run the same 2D integer convolution without per-tap bounds checks or
        // edge-divisor handling, while preserving the legacy result exactly.
        private static unsafe byte ComputeInteriorPixel(byte* srcRow, int x, int[] flatKernel, int[] rowOffsets, int size, int radius, int divisor, int threshold) {
            long g = 0;
            byte* window = srcRow + x - radius;
            int kernelIndex = 0;

            for (int i = 0; i < size; i++) {
                byte* src = window + rowOffsets[i];

                for (int j = 0; j < size; j++) {
                    g += flatKernel[kernelIndex++] * src[j];
                }
            }

            if (divisor != 0) {
                g /= divisor;
            }

            g += threshold;
            return (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));
        }

        // Borders intentionally keep the clipped-kernel logic from the legacy convolution. Edge pixels
        // may see only part of the kernel, so their divisor and accumulated sum depend on which taps are
        // actually in-bounds.
        private static unsafe byte ComputeBorderPixel(byte* srcBase, int srcStride, int x, int y, int width, int height, int[,] kernel, int size, int radius, int divisor, int threshold, bool dynamicDivisorForEdges) {
            long g = 0;
            long div = 0;
            int processedKernelSize = 0;
            int kernelSize = size * size;

            for (int i = 0; i < size; i++) {
                int row = y + i - radius;
                if (row < 0) {
                    continue;
                }

                if (row >= height) {
                    break;
                }

                byte* src = srcBase + row * srcStride;
                for (int j = 0; j < size; j++) {
                    int column = x + j - radius;
                    if (column < 0) {
                        continue;
                    }

                    if (column < width) {
                        int k = kernel[i, j];
                        div += k;
                        g += k * src[column];
                        processedKernelSize++;
                    }
                }
            }

            if (processedKernelSize == kernelSize) {
                div = divisor;
            } else if (!dynamicDivisorForEdges) {
                div = divisor;
            }

            if (div != 0) {
                g /= div;
            }

            g += threshold;
            return (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));
        }
    }

    /// <summary>
    /// Shared Canny core for the blurred and no-blur wrappers. Gradient/orientation and non-maximum
    /// suppression are parallel because they read immutable buffers; hysteresis stays serial because it
    /// reads and mutates the destination image in scan order.
    /// </summary>
    internal static class CannyEdgeDetectorCore {
        public static unsafe void Process(UnmanagedImage source, UnmanagedImage destination, Rectangle rect, byte lowThreshold, byte highThreshold) {
            int startX = rect.Left + 1;
            int startY = rect.Top + 1;
            int stopX = startX + rect.Width - 2;
            int stopY = startY + rect.Height - 2;

            int width = rect.Width - 2;
            int height = rect.Height - 2;

            int dstStride = destination.Stride;
            int srcStride = source.Stride;
            int dstOffset = dstStride - rect.Width + 2;

            int sourceWidth = source.Width;

            byte[] orients = new byte[width * height];
            // Keep gradients in a flat row-major buffer so both parallel passes use contiguous indexing
            // instead of the extra bounds and indirection costs of a 2D array.
            float[] gradients = new float[sourceWidth * source.Height];
            // Each worker tracks its own row maximum, then the serial reduction combines them into the
            // global normalization factor used by the non-maximum suppression output scaling.
            float[] rowMaxima = new float[height];
            // Leave one logical processor free so this hot path speeds up without fully saturating
            // the machine while other imaging or UI work is happening at the same time.
            var parallelRows = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

            byte* srcBase = (byte*)source.ImageData.ToPointer();

            // Stage 1 is read-only over the source image, so rows can be processed independently.
            Parallel.For(0, height, parallelRows, row => {
                int y = startY + row;
                int orientIndex = row * width;
                int gradientRow = y * sourceWidth;
                float maxGradient = float.NegativeInfinity;
                byte* src = srcBase + y * srcStride + startX;

                for (int x = startX; x < stopX; x++, src++, orientIndex++) {
                    int gx = src[-srcStride + 1] + src[srcStride + 1]
                           - src[-srcStride - 1] - src[srcStride - 1]
                           + 2 * (src[1] - src[-1]);

                    int gy = src[-srcStride - 1] + src[-srcStride + 1]
                           - src[srcStride - 1] - src[srcStride + 1]
                           + 2 * (src[-srcStride] - src[srcStride]);

                    float gradient = (float)Math.Sqrt(gx * gx + gy * gy);
                    gradients[gradientRow + x] = gradient;
                    if (gradient > maxGradient) {
                        maxGradient = gradient;
                    }

                    double orientation;
                    if (gx == 0) {
                        orientation = (gy == 0) ? 0 : 90;
                    } else {
                        double div = (double)gy / gx;
                        double toAngle = 180.0 / Math.PI;

                        if (div < 0) {
                            orientation = 180 - Math.Atan(-div) * toAngle;
                        } else {
                            orientation = Math.Atan(div) * toAngle;
                        }

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

                    orients[orientIndex] = (byte)orientation;
                }

                rowMaxima[row] = maxGradient;
            });

            float maxGradient = float.NegativeInfinity;
            for (int i = 0; i < rowMaxima.Length; i++) {
                if (rowMaxima[i] > maxGradient) {
                    maxGradient = rowMaxima[i];
                }
            }

            byte* dstBase = (byte*)destination.ImageData.ToPointer();

            // Stage 2 only reads the gradient/orientation buffers and writes one destination row, so it
            // is also safe to parallelize without affecting the legacy result.
            Parallel.For(0, height, parallelRows, row => {
                int y = startY + row;
                int orientIndex = row * width;
                int gradientRow = y * sourceWidth;
                int gradientRowAbove = gradientRow - sourceWidth;
                int gradientRowBelow = gradientRow + sourceWidth;
                byte* dst = dstBase + y * dstStride + startX;

                for (int x = startX; x < stopX; x++, dst++, orientIndex++) {
                    float leftPixel = 0;
                    float rightPixel = 0;

                    switch (orients[orientIndex]) {
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

                    float gradient = gradients[gradientRow + x];
                    if ((gradient < leftPixel) || (gradient < rightPixel)) {
                        *dst = 0;
                    } else {
                        *dst = (byte)(gradient / maxGradient * 255);
                    }
                }
            });

            // Hysteresis must stay serial because each decision reads neighboring pixels from the same
            // destination buffer that this pass is mutating in scan order.
            byte* hysteresis = dstBase + dstStride * startY + startX;
            for (int y = startY; y < stopY; y++) {
                for (int x = startX; x < stopX; x++, hysteresis++) {
                    if (*hysteresis < highThreshold) {
                        if (*hysteresis < lowThreshold) {
                            *hysteresis = 0;
                        } else if ((hysteresis[-1] < highThreshold) &&
                                   (hysteresis[1] < highThreshold) &&
                                   (hysteresis[-dstStride - 1] < highThreshold) &&
                                   (hysteresis[-dstStride] < highThreshold) &&
                                   (hysteresis[-dstStride + 1] < highThreshold) &&
                                   (hysteresis[dstStride - 1] < highThreshold) &&
                                   (hysteresis[dstStride] < highThreshold) &&
                                   (hysteresis[dstStride + 1] < highThreshold)) {
                            *hysteresis = 0;
                        }
                    }
                }
                hysteresis += dstOffset;
            }

            Drawing.Rectangle(destination, rect, Color.Black);
        }
    }


}
