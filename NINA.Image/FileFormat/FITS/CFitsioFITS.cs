using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static NINA.Image.FileFormat.FITS.CfitsioNative;
using NINA.Image.ImageData;
using System.Globalization;
using NINA.Core.Enum;
using NINA.Astrometry;
using NINA.Core.Utility;

namespace NINA.Image.FileFormat.FITS {
    public class CFitsioFITS {
        private nint filePtr;

        private CFitsioFITS(string filePath, COMPRESSION compression) {
            CfitsioNative.fits_create_file(out var ptr, filePath, out var status);
            CheckStatus("fits_create_file", status);
            this.filePtr = ptr;

            if (compression != COMPRESSION.NOCOMPRESS) {
                CfitsioNative.fits_set_compression_type(ptr, compression, out status);
                CheckStatus("fits_set_compression_type", status);
            }
        }

        public CFitsioFITS(string filePath, ushort[] data, int width, int height, COMPRESSION compression = COMPRESSION.NOCOMPRESS) : this(filePath, compression) {
            CfitsioNative.fits_create_img(filePtr, CfitsioNative.BITPIX.USHORT_IMG, 2, new int[] { width, height }, out var status);
            CheckStatus("fits_create_img", status);
            CfitsioNative.fits_write_img(filePtr, CfitsioNative.DATATYPE.TUSHORT, 1, width * height, data, out status);
            CheckStatus("fits_write_img", status);
        }

        public CFitsioFITS(string filePath, uint[] data, int width, int height, COMPRESSION compression = COMPRESSION.NOCOMPRESS) : this(filePath, compression) {
            CfitsioNative.fits_create_img(filePtr, CfitsioNative.BITPIX.ULONG_IMG, 2, new int[] { width, height }, out var status);
            CheckStatus("fits_create_img", status);
            CfitsioNative.fits_write_img_uint(filePtr, CfitsioNative.DATATYPE.TUINT, 1, width * height, data, out status);
            CheckStatus("fits_write_img", status);
        }

        public CFitsioFITS(string filePath, int[] data, int width, int height, COMPRESSION compression = COMPRESSION.NOCOMPRESS) : this(filePath, compression) {
            CfitsioNative.fits_create_img(filePtr, CfitsioNative.BITPIX.LONG_IMG, 2, new int[] { width, height }, out var status);
            CheckStatus("fits_create_img", status);
            CfitsioNative.fits_write_img_int(filePtr, CfitsioNative.DATATYPE.TINT, 1, width * height, data, out status);
            CheckStatus("fits_write_img", status);
        }

        public CFitsioFITS(string filePath, float[] data, int width, int height, COMPRESSION compression = COMPRESSION.NOCOMPRESS) : this(filePath, compression) {
            CfitsioNative.fits_create_img(filePtr, CfitsioNative.BITPIX.FLOAT_IMG, 2, new int[] { width, height }, out var status);
            CheckStatus("fits_create_img", status);
            CfitsioNative.fits_write_img_float(filePtr, CfitsioNative.DATATYPE.TDOUBLE, 1, width * height, data, out status);
            CheckStatus("fits_write_img", status);
        }

        public CFitsioFITS(string filePath, double[] data, int width, int height, COMPRESSION compression = COMPRESSION.NOCOMPRESS) : this(filePath, compression) {
            CfitsioNative.fits_create_img(filePtr, CfitsioNative.BITPIX.DOUBLE_IMG, 2, new int[] { width, height }, out var status);
            CheckStatus("fits_create_img", status);
            CfitsioNative.fits_write_img_double(filePtr, CfitsioNative.DATATYPE.TDOUBLE, 1, width * height, data, out status);
            CheckStatus("fits_write_img", status);
        }

        public void AddHeader(string keyword, string value, string comment) {
            CfitsioNative.fits_update_key_str(filePtr, keyword, value, comment, out var status);
            LogErrorStatus("fits_update_key_str", status);
        }

        public void AddHeader(string keyword, int value, string comment) {
            CfitsioNative.fits_update_key_lng(filePtr, keyword, value, comment, out var status);
            LogErrorStatus("fits_update_key_lng", status);
        }

        public void AddHeader(string keyword, double value, string comment) {
            CfitsioNative.fits_update_key_dbl(filePtr, keyword, ref value, comment, out var status);
            LogErrorStatus("fits_update_key_dbl", status);
        }

        public void AddHeader(string keyword, DateTime value, string comment) {
            AddHeader(keyword, value.ToString(@"yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture), comment);
        }

        public void Close() {
            int status;

            CfitsioNative.fits_write_chksum(filePtr, out status);
            LogErrorStatus("fits_write_chksum", status);

            CfitsioNative.fits_close_file(filePtr, out status);
            CheckStatus("fits_close_file", status);
        }

        public void PopulateHeaderCards(ImageMetaData metaData) {
            if (!string.IsNullOrWhiteSpace(metaData.Image.ImageType)) {
                var imageType = metaData.Image.ImageType;
                if (imageType == "SNAPSHOT") { imageType = "LIGHT"; }
                AddHeader("IMAGETYP", imageType, "Type of exposure");
            }

            if (!double.IsNaN(metaData.Image.ExposureTime)) {
                AddHeader("EXPOSURE", metaData.Image.ExposureTime, "[s] Exposure duration");
                AddHeader("EXPTIME", metaData.Image.ExposureTime, "[s] Exposure duration");
            }

            if (metaData.Image.ExposureStart > DateTime.MinValue) {
                AddHeader("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime(), "Time of observation (local)");
                AddHeader("DATE-OBS", metaData.Image.ExposureStart.ToUniversalTime(), "Time of observation (UTC)");
            }

            if (metaData.Image.ExposureMidPoint > DateTime.MinValue) {
                // Section 8.4.1 https://fits.gsfc.nasa.gov/standard40/fits_standard40aa-le.pdf
                // Calendar date of the midpoint of the observation, expressed in the same way as the DATE-OBS keyword.
                AddHeader("DATE-AVG", metaData.Image.ExposureMidPoint, "Averaged midpoint time (UTC)");
            }

            /* Camera */
            if (metaData.Camera.BinX > 0) {
                AddHeader("XBINNING", metaData.Camera.BinX, "X axis binning factor");
            }

            if (metaData.Camera.BinY > 0) {
                AddHeader("YBINNING", metaData.Camera.BinY, "Y axis binning factor");
            }

            if (metaData.Camera.Gain >= 0) {
                AddHeader("GAIN", metaData.Camera.Gain, "Sensor gain");
            }

            if (metaData.Camera.Offset >= 0) {
                AddHeader("OFFSET", metaData.Camera.Offset, "Sensor gain offset");
            }

            if (!double.IsNaN(metaData.Camera.ElectronsPerADU)) {
                AddHeader("EGAIN", metaData.Camera.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit");
            }

            if (!double.IsNaN(metaData.Camera.PixelSize)) {
                double pixelX = metaData.Camera.PixelSize * Math.Max(metaData.Camera.BinX, 1);
                double pixelY = metaData.Camera.PixelSize * Math.Max(metaData.Camera.BinY, 1);
                AddHeader("XPIXSZ", pixelX, "[um] Pixel X axis size");
                AddHeader("YPIXSZ", pixelY, "[um] Pixel Y axis size");
            }

            if (!string.IsNullOrEmpty(metaData.Camera.Name)) {
                AddHeader("INSTRUME", metaData.Camera.Name, "Imaging instrument name");
            }

            if (!string.IsNullOrEmpty(metaData.Camera.Id)) {
                AddHeader("CAMERAID", metaData.Camera.Id, "Imaging instrument identifier");
            }

            if (!double.IsNaN(metaData.Camera.SetPoint)) {
                AddHeader("SET-TEMP", metaData.Camera.SetPoint, "[degC] CCD temperature setpoint");
            }

            if (!double.IsNaN(metaData.Camera.Temperature)) {
                AddHeader("CCD-TEMP", metaData.Camera.Temperature, "[degC] CCD temperature");
            }

            if (!string.IsNullOrWhiteSpace(metaData.Camera.ReadoutModeName)) {
                AddHeader("READOUTM", metaData.Camera.ReadoutModeName, "Sensor readout mode");
            }

            if (metaData.Camera.SensorType != SensorType.Monochrome && metaData.Camera.BayerPattern != BayerPatternEnum.None) {
                AddHeader("BAYERPAT", metaData.Camera.SensorType.ToString().ToUpper(), "Sensor Bayer pattern");
                AddHeader("XBAYROFF", metaData.Camera.BayerOffsetX, "Bayer pattern X axis offset");
                AddHeader("YBAYROFF", metaData.Camera.BayerOffsetY, "Bayer pattern Y axis offset");
            }

            if (metaData.Camera.USBLimit > -1) {
                AddHeader("USBLIMIT", metaData.Camera.USBLimit, "Camera-specific USB setting");
            }

            /* Telescope */
            if (!string.IsNullOrWhiteSpace(metaData.Telescope.Name)) {
                AddHeader("TELESCOP", metaData.Telescope.Name, "Name of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.FocalLength) && metaData.Telescope.FocalLength > 0) {
                AddHeader("FOCALLEN", metaData.Telescope.FocalLength, "[mm] Focal length");
            }

            if (!double.IsNaN(metaData.Telescope.FocalRatio) && metaData.Telescope.FocalRatio > 0) {
                AddHeader("FOCRATIO", metaData.Telescope.FocalRatio, "Focal ratio");
            }

            if (metaData.Telescope.Coordinates != null) {
                AddHeader("RA", metaData.Telescope.Coordinates.RADegrees, "[deg] RA of telescope");
                AddHeader("DEC", metaData.Telescope.Coordinates.Dec, "[deg] Declination of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Altitude)) {
                AddHeader("CENTALT", metaData.Telescope.Altitude, "[deg] Altitude of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Azimuth)) {
                AddHeader("CENTAZ", metaData.Telescope.Azimuth, "[deg] Azimuth of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Airmass)) {
                AddHeader("AIRMASS", metaData.Telescope.Airmass, "Airmass at frame center (Gueymard 1993)");
            }

            if (metaData.Telescope.SideOfPier != PierSide.pierUnknown) {
                string keyword = "PIERSIDE";
                string comment = "Telescope pointing state";

                if (metaData.Telescope.SideOfPier == PierSide.pierEast) {
                    AddHeader(keyword, "East", comment);
                } else if (metaData.Telescope.SideOfPier == PierSide.pierWest) {
                    AddHeader(keyword, "West", comment);
                }
            }

            /* Observer */
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                AddHeader("SITEELEV", metaData.Observer.Elevation, "[m] Observation site elevation");
            }
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                AddHeader("SITELAT", metaData.Observer.Latitude, "[deg] Observation site latitude");
            }
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                AddHeader("SITELONG", metaData.Observer.Longitude, "[deg] Observation site longitude");
            }

            /* Filter Wheel */
            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Name)) {
                /* fits4win */
                AddHeader("FWHEEL", metaData.FilterWheel.Name, "Filter Wheel name");
            }

            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Filter)) {
                /* fits4win */
                AddHeader("FILTER", metaData.FilterWheel.Filter, "Active filter name");
            }

            /* Target */
            if (!string.IsNullOrWhiteSpace(metaData.Target.Name)) {
                AddHeader("OBJECT", metaData.Target.Name, "Name of the object of interest");
            }

            if (metaData.Target.Coordinates != null) {
                AddHeader("OBJCTRA", AstroUtil.HoursToFitsHMS(metaData.Target.Coordinates.RA), "[H M S] RA of imaged object");
                AddHeader("OBJCTDEC", AstroUtil.DegreesToFitsDMS(metaData.Target.Coordinates.Dec), "[D M S] Declination of imaged object");
            }

            if (!double.IsNaN(metaData.Target.PositionAngle)) {
                /* NINA Specific target rotation */
                AddHeader("OBJCTROT", metaData.Target.PositionAngle, "[deg] planned rotation of imaged object");
            }

            /* Focuser */
            if (!string.IsNullOrWhiteSpace(metaData.Focuser.Name)) {
                /* fits4win, SGP */
                AddHeader("FOCNAME", metaData.Focuser.Name, "Focusing equipment name");
            }

            if (metaData.Focuser.Position.HasValue) {
                /* fits4win, SGP */
                AddHeader("FOCPOS", metaData.Focuser.Position.Value, "[step] Focuser position");

                /* MaximDL, several observatories */
                AddHeader("FOCUSPOS", metaData.Focuser.Position.Value, "[step] Focuser position");
            }

            if (!double.IsNaN(metaData.Focuser.StepSize)) {
                /* MaximDL */
                AddHeader("FOCUSSZ", metaData.Focuser.StepSize, "[um] Focuser step size");
            }

            if (!double.IsNaN(metaData.Focuser.Temperature)) {
                /* fits4win, SGP */
                AddHeader("FOCTEMP", metaData.Focuser.Temperature, "[degC] Focuser temperature");

                /* MaximDL, several observatories */
                AddHeader("FOCUSTEM", metaData.Focuser.Temperature, "[degC] Focuser temperature");
            }

            /* Rotator */
            if (!string.IsNullOrEmpty(metaData.Rotator.Name)) {
                /* NINA */
                AddHeader("ROTNAME", metaData.Rotator.Name, "Rotator equipment name");
            }

            if (!double.IsNaN(metaData.Rotator.MechanicalPosition)) {
                /* fits4win */
                AddHeader("ROTATOR", metaData.Rotator.MechanicalPosition, "[deg] Mechanical rotator angle");

                /* MaximDL, several observatories */
                AddHeader("ROTATANG", metaData.Rotator.MechanicalPosition, "[deg] Mechanical rotator angle");
            }

            if (!double.IsNaN(metaData.Rotator.StepSize)) {
                /* NINA */
                AddHeader("ROTSTPSZ", metaData.Rotator.StepSize, "[deg] Rotator step size");
            }

            /* Weather Data */
            if (!double.IsNaN(metaData.WeatherData.CloudCover)) {
                AddHeader("CLOUDCVR", metaData.WeatherData.CloudCover, "[percent] Cloud cover");
            }

            if (!double.IsNaN(metaData.WeatherData.DewPoint)) {
                AddHeader("DEWPOINT", metaData.WeatherData.DewPoint, "[degC] Dew point");
            }

            if (!double.IsNaN(metaData.WeatherData.Humidity)) {
                AddHeader("HUMIDITY", metaData.WeatherData.Humidity, "[percent] Relative humidity");
            }

            if (!double.IsNaN(metaData.WeatherData.Pressure)) {
                AddHeader("PRESSURE", metaData.WeatherData.Pressure, "[hPa] Air pressure");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyBrightness)) {
                AddHeader("SKYBRGHT", metaData.WeatherData.SkyBrightness, "[lux] Sky brightness");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyQuality)) {
                /* fits4win */
                AddHeader("MPSAS", metaData.WeatherData.SkyQuality, "[mags/arcsec^2] Sky quality");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyTemperature)) {
                AddHeader("SKYTEMP", metaData.WeatherData.SkyTemperature, "[degC] Sky temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.StarFWHM)) {
                AddHeader("STARFWHM", metaData.WeatherData.StarFWHM, "Star FWHM");
            }

            if (!double.IsNaN(metaData.WeatherData.Temperature)) {
                AddHeader("AMBTEMP", metaData.WeatherData.Temperature, "[degC] Ambient air temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.WindDirection)) {
                AddHeader("WINDDIR", metaData.WeatherData.WindDirection, "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W");
            }

            if (!double.IsNaN(metaData.WeatherData.WindGust)) {
                AddHeader("WINDGUST", metaData.WeatherData.WindGust * 3.6, "[kph] Wind gust");
            }

            if (!double.IsNaN(metaData.WeatherData.WindSpeed)) {
                AddHeader("WINDSPD", metaData.WeatherData.WindSpeed * 3.6, "[kph] Wind speed");
            }

            //ROWORDER as proposed by Siril at https://free-astro.org/index.php?title=Siril:FITS_orientation
            AddHeader("ROWORDER", "TOP-DOWN", "FITS Image Orientation");

            AddHeader("EQUINOX", 2000.0d, "Equinox of celestial coordinate system");
            AddHeader("SWCREATE", string.Format("N.I.N.A. {0} ({1})", CoreUtil.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");

            var reserved = new string[] { "SIMPLE", "BITPIX", "NAXIS", "NAXIS1", "NAXIS2", "BZERO", "EXTEND" };
            foreach (var elem in metaData.GenericHeaders) {
                if (reserved.Any(elem.Key.Contains)) { continue; }
                switch (elem) {
                    case StringMetaDataHeader s:
                        AddHeader(s.Key, s.Value, s.Comment);
                        break;
                    case IntMetaDataHeader i:
                        AddHeader(i.Key, i.Value, i.Comment);
                        break;
                    case DoubleMetaDataHeader d:
                        AddHeader(d.Key, d.Value, d.Comment);
                        break;
                    case BoolMetaDataHeader b:
                        AddHeader(b.Key, b.Value ? "T" : "F", b.Comment);
                        break;
                    case DateTimeMetaDataHeader d:
                        AddHeader(d.Key, d.Value, d.Comment);
                        break;
                }

            }
        }
    }
}
