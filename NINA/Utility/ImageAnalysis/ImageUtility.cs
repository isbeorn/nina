using AForge.Imaging;
using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility.ImageAnalysis {

    internal class ImageUtility {

        public static ColorRemappingGeneral GetColorRemappingFilter(IImageStatistics statistics, double targetHistogramMeanPct, double shadowsClipping, System.Windows.Media.PixelFormat pf) {
            ushort[] map = GetStretchMap(statistics, targetHistogramMeanPct, shadowsClipping);

            if (pf == PixelFormats.Gray16) {
                var filter = new ColorRemappingGeneral(map);
                return filter;
            } else if (pf == PixelFormats.Rgb48) {
                var filter = new ColorRemappingGeneral(map, map, map);
                return filter;
            } else {
                throw new NotSupportedException();
            }
        }

        public static ColorRemappingGeneral GetColorRemappingFilterUnlinked(IImageStatistics redStatistics, IImageStatistics greenStatistics, IImageStatistics blueStatistics, double targetHistogramMeanPct, double shadowsClipping, System.Windows.Media.PixelFormat pf) {
            ushort[] mapRed = GetStretchMap(redStatistics, targetHistogramMeanPct, shadowsClipping);
            ushort[] mapGreen = GetStretchMap(greenStatistics, targetHistogramMeanPct, shadowsClipping);
            ushort[] mapBlue = GetStretchMap(blueStatistics, targetHistogramMeanPct, shadowsClipping);
            if (pf == PixelFormats.Rgb48) {
                var filter = new ColorRemappingGeneral(mapRed, mapGreen, mapBlue);
                return filter;
            } else {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Adjusts x for a given midToneBalance
        /// </summary>
        /// <param name="midToneBalance"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static double MidtonesTransferFunction(double midToneBalance, double x) {
            if (x > 0) {
                if (x < 1) {
                    return (midToneBalance - 1) * x / ((2 * midToneBalance - 1) * x - midToneBalance);
                }
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Converts a value from range [0;65535] to [0;1]
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static double NormalizeUShort(double val) {
            return val / (double)ushort.MaxValue;
        }

        /// <summary>
        /// Converts a value from range [0;1] to [0;65535]
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static ushort DenormalizeUShort(double val) {
            return (ushort)(val * ushort.MaxValue);
        }

        private static ushort[] GetStretchMap(IImageStatistics statistics, double targetHistogramMedianPercent, double shadowsClipping) {
            ushort[] map = new ushort[ushort.MaxValue + 1];

            var normalizedMedian = NormalizeUShort(statistics.Median);
            var normalizedMAD = NormalizeUShort(statistics.MedianAbsoluteDeviation);

            var scaleFactor = 1.4826; // see https://en.wikipedia.org/wiki/Median_absolute_deviation
            var zero = normalizedMedian + shadowsClipping * normalizedMAD * scaleFactor;

            var mtf = MidtonesTransferFunction(targetHistogramMedianPercent, normalizedMedian - zero);

            for (int i = 0; i < map.Length; i++) {
                double value = NormalizeUShort(i);

                map[i] = DenormalizeUShort(MidtonesTransferFunction(mtf, value - zero));
            }

            return map;
        }

        public static BitmapSource ConvertBitmap(System.Drawing.Bitmap bitmap, System.Windows.Media.PixelFormat pf) {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, pf, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        public static Bitmap BitmapFromSource(BitmapSource source) {
            return BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
        }

        public static Bitmap BitmapFromSource(BitmapSource source, System.Drawing.Imaging.PixelFormat pf) {
            Bitmap bmp = new Bitmap(
                    source.PixelWidth,
                    source.PixelHeight,
                    pf);
            BitmapData data = bmp.LockBits(
                    new Rectangle(System.Drawing.Point.Empty, bmp.Size),
                    ImageLockMode.WriteOnly,
                    pf);
            source.CopyPixels(
                    Int32Rect.Empty,
                    data.Scan0,
                    data.Height * data.Stride,
                    data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public static Bitmap Convert16BppTo8Bpp(BitmapSource source) {
            using (var bmp = BitmapFromSource(source)) {
                return AForge.Imaging.Image.Convert16bppTo8bpp(bmp);
            }
        }

        public static BitmapSource Convert16BppTo8BppSource(BitmapSource source) {
            FormatConvertedBitmap s = new FormatConvertedBitmap();
            s.BeginInit();
            s.Source = source;
            s.DestinationFormat = System.Windows.Media.PixelFormats.Gray8;
            s.EndInit();
            s.Freeze();
            return s;
        }

        public static BitmapSource CreateSourceFromArray(ImageArray arr, System.Windows.Media.PixelFormat pf) {
            //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
            int stride = (arr.Statistics.Width * pf.BitsPerPixel + 7) / 8;
            double dpi = 96;

            BitmapSource source = BitmapSource.Create(arr.Statistics.Width, arr.Statistics.Height, dpi, dpi, pf, null, arr.FlatArray, stride);
            source.Freeze();
            return source;
        }

        public static BitmapSource Debayer(BitmapSource source, System.Drawing.Imaging.PixelFormat pf) {
            using (MyStopWatch.Measure()) {
                if (pf != System.Drawing.Imaging.PixelFormat.Format16bppGrayScale) {
                    throw new NotSupportedException();
                }
                using (var bmp = BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)) {
                    using (var debayeredBmp = Debayer(bmp)) {
                        var newSource = ConvertBitmap(debayeredBmp, PixelFormats.Rgb48);
                        newSource.Freeze();
                        return newSource;
                    }
                }
            }
        }

        public static unsafe RGBArrays ChannelsToFlatArrays(Bitmap image) {
            int stopX = image.Width;
            int stopY = image.Height;

            RGBArrays flatArrays = new RGBArrays(new ushort[stopX * stopY], new ushort[stopX * stopY], new ushort[stopX * stopY]);

            BitmapData imageData = image.LockBits(
                new Rectangle(0, 0, stopX, stopY),
                ImageLockMode.ReadWrite, image.PixelFormat);

            UnmanagedImage unmanagedImage = new UnmanagedImage(imageData);

            int i = 0;

            ushort* ptr = (ushort*)unmanagedImage.ImageData.ToPointer();

            for (int y = 0; y < stopY; y++) {
                for (int x = 0; x < stopX; x++, ptr += 3) {
                    flatArrays.redArray[i] = ptr[RGB.R];
                    flatArrays.greenArray[i] = ptr[RGB.G];
                    flatArrays.blueArray[i] = ptr[RGB.B];
                    i++;
                }
            }
            image.UnlockBits(imageData);
            return flatArrays;
        }

        public static Bitmap Debayer(Bitmap bmp) {
            using (MyStopWatch.Measure()) {
                var filter = new BayerFilter16bpp();
                filter.BayerPattern = new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } };
                var debayered = filter.Apply(bmp);
                return debayered;
            }
        }

        public static ColorPalette GetGrayScalePalette() {
            Bitmap bmp = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            ColorPalette monoPalette = bmp.Palette;

            System.Drawing.Color[] entries = monoPalette.Entries;

            for (int i = 0; i < 256; i++) {
                entries[i] = System.Drawing.Color.FromArgb(i, i, i);
            }

            return monoPalette;
        }

        public struct RGBArrays {
            public ushort[] redArray;
            public ushort[] greenArray;
            public ushort[] blueArray;

            public RGBArrays(ushort[] red, ushort[] green, ushort[] blue) {
                redArray = red;
                greenArray = green;
                blueArray = blue;
            }
        }

        public static async Task<BitmapSource> StretchAsync(ImageArray iarr, BitmapSource source, double factor, double blackClipping) {
            return await Task<BitmapSource>.Run(() => Stretch(iarr.Statistics, source, System.Windows.Media.PixelFormats.Gray16, factor, blackClipping));
        }

        public static async Task<BitmapSource> StretchAsync(ImageArray iarr, BitmapSource source, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            return await Task<BitmapSource>.Run(() => Stretch(iarr.Statistics, source, pf, factor, blackClipping));
        }

        public static async Task<BitmapSource> StretchAsyncUnlinked(ImageArray iarrRed, ImageArray iarrGreen, ImageArray iarrBlue, BitmapSource source, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            return await Task<BitmapSource>.Run(() => StretchUnlinked(iarrRed.Statistics, iarrGreen.Statistics, iarrBlue.Statistics, source, pf, factor, blackClipping));
        }

        public static async Task<BitmapSource> StretchAsync(IImageStatistics statistics, BitmapSource source, double factor, double blackClipping) {
            return await Task<BitmapSource>.Run(() => Stretch(statistics, source, System.Windows.Media.PixelFormats.Gray16, factor, blackClipping));
        }

        public static BitmapSource Stretch(IImageStatistics statistics, BitmapSource source, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            if (pf == System.Windows.Media.PixelFormats.Gray16) {
                using (var img = ImageUtility.BitmapFromSource(source)) {
                    return Stretch(statistics, img, pf, factor, blackClipping);
                }
            } else if (pf == System.Windows.Media.PixelFormats.Rgb48) {
                using (var img = ImageUtility.BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                    return Stretch(statistics, img, pf, factor, blackClipping);
                }
            } else {
                throw new NotSupportedException();
            }
        }

        public static BitmapSource StretchUnlinked(IImageStatistics redStatistics, IImageStatistics greenStatistics, IImageStatistics blueStatistics, BitmapSource source, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            if (pf != System.Windows.Media.PixelFormats.Rgb48) {
                throw new NotSupportedException();
            } else {
                using (var img = ImageUtility.BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                    return StretchUnlinked(redStatistics, greenStatistics, blueStatistics, img, pf, factor, blackClipping);
                }
            }
        }

        public static BitmapSource StretchUnlinked(IImageStatistics redStatistics, IImageStatistics greenStatistics, IImageStatistics blueStatistics, System.Drawing.Bitmap img, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            using (MyStopWatch.Measure()) {
                var filter = ImageUtility.GetColorRemappingFilterUnlinked(redStatistics, greenStatistics, blueStatistics, factor, blackClipping, pf);
                filter.ApplyInPlace(img);

                var source = ImageUtility.ConvertBitmap(img, pf);
                source.Freeze();
                return source;
            }
        }

        public static BitmapSource Stretch(IImageStatistics statistics, System.Drawing.Bitmap img, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            using (MyStopWatch.Measure()) {
                var filter = ImageUtility.GetColorRemappingFilter(statistics, factor, blackClipping, pf);
                filter.ApplyInPlace(img);

                var source = ImageUtility.ConvertBitmap(img, pf);
                source.Freeze();
                return source;
            }
        }
    }
}