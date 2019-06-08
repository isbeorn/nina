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

        public void Debayer() {
            this.Image = ImageUtility.Debayer(this.Image, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
        }

        public async Task Stretch(double factor, double blackClipping, bool unlinked) {
            if (this.Statistics.IsBayered && unlinked) {
                this.Image = await ImageUtility.StretchUnlinked(this, factor, blackClipping);
            } else {
                this.Image = await ImageUtility.Stretch(this, factor, blackClipping);
            }
        }

        public async Task DetectStars(bool annotate, CancellationToken ct = default(CancellationToken), IProgress<ApplicationStatus> progress = default(Progress<ApplicationStatus>)) {
            var analysis = new StarDetection(this, this.Image.Format);
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
                        if (MetaData.Image.ImageType == "SNAP") MetaData.Image.ImageType = "LIGHT";
                        completefilename = SaveFits(completefilename);
                    } else if (fileType == FileTypeEnum.TIFF) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.None);
                    } else if (fileType == FileTypeEnum.TIFF_ZIP) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.Zip);
                    } else if (fileType == FileTypeEnum.TIFF_LZW) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.Lzw);
                    } else if (fileType == FileTypeEnum.XISF) {
                        if (MetaData.Image.ImageType == "SNAP") MetaData.Image.ImageType = "LIGHT";
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
                Statistics.Height,
                MetaData.Image.ImageType,
                MetaData.Image.ExposureTime
            );

            /* Camera */
            if (MetaData.Camera.BinX > 0) {
                f.AddHeaderCard("XBINNING", MetaData.Camera.BinX, "X axis binning factor");
            }
            if (MetaData.Camera.BinY > 0) {
                f.AddHeaderCard("YBINNING", MetaData.Camera.BinY, "Y axis binning factor");
            }

            f.AddHeaderCard("GAIN", MetaData.Camera.Gain, "Sensor gain");

            if (MetaData.Camera.Offset >= 0) {
                f.AddHeaderCard("OFFSET", MetaData.Camera.Offset, "Sensor gain offset");
            }

            if (!double.IsNaN(MetaData.Camera.ElectronsPerADU)) {
                f.AddHeaderCard("EGAIN", MetaData.Camera.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit");
            }

            if (!double.IsNaN(MetaData.Camera.PixelSize)) {
                f.AddHeaderCard("XPIXSZ", MetaData.Camera.PixelSize, "[um] Pixel X axis size");
                f.AddHeaderCard("YPIXSZ", MetaData.Camera.PixelSize, "[um] Pixel Y axis size");
            }

            if (!string.IsNullOrEmpty(MetaData.Camera.Name)) {
                f.AddHeaderCard("INSTRUME", MetaData.Camera.Name, "Imaging instrument name");
            }

            if (!double.IsNaN(MetaData.Camera.SetPoint)) {
                f.AddHeaderCard("SET-TEMP", MetaData.Camera.SetPoint, "[C] CCD temperature setpoint");
            }

            if (!double.IsNaN(MetaData.Camera.Temperature)) {
                f.AddHeaderCard("CCD-TEMP", MetaData.Camera.Temperature, "[C] CCD temperature");
            }

            /* Telescope */
            if (!string.IsNullOrWhiteSpace(MetaData.Telescope.Name)) {
                f.AddHeaderCard("TELESCOP", MetaData.Telescope.Name, "Name of telescope");
            }

            if (!double.IsNaN(MetaData.Telescope.FocalLength)) {
                f.AddHeaderCard("FOCALLEN", MetaData.Telescope.FocalLength, "[mm] Focal length");
            }

            if (!double.IsNaN(MetaData.Telescope.FocalRatio) && MetaData.Telescope.FocalRatio > 0) {
                f.AddHeaderCard("FOCRATIO", MetaData.Telescope.FocalRatio, "Focal ratio");
            }

            if (MetaData.Telescope.Coordinates != null) {
                f.AddHeaderCard("RA", MetaData.Telescope.Coordinates.RADegrees, "[deg] RA of telescope");
                f.AddHeaderCard("DEC", MetaData.Telescope.Coordinates.Dec, "[deg] Declination of telescope");
            }

            /* Observer */
            if (!double.IsNaN(MetaData.Observer.Elevation)) {
                f.AddHeaderCard("SITEELEV", MetaData.Observer.Elevation, "[m] Observation site elevation");
            }
            if (!double.IsNaN(MetaData.Observer.Elevation)) {
                f.AddHeaderCard("SITELAT", MetaData.Observer.Latitude, "[deg] Observation site latitude");
            }
            if (!double.IsNaN(MetaData.Observer.Elevation)) {
                f.AddHeaderCard("SITELONG", MetaData.Observer.Longitude, "[deg] Observation site longitude");
            }

            /* Filter Wheel */

            if (!string.IsNullOrWhiteSpace(MetaData.FilterWheel.Name)) {
                /* fits4win */
                f.AddHeaderCard("FWHEEL", MetaData.FilterWheel.Name, "Filter Wheel name");
            }

            if (!string.IsNullOrWhiteSpace(MetaData.FilterWheel.Filter)) {
                /* fits4win */
                f.AddHeaderCard("FILTER", MetaData.FilterWheel.Filter, "Active filter name");
            }

            /* Target */

            if (!string.IsNullOrWhiteSpace(MetaData.Target.Name)) {
                f.AddHeaderCard("OBJECT", MetaData.Target.Name, "Name of the object of interest");
            }

            if (MetaData.Target.Coordinates != null) {
                f.AddHeaderCard("OBJCTRA", MetaData.Target.Coordinates.RAString, "[H M S] RA of imaged object");
                f.AddHeaderCard("OBJCTDEC", MetaData.Target.Coordinates.DecString, "[D M S] Declination of imaged object");
            }

            /* Focuser */

            if (!string.IsNullOrWhiteSpace(MetaData.Focuser.Name)) {
                /* fits4win, SGP */
                f.AddHeaderCard("FOCNAME", MetaData.Focuser.Name, "Focusing equipment name");
            }

            if (!double.IsNaN(MetaData.Focuser.Position)) {
                /* fits4win, SGP */
                f.AddHeaderCard("FOCPOS", MetaData.Focuser.Position, "[step] Focuser position");

                /* MaximDL, several observatories */
                f.AddHeaderCard("FOCUSPOS", MetaData.Focuser.Position, "[step] Focuser position");
            }

            if (!double.IsNaN(MetaData.Focuser.StepSize)) {
                /* MaximDL */
                f.AddHeaderCard("FOCUSSZ", MetaData.Focuser.StepSize, "[um] Focuser step size");
            }

            if (!double.IsNaN(MetaData.Focuser.Temperature)) {
                /* fits4win, SGP */
                f.AddHeaderCard("FOCTEMP", MetaData.Focuser.Temperature, "[C] Focuser temperature");

                /* MaximDL, several observatories */
                f.AddHeaderCard("FOCUSTEM", MetaData.Focuser.Temperature, "[C] Focuser temperature");
            }

            /* Rotator */
            if (!string.IsNullOrEmpty(MetaData.Rotator.Name)) {
                /* NINA */
                f.AddHeaderCard("ROTNAME", MetaData.Rotator.Name, "Rotator equipment name");
            }

            if (!double.IsNaN(MetaData.Rotator.Position)) {
                /* fits4win */
                f.AddHeaderCard("ROTATOR", MetaData.Rotator.Position, "[deg] Rotator angle");

                /* MaximDL, several observatories */
                f.AddHeaderCard("ROTATANG", MetaData.Rotator.Position, "[deg] Rotator angle");
            }

            if (!double.IsNaN(MetaData.Rotator.StepSize)) {
                /* NINA */
                f.AddHeaderCard("ROTSTPSZ", MetaData.Rotator.StepSize, "[deg] Rotator step size");
            }

            f.AddHeaderCard("SWCREATE", string.Format("N.I.N.A. {0} ({1})", Utility.Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var uniquePath = Utility.Utility.GetUniqueFilePath(path + ".fits");

            using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                f.Write(fs);
            }

            return uniquePath;
        }

        private string SaveXisf(string path) {
            var header = new XISFHeader();
            DateTime now = DateTime.Now;

            header.AddImageMetaData(Statistics, MetaData.Image.ImageType);

            header.AddImageProperty(XISFImageProperty.Observation.Time.Start, now.ToUniversalTime().ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture), "Time of observation (UTC)");
            header.AddImageFITSKeyword("DATE-LOC", now.ToLocalTime().ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture), "Time of observation (local)");

            /* Camera */
            if (!string.IsNullOrWhiteSpace(MetaData.Camera.Name)) {
                header.AddImageProperty(XISFImageProperty.Instrument.Camera.Name, MetaData.Camera.Name, "Imaging instrument name");
            }
            if (!double.IsNaN(MetaData.Camera.Gain) && MetaData.Camera.Gain >= 0) {
                header.AddImageFITSKeyword("GAIN", MetaData.Camera.Gain.ToString(CultureInfo.InvariantCulture), "Sensor gain");
            }

            if (!double.IsNaN(MetaData.Camera.Offset) && MetaData.Camera.Offset >= 0) {
                header.AddImageFITSKeyword("OFFSET", MetaData.Camera.Offset.ToString(CultureInfo.InvariantCulture), "Sensor gain offset");
            }

            if (!double.IsNaN(MetaData.Camera.ElectronsPerADU)) {
                header.AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, MetaData.Camera.ElectronsPerADU.ToString(CultureInfo.InvariantCulture), "[e-/ADU] Electrons per A/D unit");
            }

            if (MetaData.Camera.BinX > 0) {
                header.AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, MetaData.Camera.BinX.ToString(CultureInfo.InvariantCulture), "X axis binning factor");
            }
            if (MetaData.Camera.BinY > 0) {
                header.AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, MetaData.Camera.BinY.ToString(CultureInfo.InvariantCulture), "Y axis binning factor");
            }

            if (!double.IsNaN(MetaData.Camera.SetPoint)) {
                header.AddImageFITSKeyword("SET-TEMP", MetaData.Camera.SetPoint.ToString(CultureInfo.InvariantCulture), "[C] CCD temperature setpoint");
            }

            if (!double.IsNaN(MetaData.Camera.Temperature)) {
                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, MetaData.Camera.Temperature.ToString(CultureInfo.InvariantCulture), "[C] CCD temperature");
            }
            if (!double.IsNaN(MetaData.Camera.PixelSize)) {
                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, MetaData.Camera.PixelSize.ToString(CultureInfo.InvariantCulture), "[um] Pixel X axis size");
                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, MetaData.Camera.PixelSize.ToString(CultureInfo.InvariantCulture), "[um] Pixel Y axis size");
            }

            /* Observer */
            if (!double.IsNaN(MetaData.Observer.Elevation)) {
                header.AddImageProperty(XISFImageProperty.Observation.Location.Elevation, MetaData.Observer.Elevation.ToString(CultureInfo.InvariantCulture), "[m] Observation site elevation");
            }
            if (!double.IsNaN(MetaData.Observer.Elevation)) {
                header.AddImageProperty(XISFImageProperty.Observation.Location.Latitude, MetaData.Observer.Latitude.ToString(CultureInfo.InvariantCulture), "[deg] Observation site latitude");
            }
            if (!double.IsNaN(MetaData.Observer.Elevation)) {
                header.AddImageProperty(XISFImageProperty.Observation.Location.Longitude, MetaData.Observer.Longitude.ToString(CultureInfo.InvariantCulture), "[deg] Observation site longitude");
            }

            /* Telescope */
            if (!string.IsNullOrWhiteSpace(MetaData.Telescope.Name)) {
                header.AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, MetaData.Telescope.Name.ToString(CultureInfo.InvariantCulture), "Name of telescope");
            }
            if (!double.IsNaN(MetaData.Telescope.FocalLength)) {
                header.AddImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, MetaData.Telescope.FocalLength.ToString(CultureInfo.InvariantCulture), "[mm] Focal length");
            }

            if (!double.IsNaN(MetaData.Telescope.FocalRatio) && MetaData.Telescope.FocalRatio > 0) {
                header.AddImageFITSKeyword("FOCRATIO", MetaData.Telescope.FocalRatio.ToString(CultureInfo.InvariantCulture), "Focal ratio");
            }

            if (!string.IsNullOrWhiteSpace(MetaData.Target.Name)) {
                header.AddImageProperty(XISFImageProperty.Observation.Object.Name, MetaData.Target.Name, "Name of the object of interest");
            }

            if (MetaData.Telescope.Coordinates != null) {
                header.AddImageProperty(XISFImageProperty.Observation.Center.RA, MetaData.Telescope.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.RA[2], MetaData.Telescope.Coordinates.RAString, "[H M S] RA of imaged object");
                header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.RA[3], MetaData.Telescope.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), "[deg] RA of telescope");

                header.AddImageProperty(XISFImageProperty.Observation.Center.Dec, MetaData.Telescope.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.Dec[2], MetaData.Telescope.Coordinates.DecString, "[D M S] Declination of imaged object");
                header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.Dec[3], MetaData.Telescope.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), "[deg] Declination of telescope");
            }

            /* Focuser */

            if (!string.IsNullOrWhiteSpace(MetaData.Focuser.Name)) {
                /* fits4win, SGP */
                header.AddImageFITSKeyword("FOCNAME", MetaData.Focuser.Name, "Focusing equipment name");
            }

            /*
             * XISF 1.0 defines Instrument:Focuser:Position as the only focuser-related image property.
             * This image property is: "(Float32) Estimated position of the focuser in millimetres, measured with respect to a device-dependent origin."
             * This unit is different from FOCUSPOS FITSKeyword, so we must do two separate actions: calculate distance from origin in millimetres and insert
             * that as the XISF Instrument:Focuser:Position property, and then insert the separate FOCUSPOS FITSKeyword (measured in steps).
             */
            if (!double.IsNaN(MetaData.Focuser.Position)) {
                if (!double.IsNaN(MetaData.Focuser.StepSize)) {
                    /* steps * step size (microns) converted to millimetres, single-precision float */
                    float focusDistance = (float)((MetaData.Focuser.Position * MetaData.Focuser.StepSize) / 1000.0);
                    header.AddImageProperty(XISFImageProperty.Instrument.Focuser.Position, focusDistance.ToString(CultureInfo.InvariantCulture));
                }

                /* fits4win, SGP */
                header.AddImageFITSKeyword("FOCPOS", MetaData.Focuser.Position.ToString(CultureInfo.InvariantCulture), "[step] Focuser position");

                /* MaximDL, several observatories */
                header.AddImageFITSKeyword("FOCUSPOS", MetaData.Focuser.Position.ToString(CultureInfo.InvariantCulture), "[step] Focuser position");
            }

            if (!double.IsNaN(MetaData.Focuser.StepSize)) {
                /* MaximDL */
                header.AddImageFITSKeyword("FOCUSSZ", MetaData.Focuser.StepSize.ToString(CultureInfo.InvariantCulture), "[um] Focuser step size");
            }

            if (!double.IsNaN(MetaData.Focuser.Temperature)) {
                /* fits4win, SGP */
                header.AddImageFITSKeyword("FOCTEMP", MetaData.Focuser.Temperature.ToString(CultureInfo.InvariantCulture), "[C] Focuser temperature");

                /* MaximDL, several observatories */
                header.AddImageFITSKeyword("FOCUSTEM", MetaData.Focuser.Temperature.ToString(CultureInfo.InvariantCulture), "[C] Focuser temperature");
            }

            /* Rotator */
            if (!string.IsNullOrWhiteSpace(MetaData.Rotator.Name)) {
                /* NINA */
                header.AddImageFITSKeyword("ROTNAME", MetaData.Rotator.Name, "Rotator equipment name");
            }

            if (!double.IsNaN(MetaData.Rotator.Position)) {
                /* fits4win */
                header.AddImageFITSKeyword("ROTATOR", MetaData.Rotator.Position.ToString(CultureInfo.InvariantCulture), "[deg] Rotator angle");

                /* MaximDL, several observatories */
                header.AddImageFITSKeyword("ROTATANG", MetaData.Rotator.Position.ToString(CultureInfo.InvariantCulture), "[deg] Rotator angle");
            }

            if (!double.IsNaN(MetaData.Rotator.StepSize)) {
                /* NINA */
                header.AddImageFITSKeyword("ROTSTPSZ", MetaData.Rotator.StepSize.ToString(CultureInfo.InvariantCulture), "[deg] Rotator step size");
            }

            if (!string.IsNullOrWhiteSpace(MetaData.FilterWheel.Name)) {
                /* fits4win */
                header.AddImageFITSKeyword("FWHEEL", MetaData.FilterWheel.Name, "Filter Wheel name");

                header.AddImageProperty(XISFImageProperty.Instrument.Filter.Name, MetaData.FilterWheel.Name, "Active filter name");
            }

            header.AddImageProperty(XISFImageProperty.Instrument.ExposureTime, MetaData.Image.ExposureTime.ToString(System.Globalization.CultureInfo.InvariantCulture), "[s] Exposure duration");

            header.AddImageFITSKeyword("SWCREATE", string.Format("N.I.N.A. {0} ({1})", Utility.Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");

            XISF img = new XISF(header);

            img.AddAttachedImage(Data.FlatArray, MetaData.Image.ImageType);

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