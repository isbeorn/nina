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
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using System.Windows.Input;

namespace NINA.Image.FileFormat.FITS {
    public class FITSHeader {

        public FITSHeader(int width, int height) {
                Add("SIMPLE", true, "C# FITS");
                Add("BITPIX", 16, "");
                Add("NAXIS", 2, "Dimensionality");
                Add("NAXIS1", width, "");
                Add("NAXIS2", height, "");
                Add("BZERO", 32768, "");
                Add("EXTEND", true, "Extensions are permitted");
            }

        private Dictionary<string, FITSHeaderCard> _headerCards = new Dictionary<string, FITSHeaderCard>();

        public ICollection<FITSHeaderCard> HeaderCards => _headerCards.Values;

        public void Add(string keyword, string value, string comment) {
            if (!_headerCards.ContainsKey(keyword)) {
                _headerCards.Add(keyword, new FITSHeaderCard(keyword, value, comment));
            }
        }

        public void Add(string keyword, int value, string comment) {
            if (!_headerCards.ContainsKey(keyword)) {
                _headerCards.Add(keyword, new FITSHeaderCard(keyword, value, comment));
            }
        }

        public void Add(string keyword, double value, string comment) {
            if (!_headerCards.ContainsKey(keyword)) {
                _headerCards.Add(keyword, new FITSHeaderCard(keyword, value, comment));
            }
        }

        public void Add(string keyword, bool value, string comment) {
            if (!_headerCards.ContainsKey(keyword)) {
                _headerCards.Add(keyword, new FITSHeaderCard(keyword, value, comment));
            }
        }

        public void Add(string keyword, DateTime value, string comment) {
            if (!_headerCards.ContainsKey(keyword)) {
                _headerCards.Add(keyword, new FITSHeaderCard(keyword, value, comment));
            }
        }

        public void Write(Stream s) {
            /* Write header */
            foreach (var card in this.HeaderCards) {
                var b = card.Encode();
                s.Write(b, 0, FITS.HEADERCARDSIZE);
            }

            /* Close header http://archive.stsci.edu/fits/fits_standard/node64.html#SECTION001221000000000000000 */
            s.Write(Encoding.ASCII.GetBytes("END".PadRight(FITS.HEADERCARDSIZE)), 0, FITS.HEADERCARDSIZE);

            /* Write blank lines for the rest of the header block */
            for (var i = this.HeaderCards.Count + 1; i % (FITS.BLOCKSIZE / FITS.HEADERCARDSIZE) != 0; i++) {
                s.Write(Encoding.ASCII.GetBytes("".PadRight(FITS.HEADERCARDSIZE)), 0, FITS.HEADERCARDSIZE);
            }
        }

        public ImageMetaData ExtractMetaData() {
            var metaData = new ImageMetaData();
            metaData.GenericHeaders = GetAllFITSKeywords();

            if (_headerCards.TryGetValue("IMAGETYP", out var card)) {
                metaData.Image.ImageType = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("IMAGETYP", out card)) {
                metaData.Image.ImageType = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("EXPOSURE", out card)) {
                metaData.Image.ExposureTime = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("EXPTIME", out card)) {
                metaData.Image.ExposureTime = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("DATE-LOC", out card)) {
                //metaData.Image.ExposureStart = DateTime.ParseExact($"{card.OriginalValue}", "yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
            }

            if (_headerCards.TryGetValue("DATE-OBS", out card)) {
                //metaData.Image.ExposureStart = DateTime.ParseExact($"{card.OriginalValue} UTC", "yyyy-MM-ddTHH:mm:ss.fff UTC", CultureInfo.InvariantCulture);
            }

            /* Camera */
            if (_headerCards.TryGetValue("XBINNING", out card)) {
                metaData.Camera.BinX = ParseInt(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("YBINNING", out card)) {
                metaData.Camera.BinY = ParseInt(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("GAIN", out card)) {
                metaData.Camera.Gain = ParseInt(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("OFFSET", out card)) {
                metaData.Camera.Offset = ParseInt(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("EGAIN", out card)) {
                metaData.Camera.ElectronsPerADU = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("XPIXSZ", out card)) {
                metaData.Camera.PixelSize = ParseDouble(card.OriginalValue) / metaData.Camera.BinX;
            }

            if (_headerCards.TryGetValue("INSTRUME", out card)) {
                metaData.Camera.Name = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("CAMERAID", out card)) {
                metaData.Camera.Id = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("SET-TEMP", out card)) {
                metaData.Camera.SetPoint = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("CCD-TEMP", out card)) {
                metaData.Camera.Temperature = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("READOUTM", out card)) {
                metaData.Camera.ReadoutModeName = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("BAYERPAT", out card)) {
                // FITS Bayer Patterns seem to include single quotes and extra spaces
                var originalValue = card.OriginalValue.Trim('\'').Trim();
                metaData.Camera.SensorType = metaData.StringToSensorType(originalValue);
            }

            if (_headerCards.TryGetValue("XBAYROFF", out card)) {
                metaData.Camera.BayerOffsetX = ParseInt(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("YBAYROFF", out card)) {
                metaData.Camera.BayerOffsetY = ParseInt(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("USBLIMIT", out card)) {
                metaData.Camera.USBLimit = ParseInt(card.OriginalValue);
            }

            /* Telescope */
            if (_headerCards.TryGetValue("TELESCOP", out card)) {
                metaData.Telescope.Name = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("FOCALLEN", out card)) {
                metaData.Telescope.FocalLength = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("FOCRATIO", out card)) {
                metaData.Telescope.FocalRatio = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("PIERSIDE", out card)) {
                metaData.Telescope.SideOfPier = ParsePierSide(card.OriginalValue);
            }

            if (_headerCards.ContainsKey("RA") && _headerCards.ContainsKey("DEC")) {
                string raHdrVal = _headerCards["RA"].OriginalValue;
                string decHdrVal = _headerCards["DEC"].OriginalValue;
                double ra;
                double dec;

                if (AstroUtil.IsHMS(raHdrVal)) {
                    ra = AstroUtil.HMSToDegrees(raHdrVal);
                } else {
                    ra = ParseDouble(raHdrVal);
                }

                if (AstroUtil.IsDMS(decHdrVal)) {
                    dec = AstroUtil.DMSToDegrees(decHdrVal);
                } else {
                    dec = ParseDouble(decHdrVal);
                }

                metaData.Telescope.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
            }

            if (_headerCards.TryGetValue("CENTALT", out card) || _headerCards.TryGetValue("OBJCTALT", out card)) {
                metaData.Telescope.Altitude = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("CENTAZ", out card) || _headerCards.TryGetValue("OBJCTAZ", out card)) {
                metaData.Telescope.Azimuth = ParseDouble(card.OriginalValue);
            }

            /* Observer */
            if (_headerCards.TryGetValue("SITEELEV", out card)) {
                metaData.Observer.Elevation = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("SITELAT", out card)) {
                string hdrVal = card.OriginalValue;
                double value;

                if (AstroUtil.IsDMS(hdrVal)) {
                    value = AstroUtil.DMSToDegrees(hdrVal);
                } else {
                    value = ParseDouble(hdrVal);
                }

                metaData.Observer.Latitude = value;
            }

            if (_headerCards.TryGetValue("SITELONG", out card)) {
                string hdrVal = card.OriginalValue;
                double value;

                if (AstroUtil.IsDMS(hdrVal)) {
                    value = AstroUtil.DMSToDegrees(hdrVal);
                } else {
                    value = ParseDouble(hdrVal);
                }

                metaData.Observer.Longitude = value;
            }

            if (_headerCards.TryGetValue("OBSERVER", out card)) {
                metaData.Observer.Name = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("OBSERVAT", out card)) {
                metaData.Observer.Observatory = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("SITENAME", out card)) {
                metaData.Observer.Site = card.OriginalValue;
            }

            /* Filter Wheel */
            if (_headerCards.TryGetValue("FWHEEL", out card)) {
                metaData.FilterWheel.Name = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("FILTER", out card)) {
                metaData.FilterWheel.Filter = card.OriginalValue;
            }

            /* Target */
            if (_headerCards.TryGetValue("OBJECT", out card)) {
                metaData.Target.Name = card.OriginalValue;
            }

            if (_headerCards.ContainsKey("OBJCTRA") && _headerCards.ContainsKey("OBJCTDEC")) {
                string raHdrVal = _headerCards["OBJCTRA"].OriginalValue;
                string decHdrVal = _headerCards["OBJCTDEC"].OriginalValue;
                double ra;
                double dec;

                if (AstroUtil.IsHMS(raHdrVal)) {
                    ra = AstroUtil.HMSToDegrees(raHdrVal);
                } else {
                    ra = ParseDouble(raHdrVal);
                }

                if (AstroUtil.IsDMS(decHdrVal)) {
                    dec = AstroUtil.DMSToDegrees(decHdrVal);
                } else {
                    dec = ParseDouble(decHdrVal);
                }

                metaData.Target.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
            }

            if (_headerCards.TryGetValue("OBJCTROT", out card)) {
                metaData.Target.PositionAngle = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("AIRMASS", out card)) {
                metaData.Telescope.Airmass = ParseDouble(card.OriginalValue);
            }

            /* Focuser */
            if (_headerCards.TryGetValue("FOCNAME", out card)) {
                metaData.Focuser.Name = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("FOCPOS", out card)) {
                metaData.Focuser.Position = ParseInt(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("FOCUSPOS", out card)) {
                metaData.Focuser.Position = ParseInt(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("FOCUSSZ", out card)) {
                metaData.Focuser.StepSize = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("FOCTEMP", out card)) {
                metaData.Focuser.Temperature = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("FOCUSTEM", out card)) {
                metaData.Focuser.Temperature = ParseDouble(card.OriginalValue);
            }

            /* Rotator */

            if (_headerCards.TryGetValue("ROTNAME", out card)) {
                metaData.Rotator.Name = card.OriginalValue;
            }

            if (_headerCards.TryGetValue("ROTATOR", out card)) {
                metaData.Rotator.MechanicalPosition = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("ROTATANG", out card)) {
                metaData.Rotator.MechanicalPosition = ParseDouble(card.OriginalValue);
            }

            if (_headerCards.TryGetValue("ROTSTPSZ", out card)) {
                metaData.Rotator.StepSize = ParseDouble(card.OriginalValue);
            }

            /* Weather Data */

            if (_headerCards.TryGetValue("CLOUDCVR", out card)) {
                metaData.WeatherData.CloudCover = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("DEWPOINT", out card)) {
                metaData.WeatherData.DewPoint = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("HUMIDITY", out card)) {
                metaData.WeatherData.Humidity = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("PRESSURE", out card)) {
                metaData.WeatherData.Pressure = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("SKYBRGHT", out card)) {
                metaData.WeatherData.SkyBrightness = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("MPSAS", out card)) {
                metaData.WeatherData.SkyQuality = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("SKYTEMP", out card)) {
                metaData.WeatherData.SkyTemperature = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("STARFWHM", out card)) {
                metaData.WeatherData.StarFWHM = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("AMBTEMP", out card)) {
                metaData.WeatherData.Temperature = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("WINDDIR", out card)) {
                metaData.WeatherData.WindDirection = ParseDouble(card.OriginalValue);
            }
            if (_headerCards.TryGetValue("WINDGUST", out card)) {
                metaData.WeatherData.WindGust = ParseDouble(card.OriginalValue) / 3.6;
            }
            if (_headerCards.TryGetValue("WINDSPD", out card)) {
                metaData.WeatherData.WindSpeed = ParseDouble(card.OriginalValue) / 3.6;
            }

            /* WCS */
            if (_headerCards.TryGetValue("CTYPE1", out var ctype1Card) && _headerCards.TryGetValue("CTYPE2", out var ctype2Card)) {
                if (ctype1Card.OriginalValue == "RA---TAN" && ctype2Card.OriginalValue == "DEC--TAN") {
                    if (_headerCards.TryGetValue("CRPIX1", out var crpix1Card)
                        && _headerCards.TryGetValue("CRPIX2", out var crpix2Card)
                        && _headerCards.TryGetValue("CRVAL1", out var crval1Card)
                        && _headerCards.TryGetValue("CRVAL2", out var crval2Card)) {
                        var crPix1 = ParseDouble(crpix1Card.OriginalValue);
                        var crPix2 = ParseDouble(crpix2Card.OriginalValue);
                        var crVal1 = ParseDouble(crval1Card.OriginalValue);
                        var crVal2 = ParseDouble(crval2Card.OriginalValue);

                        if (_headerCards.TryGetValue("CD1_1", out var cd1_1Card)
                            && _headerCards.TryGetValue("CD1_2", out var cd1_2Card)
                            && _headerCards.TryGetValue("CD2_1", out var cd2_1Card)
                            && _headerCards.TryGetValue("CD2_2", out var cd2_2Card)
                        ) {
                            // CDn_m notation
                            var cd1_1 = ParseDouble(cd1_1Card.OriginalValue);
                            var cd1_2 = ParseDouble(cd1_2Card.OriginalValue);
                            var cd2_1 = ParseDouble(cd2_1Card.OriginalValue);
                            var cd2_2 = ParseDouble(cd2_2Card.OriginalValue);

                            var wcs = new WorldCoordinateSystem(crVal1, crVal2, crPix1, crPix2, cd1_1, cd1_2, cd2_1, cd2_2);
                            metaData.WorldCoordinateSystem = wcs;
                        } else if (_headerCards.TryGetValue("CDELT1", out var CDELT1Card)
                            && _headerCards.TryGetValue("CDELT2", out var CDELT2Card)
                            && _headerCards.TryGetValue("CROTA2", out var CROTA2Card)
                        ) {
                            // Older CROTA2 notation
                            var cdelt1 = ParseDouble(CDELT1Card.OriginalValue);
                            var cdelt2 = ParseDouble(CDELT2Card.OriginalValue);
                            var crota2 = ParseDouble(CROTA2Card.OriginalValue);

                            var wcs = new WorldCoordinateSystem(crVal1, crVal2, crPix1, crPix2, cdelt1, cdelt2, crota2);
                            metaData.WorldCoordinateSystem = wcs;
                        } else {
                            Logger.Debug("FITS WCS - No CROTA2 or CDn_m keywords found");
                        }
                    } else {
                        Logger.Debug("FITS WCS - No CRPIX and CRVAL keywords found");
                    }
                } else {
                    Logger.Debug($"FITS WCS - Incompatible projection found {ctype1Card.OriginalValue} {ctype2Card.OriginalValue}");
                }
            }

            return metaData;
        }

        private List<IGenericMetaDataHeader> GetAllFITSKeywords() {
            var l = new List<IGenericMetaDataHeader>();            
            if (_headerCards.Count == 0) { return l; }

            foreach (var elem in _headerCards) {
                if (elem.Key == null) { continue; }

                var key = elem.Key;
                if (key == null) { continue; }

                if(elem.Value.Value.StartsWith("'")) {
                    var value = elem.Value.OriginalValue.Trim();
                    l.Add(new StringMetaDataHeader(key, value, elem.Value.Comment));
                } else if (elem.Value.OriginalValue == "T" || elem.Value.OriginalValue == "F") {
                    var value = elem.Value.OriginalValue.Trim() == "T" ? true : false;
                    l.Add(new BoolMetaDataHeader(key, value, elem.Value.Comment));
                } else if (elem.Value.OriginalValue.Contains(".") && double.TryParse(elem.Value.OriginalValue, CultureInfo.InvariantCulture, out var number)) {                    
                    l.Add(new DoubleMetaDataHeader(key, number, elem.Value.Comment));
                } else if (int.TryParse(elem.Value.OriginalValue, out var integer)) {
                    l.Add(new IntMetaDataHeader(key, integer, elem.Value.Comment));
                }
            }
            return l;
        }

        private double ParseDouble(string value) {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dbl)) {
                return dbl;
            } else {
                return double.NaN;
            }
        }

        private int ParseInt(string value) {
            if (value.EndsWith(".0")) {
                value = value.Replace(".0", "");
            }
            if (int.TryParse(value, out var intVal)) {
                return intVal;
            } else {
                return 0;
            }
        }

        private PierSide ParsePierSide(string value) {
            var strVal = value.Trim();

            if (strVal.StartsWith("west", true, CultureInfo.InvariantCulture)) {
                return PierSide.pierWest;
            } else if (strVal.StartsWith("east", true, CultureInfo.InvariantCulture)) {
                return PierSide.pierEast;
            } else {
                return PierSide.pierUnknown;
            }
        }

        /// <summary>
        /// Fills FITS Header Cards using all available ImageMetaData information
        /// </summary>
        /// <param name="metaData"></param>
        public void PopulateFromMetaData(ImageMetaData metaData) {
            if (!string.IsNullOrWhiteSpace(metaData.Image.ImageType)) {
                var imageType = metaData.Image.ImageType;
                if (imageType == "SNAPSHOT") { imageType = "LIGHT"; }
                Add("IMAGETYP", imageType, "Type of exposure");
            }

            if (!double.IsNaN(metaData.Image.ExposureTime)) {
                Add("EXPOSURE", metaData.Image.ExposureTime, "[s] Exposure duration");
                Add("EXPTIME", metaData.Image.ExposureTime, "[s] Exposure duration");
            }

            if (metaData.Image.ExposureStart > DateTime.MinValue) {
                Add("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime(), "Time of observation (local)");
                Add("DATE-OBS", metaData.Image.ExposureStart.ToUniversalTime(), "Time of observation (UTC)");
            }

            if (metaData.Image.ExposureMidPoint > DateTime.MinValue) {
                // Section 8.4.1 https://fits.gsfc.nasa.gov/standard40/fits_standard40aa-le.pdf
                // Calendar date of the midpoint of the observation, expressed in the same way as the DATE-OBS keyword.
                Add("DATE-AVG", metaData.Image.ExposureMidPoint, "Averaged midpoint time (UTC)");
            }

            /* Camera */
            if (metaData.Camera.BinX > 0) {
                Add("XBINNING", metaData.Camera.BinX, "X axis binning factor");
            }

            if (metaData.Camera.BinY > 0) {
                Add("YBINNING", metaData.Camera.BinY, "Y axis binning factor");
            }

            if (metaData.Camera.Gain >= 0) {
                Add("GAIN", metaData.Camera.Gain, "Sensor gain");
            }

            if (metaData.Camera.Offset >= 0) {
                Add("OFFSET", metaData.Camera.Offset, "Sensor gain offset");
            }

            if (!double.IsNaN(metaData.Camera.ElectronsPerADU)) {
                Add("EGAIN", metaData.Camera.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit");
            }

            if (!double.IsNaN(metaData.Camera.PixelSize)) {
                double pixelX = metaData.Camera.PixelSize * Math.Max(metaData.Camera.BinX, 1);
                double pixelY = metaData.Camera.PixelSize * Math.Max(metaData.Camera.BinY, 1);
                Add("XPIXSZ", pixelX, "[um] Pixel X axis size");
                Add("YPIXSZ", pixelY, "[um] Pixel Y axis size");
            }

            if (!string.IsNullOrEmpty(metaData.Camera.Name)) {
                Add("INSTRUME", metaData.Camera.Name, "Imaging instrument name");
            }

            if (!string.IsNullOrEmpty(metaData.Camera.Id)) {
                Add("CAMERAID", metaData.Camera.Id, "Imaging instrument identifier");
            }

            if (!double.IsNaN(metaData.Camera.SetPoint)) {
                Add("SET-TEMP", metaData.Camera.SetPoint, "[degC] CCD temperature setpoint");
            }

            if (!double.IsNaN(metaData.Camera.Temperature)) {
                Add("CCD-TEMP", metaData.Camera.Temperature, "[degC] CCD temperature");
            }

            if (!string.IsNullOrWhiteSpace(metaData.Camera.ReadoutModeName)) {
                Add("READOUTM", metaData.Camera.ReadoutModeName, "Sensor readout mode");
            }

            if (metaData.Camera.SensorType != SensorType.Monochrome && metaData.Camera.BayerPattern != BayerPatternEnum.None) {
                Add("BAYERPAT", metaData.Camera.SensorType.ToString().ToUpper(), "Sensor Bayer pattern");
                Add("XBAYROFF", metaData.Camera.BayerOffsetX, "Bayer pattern X axis offset");
                Add("YBAYROFF", metaData.Camera.BayerOffsetY, "Bayer pattern Y axis offset");
            }

            if (metaData.Camera.USBLimit > -1) {
                Add("USBLIMIT", metaData.Camera.USBLimit, "Camera-specific USB setting");
            }

            /* Telescope */
            if (!string.IsNullOrWhiteSpace(metaData.Telescope.Name)) {
                Add("TELESCOP", metaData.Telescope.Name, "Name of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.FocalLength) && metaData.Telescope.FocalLength > 0) {
                Add("FOCALLEN", metaData.Telescope.FocalLength, "[mm] Focal length");
            }

            if (!double.IsNaN(metaData.Telescope.FocalRatio) && metaData.Telescope.FocalRatio > 0) {
                Add("FOCRATIO", metaData.Telescope.FocalRatio, "Focal ratio");
            }

            if (metaData.Telescope.Coordinates != null) {
                Add("RA", metaData.Telescope.Coordinates.RADegrees, "[deg] RA of telescope");
                Add("DEC", metaData.Telescope.Coordinates.Dec, "[deg] Declination of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Altitude)) {
                Add("CENTALT", metaData.Telescope.Altitude, "[deg] Altitude of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Azimuth)) {
                Add("CENTAZ", metaData.Telescope.Azimuth, "[deg] Azimuth of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Airmass)) {
                Add("AIRMASS", metaData.Telescope.Airmass, "Airmass at frame center (Gueymard 1993)");
            }

            if (metaData.Telescope.SideOfPier != PierSide.pierUnknown) {
                string keyword = "PIERSIDE";
                string comment = "Telescope pointing state";

                if (metaData.Telescope.SideOfPier == PierSide.pierEast) {
                    Add(keyword, "East", comment);
                } else if (metaData.Telescope.SideOfPier == PierSide.pierWest) {
                    Add(keyword, "West", comment);
                }
            }

            /* Observer */
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                Add("SITEELEV", metaData.Observer.Elevation, "[m] Observation site elevation");
            }
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                Add("SITELAT", metaData.Observer.Latitude, "[deg] Observation site latitude");
            }
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                Add("SITELONG", metaData.Observer.Longitude, "[deg] Observation site longitude");
            }
            if (!string.IsNullOrEmpty(metaData.Observer.Name)) {
                Add("OBSERVER", metaData.Observer.Name, "Observer name");
            }
            if (!string.IsNullOrEmpty(metaData.Observer.Observatory)) {
                Add("OBSERVAT", metaData.Observer.Observatory, "Observatory name");
            }
            if (!string.IsNullOrEmpty(metaData.Observer.Name)) {
                Add("SITENAME", metaData.Observer.Site, "Observatory site name");
            }

            /* Filter Wheel */
            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Name)) {
                /* fits4win */
                Add("FWHEEL", metaData.FilterWheel.Name, "Filter Wheel name");
            }

            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Filter)) {
                /* fits4win */
                Add("FILTER", metaData.FilterWheel.Filter, "Active filter name");
            }

            /* Target */
            if (!string.IsNullOrWhiteSpace(metaData.Target.Name)) {
                Add("OBJECT", metaData.Target.Name, "Name of the object of interest");
            }

            if (metaData.Target.Coordinates != null) {
                Add("OBJCTRA", AstroUtil.HoursToFitsHMS(metaData.Target.Coordinates.RA), "[H M S] RA of imaged object");
                Add("OBJCTDEC", AstroUtil.DegreesToFitsDMS(metaData.Target.Coordinates.Dec), "[D M S] Declination of imaged object");
            }

            if (!double.IsNaN(metaData.Target.PositionAngle)) {
                /* NINA Specific target rotation */
                Add("OBJCTROT", metaData.Target.PositionAngle, "[deg] planned rotation of imaged object");
            }

            /* Focuser */
            if (!string.IsNullOrWhiteSpace(metaData.Focuser.Name)) {
                /* fits4win, SGP */
                Add("FOCNAME", metaData.Focuser.Name, "Focusing equipment name");
            }

            if (metaData.Focuser.Position.HasValue) {
                /* fits4win, SGP */
                Add("FOCPOS", metaData.Focuser.Position.Value, "[step] Focuser position");

                /* MaximDL, several observatories */
                Add("FOCUSPOS", metaData.Focuser.Position.Value, "[step] Focuser position");
            }

            if (!double.IsNaN(metaData.Focuser.StepSize)) {
                /* MaximDL */
                Add("FOCUSSZ", metaData.Focuser.StepSize, "[um] Focuser step size");
            }

            if (!double.IsNaN(metaData.Focuser.Temperature)) {
                /* fits4win, SGP */
                Add("FOCTEMP", metaData.Focuser.Temperature, "[degC] Focuser temperature");

                /* MaximDL, several observatories */
                Add("FOCUSTEM", metaData.Focuser.Temperature, "[degC] Focuser temperature");
            }

            /* Rotator */
            if (!string.IsNullOrEmpty(metaData.Rotator.Name)) {
                /* NINA */
                Add("ROTNAME", metaData.Rotator.Name, "Rotator equipment name");
            }

            if (!double.IsNaN(metaData.Rotator.MechanicalPosition)) {
                /* fits4win */
                Add("ROTATOR", metaData.Rotator.MechanicalPosition, "[deg] Mechanical rotator angle");

                /* MaximDL, several observatories */
                Add("ROTATANG", metaData.Rotator.MechanicalPosition, "[deg] Mechanical rotator angle");
            }

            if (!double.IsNaN(metaData.Rotator.StepSize)) {
                /* NINA */
                Add("ROTSTPSZ", metaData.Rotator.StepSize, "[deg] Rotator step size");
            }

            /* Weather Data */
            if (!double.IsNaN(metaData.WeatherData.CloudCover)) {
                Add("CLOUDCVR", metaData.WeatherData.CloudCover, "[percent] Cloud cover");
            }

            if (!double.IsNaN(metaData.WeatherData.DewPoint)) {
                Add("DEWPOINT", metaData.WeatherData.DewPoint, "[degC] Dew point");
            }

            if (!double.IsNaN(metaData.WeatherData.Humidity)) {
                Add("HUMIDITY", metaData.WeatherData.Humidity, "[percent] Relative humidity");
            }

            if (!double.IsNaN(metaData.WeatherData.Pressure)) {
                Add("PRESSURE", metaData.WeatherData.Pressure, "[hPa] Air pressure");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyBrightness)) {
                Add("SKYBRGHT", metaData.WeatherData.SkyBrightness, "[lux] Sky brightness");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyQuality)) {
                /* fits4win */
                Add("MPSAS", metaData.WeatherData.SkyQuality, "[mags/arcsec^2] Sky quality");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyTemperature)) {
                Add("SKYTEMP", metaData.WeatherData.SkyTemperature, "[degC] Sky temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.StarFWHM)) {
                Add("STARFWHM", metaData.WeatherData.StarFWHM, "Star FWHM");
            }

            if (!double.IsNaN(metaData.WeatherData.Temperature)) {
                Add("AMBTEMP", metaData.WeatherData.Temperature, "[degC] Ambient air temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.WindDirection)) {
                Add("WINDDIR", metaData.WeatherData.WindDirection, "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W");
            }

            if (!double.IsNaN(metaData.WeatherData.WindGust)) {
                Add("WINDGUST", metaData.WeatherData.WindGust * 3.6, "[kph] Wind gust");
            }

            if (!double.IsNaN(metaData.WeatherData.WindSpeed)) {
                Add("WINDSPD", metaData.WeatherData.WindSpeed * 3.6, "[kph] Wind speed");
            }

            //ROWORDER as proposed by Siril at https://free-astro.org/index.php?title=Siril:FITS_orientation
            Add("ROWORDER", "TOP-DOWN", "FITS Image Orientation");

            Add("EQUINOX", 2000.0d, "Equinox of celestial coordinate system");
            Add("SWCREATE", string.Format("N.I.N.A. {0} ({1})", CoreUtil.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");

            foreach(var elem in metaData.GenericHeaders) {
                switch(elem) {
                    case StringMetaDataHeader s:
                        Add(s.Key, s.Value, s.Comment);
                        break;
                    case IntMetaDataHeader i:
                        Add(i.Key, i.Value, i.Comment);
                        break;
                    case DoubleMetaDataHeader d:
                        Add(d.Key, d.Value, d.Comment);
                        break;
                    case BoolMetaDataHeader b:
                        Add(b.Key, b.Value, b.Comment);
                        break;
                    case DateTimeMetaDataHeader d:
                        Add(d.Key, d.Value, d.Comment);
                        break;
                }
                
            }
        }
    }
}