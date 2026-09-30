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

using Accord.Imaging;
using NINA.Image.ImageAnalysis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using AccordGaussianBlur = Accord.Imaging.Filters.GaussianBlur;
using OptimizedGaussianBlur = NINA.Image.ImageAnalysis.GaussianBlur;

namespace NINA.Test.Image.ImageAnalysis {
    [TestFixture]
    public class GaussianBlurTests {
        private readonly struct AstroCameraResolutionCase {
            public AstroCameraResolutionCase(int width, int height) {
                Width = width;
                Height = height;
            }

            public int Width { get; }
            public int Height { get; }
        }

        public sealed class InputScenario {
            public required string Name { get; init; }
            public required int Width { get; init; }
            public required int Height { get; init; }
            public required Func<int, int, byte[]> CreatePixels { get; init; }
        }

        private static readonly IReadOnlyDictionary<string, AstroCameraResolutionCase> AstroCameraResolutions = new Dictionary<string, AstroCameraResolutionCase> {
            ["Guide_1280x960"] = new AstroCameraResolutionCase(1280, 960),
            ["Planetary_1936x1096"] = new AstroCameraResolutionCase(1936, 1096),
            ["PlanetaryRoi_1937x1097"] = new AstroCameraResolutionCase(1937, 1097),
            ["DeepSky_3096x2080"] = new AstroCameraResolutionCase(3096, 2080),
            ["FourThirds_4144x2822"] = new AstroCameraResolutionCase(4144, 2822)
        };

        private const string RepresentativeKernelResolutionKey = "Planetary_1936x1096";
        private const string RepresentativePaddingResolutionKey = "PlanetaryRoi_1937x1097";
        private const string ProofGaussianCategory = "GaussianBlurProof";
        private const string ExhaustiveGaussianCategory = "GaussianBlurExhaustive";
        private const string ExhaustiveGaussianIgnoreReason = "Disabled because exhaustive Gaussian blur coverage is too long for normal test runs. Enable manually when validating Gaussian blur changes.";
        private const int ProofWidth = 257;
        private const int ProofHeight = 259;

        private static readonly IReadOnlyList<DeterministicImageFixtures.ImageFixture> ProofFixtures = new[] {
            DeterministicImageFixtures.SingleImpulseCenter,
            DeterministicImageFixtures.FeatureMix,
            DeterministicImageFixtures.Structured
        };

        private static IEnumerable<TestCaseData> FormatCoverageScenarios() {
            foreach (var resolution in AstroCameraResolutions) {
                yield return Scenario(new InputScenario {
                    Name = $"FormatCoverage_{resolution.Key}_{DeterministicImageFixtures.RepresentativeFormatCoverage.Name}",
                    Width = resolution.Value.Width,
                    Height = resolution.Value.Height,
                    CreatePixels = DeterministicImageFixtures.RepresentativeFormatCoverage.CreateBytes
                });
            }
        }

        private static IEnumerable<TestCaseData> FeatureCoverageScenarios() {
            int width = AstroCameraResolutions[RepresentativeKernelResolutionKey].Width;
            int height = AstroCameraResolutions[RepresentativeKernelResolutionKey].Height;

            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.CuratedFeatures) {
                yield return Scenario(new InputScenario {
                    Name = $"FeatureCoverage_{fixture.Name}_{RepresentativeKernelResolutionKey}",
                    Width = width,
                    Height = height,
                    CreatePixels = fixture.CreateBytes
                });
            }
        }

        private static IEnumerable<TestCaseData> FixtureSource() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in DeterministicImageFixtures.CuratedFeatures) {
                yield return new TestCaseData(fixture.Name, fixture.CreateBytes).SetName(fixture.Name);
            }
        }

        private static IEnumerable<TestCaseData> ProofScenarios() {
            foreach (DeterministicImageFixtures.ImageFixture fixture in ProofFixtures) {
                yield return Scenario(new InputScenario {
                    Name = $"Proof_{fixture.Name}_{ProofWidth}x{ProofHeight}",
                    Width = ProofWidth,
                    Height = ProofHeight,
                    CreatePixels = fixture.CreateBytes
                });
            }
        }

        private static IEnumerable<TestCaseData> DeterminismProofScenarios() {
            yield return Scenario(new InputScenario {
                Name = $"Proof_Determinism_{DeterministicImageFixtures.FeatureMix.Name}_{ProofWidth}x{ProofHeight}",
                Width = ProofWidth,
                Height = ProofHeight,
                CreatePixels = DeterministicImageFixtures.FeatureMix.CreateBytes
            });
        }

        [Test]
        [Category(ProofGaussianCategory)]
        [TestCaseSource(nameof(ProofScenarios))]
        public void GaussianBlur_DefaultParameters_Proof_MatchesAccordReference(InputScenario scenario) {
            AssertMatchesAccordReference(scenario, sigma: 1.4, size: 5);
        }

        [Test]
        [Category(ProofGaussianCategory)]
        [TestCaseSource(nameof(ProofScenarios))]
        public void GaussianBlur_CustomKernel_Proof_MatchesAccordReference(InputScenario scenario) {
            AssertMatchesAccordReference(scenario, sigma: 2.2, size: 10);
        }

        [Test]
        [Category(ProofGaussianCategory)]
        [TestCaseSource(nameof(ProofScenarios))]
        public void GaussianBlur_Proof_DoesNotModifySourceImage(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            using UnmanagedImage source = CreateGray8Image(scenario.Width, scenario.Height, sourcePixels);
            byte[] before = ReadImageBytes(source);

            using UnmanagedImage _ = CreateOptimizedBlur(source, sigma: 1.4, size: 5);

            byte[] after = ReadImageBytes(source);
            Assert.That(after, Is.EqualTo(before), $"The optimized Gaussian blur should not modify the source image ({scenario.Name}).");
        }

        [Test]
        [Category(ProofGaussianCategory)]
        [TestCaseSource(nameof(DeterminismProofScenarios))]
        public void GaussianBlur_Proof_RepeatedRuns_AreDeterministic(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);
            byte[]? expectedBytes = null;

            for (int iteration = 0; iteration < 3; iteration++) {
                using UnmanagedImage source = CreateGray8Image(scenario.Width, scenario.Height, sourcePixels);
                using UnmanagedImage actual = CreateOptimizedBlur(source, sigma: 1.4, size: 5);

                byte[] actualBytes = ReadImageBytes(actual);
                expectedBytes ??= actualBytes;

                Assert.That(actualBytes, Is.EqualTo(expectedBytes), $"Iteration {iteration} produced a different blurred image ({scenario.Name}).");
            }
        }

        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveGaussianIgnoreReason)]
#endif
        [Category(ExhaustiveGaussianCategory)]
        [TestCaseSource(nameof(FormatCoverageScenarios))]
        public void GaussianBlur_DefaultParameters_CoversSupportedResolutions(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            using UnmanagedImage source = CreateGray8Image(scenario.Width, scenario.Height, sourcePixels);
            using UnmanagedImage expected = CreateReferenceBlur(source, sigma: 1.4, size: 5);
            using UnmanagedImage actual = CreateOptimizedBlur(source, sigma: 1.4, size: 5);

            Assert.That(source.Offset > 0, Is.EqualTo(ShouldHaveStridePadding(scenario.Width)), "Unexpected source stride padding.");
            AssertBitExactImage(expected, actual);
        }

        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveGaussianIgnoreReason)]
#endif
        [Category(ExhaustiveGaussianCategory)]
        [TestCaseSource(nameof(FeatureCoverageScenarios))]
        public void GaussianBlur_DefaultParameters_MatchesAccordReferenceAcrossFeatures(InputScenario scenario) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            using UnmanagedImage source = CreateGray8Image(scenario.Width, scenario.Height, sourcePixels);
            using UnmanagedImage expected = CreateReferenceBlur(source, sigma: 1.4, size: 5);
            using UnmanagedImage actual = CreateOptimizedBlur(source, sigma: 1.4, size: 5);

            Assert.That(source.Offset > 0, Is.EqualTo(ShouldHaveStridePadding(scenario.Width)), "Unexpected source stride padding.");
            AssertBitExactImage(expected, actual);
        }

        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveGaussianIgnoreReason)]
#endif
        [Category(ExhaustiveGaussianCategory)]
        [TestCaseSource(nameof(FixtureSource))]
        public void GaussianBlur_CustomKernel_MatchesAccordReference(string fixtureName, Func<int, int, byte[]> createPixels) {
            int width = AstroCameraResolutions[RepresentativeKernelResolutionKey].Width;
            int height = AstroCameraResolutions[RepresentativeKernelResolutionKey].Height;
            byte[] sourcePixels = createPixels(width, height);

            using UnmanagedImage source = CreateGray8Image(width, height, sourcePixels);
            using UnmanagedImage expected = CreateReferenceBlur(source, sigma: 2.2, size: 10);
            using UnmanagedImage actual = CreateOptimizedBlur(source, sigma: 2.2, size: 10);

            AssertBitExactImage(expected, actual);
        }

        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveGaussianIgnoreReason)]
#endif
        [Category(ExhaustiveGaussianCategory)]
        [TestCaseSource(nameof(FixtureSource))]
        public void GaussianBlur_DoesNotModifySourceImage(string fixtureName, Func<int, int, byte[]> createPixels) {
            int width = AstroCameraResolutions[RepresentativePaddingResolutionKey].Width;
            int height = AstroCameraResolutions[RepresentativePaddingResolutionKey].Height;
            byte[] sourcePixels = createPixels(width, height);

            using UnmanagedImage source = CreateGray8Image(width, height, sourcePixels);
            byte[] before = ReadImageBytes(source);

            using UnmanagedImage _ = CreateOptimizedBlur(source, sigma: 1.4, size: 5);

            byte[] after = ReadImageBytes(source);
            Assert.That(after, Is.EqualTo(before), "The optimized Gaussian blur should not modify the source image.");
        }

        [Test]
#if !RUN_EXHAUSTIVE_IMAGE_ANALYSIS_TESTS
        [Ignore(ExhaustiveGaussianIgnoreReason)]
#endif
        [Category(ExhaustiveGaussianCategory)]
        [TestCaseSource(nameof(FixtureSource))]
        public void GaussianBlur_RepeatedRuns_AreDeterministic(string fixtureName, Func<int, int, byte[]> createPixels) {
            int width = AstroCameraResolutions[RepresentativePaddingResolutionKey].Width;
            int height = AstroCameraResolutions[RepresentativePaddingResolutionKey].Height;
            byte[] sourcePixels = createPixels(width, height);
            byte[]? expectedBytes = null;

            for (int iteration = 0; iteration < 5; iteration++) {
                using UnmanagedImage source = CreateGray8Image(width, height, sourcePixels);
                using UnmanagedImage actual = CreateOptimizedBlur(source, sigma: 1.4, size: 5);

                byte[] actualBytes = ReadImageBytes(actual);
                expectedBytes ??= actualBytes;

                Assert.That(actualBytes, Is.EqualTo(expectedBytes), $"Iteration {iteration} produced a different blurred image.");
            }
        }

        private static TestCaseData Scenario(InputScenario scenario) {
            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        private static void AssertMatchesAccordReference(InputScenario scenario, double sigma, int size) {
            byte[] sourcePixels = scenario.CreatePixels(scenario.Width, scenario.Height);

            using UnmanagedImage source = CreateGray8Image(scenario.Width, scenario.Height, sourcePixels);
            using UnmanagedImage expected = CreateReferenceBlur(source, sigma, size);
            using UnmanagedImage actual = CreateOptimizedBlur(source, sigma, size);

            Assert.That(source.Offset > 0, Is.EqualTo(ShouldHaveStridePadding(scenario.Width)), "Unexpected source stride padding.");
            AssertBitExactImage(expected, actual);
        }

        private static UnmanagedImage CreateGray8Image(int width, int height, byte[] pixels) {
            if (pixels.Length != width * height) {
                throw new ArgumentException("Source pixel array length does not match image dimensions.", nameof(pixels));
            }

            UnmanagedImage image = UnmanagedImage.Create(width, height, PixelFormat.Format8bppIndexed);

            try {
                int stride = image.Stride;
                byte[] buffer = new byte[stride * height];

                // Fill row padding with a sentinel so accidental reads beyond the logical width show up
                // as output mismatches instead of silently comparing equal.
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

        private static UnmanagedImage CreateReferenceBlur(UnmanagedImage source, double sigma, int size) {
            var gaussianFilter = new AccordGaussianBlur {
                Sigma = sigma,
                Size = size
            };

            return gaussianFilter.Apply(source);
        }

        private static UnmanagedImage CreateOptimizedBlur(UnmanagedImage source, double sigma, int size) {
            var gaussianFilter = new OptimizedGaussianBlur {
                Sigma = sigma,
                Size = size
            };

            return gaussianFilter.Apply(source);
        }

        private static void AssertBitExactImage(UnmanagedImage expected, UnmanagedImage actual) {
            Assert.That(actual.PixelFormat, Is.EqualTo(expected.PixelFormat), "Pixel format mismatch.");
            Assert.That(actual.Width, Is.EqualTo(expected.Width), "Width mismatch.");
            Assert.That(actual.Height, Is.EqualTo(expected.Height), "Height mismatch.");
            Assert.That(actual.Stride, Is.EqualTo(expected.Stride), "Stride mismatch.");

            byte[] expectedBytes = ReadImageBytes(expected);
            byte[] actualBytes = ReadImageBytes(actual);

            Assert.That(actualBytes, Is.EqualTo(expectedBytes), "Blurred output bytes should match Accord exactly.");
        }

        private static byte[] ReadImageBytes(UnmanagedImage image) {
            byte[] buffer = new byte[image.NumberOfBytes];
            Marshal.Copy(image.ImageData, buffer, 0, buffer.Length);
            return buffer;
        }

        private static bool ShouldHaveStridePadding(int width) {
            return width % 4 != 0;
        }
    }
}
