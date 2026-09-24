#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

// Uncomment to run this file's exhaustive image-analysis tests instead of reporting them as ignored.
//#define RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS

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
    public partial class CannyEdgeDetectorVariantTests {
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

        private const byte DefaultLowThreshold = 10;
        private const byte DefaultHighThreshold = 80;
        private const double DefaultGaussianSigma = 1.4;
        private const int DefaultGaussianSize = 5;
        private const string PopularFeatureResolutionKey = "Planetary_1936x1096";
        private const string CustomGaussianResolutionKey = "Planetary_1936x1096";
        private const string ProofCannyCategory = "CannyEdgeDetectorProof";
        private const string ExhaustiveCannyCategory = "CannyEdgeDetectorExhaustive";
        private const string ExhaustiveCannyIgnoreReason = "Disabled because exhaustive Canny coverage is too long for normal test runs. Enable manually when validating Canny optimization changes.";
        private const int ProofWidth = 257;
        private const int ProofHeight = 259;
        private const int ExhaustiveBinaryWidth = 4;
        private const int ExhaustiveBinaryHeight = 4;

        private static IEnumerable<TestCaseData> FormatCoverageCases() {
            foreach (var resolution in AstroCameraResolutions) {
                yield return new TestCaseData(
                        $"{DeterministicImageFixtures.RepresentativeFormatCoverage.Name}_{resolution.Key}",
                        resolution.Value.Width,
                        resolution.Value.Height,
                        DeterministicImageFixtures.RepresentativeFormatCoverage.CreateBytes)
                    .SetName($"FormatCoverage_{resolution.Key}_{DeterministicImageFixtures.RepresentativeFormatCoverage.Name}");
            }
        }

        private static IEnumerable<TestCaseData> FeatureCoverageCases() {
            int width = AstroCameraResolutions[PopularFeatureResolutionKey].Width;
            int height = AstroCameraResolutions[PopularFeatureResolutionKey].Height;

            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.CuratedFeatures) {
                yield return new TestCaseData(
                        $"{fixture.Name}_{PopularFeatureResolutionKey}",
                        width,
                        height,
                        fixture.CreateBytes)
                    .SetName($"FeatureCoverage_{fixture.Name}_{PopularFeatureResolutionKey}");
            }

            foreach (DeterministicImageFixtures.ThresholdAwareImageFixture fixture in DeterministicImageFixtures.HysteresisFeatures) {
                yield return new TestCaseData(
                        $"{fixture.Name}_{PopularFeatureResolutionKey}",
                        width,
                        height,
                        new Func<int, int, byte[]>((imageWidth, imageHeight) => fixture.CreateBytes(imageWidth, imageHeight, DefaultLowThreshold, DefaultHighThreshold)))
                    .SetName($"FeatureCoverage_{fixture.Name}_{PopularFeatureResolutionKey}");
            }
        }

        private static IEnumerable<TestCaseData> FixtureCases() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.CuratedFeatures) {
                yield return new TestCaseData(fixture.Name, fixture.CreateBytes)
                    .SetName(fixture.Name);
            }
        }

        // This is the production star-detection path: blurred Canny with the normal thresholds.
        // Every listed resolution must match Accord exactly, not just produce similar-looking edges.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(FormatCoverageCases))]
        public void CannyEdgeDetector_DefaultParameters_CoversSupportedResolutions(string scenarioName, int width, int height, Func<int, int, byte[]> createPixels) {
            byte[] sourcePixels = createPixels(width, height);
            byte[] expectedPixels = ComputeReferenceBlurredCanny(sourcePixels, width, height, lowThreshold: DefaultLowThreshold, highThreshold: DefaultHighThreshold, gaussianSigma: DefaultGaussianSigma, gaussianSize: DefaultGaussianSize);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedCannyEdgeDetector(lowThreshold: DefaultLowThreshold, highThreshold: DefaultHighThreshold);

            optimized.ApplyInPlace(optimizedInput);

            Assert.That(HasStridePadding(optimizedInput, bytesPerPixel: 1), Is.EqualTo(ShouldHaveStridePadding(width, bytesPerPixel: 1)), "Unexpected stride padding.");
            AssertBitExactPixels(expectedPixels, optimizedInput, scenarioName);
        }

        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(FeatureCoverageCases))]
        public void CannyEdgeDetector_DefaultParameters_MatchesAccordReferenceAcrossFeatures(string scenarioName, int width, int height, Func<int, int, byte[]> createPixels) {
            byte[] sourcePixels = createPixels(width, height);
            byte[] expectedPixels = ComputeReferenceBlurredCanny(sourcePixels, width, height, lowThreshold: DefaultLowThreshold, highThreshold: DefaultHighThreshold, gaussianSigma: DefaultGaussianSigma, gaussianSize: DefaultGaussianSize);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedCannyEdgeDetector(lowThreshold: DefaultLowThreshold, highThreshold: DefaultHighThreshold);

            optimized.ApplyInPlace(optimizedInput);

            AssertBitExactPixels(expectedPixels, optimizedInput, scenarioName);
        }

        // The optimized blur has separate interior and border code paths, so validate a larger kernel
        // size as well. This catches errors that only appear when the Gaussian footprint changes.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(FixtureCases))]
        public void CannyEdgeDetector_CustomGaussianSize_MatchesAccordReference(string fixtureName, Func<int, int, byte[]> createPixels) {
            int width = AstroCameraResolutions[CustomGaussianResolutionKey].Width;
            int height = AstroCameraResolutions[CustomGaussianResolutionKey].Height;
            byte[] sourcePixels = createPixels(width, height);
            byte[] expectedPixels = ComputeReferenceBlurredCanny(sourcePixels, width, height, lowThreshold: 20, highThreshold: 100, gaussianSigma: DefaultGaussianSigma, gaussianSize: 10);

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
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(FormatCoverageCases))]
        public void NoBlurCannyEdgeDetector_CoversSupportedResolutions(string scenarioName, int width, int height, Func<int, int, byte[]> createPixels) {
            byte[] sourcePixels = createPixels(width, height);
            byte[] expectedPixels = ComputeReferenceNoBlurCanny(sourcePixels, width, height, lowThreshold: DefaultLowThreshold, highThreshold: DefaultHighThreshold);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedNoBlurCannyEdgeDetector(lowThreshold: DefaultLowThreshold, highThreshold: DefaultHighThreshold);

            optimized.ApplyInPlace(optimizedInput);

            Assert.That(HasStridePadding(optimizedInput, bytesPerPixel: 1), Is.EqualTo(ShouldHaveStridePadding(width, bytesPerPixel: 1)), "Unexpected stride padding.");
            AssertBitExactPixels(expectedPixels, optimizedInput, scenarioName);
        }

        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(FeatureCoverageCases))]
        public void NoBlurCannyEdgeDetector_MatchesPreservedReferenceAcrossFeatures(string scenarioName, int width, int height, Func<int, int, byte[]> createPixels) {
            byte[] sourcePixels = createPixels(width, height);
            byte[] expectedPixels = ComputeReferenceNoBlurCanny(sourcePixels, width, height, lowThreshold: DefaultLowThreshold, highThreshold: DefaultHighThreshold);

            using Bitmap optimizedInput = CreateGray8Bitmap(width, height, sourcePixels);

            var optimized = new OptimizedNoBlurCannyEdgeDetector(lowThreshold: DefaultLowThreshold, highThreshold: DefaultHighThreshold);

            optimized.ApplyInPlace(optimizedInput);

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

    // Additional black-box coverage for the same Canny test group. Keep it in the same type so the
    // full regression surface stays together: format coverage, feature coverage, ROI coverage,
    // determinism, and randomized scenes.
    public partial class CannyEdgeDetectorVariantTests {
        // NUnit requires public parameter types for public parameterized test methods.
        // Keeping the scenario as a small record-like type makes the test matrix easy to read.
        public sealed class InputScenario {
            public required string Name { get; init; }
            public required int Width { get; init; }
            public required int Height { get; init; }
            public required Func<int, int, byte[]> CreatePixels { get; init; }
            public Func<int, int, Rectangle>? CreateRect { get; init; }
        }

        public sealed class HysteresisThresholdScenario {
            public required string Name { get; init; }
            public required int Width { get; init; }
            public required int Height { get; init; }
            public required byte LowThreshold { get; init; }
            public required byte HighThreshold { get; init; }
            public required Func<int, int, byte, byte, byte[]> CreatePixels { get; init; }
        }

        // These values match the production star-detection defaults. Hardening should validate the
        // exact behavior that the application actually runs, not a synthetic threshold pair that no
        // real code path uses.
        private const int PlanetaryWidth = 1936;
        private const int PlanetaryHeight = 1096;
        private const int PlanetaryRoiWidth = 1937;
        private const int PlanetaryRoiHeight = 1097;
        private const int RandomFeatureScenarioCount = 100;
        private static readonly IReadOnlyList<int> RandomFeatureSeeds = CreateDeterministicRandomFeatureSeeds(RandomFeatureScenarioCount);
        private static readonly IReadOnlyList<(byte LowThreshold, byte HighThreshold)> HysteresisThresholdPairs = new (byte, byte)[] {
            ((byte)4, (byte)20),
            ((byte)10, (byte)80),
            ((byte)40, (byte)80),
            ((byte)79, (byte)120),
            ((byte)120, (byte)200),
            ((byte)200, (byte)240)
        };

        // Small odd-width proof scenarios are enabled in normal test runs. They still exercise stride
        // padding, all curated feature families, and all hysteresis fixtures, but avoid the large camera
        // frame cost of the explicit matrix below.
        private static IEnumerable<TestCaseData> RepresentativeCuratedProofScenarios() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.CuratedFeatures) {
                yield return Scenario(new InputScenario {
                    Name = $"Proof_{fixture.Name}_{ProofWidth}x{ProofHeight}",
                    Width = ProofWidth,
                    Height = ProofHeight,
                    CreatePixels = fixture.CreateBytes
                });
            }
        }

        private static IEnumerable<TestCaseData> RepresentativeProofScenarios() {
            foreach (TestCaseData scenario in RepresentativeCuratedProofScenarios()) {
                yield return scenario;
            }

            foreach (DeterministicImageFixtures.ThresholdAwareImageFixture fixture in DeterministicImageFixtures.HysteresisFeatures) {
                yield return Scenario(new InputScenario {
                    Name = $"Proof_{fixture.Name}_{ProofWidth}x{ProofHeight}",
                    Width = ProofWidth,
                    Height = ProofHeight,
                    CreatePixels = (width, height) => fixture.CreateBytes(width, height, DefaultLowThreshold, DefaultHighThreshold)
                });
            }
        }

        private static IEnumerable<TestCaseData> HysteresisThresholdScenarios() {
            foreach (DeterministicImageFixtures.ThresholdAwareImageFixture fixture in DeterministicImageFixtures.HysteresisFeatures) {
                foreach ((byte lowThreshold, byte highThreshold) in HysteresisThresholdPairs) {
                    yield return new TestCaseData(new HysteresisThresholdScenario {
                        Name = $"Proof_{fixture.Name}_{lowThreshold}_{highThreshold}_{ProofWidth}x{ProofHeight}",
                        Width = ProofWidth,
                        Height = ProofHeight,
                        LowThreshold = lowThreshold,
                        HighThreshold = highThreshold,
                        CreatePixels = fixture.CreateBytes
                    }).SetName($"Proof_{fixture.Name}_Low{lowThreshold}_High{highThreshold}_{ProofWidth}x{ProofHeight}");
                }
            }
        }

        // Keep the heavy black-box parity checks focused on one popular real-world resolution and run
        // the full curated feature family there. This keeps the suite systematic without exploding the matrix.
        private static IEnumerable<TestCaseData> FeatureScenarios() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.CuratedFeatures) {
                yield return Scenario(new InputScenario {
                    Name = $"{fixture.Name}_Planetary_1936x1096",
                    Width = PlanetaryWidth,
                    Height = PlanetaryHeight,
                    CreatePixels = fixture.CreateBytes
                });
            }

            foreach (DeterministicImageFixtures.ThresholdAwareImageFixture fixture in DeterministicImageFixtures.HysteresisFeatures) {
                yield return Scenario(new InputScenario {
                    Name = $"{fixture.Name}_Planetary_1936x1096",
                    Width = PlanetaryWidth,
                    Height = PlanetaryHeight,
                    CreatePixels = (width, height) => fixture.CreateBytes(width, height, DefaultLowThreshold, DefaultHighThreshold)
                });
            }
        }

        private static IEnumerable<TestCaseData> RandomFeatureScenarios() {
            foreach (int seed in RandomFeatureSeeds) {
                yield return Scenario(new InputScenario {
                    Name = $"RandomFeatureMix_Seed{seed}_Planetary_1936x1096",
                    Width = PlanetaryWidth,
                    Height = PlanetaryHeight,
                    CreatePixels = (width, height) => DeterministicImageFixtures.CreateRandomFeatureSceneBytes(width, height, seed)
                });
            }
        }

        // Partial-rectangle processing is easy to break accidentally because it stresses coordinate
        // math, border cleanup, and stride handling all at once. Keep the ROI geometry fixed and run
        // the curated/hysteresis family through it on the padded ROI frame.
        private static IEnumerable<TestCaseData> PartialRectScenarios() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.CuratedFeatures) {
                yield return Scenario(new InputScenario {
                    Name = $"PartialRect_{fixture.Name}_PlanetaryRoi_1937x1097",
                    Width = PlanetaryRoiWidth,
                    Height = PlanetaryRoiHeight,
                    CreatePixels = fixture.CreateBytes,
                    CreateRect = static (width, height) => new Rectangle(61, 43, width - 122, height - 86)
                });
            }

            foreach (DeterministicImageFixtures.ThresholdAwareImageFixture fixture in DeterministicImageFixtures.HysteresisFeatures) {
                yield return Scenario(new InputScenario {
                    Name = $"PartialRect_{fixture.Name}_PlanetaryRoi_1937x1097",
                    Width = PlanetaryRoiWidth,
                    Height = PlanetaryRoiHeight,
                    CreatePixels = (width, height) => fixture.CreateBytes(width, height, DefaultLowThreshold, DefaultHighThreshold),
                    CreateRect = static (width, height) => new Rectangle(61, 43, width - 122, height - 86)
                });
            }
        }

        // Determinism does not need the full matrix. Keep a compact set of broad-coverage fixtures here.
        private static IEnumerable<TestCaseData> DeterminismScenarios() {
            yield return Scenario(new InputScenario {
                Name = "Determinism_SparseStarField_PlanetaryRoi_1937x1097",
                Width = PlanetaryRoiWidth,
                Height = PlanetaryRoiHeight,
                CreatePixels = DeterministicImageFixtures.SparseStarField.CreateBytes
            });
            yield return Scenario(new InputScenario {
                Name = "Determinism_FeatureMix_PlanetaryRoi_1937x1097",
                Width = PlanetaryRoiWidth,
                Height = PlanetaryRoiHeight,
                CreatePixels = DeterministicImageFixtures.FeatureMix.CreateBytes
            });
            yield return Scenario(new InputScenario {
                Name = "Determinism_Structured_PlanetaryRoi_1937x1097",
                Width = PlanetaryRoiWidth,
                Height = PlanetaryRoiHeight,
                CreatePixels = DeterministicImageFixtures.Structured.CreateBytes
            });
            yield return Scenario(new InputScenario {
                Name = "Determinism_HysteresisConnectedWeakEdge_PlanetaryRoi_1937x1097",
                Width = PlanetaryRoiWidth,
                Height = PlanetaryRoiHeight,
                CreatePixels = (width, height) => DeterministicImageFixtures.HysteresisConnectedWeakEdge.CreateBytes(width, height, DefaultLowThreshold, DefaultHighThreshold)
            });
            yield return Scenario(new InputScenario {
                Name = "Determinism_RandomFeatureMix_Seed101_PlanetaryRoi_1937x1097",
                Width = PlanetaryRoiWidth,
                Height = PlanetaryRoiHeight,
                CreatePixels = (width, height) => DeterministicImageFixtures.CreateRandomFeatureSceneBytes(width, height, 101)
            });
        }

        [Test]
        [Category(ProofCannyCategory)]
        [TestCaseSource(nameof(RepresentativeProofScenarios))]
        public void NoBlurCannyEdgeDetector_RepresentativeProof_MatchesReference(InputScenario scenario) {
            AssertNoBlurMatchesReference(scenario, "NoBlurProof", DefaultLowThreshold, DefaultHighThreshold);
        }

        [Test]
        [Category(ProofCannyCategory)]
        [TestCaseSource(nameof(RepresentativeProofScenarios))]
        public void CannyEdgeDetector_RepresentativeProof_MatchesAccordReference(InputScenario scenario) {
            AssertBlurredMatchesAccordReference(scenario, "BlurredProof", DefaultLowThreshold, DefaultHighThreshold, DefaultGaussianSigma, DefaultGaussianSize);
        }

        [Test]
        [Category(ProofCannyCategory)]
        [TestCaseSource(nameof(RepresentativeCuratedProofScenarios))]
        public void CannyEdgeDetector_CustomGaussianSize_RepresentativeProof_MatchesAccordReference(InputScenario scenario) {
            AssertBlurredMatchesAccordReference(scenario, "BlurredCustomGaussianProof", 20, 100, DefaultGaussianSigma, 10);
        }

        [Test]
        [Category(ProofCannyCategory)]
        [TestCaseSource(nameof(HysteresisThresholdScenarios))]
        public void NoBlurCannyEdgeDetector_HysteresisThresholdMatrix_MatchesReference(HysteresisThresholdScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height, scenario.LowThreshold, scenario.HighThreshold);
            AssertNoBlurMatchesReference(scenario.Name, sourcePixels, scenario.Width, scenario.Height, "NoBlurHysteresisThresholdProof", scenario.LowThreshold, scenario.HighThreshold);
        }

        [Test]
        [Category(ProofCannyCategory)]
        [TestCaseSource(nameof(HysteresisThresholdScenarios))]
        public void CannyEdgeDetector_HysteresisThresholdMatrix_MatchesAccordReference(HysteresisThresholdScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height, scenario.LowThreshold, scenario.HighThreshold);
            AssertBlurredMatchesAccordReference(scenario.Name, sourcePixels, scenario.Width, scenario.Height, "BlurredHysteresisThresholdProof", scenario.LowThreshold, scenario.HighThreshold, DefaultGaussianSigma, DefaultGaussianSize);
        }

        [Test]
        [Category(ProofCannyCategory)]
        public void NoBlurCannyEdgeDetector_AllFourByFourBinaryInputs_MatchReference() {
            int caseCount = 1 << (ExhaustiveBinaryWidth * ExhaustiveBinaryHeight);

            for (int mask = 0; mask < caseCount; mask++) {
                byte[] sourcePixels = CreateBinaryMaskPixels(ExhaustiveBinaryWidth, ExhaustiveBinaryHeight, mask);

                using Bitmap expected = CreateGray8Bitmap(ExhaustiveBinaryWidth, ExhaustiveBinaryHeight, sourcePixels);
                using Bitmap actual = CreateGray8Bitmap(ExhaustiveBinaryWidth, ExhaustiveBinaryHeight, sourcePixels);

                var reference = new global::NINA.Tests.NoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);
                var optimized = new OptimizedNoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);

                reference.ApplyInPlace(expected);
                optimized.ApplyInPlace(actual);

                byte[] expectedBytes = ReadBitmapBytes(expected);
                byte[] actualBytes = ReadBitmapBytes(actual);
                int mismatchIndex = FindFirstMismatch(expectedBytes, actualBytes);

                if (mismatchIndex >= 0) {
                    Assert.Fail($"4x4 binary no-blur Canny mismatch for mask 0x{mask:X4} at byte index {mismatchIndex}.");
                }
            }
        }

        // This is the black-box no-blur hardening test. It does not know anything about internal
        // stages. It only checks the final bitmap against the preserved reference implementation.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(FeatureScenarios))]
        public void NoBlurCannyEdgeDetector_MatchesReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = GetRect(scenario, scenario.Width, scenario.Height);

            using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

            var reference = new global::NINA.Tests.NoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);
            var optimized = new OptimizedNoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);

            reference.ApplyInPlace(expected, rect);
            optimized.ApplyInPlace(actual, rect);

            AssertBitmapExact(scenario.Name, "NoBlurFinal", sourcePixels, expected, actual);
        }

        // Same idea, but for the blurred production path. This remains a strict final-output compare
        // against Accord, which is still the most important external behavior contract.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(FeatureScenarios))]
        public void BlurredCannyEdgeDetector_MatchesAccordReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = GetRect(scenario, scenario.Width, scenario.Height);

            using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

            var reference = new Accord.Imaging.Filters.CannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold) {
                GaussianSigma = DefaultGaussianSigma,
                GaussianSize = DefaultGaussianSize
            };
            var optimized = new OptimizedCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold) {
                GaussianSigma = DefaultGaussianSigma,
                GaussianSize = DefaultGaussianSize
            };

            reference.ApplyInPlace(expected, rect);
            optimized.ApplyInPlace(actual, rect);

            AssertBitmapExact(scenario.Name, "BlurredFinal", sourcePixels, expected, actual);
        }

        // The runtime-random scenes stress combinations of stars, blobs, patches, contours, and squares
        // at center and border positions. Any mismatch writes out the input/reference/actual/diff artifacts.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(RandomFeatureScenarios))]
        public void NoBlurCannyEdgeDetector_RandomFeatureScenes_MatchReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            const string resultName = "NoBlurRandom";

            try {
                using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
                using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

                var reference = new global::NINA.Tests.NoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);
                var optimized = new OptimizedNoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);

                reference.ApplyInPlace(expected);
                optimized.ApplyInPlace(actual);

                AssertBitmapExact(scenario.Name, resultName, sourcePixels, expected, actual);
            } catch {
                SaveExceptionArtifacts(scenario.Name, resultName, sourcePixels, scenario.Width, scenario.Height);
                throw;
            }
        }

        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(RandomFeatureScenarios))]
        public void BlurredCannyEdgeDetector_RandomFeatureScenes_MatchAccordReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            const string resultName = "BlurredRandom";

            try {
                using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
                using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

                var reference = new Accord.Imaging.Filters.CannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold) {
                    GaussianSigma = DefaultGaussianSigma,
                    GaussianSize = DefaultGaussianSize
                };
                var optimized = new OptimizedCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold) {
                    GaussianSigma = DefaultGaussianSigma,
                    GaussianSize = DefaultGaussianSize
                };

                reference.ApplyInPlace(expected);
                optimized.ApplyInPlace(actual);

                AssertBitmapExact(scenario.Name, resultName, sourcePixels, expected, actual);
            } catch {
                SaveExceptionArtifacts(scenario.Name, resultName, sourcePixels, scenario.Width, scenario.Height);
                throw;
            }
        }

        // Partial-rectangle parity deserves its own named test even though the scenario also exists as
        // data, because reviewers often look for this exact behavior by name.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(PartialRectScenarios))]
        public void NoBlurCannyEdgeDetector_PartialRect_MatchesReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = GetRect(scenario, scenario.Width, scenario.Height);

            using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

            var reference = new global::NINA.Tests.NoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);
            var optimized = new OptimizedNoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);

            reference.ApplyInPlace(expected, rect);
            optimized.ApplyInPlace(actual, rect);

            AssertBitmapExact(scenario.Name, "NoBlurPartialRect", sourcePixels, expected, actual);
        }

        // Same explicit partial-rectangle parity for the blurred path. This is exactly the sort of
        // thing that can silently drift when rectangle math changes.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(PartialRectScenarios))]
        public void CannyEdgeDetector_PartialRect_MatchesAccordReference(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            Rectangle rect = GetRect(scenario, scenario.Width, scenario.Height);

            using Bitmap expected = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);

            var reference = new Accord.Imaging.Filters.CannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold) {
                GaussianSigma = DefaultGaussianSigma,
                GaussianSize = DefaultGaussianSize
            };
            var optimized = new OptimizedCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold) {
                GaussianSigma = DefaultGaussianSigma,
                GaussianSize = DefaultGaussianSize
            };

            reference.ApplyInPlace(expected, rect);
            optimized.ApplyInPlace(actual, rect);

            AssertBitmapExact(scenario.Name, "BlurredPartialRect", sourcePixels, expected, actual);
        }

        // Parallel code can be "correct on average" but still nondeterministic between runs. This test
        // exists specifically to catch that class of failure.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(DeterminismScenarios))]
        public void NoBlurCannyEdgeDetector_RepeatedRuns_AreDeterministic(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            byte[]? expectedBytes = null;

            for (int iteration = 0; iteration < 5; iteration++) {
                using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
                var optimized = new OptimizedNoBlurCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold);

                optimized.ApplyInPlace(actual);

                byte[] actualBytes = ReadBitmapBytes(actual);
                expectedBytes ??= actualBytes;

                Assert.That(actualBytes, Is.EqualTo(expectedBytes), $"Iteration {iteration} produced a different no-blur Canny bitmap.");
            }
        }

        // Do the same determinism check for the blurred wrapper, because the blur stage and the Canny
        // stage are both parallelized and either one could introduce run-to-run drift.
        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveCannyIgnoreReason)]
#endif
        [Category(ExhaustiveCannyCategory)]
        [TestCaseSource(nameof(DeterminismScenarios))]
        public void CannyEdgeDetector_RepeatedRuns_AreDeterministic(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            byte[]? expectedBytes = null;

            for (int iteration = 0; iteration < 5; iteration++) {
                using Bitmap actual = CreateGray8Bitmap(scenario.Width, scenario.Height, sourcePixels);
                var optimized = new OptimizedCannyEdgeDetector(DefaultLowThreshold, DefaultHighThreshold) {
                    GaussianSigma = DefaultGaussianSigma,
                    GaussianSize = DefaultGaussianSize
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

        private static IReadOnlyList<int> CreateDeterministicRandomFeatureSeeds(int count) {
            var seeds = new List<int>(count);
            uint state = 0xC0FFEEu;

            while (seeds.Count < count) {
                state = unchecked(state * 1664525u + 1013904223u);
                seeds.Add(unchecked((int)state));
            }

            return seeds;
        }

        private static void AssertNoBlurMatchesReference(InputScenario scenario, string resultName, byte lowThreshold, byte highThreshold) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            AssertNoBlurMatchesReference(scenario.Name, sourcePixels, scenario.Width, scenario.Height, resultName, lowThreshold, highThreshold);
        }

        private static void AssertNoBlurMatchesReference(string scenarioName, byte[] sourcePixels, int width, int height, string resultName, byte lowThreshold, byte highThreshold) {
            using Bitmap expected = CreateGray8Bitmap(width, height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(width, height, sourcePixels);

            var reference = new global::NINA.Tests.NoBlurCannyEdgeDetector(lowThreshold, highThreshold);
            var optimized = new OptimizedNoBlurCannyEdgeDetector(lowThreshold, highThreshold);

            reference.ApplyInPlace(expected);
            optimized.ApplyInPlace(actual);

            AssertBitmapExact(scenarioName, resultName, sourcePixels, expected, actual);
        }

        private static void AssertBlurredMatchesAccordReference(InputScenario scenario, string resultName, byte lowThreshold, byte highThreshold, double gaussianSigma, int gaussianSize) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            AssertBlurredMatchesAccordReference(scenario.Name, sourcePixels, scenario.Width, scenario.Height, resultName, lowThreshold, highThreshold, gaussianSigma, gaussianSize);
        }

        private static void AssertBlurredMatchesAccordReference(string scenarioName, byte[] sourcePixels, int width, int height, string resultName, byte lowThreshold, byte highThreshold, double gaussianSigma, int gaussianSize) {
            using Bitmap expected = CreateGray8Bitmap(width, height, sourcePixels);
            using Bitmap actual = CreateGray8Bitmap(width, height, sourcePixels);

            var reference = new Accord.Imaging.Filters.CannyEdgeDetector(lowThreshold, highThreshold) {
                GaussianSigma = gaussianSigma,
                GaussianSize = gaussianSize
            };
            var optimized = new OptimizedCannyEdgeDetector(lowThreshold, highThreshold) {
                GaussianSigma = gaussianSigma,
                GaussianSize = gaussianSize
            };

            reference.ApplyInPlace(expected);
            optimized.ApplyInPlace(actual);

            AssertBitmapExact(scenarioName, resultName, sourcePixels, expected, actual);
        }

        private static byte[] CreateBinaryMaskPixels(int width, int height, int mask) {
            byte[] pixels = new byte[width * height];

            for (int i = 0; i < pixels.Length; i++) {
                pixels[i] = ((mask & (1 << i)) == 0) ? (byte)0 : (byte)255;
            }

            return pixels;
        }

        // Most cases use the full frame, but partial-rectangle scenarios can override that here.
        private static Rectangle GetRect(InputScenario scenario, int width, int height) {
            return scenario.CreateRect?.Invoke(width, height) ?? new Rectangle(0, 0, width, height);
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
            WriteMismatchMetadata(Path.Combine(directory, "metadata.txt"), scenarioName, resultName, width, height, stride, mismatchIndex);
        }

        // Random scenes are intentionally harder to reason about from the failure alone, so save the
        // generated input even if the test fails before the exact bitmap compare runs.
        private static void SaveExceptionArtifacts(string scenarioName, string resultName, byte[] inputPixels, int width, int height) {
            string directory = CreateArtifactDirectory(scenarioName, resultName);
            WriteGray8Png(Path.Combine(directory, "input.png"), width, height, inputPixels);
            File.WriteAllText(Path.Combine(directory, "exception.txt"),
                $"Test: {TestContext.CurrentContext.Test.Name}{Environment.NewLine}" +
                $"Scenario: {scenarioName}{Environment.NewLine}" +
                $"Result: {resultName}{Environment.NewLine}" +
                $"Width: {width}{Environment.NewLine}" +
                $"Height: {height}{Environment.NewLine}");
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
        private static void WriteMismatchMetadata(string path, string scenarioName, string resultName, int width, int height, int stride, int mismatchIndex) {
            int row = (stride > 0) ? mismatchIndex / stride : 0;
            int column = (stride > 0) ? mismatchIndex % stride : mismatchIndex;
            bool paddingMismatch = column >= width;

            File.WriteAllText(path,
                $"Test: {TestContext.CurrentContext.Test.Name}{Environment.NewLine}" +
                $"Scenario: {scenarioName}{Environment.NewLine}" +
                $"Result: {resultName}{Environment.NewLine}" +
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
