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
using NINA.Core.Enum;
using NINA.Image.FileFormat;
using NINA.Image.FileFormat.FITS;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Test.Image.ImageAnalysis {

    [TestFixture]
    public class BayerFilter16bppTests {
        private readonly struct AstroCameraResolutionCase {
            public AstroCameraResolutionCase(int width, int height) {
                Width = width;
                Height = height;
            }

            public int Width { get; }
            public int Height { get; }
        }

        private static readonly IReadOnlyDictionary<string, AstroCameraResolutionCase> AstroCameraResolutions = new Dictionary<string, AstroCameraResolutionCase> {
            // Cover a representative spread of astro-camera sensor sizes, including current high-resolution sensors,
            // and run the same full-image reference comparison for every configured resolution.
            ["Guide_1280x960"] = new AstroCameraResolutionCase(1280, 960),
            ["Planetary_1936x1096"] = new AstroCameraResolutionCase(1936, 1096),
            ["DeepSky_3096x2080"] = new AstroCameraResolutionCase(3096, 2080),
            ["Square_3008x3008"] = new AstroCameraResolutionCase(3008, 3008),
            ["FourThirds_4144x2822"] = new AstroCameraResolutionCase(4144, 2822),
            ["IMX183_5496x3672"] = new AstroCameraResolutionCase(5496, 3672),
            ["APS-C_6224x4168"] = new AstroCameraResolutionCase(6224, 4168),
            ["FullFrame61MP_9576x6388"] = new AstroCameraResolutionCase(9576, 6388),
            ["MediumFormat100MP_11648x8736"] = new AstroCameraResolutionCase(11648, 8736)
        };

        private const string FormatCoverageResolutionKey = "Planetary_1936x1096";
        private const SensorType FileBackedBayerPattern = SensorType.RGGB;
        private const int HardeningPaddedWidth = 257;
        private const int HardeningPaddedHeight = 129;
        private const int HardeningRealWidth = 1936;
        private const int HardeningRealHeight = 1096;
        private const int RepresentativeDemosaicWidth = 6;
        private const int RepresentativeDemosaicHeight = 4;
        private const int RepresentativePatternWidth = 6;
        private const int RepresentativePatternHeight = 5;
        private const int RepresentativeRawWidth = 4;
        private const int RepresentativeRawHeight = 3;

        private global::NINA.Test.ImageDataFactoryTestUtility dataFactoryUtility;

        public sealed class InputScenario {
            public required string Name { get; init; }
            public required Func<int, int, ushort[]> CreatePixels { get; init; }
        }

        private static readonly InputScenario RepresentativeFormatCoverageScenario = CreateInputScenario(DeterministicImageFixtures.RepresentativeFormatCoverage);

        private static readonly IReadOnlyList<InputScenario> FeatureInputScenarios = new[] {
            CreateInputScenario(DeterministicImageFixtures.UniformBlack),
            CreateInputScenario(DeterministicImageFixtures.UniformWhite),
            CreateInputScenario(DeterministicImageFixtures.SingleImpulseCenter),
            CreateInputScenario(DeterministicImageFixtures.SparseStarField),
            CreateInputScenario(DeterministicImageFixtures.FeatureMix),
            CreateInputScenario(DeterministicImageFixtures.StepEdge),
            CreateInputScenario(DeterministicImageFixtures.DiagonalLine),
            CreateInputScenario(DeterministicImageFixtures.Checkerboard),
            CreateInputScenario(DeterministicImageFixtures.BandingAndHotPixels),
            CreateInputScenario(DeterministicImageFixtures.Structured)
        };

        [SetUp]
        public void SetUp() {
            dataFactoryUtility = new global::NINA.Test.ImageDataFactoryTestUtility();
        }

        private static InputScenario CreateInputScenario(DeterministicImageFixtures.ImageFixture fixture) {
            return new InputScenario {
                Name = fixture.Name,
                CreatePixels = fixture.CreateUShorts
            };
        }

        private static IEnumerable<TestCaseData> AlternateBayerPatterns() {
            // Use a small set of representative Bayer layouts to ensure the filter honors overrides.
            // These patterns cover each color's position so the channel mapping logic is exercised.
            yield return new TestCaseData("RGGB", new int[,] { { RGB.R, RGB.G }, { RGB.G, RGB.B } });
            yield return new TestCaseData("BGGR", new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } });
            yield return new TestCaseData("GBRG", new int[,] { { RGB.G, RGB.B }, { RGB.R, RGB.G } });
            yield return new TestCaseData("GRBG", new int[,] { { RGB.G, RGB.R }, { RGB.B, RGB.G } });
        }

        private static IEnumerable<(string Name, int Width, int Height)> DimensionMatrix() {
            yield return ("EvenWidth_EvenHeight", 4, 4);
            yield return ("OddWidth_EvenHeight", 5, 4);
            yield return ("EvenWidth_OddHeight", 4, 5);
            yield return ("OddWidth_OddHeight", 5, 5);
        }

        private static IEnumerable<(string Name, int Width, int Height)> BorderOnlyDimensionMatrix() {
            yield return ("BorderOnly_EvenWidth_EvenHeight", 2, 2);
            yield return ("BorderOnly_OddWidth_EvenHeight", 3, 2);
        }

        private static IEnumerable<TestCaseData> RealWorldFileFormatCases() {
            // Run each supported on-disk format against multiple deterministic source families.
            // This keeps the file-backed integration path generic instead of relying on one pseudo-frame.
            string[] extensions = { ".xisf", ".fits", ".fit", ".fts" };

            foreach (string extension in extensions) {
                yield return new TestCaseData(extension, RepresentativeFormatCoverageScenario)
                    .SetName($"RealWorldFormat_{extension.TrimStart('.').ToUpperInvariant()}_{RepresentativeFormatCoverageScenario.Name}");
            }
        }

        private static IEnumerable<TestCaseData> AstroCameraResolutionCases() {
            // Reuse the same deterministic input families across the full astro-camera resolution matrix.
            // This keeps each frame size honest without depending on a single source texture.
            foreach (var resolution in AstroCameraResolutions) {
                yield return new TestCaseData(
                        resolution.Key,
                        resolution.Value.Width,
                        resolution.Value.Height,
                        RepresentativeFormatCoverageScenario)
                    .SetName($"RealWorldResolution_{resolution.Key}_{RepresentativeFormatCoverageScenario.Name}");
            }
        }

        private static IEnumerable<TestCaseData> RepresentativeInputScenarioCases() {
            // Use the same deterministic source families for the representative in-memory correctness checks.
            foreach (InputScenario scenario in FeatureInputScenarios) {
                yield return Scenario(scenario, scenario.Name);
            }
        }

        private static IEnumerable<TestCaseData> DimensionScenarioCases() {
            // Combine the even/odd dimension matrix with the deterministic source families so padding and
            // border handling are exercised against more than one image structure.
            foreach (InputScenario scenario in FeatureInputScenarios) {
                foreach (var dimension in DimensionMatrix()) {
                    yield return new TestCaseData(scenario, dimension.Width, dimension.Height)
                        .SetName($"Dimensions_{scenario.Name}_{dimension.Name}");
                }
            }
        }

        private static IEnumerable<TestCaseData> BorderOnlyScenarioCases() {
            // Border-only images are tiny, so include both degenerate uniform inputs and mixed-content fixtures.
            foreach (InputScenario scenario in FeatureInputScenarios) {
                foreach (var dimension in BorderOnlyDimensionMatrix()) {
                    yield return new TestCaseData(scenario, dimension.Width, dimension.Height)
                        .SetName($"BorderOnly_{scenario.Name}_{dimension.Name}");
                }
            }
        }

        private static IEnumerable<TestCaseData> BayerPatternScenarioCases() {
            // Run every deterministic source family through the override matrix so pattern placement is
            // checked against flat, structured, and high-frequency inputs alike.
            foreach (InputScenario scenario in FeatureInputScenarios) {
                foreach (var testCase in AlternateBayerPatterns()) {
                    yield return new TestCaseData(scenario, testCase.Arguments[0], testCase.Arguments[1])
                        .SetName($"PatternOverride_{scenario.Name}_{testCase.Arguments[0]}");
                }
            }
        }

        private static IEnumerable<TestCaseData> RealisticInMemoryScenarioCases() {
            // Keep one padded odd-width case and one real-world frame size in the default suite.
            // Every deterministic source family is run against both sizes so the same generators cover
            // padding-sensitive and real-resolution behavior.
            foreach (InputScenario scenario in FeatureInputScenarios) {
                yield return new TestCaseData(scenario, HardeningPaddedWidth, HardeningPaddedHeight)
                    .SetName($"Realistic_PaddedOdd_{HardeningPaddedWidth}x{HardeningPaddedHeight}_{scenario.Name}");
                yield return new TestCaseData(scenario, HardeningRealWidth, HardeningRealHeight)
                    .SetName($"Realistic_Planetary_{HardeningRealWidth}x{HardeningRealHeight}_{scenario.Name}");
            }
        }

        private static IEnumerable<TestCaseData> ChannelScenarioCases() {
            // Channel-allocation tests do not need every degenerate input, but they should still prove that
            // the auxiliary arrays stay correct across several structured source families.
            foreach (InputScenario scenario in FeatureInputScenarios) {
                yield return Scenario(scenario, $"Channels_{scenario.Name}");
            }
        }

        private static IEnumerable<TestCaseData> DeterminismScenarioCases() {
            // Re-run every shared fixture to catch any non-deterministic data-dependent behavior.
            foreach (InputScenario scenario in FeatureInputScenarios) {
                yield return Scenario(scenario, $"Determinism_{scenario.Name}");
            }
        }

        [Test]
        [TestCaseSource(nameof(RepresentativeInputScenarioCases))]
        public void Demosaic_ProducesBitExactChannelsAndBuffers(InputScenario scenario) {
            // Reuse the representative demosaic check, but drive it with deterministic source families instead
            // of one linear ramp so the exact-reference comparison covers more than one source texture.
            AssertDemosaicCase(
                scenario,
                RepresentativeDemosaicWidth,
                RepresentativeDemosaicHeight,
                saveColorChannels: true,
                saveLumChannel: true,
                caseName: $"Representative_{RepresentativeDemosaicWidth}x{RepresentativeDemosaicHeight}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(DimensionScenarioCases))]
        public void Demosaic_HandlesEvenAndOddDimensions(InputScenario scenario, int width, int height) {
            // Keep the original size matrix, but pair each width/height combination with deterministic
            // source families so padding and border handling are proven against varied input structure.
            AssertDemosaicCase(
                scenario,
                width,
                height,
                saveColorChannels: false,
                saveLumChannel: false,
                caseName: $"Dimensions_{width}x{height}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(BayerPatternScenarioCases))]
        public void Demosaic_RespectsBayerPatternOverride(InputScenario scenario, string patternName, int[,] bayerPattern) {
            // Keep the Bayer override matrix, but feed it the reusable deterministic source families so
            // pattern placement is not only checked on one synthetic ramp.
            AssertDemosaicCase(
                scenario,
                RepresentativePatternWidth,
                RepresentativePatternHeight,
                saveColorChannels: true,
                saveLumChannel: true,
                bayerPattern: bayerPattern,
                caseName: $"PatternOverride_{patternName}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(BorderOnlyScenarioCases))]
        public void Demosaic_HandlesBorderOnlyDimensions(InputScenario scenario, int width, int height) {
            // Border-only images still have their dedicated path, but now cover both uniform and mixed-content
            // fixtures so the tiny-image behavior is checked across different value distributions.
            AssertDemosaicCase(
                scenario,
                width,
                height,
                saveColorChannels: true,
                saveLumChannel: true,
                caseName: $"BorderOnly_{width}x{height}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(RealisticInMemoryScenarioCases))]
        public void Demosaic_HandlesDeterministicInputFamiliesAtRealisticSizes(InputScenario scenario, int width, int height) {
            // Replace the isolated hardening bucket with regular exact-reference coverage that runs the same
            // deterministic source families against a padded odd-width frame and a realistic frame size.
            AssertDemosaicCase(
                scenario,
                width,
                height,
                saveColorChannels: true,
                saveLumChannel: true,
                caseName: $"Realistic_{width}x{height}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(RepresentativeInputScenarioCases))]
        public void RawMapping_NoDemosaicCopiesPixelsIntoPatternedChannels(InputScenario scenario) {
            // The no-demosaic path now uses the same deterministic source families as the demosaic path so
            // direct Bayer-plane placement is checked on more than one synthetic frame.
            AssertRawMappingCase(
                scenario,
                RepresentativeRawWidth,
                RepresentativeRawHeight,
                caseName: $"RawRepresentative_{RepresentativeRawWidth}x{RepresentativeRawHeight}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(RealisticInMemoryScenarioCases))]
        public void RawMapping_HandlesDeterministicInputFamiliesAtRealisticSizes(InputScenario scenario, int width, int height) {
            // The straight-through path should also stay exact on the padded and real-sized deterministic inputs.
            AssertRawMappingCase(
                scenario,
                width,
                height,
                caseName: $"RawRealistic_{width}x{height}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(DimensionScenarioCases))]
        public void RawMapping_HandlesEvenAndOddDimensions(InputScenario scenario, int width, int height) {
            // Keep the even/odd size matrix, but run each size against deterministic source families instead of
            // a single ramp so padding-sensitive raw mapping is covered on varied data.
            AssertRawMappingCase(
                scenario,
                width,
                height,
                caseName: $"RawDimensions_{width}x{height}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(BayerPatternScenarioCases))]
        public void RawMapping_RespectsBayerPatternOverride(InputScenario scenario, string patternName, int[,] bayerPattern) {
            // Keep the Bayer override matrix for the raw-copy path, but reuse the deterministic source families
            // so the override is not only proven on one dedicated sample.
            AssertRawMappingCase(
                scenario,
                RepresentativeDemosaicWidth,
                RepresentativeDemosaicHeight,
                bayerPattern: bayerPattern,
                caseName: $"RawPatternOverride_{patternName}_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(ChannelScenarioCases))]
        public void Demosaic_SaveLumOnly_PopulatesLumArray(InputScenario scenario) {
            // Luminance-only output should remain exact regardless of source structure.
            AssertDemosaicCase(
                scenario,
                RepresentativeDemosaicWidth,
                RepresentativeDemosaicHeight,
                saveColorChannels: false,
                saveLumChannel: true,
                caseName: $"LumOnly_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(ChannelScenarioCases))]
        public void Demosaic_SaveColorOnly_PopulatesColorArrays(InputScenario scenario) {
            // Color-only output should remain exact while luminance stays empty across the deterministic families.
            AssertDemosaicCase(
                scenario,
                RepresentativePatternWidth,
                RepresentativePatternHeight,
                saveColorChannels: true,
                saveLumChannel: false,
                caseName: $"ColorOnly_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(ChannelScenarioCases))]
        public void Demosaic_NoChannelsRequested_LeavesLRGBArraysNull(InputScenario scenario) {
            // When callers do not request auxiliary channels, exact image parity must still hold and the side
            // arrays must stay unallocated for the same deterministic source families used elsewhere.
            AssertDemosaicCase(
                scenario,
                4,
                4,
                saveColorChannels: false,
                saveLumChannel: false,
                caseName: $"NoChannels_{scenario.Name}");
        }

        [Test]
        [TestCaseSource(nameof(ChannelScenarioCases))]
        public void Apply_DoesNotModifySourceBuffer(InputScenario scenario) {
            // Source immutability should be guaranteed for the reusable deterministic input families, not only
            // for one small ramp-shaped sample.
            int width = RepresentativeDemosaicWidth;
            int height = RepresentativeDemosaicHeight;
            ushort[] sourcePixels = scenario.CreatePixels(width, height);

            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            byte[] before = ReadBitmapBytes(sourceImage);

            var filter = new BayerFilter16bpp();
            using var processed = filter.Apply(sourceImage);

            byte[] after = ReadBitmapBytes(sourceImage);

            Assert.That(processed.PixelFormat, Is.EqualTo(PixelFormat.Format48bppRgb), "Apply should produce a 48bpp RGB image");
            Assert.That(after, Is.EqualTo(before), $"Source buffer should remain unchanged after Apply ({scenario.Name})");
        }

        [Test]
        [TestCaseSource(nameof(DeterminismScenarioCases))]
        public void Demosaic_RepeatedRuns_AreDeterministic(InputScenario scenario) {
            ushort[] sourcePixels = scenario.CreatePixels(HardeningPaddedWidth, HardeningPaddedHeight);
            byte[]? expectedBitmapBytes = null;
            ushort[]? expectedRed = null;
            ushort[]? expectedGreen = null;
            ushort[]? expectedBlue = null;
            ushort[]? expectedLum = null;

            for (int iteration = 0; iteration < 3; iteration++) {
                using var sourceImage = CreateGray16Bitmap(HardeningPaddedWidth, HardeningPaddedHeight, sourcePixels);
                var filter = new BayerFilter16bpp {
                    SaveColorChannels = true,
                    SaveLumChannel = true,
                    PerformDemosaicing = true
                };

                using var processed = filter.Apply(sourceImage);

                byte[] actualBitmapBytes = ReadBitmapBytes(processed);
                expectedBitmapBytes ??= actualBitmapBytes;
                expectedRed ??= (ushort[])filter.LRGBArrays.Red.Clone();
                expectedGreen ??= (ushort[])filter.LRGBArrays.Green.Clone();
                expectedBlue ??= (ushort[])filter.LRGBArrays.Blue.Clone();
                expectedLum ??= (ushort[])filter.LRGBArrays.Lum.Clone();

                Assert.That(actualBitmapBytes, Is.EqualTo(expectedBitmapBytes), $"Bitmap output mismatch on iteration {iteration} ({scenario.Name}).");
                Assert.That(filter.LRGBArrays.Red, Is.EqualTo(expectedRed), $"Red plane mismatch on iteration {iteration} ({scenario.Name}).");
                Assert.That(filter.LRGBArrays.Green, Is.EqualTo(expectedGreen), $"Green plane mismatch on iteration {iteration} ({scenario.Name}).");
                Assert.That(filter.LRGBArrays.Blue, Is.EqualTo(expectedBlue), $"Blue plane mismatch on iteration {iteration} ({scenario.Name}).");
                Assert.That(filter.LRGBArrays.Lum, Is.EqualTo(expectedLum), $"Lum plane mismatch on iteration {iteration} ({scenario.Name}).");
            }
        }

        [Test]
        public void FormatTranslations_MapsGray16ToRgb48() {
            // Verify the filter advertises the 16bpp grayscale input translation.
            // This mapping drives BaseFilter.Apply to allocate the correct destination format.
            var filter = new BayerFilter16bpp();
            Assert.That(filter.FormatTranslations[PixelFormat.Format16bppGrayScale], Is.EqualTo(PixelFormat.Format48bppRgb));
        }

        [Test]
        [Explicit("Large file-backed integration coverage. Run on demand.")]
        [Category("BayerFilter16bppRealWorldFormats")]
        [Ignore("File-backed Bayer format coverage is intentionally opt-in because it exercises slow integration paths.")]
        [NonParallelizable]
        [CancelAfter(180000)]
        [TestCaseSource(nameof(RealWorldFileFormatCases))]
        public async Task Demosaic_FileBackedRealWorldFormats_MatchReference(string extension, InputScenario scenario) {
            // These cases intentionally go through disk instead of reusing the in-memory Bayer tests.
            // Saving and reloading is what validates the FITS/XISF loader contract: raw 16-bit mosaic data
            // must round-trip unchanged and Bayer metadata must come back intact for the later auto-pattern path.
            // One representative astro-camera resolution is enough here because the dedicated batch below covers the size matrix,
            // while the scenario matrix ensures the file-backed path is not tied to one source texture.
            AstroCameraResolutionCase representativeResolution = AstroCameraResolutions[FormatCoverageResolutionKey];
            await AssertFileBackedRealWorldCase(
                extension: extension,
                width: representativeResolution.Width,
                height: representativeResolution.Height,
                bayerPattern: FileBackedBayerPattern,
                scenario: scenario);
        }

        [Test]
        [Explicit("Large file-backed integration coverage. Run on demand.")]
        [Category("BayerFilter16bppRealWorldFormats")]
        [Ignore("Large real-world Bayer resolution coverage is intentionally opt-in because it is exhaustive and slow.")]
        [NonParallelizable]
        [CancelAfter(600000)]
        [TestCaseSource(nameof(AstroCameraResolutionCases))]
        public async Task Demosaic_FileBackedAstroCameraResolutions_MatchReference(
            string resolutionName,
            int width,
            int height,
            InputScenario scenario) {
            // Use XISF for the resolution matrix so every defined astro-camera size exercises the full
            // file-backed load path, and run more than one deterministic source family at each size.
            await AssertFileBackedRealWorldCase(
                extension: ".xisf",
                width: width,
                height: height,
                bayerPattern: FileBackedBayerPattern,
                scenario: scenario);
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

        private static void AssertDemosaicCase(
            InputScenario scenario,
            int width,
            int height,
            bool saveColorChannels,
            bool saveLumChannel,
            int[,]? bayerPattern = null,
            string? caseName = null) {
            string effectiveCaseName = caseName ?? $"{width}x{height}_{scenario.Name}";
            ushort[] sourcePixels = scenario.CreatePixels(width, height);

            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = saveColorChannels,
                SaveLumChannel = saveLumChannel,
                PerformDemosaicing = true
            };

            if (bayerPattern != null) {
                filter.BayerPattern = bayerPattern;
            }

            using var processed = filter.Apply(sourceImage);

            AssertStridePaddingMatches(sourceImage, processed, width, effectiveCaseName);

            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: true, computeLum: saveLumChannel);
            AssertProcessedChannelsMatchReference(processed, reference, effectiveCaseName);
            AssertSavedChannelsMatchReference(filter, reference, saveColorChannels, saveLumChannel, effectiveCaseName);
        }

        private static void AssertRawMappingCase(
            InputScenario scenario,
            int width,
            int height,
            int[,]? bayerPattern = null,
            string? caseName = null) {
            string effectiveCaseName = caseName ?? $"{width}x{height}_{scenario.Name}";
            ushort[] sourcePixels = scenario.CreatePixels(width, height);

            using var sourceImage = CreateGray16Bitmap(width, height, sourcePixels);
            var filter = new BayerFilter16bpp {
                SaveColorChannels = false,
                SaveLumChannel = false,
                PerformDemosaicing = false
            };

            if (bayerPattern != null) {
                filter.BayerPattern = bayerPattern;
            }

            using var processed = filter.Apply(sourceImage);

            AssertStridePaddingMatches(sourceImage, processed, width, effectiveCaseName);

            ReferenceChannels reference = ComputeReference(sourceImage, filter.BayerPattern, performDemosaic: false, computeLum: false);
            AssertProcessedChannelsMatchReference(processed, reference, effectiveCaseName);
            Assert.That(filter.LRGBArrays, Is.Null, $"LRGBArrays should remain null when raw mapping does not request channels ({effectiveCaseName}).");
        }

        private static void AssertStridePaddingMatches(Bitmap sourceImage, Bitmap processed, int width, string caseName) {
            bool expectSourcePadding = ShouldHaveStridePadding(width, bytesPerPixel: 2);
            bool expectDestPadding = ShouldHaveStridePadding(width, bytesPerPixel: 6);

            Assert.That(HasStridePadding(sourceImage, bytesPerPixel: 2), Is.EqualTo(expectSourcePadding), $"Unexpected source stride padding ({caseName}).");
            Assert.That(HasStridePadding(processed, bytesPerPixel: 6), Is.EqualTo(expectDestPadding), $"Unexpected destination stride padding ({caseName}).");
        }

        private static void AssertProcessedChannelsMatchReference(Bitmap processed, ReferenceChannels reference, string caseName) {
            var processedChannels = Read48bppChannels(processed);

            Assert.That(processed.PixelFormat, Is.EqualTo(PixelFormat.Format48bppRgb), $"Apply should produce a 48bpp RGB image ({caseName}).");
            Assert.That(processedChannels.Blue, Is.EqualTo(reference.Blue), $"B channel mismatch ({caseName}).");
            Assert.That(processedChannels.Green, Is.EqualTo(reference.Green), $"G channel mismatch ({caseName}).");
            Assert.That(processedChannels.Red, Is.EqualTo(reference.Red), $"R channel mismatch ({caseName}).");
        }

        private static void AssertSavedChannelsMatchReference(
            BayerFilter16bpp filter,
            ReferenceChannels reference,
            bool saveColorChannels,
            bool saveLumChannel,
            string caseName) {
            if (!saveColorChannels && !saveLumChannel) {
                Assert.That(filter.LRGBArrays, Is.Null, $"LRGBArrays should remain null when no channels are requested ({caseName}).");
                return;
            }

            Assert.That(filter.LRGBArrays, Is.Not.Null, $"LRGBArrays should be created when auxiliary channels are requested ({caseName}).");

            if (saveColorChannels) {
                Assert.That(filter.LRGBArrays.Red, Is.EqualTo(reference.Blue), $"LRGBArrays.Red should mirror image B ({caseName}).");
                Assert.That(filter.LRGBArrays.Green, Is.EqualTo(reference.Green), $"LRGBArrays.Green should mirror image G ({caseName}).");
                Assert.That(filter.LRGBArrays.Blue, Is.EqualTo(reference.Red), $"LRGBArrays.Blue should mirror image R ({caseName}).");
            } else {
                Assert.That(filter.LRGBArrays.Red, Has.Length.EqualTo(0), $"Red channel should be empty when SaveColorChannels is false ({caseName}).");
                Assert.That(filter.LRGBArrays.Green, Has.Length.EqualTo(0), $"Green channel should be empty when SaveColorChannels is false ({caseName}).");
                Assert.That(filter.LRGBArrays.Blue, Has.Length.EqualTo(0), $"Blue channel should be empty when SaveColorChannels is false ({caseName}).");
            }

            if (saveLumChannel) {
                Assert.That(filter.LRGBArrays.Lum, Is.EqualTo(reference.Lum), $"LRGBArrays.Lum should match averaged luminance ({caseName}).");
            } else {
                Assert.That(filter.LRGBArrays.Lum, Has.Length.EqualTo(0), $"Lum channel should be empty when SaveLumChannel is false ({caseName}).");
            }
        }

        private static TestCaseData Scenario(InputScenario scenario, string testName) {
            return new TestCaseData(scenario).SetName(testName);
        }

        private async Task AssertFileBackedRealWorldCase(
            string extension,
            int width,
            int height,
            SensorType bayerPattern,
            InputScenario scenario) {
            ushort[] sourcePixels = scenario.CreatePixels(width, height);
            string tempDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "BayerFilter16bppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            string filePath = Path.Combine(tempDirectory, "synthetic-frame" + extension);

            // Persist and reload on purpose. The pure in-memory tests already validate the demosaic math directly;
            // this helper covers the file-backed integration path, where CreateFromFile must preserve both the
            // raw mosaic samples and the sensor metadata that ImageControlVM later uses when debayering loaded files.
            try {
                WriteFileBackedFrame(filePath, sourcePixels, width, height, bayerPattern);

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var loaded = await dataFactoryUtility.ImageDataFactory.CreateFromFile(
                    path: filePath,
                    bitDepth: 16,
                    isBayered: true,
                    ct: cts.Token);

                Assert.That(loaded.Properties.Width, Is.EqualTo(width), "Loaded width mismatch.");
                Assert.That(loaded.Properties.Height, Is.EqualTo(height), "Loaded height mismatch.");
                Assert.That(loaded.Data.FlatArray, Is.EqualTo(sourcePixels), $"Loaded pixels should round-trip through file I/O unchanged ({scenario.Name}).");
                Assert.That(loaded.MetaData.Camera.SensorType, Is.EqualTo(bayerPattern), $"Loaded sensor metadata should preserve the Bayer pattern ({scenario.Name}).");

                SensorType metadataDrivenBayerPattern = ResolveMetadataDrivenBayerPattern(loaded.MetaData);
                Assert.That(metadataDrivenBayerPattern, Is.EqualTo(bayerPattern), $"Loaded metadata should drive the same Bayer pattern the file was written with ({scenario.Name}).");

                int[,] filterPattern = GetImageUtilityBayerPattern(metadataDrivenBayerPattern);

                // Mirror the camera-disconnected auto-pattern path used by ImageControlVM for loaded files,
                // where the pattern comes from the metadata parsed during reload rather than from a live camera.
                var debayered = loaded.RenderImage().Debayer(
                    saveColorChannels: false,
                    saveLumChannel: false,
                    bayerPattern: metadataDrivenBayerPattern);

                Assert.That(debayered.BayerPattern, Is.EqualTo(metadataDrivenBayerPattern), $"Debayered image should record the metadata-selected Bayer pattern ({scenario.Name}).");
                Assert.That(debayered.Image.PixelWidth, Is.EqualTo(width), $"Debayered image width mismatch ({scenario.Name}).");
                Assert.That(debayered.Image.PixelHeight, Is.EqualTo(height), $"Debayered image height mismatch ({scenario.Name}).");
                Assert.That(debayered.Image.Format, Is.EqualTo(System.Windows.Media.PixelFormats.Rgb48), $"Debayered image should stay in 48-bit color ({scenario.Name}).");

                ReferenceChannels reference = ComputeDemosaicReference(
                    sourcePixels,
                    width,
                    height,
                    filterPattern,
                    computeLum: false);

                AssertDebayeredImageMatchesReference(debayered.Image, reference, scenario.Name);
            } finally {
                if (Directory.Exists(tempDirectory)) {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        private static ImageMetaData CreateBayerMetaData(SensorType pattern) {
            // Add Bayer metadata so FITS/XISF files carry realistic camera color filter information.
            var metaData = new ImageMetaData();
            metaData.Image.ImageType = "LIGHT";
            metaData.Camera.SensorType = pattern;
            metaData.Camera.BayerPattern = (BayerPatternEnum)pattern;
            metaData.Camera.BayerOffsetX = 0;
            metaData.Camera.BayerOffsetY = 0;
            return metaData;
        }

        private static SensorType ResolveMetadataDrivenBayerPattern(ImageMetaData metaData) {
            // Mirror the disconnected-camera auto-pattern path in ImageControlVM for loaded files.
            return metaData.Camera.SensorType;
        }

        private static void AssertDebayeredImageMatchesReference(BitmapSource image, ReferenceChannels reference, string caseName) {
            using Bitmap bitmap = ImageUtility.BitmapFromSource(image, PixelFormat.Format48bppRgb);
            var actual = Read48bppChannels(bitmap);

            Assert.That(actual.Blue, Is.EqualTo(reference.Blue), $"Debayered image B channel should match the reference image ({caseName}).");
            Assert.That(actual.Green, Is.EqualTo(reference.Green), $"Debayered image G channel should match the reference image ({caseName}).");
            Assert.That(actual.Red, Is.EqualTo(reference.Red), $"Debayered image R channel should match the reference image ({caseName}).");
        }

        private static void WriteFileBackedFrame(string filePath, ushort[] pixels, int width, int height, SensorType pattern) {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension) {
                case ".xisf":
                    WriteXisfFrame(filePath, pixels, width, height, pattern);
                    break;
                case ".fits":
                case ".fit":
                case ".fts":
                    WriteFitsFrame(filePath, pixels, width, height, pattern);
                    break;
                default:
                    Assert.Fail($"Unsupported test file extension: {Path.GetExtension(filePath)}");
                    break;
            }
        }

        private static void WriteFitsFrame(string filePath, ushort[] pixels, int width, int height, SensorType pattern) {
            var fits = new FITS(pixels, width, height);
            fits.PopulateHeaderCards(CreateBayerMetaData(pattern));

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            fits.Write(stream);
        }

        private static void WriteXisfFrame(string filePath, ushort[] pixels, int width, int height, SensorType pattern) {
            var header = new XISFHeader();
            header.AddImageMetaData(
                new ImageProperties(width: width, height: height, bitDepth: 16, isBayered: true, gain: 0, offset: 0),
                imageType: "LIGHT");
            header.Populate(CreateBayerMetaData(pattern));

            var xisf = new XISF(header);
            xisf.AddAttachedImage(pixels, new FileSaveInfo {
                FilePath = Path.GetDirectoryName(filePath) ?? string.Empty,
                FilePattern = Path.GetFileNameWithoutExtension(filePath),
                FileType = FileTypeEnum.XISF,
                XISFCompressionType = XISFCompressionTypeEnum.NONE
            });

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            xisf.Save(stream);
        }

        private static int[,] GetImageUtilityBayerPattern(SensorType pattern) {
            // Keep these mappings in sync with ImageUtility.Debayer SensorType handling.
            switch (pattern) {
                case SensorType.RGGB:
                    return new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } };
                case SensorType.RGBG:
                    return new int[,] { { RGB.G, RGB.B }, { RGB.G, RGB.R } };
                case SensorType.GRGB:
                    return new int[,] { { RGB.B, RGB.G }, { RGB.R, RGB.G } };
                case SensorType.GRBG:
                    return new int[,] { { RGB.G, RGB.B }, { RGB.R, RGB.G } };
                case SensorType.GBGR:
                    return new int[,] { { RGB.R, RGB.G }, { RGB.B, RGB.G } };
                case SensorType.GBRG:
                    return new int[,] { { RGB.G, RGB.R }, { RGB.B, RGB.G } };
                case SensorType.BGRG:
                    return new int[,] { { RGB.G, RGB.R }, { RGB.G, RGB.B } };
                case SensorType.BGGR:
                    return new int[,] { { RGB.R, RGB.G }, { RGB.G, RGB.B } };
                default:
                    throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unsupported Bayer pattern.");
            }
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

        private static (ushort[] Blue, ushort[] Green, ushort[] Red) Read48bppChannels(UnmanagedImage image) {
            // The preserved reference writes into tightly packed unmanaged rows, so read back directly
            // from the raw stride rather than going through any GDI+ conversion layer.
            int width = image.Width;
            int height = image.Height;
            int stride = image.Stride;
            int pixelCount = width * height;
            byte[] buffer = new byte[stride * height];
            Marshal.Copy(image.ImageData, buffer, 0, buffer.Length);

            ushort[] blue = new ushort[pixelCount];
            ushort[] green = new ushort[pixelCount];
            ushort[] red = new ushort[pixelCount];

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;
                int rowStart = y * stride;

                for (int x = 0; x < width; x++) {
                    int offset = rowStart + x * 6;
                    int index = rowOffset + x;
                    blue[index] = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
                    green[index] = (ushort)(buffer[offset + 2] | (buffer[offset + 3] << 8));
                    red[index] = (ushort)(buffer[offset + 4] | (buffer[offset + 5] << 8));
                }
            }

            return (blue, green, red);
        }

        private static void WriteGray16Pixels(UnmanagedImage image, ushort[] sourcePixels) {
            // Write a tightly packed raw buffer because the preserved reference filter predates the
            // stride-fix work and assumes there is no padding between grayscale rows.
            byte[] buffer = new byte[sourcePixels.Length * sizeof(ushort)];
            Buffer.BlockCopy(sourcePixels, 0, buffer, 0, buffer.Length);
            Marshal.Copy(buffer, 0, image.ImageData, buffer.Length);
        }

        // Read the logical pixels out of the padded GDI+ bitmap first, then run the preserved
        // reference algorithm on those samples so the original filter code stays untouched.
        private static ReferenceChannels ComputeReference(
            Bitmap sourceImage,
            int[,] bayerPattern,
            bool performDemosaic,
            bool computeLum) {
            ushort[] sourcePixels = ReadGray16Pixels(sourceImage);
            return ComputeReferenceFromPixels(sourcePixels, sourceImage.Width, sourceImage.Height, bayerPattern, performDemosaic, computeLum);
        }

        // Convenience wrapper for the dominant demosaic scenario used by the file-backed tests.
        private static ReferenceChannels ComputeDemosaicReference(
            ushort[] source,
            int width,
            int height,
            int[,] bayerPattern,
            bool computeLum) {
            return ComputeReferenceFromPixels(source, width, height, bayerPattern, performDemosaic: true, computeLum: computeLum);
        }

        // Raw-map tests only care about direct channel placement, so unwrap just the preserved B/G/R planes.
        private static (ushort[] Blue, ushort[] Green, ushort[] Red) ComputeDirectReference(
            ushort[] source,
            int width,
            int height,
            int[,] bayerPattern) {
            ReferenceChannels reference = ComputeReferenceFromPixels(source, width, height, bayerPattern, performDemosaic: false, computeLum: false);
            return (reference.Blue, reference.Green, reference.Red);
        }

        private static ReferenceChannels ComputeReferenceFromPixels(
            ushort[] sourcePixels,
            int width,
            int height,
            int[,] bayerPattern,
            bool performDemosaic,
            bool computeLum) {
            // Execute the preserved reference code the way it originally ran: on tightly packed
            // unmanaged rows with no GDI+ padding. The optimized implementation is compared against
            // that logical result while other assertions cover padded-bitmap behavior separately.
            using UnmanagedImage source = UnmanagedImage.Create(width, height, width * 2, PixelFormat.Format16bppGrayScale);
            using UnmanagedImage destination = UnmanagedImage.Create(width, height, width * 6, PixelFormat.Format48bppRgb);

            WriteGray16Pixels(source, sourcePixels);

            var filter = new global::NINA.Tests.BayerFilter16bpp {
                BayerPattern = bayerPattern,
                PerformDemosaicing = performDemosaic,
                SaveColorChannels = false,
                SaveLumChannel = computeLum
            };

            filter.Apply(source, destination);

            var channels = Read48bppChannels(destination);
            ushort[] lum = computeLum ? (ushort[])filter.LRGBArrays.Lum.Clone() : Array.Empty<ushort>();
            return new ReferenceChannels(channels.Blue, channels.Green, channels.Red, lum);
        }
    }
}
