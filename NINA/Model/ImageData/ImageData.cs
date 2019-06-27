using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.ImageAnalysis;
using NINA.Utility.RawConverter;
using nom.tam.fits;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.ImageData {

    public class ImageData : IImageData {

        public ImageData(ushort[] input, int width, int height, int bitDepth, bool isBayered) {
            var array = new ImageArray();
            array.FlatArray = input;
            this.Data = array;
            this.Statistics = new ImageStatistics(width, height, bitDepth, isBayered);
            this.MetaData = new ImageMetaData();
        }

        public BitmapSource Image { get; private set; }
        public IImageArray Data { get; private set; }
        public LRGBArrays DebayeredData { get; private set; }
        public IImageStatistics Statistics { get; set; }
        public ImageMetaData MetaData { get; set; }

        public static async Task<ImageData> Create(Array input, int bitDepth, bool isBayered) {
            if (input.Rank > 2) { throw new NotSupportedException(); }
            int width = input.GetLength(0);
            int height = input.GetLength(1);
            var flatArray = await Task.Run(() => FlipAndConvert2d(input));
            return new ImageData(flatArray, width, height, bitDepth, isBayered);
        }

        public Task CalculateStatistics() {
            return Statistics.Calculate(this.Data.FlatArray);
        }

        public void RenderImage() {
            Image = ImageUtility.CreateSourceFromArray(this.Data, this.Statistics, System.Windows.Media.PixelFormats.Gray16);
        }

        public void Debayer(bool saveColorChannels = false, bool saveLumChannel = false) {
            var debayeredImage = ImageUtility.Debayer(this.Image, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale, saveColorChannels, saveLumChannel);
            this.Image = debayeredImage.ImageSource;
            this.DebayeredData = debayeredImage.Data;
        }

        public async Task Stretch(double factor, double blackClipping, bool unlinked) {
            if (this.Statistics.IsBayered && unlinked) {
                this.Image = await ImageUtility.StretchUnlinked(this, factor, blackClipping);
            } else {
                this.Image = await ImageUtility.Stretch(this, factor, blackClipping);
            }
            if (DebayeredData != null) {
                //RGB arrays no longer needed - Dispose of them
                DebayeredData.Red = DebayeredData.Green = DebayeredData.Blue = null;
            }
        }

        public async Task DetectStars(bool annotate, StarSensitivityEnum sensitivity, NoiseReductionEnum noiseReduction, CancellationToken ct = default(CancellationToken), IProgress<ApplicationStatus> progress = default(Progress<ApplicationStatus>)) {
            var analysis = new StarDetection(this, this.Image.Format, sensitivity, noiseReduction);
            await analysis.DetectAsync(progress, ct);

            if (annotate) {
                this.Image = analysis.GetAnnotatedImage();
            }

            Statistics.HFR = analysis.AverageHFR;
            Statistics.DetectedStars = analysis.DetectedStars;
        }

        #region "Save"

        /// <summary>
        ///  Saves file to application temp path
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> PrepareSave(string filePath, FileTypeEnum fileType, CancellationToken token = default) {
            var actualPath = string.Empty;
            try {
                using (MyStopWatch.Measure()) {
                    actualPath = await SaveToDiskAsync(filePath, fileType, token);
                }
            } catch (OperationCanceledException ex) {
                throw ex;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            } finally {
            }
            return actualPath;
        }

        /// <summary>
        /// Renames and moves file to destination according to pattern
        /// </summary>
        /// <param name="file"></param>
        /// <param name="targetPath"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public string FinalizeSave(string file, string pattern) {
            try {
                var imagePatterns = GetImagePatterns();
                var fileName = imagePatterns.GetImageFileString(pattern);
                var extension = Path.GetExtension(file);
                var targetPath = Path.GetDirectoryName(file);
                var newFileName = Utility.Utility.GetUniqueFilePath(Path.Combine(targetPath, $"{fileName}.{extension}"));

                var fi = new FileInfo(newFileName);
                if (!fi.Directory.Exists) {
                    fi.Directory.Create();
                }

                File.Move(file, newFileName);
                return newFileName;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            } finally {
            }
        }

        private ImagePatterns GetImagePatterns() {
            var p = new ImagePatterns();
            p.Set(ImagePatternKeys.Filter, MetaData.FilterWheel.Filter);
            p.Set(ImagePatternKeys.ExposureTime, MetaData.Image.ExposureTime);
            p.Set(ImagePatternKeys.ApplicationStartDate, Utility.Utility.ApplicationStartDate.ToString("yyyy-MM-dd"));
            p.Set(ImagePatternKeys.Date, MetaData.Image.ExposureStart.ToString("yyyy-MM-dd"));
            p.Set(ImagePatternKeys.Time, MetaData.Image.ExposureStart.ToString("HH-mm-ss"));
            p.Set(ImagePatternKeys.DateTime, MetaData.Image.ExposureStart.ToString("yyyy-MM-dd_HH-mm-ss"));
            p.Set(ImagePatternKeys.FrameNr, MetaData.Image.ExposureNumber);
            p.Set(ImagePatternKeys.ImageType, MetaData.Image.ImageType);

            p.Set(ImagePatternKeys.TargetName, MetaData.Target.Name);

            if (MetaData.Image.RecordedRMS != null) {
                p.Set(ImagePatternKeys.RMS, MetaData.Image.RecordedRMS.Total);
                p.Set(ImagePatternKeys.RMSArcSec, MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale);
            }

            if (!double.IsNaN(MetaData.Focuser.Position)) {
                p.Set(ImagePatternKeys.FocuserPosition, MetaData.Focuser.Position);
            }

            if (!double.IsNaN(MetaData.Focuser.Temperature)) {
                p.Set(ImagePatternKeys.FocuserTemp, MetaData.Focuser.Temperature);
            }

            if (MetaData.Camera.Binning == string.Empty) {
                p.Set(ImagePatternKeys.Binning, "1x1");
            } else {
                p.Set(ImagePatternKeys.Binning, MetaData.Camera.Binning);
            }

            if (!double.IsNaN(MetaData.Camera.Temperature)) {
                p.Set(ImagePatternKeys.SensorTemp, MetaData.Camera.Temperature);
            }
            if (!double.IsNaN(MetaData.Camera.Gain)) {
                p.Set(ImagePatternKeys.Gain, MetaData.Camera.Gain);
            }
            if (!double.IsNaN(MetaData.Camera.Offset)) {
                p.Set(ImagePatternKeys.Offset, MetaData.Camera.Offset);
            }

            if (!double.IsNaN(Statistics.HFR)) {
                p.Set(ImagePatternKeys.HFR, Statistics.HFR);
            }
            return p;
        }

        public async Task<string> SaveToDisk(string path, string pattern, FileTypeEnum fileType, CancellationToken token) {
            var actualPath = string.Empty;
            try {
                using (MyStopWatch.Measure()) {
                    var tempPath = await SaveToDiskAsync(path, fileType, token);
                    actualPath = FinalizeSave(tempPath, pattern);
                }
            } catch (OperationCanceledException ex) {
                throw ex;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            } finally {
            }
            return actualPath;
        }

        private Task<string> SaveToDiskAsync(string path, FileTypeEnum fileType, CancellationToken token) {
            return Task.Run(() => {
                var completefilename = Path.Combine(path, Guid.NewGuid().ToString());
                if (this.Data.RAWData != null) {
                    completefilename = SaveRAW(completefilename);
                    fileType = FileTypeEnum.RAW;
                } else {
                    if (fileType == FileTypeEnum.FITS) {
                        completefilename = SaveFits(completefilename);
                    } else if (fileType == FileTypeEnum.TIFF) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.None);
                    } else if (fileType == FileTypeEnum.TIFF_ZIP) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.Zip);
                    } else if (fileType == FileTypeEnum.TIFF_LZW) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.Lzw);
                    } else if (fileType == FileTypeEnum.XISF) {
                        completefilename = SaveXisf(completefilename);
                    } else {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.None);
                    }
                }
                return completefilename;
            }, token);
        }

        private string SaveRAW(string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var uniquePath = Utility.Utility.GetUniqueFilePath(path + "." + Data.RAWType);
            File.WriteAllBytes(uniquePath, Data.RAWData);
            return uniquePath;
        }

        private string SaveTiff(string path, TiffCompressOption c) {
            BitmapSource bmpSource = ImageUtility.CreateSourceFromArray(Data, Statistics, System.Windows.Media.PixelFormats.Gray16);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var uniquePath = Utility.Utility.GetUniqueFilePath(path + ".tif");

            using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Compression = c;
                encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                encoder.Save(fs);
            }
            return uniquePath;
        }

        private string SaveFits(string path) {
            FITS f = new FITS(
                Data.FlatArray,
                Statistics.Width,
                Statistics.Height
            );

            f.PopulateHeaderCards(MetaData);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var uniquePath = Utility.Utility.GetUniqueFilePath(path + ".fits");

            using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                f.Write(fs);
            }

            return uniquePath;
        }

        private string SaveXisf(string path) {
            var header = new XISFHeader();

            header.AddImageMetaData(Statistics, MetaData.Image.ImageType);

            header.Populate(MetaData);

            XISF img = new XISF(header);

            img.AddAttachedImage(Data.FlatArray);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var uniquePath = Utility.Utility.GetUniqueFilePath(path + ".xisf");

            using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                img.Save(fs);
            }

            return uniquePath;
        }

        #endregion "Save"

        #region "Load"

        /// <summary>
        /// Loads an image from a given file path
        /// </summary>
        /// <param name="path">File Path to image</param>
        /// <param name="bitDepth">bit depth of each pixel</param>
        /// <param name="isBayered">Flag to indicate if the image is bayer matrix encoded</param>
        /// <param name="rawConverter">Which type of raw converter to use, when image is in RAW format</param>
        /// <param name="ct">Token to cancel operation</param>
        /// <returns></returns>
        public static async Task<IImageData> FromFile(string path, int bitDepth, bool isBayered, RawConverterEnum rawConverter, CancellationToken ct = default(CancellationToken)) {
            if (!File.Exists(path)) {
                throw new FileNotFoundException();
            }
            BitmapDecoder decoder;
            switch (Path.GetExtension(path).ToLower()) {
                case ".gif":
                    decoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    return await BitmapToImageArray(decoder, isBayered);

                case ".tif":
                case ".tiff":
                    decoder = new TiffBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    return await BitmapToImageArray(decoder, isBayered);

                case ".jpg":
                case ".jpeg":
                    decoder = new JpegBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    return await BitmapToImageArray(decoder, isBayered);

                case ".png":
                    decoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    return await BitmapToImageArray(decoder, isBayered);

                case ".xisf":
                    return await XISF.Load(new Uri(path), isBayered);

                case ".fit":
                case ".fits":
                    return await FitsToImageArray(path, bitDepth, isBayered);

                case ".cr2":
                case ".nef":
                case ".raf":
                case ".raw":
                case ".pef":
                case ".dng":
                    return await RawToImageArray(path, bitDepth, rawConverter, ct);

                default:
                    throw new NotSupportedException();
            }
        }

        private static Task<IImageData> FitsToImageArray(string path, int bitDepth, bool isBayered) {
            return Task.Run(async () => {
                Fits f = new Fits(path);
                ImageHDU hdu = (ImageHDU)f.ReadHDU();
                Array[] arr = (Array[])hdu.Data.DataArray;

                var width = hdu.Header.GetIntValue("NAXIS1");
                var height = hdu.Header.GetIntValue("NAXIS2");
                ushort[] pixels = new ushort[width * height];
                var i = 0;
                foreach (var row in arr) {
                    foreach (short val in row) {
                        pixels[i++] = (ushort)(val + short.MaxValue);
                    }
                }
                var data = await Task.FromResult<IImageData>(new ImageData(pixels, width, height, bitDepth, isBayered));
                await data.CalculateStatistics();
                return data;
            });
        }

        private static async Task<IImageData> RawToImageArray(string path, int bitDepth, RawConverterEnum rawConverter, CancellationToken ct) {
            using (var fs = new FileStream(path, FileMode.Open)) {
                using (var ms = new System.IO.MemoryStream()) {
                    fs.CopyTo(ms);
                    var converter = RawConverter.CreateInstance(rawConverter);
                    var data = await converter.Convert(ms, bitDepth, ct);
                    data.Data.RAWType = Path.GetExtension(path).ToLower().Substring(1);
                    await data.CalculateStatistics();
                    return data;
                }
            }
        }

        private static async Task<IImageData> BitmapToImageArray(BitmapDecoder decoder, bool isBayered) {
            var bmp = new FormatConvertedBitmap();
            bmp.BeginInit();
            bmp.Source = decoder.Frames[0];
            bmp.DestinationFormat = System.Windows.Media.PixelFormats.Gray16;
            bmp.EndInit();

            int stride = (bmp.PixelWidth * bmp.Format.BitsPerPixel + 7) / 8;
            int arraySize = stride * bmp.PixelHeight;
            ushort[] pixels = new ushort[bmp.PixelWidth * bmp.PixelHeight];
            bmp.CopyPixels(pixels, stride, 0);
            var data = new ImageData(pixels, bmp.PixelWidth, bmp.PixelHeight, 16, isBayered);
            await data.CalculateStatistics();
            return data;
        }

        #endregion "Load"

        private static ushort[] FlipAndConvert2d(Array input) {
            using (MyStopWatch.Measure("FlipAndConvert2d")) {
                Int32[,] arr = (Int32[,])input;
                int width = arr.GetLength(0);
                int height = arr.GetLength(1);

                int length = width * height;
                ushort[] flatArray = new ushort[length];
                ushort value;

                unsafe {
                    fixed (Int32* ptr = arr) {
                        int idx = 0, row = 0;
                        for (int i = 0; i < length; i++) {
                            value = (ushort)ptr[i];

                            idx = ((i % height) * width) + row;
                            if ((i % (height)) == (height - 1)) row++;

                            ushort b = value;
                            flatArray[idx] = b;
                        }
                    }
                }
                return flatArray;
            }
        }
    }
}