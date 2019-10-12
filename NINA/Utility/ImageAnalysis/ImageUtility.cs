using AForge.Imaging;
using Microsoft.Win32.SafeHandles;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
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

        public static BitmapSource CreateSourceFromArray(IImageArray arr, IImageStatistics statistics, System.Windows.Media.PixelFormat pf) {
            //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
            int stride = (statistics.Width * pf.BitsPerPixel + 7) / 8;
            double dpi = 96;

            BitmapSource source = BitmapSource.Create(statistics.Width, statistics.Height, dpi, dpi, pf, null, arr.FlatArray, stride);
            source.Freeze();
            return source;
        }

        public static DebayeredImageData Debayer(BitmapSource source, System.Drawing.Imaging.PixelFormat pf, bool saveColorChannels = false, bool saveLumChannel = false) {
            using (MyStopWatch.Measure()) {
                if (pf != System.Drawing.Imaging.PixelFormat.Format16bppGrayScale) {
                    throw new NotSupportedException();
                }
                using (var bmp = BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)) {
                    return Debayer(bmp, saveColorChannels, saveLumChannel);
                }
            }
        }

        public static DebayeredImageData Debayer(Bitmap bmp, bool saveColorChannels = false, bool saveLumChannel = false) {
            using (MyStopWatch.Measure()) {
                var filter = new BayerFilter16bpp();
                filter.SaveColorChannels = saveColorChannels;
                filter.SaveLumChannel = saveLumChannel;
                filter.BayerPattern = new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } };
                DebayeredImageData debayered = new DebayeredImageData();
                debayered.ImageSource = ConvertBitmap(filter.Apply(bmp), PixelFormats.Rgb48);
                debayered.ImageSource.Freeze();
                debayered.Data = filter.LRGBArrays;
                return debayered;
            }
        }

        public static ColorPalette GetGrayScalePalette() {
            using (var bmp = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format8bppIndexed)) {
                ColorPalette monoPalette = bmp.Palette;

                System.Drawing.Color[] entries = monoPalette.Entries;

                for (int i = 0; i < 256; i++) {
                    entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }

                return monoPalette;
            }
        }

        public static Task<BitmapSource> Stretch(ImageData data, double factor, double blackClipping) {
            return Task.Run(() => {
                if (data.Image.Format == System.Windows.Media.PixelFormats.Gray16) {
                    using (var img = ImageUtility.BitmapFromSource(data.Image, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)) {
                        return Stretch(data.Statistics, img, data.Image.Format, factor, blackClipping);
                    }
                } else if (data.Image.Format == System.Windows.Media.PixelFormats.Rgb48) {
                    using (var img = ImageUtility.BitmapFromSource(data.Image, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                        return Stretch(data.Statistics, img, data.Image.Format, factor, blackClipping);
                    }
                } else {
                    throw new NotSupportedException();
                }
            });
        }

        public static Task<BitmapSource> StretchUnlinked(ImageData data, double factor, double blackClipping) {
            return Task.Run(async () => {
                if (data.Image.Format != System.Windows.Media.PixelFormats.Rgb48) {
                    throw new NotSupportedException();
                } else {
                    var r = new Model.ImageData.ImageStatistics(data.Image.PixelWidth, data.Image.PixelHeight, data.Statistics.BitDepth, false);
                    var g = new Model.ImageData.ImageStatistics(data.Image.PixelWidth, data.Image.PixelHeight, data.Statistics.BitDepth, false);
                    var b = new Model.ImageData.ImageStatistics(data.Image.PixelWidth, data.Image.PixelHeight, data.Statistics.BitDepth, false);
                    await Task.WhenAll(r.Calculate(data.DebayeredData.Red), g.Calculate(data.DebayeredData.Green), b.Calculate(data.DebayeredData.Blue));
                    using (var img = ImageUtility.BitmapFromSource(data.Image, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                        return StretchUnlinked(r, g, b, img, data.Image.Format, factor, blackClipping);
                    }
                }
            });
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