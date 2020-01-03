#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using NINA.Model.ImageData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NINA.Utility {

    /// <summary>
    /// Specification:
    /// https://fits.gsfc.nasa.gov/fits_standard.html
    /// http://archive.stsci.edu/fits/fits_standard/fits_standard.html
    /// </summary>
    public class FITS {

        public FITS(ushort[] data, int width, int height) {
            this._imageData = data;

            DateTime now = DateTime.Now;

            AddHeaderCard("SIMPLE", true, "C# FITS");
            AddHeaderCard("BITPIX", 16, "");
            AddHeaderCard("NAXIS", 2, "Dimensionality");
            AddHeaderCard("NAXIS1", width, "");
            AddHeaderCard("NAXIS2", height, "");
            AddHeaderCard("BZERO", 32768, "");
            AddHeaderCard("EXTEND", true, "Extensions are permitted");
        }

        private List<FITSHeaderCard> _headerCards = new List<FITSHeaderCard>();

        public ICollection<FITSHeaderCard> HeaderCards {
            get {
                return _headerCards.AsReadOnly();
            }
        }

        private ushort[] _imageData;

        public ICollection<ushort> ImageData {
            get {
                return Array.AsReadOnly(_imageData);
            }
        }

        /// <summary>
        /// Fills FITS Header Cards using all available ImageMetaData information
        /// </summary>
        /// <param name="metaData"></param>
        public void PopulateHeaderCards(ImageMetaData metaData) {
            if (!string.IsNullOrWhiteSpace(metaData.Image.ImageType)) {
                var imageType = metaData.Image.ImageType;
                if (imageType == "SNAPSHOT") { imageType = "LIGHT"; }
                this.AddHeaderCard("IMAGETYP", imageType, "Type of exposure");
            }

            if (!double.IsNaN(metaData.Image.ExposureTime)) {
                this.AddHeaderCard("EXPOSURE", metaData.Image.ExposureTime, "[s] Exposure duration");
            }

            if (metaData.Image.ExposureStart > DateTime.MinValue) {
                this.AddHeaderCard("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime(), "Time of observation (local)");
                this.AddHeaderCard("DATE-OBS", metaData.Image.ExposureStart.ToUniversalTime(), "Time of observation (UTC)");
            }

            /* Camera */
            if (metaData.Camera.BinX > 0) {
                this.AddHeaderCard("XBINNING", metaData.Camera.BinX, "X axis binning factor");
            }
            if (metaData.Camera.BinY > 0) {
                this.AddHeaderCard("YBINNING", metaData.Camera.BinY, "Y axis binning factor");
            }

            if (!double.IsNaN(metaData.Camera.Gain) && metaData.Camera.Gain >= 0) {
                this.AddHeaderCard("GAIN", metaData.Camera.Gain, "Sensor gain");
            }

            if (metaData.Camera.Offset >= 0) {
                this.AddHeaderCard("OFFSET", metaData.Camera.Offset, "Sensor gain offset");
            }

            if (!double.IsNaN(metaData.Camera.ElectronsPerADU)) {
                this.AddHeaderCard("EGAIN", metaData.Camera.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit");
            }

            if (!double.IsNaN(metaData.Camera.PixelSize)) {
                this.AddHeaderCard("XPIXSZ", metaData.Camera.PixelSize, "[um] Pixel X axis size");
                this.AddHeaderCard("YPIXSZ", metaData.Camera.PixelSize, "[um] Pixel Y axis size");
            }

            if (!string.IsNullOrEmpty(metaData.Camera.Name)) {
                this.AddHeaderCard("INSTRUME", metaData.Camera.Name, "Imaging instrument name");
            }

            if (!double.IsNaN(metaData.Camera.SetPoint)) {
                this.AddHeaderCard("SET-TEMP", metaData.Camera.SetPoint, "[degC] CCD temperature setpoint");
            }

            if (!double.IsNaN(metaData.Camera.Temperature)) {
                this.AddHeaderCard("CCD-TEMP", metaData.Camera.Temperature, "[degC] CCD temperature");
            }

            /* Telescope */
            if (!string.IsNullOrWhiteSpace(metaData.Telescope.Name)) {
                this.AddHeaderCard("TELESCOP", metaData.Telescope.Name, "Name of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.FocalLength) && metaData.Telescope.FocalLength > 0) {
                this.AddHeaderCard("FOCALLEN", metaData.Telescope.FocalLength, "[mm] Focal length");
            }

            if (!double.IsNaN(metaData.Telescope.FocalRatio) && metaData.Telescope.FocalRatio > 0) {
                this.AddHeaderCard("FOCRATIO", metaData.Telescope.FocalRatio, "Focal ratio");
            }

            if (metaData.Telescope.Coordinates != null) {
                this.AddHeaderCard("RA", metaData.Telescope.Coordinates.RADegrees, "[deg] RA of telescope");
                this.AddHeaderCard("DEC", metaData.Telescope.Coordinates.Dec, "[deg] Declination of telescope");
            }

            /* Observer */
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                this.AddHeaderCard("SITEELEV", metaData.Observer.Elevation, "[m] Observation site elevation");
            }
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                this.AddHeaderCard("SITELAT", metaData.Observer.Latitude, "[deg] Observation site latitude");
            }
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                this.AddHeaderCard("SITELONG", metaData.Observer.Longitude, "[deg] Observation site longitude");
            }

            /* Filter Wheel */
            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Name)) {
                /* fits4win */
                this.AddHeaderCard("FWHEEL", metaData.FilterWheel.Name, "Filter Wheel name");
            }

            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Filter)) {
                /* fits4win */
                this.AddHeaderCard("FILTER", metaData.FilterWheel.Filter, "Active filter name");
            }

            /* Target */
            if (!string.IsNullOrWhiteSpace(metaData.Target.Name)) {
                this.AddHeaderCard("OBJECT", metaData.Target.Name, "Name of the object of interest");
            }

            if (metaData.Target.Coordinates != null) {
                this.AddHeaderCard("OBJCTRA", Astrometry.Astrometry.HoursToFitsHMS(metaData.Target.Coordinates.RA), "[H M S] RA of imaged object");
                this.AddHeaderCard("OBJCTDEC", Astrometry.Astrometry.DegreesToFitsDMS(metaData.Target.Coordinates.Dec), "[D M S] Declination of imaged object");
            }

            /* Focuser */
            if (!string.IsNullOrWhiteSpace(metaData.Focuser.Name)) {
                /* fits4win, SGP */
                this.AddHeaderCard("FOCNAME", metaData.Focuser.Name, "Focusing equipment name");
            }

            if (!double.IsNaN(metaData.Focuser.Position)) {
                /* fits4win, SGP */
                this.AddHeaderCard("FOCPOS", metaData.Focuser.Position, "[step] Focuser position");

                /* MaximDL, several observatories */
                this.AddHeaderCard("FOCUSPOS", metaData.Focuser.Position, "[step] Focuser position");
            }

            if (!double.IsNaN(metaData.Focuser.StepSize)) {
                /* MaximDL */
                this.AddHeaderCard("FOCUSSZ", metaData.Focuser.StepSize, "[um] Focuser step size");
            }

            if (!double.IsNaN(metaData.Focuser.Temperature)) {
                /* fits4win, SGP */
                this.AddHeaderCard("FOCTEMP", metaData.Focuser.Temperature, "[degC] Focuser temperature");

                /* MaximDL, several observatories */
                this.AddHeaderCard("FOCUSTEM", metaData.Focuser.Temperature, "[degC] Focuser temperature");
            }

            /* Rotator */
            if (!string.IsNullOrEmpty(metaData.Rotator.Name)) {
                /* NINA */
                this.AddHeaderCard("ROTNAME", metaData.Rotator.Name, "Rotator equipment name");
            }

            if (!double.IsNaN(metaData.Rotator.Position)) {
                /* fits4win */
                this.AddHeaderCard("ROTATOR", metaData.Rotator.Position, "[deg] Rotator angle");

                /* MaximDL, several observatories */
                this.AddHeaderCard("ROTATANG", metaData.Rotator.Position, "[deg] Rotator angle");
            }

            if (!double.IsNaN(metaData.Rotator.StepSize)) {
                /* NINA */
                this.AddHeaderCard("ROTSTPSZ", metaData.Rotator.StepSize, "[deg] Rotator step size");
            }

            /* Weather Data */
            if (!double.IsNaN(metaData.WeatherData.CloudCover)) {
                this.AddHeaderCard("CLOUDCVR", metaData.WeatherData.CloudCover, "[percent] Cloud cover");
            }

            if (!double.IsNaN(metaData.WeatherData.DewPoint)) {
                this.AddHeaderCard("DEWPOINT", metaData.WeatherData.DewPoint, "[degC] Dew point");
            }

            if (!double.IsNaN(metaData.WeatherData.Humidity)) {
                this.AddHeaderCard("HUMIDITY", metaData.WeatherData.Humidity, "[percent] Relative humidity");
            }

            if (!double.IsNaN(metaData.WeatherData.Pressure)) {
                this.AddHeaderCard("PRESSURE", metaData.WeatherData.Pressure, "[hPa] Air pressure");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyBrightness)) {
                this.AddHeaderCard("SKYBRGHT", metaData.WeatherData.SkyBrightness, "[lux] Sky brightness");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyQuality)) {
                /* fits4win */
                this.AddHeaderCard("MPSAS", metaData.WeatherData.SkyQuality, "[mags/arcsec^2] Sky quality");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyTemperature)) {
                this.AddHeaderCard("SKYTEMP", metaData.WeatherData.SkyTemperature, "[degC] Sky temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.StarFWHM)) {
                this.AddHeaderCard("STARFWHM", metaData.WeatherData.StarFWHM, "Star FWHM");
            }

            if (!double.IsNaN(metaData.WeatherData.Temperature)) {
                this.AddHeaderCard("AMBTEMP", metaData.WeatherData.Temperature, "[degC] Ambient air temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.WindDirection)) {
                this.AddHeaderCard("WINDDIR", metaData.WeatherData.WindDirection, "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W");
            }

            if (!double.IsNaN(metaData.WeatherData.WindGust)) {
                this.AddHeaderCard("WINDGUST", metaData.WeatherData.WindGust * 3.6, "[kph] Wind gust");
            }

            if (!double.IsNaN(metaData.WeatherData.WindSpeed)) {
                this.AddHeaderCard("WINDSPD", metaData.WeatherData.WindSpeed * 3.6, "[kph] Wind speed");
            }

            this.AddHeaderCard("SWCREATE", string.Format("N.I.N.A. {0} ({1})", Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");
        }

        private void AddHeaderCard(string keyword, string value, string comment) {
            _headerCards.Add(new FITSHeaderCard(keyword, value, comment));
        }

        private void AddHeaderCard(string keyword, int value, string comment) {
            _headerCards.Add(new FITSHeaderCard(keyword, value, comment));
        }

        private void AddHeaderCard(string keyword, double value, string comment) {
            _headerCards.Add(new FITSHeaderCard(keyword, value, comment));
        }

        private void AddHeaderCard(string keyword, bool value, string comment) {
            _headerCards.Add(new FITSHeaderCard(keyword, value, comment));
        }

        private void AddHeaderCard(string keyword, DateTime value, string comment) {
            _headerCards.Add(new FITSHeaderCard(keyword, value, comment));
        }

        public void Write(Stream s) {
            /* Write header */
            foreach (var card in _headerCards) {
                var b = card.Encode();
                s.Write(b, 0, HEADERCARDSIZE);
            }

            /* Close header http://archive.stsci.edu/fits/fits_standard/node64.html#SECTION001221000000000000000 */
            s.Write(Encoding.ASCII.GetBytes("END".PadRight(HEADERCARDSIZE)), 0, HEADERCARDSIZE);

            /* Write blank lines for the rest of the header block */
            for (var i = _headerCards.Count + 1; i % (BLOCKSIZE / HEADERCARDSIZE) != 0; i++) {
                s.Write(Encoding.ASCII.GetBytes("".PadRight(HEADERCARDSIZE)), 0, HEADERCARDSIZE);
            }

            /* Write image data */
            for (int i = 0; i < this._imageData.Length; i++) {
                var val = (short)(this._imageData[i] - (short.MaxValue + 1));
                s.WriteByte((byte)(val >> 8));
                s.WriteByte((byte)val);
            }

            long remainingBlockPadding = (long)Math.Ceiling((double)s.Position / (double)BLOCKSIZE) * (long)BLOCKSIZE - s.Position;
            byte zeroByte = 0;
            //Pad remaining FITS block with zero values
            for (int i = 0; i < remainingBlockPadding; i++) {
                s.WriteByte(zeroByte);
            }
        }

        /* Blocksize specification: http://archive.stsci.edu/fits/fits_standard/node13.html#SECTION00810000000000000000 */
        private const int BLOCKSIZE = 2880;
        /* Header card size Specification: http://archive.stsci.edu/fits/fits_standard/node29.html#SECTION00912100000000000000 */
        private const int HEADERCARDSIZE = 80;
    }

    public class FITSHeaderCard {/* Extended ascii encoding*/
        private static Encoding ascii = Encoding.GetEncoding("iso-8859-1");

        public FITSHeaderCard(string key, string value, string comment) {
            /*
             * FITS Standard 4.0, Section 4.2.1:
             * A single quote is represented within a string as two successive single quotes
             */
            this.Key = key;
            if (value.Length > 18) {
                value = value.Substring(0, 18);
            }
            this.Value = $"'{value.Replace(@"'", @"''")}'".PadRight(20);

            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, bool value, string comment) {
            this.Key = key;
            this.Value = value ? "T" : "F";
            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, double value, string comment) {
            this.Key = key;
            this.Value = Math.Round(value, 15).ToString(CultureInfo.InvariantCulture);
            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, DateTime value, string comment) {
            this.Key = key;
            this.Value = @"'" + value.ToString(@"yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + @"'";
            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public FITSHeaderCard(string key, int value, string comment) {
            this.Key = key;
            this.Value = value.ToString(CultureInfo.InvariantCulture);
            if (comment.Length > 45) {
                comment = comment.Substring(0, 45);
            }
            this.Comment = comment;
        }

        public string Key { get; }
        public string Value { get; }
        public string Comment { get; }

        public string GetHeaderString() {
            var encodedKeyword = Key.ToUpper().PadRight(8);
            var encodedValue = Value.PadLeft(20);

            var header = $"{encodedKeyword}= {encodedValue} / ";
            var encodedComment = Comment.PadRight(80 - header.Length);
            header += encodedComment;
            return header;
        }

        /// <summary>
        /// Encodes a FITS header according to FITS specifications to be exactly 80 characters long
        /// value + comment length must not exceed 67 characters
        /// </summary>
        /// <param name="keyword">FITS Keyword. Max length 8 chars</param>
        /// <param name="value">Keyword Value</param>
        /// <param name="comment">Description of Keyword</param>
        /// <remarks>
        /// Header Specification:
        /// http://archive.stsci.edu/fits/fits_standard/node29.html#SECTION00912100000000000000
        /// More in depth: https://fits.gsfc.nasa.gov/fits_standard.html
        /// </remarks>
        public byte[] Encode() {
            return ascii.GetBytes(GetHeaderString());
        }
    }
}