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
using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {
    // Gaussian blur filter.
    // Local 8bpp implementation aligned to the original Accord.Imaging.Filters.GaussianBlur
    // from Accord.Imaging/AForge.Imaging/Filters/Convolution/GaussianBlur.cs.
    // The optimization is structural only: full-kernel interior pixels use a cheaper path, while border
    // pixels stay on the generic clipped-kernel path because their divisor depends on which coefficients
    // are actually in-bounds.
    internal sealed class GaussianBlur {
        private readonly Accord.Imaging.Filters.GaussianBlur filter;

        public double Sigma {
            get => filter.Sigma;
            set => filter.Sigma = value;
        }

        public int Size {
            get => filter.Size;
            set => filter.Size = value;
        }

        internal int[,] Kernel => filter.Kernel;
        internal int Divisor => filter.Divisor;
        internal int Threshold => filter.Threshold;
        internal bool DynamicDivisorForEdges => filter.DynamicDivisorForEdges;
        internal bool ProcessAlpha {
            get => filter.ProcessAlpha;
            set => filter.ProcessAlpha = value;
        }

        public GaussianBlur() {
            filter = new Accord.Imaging.Filters.GaussianBlur();
        }

        public GaussianBlur(double sigma) : this() {
            Sigma = sigma;
        }

        public GaussianBlur(double sigma, int size) : this() {
            Sigma = sigma;
            Size = size;
        }

        public unsafe UnmanagedImage Apply(UnmanagedImage sourceData) {
            System.Diagnostics.Debug.Assert(sourceData.PixelFormat == PixelFormat.Format8bppIndexed,
                "CannyEdgeDetector only accepts 8bpp grayscale input, so this exact blur should not see other formats.");

            if (sourceData.PixelFormat != PixelFormat.Format8bppIndexed) {
                // Defensive fallback for unexpected direct callers. The detector rejects other formats before
                // this point, so production code should stay on the exact 8bpp path above.
                return filter.Apply(sourceData);
            }

            UnmanagedImage destinationData = UnmanagedImage.Create(sourceData.Width, sourceData.Height, sourceData.PixelFormat);

            int width = sourceData.Width;
            int height = sourceData.Height;
            int srcStride = sourceData.Stride;
            int dstStride = destinationData.Stride;

            int[,] kernel = Kernel;
            int size = kernel.GetLength(0);
            int radius = size >> 1;
            int divisor = Divisor;
            int threshold = Threshold;
            bool dynamicDivisorForEdges = DynamicDivisorForEdges;
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
}
