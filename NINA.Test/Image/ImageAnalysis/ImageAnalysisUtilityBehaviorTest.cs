using FluentAssertions;
using NINA.Core.Enum;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NUnit.Framework;
using OxyPlot;
using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace NINA.Test.Image.ImageAnalysis {

    [TestFixture]
    public class ImageAnalysisUtilityBehaviorTest {

        /// <summary>
        /// Verifies crop and ROI math for centered crops, donut-shaped guide ROIs, rejected inner regions, and rejected outer regions.
        /// </summary>
        [Test]
        public void DetectionUtility_CropAndRoiMathIsCenteredAndBoundaryAware() {
            using var image = new Bitmap(width: 101, height: 51, format: DrawingPixelFormat.Format24bppRgb);

            Rectangle crop = DetectionUtility.GetCropRectangle(image, cropRatio: 0.5);

            crop.Should().Be(new Rectangle(25, 12, 50, 25));
            DetectionUtility.InROI(new Size(100, 100), new Rectangle(20, 20, 10, 10), outerCropRatio: 0.8, innerCropRatio: 0.2).Should().BeTrue();
            DetectionUtility.InROI(new Size(100, 100), new Rectangle(45, 45, 4, 4), outerCropRatio: 0.8, innerCropRatio: 0.2).Should().BeFalse();
            DetectionUtility.InROI(new Size(100, 100), new Rectangle(1, 1, 5, 5), outerCropRatio: 0.8, innerCropRatio: 0.2).Should().BeFalse();
            new Rectangle(5, 5, 5, 5).FullyInsideRect(new Rectangle(5, 5, 10, 10)).Should().BeTrue();
            new Rectangle(5, 5, 11, 5).FullyInsideRect(new Rectangle(5, 5, 10, 10)).Should().BeFalse();
        }

        /// <summary>
        /// Verifies the Laplacian-of-Gaussian kernel keeps the expected zero-sum invariant and radial symmetry.
        /// </summary>
        [Test]
        public void LaplacianOfGaussianKernel_IsZeroSumAndSymmetric() {
            int[,] kernel = DetectionUtility.LaplacianOfGaussianKernel(size: 5, sigma: 1.4);

            int sum = 0;
            for (int y = 0; y < 5; y++) {
                for (int x = 0; x < 5; x++) {
                    sum += kernel[x, y];
                    kernel[x, y].Should().Be(kernel[4 - x, 4 - y]);
                }
            }

            sum.Should().Be(0);
            kernel[2, 2].Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Verifies resize-for-detection returns the original bitmap below the threshold and a resized replacement above the threshold.
        /// </summary>
        [Test]
        public void ResizeForDetection_ReturnsOriginalOrScaledBitmapAccordingToMaximumWidth() {
            using var small = new Bitmap(width: 20, height: 10, format: DrawingPixelFormat.Format24bppRgb);
            Bitmap same = DetectionUtility.ResizeForDetection(small, maxWidth: 20, resizeFactor: 0.5);
            same.Should().BeSameAs(small);

            using var large = new Bitmap(width: 100, height: 50, format: DrawingPixelFormat.Format24bppRgb);
            using Bitmap resized = DetectionUtility.ResizeForDetection(large, maxWidth: 50, resizeFactor: 0.25);

            resized.Width.Should().Be(25);
            resized.Height.Should().Be(12);
        }

        /// <summary>
        /// Verifies bit shifting handles null input, no-op shifts, vectorized body elements, and scalar tail elements deterministically.
        /// </summary>
        [Test]
        public void BitShiftLeftInPlace_HandlesGuardNoOpVectorBodyAndTail() {
            Action nullShift = () => ImageUtility.BitShiftLeftInPlace(null, 1);
            nullShift.Should().Throw<ArgumentNullException>();

            ushort[] noOp = { 1, 2, 3 };
            ImageUtility.BitShiftLeftInPlace(noOp, 0);
            noOp.Should().Equal(1, 2, 3);

            ushort[] data = Enumerable.Range(0, Vector<ushort>.Count + 5).Select(x => (ushort)x).ToArray();
            ushort[] expected = data.Select(x => (ushort)(x << 2)).ToArray();

            ImageUtility.BitShiftLeftInPlace(data, 2);

            data.Should().Equal(expected);
        }

        /// <summary>
        /// Verifies 32-bit image arrays clamp out-of-range values and cache the converted 16-bit projection.
        /// </summary>
        [Test]
        public void ImageArrayInt_FlatArrayClampsOutOfRangeValuesAndCachesProjection() {
            int[] source = { -1, 0, 42, ushort.MaxValue, ushort.MaxValue + 1 };
            var imageArray = new ImageArrayInt(source);

            ushort[] firstProjection = imageArray.FlatArray;
            source[2] = 99;
            ushort[] secondProjection = imageArray.FlatArray;

            firstProjection.Should().Equal(ushort.MaxValue, 0, 42, ushort.MaxValue, ushort.MaxValue);
            secondProjection.Should().BeSameAs(firstProjection);
            secondProjection[2].Should().Be(42);
            imageArray.FlatArrayInt.Should().BeSameAs(source);
        }

        /// <summary>
        /// Verifies stretch-map filter selection accepts supported pixel formats and rejects unsupported formats explicitly.
        /// </summary>
        [Test]
        public void ColorRemappingFilter_ValidatesPixelFormatsForLinkedAndUnlinkedStretch() {
            var dimStats = new TestImageStatistics(bitDepth: 16, median: 1000, medianAbsoluteDeviation: 50);
            var brightStats = new TestImageStatistics(bitDepth: 16, median: 50000, medianAbsoluteDeviation: 250);

            ImageUtility.GetColorRemappingFilter(dimStats, targetHistogramMeanPct: 0.25, shadowsClipping: -2.8, pf: PixelFormats.Gray16)
                .Should().NotBeNull();
            ImageUtility.GetColorRemappingFilter(brightStats, targetHistogramMeanPct: 0.25, shadowsClipping: -2.8, pf: PixelFormats.Rgb48)
                .Should().NotBeNull();
            ImageUtility.GetColorRemappingFilterUnlinked(dimStats, brightStats, dimStats, targetHistogramMeanPct: 0.25, shadowsClipping: -2.8, pf: PixelFormats.Rgb48)
                .Should().NotBeNull();

            Action unsupportedLinked = () => ImageUtility.GetColorRemappingFilter(dimStats, 0.25, -2.8, PixelFormats.Bgr24);
            Action unsupportedUnlinked = () => ImageUtility.GetColorRemappingFilterUnlinked(dimStats, brightStats, dimStats, 0.25, -2.8, PixelFormats.Gray16);

            unsupportedLinked.Should().Throw<NotSupportedException>();
            unsupportedUnlinked.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Verifies grayscale palette generation produces the canonical 0..255 ramp used for 8-bit analysis images.
        /// </summary>
        [Test]
        public void GetGrayScalePalette_ReturnsMonotonicGrayRamp() {
            ColorPalette palette = ImageUtility.GetGrayScalePalette();

            palette.Entries.Should().HaveCount(256);
            palette.Entries[0].R.Should().Be(0);
            palette.Entries[0].G.Should().Be(0);
            palette.Entries[0].B.Should().Be(0);
            palette.Entries[128].R.Should().Be(128);
            palette.Entries[128].G.Should().Be(128);
            palette.Entries[128].B.Should().Be(128);
            palette.Entries[255].R.Should().Be(255);
            palette.Entries[255].G.Should().Be(255);
            palette.Entries[255].B.Should().Be(255);
        }

        /// <summary>
        /// Verifies bitmap-source creation preserves dimensions, pixel format, pixel values, and freezes the WPF image for cross-thread use.
        /// </summary>
        [Test]
        public void CreateSourceFromArray_PreservesPixelsAndFreezesBitmapSource() {
            ushort[] pixels = { 1, 2, 3, 4, 5, 6 };
            var imageArray = new ImageArray(pixels);
            var properties = new ImageProperties(width: 3, height: 2, bitDepth: 16, isBayered: false, gain: 0, offset: 0);

            BitmapSource source = ImageUtility.CreateSourceFromArray(imageArray, properties, PixelFormats.Gray16);
            ushort[] roundTripped = new ushort[pixels.Length];
            source.CopyPixels(roundTripped, stride: 3 * sizeof(ushort), offset: 0);

            source.PixelWidth.Should().Be(3);
            source.PixelHeight.Should().Be(2);
            source.Format.Should().Be(PixelFormats.Gray16);
            source.IsFrozen.Should().BeTrue();
            roundTripped.Should().Equal(pixels);
        }

        /// <summary>
        /// Verifies debayer rejects unsupported source pixel formats before attempting Bayer interpolation.
        /// </summary>
        [Test]
        public void Debayer_RejectsUnsupportedBitmapPixelFormat() {
            using Bitmap bitmap = CreateGray8Bitmap(width: 4, height: 4, (x, y) => (byte)(x + y));
            BitmapSource source = ImageUtility.ConvertBitmap(bitmap, PixelFormats.Gray8);

            Action debayer = () => ImageUtility.Debayer(source, DrawingPixelFormat.Format24bppRgb, bayerPattern: SensorType.RGGB);

            debayer.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Verifies Gaussian blur preserves a constant 8-bit frame, including palette and dimensions.
        /// </summary>
        [Test]
        public void FastGaussianBlur_ProcessPreservesConstantFrame() {
            using Bitmap source = CreateGray8Bitmap(width: 9, height: 7, (_, _) => 123);
            var blur = new FastGaussianBlur(source);

            using Bitmap processed = blur.Process(radial: 2);

            processed.Width.Should().Be(source.Width);
            processed.Height.Should().Be(source.Height);
            processed.PixelFormat.Should().Be(DrawingPixelFormat.Format8bppIndexed);
            ReadGray8Pixels(processed).Should().OnlyContain(x => x == 123);
        }

        /// <summary>
        /// Verifies the no-blur Canny detector produces edge pixels for a synthetic step edge and keeps the unprocessed border black.
        /// </summary>
        [Test]
        public void NoBlurCannyEdgeDetector_DetectsSyntheticStepEdgeAndClearsBorder() {
            using Bitmap source = CreateGray8Bitmap(width: 16, height: 16, (x, _) => x < 8 ? (byte)0 : byte.MaxValue);
            var detector = new NoBlurCannyEdgeDetector(lowThreshold: 10, highThreshold: 20);

            using Bitmap edges = detector.Apply(source);
            byte[] pixels = ReadGray8Pixels(edges);

            edges.PixelFormat.Should().Be(DrawingPixelFormat.Format8bppIndexed);
            pixels.Should().Contain(x => x > 0);
            Enumerable.Range(0, edges.Width).Select(x => pixels[x]).Should().OnlyContain(x => x == 0);
            Enumerable.Range(0, edges.Width).Select(x => pixels[(edges.Height - 1) * edges.Width + x]).Should().OnlyContain(x => x == 0);
            Enumerable.Range(0, edges.Height).Select(y => pixels[y * edges.Width]).Should().OnlyContain(x => x == 0);
            Enumerable.Range(0, edges.Height).Select(y => pixels[y * edges.Width + edges.Width - 1]).Should().OnlyContain(x => x == 0);
        }

        /// <summary>
        /// Verifies Bahtinov analysis handles a blank grayscale frame deterministically and returns a frozen rendered image without false focus distance.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void BahtinovAnalysis_BlankGrayFrameReturnsImageWithoutFocusDistance() {
            using Bitmap bitmap = CreateGray8Bitmap(width: 32, height: 32, (_, _) => 0);
            BitmapSource source = ImageUtility.ConvertBitmap(bitmap, PixelFormats.Gray8);
            source.Freeze();
            var analysis = new BahtinovAnalysis(source, Colors.White);

            BahtinovImage result = analysis.GrabBahtinov();

            result.Image.Should().NotBeNull();
            result.Image.PixelWidth.Should().Be(32);
            result.Image.PixelHeight.Should().Be(32);
            result.Image.Format.Should().Be(PixelFormats.Bgr24);
            result.Image.IsFrozen.Should().BeTrue();
            result.Distance.Should().Be(0);
        }

        private static Bitmap CreateGray8Bitmap(int width, int height, Func<int, int, byte> pixelFactory) {
            Bitmap bitmap = new Bitmap(width, height, DrawingPixelFormat.Format8bppIndexed);
            bitmap.Palette = ImageUtility.GetGrayScalePalette();

            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, DrawingPixelFormat.Format8bppIndexed);

            try {
                int stride = Math.Abs(data.Stride);
                byte[] buffer = new byte[stride * height];

                for (int y = 0; y < height; y++) {
                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++) {
                        buffer[rowOffset + x] = pixelFactory(x, y);
                    }
                }

                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                return bitmap;
            } catch {
                bitmap.Dispose();
                throw;
            } finally {
                if (data != null) {
                    bitmap.UnlockBits(data);
                }
            }
        }

        private static byte[] ReadGray8Pixels(Bitmap bitmap) {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, DrawingPixelFormat.Format8bppIndexed);

            try {
                int stride = Math.Abs(data.Stride);
                byte[] buffer = new byte[stride * bitmap.Height];
                byte[] pixels = new byte[bitmap.Width * bitmap.Height];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                for (int y = 0; y < bitmap.Height; y++) {
                    Buffer.BlockCopy(buffer, y * stride, pixels, y * bitmap.Width, bitmap.Width);
                }

                return pixels;
            } finally {
                bitmap.UnlockBits(data);
            }
        }

        private class TestImageStatistics : IImageStatistics {
            public TestImageStatistics(int bitDepth, double median, double medianAbsoluteDeviation) {
                BitDepth = bitDepth;
                Median = median;
                MedianAbsoluteDeviation = medianAbsoluteDeviation;
            }

            public int BitDepth { get; }
            public double StDev => 0;
            public double Mean => Median;
            public double Median { get; }
            public double MedianAbsoluteDeviation { get; }
            public int Max => 0;
            public long MaxOccurrences => 0;
            public int Min => 0;
            public long MinOccurrences => 0;
            public ImmutableList<DataPoint> Histogram => ImmutableList<DataPoint>.Empty;
        }
    }
}
