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
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using NINA.Image.ImageAnalysis;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NINA.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class DebayerBenchmarks : DebayerBenchmarkBase {
    [Benchmark(Baseline = true)]
    public int OriginalBayerFilter16bpp() {
        var filter = ImagingBenchmarkData.CreateOriginalBayerFilter(BenchmarkCase);
        using var processed = filter.Apply(SourceImage!);
        return processed.Width;
    }
    [Benchmark]
    public int ImprovedBayerFilter16bpp() {
        var filter = ImagingBenchmarkData.CreateOptimizedBayerFilter(BenchmarkCase);
        using var processed = filter.Apply(SourceImage!);
        return processed.Width;
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class CannyBlurredBenchmarks : Gray8BenchmarkBase {
    [Benchmark(Baseline = true)]
    public int OriginalBlurredCannyEdgeDetector() {
        using var bitmap = ImagingBenchmarkData.CreateGray8Bitmap(BenchmarkCase.Width, BenchmarkCase.Height, SourcePixels!);
        var detector = new Accord.Imaging.Filters.CannyEdgeDetector(lowThreshold: 10, highThreshold: 80) {
            GaussianSize = 5,
            GaussianSigma = 1.4
        };
        detector.ApplyInPlace(bitmap);
        return bitmap.Width;
    }
    [Benchmark]
    public int ImprovedBlurredCannyEdgeDetector() {
        using var bitmap = ImagingBenchmarkData.CreateGray8Bitmap(BenchmarkCase.Width, BenchmarkCase.Height, SourcePixels!);
        var detector = new NINA.Image.ImageAnalysis.CannyEdgeDetector(lowThreshold: 10, highThreshold: 80) {
            GaussianSize = 5,
            GaussianSigma = 1.4
        };
        detector.ApplyInPlace(bitmap);
        return bitmap.Width;
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class CannyNoBlurBenchmarks : Gray8BenchmarkBase {
    [Benchmark(Baseline = true)]
    public int OriginalNoBlurCannyEdgeDetector() {
        using var bitmap = ImagingBenchmarkData.CreateGray8Bitmap(BenchmarkCase.Width, BenchmarkCase.Height, SourcePixels!);
        var detector = new NINA.Tests.NoBlurCannyEdgeDetector(lowThreshold: 10, highThreshold: 80);
        detector.ApplyInPlace(bitmap);
        return bitmap.Width;
    }
    [Benchmark]
    public int ImprovedNoBlurCannyEdgeDetector() {
        using var bitmap = ImagingBenchmarkData.CreateGray8Bitmap(BenchmarkCase.Width, BenchmarkCase.Height, SourcePixels!);
        var detector = new NINA.Image.ImageAnalysis.NoBlurCannyEdgeDetector(lowThreshold: 10, highThreshold: 80);
        detector.ApplyInPlace(bitmap);
        return bitmap.Width;
    }
}

public abstract class DebayerBenchmarkBase {
    [ParamsSource(nameof(CaseNames))]
    public string CaseName { get; set; } = string.Empty;

    public IEnumerable<string> CaseNames => ImagingBenchmarkData.BayerCaseNames;

    protected ImagingBenchmarkData.BayerCase BenchmarkCase { get; private set; } = null!;
    protected Bitmap? SourceImage { get; private set; }

    [GlobalSetup]
    public void Setup() {
        BenchmarkCase = ImagingBenchmarkData.GetBayerCase(CaseName);
        ushort[] pixels = ImagingBenchmarkData.CreateBayerPixels(BenchmarkCase.Width, BenchmarkCase.Height);
        SourceImage = ImagingBenchmarkData.CreateGray16Bitmap(BenchmarkCase.Width, BenchmarkCase.Height, pixels);
    }

    [GlobalCleanup]
    public void Cleanup() {
        SourceImage?.Dispose();
    }
}

public abstract class Gray8BenchmarkBase {
    [ParamsSource(nameof(CaseNames))]
    public string CaseName { get; set; } = string.Empty;

    public IEnumerable<string> CaseNames => ImagingBenchmarkData.Gray8CaseNames;

    protected ImagingBenchmarkData.Gray8Case BenchmarkCase { get; private set; } = null!;
    protected byte[]? SourcePixels { get; private set; }

    [GlobalSetup]
    public void Setup() {
        BenchmarkCase = ImagingBenchmarkData.GetGray8Case(CaseName);
        SourcePixels = ImagingBenchmarkData.CreateStructuredPixels(BenchmarkCase.Width, BenchmarkCase.Height);
    }
}

public static class ImagingBenchmarkData {
    public sealed class BayerCase {
        public required string Name { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required bool PerformDemosaic { get; init; }
        public required bool SaveColorChannels { get; init; }
        public required bool SaveLumChannel { get; init; }
    }

    public sealed class Gray8Case {
        public required string Name { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
    }

    private static readonly Dictionary<string, BayerCase> BayerCases = new Dictionary<string, BayerCase> {
        ["Bayer16Aligned_IMX571_6224x4168_Demosaic"] = new BayerCase {
            Name = "Bayer16Aligned_IMX571_6224x4168_Demosaic",
            Width = 6224,
            Height = 4168,
            PerformDemosaic = true,
            SaveColorChannels = true,
            SaveLumChannel = true
        }
    };

    private static readonly Dictionary<string, Gray8Case> Gray8Cases = new Dictionary<string, Gray8Case> {
        ["Gray8Aligned_IMX571_6224x4168"] = new Gray8Case { Name = "Gray8Aligned_IMX571_6224x4168", Width = 6224, Height = 4168 }
    };

    public static IEnumerable<string> BayerCaseNames => BayerCases.Keys;
    public static IEnumerable<string> Gray8CaseNames => Gray8Cases.Keys;
    public static BayerCase GetBayerCase(string name) => BayerCases[name];
    public static Gray8Case GetGray8Case(string name) => Gray8Cases[name];

    public static NINA.Tests.BayerFilter16bpp CreateOriginalBayerFilter(BayerCase benchmarkCase) {
        return new NINA.Tests.BayerFilter16bpp {
            BayerPattern = CreateBenchmarkBayerPattern(),
            PerformDemosaicing = benchmarkCase.PerformDemosaic,
            SaveColorChannels = benchmarkCase.SaveColorChannels,
            SaveLumChannel = benchmarkCase.SaveLumChannel
        };
    }

    public static NINA.Image.ImageAnalysis.BayerFilter16bpp CreateOptimizedBayerFilter(BayerCase benchmarkCase) {
        return new NINA.Image.ImageAnalysis.BayerFilter16bpp {
            BayerPattern = CreateBenchmarkBayerPattern(),
            PerformDemosaicing = benchmarkCase.PerformDemosaic,
            SaveColorChannels = benchmarkCase.SaveColorChannels,
            SaveLumChannel = benchmarkCase.SaveLumChannel
        };
    }

    public static Bitmap CreateGray16Bitmap(int width, int height, ushort[] pixels) {
        Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format16bppGrayScale);

        try {
            FillGray16Bitmap(bitmap, pixels);
            return bitmap;
        } catch {
            bitmap.Dispose();
            throw;
        }
    }

    public static Bitmap CreateGray8Bitmap(int width, int height, byte[] pixels) {
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

    public static byte[] CreateStructuredPixels(int width, int height) {
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

    public static ushort[] CreateBayerPixels(int width, int height) {
        ushort[] pixels = new ushort[width * height];

        for (int y = 0; y < height; y++) {
            int rowOffset = y * width;

            for (int x = 0; x < width; x++) {
                int background = 700 + ((x * 17 + y * 29) % 1200);
                int nebula = ((x * x + y * y) % 4096) / 2;
                int sparkle = (((x * 73856093) ^ (y * 19349663)) & 1023) < 2 ? 18000 : 0;
                int value = background + nebula + sparkle;

                if (value > ushort.MaxValue) {
                    value = ushort.MaxValue;
                }

                pixels[rowOffset + x] = (ushort)value;
            }
        }

        return pixels;
    }

    private static void FillGray16Bitmap(Bitmap bitmap, ushort[] pixels) {
        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format16bppGrayScale);

        try {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = Math.Abs(data.Stride);
            byte[] rowBuffer = new byte[stride];

            for (int y = 0; y < height; y++) {
                Array.Fill(rowBuffer, (byte)0xCD);

                int rowOffset = y * width;
                for (int x = 0; x < width; x++) {
                    ushort value = pixels[rowOffset + x];
                    int pixelOffset = x * 2;
                    rowBuffer[pixelOffset] = (byte)(value & 0xFF);
                    rowBuffer[pixelOffset + 1] = (byte)(value >> 8);
                }

                Marshal.Copy(rowBuffer, 0, GetRowPointer(data, y), rowBuffer.Length);
            }
        } finally {
            bitmap.UnlockBits(data);
        }
    }

    private static ColorPalette CreateGrayscalePalette(ColorPalette palette) {
        for (int i = 0; i < 256; i++) {
            palette.Entries[i] = Color.FromArgb(i, i, i);
        }

        return palette;
    }

    private static void FillGray8Bitmap(Bitmap bitmap, byte[] pixels) {
        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

        try {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int stride = Math.Abs(data.Stride);
            byte[] rowBuffer = new byte[stride];

            for (int y = 0; y < height; y++) {
                Array.Fill(rowBuffer, (byte)0);
                Buffer.BlockCopy(pixels, y * width, rowBuffer, 0, width);
                Marshal.Copy(rowBuffer, 0, GetRowPointer(data, y), rowBuffer.Length);
            }
        } finally {
            bitmap.UnlockBits(data);
        }
    }

    private static IntPtr GetRowPointer(BitmapData data, int y) {
        return IntPtr.Add(data.Scan0, data.Stride * y);
    }

    private static int[,] CreateBenchmarkBayerPattern() {
        return new int[,] { { RGB.R, RGB.G }, { RGB.G, RGB.B } };
    }
}