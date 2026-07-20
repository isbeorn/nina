#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;

namespace NINA.Test.Image.ImageAnalysis {
    // Shared deterministic image generators for exact-comparison tests.
    // The same source families can be used for 8-bit blur tests and 16-bit Bayer tests so coverage
    // stays conceptually aligned even though the pixel formats differ.
    internal static class DeterministicImageFixtures {
        internal sealed class ImageFixture {
            public required string Name { get; init; }
            public required Func<int, int, byte[]> CreateBytes { get; init; }
            public required Func<int, int, ushort[]> CreateUShorts { get; init; }
        }

        internal sealed class ThresholdAwareImageFixture {
            public required string Name { get; init; }
            public required Func<int, int, byte, byte, byte[]> CreateBytes { get; init; }
        }

        public static ImageFixture UniformBlack { get; } = CreateFixture(
            "UniformBlack",
            static (width, height) => CreateUniformUShorts(width, height, 0));

        public static ImageFixture UniformWhite { get; } = CreateFixture(
            "UniformWhite",
            static (width, height) => CreateUniformUShorts(width, height, ushort.MaxValue));

        public static ImageFixture SingleImpulseCenter { get; } = CreateFixture(
            "SingleImpulseCenter",
            CreateSingleImpulseCenterUShorts);

        public static ImageFixture SparseStarField { get; } = CreateFixture(
            "SparseStarField",
            CreateSparseStarFieldUShorts);

        public static ImageFixture FeatureMix { get; } = CreateFixture(
            "FeatureMix",
            static (width, height) => CreateRandomFeatureSceneUShorts(width, height, 20260411));

        public static ImageFixture StepEdge { get; } = CreateFixture(
            "VerticalStep",
            CreateStepEdgeUShorts);

        public static ImageFixture DiagonalLine { get; } = CreateFixture(
            "DiagonalLine",
            CreateDiagonalLineUShorts);

        public static ImageFixture Checkerboard { get; } = CreateFixture(
            "Checkerboard",
            CreateCheckerboardUShorts);

        public static ImageFixture BandingAndHotPixels { get; } = CreateFixture(
            "BandingAndHotPixels",
            CreateBandingAndHotPixelsUShorts);

        public static ImageFixture SeededFuzz17 { get; } = CreateFixture(
            "SeededFuzz_Seed17",
            static (width, height) => CreateSeededFuzzUShorts(width, height, 17));

        public static ImageFixture Structured { get; } = CreateFixture(
            "Structured",
            CreateStructuredUShorts);

        public static ThresholdAwareImageFixture HysteresisConnectedWeakEdge { get; } = CreateThresholdAwareFixture(
            "HysteresisConnectedWeakEdge",
            CreateHysteresisConnectedWeakEdgeBytes);

        public static ThresholdAwareImageFixture HysteresisIsolatedWeakEdge { get; } = CreateThresholdAwareFixture(
            "HysteresisIsolatedWeakEdge",
            CreateHysteresisIsolatedWeakEdgeBytes);

        public static ThresholdAwareImageFixture HysteresisDiagonalBridge { get; } = CreateThresholdAwareFixture(
            "HysteresisDiagonalBridge",
            CreateHysteresisDiagonalBridgeBytes);

        public static ImageFixture RepresentativeFormatCoverage { get; } = Structured;

        public static IReadOnlyList<ImageFixture> All { get; } = new[] {
            UniformBlack,
            UniformWhite,
            SingleImpulseCenter,
            SparseStarField,
            FeatureMix,
            StepEdge,
            DiagonalLine,
            Checkerboard,
            BandingAndHotPixels,
            SeededFuzz17,
            Structured
        };

        public static IReadOnlyList<ImageFixture> CuratedFeatures { get; } = new[] {
            UniformBlack,
            UniformWhite,
            SingleImpulseCenter,
            SparseStarField,
            FeatureMix,
            StepEdge,
            DiagonalLine,
            Checkerboard,
            BandingAndHotPixels,
            Structured
        };

        public static IReadOnlyList<ThresholdAwareImageFixture> HysteresisFeatures { get; } = new[] {
            HysteresisConnectedWeakEdge,
            HysteresisIsolatedWeakEdge,
            HysteresisDiagonalBridge
        };

        public static byte[] CreateUniformBytes(int width, int height, byte value) {
            byte[] pixels = new byte[width * height];
            Array.Fill(pixels, value);
            return pixels;
        }

        public static ushort[] CreateUniformUShorts(int width, int height, ushort value) {
            ushort[] pixels = new ushort[width * height];
            Array.Fill(pixels, value);
            return pixels;
        }

        public static byte[] CreateSingleImpulseCenterBytes(int width, int height) {
            return DownscaleToBytes(CreateSingleImpulseCenterUShorts(width, height));
        }

        public static ushort[] CreateSingleImpulseCenterUShorts(int width, int height) {
            ushort[] pixels = CreateUniformUShorts(width, height, 0);
            pixels[(height / 2) * width + (width / 2)] = ushort.MaxValue;
            return pixels;
        }

        public static byte[] CreateStructuredBytes(int width, int height) {
            return DownscaleToBytes(CreateStructuredUShorts(width, height));
        }

        public static ushort[] CreateStructuredUShorts(int width, int height) {
            ushort[] pixels = new ushort[width * height];

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;

                for (int x = 0; x < width; x++) {
                    int value = 5000 + ((x * 137 + y * 257 + ((x * y) % 61) * 113) & 0x3FFF);

                    if (x > width / 3 && x < (width * 2) / 3) {
                        value += 7000;
                    }

                    if (y > height / 2) {
                        value += 5000;
                    }

                    if (Math.Abs(x - (y * width) / Math.Max(1, height)) <= 2) {
                        value += 9000;
                    }

                    if (((x / 7) + (y / 11)) % 2 == 0) {
                        value -= 3500;
                    }

                    if (((x * 73856093) ^ (y * 19349663)) % 257 == 0) {
                        value = 60000;
                    }

                    pixels[rowOffset + x] = ClampToUShort(value);
                }
            }

            return pixels;
        }

        public static byte[] CreateBandingAndHotPixelsBytes(int width, int height) {
            return DownscaleToBytes(CreateBandingAndHotPixelsUShorts(width, height));
        }

        public static ushort[] CreateBandingAndHotPixelsUShorts(int width, int height) {
            ushort[] pixels = new ushort[width * height];

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;
                int rowBias = ((y / 9) % 2 == 0) ? 2200 : -1600;

                for (int x = 0; x < width; x++) {
                    int value = 6000 + rowBias + ((x / 64) % 3) * 1400 + ((x * 5 + y * 3) & 0x03FF);

                    if (x > width / 4 && x < (width * 3) / 4) {
                        value += 2800;
                    }

                    if ((x + 3 * y) % 211 == 0) {
                        value = ushort.MaxValue;
                    }

                    pixels[rowOffset + x] = ClampToUShort(value);
                }
            }

            return pixels;
        }

        public static byte[] CreateSparseStarFieldBytes(int width, int height) {
            return DownscaleToBytes(CreateSparseStarFieldUShorts(width, height));
        }

        public static ushort[] CreateSparseStarFieldUShorts(int width, int height) {
            ushort[] pixels = CreateUniformUShorts(width, height, 1500);
            Random random = new Random(20260408);
            int starCount = GetDenseStarCount(width, height);

            for (int i = 0; i < starCount; i++) {
                AddRandomStar(pixels, width, height, random);
            }

            return pixels;
        }

        public static byte[] CreateStepEdgeBytes(int width, int height) {
            return DownscaleToBytes(CreateStepEdgeUShorts(width, height));
        }

        public static ushort[] CreateStepEdgeUShorts(int width, int height) {
            ushort[] pixels = new ushort[width * height];
            ushort leftValue = ToExpandedUShort(24);
            ushort rightValue = ToExpandedUShort(220);

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;

                for (int x = 0; x < width; x++) {
                    pixels[rowOffset + x] = (x < width / 2) ? leftValue : rightValue;
                }
            }

            return pixels;
        }

        public static byte[] CreateDiagonalLineBytes(int width, int height) {
            return DownscaleToBytes(CreateDiagonalLineUShorts(width, height));
        }

        public static ushort[] CreateDiagonalLineUShorts(int width, int height) {
            ushort[] pixels = CreateUniformUShorts(width, height, ToExpandedUShort(18));
            ushort shoulder = ToExpandedUShort(180);

            for (int y = 0; y < height; y++) {
                int x = (y * width) / Math.Max(1, height);
                pixels[y * width + x] = ushort.MaxValue;

                if (x + 1 < width) {
                    pixels[y * width + x + 1] = shoulder;
                }
            }

            return pixels;
        }

        public static byte[] CreateCheckerboardBytes(int width, int height) {
            return DownscaleToBytes(CreateCheckerboardUShorts(width, height));
        }

        public static ushort[] CreateCheckerboardUShorts(int width, int height) {
            ushort[] pixels = new ushort[width * height];
            ushort lowValue = ToExpandedUShort(32);
            ushort highValue = ToExpandedUShort(224);

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;

                for (int x = 0; x < width; x++) {
                    pixels[rowOffset + x] = (((x / 2) + (y / 2)) % 2 == 0) ? lowValue : highValue;
                }
            }

            return pixels;
        }

        public static byte[] CreateSeededFuzzBytes(int width, int height, int seed) {
            return DownscaleToBytes(CreateSeededFuzzUShorts(width, height, seed));
        }

        public static ushort[] CreateSeededFuzzUShorts(int width, int height, int seed) {
            ushort[] pixels = new ushort[width * height];
            Random random = new Random(seed);

            for (int i = 0; i < pixels.Length; i++) {
                pixels[i] = (ushort)random.Next(0, ushort.MaxValue + 1);
            }

            return pixels;
        }

        public static byte[] CreateRandomFeatureSceneBytes(int width, int height, int seed) {
            return DownscaleToBytes(CreateRandomFeatureSceneUShorts(width, height, seed));
        }

        public static ushort[] CreateRandomFeatureSceneUShorts(int width, int height, int seed) {
            ushort[] pixels = CreateFeatureSceneBackground(width, height);
            Random random = new Random(seed);
            int area = width * height;

            // Randomized Canny scenes should look like real star-rich frames, not a sparse synthetic
            // collage. Lay down a dedicated stellar population first, then mix in broader structures.
            int starCount = GetDenseStarCount(width, height);
            int nonStellarFeatureCount = Math.Max(18, area / 90000);

            for (int i = 0; i < starCount; i++) {
                AddRandomStar(pixels, width, height, random);
            }

            for (int i = 0; i < nonStellarFeatureCount; i++) {
                switch (random.Next(4)) {
                    case 0:
                        AddRandomSoftBlob(pixels, width, height, random);
                        break;
                    case 1:
                        AddRandomSquare(pixels, width, height, random);
                        break;
                    case 2:
                        AddRandomPatch(pixels, width, height, random);
                        break;
                    default:
                        AddRandomContourRing(pixels, width, height, random);
                        break;
                }
            }

            return pixels;
        }

        public static byte[] CreateHysteresisConnectedWeakEdgeBytes(int width, int height, byte lowThreshold, byte highThreshold) {
            ushort[] pixels = CreateHysteresisBaseScene(width, height, lowThreshold, highThreshold, out int strongValue, out int weakValue, out _);

            int strongLeft = Math.Max(2, width / 4);
            int strongTop = Math.Max(2, height / 6);
            int strongBottom = Math.Max(strongTop + 8, (height * 5) / 6);
            int strongWidth = Math.Max(12, width / 20);
            AddFilledRectangle(pixels, width, height, strongLeft, strongTop, strongWidth, strongBottom - strongTop, strongValue);

            int weakLeft = strongLeft + strongWidth - 1;
            int weakTop = Math.Max(2, height / 3);
            int weakWidth = Math.Max(14, width / 8);
            int weakHeight = Math.Max(8, height / 18);
            AddFilledRectangle(pixels, width, height, weakLeft, weakTop, weakWidth, weakHeight, weakValue);

            return DownscaleToBytes(pixels);
        }

        public static byte[] CreateHysteresisIsolatedWeakEdgeBytes(int width, int height, byte lowThreshold, byte highThreshold) {
            ushort[] pixels = CreateHysteresisBaseScene(width, height, lowThreshold, highThreshold, out int strongValue, out int weakValue, out int belowLowValue);

            int strongLeft = Math.Max(2, width / 4);
            int strongTop = Math.Max(2, height / 6);
            int strongBottom = Math.Max(strongTop + 8, (height * 5) / 6);
            int strongWidth = Math.Max(12, width / 20);
            AddFilledRectangle(pixels, width, height, strongLeft, strongTop, strongWidth, strongBottom - strongTop, strongValue);

            int weakLeft = Math.Min(width - 12, (width * 2) / 3);
            int weakTop = Math.Max(2, height / 3);
            int weakWidth = Math.Max(10, width / 10);
            int weakHeight = Math.Max(8, height / 18);
            AddFilledRectangle(pixels, width, height, weakLeft, weakTop, weakWidth, weakHeight, weakValue);

            int belowLowLeft = Math.Max(2, width / 3);
            int belowLowTop = Math.Max(2, height / 5);
            AddFilledRectangle(pixels, width, height, belowLowLeft, belowLowTop, Math.Max(8, width / 14), Math.Max(6, height / 20), belowLowValue);

            return DownscaleToBytes(pixels);
        }

        public static byte[] CreateHysteresisDiagonalBridgeBytes(int width, int height, byte lowThreshold, byte highThreshold) {
            ushort[] pixels = CreateHysteresisBaseScene(width, height, lowThreshold, highThreshold, out int strongValue, out int weakValue, out _);

            int strongSize = Math.Max(14, Math.Min(width, height) / 12);
            int strongX = Math.Max(2, width / 5);
            int strongY = Math.Max(2, height / 5);
            AddFilledRectangle(pixels, width, height, strongX, strongY, strongSize, strongSize, strongValue);

            int bridgeLength = Math.Max(5, Math.Min(width, height) / 14);
            for (int i = 0; i < bridgeLength; i++) {
                int x = strongX + strongSize - 1 + i * 2;
                int y = strongY + strongSize - 1 + i * 2;
                AddFilledRectangle(pixels, width, height, x, y, 3, 3, weakValue);
            }

            int tailX = Math.Min(width - 16, strongX + strongSize + bridgeLength * 2);
            int tailY = Math.Min(height - 12, strongY + strongSize + bridgeLength * 2);
            AddFilledRectangle(pixels, width, height, tailX, tailY, Math.Max(8, width / 18), Math.Max(6, height / 22), weakValue);

            return DownscaleToBytes(pixels);
        }

        public static byte[] DownscaleToBytes(ushort[] pixels) {
            byte[] bytes = new byte[pixels.Length];

            for (int i = 0; i < pixels.Length; i++) {
                bytes[i] = (byte)(pixels[i] >> 8);
            }

            return bytes;
        }

        private static ImageFixture CreateFixture(string name, Func<int, int, ushort[]> createUShorts) {
            return new ImageFixture {
                Name = name,
                CreateBytes = (width, height) => DownscaleToBytes(createUShorts(width, height)),
                CreateUShorts = createUShorts
            };
        }

        private static ThresholdAwareImageFixture CreateThresholdAwareFixture(string name, Func<int, int, byte, byte, byte[]> createBytes) {
            return new ThresholdAwareImageFixture {
                Name = name,
                CreateBytes = createBytes
            };
        }

        private static ushort[] CreateFeatureSceneBackground(int width, int height) {
            ushort[] pixels = new ushort[width * height];

            for (int y = 0; y < height; y++) {
                int rowOffset = y * width;
                int rowBias = (y * 97) & 0x03FF;

                for (int x = 0; x < width; x++) {
                    int value = 2500 + rowBias + ((x * 53 + y * 29) & 0x01FF);
                    pixels[rowOffset + x] = ClampToUShort(value);
                }
            }

            return pixels;
        }

        private static ushort[] CreateHysteresisBaseScene(int width, int height, byte lowThreshold, byte highThreshold, out int strongValue, out int weakValue, out int belowLowValue) {
            ushort[] pixels = CreateUniformUShorts(width, height, ToExpandedUShort(20));
            int strongDelta = ToExpandedUShort(220);
            int weakMidPoint = Math.Max(lowThreshold + 8, (lowThreshold + highThreshold) / 2);
            int belowLowPoint = Math.Max(2, lowThreshold / 2);

            strongValue = ToExpandedUShort(20) + strongDelta;
            weakValue = ToExpandedUShort(20) + (strongDelta * weakMidPoint / 255);
            belowLowValue = ToExpandedUShort(20) + (strongDelta * belowLowPoint / 255);

            return pixels;
        }

        private static void AddRandomStar(ushort[] pixels, int width, int height, Random random) {
            double starClass = random.NextDouble();
            ushort core;
            int radiusX;
            int radiusY;
            bool addSpikes;

            // Keep a mix of tight stars, medium stars, and a smaller set of broad stars with visible
            // contour falloff. That gives the random corpus much more size variety.
            if (starClass < 0.55) {
                core = (ushort)random.Next(26000, 65536);
                radiusX = random.Next(1, 4);
                radiusY = random.Next(1, 4);
                addSpikes = random.NextDouble() < 0.25;
            } else if (starClass < 0.88) {
                core = (ushort)random.Next(22000, 62000);
                radiusX = random.Next(3, 8);
                radiusY = random.Next(2, 7);
                addSpikes = random.NextDouble() < 0.55;
            } else {
                core = (ushort)random.Next(18000, 56000);
                radiusX = random.Next(7, 19);
                radiusY = random.Next(6, 15);
                addSpikes = true;
            }

            (int centerX, int centerY) = GetStarAnchor(width, height, radiusX, radiusY, random);

            AddGradientStar(pixels, width, height, centerX, centerY, core, radiusX, radiusY, addSpikes);

            // Larger stars need a softer halo so they do not all read as the same compact shape.
            int haloRadiusX = Math.Max(radiusX + 1, (int)Math.Ceiling(radiusX * 1.8));
            int haloRadiusY = Math.Max(radiusY + 1, (int)Math.Ceiling(radiusY * 1.8));
            ushort haloPeak = (ushort)Math.Max(2500, core / (starClass < 0.55 ? 9 : 6));
            AddEllipseFalloff(pixels, width, height, centerX, centerY, haloRadiusX, haloRadiusY, haloPeak, edgeHardness: 1.3);
        }

        private static int GetDenseStarCount(int width, int height) {
            int area = width * height;
            int scaledCount = area / 9000;
            int denseTarget = Math.Clamp(scaledCount, 240, 320);
            int maxReasonableCount = Math.Max(1, area / 16);
            return Math.Min(denseTarget, maxReasonableCount);
        }

        private static (int X, int Y) GetStarAnchor(int width, int height, int radiusX, int radiusY, Random random) {
            int haloMarginX = Math.Min(Math.Max(0, width / 2), Math.Max(radiusX * 2, 4));
            int haloMarginY = Math.Min(Math.Max(0, height / 2), Math.Max(radiusY * 2, 4));
            int minX = Math.Min(haloMarginX, Math.Max(0, width - 1));
            int maxX = Math.Max(minX + 1, width - haloMarginX);
            int minY = Math.Min(haloMarginY, Math.Max(0, height - 1));
            int maxY = Math.Max(minY + 1, height - haloMarginY);

            int x = random.Next(minX, maxX);
            int y = random.Next(minY, maxY);

            return (Math.Clamp(x, 0, Math.Max(0, width - 1)), Math.Clamp(y, 0, Math.Max(0, height - 1)));
        }

        private static void AddRandomSoftBlob(ushort[] pixels, int width, int height, Random random) {
            (int centerX, int centerY) = GetFeatureAnchor(width, height, random);
            int radiusX = random.Next(6, Math.Max(7, width / 12));
            int radiusY = random.Next(6, Math.Max(7, height / 12));
            ushort peak = (ushort)random.Next(14000, 42000);
            AddEllipseFalloff(pixels, width, height, centerX, centerY, radiusX, radiusY, peak, edgeHardness: 1.8);
        }

        private static void AddRandomSquare(ushort[] pixels, int width, int height, Random random) {
            (int centerX, int centerY) = GetFeatureAnchor(width, height, random);
            int halfSize = random.Next(4, Math.Max(5, Math.Min(width, height) / 14));
            ushort value = (ushort)random.Next(12000, 48000);

            for (int dy = -halfSize; dy <= halfSize; dy++) {
                for (int dx = -halfSize; dx <= halfSize; dx++) {
                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int falloff = Math.Max(0, value - distance * (value / Math.Max(2, halfSize + 1)));
                    AddPixelSaturating(pixels, width, height, centerX + dx, centerY + dy, (ushort)falloff);
                }
            }
        }

        private static void AddRandomPatch(ushort[] pixels, int width, int height, Random random) {
            (int centerX, int centerY) = GetFeatureAnchor(width, height, random);
            int patchWidth = random.Next(10, Math.Max(11, width / 8));
            int patchHeight = random.Next(10, Math.Max(11, height / 8));
            int left = centerX - patchWidth / 2;
            int top = centerY - patchHeight / 2;
            ushort value = (ushort)random.Next(9000, 28000);

            for (int y = 0; y < patchHeight; y++) {
                for (int x = 0; x < patchWidth; x++) {
                    int edgeDistance = Math.Min(Math.Min(x, y), Math.Min(patchWidth - 1 - x, patchHeight - 1 - y));
                    int intensity = Math.Max(0, value - edgeDistance * (value / Math.Max(3, Math.Min(patchWidth, patchHeight) / 2)));
                    AddPixelSaturating(pixels, width, height, left + x, top + y, (ushort)intensity);
                }
            }
        }

        private static void AddRandomContourRing(ushort[] pixels, int width, int height, Random random) {
            (int centerX, int centerY) = GetFeatureAnchor(width, height, random);
            int outerRadius = random.Next(6, Math.Max(7, Math.Min(width, height) / 12));
            int innerRadius = Math.Max(2, outerRadius / 2);
            ushort value = (ushort)random.Next(12000, 36000);

            for (int dy = -outerRadius; dy <= outerRadius; dy++) {
                for (int dx = -outerRadius; dx <= outerRadius; dx++) {
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance > outerRadius || distance < innerRadius) {
                        continue;
                    }

                    double ringWeight = 1.0 - Math.Abs(distance - (innerRadius + outerRadius) * 0.5) / Math.Max(1.0, (outerRadius - innerRadius) * 0.5);
                    ushort intensity = (ushort)Math.Max(0, value * ringWeight);
                    AddPixelSaturating(pixels, width, height, centerX + dx, centerY + dy, intensity);
                }
            }
        }

        private static (int X, int Y) GetFeatureAnchor(int width, int height, Random random) {
            int mode = random.Next(6);
            int x;
            int y;

            switch (mode) {
                case 0:
                    x = width / 2 + random.Next(-Math.Max(2, width / 10), Math.Max(3, width / 10));
                    y = height / 2 + random.Next(-Math.Max(2, height / 10), Math.Max(3, height / 10));
                    break;
                case 1:
                    x = random.Next(0, Math.Max(1, width / 8));
                    y = random.Next(0, Math.Max(1, height));
                    break;
                case 2:
                    x = random.Next(Math.Max(0, width - Math.Max(1, width / 8)), Math.Max(1, width));
                    y = random.Next(0, Math.Max(1, height));
                    break;
                case 3:
                    x = random.Next(0, Math.Max(1, width));
                    y = random.Next(0, Math.Max(1, height / 8));
                    break;
                case 4:
                    x = random.Next(0, Math.Max(1, width));
                    y = random.Next(Math.Max(0, height - Math.Max(1, height / 8)), Math.Max(1, height));
                    break;
                default:
                    x = random.Next(0, Math.Max(1, width));
                    y = random.Next(0, Math.Max(1, height));
                    break;
            }

            return (Math.Clamp(x, 0, Math.Max(0, width - 1)), Math.Clamp(y, 0, Math.Max(0, height - 1)));
        }

        private static void AddGradientStar(ushort[] pixels, int width, int height, int centerX, int centerY, ushort core, int radiusX, int radiusY, bool addSpikes) {
            int maxRadius = Math.Max(radiusX, radiusY) + 2;

            for (int dy = -maxRadius; dy <= maxRadius; dy++) {
                for (int dx = -maxRadius; dx <= maxRadius; dx++) {
                    double normalizedX = dx / (double)Math.Max(1, radiusX);
                    double normalizedY = dy / (double)Math.Max(1, radiusY);
                    double distanceSquared = normalizedX * normalizedX + normalizedY * normalizedY;
                    double weight = Math.Exp(-distanceSquared * 1.35);

                    if (addSpikes && (dx == 0 || dy == 0)) {
                        weight = Math.Max(weight, Math.Exp(-Math.Abs(dx + dy) * 0.55));
                    }

                    ushort intensity = (ushort)Math.Max(0, core * weight);
                    if (intensity > 0) {
                        AddPixelSaturating(pixels, width, height, centerX + dx, centerY + dy, intensity);
                    }
                }
            }
        }

        private static void AddEllipseFalloff(ushort[] pixels, int width, int height, int centerX, int centerY, int radiusX, int radiusY, ushort peak, double edgeHardness) {
            for (int dy = -radiusY; dy <= radiusY; dy++) {
                for (int dx = -radiusX; dx <= radiusX; dx++) {
                    double normalizedX = dx / (double)Math.Max(1, radiusX);
                    double normalizedY = dy / (double)Math.Max(1, radiusY);
                    double distanceSquared = normalizedX * normalizedX + normalizedY * normalizedY;
                    if (distanceSquared > 1.0) {
                        continue;
                    }

                    double weight = Math.Pow(1.0 - distanceSquared, edgeHardness);
                    ushort intensity = (ushort)Math.Max(0, peak * weight);
                    AddPixelSaturating(pixels, width, height, centerX + dx, centerY + dy, intensity);
                }
            }
        }

        private static void AddFilledRectangle(ushort[] pixels, int width, int height, int left, int top, int rectangleWidth, int rectangleHeight, int value) {
            for (int y = 0; y < rectangleHeight; y++) {
                for (int x = 0; x < rectangleWidth; x++) {
                    AddPixelSaturating(pixels, width, height, left + x, top + y, ClampToUShort(value));
                }
            }
        }

        private static void AddPixelSaturating(ushort[] pixels, int width, int height, int x, int y, ushort amount) {
            if ((uint)x >= (uint)width || (uint)y >= (uint)height) {
                return;
            }

            int index = y * width + x;
            int value = pixels[index] + amount;
            pixels[index] = (ushort)Math.Min(ushort.MaxValue, value);
        }

        private static ushort ClampToUShort(int value) {
            if (value < 0) {
                return 0;
            }

            if (value > ushort.MaxValue) {
                return ushort.MaxValue;
            }

            return (ushort)value;
        }

        private static ushort ToExpandedUShort(byte value) {
            return (ushort)(value * 257);
        }
    }
}
