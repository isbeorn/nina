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

        public static IReadOnlyList<ImageFixture> All { get; } = new[] {
            UniformBlack,
            UniformWhite,
            SingleImpulseCenter,
            SparseStarField,
            StepEdge,
            DiagonalLine,
            Checkerboard,
            BandingAndHotPixels,
            SeededFuzz17,
            Structured
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
            int starCount = Math.Max(24, (width * height) / 24000);

            for (int i = 0; i < starCount; i++) {
                // Keep stars away from the outer frame so every size variant can fit without clipping.
                int margin = Math.Min(4, Math.Min(Math.Max(0, width / 2), Math.Max(0, height / 2)));
                int minX = Math.Min(margin, Math.Max(0, width - 1));
                int maxX = Math.Max(minX + 1, width - margin);
                int minY = Math.Min(margin, Math.Max(0, height - 1));
                int maxY = Math.Max(minY + 1, height - margin);
                int x = random.Next(minX, maxX);
                int y = random.Next(minY, maxY);
                ushort core = (ushort)random.Next(40000, 65536);
                int radius = random.Next(0, 3);
                AddSyntheticStar(pixels, width, height, x, y, core, radius);
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

        private static void AddSyntheticStar(ushort[] pixels, int width, int height, int centerX, int centerY, ushort core, int radius) {
            for (int dy = -radius; dy <= radius; dy++) {
                for (int dx = -radius; dx <= radius; dx++) {
                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    ushort intensity = distance switch {
                        0 => core,
                        1 => (ushort)Math.Max(6000, core / 4),
                        2 => (ushort)Math.Max(2500, core / 10),
                        _ => 0
                    };

                    if (intensity == 0) {
                        continue;
                    }

                    // Keep radius-1 stars slightly cross-shaped, while radius-2 stars stay broader.
                    if (radius == 1 && distance == 1 && Math.Abs(dx) == 1 && Math.Abs(dy) == 1) {
                        continue;
                    }

                    AddPixelSaturating(pixels, width, height, centerX + dx, centerY + dy, intensity);
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
