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
using System.IO;
using System.Runtime.InteropServices;

using OptimizedCannyEdgeDetector = NINA.Image.ImageAnalysis.CannyEdgeDetector;
using OptimizedNoBlurCannyEdgeDetector = NINA.Image.ImageAnalysis.NoBlurCannyEdgeDetector;

namespace NINA.Test.Image.ImageAnalysis {
    // Regression coverage for both Canny variants used by NINA. The blurred path is compared against
    // Accord, while the NINA-owned no-blur path is compared against the preserved reference source.
    // Keeping them together makes it clear that they share the same image fixtures and full-frame cases.
    [TestFixture]
    [Ignore("These tests are exhaustive and take some time to run. Enable if needed.")]
    public class CannyEdgeDetectorVariantTests {
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
                foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.All) {
                    yield return new TestCaseData(
                            $"{fixture.Name}_{resolution.Key}",
                            resolution.Value.Width,
                            resolution.Value.Height,
                            fixture.CreateBytes)
                        .SetName($"RealWorldResolution_{resolution.Key}_{fixture.Name}");
                }
            }
        }

        private static IEnumerable<TestCaseData> FixtureCases() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.All) {
                yield return new TestCaseData(fixture.Name, fixture.CreateBytes)
                    .SetName(fixture.Name);
            }
        }

        // This is the production star-detection path: blurred Canny with the normal thresholds.
        // Every listed resolution must match Accord exactly, not just produce similar-looking edges.
        [Test]
        [TestCaseSource(nameof(AstroCameraResolutionCases))]
        public void CannyEdgeDetector_DefaultParameters_MatchesAccordReference(string scenarioName, int width, int height, Func<int, int, byte[]> createPixels) {
            byte[] sourcePixels = createPixels(width, height);
            byte[] expectedPixels = ComputeReferenceBlurredCanny(sourcePixels, width, height, lowThreshold: 10, highThreshold: 80, gaussianSigma: 1.4, gaussianSize: 5);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedCannyEdgeDetector(lowThreshold: 10, highThreshold: 80);

            optimized.ApplyInPlace(optimizedInput);

            Assert.That(HasStridePadding(optimizedInput, bytesPerPixel: 1), Is.EqualTo(ShouldHaveStridePadding(width, bytesPerPixel: 1)), "Unexpected stride padding.");
            AssertBitExactPixels(expectedPixels, optimizedInput, scenarioName);
        }

        // The optimized blur has separate interior and border code paths, so validate a larger kernel
        // size as well. This catches errors that only appear when the Gaussian footprint changes.
        [Test]
        [TestCaseSource(nameof(FixtureCases))]
        public void CannyEdgeDetector_CustomGaussianSize_MatchesAccordReference(string fixtureName, Func<int, int, byte[]> createPixels) {
            int width = AstroCameraResolutions[CustomGaussianResolutionKey].Width;
            int height = AstroCameraResolutions[CustomGaussianResolutionKey].Height;
            byte[] sourcePixels = createPixels(width, height);
            byte[] expectedPixels = ComputeReferenceBlurredCanny(sourcePixels, width, height, lowThreshold: 20, highThreshold: 100, gaussianSigma: 1.4, gaussianSize: 10);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedCannyEdgeDetector(lowThreshold: 20, highThreshold: 100) {
                GaussianSize = 10
            };

            optimized.ApplyInPlace(optimizedInput);

            AssertBitExactPixels(expectedPixels, optimizedInput, fixtureName);
        }

        // The no-blur variant is NINA-owned code, so the preserved implementation under NINA.Tests
        // is the correct reference behavior for this path rather than Accord.
        [Test]
        [TestCaseSource(nameof(AstroCameraResolutionCases))]
        public void NoBlurCannyEdgeDetector_MatchesPreservedReference(string scenarioName, int width, int height, Func<int, int, byte[]> createPixels) {
            byte[] sourcePixels = createPixels(width, height);
            byte[] expectedPixels = ComputeReferenceNoBlurCanny(sourcePixels, width, height, lowThreshold: 10, highThreshold: 80);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedNoBlurCannyEdgeDetector(lowThreshold: 10, highThreshold: 80);

            optimized.ApplyInPlace(optimizedInput);

            Assert.That(HasStridePadding(optimizedInput, bytesPerPixel: 1), Is.EqualTo(ShouldHaveStridePadding(width, bytesPerPixel: 1)), "Unexpected stride padding.");
            AssertBitExactPixels(expectedPixels, optimizedInput, scenarioName);
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

        private static void AssertBitExactPixels(byte[] expectedPixels, Bitmap actual, string scenarioName) {
            Assert.That(expectedPixels.Length, Is.EqualTo(actual.Width * actual.Height), "Expected pixel buffer length mismatch.");

            byte[] actualPixels = ReadGray8Pixels(actual);
            Assert.That(actualPixels, Is.EqualTo(expectedPixels), $"8-bit output pixels should match the preserved reference exactly ({scenarioName}).");
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

    // Extra hardening coverage for Canny. This stays in the same file as the main regression suite on
    // purpose, because reviewers should be able to see the "broad real-world parity" tests and the
    // "different input families on realistic frame sizes" tests side by side.
    [TestFixture]
    [Ignore("These tests are exhaustive and take some time to run. Enable if needed.")]
    public class CannyEdgeDetectorHardeningTests {
        // NUnit requires public parameter types for public parameterized test methods.
        // Keeping the scenario as a small record-like type makes the test matrix easy to read.
        public sealed class InputScenario {
            public required string Name { get; init; }
            public required int Width { get; init; }
            public required int Height { get; init; }
            public required Func<int, int, byte[]> CreatePixels { get; init; }
            public Func<int, int, Rectangle>? CreateRect { get; init; }
        }

        // These values match the production star-detection defaults. Hardening should validate the
        // exact behavior that the application actually runs, not a synthetic threshold pair that no
        // real code path uses.
        private const byte LowThreshold = 10;
        private const byte HighThreshold = 80;
        private const double GaussianSigma = 1.4;
        private const int GaussianSize = 5;
        private const int GuideWidth = 1280;
        private const int GuideHeight = 960;
        private const int PlanetaryWidth = 1936;
        private const int PlanetaryHeight = 1096;
        private const int PlanetaryRoiWidth = 1937;
        private const int PlanetaryRoiHeight = 1097;
        private const int DeepSkyWidth = 3096;
        private const int DeepSkyHeight = 2080;
        private const int FourThirdsWidth = 4144;
        private const int FourThirdsHeight = 2822;

        // Run the same shared fixture family across the same realistic frame shapes. This keeps the
        // hardening matrix systematic instead of coupling a specific source texture to a specific size.
        private static IEnumerable<TestCaseData> OutputScenarios() {
            foreach ((string resolutionName, int width, int height) in new[] {
                ("Guide_1280x960", GuideWidth, GuideHeight),
                ("Planetary_1936x1096", PlanetaryWidth, PlanetaryHeight),
                ("PlanetaryRoi_1937x1097", PlanetaryRoiWidth, PlanetaryRoiHeight),
                ("DeepSky_3096x2080", DeepSkyWidth, DeepSkyHeight),
                ("FourThirds_4144x2822", FourThirdsWidth, FourThirdsHeight)
            }) {
                foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.All) {
                    yield return Scenario(new InputScenario {
                        Name = $"{fixture.Name}_{resolutionName}",
                        Width = width,
                        Height = height,
                        CreatePixels = fixture.CreateBytes
                    });
                }
            }
        }

        // Partial-rectangle processing is easy to break accidentally because it stresses coordinate
        // math, border cleanup, and stride handling all at once. Keep the ROI geometry fixed and run
        // every shared fixture through it.
        private static IEnumerable<TestCaseData> PartialRectScenarios() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.All) {
                yield return Scenario(new InputScenario {
                    Name = $"PartialRect_{fixture.Name}_PlanetaryRoi_1937x1097",
                    Width = PlanetaryRoiWidth,
                    Height = PlanetaryRoiHeight,
                    CreatePixels = fixture.CreateBytes,
                    CreateRect = static (width, height) => new Rectangle(61, 43, width - 122, height - 86)
                });
            }
        }

        // Determinism is data-dependent, so rerun every shared fixture on the padded ROI-sized frame.
        private static IEnumerable<TestCaseData> DeterminismScenarios() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.All) {
                yield return Scenario(new InputScenario {
                    Name = $"Determinism_{fixture.Name}_PlanetaryRoi_1937x1097",
                    Width = PlanetaryRoiWidth,
                    Height = PlanetaryRoiHeight,
                    CreatePixels = fixture.CreateBytes
                });
            }
        }

        // This is the black-box no-blur hardening test. It does not know anything about internal
        // stages. It only checks the final bitmap against the preserved reference implementation.
        [Test]
        [TestCaseSource(nameof(OutputScenarios))]
        public void NoBlurCannyEdgeDetector_MatchesReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = GetRect(scenario, scenario.Width, scenario.Height);

            using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

            var reference = new global::NINA.Tests.NoBlurCannyEdgeDetector(LowThreshold, HighThreshold);
            var optimized = new OptimizedNoBlurCannyEdgeDetector(LowThreshold, HighThreshold);

            reference.ApplyInPlace(expected, rect);
            optimized.ApplyInPlace(actual, rect);

            AssertBitmapExact(scenario.Name, "NoBlurFinal", sourcePixels, expected, actual);
        }

        // Same idea, but for the blurred production path. This remains a strict final-output compare
        // against Accord, which is still the most important external behavior contract.
        [Test]
        [TestCaseSource(nameof(OutputScenarios))]
        public void BlurredCannyEdgeDetector_MatchesAccordReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = GetRect(scenario, scenario.Width, scenario.Height);

            using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

            var reference = new Accord.Imaging.Filters.CannyEdgeDetector(LowThreshold, HighThreshold) {
                GaussianSigma = GaussianSigma,
                GaussianSize = GaussianSize
            };
            var optimized = new OptimizedCannyEdgeDetector(LowThreshold, HighThreshold) {
                GaussianSigma = GaussianSigma,
                GaussianSize = GaussianSize
            };

            reference.ApplyInPlace(expected, rect);
            optimized.ApplyInPlace(actual, rect);

            AssertBitmapExact(scenario.Name, "BlurredFinal", sourcePixels, expected, actual);
        }

        // Partial-rectangle parity deserves its own named test even though the scenario also exists as
        // data, because reviewers often look for this exact behavior by name.
        [Test]
        [TestCaseSource(nameof(PartialRectScenarios))]
        public void NoBlurCannyEdgeDetector_PartialRect_MatchesReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = GetRect(scenario, scenario.Width, scenario.Height);

            using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

            var reference = new global::NINA.Tests.NoBlurCannyEdgeDetector(LowThreshold, HighThreshold);
            var optimized = new OptimizedNoBlurCannyEdgeDetector(LowThreshold, HighThreshold);

            reference.ApplyInPlace(expected, rect);
            optimized.ApplyInPlace(actual, rect);

            AssertBitmapExact(scenario.Name, "NoBlurPartialRect", sourcePixels, expected, actual);
        }

        // Same explicit partial-rectangle parity for the blurred path. This is exactly the sort of
        // thing that can silently drift when rectangle math changes.
        [Test]
        [TestCaseSource(nameof(PartialRectScenarios))]
        public void CannyEdgeDetector_PartialRect_MatchesAccordReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = GetRect(scenario, scenario.Width, scenario.Height);

            using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

            var reference = new Accord.Imaging.Filters.CannyEdgeDetector(LowThreshold, HighThreshold) {
                GaussianSigma = GaussianSigma,
                GaussianSize = GaussianSize
            };
            var optimized = new OptimizedCannyEdgeDetector(LowThreshold, HighThreshold) {
                GaussianSigma = GaussianSigma,
                GaussianSize = GaussianSize
            };

            reference.ApplyInPlace(expected, rect);
            optimized.ApplyInPlace(actual, rect);

            AssertBitmapExact(scenario.Name, "BlurredPartialRect", sourcePixels, expected, actual);
        }

        // Parallel code can be "correct on average" but still nondeterministic between runs. This test
        // exists specifically to catch that class of failure.
        [Test]
        [TestCaseSource(nameof(DeterminismScenarios))]
        public void NoBlurCannyEdgeDetector_RepeatedRuns_AreDeterministic(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            byte[]? expectedBytes = null;

            for (int iteration = 0; iteration < 5; iteration++) {
                using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
                var optimized = new OptimizedNoBlurCannyEdgeDetector(LowThreshold, HighThreshold);

                optimized.ApplyInPlace(actual);

                byte[] actualBytes = ReadBitmapBytes(actual);
                expectedBytes ??= actualBytes;

                Assert.That(actualBytes, Is.EqualTo(expectedBytes), $"Iteration {iteration} produced a different no-blur Canny bitmap.");
            }
        }

        // Do the same determinism check for the blurred wrapper, because the blur stage and the Canny
        // stage are both parallelized and either one could introduce run-to-run drift.
        [Test]
        [TestCaseSource(nameof(DeterminismScenarios))]
        public void CannyEdgeDetector_RepeatedRuns_AreDeterministic(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            byte[]? expectedBytes = null;

            for (int iteration = 0; iteration < 5; iteration++) {
                using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
                var optimized = new OptimizedCannyEdgeDetector(LowThreshold, HighThreshold) {
                    GaussianSigma = GaussianSigma,
                    GaussianSize = GaussianSize
                };

                optimized.ApplyInPlace(actual);

                byte[] actualBytes = ReadBitmapBytes(actual);
                expectedBytes ??= actualBytes;

                Assert.That(actualBytes, Is.EqualTo(expectedBytes), $"Iteration {iteration} produced a different blurred Canny bitmap.");
            }
        }

        // Keep scenario naming centralized so the NUnit output stays readable when the matrix grows.
        private static TestCaseData Scenario(InputScenario scenario) {
            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        // Most cases use the full frame, but partial-rectangle scenarios can override that here.
        private static Rectangle GetRect(InputScenario scenario, int width, int height) {
            return scenario.CreateRect?.Invoke(width, height) ?? new Rectangle(0, 0, width, height);
        }

        // Build a grayscale bitmap and fill row padding with a sentinel. This is important because the
        // hardening suite compares the full bitmap buffer, including padding bytes.
        private static Bitmap CreateGray8Bitmap(int width, int height, byte[] pixels) {
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

        // GDI+ grayscale bitmaps need a proper palette or the artifact images become misleading.
        private static ColorPalette CreateGrayscalePalette(ColorPalette palette) {
            for (int i = 0; i < 256; i++) {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }

            return palette;
        }

        // The sentinel padding value is not random decoration. It makes any row-overrun or padding write
        // bugs show up immediately in the exact buffer compare.
        private static void FillGray8Bitmap(Bitmap bitmap, byte[] pixels) {
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
                    Marshal.Copy(rowBuffer, 0, IntPtr.Add(data.Scan0, data.Stride * y), rowBuffer.Length);
                }
            } finally {
                bitmap.UnlockBits(data);
            }
        }

        // This is stricter than the older logical-pixel compare: it checks the entire bitmap buffer,
        // including stride padding, and it writes out artifacts when a mismatch is found.
        private static void AssertBitmapExact(string scenarioName, string resultName, byte[] inputPixels, Bitmap expected, Bitmap actual) {
            Assert.That(actual.PixelFormat, Is.EqualTo(expected.PixelFormat), "Pixel format mismatch.");
            Assert.That(actual.Width, Is.EqualTo(expected.Width), "Width mismatch.");
            Assert.That(actual.Height, Is.EqualTo(expected.Height), "Height mismatch.");

            byte[] expectedBytes = ReadBitmapBytes(expected);
            byte[] actualBytes = ReadBitmapBytes(actual);
            int stride = GetBitmapStride(expected);
            int mismatchIndex = FindFirstMismatch(expectedBytes, actualBytes);

            if (mismatchIndex >= 0) {
                SaveImageArtifacts(scenarioName, resultName, inputPixels, expected.Width, expected.Height, stride, expectedBytes, actualBytes, mismatchIndex);
                Assert.Fail($"Bitmap output mismatch at byte index {mismatchIndex}.");
            }
        }

        // Read the actual stride because tests care about the exact in-memory output, not only the
        // logical width and height.
        private static int GetBitmapStride(Bitmap image) {
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

            try {
                return Math.Abs(data.Stride);
            } finally {
                image.UnlockBits(data);
            }
        }

        // Read the full bitmap backing store row by row so padding bytes are preserved exactly.
        private static byte[] ReadBitmapBytes(Bitmap image) {
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

            try {
                int stride = Math.Abs(data.Stride);
                byte[] buffer = new byte[stride * image.Height];
                byte[] rowBuffer = new byte[stride];

                for (int y = 0; y < image.Height; y++) {
                    Marshal.Copy(IntPtr.Add(data.Scan0, data.Stride * y), rowBuffer, 0, rowBuffer.Length);
                    Buffer.BlockCopy(rowBuffer, 0, buffer, y * stride, rowBuffer.Length);
                }

                return buffer;
            } finally {
                image.UnlockBits(data);
            }
        }

        // Keep the first mismatch only. For debugging we care most about the earliest divergence.
        private static int FindFirstMismatch(byte[] expected, byte[] actual) {
            if (expected.Length != actual.Length) {
                return Math.Min(expected.Length, actual.Length);
            }

            for (int i = 0; i < expected.Length; i++) {
                if (expected[i] != actual[i]) {
                    return i;
                }
            }

            return -1;
        }

        // Artifact generation matters because "buffers differ" is not enough when someone has to review
        // a regression quickly. Input, expected, actual, diff, and byte-position metadata make failures
        // much easier to diagnose.
        private static void SaveImageArtifacts(string scenarioName, string resultName, byte[] inputPixels, int width, int height, int stride, byte[] expected, byte[] actual, int mismatchIndex) {
            string directory = CreateArtifactDirectory(scenarioName, resultName);
            WriteGray8Png(Path.Combine(directory, "input.png"), width, height, inputPixels);
            WriteGray8PngFromStridedBuffer(Path.Combine(directory, "expected.png"), width, height, stride, expected);
            WriteGray8PngFromStridedBuffer(Path.Combine(directory, "actual.png"), width, height, stride, actual);
            WriteGray8PngFromStridedBuffer(Path.Combine(directory, "diff.png"), width, height, stride, CreateDiffBuffer(expected, actual));
            WriteMismatchMetadata(Path.Combine(directory, "metadata.txt"), width, height, stride, mismatchIndex);
        }

        // Store artifacts under the test work directory so failures are collected alongside the test run.
        private static string CreateArtifactDirectory(string scenarioName, string resultName) {
            string testName = SanitizePathSegment(TestContext.CurrentContext.Test.Name);
            string scenarioSegment = SanitizePathSegment(scenarioName);
            string resultSegment = SanitizePathSegment(resultName);
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Artifacts", "Canny", testName, scenarioSegment, resultSegment);
            Directory.CreateDirectory(directory);
            return directory;
        }

        // Keep path generation robust even if test names change.
        private static string SanitizePathSegment(string value) {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] sanitized = value.ToCharArray();

            for (int i = 0; i < sanitized.Length; i++) {
                for (int j = 0; j < invalidChars.Length; j++) {
                    if (sanitized[i] == invalidChars[j]) {
                        sanitized[i] = '_';
                        break;
                    }
                }
            }

            return new string(sanitized);
        }

        // Write logical grayscale pixels as a PNG so artifact directories are human-readable.
        private static void WriteGray8Png(string path, int width, int height, byte[] pixels) {
            if (width <= 0 || height <= 0) {
                return;
            }

            using Bitmap bitmap = CreateGray8Bitmap(width, height, pixels);
            bitmap.Save(path, ImageFormat.Png);
        }

        // Convert a strided image buffer back to logical pixels before saving it as an artifact image.
        private static void WriteGray8PngFromStridedBuffer(string path, int width, int height, int stride, byte[] bytes) {
            if (width <= 0 || height <= 0) {
                return;
            }

            byte[] logicalPixels = new byte[width * height];

            for (int y = 0; y < height; y++) {
                Buffer.BlockCopy(bytes, y * stride, logicalPixels, y * width, width);
            }

            WriteGray8Png(path, width, height, logicalPixels);
        }

        // The metadata file points directly to the first mismatching byte and whether that byte was in
        // padding. That is often the fastest way to tell if a regression is a content bug or a stride bug.
        private static void WriteMismatchMetadata(string path, int width, int height, int stride, int mismatchIndex) {
            int row = (stride > 0) ? mismatchIndex / stride : 0;
            int column = (stride > 0) ? mismatchIndex % stride : mismatchIndex;
            bool paddingMismatch = column >= width;

            File.WriteAllText(path,
                $"First mismatch byte index: {mismatchIndex}{Environment.NewLine}" +
                $"Row: {row}{Environment.NewLine}" +
                $"Column: {column}{Environment.NewLine}" +
                $"Logical width: {width}{Environment.NewLine}" +
                $"Height: {height}{Environment.NewLine}" +
                $"Stride: {stride}{Environment.NewLine}" +
                $"In padding: {paddingMismatch}{Environment.NewLine}");
        }

        // Absolute difference is enough for visualization; the point is to highlight where the outputs
        // diverged, not to invent another correctness metric.
        private static byte[] CreateDiffBuffer(byte[] expected, byte[] actual) {
            int length = Math.Min(expected.Length, actual.Length);
            byte[] diff = new byte[length];

            for (int i = 0; i < length; i++) {
                diff[i] = (byte)Math.Abs(expected[i] - actual[i]);
            }

            return diff;
        }
    }
}
