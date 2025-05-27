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
using NINA.Image.ImageData;
using NINA.Astrometry;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NINA.Core.Utility;
using System.Collections.Generic;

namespace NINA.Image.FileFormat.XISF {
    /*
     * Specifications: http://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html#xisf_header
     */

    public class XISFHeader {
        public XDocument Content { get; private set; }
        public XElement MetaData { get; private set; }
        public XElement Image { get; private set; }
        public uint Size { get; private set; }

        private XElement Xisf;
        private XNamespace xmlns = XNamespace.Get("http://www.pixinsight.com/xisf");
        private XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        /// <summary>
        /// Create a new XISF Header
        /// </summary>
        public XISFHeader() {
            Xisf = new XElement(xmlns + "xisf",
                    new XAttribute("version", "1.0"),
                    new XAttribute("xmlns", "http://www.pixinsight.com/xisf"),
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(xsi + "schemaLocation", "http://www.pixinsight.com/xisf http://pixinsight.com/xisf/xisf-1.0.xsd")
            );

            MetaData = new XElement(xmlns + "Metadata");

            AddMetaDataProperty(XISFMetaDataProperty.XISF.CreationTime, DateTime.UtcNow.ToString("o"));
            AddMetaDataProperty(XISFMetaDataProperty.XISF.CreatorApplication, CoreUtil.Title);

            Xisf.Add(MetaData);

            Content = new XDocument(
                new XDeclaration("1.0", "UTF-8", null), 
                Xisf
            );
        }

        /// <summary>
        /// Creates a XISF Header out of an existing XISF xml
        /// </summary>
        /// <param name="header"></param>
        public XISFHeader(XElement header) : this() {
            MetaData = header.Element(xmlns + "Metadata");
            if (MetaData == null) {
                MetaData = header.Elements().FirstOrDefault(x => x.Name?.LocalName == "Metadata");
            }

            Image = header.Element(xmlns + "Image");
            if (Image == null) {
                Image = header.Elements().FirstOrDefault(x => x.Name?.LocalName == "Image");
            }

            Xisf.Add(MetaData);
            Xisf.Add(Image);
        }

        public int ByteCount {
            get {
                using (MemoryStream memoryStream = new MemoryStream()) {
                    Save(memoryStream);
                    return (int)memoryStream.Length;
                }
            }
        }

        public ImageMetaData ExtractMetaData() {
            var metaData = new ImageMetaData();
            metaData.GenericHeaders = GetAllFITSKeywords();

            if (TryGetImageProperty(XISFImageProperty.Observation.Time.Start, out var value)) {
                metaData.Image.ExposureStart = DateTime.Parse(value);
            }

            if (TryGetFITSProperty("DATE-AVG", out value)) {
                metaData.Image.ExposureMidPoint = DateTime.Parse(value);
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.ExposureTime, out value)) {
                metaData.Image.ExposureTime = double.Parse(value, CultureInfo.InvariantCulture);
            }

            /* Camera */
            if (TryGetImageProperty(XISFImageProperty.Instrument.Camera.Name, out value)) {
                metaData.Camera.Name = value;
            }

            if (TryGetFITSProperty("CAMERAID", out value)) {
                metaData.Camera.Id = value;
            }

            if (TryGetFITSProperty("GAIN", out value)) {
                metaData.Camera.Gain = int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetFITSProperty("OFFSET", out value)) {
                metaData.Camera.Offset = int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.Camera.Gain, out value)) {
                metaData.Camera.ElectronsPerADU = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.Camera.XBinning, out value)) {
                metaData.Camera.BinX = int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.Camera.YBinning, out value)) {
                metaData.Camera.BinY = int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, out value)) {
                metaData.Camera.Temperature = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetFITSProperty("SET-TEMP", out value)) {
                metaData.Camera.SetPoint = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, out value)) {
                metaData.Camera.PixelSize = double.Parse(value, CultureInfo.InvariantCulture) / metaData.Camera.BinX;
            }

            if (TryGetFITSProperty("READOUTM", out value)) {
                metaData.Camera.ReadoutModeName = value;
            }

            if (TryGetFITSProperty("BAYERPAT", out value)) {
                metaData.Camera.SensorType = metaData.StringToSensorType(value);
            }

            if (TryGetFITSProperty("XBAYEROFF", out value)) {
                metaData.Camera.BayerOffsetX = int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetFITSProperty("YBAYEROFF", out value)) {
                metaData.Camera.BayerOffsetY = int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetFITSProperty("USBLIMIT", out value)) {
                metaData.Camera.USBLimit = int.Parse(value, CultureInfo.InvariantCulture);
            }

            /* Observer */
            if (TryGetImageProperty(XISFImageProperty.Observation.Location.Elevation, out value)) {
                metaData.Observer.Elevation = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Observation.Location.Latitude, out value)) {
                metaData.Observer.Latitude = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Observation.Location.Longitude, out value)) {
                metaData.Observer.Longitude = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Observation.Location.Name, out value)) {
                metaData.Observer.Site = value;
            }

            if (TryGetFITSProperty("OBSERVAT", out value)) {
                metaData.Observer.Observatory = value;
            }

            if (TryGetImageProperty(XISFImageProperty.Observer.Name, out value)) {
                metaData.Observer.Name = value;
            }

            /* Telescope */
            if (TryGetImageProperty(XISFImageProperty.Instrument.Telescope.Name, out value)) {
                metaData.Telescope.Name = value;
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, out value)) {
                metaData.Telescope.FocalLength = double.Parse(value, CultureInfo.InvariantCulture) * 1e3;
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.Telescope.Aperture, out value)) {
                metaData.Telescope.FocalRatio = double.Parse(value, CultureInfo.InvariantCulture) * 1e3 / metaData.Telescope.FocalLength;
            }

            if (TryGetImageProperty(XISFImageProperty.Observation.Center.RA, out value)) {
                var ra = double.Parse(value, CultureInfo.InvariantCulture);
                if (TryGetImageProperty(XISFImageProperty.Observation.Center.Dec, out value)) {
                    var dec = double.Parse(value, CultureInfo.InvariantCulture);
                    metaData.Telescope.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                }
            }

            if (TryGetFITSProperty("PIERSIDE", out value)) {
                metaData.Telescope.SideOfPier = ParsePierSide(value);
            }

            /* Target */
            if (TryGetImageProperty(XISFImageProperty.Observation.Object.Name, out value)) {
                metaData.Target.Name = value;
            }

            if (TryGetFITSProperty("OBJCTROT", out value)) {
                metaData.Target.PositionAngle = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetImageProperty(XISFImageProperty.Observation.Object.RA, out value)) {
                var ra = double.Parse(value, CultureInfo.InvariantCulture);
                if (TryGetImageProperty(XISFImageProperty.Observation.Object.Dec, out value)) {
                    var dec = double.Parse(value, CultureInfo.InvariantCulture);
                    metaData.Telescope.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                }
            }

            if (TryGetFITSProperty("CENTALT", out value) || TryGetFITSProperty("OBJCTALT", out value)) {
                metaData.Telescope.Altitude = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetFITSProperty("CENTAZ", out value) || TryGetFITSProperty("OBJCTAZ", out value)) {
                metaData.Telescope.Azimuth = double.Parse(value, CultureInfo.InvariantCulture);
            }

            /* Focuser */
            if (TryGetFITSProperty("FOCNAME", out value)) {
                metaData.Focuser.Name = value;
            }

            /* Filter */
            if (TryGetImageProperty(XISFImageProperty.Instrument.Filter.Name, out value)) {
                metaData.FilterWheel.Filter = value;
            }

            /* Weather Data */
            if (TryGetImageProperty(XISFImageProperty.Observation.Meteorology.RelativeHumidity, out value)) {
                metaData.WeatherData.Humidity = double.Parse(value, CultureInfo.InvariantCulture);
            }
            if (TryGetImageProperty(XISFImageProperty.Observation.Meteorology.AtmosphericPressure, out value)) {
                metaData.WeatherData.Pressure = double.Parse(value, CultureInfo.InvariantCulture);
            }
            if (TryGetImageProperty(XISFImageProperty.Observation.Meteorology.AmbientTemperature, out value)) {
                metaData.WeatherData.Temperature = double.Parse(value, CultureInfo.InvariantCulture);
            }
            if (TryGetImageProperty(XISFImageProperty.Observation.Meteorology.WindDirection, out value)) {
                metaData.WeatherData.WindDirection = double.Parse(value, CultureInfo.InvariantCulture);
            }
            if (TryGetImageProperty(XISFImageProperty.Observation.Meteorology.WindGust, out value)) {
                metaData.WeatherData.WindGust = double.Parse(value, CultureInfo.InvariantCulture);
            }
            if (TryGetImageProperty(XISFImageProperty.Observation.Meteorology.WindSpeed, out value)) {
                metaData.WeatherData.WindSpeed = double.Parse(value, CultureInfo.InvariantCulture);
            }

            /* WCS */

            if (TryGetFITSProperty("CTYPE1", out var ctype1) && TryGetFITSProperty("CTYPE2", out var ctype2)) {
                if (ctype1 == "RA---TAN" && ctype2 == "DEC--TAN") {
                    if (TryGetFITSProperty("CRPIX1", out var CRPIX1Value)
                      && TryGetFITSProperty("CRPIX2", out var CRPIX2Value)
                      && TryGetFITSProperty("CRVAL1", out var CRVAL1Value)
                      && TryGetFITSProperty("CRVAL2", out var CRVAL2Value)
                    ) {
                        var crPix1 = double.Parse(CRPIX1Value, CultureInfo.InvariantCulture);
                        var crPix2 = double.Parse(CRPIX2Value, CultureInfo.InvariantCulture);
                        var crVal1 = double.Parse(CRVAL1Value, CultureInfo.InvariantCulture);
                        var crVal2 = double.Parse(CRVAL2Value, CultureInfo.InvariantCulture);

                        if (TryGetFITSProperty("CD1_1", out var CD1_1Value)
                            && TryGetFITSProperty("CD2_1", out var CD2_1Value)
                            && TryGetFITSProperty("CD1_2", out var CD1_2Value)
                            && TryGetFITSProperty("CD2_2", out var CD2_2Value)

                        ) {
                            // CDn_m notation
                            var cd1_1 = double.Parse(CD1_1Value, CultureInfo.InvariantCulture);
                            var cd2_1 = double.Parse(CD2_1Value, CultureInfo.InvariantCulture);
                            var cd1_2 = double.Parse(CD1_2Value, CultureInfo.InvariantCulture);
                            var cd2_2 = double.Parse(CD2_2Value, CultureInfo.InvariantCulture);
                            var wcs = new WorldCoordinateSystem(crVal1, crVal2, crPix1, crPix2, cd1_1, cd1_2, cd2_1, cd2_2);
                            metaData.WorldCoordinateSystem = wcs;
                        } else if (TryGetFITSProperty("CDELT1", out var CDELT1Value)
                                && TryGetFITSProperty("CDELT2", out var CDELT2Value)
                                && TryGetFITSProperty("CROTA2", out var CROTA2Value)
                            ) {
                            // Older CROTA2 notation
                            var cdelt1 = double.Parse(CDELT1Value, CultureInfo.InvariantCulture);
                            var cdelt2 = double.Parse(CDELT2Value, CultureInfo.InvariantCulture);
                            var crota2 = double.Parse(CROTA2Value, CultureInfo.InvariantCulture);
                            var wcs = new WorldCoordinateSystem(crVal1, crVal2, crPix1, crPix2, cdelt1, cdelt2, crota2);
                            metaData.WorldCoordinateSystem = wcs;
                        } else {
                            Logger.Debug("XISF WCS - No CROTA2 or CDn_m keywords found");
                        }
                    } else {
                        Logger.Debug("XISF WCS - No CRPIX and CRVAL keywords found");
                    }
                } else {
                    Logger.Debug($"XISF WCS - Incompatible projection found {ctype1} {ctype2}");
                }
            }

            return metaData;
        }

        /// <summary>
        /// Retrieves a XISF Image Property from the Image Header
        /// </summary>
        /// <param name="property">[key, type] of property</param>
        /// <param name="value">value for the given key, if available</param>
        /// <returns>True if successful, false if invalid input or no info found for given key</returns>
        private bool TryGetImageProperty(string[] property, out string value) {
            value = string.Empty;
            if (property?.Length < 2) { return false; }

            string id = property[0];
            string type = property[1];

            var elem = Image.Descendants().FirstOrDefault(el => el.Attribute("id")?.Value == id);
            if (elem == null) { return false; }

            if (type == "String") {
                value = elem.Value;
            } else {
                if (elem.Attribute("value") == null) {
                    return false;
                }
                value = elem.Attribute("value").Value;
            }

            return true;
        }

        /// <summary>
        /// Retrieves a FITS Image Keyword from the Image Header
        /// </summary>
        /// <param name="key">FITS Keyword</param>
        /// <param name="value">value for the given key, if available</param>
        /// <returns>True if successful, false if invalid input or no info found for given key</returns>
        private bool TryGetFITSProperty(string key, out string value) {
            value = string.Empty;
            var elements = Image.Elements(xmlns + "FITSKeyword");
            if (!elements.Any()) { return false; }

            var elem = elements.FirstOrDefault(el => el.Attribute("name")?.Value == key);
            if (elem == null) { return false; }

            value = elem.Attribute("value").Value;

            if (value.StartsWith("'")) {
                value = value.Trim();
                value = value.Remove(value.Length - 1, 1).Remove(0, 1).Replace(@"''", @"'");
            }

            return true;
        }

        private List<IGenericMetaDataHeader> GetAllFITSKeywords() {
            var l = new List<IGenericMetaDataHeader>();
            var elements = Image.Elements(xmlns + "FITSKeyword");
            if (!elements.Any()) { return l; }

            foreach (var elem in elements) {
                if (elem == null) { continue; }

                var key = elem.Attribute("name")?.Value;
                if (key == null) { continue; }

                var value = elem.Attribute("value").Value;
                var comment = elem.Attribute("comment")?.Value ?? string.Empty;


                if (value.StartsWith("'")) {
                    value = value.Trim();
                    value = value.Remove(value.Length - 1, 1).Remove(0, 1).Replace(@"''", @"'");
                    l.Add(new StringMetaDataHeader(key, value, comment));
                } else if (value == "T" || value == "F") {
                    var boolean = value.Trim() == "T" ? true : false;
                    l.Add(new BoolMetaDataHeader(key, boolean, comment));
                } else if (value.Contains(".") && double.TryParse(value, CultureInfo.InvariantCulture, out var number)) {
                    l.Add(new DoubleMetaDataHeader(key, number, comment));
                } else if (int.TryParse(value, out var integer)) {
                    l.Add(new IntMetaDataHeader(key, integer, comment));
                }
            }
            return l;
        }

        public void Populate(ImageMetaData metaData) {
            if (metaData.Image.ExposureStart > DateTime.MinValue) {
                AddImageProperty(XISFImageProperty.Observation.Time.Start, metaData.Image.ExposureStart.ToUniversalTime(), "Time of observation (UTC)");
                AddImageFITSKeyword("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime(), "Time of observation (local)");
            }

            if (metaData.Image.ExposureMidPoint > DateTime.MinValue) {
                AddImageFITSKeyword("DATE-AVG", metaData.Image.ExposureMidPoint, "Averaged midpoint time (UTC)");
            }

            if (!double.IsNaN(metaData.Image.ExposureTime)) {
                AddImageProperty(XISFImageProperty.Instrument.ExposureTime, metaData.Image.ExposureTime, "[s] Exposure duration");
                AddImageFITSKeyword("EXPTIME", metaData.Image.ExposureTime, "[s] Exposure duration");
            }

            /* Camera */
            if (!string.IsNullOrWhiteSpace(metaData.Camera.Name)) {
                AddImageProperty(XISFImageProperty.Instrument.Camera.Name, metaData.Camera.Name, "Imaging instrument name");
            }
            if (!string.IsNullOrWhiteSpace(metaData.Camera.Id)) {
                AddImageFITSKeyword("CAMERAID", metaData.Camera.Id, "Imaging instrument identifier");
            }
            if (metaData.Camera.Gain >= 0) {
                AddImageFITSKeyword("GAIN", metaData.Camera.Gain, "Sensor gain");
            }

            if (metaData.Camera.Offset >= 0) {
                AddImageFITSKeyword("OFFSET", metaData.Camera.Offset, "Sensor gain offset");
            }

            if (!double.IsNaN(metaData.Camera.ElectronsPerADU)) {
                AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, metaData.Camera.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit");
            }

            if (metaData.Camera.BinX > 0) {
                AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, metaData.Camera.BinX, "X axis binning factor");
            }

            if (metaData.Camera.BinY > 0) {
                AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, metaData.Camera.BinY, "Y axis binning factor");
            }

            if (!double.IsNaN(metaData.Camera.SetPoint)) {
                AddImageFITSKeyword("SET-TEMP", metaData.Camera.SetPoint, "[degC] CCD temperature setpoint");
            }

            if (!double.IsNaN(metaData.Camera.Temperature)) {
                AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, metaData.Camera.Temperature, "[degC] CCD temperature");
            }

            if (!double.IsNaN(metaData.Camera.PixelSize)) {
                double pixelX = metaData.Camera.PixelSize * Math.Max(metaData.Camera.BinX, 1);
                double pixelY = metaData.Camera.PixelSize * Math.Max(metaData.Camera.BinY, 1);
                AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, pixelX, "[um] Pixel X axis size");
                AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, pixelY, "[um] Pixel Y axis size");
            }

            if (!string.IsNullOrWhiteSpace(metaData.Camera.ReadoutModeName)) {
                AddImageFITSKeyword("READOUTM", metaData.Camera.ReadoutModeName, "Sensor readout mode");
            }

            if (metaData.Camera.SensorType != SensorType.Monochrome && metaData.Camera.BayerPattern != BayerPatternEnum.None) {
                AddImageFITSKeyword("BAYERPAT", metaData.Camera.SensorType.ToString().ToUpper(), "Sensor Bayer pattern");
                AddImageFITSKeyword("XBAYROFF", metaData.Camera.BayerOffsetX, "Bayer pattern X axis offset");
                AddImageFITSKeyword("YBAYROFF", metaData.Camera.BayerOffsetY, "Bayer pattern Y axis offset");

                /*
                 * Add XISF ColorFilterArray element. We support only 2x2 bayer patterns for now.
                 */
                AddCfaAttribute(metaData.Camera.SensorType.ToString().ToUpper(), 2, 2);
            }

            if (metaData.Camera.USBLimit > -1) {
                AddImageFITSKeyword("USBLIMIT", metaData.Camera.USBLimit, "Camera-specific USB setting");
            }

            /* Observer */
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                AddImageProperty(XISFImageProperty.Observation.Location.Elevation, metaData.Observer.Elevation, "[m] Observation site elevation");
            }
            if (!double.IsNaN(metaData.Observer.Latitude)) {
                AddImageProperty(XISFImageProperty.Observation.Location.Latitude, metaData.Observer.Latitude, "[deg] Observation site latitude");
            }
            if (!double.IsNaN(metaData.Observer.Longitude)) {
                AddImageProperty(XISFImageProperty.Observation.Location.Longitude, metaData.Observer.Longitude, "[deg] Observation site longitude");
            }
            if (!string.IsNullOrEmpty(metaData.Observer.Name)) {
                AddImageProperty(XISFImageProperty.Observer.Name, metaData.Observer.Name, "Observer name");
            }
            if (!string.IsNullOrEmpty(metaData.Observer.Site)) {
                AddImageProperty(XISFImageProperty.Observation.Location.Name, metaData.Observer.Site, "Observatory site name");
            }
            if (!string.IsNullOrEmpty(metaData.Observer.Observatory)) {
                AddImageFITSKeyword("OBSERVAT", metaData.Observer.Observatory, "Observatory name");
            }

            /* Telescope */
            if (!string.IsNullOrWhiteSpace(metaData.Telescope.Name)) {
                AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, metaData.Telescope.Name, "Name of telescope");
            }
            if (!double.IsNaN(metaData.Telescope.FocalLength) && metaData.Telescope.FocalLength > 0) {
                AddImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, metaData.Telescope.FocalLength / 1e3, "[m] Focal Length");
                AddImageFITSKeyword("FOCALLEN", metaData.Telescope.FocalLength, "[mm] Focal length");

                if (!double.IsNaN(metaData.Telescope.FocalRatio) && metaData.Telescope.FocalRatio > 0) {
                    double aperture = (metaData.Telescope.FocalLength / metaData.Telescope.FocalRatio) / 1e3;
                    AddImageProperty(XISFImageProperty.Instrument.Telescope.Aperture, aperture, "[m] Aperture", false);
                    AddImageFITSKeyword("FOCRATIO", metaData.Telescope.FocalRatio, "Focal ratio");
                }
            }

            if (metaData.Telescope.Coordinates != null) {
                AddImageProperty(XISFImageProperty.Observation.Center.RA, metaData.Telescope.Coordinates.RADegrees, "[deg] RA of telescope");
                AddImageProperty(XISFImageProperty.Observation.Center.Dec, metaData.Telescope.Coordinates.Dec, "[deg] Declination of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Altitude)) {
                AddImageFITSKeyword("CENTALT", metaData.Telescope.Altitude, "[deg] Altitude of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Azimuth)) {
                AddImageFITSKeyword("CENTAZ", metaData.Telescope.Azimuth, "[deg] Azimuth of telescope");
            }

            if (!double.IsNaN(metaData.Telescope.Airmass)) {
                AddImageFITSKeyword("AIRMASS", metaData.Telescope.Airmass, "Airmass at frame center (Gueymard 1993)");
            }

            if (metaData.Telescope.SideOfPier != PierSide.pierUnknown) {
                string keyword = "PIERSIDE";
                string comment = "Telescope pointing state";

                if (metaData.Telescope.SideOfPier == PierSide.pierEast) {
                    AddImageFITSKeyword(keyword, "East", comment);
                } else if (metaData.Telescope.SideOfPier == PierSide.pierWest) {
                    AddImageFITSKeyword(keyword, "West", comment);
                }
            }

            /* Target */
            if (!string.IsNullOrWhiteSpace(metaData.Target.Name)) {
                AddImageProperty(XISFImageProperty.Observation.Object.Name, metaData.Target.Name, "Name of the object of interest");
            }

            if (metaData.Target.Coordinates != null) {
                AddImageProperty(XISFImageProperty.Observation.Object.RA, metaData.Target.Coordinates.RADegrees, "[deg] RA of imaged object", false);
                AddImageFITSKeyword(XISFImageProperty.Observation.Object.RA[2], AstroUtil.HoursToFitsHMS(metaData.Target.Coordinates.RA), "[H M S] RA of imaged object");
                AddImageProperty(XISFImageProperty.Observation.Object.Dec, metaData.Target.Coordinates.Dec, "[deg] Declination of imaged object", false);
                AddImageFITSKeyword(XISFImageProperty.Observation.Object.Dec[2], AstroUtil.DegreesToFitsDMS(metaData.Target.Coordinates.Dec), "[D M S] Declination of imaged object");
            }

            if (!double.IsNaN(metaData.Target.PositionAngle)) {
                /* NINA Specific target rotation */
                AddImageFITSKeyword("OBJCTROT", metaData.Target.PositionAngle, "[deg] planned rotation of imaged object");
            }

            /* Focuser */
            if (!string.IsNullOrWhiteSpace(metaData.Focuser.Name)) {
                /* fits4win, SGP */
                AddImageFITSKeyword("FOCNAME", metaData.Focuser.Name, "Focusing equipment name");
            }

            /*
             * XISF 1.0 defines Instrument:Focuser:Position as the only focuser-related image property.
             * This image property is: "(Float32) Estimated position of the focuser in millimetres, measured with respect to a device-dependent origin."
             * This unit is different from FOCUSPOS FITSKeyword, so we must do two separate actions: calculate distance from origin in millimetres and insert
             * that as the XISF Instrument:Focuser:Position property, and then insert the separate FOCUSPOS FITSKeyword (measured in steps).
             */
            if (metaData.Focuser.Position.HasValue) {
                if (!double.IsNaN(metaData.Focuser.StepSize)) {
                    /* steps * step size (microns) converted to millimetres, single-precision float */
                    float focusDistance = (float)((metaData.Focuser.Position.Value * metaData.Focuser.StepSize) / 1000.0);
                    AddImageProperty(XISFImageProperty.Instrument.Focuser.Position, focusDistance);
                }

                /* fits4win, SGP */
                AddImageFITSKeyword("FOCPOS", metaData.Focuser.Position.Value, "[step] Focuser position");

                /* MaximDL, several observatories */
                AddImageFITSKeyword("FOCUSPOS", metaData.Focuser.Position.Value, "[step] Focuser position");
            }

            if (!double.IsNaN(metaData.Focuser.StepSize)) {
                /* MaximDL */
                AddImageFITSKeyword("FOCUSSZ", metaData.Focuser.StepSize, "[um] Focuser step size");
            }

            if (!double.IsNaN(metaData.Focuser.Temperature)) {
                /* fits4win, SGP */
                AddImageFITSKeyword("FOCTEMP", metaData.Focuser.Temperature, "[degC] Focuser temperature");

                /* MaximDL, several observatories */
                AddImageFITSKeyword("FOCUSTEM", metaData.Focuser.Temperature, "[degC] Focuser temperature");
            }

            /* Rotator */
            if (!string.IsNullOrWhiteSpace(metaData.Rotator.Name)) {
                /* NINA */
                AddImageFITSKeyword("ROTNAME", metaData.Rotator.Name, "Rotator equipment name");
            }

            if (!double.IsNaN(metaData.Rotator.MechanicalPosition)) {
                /* fits4win */
                AddImageFITSKeyword("ROTATOR", metaData.Rotator.MechanicalPosition, "[deg] Mechanical rotator angle");

                /* MaximDL, several observatories */
                AddImageFITSKeyword("ROTATANG", metaData.Rotator.MechanicalPosition, "[deg] Mechanical rotator angle");
            }

            if (!double.IsNaN(metaData.Rotator.StepSize)) {
                /* NINA */
                AddImageFITSKeyword("ROTSTPSZ", metaData.Rotator.StepSize, "[deg] Rotator step size");
            }

            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Name)) {
                /* fits4win */
                AddImageFITSKeyword("FWHEEL", metaData.FilterWheel.Name, "Filter Wheel name");
            }

            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Filter)) {
                /* fits4win */
                AddImageProperty(XISFImageProperty.Instrument.Filter.Name, metaData.FilterWheel.Filter, "Active filter name");
            }

            /* Weather Data */
            if (!double.IsNaN(metaData.WeatherData.CloudCover)) {
                AddImageFITSKeyword("CLOUDCVR", metaData.WeatherData.CloudCover, "[percent] Cloud cover");
            }

            if (!double.IsNaN(metaData.WeatherData.DewPoint)) {
                AddImageFITSKeyword("DEWPOINT", metaData.WeatherData.DewPoint, "[degC] Dew point");
            }

            if (!double.IsNaN(metaData.WeatherData.Humidity)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.RelativeHumidity, metaData.WeatherData.Humidity, "[percent] Relative humidity");
            }

            if (!double.IsNaN(metaData.WeatherData.Pressure)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.AtmosphericPressure, metaData.WeatherData.Pressure, "[hPa] Air pressure");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyBrightness)) {
                AddImageFITSKeyword("SKYBRGHT", metaData.WeatherData.SkyBrightness, "[lux] Sky brightness");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyQuality)) {
                /* fits4win */
                AddImageFITSKeyword("MPSAS", metaData.WeatherData.SkyQuality, "[mags/arcsec^2] Sky quality");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyTemperature)) {
                AddImageFITSKeyword("SKYTEMP", metaData.WeatherData.SkyTemperature, "[degC] Sky temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.StarFWHM)) {
                AddImageFITSKeyword("STARFWHM", metaData.WeatherData.StarFWHM, "Star FWHM");
            }

            if (!double.IsNaN(metaData.WeatherData.Temperature)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.AmbientTemperature, metaData.WeatherData.Temperature, "[degC] Ambient air temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.WindDirection)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.WindDirection, metaData.WeatherData.WindDirection, "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W");
            }

            if (!double.IsNaN(metaData.WeatherData.WindGust)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.WindGust, metaData.WeatherData.WindGust * 3.6, "[kph] Wind gust");
            }

            if (!double.IsNaN(metaData.WeatherData.WindSpeed)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.WindSpeed, metaData.WeatherData.WindSpeed * 3.6, "[kph] Wind speed");
            }

            AddImageProperty(XISFImageProperty.Observation.Equinox, 2000d, "Equinox of celestial coordinate system");
            AddImageFITSKeyword("SWCREATE", string.Format("N.I.N.A. {0} ({1})", CoreUtil.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");

            foreach (var elem in metaData.GenericHeaders) {
                switch (elem) {
                    case StringMetaDataHeader s:
                        AddImageFITSKeyword(s.Key, s.Value, s.Comment);
                        break;
                    case IntMetaDataHeader i:
                        AddImageFITSKeyword(i.Key, i.Value, i.Comment);
                        break;
                    case DoubleMetaDataHeader d:
                        AddImageFITSKeyword(d.Key, d.Value, d.Comment);
                        break;
                    case BoolMetaDataHeader b:
                        AddImageFITSKeyword(b.Key, b.Value, b.Comment);
                        break;
                    case DateTimeMetaDataHeader d:
                        AddImageFITSKeyword(d.Key, d.Value, d.Comment);
                        break;
                }
            }
        }

        /// <summary>
        /// Add meta data property to file
        /// </summary>
        /// <param name="id">     id</param>
        /// <param name="type">   datatype</param>
        /// <param name="value">  value of that specific property</param>
        /// <param name="comment">optional comment</param>
        public void AddMetaDataProperty(string id, string type, string value, string comment = "") {
            string[] prop = { id, type };
            AddProperty(MetaData, prop, value, comment);
        }

        /// <summary>
        /// Add meta data property to file
        /// </summary>
        /// <param name="property">array of strings as [id, datatype]</param>
        /// <param name="value">   value of that specific property</param>
        /// <param name="comment"> optional comment</param>
        public void AddMetaDataProperty(string[] property, string value, string comment = "") {
            AddProperty(MetaData, property, value, comment);
        }

        /// <summary>
        /// Add an image property to file
        /// </summary>
        /// <param name="property">   array of strings as [id, datatype, fitskey (optional)]</param>
        /// <param name="value">      value of that specific property</param>
        /// <param name="comment">    optional comment</param>
        /// <param name="autoaddfits">default: true; if fitskey available automatically add FITSHeader</param>
        public void AddImagePropertyInternal(string[] property, string value, string comment, bool autoaddfits, string fitsvalue) {
            if (Image == null) { throw new InvalidOperationException("No Image component available to add property!"); }
            AddProperty(Image, property, value, comment);
            if (property.Length > 2 && autoaddfits) {
                if (fitsvalue == null) {
                    AddImageFITSKeywordInternal(property[2], value, comment);
                } else {
                    AddImageFITSKeywordInternal(property[2], fitsvalue, comment);
                }
            }
        }

        public void AddImageProperty(string[] property, String value, string comment = "", bool autoaddfits = true) {
            AddImagePropertyInternal(property, value, comment, autoaddfits, "'" + value + "'");
        }

        public void AddImageProperty(string[] property, DateTime value, string comment = "", bool autoaddfits = true) {
            AddImagePropertyInternal(property, value.ToString(@"yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture), comment, autoaddfits, "'" + value.ToString(@"yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "'");
        }

        public void AddImageProperty(string[] property, int value, string comment = "", bool autoaddfits = true) {
            AddImagePropertyInternal(property, value.ToString(CultureInfo.InvariantCulture), comment, autoaddfits, value.ToString(CultureInfo.InvariantCulture));
        }

        public void AddImageProperty(string[] property, double value, string comment = "", bool autoaddfits = true) {
            AddImagePropertyInternal(property, value.ToString(CultureInfo.InvariantCulture), comment, autoaddfits, DoubleToFitsString(value));
        }

        public void AddImageProperty(string[] property, float value, string comment = "", bool autoaddfits = true) {
            AddImagePropertyInternal(property, value.ToString(CultureInfo.InvariantCulture), comment, autoaddfits, FloatToFitsString(value));
        }

        private void AddImageFITSKeywordInternal(string name, string value, string comment = "") {
            if (Image == null) { throw new InvalidOperationException("No Image component available to add FITS Keyword!"); }

            var exists = Image.Elements(xmlns + "FITSKeyword").Attributes("name").Any(x => x.Value == name);
            if (!exists) {
                Image.Add(new XElement(xmlns + "FITSKeyword",
                            new XAttribute("name", name),
                            new XAttribute("value", RemoveInvalidXMLChars(value)),
                            new XAttribute("comment", comment)));
            }
        }

        public void AddImageFITSKeyword(string name, string value, string comment = "") {
            AddImageFITSKeywordInternal(name, "'" + value + "'", comment);
        }
        public void AddImageFITSKeyword(string name, bool value, string comment = "") {
            AddImageFITSKeywordInternal(name, value ? "T" : "F", comment);
        }

        public void AddImageFITSKeyword(string name, DateTime value, string comment = "") {
            AddImageFITSKeywordInternal(name, "'" + value.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "'", comment);
        }

        public void AddImageFITSKeyword(string name, int value, string comment = "") {
            AddImageFITSKeywordInternal(name, value.ToString(CultureInfo.InvariantCulture), comment);
        }

        public void AddImageFITSKeyword(string name, double value, string comment = "") {
            AddImageFITSKeywordInternal(name, DoubleToFitsString(value), comment);
        }

        public void AddImageFITSKeyword(string name, float value, string comment = "") {
            AddImageFITSKeywordInternal(name, FloatToFitsString(value), comment);
        }

        private string DoubleToFitsString(double value) {
            return value.ToString("0.0##############", CultureInfo.InvariantCulture);
        }

        private string FloatToFitsString(float value) {
            return value.ToString("0.0##############", CultureInfo.InvariantCulture);
        }

        private void AddProperty(XElement elem, string[] property, string value, string comment = "") {
            if (property?.Length < 2 || elem == null) {
                return;
            }
            string id = property[0];
            string type = property[1];
            XElement xelem;

            if (type == "String") {
                xelem = new XElement(xmlns + "Property",
                    new XAttribute("id", id),
                    new XAttribute("type", type),
                    new XAttribute("comment", comment),
                    RemoveInvalidXMLChars(value)
                );
            } else {
                xelem = new XElement(xmlns + "Property",
                    new XAttribute("id", id),
                    new XAttribute("type", type),
                    new XAttribute("comment", comment),
                    new XAttribute("value", RemoveInvalidXMLChars(value))
                );
            }
            elem.Add(xelem);
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

        public void AddCfaAttribute(string cfaPattern, int cfaWidth, int cfaHeight) {
            if (Image == null) { throw new InvalidOperationException("No Image component available to add CFA attribute!"); }
            Image.Add(new XElement(xmlns + "ColorFilterArray",
                        new XAttribute("pattern", cfaPattern),
                        new XAttribute("width", cfaWidth),
                        new XAttribute("height", cfaHeight),
                        new XAttribute("name", cfaPattern + " Bayer Filter")));
        }

        // filters control characters but allows only properly-formed surrogate sequences
        private static Regex _invalidXMLChars = new Regex(
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
            RegexOptions.Compiled);

        /// <summary>
        /// removes any unusual unicode characters that can't be encoded into XML
        /// </summary>
        public static string RemoveInvalidXMLChars(string text) {
            if (string.IsNullOrEmpty(text)) return "";
            return _invalidXMLChars.Replace(text, "�");
        }

        /// <summary>
        /// Adds the image metadata to the header
        /// Image data has to be added at a later point to the xisf body
        /// </summary>
        /// <param name="imageProperties"></param>
        /// <param name="imageType"></param>
        public void AddImageMetaData(ImageProperties imageProperties, string imageType, XISFSampleFormat format = XISFSampleFormat.UInt16) {
            if (imageType == "SNAPSHOT") { imageType = "LIGHT"; } 

            XElement image = new XElement(xmlns + "Image",
                    new XAttribute("geometry", imageProperties.Width + ":" + imageProperties.Height + ":" + "1"),
                    new XAttribute("sampleFormat", format.ToString()),
                    new XAttribute("imageType", imageType),
                    new XAttribute("colorSpace", "Gray")
                    );

            Image = image;
            Xisf.Add(image);
            AddImageFITSKeyword("IMAGETYP", imageType, "Type of exposure");
        }

        public void Save(Stream s) {
            using (System.Xml.XmlWriter sw = System.Xml.XmlWriter.Create(s, new System.Xml.XmlWriterSettings { OmitXmlDeclaration = false, Indent = true, Encoding = new UTF8Encoding(false) })) {
                Content.Save(sw);
            }
        }
    }

    public enum XISFSampleFormat {
        UInt16,
        UInt32
    }
}