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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OptimizedBlurredCannyEdgeDetector = NINA.Image.ImageAnalysis.CannyEdgeDetector;
using OptimizedGaussianBlur = NINA.Image.ImageAnalysis.GaussianBlur;
using OptimizedNoBlurCannyEdgeDetector = NINA.Image.ImageAnalysis.NoBlurCannyEdgeDetector;

namespace NINA.Test.Image.ImageAnalysis {
    public abstract class CannyAndGaussianProofTestSupport {
        // One category keeps these proof-style checks easy to run as a group.
        // The tests are deliberately small, so this category is safe for normal local runs.
        protected const string ProofCategory = "CannyAndGaussianProof";

        // The low and high thresholds are kept far enough apart to create all three
        // Canny hysteresis outcomes: below low, weak between thresholds, and strong.
        protected const byte DefaultLowThreshold = 10;
        protected const byte DefaultHighThreshold = 80;

        // Match the default blurred Canny settings used by the production filter.
        // A 5x5 kernel has radius 2, which makes the tiny-image and border geometry easy to reason about.
        protected const double DefaultGaussianSigma = 1.4;
        protected const int DefaultGaussianSize = 5;

        // A math scenario is a compact image recipe. The image is synthetic, but both
        // the reference filter and the optimized filter receive exactly the same bytes.
        public sealed class MathScenario {
            public required string Name { get; init; }
            public required int Width { get; init; }
            public required int Height { get; init; }
            public required Func<int, int, byte[]> CreatePixels { get; init; }
            public Rectangle? Rect { get; init; }
            public byte LowThreshold { get; init; } = DefaultLowThreshold;
            public byte HighThreshold { get; init; } = DefaultHighThreshold;
        }

        protected static byte[] RunReferenceNoBlurCanny(byte[] sourcePixels, int width, int height, Rectangle rect, byte lowThreshold, byte highThreshold) {
            // The reference no-blur implementation mutates a bitmap in place, so create a fresh
            // 8bpp bitmap for every call and read the bytes back after ApplyInPlace returns.
            using Bitmap bitmap = CreateGray8Bitmap(width, height, sourcePixels);
            var detector = new global::NINA.Tests.NoBlurCannyEdgeDetector(lowThreshold, highThreshold);

            detector.ApplyInPlace(bitmap, rect);
            return ReadGray8Pixels(bitmap);
        }

        protected static byte[] RunOptimizedNoBlurCanny(byte[] sourcePixels, int width, int height, Rectangle rect, byte lowThreshold, byte highThreshold) {
            // The optimized path gets the same bitmap shape, palette, stride padding, thresholds,
            // and rectangle as the reference path above.
            using Bitmap bitmap = CreateGray8Bitmap(width, height, sourcePixels);
            var detector = new OptimizedNoBlurCannyEdgeDetector(lowThreshold, highThreshold);

            detector.ApplyInPlace(bitmap, rect);
            return ReadGray8Pixels(bitmap);
        }

        protected static byte[] RunReferenceBlurredCanny(byte[] sourcePixels, int width, int height, Rectangle rect, byte lowThreshold, byte highThreshold, double gaussianSigma, int gaussianSize) {
            // Accord's blurred Canny is the reference for the production blurred Canny filter.
            using Bitmap bitmap = CreateGray8Bitmap(width, height, sourcePixels);
            var detector = new Accord.Imaging.Filters.CannyEdgeDetector(lowThreshold, highThreshold) {
                GaussianSigma = gaussianSigma,
                GaussianSize = gaussianSize
            };

            detector.ApplyInPlace(bitmap, rect);
            return ReadGray8Pixels(bitmap);
        }

        protected static byte[] RunOptimizedBlurredCanny(byte[] sourcePixels, int width, int height, Rectangle rect, byte lowThreshold, byte highThreshold, double gaussianSigma, int gaussianSize) {
            // Same blurred path through the optimized implementation. Keeping setup identical
            // avoids hiding a filter difference behind bitmap construction differences.
            using Bitmap bitmap = CreateGray8Bitmap(width, height, sourcePixels);
            var detector = new OptimizedBlurredCannyEdgeDetector(lowThreshold, highThreshold) {
                GaussianSigma = gaussianSigma,
                GaussianSize = gaussianSize
            };

            detector.ApplyInPlace(bitmap, rect);
            return ReadGray8Pixels(bitmap);
        }

        protected static byte[] RunReferenceGaussianBlur(byte[] sourcePixels, int width, int height, double gaussianSigma, int gaussianSize) {
            // GaussianBlur.Apply returns an unmanaged image. Read the whole buffer because stride
            // and padding are part of what can differ between image implementations.
            using UnmanagedImage source = CreateGray8Image(width, height, sourcePixels);
            var filter = new Accord.Imaging.Filters.GaussianBlur {
                Sigma = gaussianSigma,
                Size = gaussianSize
            };

            using UnmanagedImage output = filter.Apply(source);
            return ReadUnmanagedImageBytes(output);
        }

        protected static byte[] RunOptimizedGaussianBlur(byte[] sourcePixels, int width, int height, double gaussianSigma, int gaussianSize) {
            // Same unmanaged input shape as Accord receives, then compare the full output buffer.
            using UnmanagedImage source = CreateGray8Image(width, height, sourcePixels);
            var filter = new OptimizedGaussianBlur {
                Sigma = gaussianSigma,
                Size = gaussianSize
            };

            using UnmanagedImage output = filter.Apply(source);
            return ReadUnmanagedImageBytes(output);
        }

        protected static byte[] CreateLinearPlane(int width, int height, int baseValue, int xStep, int yStep) {
            // Simple plane: pixel(x,y) = base + x*xStep + y*yStep. This gives predictable
            // Sobel directions without hand-writing a full byte array.
            byte[] pixels = new byte[width * height];

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    // Clamp instead of wrapping so high or low slopes do not introduce accidental edges.
                    pixels[ToIndex(x, y, width)] = ClampToByte(baseValue + x * xStep + y * yStep);
                }
            }

            return pixels;
        }

        protected static byte[] CreateHorizontalStripe(int width, int height, int row, byte value) {
            // A one-row stripe is a compact way to move the strongest Sobel response vertically.
            // The caller chooses top, middle, or bottom source row depending on the max-gradient case.
            byte[] pixels = new byte[width * height];
            int clampedRow = Math.Clamp(row, 0, height - 1);

            for (int x = 0; x < width; x++) {
                pixels[ToIndex(x, clampedRow, width)] = value;
            }

            return pixels;
        }

        protected static byte[] CreateRingAndSpikes(int width, int height) {
            // Start with a gentle plane so the background is deterministic and not completely flat.
            byte[] pixels = CreateLinearPlane(width, height, 36, 2, 1);
            int centerX = width / 2;
            int centerY = height / 2;

            // Use Chebyshev distance so the "ring" is square. That gives horizontal, vertical,
            // and diagonal edge neighborhoods in a small image.
            for (int y = 2; y < height - 2; y++) {
                for (int x = 2; x < width - 2; x++) {
                    int dx = Math.Abs(x - centerX);
                    int dy = Math.Abs(y - centerY);
                    int distance = Math.Max(dx, dy);

                    if (distance == 4 || distance == 5) {
                        // Bright square ring: strong edge candidates.
                        pixels[ToIndex(x, y, width)] = 220;
                    } else if (distance <= 2) {
                        // Softer center: creates contrast without making the whole image binary.
                        pixels[ToIndex(x, y, width)] = 110;
                    }
                }
            }

            // Alternating horizontal spikes create local maxima and local competitors for
            // non-maximum suppression along the center row.
            for (int x = 3; x < width - 3; x++) {
                pixels[ToIndex(x, centerY, width)] = (x % 2 == 0) ? (byte)245 : (byte)70;
            }

            // Alternating vertical spikes do the same for the center column.
            for (int y = 3; y < height - 3; y++) {
                pixels[ToIndex(centerX, y, width)] = (y % 2 == 0) ? (byte)235 : (byte)65;
            }

            return pixels;
        }

        private static Bitmap CreateGray8Bitmap(int width, int height, byte[] pixels) {
            // Bitmap-based Canny filters operate on indexed 8bpp images. Validate the packed
            // source buffer before copying it into a potentially padded bitmap stride.
            if (pixels.Length != width * height) {
                throw new ArgumentException("Source pixel array length does not match image dimensions.", nameof(pixels));
            }

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            try {
                // Build a real grayscale palette so byte value N displays and processes as intensity N.
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++) {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }

                bitmap.Palette = palette;

                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                try {
                    // Bitmap stride can be wider than image width. Fill padding with a sentinel
                    // value so accidental reads from padding become visible in byte comparisons.
                    int stride = Math.Abs(data.Stride);
                    byte[] row = new byte[stride];

                    for (int y = 0; y < height; y++) {
                        Array.Fill(row, (byte)0xCD);
                        Buffer.BlockCopy(pixels, y * width, row, 0, width);
                        Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, data.Stride * y), row.Length);
                    }
                } finally {
                    bitmap.UnlockBits(data);
                }

                return bitmap;
            } catch {
                bitmap.Dispose();
                throw;
            }
        }

        private static byte[] ReadGray8Pixels(Bitmap bitmap) {
            // Read only visible pixels back into a tightly packed buffer. Padding bytes are not
            // part of the logical image comparison.
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            try {
                int width = bitmap.Width;
                int height = bitmap.Height;
                int stride = Math.Abs(data.Stride);
                byte[] row = new byte[stride];
                byte[] pixels = new byte[width * height];

                for (int y = 0; y < height; y++) {
                    Marshal.Copy(IntPtr.Add(data.Scan0, data.Stride * y), row, 0, row.Length);
                    Buffer.BlockCopy(row, 0, pixels, y * width, width);
                }

                return pixels;
            } finally {
                bitmap.UnlockBits(data);
            }
        }

        private static UnmanagedImage CreateGray8Image(int width, int height, byte[] pixels) {
            // GaussianBlur.Apply works on Accord UnmanagedImage, so this creates the same 8bpp
            // source layout for both reference and optimized Gaussian filters.
            if (pixels.Length != width * height) {
                throw new ArgumentException("Source pixel array length does not match image dimensions.", nameof(pixels));
            }

            UnmanagedImage image = UnmanagedImage.Create(width, height, PixelFormat.Format8bppIndexed);

            try {
                // UnmanagedImage also has stride padding. Initialize all bytes, then copy only
                // the logical image width into each row.
                int stride = image.Stride;
                byte[] buffer = new byte[stride * height];
                Array.Fill(buffer, (byte)0xCD);

                for (int y = 0; y < height; y++) {
                    Buffer.BlockCopy(pixels, y * width, buffer, y * stride, width);
                }

                Marshal.Copy(buffer, 0, image.ImageData, buffer.Length);
                return image;
            } catch {
                image.Dispose();
                throw;
            }
        }

        private static byte[] ReadUnmanagedImageBytes(UnmanagedImage image) {
            // For unmanaged images, compare the full buffer including stride padding because
            // Apply returns a complete image object rather than a tight byte array.
            byte[] buffer = new byte[image.NumberOfBytes];
            Marshal.Copy(image.ImageData, buffer, 0, buffer.Length);
            return buffer;
        }

        private static int ToIndex(int x, int y, int width) {
            // All synthetic fixtures use tight row-major indexing.
            return y * width + x;
        }

        private static byte ClampToByte(int value) {
            // Match byte output saturation used by the image filters.
            return (byte)((value > 255) ? 255 : ((value < 0) ? 0 : value));
        }
    }
}
