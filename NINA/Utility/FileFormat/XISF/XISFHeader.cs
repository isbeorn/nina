#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Utility.Astrometry;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NINA.Utility.FileFormat.XISF {
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
            AddMetaDataProperty(XISFMetaDataProperty.XISF.CreatorApplication, Utility.Title);

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

        public int ByteCount => Encoding.UTF8.GetByteCount(Content.ToString());

        public ImageMetaData ExtractMetaData() {
            var metaData = new ImageMetaData();

            if (TryGetImageProperty(XISFImageProperty.Observation.Time.Start, out var value)) {
                metaData.Image.ExposureStart = DateTime.Parse(value);
            }

            if (TryGetImageProperty(XISFImageProperty.Instrument.ExposureTime, out value)) {
                metaData.Image.ExposureTime = double.Parse(value, CultureInfo.InvariantCulture);
            }

            /* Camera */
            if (TryGetImageProperty(XISFImageProperty.Instrument.Camera.Name, out value)) {
                metaData.Camera.Name = value;
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
                metaData.Camera.SetPoint = double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (TryGetFITSProperty("SET-TEMP", out value)) {
                metaData.Camera.Gain = int.Parse(value, CultureInfo.InvariantCulture);
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

            /* Target */
            if (TryGetImageProperty(XISFImageProperty.Observation.Object.Name, out value)) {
                metaData.Target.Name = value;
            }

            if (TryGetImageProperty(XISFImageProperty.Observation.Object.RA, out value)) {
                var ra = double.Parse(value, CultureInfo.InvariantCulture);
                if (TryGetImageProperty(XISFImageProperty.Observation.Object.Dec, out value)) {
                    var dec = double.Parse(value, CultureInfo.InvariantCulture);
                    metaData.Telescope.Coordinates = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000);
                }
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
            if (elements.Count() == 0) { return false; }

            var elem = elements.FirstOrDefault(el => el.Attribute("name")?.Value == key);
            if (elem == null) { return false; }

            value = elem.Attribute("value").Value;

            if (value.StartsWith("'")) {
                value = value.Trim();
                value = value.Remove(value.Length - 1, 1).Remove(0, 1).Replace(@"''", @"'");
            }

            return true;
        }

        public void Populate(ImageMetaData metaData) {
            if (metaData.Image.ExposureStart > DateTime.MinValue) {
                AddImageProperty(XISFImageProperty.Observation.Time.Start, metaData.Image.ExposureStart.ToUniversalTime().ToString(@"yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture), "Time of observation (UTC)");
                AddImageFITSKeyword("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime().ToString(@"yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture), "Time of observation (local)");
            }

            if (!double.IsNaN(metaData.Image.ExposureTime)) {
                AddImageProperty(XISFImageProperty.Instrument.ExposureTime, metaData.Image.ExposureTime.ToString(CultureInfo.InvariantCulture), "[s] Exposure duration");
                AddImageFITSKeyword("EXPTIME", metaData.Image.ExposureTime.ToString(CultureInfo.InvariantCulture), "[s] Exposure duration");
            }

            /* Camera */
            if (!string.IsNullOrWhiteSpace(metaData.Camera.Name)) {
                AddImageProperty(XISFImageProperty.Instrument.Camera.Name, metaData.Camera.Name, "Imaging instrument name");
            }
            if (metaData.Camera.Gain >= 0) {
                AddImageFITSKeyword("GAIN", metaData.Camera.Gain.ToString(CultureInfo.InvariantCulture), "Sensor gain");
            }

            if (metaData.Camera.Offset >= 0) {
                AddImageFITSKeyword("OFFSET", metaData.Camera.Offset.ToString(CultureInfo.InvariantCulture), "Sensor gain offset");
            }

            if (!double.IsNaN(metaData.Camera.ElectronsPerADU)) {
                AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, metaData.Camera.ElectronsPerADU.ToString(CultureInfo.InvariantCulture), "[e-/ADU] Electrons per A/D unit");
            }

            if (metaData.Camera.BinX > 0) {
                AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, metaData.Camera.BinX.ToString(CultureInfo.InvariantCulture), "X axis binning factor");
            }
            if (metaData.Camera.BinY > 0) {
                AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, metaData.Camera.BinY.ToString(CultureInfo.InvariantCulture), "Y axis binning factor");
            }

            if (!double.IsNaN(metaData.Camera.SetPoint)) {
                AddImageFITSKeyword("SET-TEMP", metaData.Camera.SetPoint.ToString(CultureInfo.InvariantCulture), "[degC] CCD temperature setpoint");
            }

            if (!double.IsNaN(metaData.Camera.Temperature)) {
                AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, metaData.Camera.Temperature.ToString(CultureInfo.InvariantCulture), "[degC] CCD temperature");
            }

            if (!double.IsNaN(metaData.Camera.PixelSize)) {
                double pixelX = metaData.Camera.PixelSize * Math.Max(metaData.Camera.BinX, 1);
                double pixelY = metaData.Camera.PixelSize * Math.Max(metaData.Camera.BinY, 1);
                AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, pixelX.ToString(CultureInfo.InvariantCulture), "[um] Pixel X axis size");
                AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, pixelY.ToString(CultureInfo.InvariantCulture), "[um] Pixel Y axis size");
            }

            if (!string.IsNullOrWhiteSpace(metaData.Camera.ReadoutModeName)) {
                AddImageFITSKeyword("READOUTM", metaData.Camera.ReadoutModeName.ToString(CultureInfo.InvariantCulture), "Sensor readout mode");
            }

            if (metaData.Camera.SensorType != SensorType.Monochrome) {
                AddImageFITSKeyword("BAYERPAT", metaData.Camera.SensorType.ToString().ToUpper(), "Sensor Bayer pattern");
                AddImageFITSKeyword("XBAYROFF", metaData.Camera.BayerOffsetX.ToString(CultureInfo.InvariantCulture), "Bayer pattern X axis offset");
                AddImageFITSKeyword("YBAYROFF", metaData.Camera.BayerOffsetY.ToString(CultureInfo.InvariantCulture), "Bayer pattern Y axis offset");

                /*
                 * Add XISF ColorFilterArray element. We support only 2x2 bayer patterns for now.
                 */
                AddCfaAttribute(metaData.Camera.SensorType.ToString().ToUpper(), 2, 2);
            }

            /* Observer */
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                AddImageProperty(XISFImageProperty.Observation.Location.Elevation, metaData.Observer.Elevation.ToString(CultureInfo.InvariantCulture), "[m] Observation site elevation");
            }
            if (!double.IsNaN(metaData.Observer.Latitude)) {
                AddImageProperty(XISFImageProperty.Observation.Location.Latitude, metaData.Observer.Latitude.ToString(CultureInfo.InvariantCulture), "[deg] Observation site latitude");
            }
            if (!double.IsNaN(metaData.Observer.Longitude)) {
                AddImageProperty(XISFImageProperty.Observation.Location.Longitude, metaData.Observer.Longitude.ToString(CultureInfo.InvariantCulture), "[deg] Observation site longitude");
            }

            /* Telescope */
            if (!string.IsNullOrWhiteSpace(metaData.Telescope.Name)) {
                AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, metaData.Telescope.Name.ToString(CultureInfo.InvariantCulture), "Name of telescope");
            }
            if (!double.IsNaN(metaData.Telescope.FocalLength) && metaData.Telescope.FocalLength > 0) {
                AddImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, (metaData.Telescope.FocalLength / 1e3).ToString(CultureInfo.InvariantCulture), "[m] Focal Length");
                AddImageFITSKeyword("FOCALLEN", metaData.Telescope.FocalLength.ToString(CultureInfo.InvariantCulture), "[mm] Focal length");

                if (!double.IsNaN(metaData.Telescope.FocalRatio) && metaData.Telescope.FocalRatio > 0) {
                    double aperture = (metaData.Telescope.FocalLength / metaData.Telescope.FocalRatio) / 1e3;
                    AddImageProperty(XISFImageProperty.Instrument.Telescope.Aperture, aperture.ToString(CultureInfo.InvariantCulture), "[m] Aperture", false);
                    AddImageFITSKeyword("FOCRATIO", metaData.Telescope.FocalRatio.ToString(CultureInfo.InvariantCulture), "Focal ratio");
                }
            }

            if (metaData.Telescope.Coordinates != null) {
                AddImageProperty(XISFImageProperty.Observation.Center.RA, metaData.Telescope.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), "[deg] RA of telescope");
                AddImageProperty(XISFImageProperty.Observation.Center.Dec, metaData.Telescope.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), "[deg] Declination of telescope");
            }

            /* Target */
            if (!string.IsNullOrWhiteSpace(metaData.Target.Name)) {
                AddImageProperty(XISFImageProperty.Observation.Object.Name, metaData.Target.Name, "Name of the object of interest");
            }

            if (metaData.Target.Coordinates != null) {
                AddImageProperty(XISFImageProperty.Observation.Object.RA, metaData.Target.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), "[deg] RA of imaged object", false);
                AddImageFITSKeyword(XISFImageProperty.Observation.Object.RA[2], Astrometry.Astrometry.HoursToFitsHMS(metaData.Target.Coordinates.RA), "[H M S] RA of imaged object");
                AddImageProperty(XISFImageProperty.Observation.Object.Dec, metaData.Target.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), "[deg] Declination of imaged object", false);
                AddImageFITSKeyword(XISFImageProperty.Observation.Object.Dec[2], Astrometry.Astrometry.DegreesToFitsDMS(metaData.Target.Coordinates.Dec), "[D M S] Declination of imaged object");
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
            if (!double.IsNaN(metaData.Focuser.Position)) {
                if (!double.IsNaN(metaData.Focuser.StepSize)) {
                    /* steps * step size (microns) converted to millimetres, single-precision float */
                    float focusDistance = (float)((metaData.Focuser.Position * metaData.Focuser.StepSize) / 1000.0);
                    AddImageProperty(XISFImageProperty.Instrument.Focuser.Position, focusDistance.ToString(CultureInfo.InvariantCulture));
                }

                /* fits4win, SGP */
                AddImageFITSKeyword("FOCPOS", metaData.Focuser.Position.ToString(CultureInfo.InvariantCulture), "[step] Focuser position");

                /* MaximDL, several observatories */
                AddImageFITSKeyword("FOCUSPOS", metaData.Focuser.Position.ToString(CultureInfo.InvariantCulture), "[step] Focuser position");
            }

            if (!double.IsNaN(metaData.Focuser.StepSize)) {
                /* MaximDL */
                AddImageFITSKeyword("FOCUSSZ", metaData.Focuser.StepSize.ToString(CultureInfo.InvariantCulture), "[um] Focuser step size");
            }

            if (!double.IsNaN(metaData.Focuser.Temperature)) {
                /* fits4win, SGP */
                AddImageFITSKeyword("FOCTEMP", metaData.Focuser.Temperature.ToString(CultureInfo.InvariantCulture), "[degC] Focuser temperature");

                /* MaximDL, several observatories */
                AddImageFITSKeyword("FOCUSTEM", metaData.Focuser.Temperature.ToString(CultureInfo.InvariantCulture), "[degC] Focuser temperature");
            }

            /* Rotator */
            if (!string.IsNullOrWhiteSpace(metaData.Rotator.Name)) {
                /* NINA */
                AddImageFITSKeyword("ROTNAME", metaData.Rotator.Name, "Rotator equipment name");
            }

            if (!double.IsNaN(metaData.Rotator.Position)) {
                /* fits4win */
                AddImageFITSKeyword("ROTATOR", metaData.Rotator.Position.ToString(CultureInfo.InvariantCulture), "[deg] Rotator angle");

                /* MaximDL, several observatories */
                AddImageFITSKeyword("ROTATANG", metaData.Rotator.Position.ToString(CultureInfo.InvariantCulture), "[deg] Rotator angle");
            }

            if (!double.IsNaN(metaData.Rotator.StepSize)) {
                /* NINA */
                AddImageFITSKeyword("ROTSTPSZ", metaData.Rotator.StepSize.ToString(CultureInfo.InvariantCulture), "[deg] Rotator step size");
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
                AddImageFITSKeyword("CLOUDCVR", metaData.WeatherData.CloudCover.ToString(CultureInfo.InvariantCulture), "[percent] Cloud cover");
            }

            if (!double.IsNaN(metaData.WeatherData.DewPoint)) {
                AddImageFITSKeyword("DEWPOINT", metaData.WeatherData.DewPoint.ToString(CultureInfo.InvariantCulture), "[degC] Dew point");
            }

            if (!double.IsNaN(metaData.WeatherData.Humidity)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.RelativeHumidity, metaData.WeatherData.Humidity.ToString(CultureInfo.InvariantCulture), "[percent] Relative humidity");
            }

            if (!double.IsNaN(metaData.WeatherData.Pressure)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.AtmosphericPressure, metaData.WeatherData.Pressure.ToString(CultureInfo.InvariantCulture), "[hPa] Air pressure");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyBrightness)) {
                AddImageFITSKeyword("SKYBRGHT", metaData.WeatherData.SkyBrightness.ToString(CultureInfo.InvariantCulture), "[lux] Sky brightness");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyQuality)) {
                /* fits4win */
                AddImageFITSKeyword("MPSAS", metaData.WeatherData.SkyQuality.ToString(CultureInfo.InvariantCulture), "[mags/arcsec^2] Sky quality");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyTemperature)) {
                AddImageFITSKeyword("SKYTEMP", metaData.WeatherData.SkyTemperature.ToString(CultureInfo.InvariantCulture), "[degC] Sky temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.StarFWHM)) {
                AddImageFITSKeyword("STARFWHM", metaData.WeatherData.StarFWHM.ToString(CultureInfo.InvariantCulture), "Star FWHM");
            }

            if (!double.IsNaN(metaData.WeatherData.Temperature)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.AmbientTemperature, metaData.WeatherData.Temperature.ToString(CultureInfo.InvariantCulture), "[degC] Ambient air temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.WindDirection)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.WindDirection, metaData.WeatherData.WindDirection.ToString(CultureInfo.InvariantCulture), "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W");
            }

            if (!double.IsNaN(metaData.WeatherData.WindGust)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.WindGust, (metaData.WeatherData.WindGust * 3.6).ToString(CultureInfo.InvariantCulture), "[kph] Wind gust");
            }

            if (!double.IsNaN(metaData.WeatherData.WindSpeed)) {
                AddImageProperty(XISFImageProperty.Observation.Meteorology.WindSpeed, (metaData.WeatherData.WindSpeed * 3.6).ToString(CultureInfo.InvariantCulture), "[kph] Wind speed");
            }

            AddImageFITSKeyword("SWCREATE", string.Format("N.I.N.A. {0} ({1})", Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");
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
        public void AddImageProperty(string[] property, string value, string comment = "", bool autoaddfits = true) {
            if (Image == null) { throw new InvalidOperationException("No Image component available to add property!"); }
            AddProperty(Image, property, value, comment);
            if (property.Length > 2 && autoaddfits) {
                AddImageFITSKeyword(property[2], value, comment);
            }
        }

        public void AddImageFITSKeyword(string name, string value, string comment = "") {
            if (Image == null) { throw new InvalidOperationException("No Image component available to add FITS Keyword!"); }
            Image.Add(new XElement(xmlns + "FITSKeyword",
                        new XAttribute("name", name),
                        new XAttribute("value", RemoveInvalidXMLChars(value)),
                        new XAttribute("comment", comment)));
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
        public void AddImageMetaData(ImageProperties imageProperties, string imageType) {
            if (imageType == "SNAPSHOT") { imageType = "LIGHT"; }

            XElement image = new XElement(xmlns + "Image",
                    new XAttribute("geometry", imageProperties.Width + ":" + imageProperties.Height + ":" + "1"),
                    new XAttribute("sampleFormat", "UInt16"),
                    new XAttribute("imageType", imageType),
                    new XAttribute("colorSpace", "Gray")
                    );

            Image = image;
            Xisf.Add(image);
            AddImageFITSKeyword("IMAGETYP", imageType, "Type of exposure");
        }

        public void Save(Stream s) {
            using (System.Xml.XmlWriter sw = System.Xml.XmlWriter.Create(s, new System.Xml.XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.UTF8 })) {
                Content.Save(sw);
            }
        }
    }
}