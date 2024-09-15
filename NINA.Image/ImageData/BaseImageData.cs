#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.FileFormat;
using NINA.Image.FileFormat.FITS;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Image.RawConverter;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Image.ImageData {

    public partial class BaseImageData : IImageData {
        protected readonly IProfileService profileService;
        protected readonly IStarDetection starDetection;
        protected readonly IStarAnnotator starAnnotator;

        public BaseImageData(ushort[] input, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData, IProfileService profileService, IStarDetection starDetection, IStarAnnotator starAnnotator)
            : this(
                  imageArray: new ImageArray(flatArray: input),
                  width: width,
                  height: height,
                  bitDepth: bitDepth,
                  isBayered: isBayered,
                  metaData: metaData,
                  profileService: profileService,
                  starDetection: starDetection,
                  starAnnotator: starAnnotator) {
        }

        public BaseImageData(IImageArray imageArray, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData, IProfileService profileService, IStarDetection starDetection, IStarAnnotator starAnnotator) {
            Data = imageArray;
            MetaData = metaData;
            Properties = new ImageProperties(width: width, height: height, bitDepth: bitDepth, isBayered: isBayered, gain: metaData.Camera.Gain, offset: metaData.Camera.Offset);
            StarDetectionAnalysis = starDetection.CreateAnalysis();
            Statistics = new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => await Task.Run(() => ImageStatistics.Create(this)));
            this.profileService = profileService;
            this.starDetection = starDetection;
            this.starAnnotator = starAnnotator;
        }

        public IImageArray Data { get; private set; }

        public ImageProperties Properties { get; private set; }

        public ImageMetaData MetaData { get; private set; }

        public Nito.AsyncEx.AsyncLazy<IImageStatistics> Statistics { get; private set; }

        public IStarDetectionAnalysis StarDetectionAnalysis { get; set; }

        public IRenderedImage RenderImage() {
            return RenderedImage.Create(RenderBitmapSource(), this, profileService, starDetection, starAnnotator);
        }

        public BitmapSource RenderBitmapSource() {
            return ImageUtility.CreateSourceFromArray(Data, Properties, PixelFormats.Gray16);
        }

        public void SetImageStatistics(IImageStatistics imageStatistics) {
            Statistics = new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(imageStatistics));
        }

        #region "Save"

        [Obsolete]
        /// <summary>
        ///  Saves file to application temp path
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> PrepareSave(FileSaveInfo fileSaveInfo, CancellationToken cancelToken = default) {
            var actualPath = string.Empty;
            try {
                using (MyStopWatch.Measure()) {
                    // Reference: https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
                    var cancelTaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    using (cancelToken.Register(() => cancelTaskSource.SetCanceled())) {
                        var saveTask = SaveToDiskAsync(fileSaveInfo, Guid.NewGuid().ToString(), cancelToken, false);
                        await Task.WhenAny(cancelTaskSource.Task, saveTask);
                        cancelToken.ThrowIfCancellationRequested();
                        actualPath = saveTask.Result;
                    }

                    Logger.Debug($"Saved temporary image at {actualPath}");
                }
            } catch (OperationCanceledException) {
                throw;
            } catch (AggregateException ae) {
                Logger.Error(ae);
                throw ae.InnerException;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            } finally {
            }
            return actualPath;
        }

        [Obsolete]
        /// <summary>
        /// Renames and moves file to destination according to pattern
        /// </summary>
        /// <param name="file"></param>
        /// <param name="pattern"></param>
        /// <param name="customPatterns"></param>
        /// <returns></returns>        
        public string FinalizeSave(string file, string pattern, IList<ImagePattern> customPatterns) {
            try {
                if (pattern.Contains(ImagePatternKeys.SensorTemp) && double.IsNaN(MetaData.Camera.Temperature) && !string.IsNullOrEmpty(Data.RAWType)) {
                    string sensorTemp = GetSensorTempFromExifTool(file);
                    pattern = pattern.Replace(ImagePatternKeys.SensorTemp, sensorTemp);
                }

                var imagePatterns = GetImagePatterns();
                foreach (var cp in customPatterns) {
                    imagePatterns.Add(cp);
                }

                var fileName = imagePatterns.GetImageFileString(pattern);
                var extension = GetFileExtensionsRegex().Match(file).Value;
                var targetPath = Path.GetDirectoryName(file);
                var newFileName = CoreUtil.GetUniqueFilePath(Path.Combine(targetPath, $"{fileName}{extension}"));

                var fi = new FileInfo(newFileName);
                if (!fi.Directory.Exists) {
                    fi.Directory.Create();
                }

                var fileinfo = new FileInfo(file);

                Logger.Info($"Finalize image and moving it to {newFileName}");
                fileinfo.MoveTo(newFileName);

                return newFileName;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            } finally {
            }
        }

        [GeneratedRegex(@"(?:(?:\.\w+)?\.\w+$)")]
        private static partial Regex GetFileExtensionsRegex();

        private string GetSensorTempFromExifTool(string file) {
            string tempString = string.Empty;
            try {
                string EXIFTOOLLOCATION = Path.Combine(CoreUtil.APPLICATIONDIRECTORY, "Utility", "ExifTool", "exiftool.exe");
                var sb = new StringBuilder();

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = EXIFTOOLLOCATION;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.CreateNoWindow = true;
                startInfo.Arguments = $"-CameraTemperature \"{file}\"";
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;

                process.OutputDataReceived += (sender, e) => {
                    sb.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) => {
                    sb.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                Logger.Trace(sb.ToString());

                // remove whitespace and format
                tempString = sb.ToString().Replace(" ", "");
                tempString = tempString.Substring(tempString.IndexOf(':') + 1).ToLower().Trim();

                if (!Regex.IsMatch(tempString, "^[0-9]{1,4}[cCfFkK]$")) {
                    Logger.Error($"Value returned by EXIF Tool is no valid temperature: {tempString}");
                    tempString = string.Empty;
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            return tempString;
        }

        public ImagePatterns GetImagePatterns() {
            var p = new ImagePatterns();
            var metadata = MetaData;
            p.Set(ImagePatternKeys.Filter, metadata.FilterWheel.Filter);
            p.Set(ImagePatternKeys.ExposureTime, metadata.Image.ExposureTime);
            p.Set(ImagePatternKeys.ApplicationStartDate, CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd"));
            p.Set(ImagePatternKeys.Date, metadata.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd"));

            // ExposureStart is initialized to DateTime.MinValue, and we cannot subtract time from that. Only evaluate
            // the $$DATEMINUS12$$ pattern if the time is at least 12 hours on from DateTime.MinValue.
            if (metadata.Image.ExposureStart > DateTime.MinValue.AddHours(12)) {
                p.Set(ImagePatternKeys.DateMinus12, metadata.Image.ExposureStart.ToLocalTime().AddHours(-12).ToString("yyyy-MM-dd"));
            }

            p.Set(ImagePatternKeys.DateUtc, metadata.Image.ExposureStart.ToUniversalTime().ToString("yyyy-MM-dd"));
            p.Set(ImagePatternKeys.Time, metadata.Image.ExposureStart.ToLocalTime().ToString("HH-mm-ss"));
            p.Set(ImagePatternKeys.TimeUtc, metadata.Image.ExposureStart.ToUniversalTime().ToString("HH-mm-ss"));
            p.Set(ImagePatternKeys.DateTime, metadata.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss"));
            p.Set(ImagePatternKeys.FrameNr, metadata.Image.ExposureNumber.ToString("0000"));
            p.Set(ImagePatternKeys.ImageType, metadata.Image.ImageType);
            p.Set(ImagePatternKeys.TargetName, metadata.Target.Name);

            if (metadata.Image.RecordedRMS != null) {
                p.Set(ImagePatternKeys.RMS, metadata.Image.RecordedRMS.Total);
                p.Set(ImagePatternKeys.RMSArcSec, metadata.Image.RecordedRMS.Total * metadata.Image.RecordedRMS.Scale);
                p.Set(ImagePatternKeys.PeakRA, metadata.Image.RecordedRMS.PeakRA);
                p.Set(ImagePatternKeys.PeakRAArcSec, metadata.Image.RecordedRMS.PeakRA * metadata.Image.RecordedRMS.Scale);
                p.Set(ImagePatternKeys.PeakDec, metadata.Image.RecordedRMS.PeakDec);
                p.Set(ImagePatternKeys.PeakDecArcSec, metadata.Image.RecordedRMS.PeakDec * metadata.Image.RecordedRMS.Scale);
            }

            if (metadata.Focuser.Position.HasValue) {
                p.Set(ImagePatternKeys.FocuserPosition, metadata.Focuser.Position.Value);
            }

            if (!double.IsNaN(metadata.Focuser.Temperature)) {
                p.Set(ImagePatternKeys.FocuserTemp, metadata.Focuser.Temperature);
            }

            if (metadata.Camera.Binning == string.Empty) {
                p.Set(ImagePatternKeys.Binning, "1x1");
            } else {
                p.Set(ImagePatternKeys.Binning, metadata.Camera.Binning);
            }

            if (!double.IsNaN(metadata.Camera.Temperature)) {
                p.Set(ImagePatternKeys.SensorTemp, metadata.Camera.Temperature);
            }

            if (!double.IsNaN(metadata.Camera.SetPoint)) {
                p.Set(ImagePatternKeys.TemperatureSetPoint, metadata.Camera.SetPoint);
            }

            if (metadata.Camera.Gain >= 0) {
                p.Set(ImagePatternKeys.Gain, metadata.Camera.Gain);
            }

            if (metadata.Camera.Offset >= 0) {
                p.Set(ImagePatternKeys.Offset, metadata.Camera.Offset);
            }

            if (metadata.Camera.USBLimit >= 0) {
                p.Set(ImagePatternKeys.USBLimit, metadata.Camera.USBLimit);
            }

            if (!double.IsNaN(StarDetectionAnalysis.HFR)) {
                p.Set(ImagePatternKeys.HFR, StarDetectionAnalysis.HFR);
            }

            if (!double.IsNaN(metadata.WeatherData.SkyQuality)) {
                p.Set(ImagePatternKeys.SQM, metadata.WeatherData.SkyQuality);
            }

            if (!string.IsNullOrEmpty(metadata.Camera.ReadoutModeName)) {
                p.Set(ImagePatternKeys.ReadoutMode, metadata.Camera.ReadoutModeName);
            }

            if (!string.IsNullOrEmpty(metadata.Camera.Name)) {
                p.Set(ImagePatternKeys.Camera, metadata.Camera.Name);
            }

            if (!string.IsNullOrEmpty(metadata.Telescope.Name)) {
                p.Set(ImagePatternKeys.Telescope, metadata.Telescope.Name);
            }

            if (!double.IsNaN(metadata.Rotator.MechanicalPosition)) {
                p.Set(ImagePatternKeys.RotatorAngle, metadata.Rotator.MechanicalPosition);
            }

            if (StarDetectionAnalysis.DetectedStars >= 0) {
                p.Set(ImagePatternKeys.StarCount, StarDetectionAnalysis.DetectedStars);
            }

            p.Set(ImagePatternKeys.SequenceTitle, metadata.Sequence.Title);

            return p;
        }

        
        public async Task<string> SaveToDisk(FileSaveInfo fileSaveInfo, CancellationToken token, bool forceFileType, IList<ImagePattern> customPatterns) {
            if(customPatterns == null) { customPatterns = new List<ImagePattern>(); }
            var pattern = fileSaveInfo.FilePattern;
            string actualPath = string.Empty;
            try {
                if (pattern.Contains(ImagePatternKeys.SensorTemp) && double.IsNaN(MetaData.Camera.Temperature) && !string.IsNullOrEmpty(Data.RAWType)) {
                    // For DSLRs we need to retrieve the temperature after the file is written. Hence we replace the pattern with this special placeholder
                    pattern = pattern.Replace(ImagePatternKeys.SensorTemp, "$$DSLR_SENSORTEMP$$");
                }

                var imagePatterns = GetImagePatterns();
                foreach (var cp in customPatterns) {
                    imagePatterns.Add(cp);
                }
                var fileName = imagePatterns.GetImageFileString(pattern);

                actualPath = await SaveToDiskAsync(fileSaveInfo, fileName, token, forceFileType);

                if (pattern.Contains("$$DSLR_SENSORTEMP$$") && double.IsNaN(MetaData.Camera.Temperature) && !string.IsNullOrEmpty(Data.RAWType)) {
                    // Extract the temperature from the EXIF info for DSLRs
                    actualPath = ExtractDSLRTemperatureAndMoveFile(actualPath);
                }
                Logger.Info($"Saved image to {actualPath}");
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            } 
            return actualPath;
        }
        
        private string ExtractDSLRTemperatureAndMoveFile(string actualPath) {
            var oldPath = actualPath;
            try {
                string sensorTemp = GetSensorTempFromExifTool(actualPath);
                actualPath = actualPath.Replace("$$DSLR_SENSORTEMP$$", sensorTemp.ToString(CultureInfo.InvariantCulture));

                // Create a folder in case this pattern was set up to be a folder
                var fi = new FileInfo(actualPath);
                if (!fi.Directory.Exists) {
                    fi.Directory.Create();
                }

                File.Move(oldPath, actualPath);
            } catch (Exception ex) {
                // In case of any error point to the original file
                actualPath = oldPath;
                Logger.Error(ex);

            }
            return actualPath;
        }
        
        
        public Task<string> SaveToDisk(FileSaveInfo fileSaveInfo, CancellationToken token, bool forceFileType = false) {
            return SaveToDisk(fileSaveInfo, token, forceFileType, new List<ImagePattern>());
        }

        private Task<string> SaveToDiskAsync(FileSaveInfo fileSaveInfo, string fileName, CancellationToken cancelToken, bool forceFileType = false) {
            return Task.Run(() => {
                string path = string.Empty;
                fileSaveInfo.FilePath = Path.Combine(fileSaveInfo.FilePath, fileName);

                if (!forceFileType && Data.RAWData != null) {
                    fileSaveInfo.FileType = FileTypeEnum.RAW;
                    path = SaveRAW(fileSaveInfo.FilePath);
                } else {
                    switch (fileSaveInfo.FileType) {
                        case FileTypeEnum.FITS:
                            path = SaveFits(fileSaveInfo);
                            break;

                        case FileTypeEnum.XISF:
                            path = SaveXisf(fileSaveInfo);
                            break;

                        case FileTypeEnum.TIFF:
                        default:
                            path = SaveTiff(fileSaveInfo);
                            break;
                    }
                }

                return path;
            }, cancelToken);
        }

        private string SaveRAW(string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            IImageArray data = Data;
            string uniquePath = CoreUtil.GetUniqueFilePath(path + "." + data.RAWType);
            File.WriteAllBytes(uniquePath, data.RAWData);
            return uniquePath;
        }

        private string SaveTiff(FileSaveInfo fileSaveInfo) {
            Directory.CreateDirectory(Path.GetDirectoryName(fileSaveInfo.FilePath));
            string uniquePath = CoreUtil.GetUniqueFilePath(fileSaveInfo.FilePath + fileSaveInfo.GetExtension(".tif"));

            using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();

                switch (fileSaveInfo.TIFFCompressionType) {
                    case TIFFCompressionTypeEnum.LZW:
                        encoder.Compression = TiffCompressOption.Lzw;
                        break;

                    case TIFFCompressionTypeEnum.ZIP:
                        encoder.Compression = TiffCompressOption.Zip;
                        break;

                    default:
                        encoder.Compression = TiffCompressOption.None;
                        break;
                }

                var metadata = new BitmapMetadata("tiff");
                try {
                    // This will populate the TIFF Metadata using stringified FITS style header cards, to retain partial meta data that is not natively supported by TIFF                    
                    metadata.ApplicationName = CoreUtil.Title;

                    var fitsHeader = new FITSHeader(Properties.Width, Properties.Height);
                    fitsHeader.PopulateFromMetaData(MetaData);
                    var sb = new StringBuilder();
                    foreach (var header in fitsHeader.HeaderCards) {
                        sb.AppendLine(header.GetHeaderString());
                    }
                    sb.AppendLine("END");
                    metadata.Title = sb.ToString();
                } catch (Exception ex) {
                    Logger.Error("Failed to generate TIFF metadata", ex);
                }

                var frame = BitmapFrame.Create(RenderBitmapSource(), null, metadata, null);
                encoder.Frames.Add(frame);
                encoder.Save(fs);
            }

            return uniquePath;
        }

        private static CfitsioNative.COMPRESSION GetFITSCompression(FITSCompressionTypeEnum fITSCompressionTypeEnum) {
            return fITSCompressionTypeEnum switch {
                FITSCompressionTypeEnum.NONE => CfitsioNative.COMPRESSION.NOCOMPRESS,
                FITSCompressionTypeEnum.RICE => CfitsioNative.COMPRESSION.RICE_1,
                FITSCompressionTypeEnum.PLIO => CfitsioNative.COMPRESSION.PLIO_1,
                FITSCompressionTypeEnum.HCOMPRESS => CfitsioNative.COMPRESSION.HCOMPRESS_1,
                FITSCompressionTypeEnum.GZIP1 => CfitsioNative.COMPRESSION.GZIP_1,
                FITSCompressionTypeEnum.GZIP2 => CfitsioNative.COMPRESSION.GZIP_2,
                _ => CfitsioNative.COMPRESSION.NOCOMPRESS,
            };
        }

        private string SaveFits(FileSaveInfo fileSaveInfo) {
            string extension = ".fits";
            Directory.CreateDirectory(Path.GetDirectoryName(fileSaveInfo.FilePath));

            if(fileSaveInfo.FITSUseLegacyWriter) {
                var uniquePath = CoreUtil.GetUniqueFilePath(fileSaveInfo.FilePath + fileSaveInfo.GetExtension(extension));
                FITS f = new FITS(
                    Data.FlatArray,
                    Properties.Width,
                    Properties.Height
                );

                f.PopulateHeaderCards(MetaData);

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    f.Write(fs);
                }
                return uniquePath;
            } else {
                if (fileSaveInfo.FITSAddFzExtension && fileSaveInfo.FITSCompressionType != FITSCompressionTypeEnum.NONE) {
                    extension += ".fz";
                }

                // CFitsio treats paranthesis for special logic and are thus not allowed
                fileSaveInfo.FilePath = fileSaveInfo.FilePath.Replace("(", "_").Replace(")", "_").Replace("[", "_").Replace("]", "_");
                var uniquePath = CoreUtil.GetUniqueFilePath(fileSaveInfo.FilePath + fileSaveInfo.GetExtension(extension), "{0}_{1}");
                
                var compression = GetFITSCompression(fileSaveInfo.FITSCompressionType);

                CFitsioFITS f = null;
                try {
                    if (Data.FlatArrayInt != null) {
                        f = new CFitsioFITS(uniquePath, Data.FlatArrayInt, Properties.Width, Properties.Height, compression);
                    } else {
                        f = new CFitsioFITS(uniquePath, Data.FlatArray, Properties.Width, Properties.Height, compression);
                    }
                    f.PopulateHeaderCards(MetaData);
                } finally {
                    f?.Close();
                }
                return uniquePath;
            }
        }

        private string SaveXisf(FileSaveInfo fileSaveInfo) {
            XISFHeader header = new XISFHeader();

            var sampleFormat = Data.FlatArrayInt != null ? XISFSampleFormat.UInt32 : XISFSampleFormat.UInt16;
            header.AddImageMetaData(Properties, MetaData.Image.ImageType, sampleFormat);

            header.Populate(MetaData);

            XISF img = new XISF(header);

            if (Data.FlatArrayInt != null) {
                img.AddAttachedImageInt(Data.FlatArrayInt, fileSaveInfo);
            } else {
                img.AddAttachedImage(Data.FlatArray, fileSaveInfo);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fileSaveInfo.FilePath));
            string uniquePath = CoreUtil.GetUniqueFilePath(fileSaveInfo.FilePath + fileSaveInfo.GetExtension(".xisf"));

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
        public static Task<IImageData> FromFile(string path, int bitDepth, bool isBayered, IRawConverter rawConverter, IImageDataFactory imageDataFactory, CancellationToken ct = default) {
            return Task.Run(async () => {
                if (!File.Exists(path)) {
                    throw new FileNotFoundException();
                }
                BitmapDecoder decoder;
                switch (Path.GetExtension(path).ToLower()) {
                    case ".gif":
                        decoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        return BitmapToImageArray(decoder, isBayered, imageDataFactory);

                    case ".tif":
                    case ".tiff":
                        decoder = new TiffBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        return BitmapToImageArray(decoder, isBayered, imageDataFactory);

                    case ".jpg":
                    case ".jpeg":
                        decoder = new JpegBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        return BitmapToImageArray(decoder, isBayered, imageDataFactory);

                    case ".png":
                        decoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        return BitmapToImageArray(decoder, isBayered, imageDataFactory);

                    case ".xisf":
                        return await XISF.Load(new Uri(path), isBayered, imageDataFactory, ct);

                    case ".fit":
                    case ".fits":
                    case ".fts":
                    case ".fz":
                        return await FITS.Load(new Uri(path), isBayered, imageDataFactory, ct);

                    case ".cr2":
                    case ".cr3":
                    case ".nef":
                    case ".raf":
                    case ".raw":
                    case ".pef":
                    case ".dng":
                    case ".arw":
                    case ".orf":
                        return await RawToImageArray(path, bitDepth, rawConverter, ct);

                    default:
                        throw new NotSupportedException();
                }
            }, ct);
        }

        public static bool FileIsSupported(string path) {
            if (!File.Exists(path)) {
                throw new FileNotFoundException();
            }

            var supportedExtensions = new Regex(@".*\.(gif|tiff?|jpe?g|[dp]ng|xisf|fits?|cr[23]|nef|ra[fw]|pef|arw|orf)", RegexOptions.IgnoreCase);
            return supportedExtensions.IsMatch(path);
        }

        private static async Task<IImageData> RawToImageArray(string path, int bitDepth, IRawConverter rawConverter, CancellationToken ct) {
            using (var fs = new FileStream(path, FileMode.Open)) {
                using (var ms = new MemoryStream()) {
                    await fs.CopyToAsync(ms);
                    var rawType = Path.GetExtension(path).ToLower().Substring(1);
                    var data = await rawConverter.Convert(s: ms, bitDepth: bitDepth, rawType: rawType, metaData: new ImageMetaData(), token: ct);
                    return data;
                }
            }
        }

        private static IImageData BitmapToImageArray(BitmapDecoder decoder, bool isBayered, IImageDataFactory imageDataFactory) {
            var bmp = new FormatConvertedBitmap();
            bmp.BeginInit();
            bmp.Source = decoder.Frames[0];
            bmp.DestinationFormat = PixelFormats.Gray16;
            bmp.EndInit();

            var metaData = new ImageMetaData();
            if (decoder.Frames[0].Metadata is BitmapMetadata bmpMd) {
                try {
                    if (!string.IsNullOrWhiteSpace(bmpMd.Title)) {
                        /* Parse potential FITS header on a best guess base by checking for a start of "SIMPLE" and stop at "END" or no more lines 
                         * Anything that would break the parse will just result in a failed meta data read and empty meta data is used instead.
                         */
                        if (bmpMd.Title.StartsWith("SIMPLE")) {
                            var fitsHeader = new FITSHeader(bmp.PixelWidth, bmp.PixelHeight);
                            // Assume FITS style meta data is available
                            using (StringReader reader = new StringReader(bmpMd.Title)) {
                                string line = string.Empty;
                                do {
                                    line = reader.ReadLine();
                                    if (line == null) { continue; }
                                    if (line == "END") { break; }

                                    // do something with the line
                                    var indexSlash = line.IndexOf('/');

                                    var key = line.Substring(0, 8).Trim();

                                    var value = string.Empty;
                                    if (indexSlash > 0) {
                                        value = line.Substring(9, indexSlash - 10).Trim();
                                    } else {
                                        value = line.Substring(9, 80 - 9).Trim();
                                    }

                                    var comment = string.Empty;
                                    if (indexSlash > 0) {
                                        comment = line.Substring(indexSlash + 1, line.Length - indexSlash - 1).Trim();
                                    }


                                    if (value.Contains(".")) {
                                        if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue)) {
                                            fitsHeader.Add(key, parsedValue, comment);
                                        }
                                    } else {
                                        if (int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue)) {
                                            fitsHeader.Add(key, parsedValue, comment);
                                        }
                                    }


                                } while (line != null);

                                try {
                                    metaData = fitsHeader.ExtractMetaData();
                                } catch (Exception ex) {
                                    Logger.Error(ex.Message);
                                }
                            }
                        }
                    }

                } catch (Exception ex) {
                    Logger.Error("Failed to parse FITS like metadata from TIFF", ex);
                }
            }

            var stride = (bmp.PixelWidth * bmp.Format.BitsPerPixel + 7) / 8;
            var pixels = new ushort[bmp.PixelWidth * bmp.PixelHeight];
            bmp.CopyPixels(pixels, stride, 0);
            return imageDataFactory.CreateBaseImageData(pixels, bmp.PixelWidth, bmp.PixelHeight, 16, isBayered, metaData);
        }

        #endregion "Load"
    }

    public class ImageDataFactory : IImageDataFactory {
        protected readonly IProfileService profileService;
        protected readonly IPluggableBehaviorSelector<IStarDetection> starDetectionSelector;
        protected readonly IPluggableBehaviorSelector<IStarAnnotator> starAnnotatorSelector;

        public ImageDataFactory(IProfileService profileService, IPluggableBehaviorSelector<IStarDetection> starDetectionSelector, IPluggableBehaviorSelector<IStarAnnotator> starAnnotatorSelector) {
            this.profileService = profileService;
            this.starDetectionSelector = starDetectionSelector;
            this.starAnnotatorSelector = starAnnotatorSelector;
        }

        public BaseImageData CreateBaseImageData(ushort[] input, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) {
            return new BaseImageData(input, width, height, bitDepth, isBayered, metaData, this.profileService, this.starDetectionSelector.GetBehavior(), this.starAnnotatorSelector.GetBehavior());
        }

        public BaseImageData CreateBaseImageData(IImageArray imageArray, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) {
            return new BaseImageData(imageArray, width, height, bitDepth, isBayered, metaData, this.profileService, this.starDetectionSelector.GetBehavior(), this.starAnnotatorSelector.GetBehavior());
        }

        public Task<IImageData> CreateFromFile(string path, int bitDepth, bool isBayered, RawConverterEnum rawConverter, CancellationToken ct = default) {
            return BaseImageData.FromFile(path, bitDepth, isBayered, RawConverterFactory.CreateInstance(rawConverter, this), this, ct);
        }
    }
}