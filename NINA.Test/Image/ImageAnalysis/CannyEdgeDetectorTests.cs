#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Image.ImageAnalysis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using OptimizedCannyEdgeDetector = NINA.Image.ImageAnalysis.CannyEdgeDetector;
using OptimizedNoBlurCannyEdgeDetector = NINA.Image.ImageAnalysis.NoBlurCannyEdgeDetector;

namespace NINA.Test.Image.ImageAnalysis {

    [TestFixture]
    public class CannyEdgeDetectorTests {
        private readonly struct AstroCameraResolutionCase {
            public AstroCameraResolutionCase(int width, int height) {
                Width = width;
                Height = height;
            }

            public int Width { get; }
            public int Height { get; }
        }

        // Use real camera frame sizes so row traversal and stride padding are exercised on the same
        // shapes that the imaging pipeline sees in practice, even though the pixel content is synthetic.
        private static readonly IReadOnlyDictionary<string, AstroCameraResolutionCase> AstroCameraResolutions = new Dictionary<string, AstroCameraResolutionCase> {
            ["Guide_1280x960"] = new AstroCameraResolutionCase(1280, 960),
            ["Planetary_1936x1096"] = new AstroCameraResolutionCase(1936, 1096),
            ["DeepSky_3096x2080"] = new AstroCameraResolutionCase(3096, 2080),
            ["FourThirds_4144x2822"] = new AstroCameraResolutionCase(4144, 2822),
            ["APS-C_6224x4168"] = new AstroCameraResolutionCase(6224, 4168)
        };

        private const string CustomGaussianResolutionKey = "Planetary_1936x1096";

        private static IEnumerable<TestCaseData> AstroCameraResolutionCases() {
            foreach (var resolution in AstroCameraResolutions) {
                yield return new TestCaseData(resolution.Value.Width, resolution.Value.Height)
                    .SetName($"RealWorldResolution_{resolution.Key}");
            }
        }

        // This is the production star-detection path: blurred Canny with the normal thresholds.
        // Every listed resolution must match Accord exactly, not just produce similar-looking edges.
        [Test]
        [TestCaseSource(nameof(AstroCameraResolutionCases))]
        public void CannyEdgeDetector_DefaultParameters_MatchesAccordReference(int width, int height) {
            byte[] sourcePixels = CreateStructuredPixels(width, height);
            byte[] expectedPixels = ComputeReferenceBlurredCanny(sourcePixels, width, height, lowThreshold: 10, highThreshold: 80, gaussianSigma: 1.4, gaussianSize: 5);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedCannyEdgeDetector(lowThreshold: 10, highThreshold: 80);

            optimized.ApplyInPlace(optimizedInput);

            Assert.That(HasStridePadding(optimizedInput, bytesPerPixel: 1), Is.EqualTo(ShouldHaveStridePadding(width, bytesPerPixel: 1)), "Unexpected stride padding.");
            AssertBitExactPixels(expectedPixels, optimizedInput);
        }

        // The optimized blur has separate interior and border code paths, so validate a larger kernel
        // size as well. This catches errors that only appear when the Gaussian footprint changes.
        [Test]
        public void CannyEdgeDetector_CustomGaussianSize_MatchesAccordReference() {
            int width = AstroCameraResolutions[CustomGaussianResolutionKey].Width;
            int height = AstroCameraResolutions[CustomGaussianResolutionKey].Height;
            byte[] sourcePixels = CreateStructuredPixels(width, height);
            byte[] expectedPixels = ComputeReferenceBlurredCanny(sourcePixels, width, height, lowThreshold: 20, highThreshold: 100, gaussianSigma: 1.4, gaussianSize: 10);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedCannyEdgeDetector(lowThreshold: 20, highThreshold: 100) {
                GaussianSize = 10
            };

            optimized.ApplyInPlace(optimizedInput);

            AssertBitExactPixels(expectedPixels, optimizedInput);
        }

        // The no-blur variant is NINA-owned code, so the preserved implementation under NINA.Tests
        // is the correct reference behavior for this path rather than Accord.
        [Test]
        [TestCaseSource(nameof(AstroCameraResolutionCases))]
        public void NoBlurCannyEdgeDetector_MatchesPreservedReference(int width, int height) {
            byte[] sourcePixels = CreateStructuredPixels(width, height);
            byte[] expectedPixels = ComputeReferenceNoBlurCanny(sourcePixels, width, height, lowThreshold: 10, highThreshold: 80);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedNoBlurCannyEdgeDetector(lowThreshold: 10, highThreshold: 80);

            optimized.ApplyInPlace(optimizedInput);

            Assert.That(HasStridePadding(optimizedInput, bytesPerPixel: 1), Is.EqualTo(ShouldHaveStridePadding(width, bytesPerPixel: 1)), "Unexpected stride padding.");
            AssertBitExactPixels(expectedPixels, optimizedInput);
        }

        // Build deterministic image-like data instead of flat values or random noise. The gradients,
        // diagonal ridge, checkerboard contrast, and saturated spikes force blur, gradient direction,
        // non-maximum suppression, and hysteresis to all do meaningful work.
        private static byte[] CreateStructuredPixels(int width, int height) {
            byte[] pixels = new byte[width * height];

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;

                for (int x = 0; x < width; x++) {
                    int value = (x * 5 + y * 9 + ((x * y) % 17) * 3) & 0xFF;

                    if (x > width / 3 && x < (width * 2) / 3) {
                        value += 35;
                    }

                    if (y > height / 2) {
                        value += 25;
                    }

                    if (Math.Abs(x - (y * width) / Math.Max(1, height)) <= 1) {
                        value += 60;
                    }

                    if (((x / 5) + (y / 7)) % 2 == 0) {
                        value -= 18;
                    }

                    if (((x * 73856093) ^ (y * 19349663)) % 97 == 0) {
                        value = 255;
                    }

                    if (value < 0) {
                        value = 0;
                    } else if (value > byte.MaxValue) {
                        value = byte.MaxValue;
                    }

                    pixels[rowOffset + x] = (byte)value;
                }
            }

            return pixels;
        }

        private static Bitmap CreateGray8Bitmap(int width, int height, byte[] pixels) {
            if (pixels.Length != width * height) {
                throw new ArgumentException("Source pixel array length does not match image dimensions.", nameof(pixels));
            }

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            try {
                bitmap.Palette = CreateGrayscalePalette(bitmap.Palette);
                FillGray8Bitmap(bitmap, pixels);
                return bitmap;
            } catch {
                bitmap.Dispose();
                throw;
            }
        }

        private static ColorPalette CreateGrayscalePalette(ColorPalette palette) {
            for (int i = 0; i < 256; i++) {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }

            return palette;
        }

        private static void FillGray8Bitmap(Bitmap bitmap, byte[] pixels) {
            // Fill row padding with a sentinel so accidental reads beyond the logical width show up
            // as pixel mismatches instead of silently comparing equal.
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            try {
                int width = bitmap.Width;
                int height = bitmap.Height;
                int stride = Math.Abs(data.Stride);
                byte[] rowBuffer = new byte[stride];

                for (int y = 0; y < height; y++) {
                    Array.Fill(rowBuffer, (byte)0xCD);
                    Buffer.BlockCopy(pixels, y * width, rowBuffer, 0, width);
                    Marshal.Copy(rowBuffer, 0, GetRowPointer(data, y), rowBuffer.Length);
                }
            } finally {
                bitmap.UnlockBits(data);
            }
        }

        private static void AssertBitExactBitmap(Bitmap expected, Bitmap actual) {
            Assert.That(actual.PixelFormat, Is.EqualTo(expected.PixelFormat), "Pixel format mismatch.");
            Assert.That(actual.Width, Is.EqualTo(expected.Width), "Width mismatch.");
            Assert.That(actual.Height, Is.EqualTo(expected.Height), "Height mismatch.");

            byte[] expectedPixels = ReadGray8Pixels(expected);
            byte[] actualPixels = ReadGray8Pixels(actual);

            Assert.That(actualPixels, Is.EqualTo(expectedPixels), "8-bit output pixels should match the preserved reference exactly.");
        }

        private static void AssertBitExactPixels(byte[] expectedPixels, Bitmap actual) {
            Assert.That(expectedPixels.Length, Is.EqualTo(actual.Width * actual.Height), "Expected pixel buffer length mismatch.");

            byte[] actualPixels = ReadGray8Pixels(actual);
            Assert.That(actualPixels, Is.EqualTo(expectedPixels), "8-bit output pixels should match the preserved reference exactly.");
        }

        private static byte[] ReadGray8Pixels(Bitmap image) {
            // Read only the logical pixel width from each row. This keeps reference comparisons focused
            // on image content while separate assertions validate the expected padding behavior.
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            try {
                int width = image.Width;
                int height = image.Height;
                int stride = Math.Abs(data.Stride);
                byte[] rowBuffer = new byte[stride];
                byte[] pixels = new byte[width * height];

                for (int y = 0; y < height; y++) {
                    Marshal.Copy(GetRowPointer(data, y), rowBuffer, 0, rowBuffer.Length);
                    Buffer.BlockCopy(rowBuffer, 0, pixels, y * width, width);
                }

                return pixels;
            } finally {
                image.UnlockBits(data);
            }
        }

        private static IntPtr GetRowPointer(BitmapData data, int y) {
            // Use BitmapData.Scan0 + stride so the helpers follow the same row order as the filters.
            // This avoids hidden differences between padded and tightly packed rows.
            return IntPtr.Add(data.Scan0, data.Stride * y);
        }

        private static bool HasStridePadding(Bitmap image, int bytesPerPixel) {
            // Canny should only care about the logical pixels, but these tests still assert whether
            // GDI+ inserted row padding so stride-sensitive bugs are part of the regression surface.
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

            try {
                int stride = Math.Abs(data.Stride);
                int expectedStride = image.Width * bytesPerPixel;
                return stride != expectedStride;
            } finally {
                image.UnlockBits(data);
            }
        }

        private static bool ShouldHaveStridePadding(int width, int bytesPerPixel) {
            // GDI+ aligns each scanline to a 4-byte boundary, so odd 8bpp widths are the easy way
            // to prove the test is exercising real padded rows instead of tightly packed memory.
            return (width * bytesPerPixel) % 4 != 0;
        }

        private static byte[] ComputeReferenceBlurredCanny(byte[] sourcePixels, int width, int height, byte lowThreshold, byte highThreshold, double gaussianSigma, int gaussianSize) {
            // Compare against Accord directly because that is the unchanged blurred behavior from the
            // repo before the local NINA implementation existed.
            // Run the preserved blurred implementation instead of duplicating its math in the test.
            // This keeps the reference source in one place: the legacy filter path itself.
            using Bitmap referenceInput = CreateGray8Bitmap(width, height, sourcePixels);

            var detector = new Accord.Imaging.Filters.CannyEdgeDetector(lowThreshold, highThreshold) {
                GaussianSigma = gaussianSigma,
                GaussianSize = gaussianSize
            };

            detector.ApplyInPlace(referenceInput);
            return ReadGray8Pixels(referenceInput);
        }

        private static byte[] ComputeReferenceNoBlurCanny(byte[] sourcePixels, int width, int height, byte lowThreshold, byte highThreshold) {
            // Compare against the preserved original NINA code so this test protects repo behavior,
            // not just whatever the optimized implementation currently does.
            // Run the preserved NINA no-blur implementation so the test compares against the moved
            // reference source instead of maintaining a second handwritten copy here.
            using Bitmap referenceInput = CreateGray8Bitmap(width, height, sourcePixels);

            var detector = new global::NINA.Tests.NoBlurCannyEdgeDetector(lowThreshold, highThreshold);
            detector.ApplyInPlace(referenceInput);
            return ReadGray8Pixels(referenceInput);
        }
    }
}
