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
using NINA.Image.ImageAnalysis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NINA.Test.Image.ImageAnalysis {

    [TestFixture]
    public class BayerFilter16bppTests {
        private static IEnumerable<TestCaseData> AlternateBayerPatterns() {
            // Use a small set of representative Bayer layouts to ensure the filter honors overrides.
            // These patterns cover each color's position so the channel mapping logic is exercised.
            yield return new TestCaseData("RGGB", new int[,] { { RGB.R, RGB.G }, { RGB.G, RGB.B } });
            yield return new TestCaseData("BGGR", new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } });
            yield return new TestCaseData("GBRG", new int[,] { { RGB.G, RGB.B }, { RGB.R, RGB.G } });
            yield return new TestCaseData("GRBG", new int[,] { { RGB.G, RGB.R }, { RGB.B, RGB.G } });
        }

        private static IEnumerable<TestCaseData> DimensionCases() {
            // Cover all even/odd width and height combinations to verify edge handling.
            // Odd widths should force GDI+ to add stride padding for both 16bpp and 48bpp images.
            yield return new TestCaseData(4, 4).SetName("EvenWidth_EvenHeight");
            yield return new TestCaseData(5, 4).SetName("OddWidth_EvenHeight");
            yield return new TestCaseData(4, 5).SetName("EvenWidth_OddHeight");
            yield return new TestCaseData(5, 5).SetName("OddWidth_OddHeight");
        }

        private static IEnumerable<TestCaseData> BorderOnlyDimensionCases() {
            // Use a 2-row image so the demosaic path skips inner rows entirely.
            // This forces all pixels through the border handling logic.
            yield return new TestCaseData(2, 2).SetName("BorderOnly_EvenWidth_EvenHeight");

            // Include an odd width to exercise stride padding while still running the border-only path.
            // This validates the border logic against GDI+ row alignment rules.
            yield return new TestCaseData(3, 2).SetName("BorderOnly_OddWidth_EvenHeight");
        }

        [Test]
        public void Demosaic_ProducesBitExactChannelsAndBuffers() {
            // Build a small Bayer frame with mixed values so every neighbor path is exercised.
            // An even width keeps stride padding out of this test so we isolate the demosaic logic.
            int width = 6;
            int height = 4;
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 100, step: 7);

            // Create a 16bpp grayscale bitmap backed by GDI+ so the stride matches real-world usage.
            // The filter will lock this bitmap and process the same memory layout as production code.
            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = true,
                SaveLumChannel = true,
                PerformDemosaicing = true
            };

            // Apply the filter through the managed Bitmap path so the destination image is also GDI+ backed.
            // This ensures both source and destination use real stride padding rules.
            using var processed = filter.Apply(sourceImage);

            // Compute expected output using the reference algorithm copied from BayerFilter16bpp.VerifyReference.
            // This gives us a bit-exact baseline for the demosaic output and luminance.
            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: true, computeLum: true);
            var processedChannels = Read48bppChannels(processed);

            // Validate the BGR output channels produced by the filter.
            // Any mismatch here indicates demosaic averaging or indexing errors.
            Assert.That(processed.PixelFormat, Is.EqualTo(PixelFormat.Format48bppRgb), "Apply should produce a 48bpp RGB image");
            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), "B channel mismatch");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), "G channel mismatch");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), "R channel mismatch");

            // Validate the saved per-channel arrays, which intentionally mirror the BGR image layout.
            // This ensures the auxiliary buffers stay consistent with the rendered image.
            Assert.That(filter.LRGBArrays, Is.Not.Null, "LRGBArrays should be created when color or luminance is requested");
            Assert.That(filter.LRGBArrays.Red, Is.EqualTo(reference.Blue), "LRGBArrays.Red should mirror image B");
            Assert.That(filter.LRGBArrays.Green, Is.EqualTo(reference.Green), "LRGBArrays.Green should mirror image G");
            Assert.That(filter.LRGBArrays.Blue, Is.EqualTo(reference.Red), "LRGBArrays.Blue should mirror image R");
            Assert.That(filter.LRGBArrays.Lum, Is.EqualTo(reference.Lum), "LRGBArrays.Lum should match averaged luminance");
        }

        [Test]
        [TestCaseSource(nameof(DimensionCases))]
        public void Demosaic_HandlesEvenAndOddDimensions(int width, int height) {
            // Verify that every even/odd dimension combination produces bit-exact output.
            // This ensures border logic and stride padding behave correctly at all sizes.
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 120, step: 5);

            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = false,
                SaveLumChannel = false,
                PerformDemosaicing = true
            };

            using var processed = filter.Apply(sourceImage);

            // Confirm that padding exists exactly when the stride cannot be 4-byte aligned without it.
            // This makes the stride behavior part of the contract we verify in tests.
            bool expectSourcePadding = ShouldHaveStridePadding(width, bytesPerPixel: 2);
            bool expectDestPadding = ShouldHaveStridePadding(width, bytesPerPixel: 6);
            Assert.That(HasStridePadding(sourceImage, bytesPerPixel: 2), Is.EqualTo(expectSourcePadding), "Unexpected source stride padding.");
            Assert.That(HasStridePadding(processed, bytesPerPixel: 6), Is.EqualTo(expectDestPadding), "Unexpected destination stride padding.");

            // Compute the reference output and validate the produced image channels.
            // This is the core correctness check for all resolution combinations.
            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: true, computeLum: false);
            var processedChannels = Read48bppChannels(processed);

            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), "B channel mismatch");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), "G channel mismatch");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), "R channel mismatch");
        }
        [Test]
        [TestCaseSource(nameof(AlternateBayerPatterns))]
        public void Demosaic_RespectsBayerPatternOverride(string patternName, int[,] bayerPattern) {
            // Use a non-default Bayer layout so we verify that the pattern override is honored.
            // The sample is large enough to hit every neighbor path with mixed values.
            int width = 6;
            int height = 5;
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 250, step: 5);

            // Create the input image and configure the filter with the supplied pattern.
            // This isolates the effect of the pattern override on the output channels.
            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = true,
                SaveLumChannel = true,
                PerformDemosaicing = true,
                BayerPattern = bayerPattern
            };

            // Apply the filter and compute the expected output using the same pattern.
            // This ties the reference directly to the override under test.
            using var processed = filter.Apply(sourceImage);

            ReferenceChannels reference = ComputeReference(sourceImage, bayerPattern, performDemosaic: true, computeLum: true);
            var processedChannels = Read48bppChannels(processed);

            // Verify the BGR output channels match the reference.
            // A mismatch here means the pattern override is not respected in the core output.
            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), $"B channel mismatch ({patternName})");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), $"G channel mismatch ({patternName})");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), $"R channel mismatch ({patternName})");

            // Verify the saved LRGBArrays mirror the BGR output as implemented by the filter.
            // This ensures the auxiliary buffers are consistent with the output image.
            Assert.That(filter.LRGBArrays.Red, Is.EqualTo(reference.Blue), $"LRGBArrays.Red should mirror image B ({patternName})");
            Assert.That(filter.LRGBArrays.Green, Is.EqualTo(reference.Green), $"LRGBArrays.Green should mirror image G ({patternName})");
            Assert.That(filter.LRGBArrays.Blue, Is.EqualTo(reference.Red), $"LRGBArrays.Blue should mirror image R ({patternName})");
            Assert.That(filter.LRGBArrays.Lum, Is.EqualTo(reference.Lum), $"LRGBArrays.Lum should match averaged luminance ({patternName})");
        }

        [Test]
        [TestCaseSource(nameof(BorderOnlyDimensionCases))]
        public void Demosaic_HandlesBorderOnlyDimensions(int width, int height) {
            // Exercise the path where no inner rows exist so the border handler processes every pixel.
            // This ensures edge handling is correct when the image is only two rows tall.
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 90, step: 6);

            // Use GDI+ bitmaps so stride padding mirrors production behavior.
            // The filter must respect these padded rows in both source and destination images.
            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = true,
                SaveLumChannel = true,
                PerformDemosaicing = true
            };

            using var processed = filter.Apply(sourceImage);

            // Validate stride padding expectations for this width and pixel format.
            // This confirms the test is actually hitting padded rows when expected.
            bool expectSourcePadding = ShouldHaveStridePadding(width, bytesPerPixel: 2);
            bool expectDestPadding = ShouldHaveStridePadding(width, bytesPerPixel: 6);
            Assert.That(HasStridePadding(sourceImage, bytesPerPixel: 2), Is.EqualTo(expectSourcePadding), "Unexpected source stride padding.");
            Assert.That(HasStridePadding(processed, bytesPerPixel: 6), Is.EqualTo(expectDestPadding), "Unexpected destination stride padding.");

            // Compute the reference output and validate both the image and LRGBArrays.
            // This asserts bit-exact behavior for both color planes and luminance.
            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: true, computeLum: true);
            var processedChannels = Read48bppChannels(processed);

            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), "B channel mismatch");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), "G channel mismatch");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), "R channel mismatch");

            Assert.That(filter.LRGBArrays, Is.Not.Null, "LRGBArrays should be created when color or luminance is requested");
            Assert.That(filter.LRGBArrays.Red, Is.EqualTo(reference.Blue), "LRGBArrays.Red should mirror image B");
            Assert.That(filter.LRGBArrays.Green, Is.EqualTo(reference.Green), "LRGBArrays.Green should mirror image G");
            Assert.That(filter.LRGBArrays.Blue, Is.EqualTo(reference.Red), "LRGBArrays.Blue should mirror image R");
            Assert.That(filter.LRGBArrays.Lum, Is.EqualTo(reference.Lum), "LRGBArrays.Lum should match averaged luminance");
        }

        [Test]
        public void RawMapping_NoDemosaicCopiesPixelsIntoPatternedChannels() {
            // Verify the straight-through path respects the Bayer pattern and zeroes unused channels.
            // This path should only copy the raw value into the plane dictated by the pattern.
            int width = 4;
            int height = 3;
            ushort[] sourcePixels = {
                50, 60, 70, 80,
                90, 100, 110, 120,
                130, 140, 150, 160
            };

            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = false,
                SaveLumChannel = false,
                PerformDemosaicing = false
            };

            // Apply the filter and extract the BGR planes for validation.
            // Reading the output this way avoids any managed bitmap conversions.
            using var processed = filter.Apply(sourceImage);

            // Expected channels follow the Bayer pattern with other planes held at zero.
            // The direct mapping should not synthesize values for missing colors.
            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: false, computeLum: false);
            var processedChannels = Read48bppChannels(processed);

            // Validate each channel is populated only where the Bayer pattern allows.
            // Any non-zero value outside the pattern indicates incorrect mapping.
            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), "B channel should contain Bayer B samples only");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), "G channel should contain Bayer G samples only");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), "R channel should contain Bayer R samples only");
        }

        [Test]
        [TestCaseSource(nameof(DimensionCases))]
        public void RawMapping_HandlesEvenAndOddDimensions(int width, int height) {
            // Verify the raw mapping path for the full even/odd size matrix.
            // This ensures row stepping and Bayer placement remain correct with padding.
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 60, step: 4);

            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = false,
                SaveLumChannel = false,
                PerformDemosaicing = false
            };

            using var processed = filter.Apply(sourceImage);

            // Confirm stride padding is present only when required by 4-byte alignment.
            // This keeps the test aligned with actual GDI+ padding behavior.
            bool expectSourcePadding = ShouldHaveStridePadding(width, bytesPerPixel: 2);
            bool expectDestPadding = ShouldHaveStridePadding(width, bytesPerPixel: 6);
            Assert.That(HasStridePadding(sourceImage, bytesPerPixel: 2), Is.EqualTo(expectSourcePadding), "Unexpected source stride padding.");
            Assert.That(HasStridePadding(processed, bytesPerPixel: 6), Is.EqualTo(expectDestPadding), "Unexpected destination stride padding.");

            // Expected channels follow the Bayer pattern with other planes held at zero.
            // Padding bytes must not influence any of these logical pixel values.
            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: false, computeLum: false);
            var processedChannels = Read48bppChannels(processed);

            // Validate that the raw mapping respects the Bayer pattern even with padded strides.
            // Failures here indicate row stepping is not correctly skipping padding.
            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), "B channel mismatch");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), "G channel mismatch");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), "R channel mismatch");
        }

        [Test]
        [TestCaseSource(nameof(AlternateBayerPatterns))]
        public void RawMapping_RespectsBayerPatternOverride(string patternName, int[,] bayerPattern) {
            // Exercise the no-demosaic path with non-default patterns to ensure mapping is correct.
            // Keep width even so this test focuses strictly on pattern mapping, not stride padding.
            int width = 6;
            int height = 4;
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 50, step: 3);

            // Configure the filter with the pattern override and run the raw mapping path.
            // This isolates the mapping behavior without demosaic interpolation.
            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                PerformDemosaicing = false,
                BayerPattern = bayerPattern
            };

            using var processed = filter.Apply(sourceImage);

            // Compute expected raw channel placement for this pattern.
            // This reference assumes a direct copy into the plane indicated by the pattern.
            ReferenceChannels reference = ComputeReference(sourceImage, bayerPattern, performDemosaic: false, computeLum: false);
            var processedChannels = Read48bppChannels(processed);

            // Validate that the pattern override is honored for each channel.
            // Any mismatch means the override is ignored or misapplied.
            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), $"B channel mismatch ({patternName})");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), $"G channel mismatch ({patternName})");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), $"R channel mismatch ({patternName})");
        }
        [Test]
        public void Demosaic_SaveLumOnly_PopulatesLumArray() {
            // Confirm luminance output is available without allocating color planes.
            // This verifies that SaveLumChannel can be used independently.
            int width = 6;
            int height = 4;
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 120, step: 11);

            // Configure the filter to save only luminance and run demosaic.
            // This ensures the color arrays remain empty while luminance is filled.
            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = false,
                SaveLumChannel = true,
                PerformDemosaicing = true
            };

            using var processed = filter.Apply(sourceImage);

            // Compute the expected demosaic output and luminance channel.
            // The luminance is computed as the mean of the RGB channels.
            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: true, computeLum: true);
            var processedChannels = Read48bppChannels(processed);

            // Validate the output image channels and the luminance buffer.
            // This confirms both the image and the saved arrays are consistent.
            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), "B channel mismatch");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), "G channel mismatch");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), "R channel mismatch");

            Assert.That(filter.LRGBArrays, Is.Not.Null, "LRGBArrays should be created when luminance is requested");
            Assert.That(filter.LRGBArrays.Lum, Is.EqualTo(reference.Lum), "Lum channel mismatch");
            Assert.That(filter.LRGBArrays.Red, Has.Length.EqualTo(0), "Red channel should be empty when SaveColorChannels is false");
            Assert.That(filter.LRGBArrays.Green, Has.Length.EqualTo(0), "Green channel should be empty when SaveColorChannels is false");
            Assert.That(filter.LRGBArrays.Blue, Has.Length.EqualTo(0), "Blue channel should be empty when SaveColorChannels is false");
        }

        [Test]
        public void Demosaic_SaveColorOnly_PopulatesColorArrays() {
            // Confirm color planes are populated while luminance remains empty.
            // Use an even width to keep stride padding out of this specific validation.
            int width = 6;
            int height = 5;
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 200, step: 9);

            // Configure the filter to save only the color planes.
            // The luminance array should remain empty in this configuration.
            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = true,
                SaveLumChannel = false,
                PerformDemosaicing = true
            };

            using var processed = filter.Apply(sourceImage);

            // Compute the expected color planes and validate both image and saved arrays.
            // This ensures the color buffers mirror the output image correctly.
            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: true, computeLum: false);
            var processedChannels = Read48bppChannels(processed);

            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), "B channel mismatch");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), "G channel mismatch");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), "R channel mismatch");

            Assert.That(filter.LRGBArrays, Is.Not.Null, "LRGBArrays should be created when color channels are requested");
            Assert.That(filter.LRGBArrays.Lum, Has.Length.EqualTo(0), "Lum channel should be empty when SaveLumChannel is false");
            Assert.That(filter.LRGBArrays.Red, Is.EqualTo(reference.Blue), "LRGBArrays.Red should mirror image B");
            Assert.That(filter.LRGBArrays.Green, Is.EqualTo(reference.Green), "LRGBArrays.Green should mirror image G");
            Assert.That(filter.LRGBArrays.Blue, Is.EqualTo(reference.Red), "LRGBArrays.Blue should mirror image R");
        }

        [Test]
        public void Demosaic_NoChannelsRequested_LeavesLRGBArraysNull() {
            // Avoid allocating LRGBArrays when no auxiliary channels are needed.
            // This ensures the filter does not allocate unnecessary buffers.
            int width = 4;
            int height = 4;
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 500, step: 1);

            // Run the filter with both save flags disabled.
            // The output image should still be correct while no arrays are allocated.
            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = false,
                SaveLumChannel = false,
                PerformDemosaicing = true
            };

            using var processed = filter.Apply(sourceImage);

            // Validate the output image and confirm no LRGBArrays are allocated.
            // This guards against unnecessary memory use when auxiliary channels are not needed.
            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: true, computeLum: false);
            var processedChannels = Read48bppChannels(processed);

            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), "B channel mismatch");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), "G channel mismatch");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), "R channel mismatch");
            Assert.That(filter.LRGBArrays, Is.Null, "LRGBArrays should remain null when no channels are requested");
        }

        [Test]
        public void Apply_DoesNotModifySourceBuffer() {
            // Ensure the filter treats the source buffer as read-only.
            // Use an even width so stride padding does not interfere with this integrity check.
            int width = 6;
            int height = 4;
            ushort[] sourcePixels = CreateSequentialPixels(width, height, startValue: 1000, step: 7);

            // Capture the source bytes before processing and verify they are unchanged afterward.
            // This protects against accidental writes to the input buffer.
            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            byte[] before = ReadBitmapBytes(sourceImage);

            var filter = new BayerFilter16bpp();
            using var processed = filter.Apply(sourceImage);

            byte[] after = ReadBitmapBytes(sourceImage);

            // Validate output format and ensure no source buffer mutation occurred.
            // This confirms the filter respects the read-only contract of Apply.
            Assert.That(processed.PixelFormat, Is.EqualTo(PixelFormat.Format48bppRgb), "Apply should produce a 48bpp RGB image");
            Assert.That(after, Is.EqualTo(before), "Source buffer should remain unchanged after Apply");
        }

        [Test]
        public void FormatTranslations_MapsGray16ToRgb48() {
            // Verify the filter advertises the 16bpp grayscale input translation.
            // This mapping drives BaseFilter.Apply to allocate the correct destination format.
            var filter = new BayerFilter16bpp();
            Assert.That(filter.FormatTranslations[PixelFormat.Format16bppGrayScale], Is.EqualTo(PixelFormat.Format48bppRgb));
        }

        private readonly struct ReferenceChannels {
            // Bundle the channel arrays so tests can pass reference results around cleanly.
            // This keeps the helper signatures compact and explicit.
            public ReferenceChannels(ushort[] blue, ushort[] green, ushort[] red, ushort[] lum) {
                Blue = blue;
                Green = green;
                Red = red;
                Lum = lum;
            }

            public ushort[] Blue { get; }
            public ushort[] Green { get; }
            public ushort[] Red { get; }
            public ushort[] Lum { get; }
        }

        private static ushort[] CreateSequentialPixels(int width, int height, int startValue, int step) {
            // Generate deterministic values that exercise the averaging paths without overflow.
            // Using a simple linear sequence keeps the expected output easy to reason about.
            int pixelCount = width * height;
            ushort[] pixels = new ushort[pixelCount];

            int value = startValue;
            for (int i = 0; i < pixelCount; i++) {
                pixels[i] = (ushort)value;
                value += step;
            }

            return pixels;
        }

        private static Bitmap CreateGray16Bitmap(int width, int height, ushort[] sourcePixels) {
            // Allocate a 16bpp grayscale bitmap through GDI+ to ensure real stride behavior.
            // The caller owns the returned bitmap and must dispose it.
            if (sourcePixels.Length != width * height) {
                throw new ArgumentException("Source pixel array length does not match image dimensions.", nameof(sourcePixels));
            }

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format16bppGrayScale);

            try {
                FillGray16Bitmap(bitmap, sourcePixels);
                return bitmap;
            } catch {
                bitmap.Dispose();
                throw;
            }
        }

        private static void FillGray16Bitmap(Bitmap bitmap, ushort[] sourcePixels) {
            // Lock the bitmap and write pixel data with explicit stride handling.
            // This mirrors how production code will read the bitmap memory.
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format16bppGrayScale);

            try {
                int width = bitmap.Width;
                int height = bitmap.Height;
                int stride = Math.Abs(data.Stride);

                // Reuse a single row buffer to avoid repeated allocations.
                // Padding bytes are filled with a sentinel to catch accidental reads.
                byte[] rowBuffer = new byte[stride];

                for (int y = 0; y < height; y++) {
                    Array.Fill(rowBuffer, (byte)0xCD);

                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++) {
                        ushort value = sourcePixels[rowOffset + x];
                        int pixelOffset = x * 2;
                        rowBuffer[pixelOffset] = (byte)(value & 0xFF);
                        rowBuffer[pixelOffset + 1] = (byte)(value >> 8);
                    }

                    IntPtr rowPtr = GetRowPointer(data, y);
                    Marshal.Copy(rowBuffer, 0, rowPtr, rowBuffer.Length);
                }
            } finally {
                bitmap.UnlockBits(data);
            }
        }

        private static ushort[] ReadGray16Pixels(Bitmap image) {
            // Read back the 16bpp grayscale pixels in memory row order (Scan0 first).
            // This keeps the reference aligned with how the filter interprets BitmapData.
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format16bppGrayScale);

            try {
                int width = image.Width;
                int height = image.Height;
                int stride = Math.Abs(data.Stride);
                ushort[] pixels = new ushort[width * height];

                // Allocate one buffer for a full stride row to keep padding handling simple.
                // The same buffer is reused for each row to minimize GC pressure.
                byte[] rowBuffer = new byte[stride];

                for (int y = 0; y < height; y++) {
                    IntPtr rowPtr = GetRowPointer(data, y);
                    Marshal.Copy(rowPtr, rowBuffer, 0, rowBuffer.Length);

                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++) {
                        int pixelOffset = x * 2;
                        pixels[rowOffset + x] = (ushort)(rowBuffer[pixelOffset] | (rowBuffer[pixelOffset + 1] << 8));
                    }
                }

                return pixels;
            } finally {
                image.UnlockBits(data);
            }
        }
        private static (ushort[] Blue, ushort[] Green, ushort[] Red) Read48bppChannels(Bitmap image) {
            // Extract BGR planes from a 48bpp image, respecting stride padding per row.
            // Accord uses BGR order even though the pixel format is named RGB.
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format48bppRgb);

            try {
                int width = image.Width;
                int height = image.Height;
                int stride = Math.Abs(data.Stride);
                int pixelCount = width * height;

                ushort[] blue = new ushort[pixelCount];
                ushort[] green = new ushort[pixelCount];
                ushort[] red = new ushort[pixelCount];

                // Read row-by-row so we can skip any stride padding correctly.
                // This keeps the memory row order consistent with Scan0 and the filter logic.
                byte[] rowBuffer = new byte[stride];

                for (int y = 0; y < height; y++) {
                    IntPtr rowPtr = GetRowPointer(data, y);
                    Marshal.Copy(rowPtr, rowBuffer, 0, rowBuffer.Length);

                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++) {
                        int offset = x * 6;
                        int idx = rowOffset + x;

                        blue[idx] = (ushort)(rowBuffer[offset] | (rowBuffer[offset + 1] << 8));
                        green[idx] = (ushort)(rowBuffer[offset + 2] | (rowBuffer[offset + 3] << 8));
                        red[idx] = (ushort)(rowBuffer[offset + 4] | (rowBuffer[offset + 5] << 8));
                    }
                }

                return (blue, green, red);
            } finally {
                image.UnlockBits(data);
            }
        }

        private static bool HasStridePadding(Bitmap image, int bytesPerPixel) {
            // Detect 4-byte aligned stride padding that appears when width is not naturally aligned.
            // This confirms whether the test is exercising the padded-stride scenario.
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
            // GDI+ aligns each scanline to a 4-byte boundary.
            // Padding is required when the natural row length is not 4-byte aligned.
            return (width * bytesPerPixel) % 4 != 0;
        }

        private static byte[] ReadBitmapBytes(Bitmap image) {
            // Capture the raw byte buffer in memory row order so we can compare before/after.
            // This includes stride padding to detect any accidental writes.
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

            try {
                int stride = Math.Abs(data.Stride);
                byte[] buffer = new byte[stride * data.Height];
                byte[] rowBuffer = new byte[stride];

                for (int y = 0; y < image.Height; y++) {
                    IntPtr rowPtr = GetRowPointer(data, y);
                    Marshal.Copy(rowPtr, rowBuffer, 0, rowBuffer.Length);
                    Buffer.BlockCopy(rowBuffer, 0, buffer, y * stride, stride);
                }

                return buffer;
            } finally {
                image.UnlockBits(data);
            }
        }

        private static IntPtr GetRowPointer(BitmapData data, int y) {
            // Use the raw memory order so row 0 corresponds to BitmapData.Scan0.
            // This matches how Accord filters interpret BitmapData and keeps parity aligned.
            int rowOffset = data.Stride * y;
            return IntPtr.Add(data.Scan0, rowOffset);
        }

        private static ReferenceChannels ComputeReference(
            Bitmap sourceImage,
            int[,] bayerPattern,
            bool performDemosaic,
            bool computeLum) {
            // Read the source pixels from the GDI+ bitmap in memory order so parity matches the filter.
            // The reference algorithm then operates on a tightly packed pixel array.
            ushort[] sourcePixels = ReadGray16Pixels(sourceImage);

            if (performDemosaic) {
                return ComputeDemosaicReference(sourcePixels, sourceImage.Width, sourceImage.Height, bayerPattern, computeLum);
            }

            var direct = ComputeDirectReference(sourcePixels, sourceImage.Width, sourceImage.Height, bayerPattern);
            return new ReferenceChannels(direct.Blue, direct.Green, direct.Red, Array.Empty<ushort>());
        }

        private static ReferenceChannels ComputeDemosaicReference(
            ushort[] source,
            int width,
            int height,
            int[,] bayerPattern,
            bool computeLum) {
            // Mirror the reference algorithm embedded in BayerFilter16bpp.VerifyReference.
            // This uses a 3x3 neighborhood and integer division for bit-precise output.
            int widthM1 = width - 1;
            int heightM1 = height - 1;
            int pixelCount = width * height;

            ushort[] blue = new ushort[pixelCount];
            ushort[] green = new ushort[pixelCount];
            ushort[] red = new ushort[pixelCount];
            ushort[] lum = computeLum ? new ushort[pixelCount] : Array.Empty<ushort>();

            int[] rgbValues = new int[3];
            int[] rgbCounters = new int[3];

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;

                for (int x = 0; x < width; x++) {
                    rgbValues[0] = rgbValues[1] = rgbValues[2] = 0;
                    rgbCounters[0] = rgbCounters[1] = rgbCounters[2] = 0;

                    int index = rowOffset + x;
                    int bayerIndex = bayerPattern[y & 1, x & 1];

                    rgbValues[bayerIndex] += source[index];
                    rgbCounters[bayerIndex]++;

                    if (x != 0) {
                        bayerIndex = bayerPattern[y & 1, (x - 1) & 1];
                        rgbValues[bayerIndex] += source[index - 1];
                        rgbCounters[bayerIndex]++;
                    }

                    if (x != widthM1) {
                        bayerIndex = bayerPattern[y & 1, (x + 1) & 1];
                        rgbValues[bayerIndex] += source[index + 1];
                        rgbCounters[bayerIndex]++;
                    }

                    if (y != 0) {
                        bayerIndex = bayerPattern[(y - 1) & 1, x & 1];
                        rgbValues[bayerIndex] += source[index - width];
                        rgbCounters[bayerIndex]++;

                        if (x != 0) {
                            bayerIndex = bayerPattern[(y - 1) & 1, (x - 1) & 1];
                            rgbValues[bayerIndex] += source[index - width - 1];
                            rgbCounters[bayerIndex]++;
                        }

                        if (x != widthM1) {
                            bayerIndex = bayerPattern[(y - 1) & 1, (x + 1) & 1];
                            rgbValues[bayerIndex] += source[index - width + 1];
                            rgbCounters[bayerIndex]++;
                        }
                    }

                    if (y != heightM1) {
                        bayerIndex = bayerPattern[(y + 1) & 1, x & 1];
                        rgbValues[bayerIndex] += source[index + width];
                        rgbCounters[bayerIndex]++;

                        if (x != 0) {
                            bayerIndex = bayerPattern[(y + 1) & 1, (x - 1) & 1];
                            rgbValues[bayerIndex] += source[index + width - 1];
                            rgbCounters[bayerIndex]++;
                        }

                        if (x != widthM1) {
                            bayerIndex = bayerPattern[(y + 1) & 1, (x + 1) & 1];
                            rgbValues[bayerIndex] += source[index + width + 1];
                            rgbCounters[bayerIndex]++;
                        }
                    }

                    ushort expR = (ushort)(rgbValues[RGB.R] / rgbCounters[RGB.R]);
                    ushort expG = (ushort)(rgbValues[RGB.G] / rgbCounters[RGB.G]);
                    ushort expB = (ushort)(rgbValues[RGB.B] / rgbCounters[RGB.B]);

                    red[index] = expR;
                    green[index] = expG;
                    blue[index] = expB;

                    if (computeLum) {
                        lum[index] = (ushort)((expR + expG + expB) / 3.0);
                    }
                }
            }

            return new ReferenceChannels(blue, green, red, lum);
        }

        private static (ushort[] Blue, ushort[] Green, ushort[] Red) ComputeDirectReference(
            ushort[] source,
            int width,
            int height,
            int[,] bayerPattern) {
            // Mirror the direct Bayer-to-channel mapping used when demosaicing is disabled.
            // Each source pixel is copied into exactly one channel based on the pattern.
            int pixelCount = width * height;
            ushort[] blue = new ushort[pixelCount];
            ushort[] green = new ushort[pixelCount];
            ushort[] red = new ushort[pixelCount];

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;

                for (int x = 0; x < width; x++) {
                    int index = rowOffset + x;
                    int color = bayerPattern[y & 1, x & 1];
                    ushort value = source[index];

                    switch (color) {
                        case RGB.R:
                            red[index] = value;
                            break;
                        case RGB.G:
                            green[index] = value;
                            break;
                        case RGB.B:
                            blue[index] = value;
                            break;
                    }
                }
            }

            return (blue, green, red);
        }
    }
}
