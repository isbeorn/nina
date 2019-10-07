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
 * Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using NINA.Model.ImageData;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NINA.Utility {

    internal class XISF {
        public XISFHeader Header { get; private set; }

        public XISFData Data { get; private set; }

        public XISF(XISFHeader header) {
            this.Header = header;
        }

        /// <summary>
        /// The header xml + padding will consist of a muliple of bytes from this size
        /// </summary>
        public int PaddedBlockSize {
            get => 4096;
        }

        public static async Task<ImageData> Load(Uri filePath, bool isBayered) {
            using (FileStream fs = new FileStream(filePath.LocalPath, FileMode.Open)) {
                //Skip to the header length info starting at byte 9
                fs.Seek(8, SeekOrigin.Current);
                byte[] headerLengthInfo = new byte[4];
                fs.Read(headerLengthInfo, 0, 4);
                //Skip the next 4 bytes as they are reserved space
                fs.Seek(4, SeekOrigin.Current);
                var headerLength = BitConverter.ToUInt32(headerLengthInfo, 0);

                byte[] bytes = new byte[headerLength];
                fs.Read(bytes, 0, (int)headerLength);
                string xmlString = System.Text.UTF8Encoding.UTF8.GetString(bytes);

                var xml = XElement.Parse(xmlString);
                var imageTag = xml.Element("Image");
                var geometry = imageTag.Attribute("geometry").Value.Split(':');
                int width = Int32.Parse(geometry[0]);
                int height = Int32.Parse(geometry[1]);
                var metadata = new ImageMetaData();
                metadata.FromXISF(imageTag);

                //Seems to be no attribute to identify the bit depth. Assume 16.
                var bitDepth = 16;
                ImageData imageData = null;
                if (imageTag.Attribute("location").Value.StartsWith("attachment")) {
                    var location = imageTag.Attribute("location").Value.Split(':');
                    var start = int.Parse(location[1]);
                    var size = int.Parse(location[2]);

                    byte[] raw = new byte[size];
                    fs.Seek(start, SeekOrigin.Begin);
                    fs.Read(raw, 0, size);
                    ushort[] img = new ushort[raw.Length / 2];
                    Buffer.BlockCopy(raw, 0, img, 0, raw.Length);

                    imageData = new ImageData(img, width, height, bitDepth, isBayered, metadata);
                } else {
                    var base64Img = xml.Element("Image").Element("Data").Value;
                    byte[] encodedImg = Convert.FromBase64String(base64Img);
                    ushort[] img = new ushort[(int)Math.Ceiling(encodedImg.Length / 2.0)];
                    Buffer.BlockCopy(encodedImg, 0, img, 0, encodedImg.Length);

                    imageData = new ImageData(img, width, height, bitDepth, isBayered, metadata);
                }

                return imageData;
            }
        }

        public void AddAttachedImage(ushort[] data) {
            if (this.Header.Image == null) { throw new InvalidOperationException("No Image Header Information available for attaching image. Add Image Header first!"); }

            var headerLengthBytes = 4;
            var reservedBytes = 4;
            var attachmentInfoMaxBytes = 100;   //Assume max 100 bytes for the attachment:{start}:{length} attribute. Should be more than enough
            var currentHeaderSize = Header.ByteCount + xisfSignature.Length + headerLengthBytes + reservedBytes + attachmentInfoMaxBytes;

            var dataBlockStart = currentHeaderSize + (PaddedBlockSize - currentHeaderSize % PaddedBlockSize);

            //Add Attached data location info to header
            Header.Image.Add(new XAttribute("location", $"attachment:{dataBlockStart}:{data.Length * sizeof(ushort)}"));

            Data = new XISFData(data);
        }

        private byte[] xisfSignature = new byte[] { 88, 73, 83, 70, 48, 49, 48, 48 };

        /// <summary>
        /// Writes monolithic XISF data to stream
        ///
        /// XISF Signature              - 8 byte
        /// Header Length               - 4 byte
        /// Reserved Space              - 4 byte
        /// XISF Header                 - n byte
        /// Padding                     - Fit the above into a multiple of PaddedBlockSize. Remaining space will be padded by zeros
        /// Attached XISF data block    - byte size of image data array
        /// </summary>
        /// <param name="s">Stream to write XISF data to</param>
        /// <returns></returns>
        /// <remarks>https://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html#monolithic_xisf_file</remarks>
        public bool Save(Stream s) {
            /*XISF0100*/
            s.Write(xisfSignature, 0, xisfSignature.Length);

            /*Xml header length */
            var headerlength = BitConverter.GetBytes(Header.ByteCount);
            s.Write(headerlength, 0, headerlength.Length);

            /*reserved space 4 byte must be 0 */
            var reserved = new byte[] { 0, 0, 0, 0 };
            s.Write(reserved, 0, reserved.Length);

            Header.Save(s);

            long remainingBlockPadding = (long)Math.Ceiling((double)s.Position / (double)PaddedBlockSize) * (long)PaddedBlockSize - s.Position;
            byte zeroByte = 0;
            //Pad remaining XISF block with zero values
            for (int i = 0; i < remainingBlockPadding; i++) {
                s.WriteByte(zeroByte);
            }

            if (this.Data != null) {
                this.Data.Save(s);
            }
            return true;
        }
    }

    /**
     * Specifications: http://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html#xisf_header
     */

    public class XISFHeader {
        public XDocument Content { get; private set; }

        public XElement MetaData { get; private set; }
        public XElement Image { get; private set; }
        private XElement Xisf;

        private XNamespace xmlns = XNamespace.Get("http://www.pixinsight.com/xisf");
        private XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        /* Create Header with embedded Image */

        public XISFHeader() {
            Xisf = new XElement(xmlns + "xisf",
                    new XAttribute("version", "1.0"),
                    new XAttribute("xmlns", "http://www.pixinsight.com/xisf"),
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(xsi + "schemaLocation", "http://www.pixinsight.com/xisf http://pixinsight.com/xisf/xisf-1.0.xsd")
            );

            MetaData = new XElement("Metadata");

            AddMetaDataProperty(XISFMetaDataProperty.XISF.CreationTime, DateTime.UtcNow.ToString("o"));
            AddMetaDataProperty(XISFMetaDataProperty.XISF.CreatorApplication, Utility.Title);

            Xisf.Add(MetaData);

            Content = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                Xisf
            );
        }

        public int ByteCount {
            get {
                return Encoding.UTF8.GetByteCount(Content.ToString());
            }
        }

        public void Populate(ImageMetaData metaData) {
            if (metaData.Image.ExposureStart > DateTime.MinValue) {
                this.AddImageProperty(XISFImageProperty.Observation.Time.Start, metaData.Image.ExposureStart.ToUniversalTime().ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture), "Time of observation (UTC)");
                this.AddImageFITSKeyword("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture), "Time of observation (local)");
            }

            if (!double.IsNaN(metaData.Image.ExposureTime)) {
                this.AddImageProperty(XISFImageProperty.Instrument.ExposureTime, metaData.Image.ExposureTime.ToString(System.Globalization.CultureInfo.InvariantCulture), "[s] Exposure duration");
            }

            /* Camera */
            if (!string.IsNullOrWhiteSpace(metaData.Camera.Name)) {
                this.AddImageProperty(XISFImageProperty.Instrument.Camera.Name, metaData.Camera.Name, "Imaging instrument name");
            }
            if (!double.IsNaN(metaData.Camera.Gain) && metaData.Camera.Gain >= 0) {
                this.AddImageFITSKeyword("GAIN", metaData.Camera.Gain.ToString(CultureInfo.InvariantCulture), "Sensor gain");
            }

            if (!double.IsNaN(metaData.Camera.Offset) && metaData.Camera.Offset >= 0) {
                this.AddImageFITSKeyword("OFFSET", metaData.Camera.Offset.ToString(CultureInfo.InvariantCulture), "Sensor gain offset");
            }

            if (!double.IsNaN(metaData.Camera.ElectronsPerADU)) {
                this.AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, metaData.Camera.ElectronsPerADU.ToString(CultureInfo.InvariantCulture), "[e-/ADU] Electrons per A/D unit");
            }

            if (metaData.Camera.BinX > 0) {
                this.AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, metaData.Camera.BinX.ToString(CultureInfo.InvariantCulture), "X axis binning factor");
            }
            if (metaData.Camera.BinY > 0) {
                this.AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, metaData.Camera.BinY.ToString(CultureInfo.InvariantCulture), "Y axis binning factor");
            }

            if (!double.IsNaN(metaData.Camera.SetPoint)) {
                this.AddImageFITSKeyword("SET-TEMP", metaData.Camera.SetPoint.ToString(CultureInfo.InvariantCulture), "[degC] CCD temperature setpoint");
            }

            if (!double.IsNaN(metaData.Camera.Temperature)) {
                this.AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, metaData.Camera.Temperature.ToString(CultureInfo.InvariantCulture), "[degC] CCD temperature");
            }
            if (!double.IsNaN(metaData.Camera.PixelSize)) {
                this.AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, metaData.Camera.PixelSize.ToString(CultureInfo.InvariantCulture), "[um] Pixel X axis size");
                this.AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, metaData.Camera.PixelSize.ToString(CultureInfo.InvariantCulture), "[um] Pixel Y axis size");
            }

            /* Observer */
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                this.AddImageProperty(XISFImageProperty.Observation.Location.Elevation, metaData.Observer.Elevation.ToString(CultureInfo.InvariantCulture), "[m] Observation site elevation");
            }
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                this.AddImageProperty(XISFImageProperty.Observation.Location.Latitude, metaData.Observer.Latitude.ToString(CultureInfo.InvariantCulture), "[deg] Observation site latitude");
            }
            if (!double.IsNaN(metaData.Observer.Elevation)) {
                this.AddImageProperty(XISFImageProperty.Observation.Location.Longitude, metaData.Observer.Longitude.ToString(CultureInfo.InvariantCulture), "[deg] Observation site longitude");
            }

            /* Telescope */
            if (!string.IsNullOrWhiteSpace(metaData.Telescope.Name)) {
                this.AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, metaData.Telescope.Name.ToString(CultureInfo.InvariantCulture), "Name of telescope");
            }
            if (!double.IsNaN(metaData.Telescope.FocalLength) && metaData.Telescope.FocalLength > 0) {
                this.AddImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, metaData.Telescope.FocalLength.ToString(CultureInfo.InvariantCulture), "[mm] Focal length");

                if (!double.IsNaN(metaData.Telescope.FocalRatio) && metaData.Telescope.FocalRatio > 0) {
                    var apterture = metaData.Telescope.FocalLength / metaData.Telescope.FocalRatio;
                    this.AddImageProperty(XISFImageProperty.Instrument.Telescope.Aperture, apterture.ToString(CultureInfo.InvariantCulture), "[mm] Aperture", false);
                    this.AddImageFITSKeyword("FOCRATIO", metaData.Telescope.FocalRatio.ToString(CultureInfo.InvariantCulture), "Focal ratio");
                }
            }

            if (metaData.Telescope.Coordinates != null) {
                this.AddImageProperty(XISFImageProperty.Observation.Center.RA, metaData.Telescope.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), "[deg] RA of telescope");
                this.AddImageProperty(XISFImageProperty.Observation.Center.Dec, metaData.Telescope.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), "[deg] Declination of telescope");
            }

            /* Target */
            if (!string.IsNullOrWhiteSpace(metaData.Target.Name)) {
                this.AddImageProperty(XISFImageProperty.Observation.Object.Name, metaData.Target.Name, "Name of the object of interest");
            }

            if (metaData.Target.Coordinates != null) {
                this.AddImageProperty(XISFImageProperty.Observation.Object.RA, metaData.Target.Coordinates.RADegrees.ToString(CultureInfo.InvariantCulture), "[deg] RA of imaged object", false);
                this.AddImageFITSKeyword(XISFImageProperty.Observation.Object.RA[2], Astrometry.Astrometry.HoursToFitsHMS(metaData.Target.Coordinates.RA), "[H M S] RA of imaged object");
                this.AddImageProperty(XISFImageProperty.Observation.Object.Dec, metaData.Target.Coordinates.Dec.ToString(CultureInfo.InvariantCulture), "[deg] Declination of imaged object", false);
                this.AddImageFITSKeyword(XISFImageProperty.Observation.Object.Dec[2], Astrometry.Astrometry.DegreesToFitsDMS(metaData.Target.Coordinates.Dec), "[D M S] Declination of imaged object");
            }

            /* Focuser */
            if (!string.IsNullOrWhiteSpace(metaData.Focuser.Name)) {
                /* fits4win, SGP */
                this.AddImageFITSKeyword("FOCNAME", metaData.Focuser.Name, "Focusing equipment name");
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
                    this.AddImageProperty(XISFImageProperty.Instrument.Focuser.Position, focusDistance.ToString(CultureInfo.InvariantCulture));
                }

                /* fits4win, SGP */
                this.AddImageFITSKeyword("FOCPOS", metaData.Focuser.Position.ToString(CultureInfo.InvariantCulture), "[step] Focuser position");

                /* MaximDL, several observatories */
                this.AddImageFITSKeyword("FOCUSPOS", metaData.Focuser.Position.ToString(CultureInfo.InvariantCulture), "[step] Focuser position");
            }

            if (!double.IsNaN(metaData.Focuser.StepSize)) {
                /* MaximDL */
                this.AddImageFITSKeyword("FOCUSSZ", metaData.Focuser.StepSize.ToString(CultureInfo.InvariantCulture), "[um] Focuser step size");
            }

            if (!double.IsNaN(metaData.Focuser.Temperature)) {
                /* fits4win, SGP */
                this.AddImageFITSKeyword("FOCTEMP", metaData.Focuser.Temperature.ToString(CultureInfo.InvariantCulture), "[degC] Focuser temperature");

                /* MaximDL, several observatories */
                this.AddImageFITSKeyword("FOCUSTEM", metaData.Focuser.Temperature.ToString(CultureInfo.InvariantCulture), "[degC] Focuser temperature");
            }

            /* Rotator */
            if (!string.IsNullOrWhiteSpace(metaData.Rotator.Name)) {
                /* NINA */
                this.AddImageFITSKeyword("ROTNAME", metaData.Rotator.Name, "Rotator equipment name");
            }

            if (!double.IsNaN(metaData.Rotator.Position)) {
                /* fits4win */
                this.AddImageFITSKeyword("ROTATOR", metaData.Rotator.Position.ToString(CultureInfo.InvariantCulture), "[deg] Rotator angle");

                /* MaximDL, several observatories */
                this.AddImageFITSKeyword("ROTATANG", metaData.Rotator.Position.ToString(CultureInfo.InvariantCulture), "[deg] Rotator angle");
            }

            if (!double.IsNaN(metaData.Rotator.StepSize)) {
                /* NINA */
                this.AddImageFITSKeyword("ROTSTPSZ", metaData.Rotator.StepSize.ToString(CultureInfo.InvariantCulture), "[deg] Rotator step size");
            }

            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Name)) {
                /* fits4win */
                this.AddImageFITSKeyword("FWHEEL", metaData.FilterWheel.Name, "Filter Wheel name");
            }

            if (!string.IsNullOrWhiteSpace(metaData.FilterWheel.Filter)) {
                /* fits4win */
                this.AddImageProperty(XISFImageProperty.Instrument.Filter.Name, metaData.FilterWheel.Filter, "Active filter name");
            }

            /* Weather Data */
            if (!double.IsNaN(metaData.WeatherData.CloudCover)) {
                this.AddImageFITSKeyword("CLOUDCVR", metaData.WeatherData.CloudCover.ToString(CultureInfo.InvariantCulture), "[percent] Cloud cover");
            }

            if (!double.IsNaN(metaData.WeatherData.DewPoint)) {
                this.AddImageFITSKeyword("DEWPOINT", metaData.WeatherData.DewPoint.ToString(CultureInfo.InvariantCulture), "[degC] Dew point");
            }

            if (!double.IsNaN(metaData.WeatherData.Humidity)) {
                this.AddImageProperty(XISFImageProperty.Observation.Meteorology.RelativeHumidity, metaData.WeatherData.Humidity.ToString(CultureInfo.InvariantCulture), "[percent] Relative humidity");
            }

            if (!double.IsNaN(metaData.WeatherData.Pressure)) {
                this.AddImageProperty(XISFImageProperty.Observation.Meteorology.AtmosphericPressure, metaData.WeatherData.Pressure.ToString(CultureInfo.InvariantCulture), "[hPa] Air pressure");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyBrightness)) {
                this.AddImageFITSKeyword("SKYBRGHT", metaData.WeatherData.SkyBrightness.ToString(CultureInfo.InvariantCulture), "[lux] Sky brightness");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyQuality)) {
                /* fits4win */
                this.AddImageFITSKeyword("MPSAS", metaData.WeatherData.SkyQuality.ToString(CultureInfo.InvariantCulture), "[mags/arcsec^2] Sky quality");
            }

            if (!double.IsNaN(metaData.WeatherData.SkyTemperature)) {
                this.AddImageFITSKeyword("SKYTEMP", metaData.WeatherData.SkyTemperature.ToString(CultureInfo.InvariantCulture), "[degC] Sky temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.StarFWHM)) {
                this.AddImageFITSKeyword("STARFWHM", metaData.WeatherData.StarFWHM.ToString(CultureInfo.InvariantCulture), "Star FWHM");
            }

            if (!double.IsNaN(metaData.WeatherData.Temperature)) {
                this.AddImageProperty(XISFImageProperty.Observation.Meteorology.AmbientTemperature, metaData.WeatherData.Temperature.ToString(CultureInfo.InvariantCulture), "[degC] Ambient air temperature");
            }

            if (!double.IsNaN(metaData.WeatherData.WindDirection)) {
                this.AddImageProperty(XISFImageProperty.Observation.Meteorology.WindDirection, metaData.WeatherData.WindDirection.ToString(CultureInfo.InvariantCulture), "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W");
            }

            if (!double.IsNaN(metaData.WeatherData.WindGust)) {
                this.AddImageProperty(XISFImageProperty.Observation.Meteorology.WindGust, (metaData.WeatherData.WindGust * 3.6).ToString(CultureInfo.InvariantCulture), "[kph] Wind gust");
            }

            if (!double.IsNaN(metaData.WeatherData.WindSpeed)) {
                this.AddImageProperty(XISFImageProperty.Observation.Meteorology.WindSpeed, (metaData.WeatherData.WindSpeed * 3.6).ToString(CultureInfo.InvariantCulture), "[kph] Wind speed");
            }

            this.AddImageFITSKeyword("SWCREATE", string.Format("N.I.N.A. {0} ({1})", Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");
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
            Image.Add(new XElement("FITSKeyword",
                        new XAttribute("name", name),
                        new XAttribute("value", value),
                        new XAttribute("comment", comment)));
        }

        private void AddProperty(XElement elem, string[] property, string value, string comment = "") {
            if (property?.Length < 2 || elem == null) {
                return;
            }
            var id = property[0];
            var type = property[1];
            XElement xelem;
            if (type == "String") {
                xelem = new XElement("Property",
                    new XAttribute("id", id),
                    new XAttribute("type", type),
                    new XAttribute("comment", comment),
                    value
                );
            } else {
                xelem = new XElement("Property",
                    new XAttribute("id", id),
                    new XAttribute("type", type),
                    new XAttribute("comment", comment),
                    new XAttribute("value", value)
                );
            }
            elem.Add(xelem);
        }

        /// <summary>
        /// Adds the image metadata to the header
        /// Image data has to be added at a later point to the xisf body
        /// </summary>
        /// <param name="imageProperties"></param>
        /// <param name="imageType"></param>
        public void AddImageMetaData(ImageProperties imageProperties, string imageType) {
            if (imageType == "SNAPSHOT") { imageType = "LIGHT"; }

            var image = new XElement("Image",
                    new XAttribute("geometry", imageProperties.Width + ":" + imageProperties.Height + ":" + "1"),
                    new XAttribute("sampleFormat", "UInt16"),
                    new XAttribute("imageType", imageType),
                    new XAttribute("colorSpace", "Gray")
                    );

            Image = image;
            Xisf.Add(image);
            AddImageFITSKeyword("IMAGETYP", imageType, "Type of exposure");
        }

        /// <summary>
        /// Adds the image tage to the Header and embedds the image as base64 inside the header image as a data block
        /// As this increases the file size and is a lot more computational heavy compared to an attached image, this should be avoided.
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="imageType"></param>
        public void AddEmbeddedImage(IImageData imageData, string imageType) {
            AddImageMetaData(imageData.Properties, imageType);
            Image.Add(new XAttribute("location", "embedded"));

            byte[] result = new byte[imageData.Data.FlatArray.Length * sizeof(ushort)];
            Buffer.BlockCopy(imageData.Data.FlatArray, 0, result, 0, result.Length);

            var base64 = Convert.ToBase64String(result);

            var data = new XElement("Data", new XAttribute("encoding", "base64"), base64);

            Image.Add(data);
        }

        public void Save(Stream s) {
            using (System.Xml.XmlWriter sw = System.Xml.XmlWriter.Create(s, new System.Xml.XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.UTF8 })) {
                Content.Save(sw);
            }
        }
    }

    public class XISFData {
        public ushort[] Data { get; }

        public XISFData(ushort[] data) {
            this.Data = data;
        }

        /// <summary>
        /// Write image data to stream
        /// </summary>
        /// <param name="s"></param>
        /// <remarks>XISF's default endianess is little endian</remarks>
        internal void Save(Stream s) {
            for (int i = 0; i < this.Data.Length; i++) {
                var val = this.Data[i];
                s.WriteByte((byte)val);
                s.WriteByte((byte)(val >> 8));
            }
        }
    }

    public static class XISFImageProperty {

        public static class Observer {
            public static readonly string Namespace = "Observer:";
            public static readonly string[] EmailAddress = { Namespace + nameof(EmailAddress), "String" };
            public static readonly string[] Name = { Namespace + nameof(Name), "String" };
            public static readonly string[] PostalAddress = { Namespace + nameof(PostalAddress), "String" };
            public static readonly string[] Website = { Namespace + nameof(Website), "String" };
        }

        public static class Organization {
            public static readonly string Namespace = "Organization:";
            public static readonly string[] EmailAddress = { Namespace + nameof(EmailAddress), "String" };
            public static readonly string[] Name = { Namespace + nameof(Name), "String" };
            public static readonly string[] PostalAddress = { Namespace + nameof(PostalAddress), "String" };
            public static readonly string[] Website = { Namespace + nameof(Website), "String" };
        }

        public static class Observation {
            public static readonly string Namespace = "Observation:";
            public static readonly string[] CelestialReferenceSystem = { Namespace + nameof(CelestialReferenceSystem), "String" };
            public static readonly string[] BibliographicReferences = { Namespace + nameof(BibliographicReferences), "String" };

            public static class Center {
                public static readonly string Namespace = Observation.Namespace + "Center:";
                public static readonly string[] Dec = { Namespace + nameof(Dec), "Float64", "DEC" };
                public static readonly string[] RA = { Namespace + nameof(RA), "Float64", "RA" };
                public static readonly string[] X = { Namespace + nameof(X), "Float64" };
                public static readonly string[] Y = { Namespace + nameof(Y), "Float64" };
            }

            public static readonly string[] Description = { Namespace + nameof(Description), "String" };
            public static readonly string[] Equinox = { Namespace + nameof(Equinox), "Float64" };
            public static readonly string[] GeodeticReferenceSystem = { Namespace + nameof(GeodeticReferenceSystem), "String" };

            public static class Location {
                public static readonly string Namespace = Observation.Namespace + "Location:";
                public static readonly string[] Elevation = { Namespace + nameof(Elevation), "Float64", "SITEELEV" };
                public static readonly string[] Latitude = { Namespace + nameof(Latitude), "Float64", "SITELAT" };
                public static readonly string[] Longitude = { Namespace + nameof(Longitude), "Float64", "SITELONG" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String" };
            }

            public static class Meteorology {
                public static readonly string Namespace = Observation.Namespace + "Meteorology:";
                public static readonly string[] AmbientTemperature = { Namespace + nameof(AmbientTemperature), "Float32", "AMBTEMP" };
                public static readonly string[] AtmosphericPressure = { Namespace + nameof(AtmosphericPressure), "Float32", "PRESSURE" };
                public static readonly string[] RelativeHumidity = { Namespace + nameof(RelativeHumidity), "Float32", "HUMIDITY" };
                public static readonly string[] WindDirection = { Namespace + nameof(WindDirection), "Float32", "WINDDIR" };
                public static readonly string[] WindGust = { Namespace + nameof(WindGust), "Float32", "WINDGUST" };
                public static readonly string[] WindSpeed = { Namespace + nameof(WindSpeed), "Float32", "WINDSPD" };
            }

            public static class Object {
                public static readonly string Namespace = Observation.Namespace + "Object:";
                public static readonly string[] Dec = { Namespace + nameof(Dec), "Float64", "OBJCTDEC" };
                public static readonly string[] RA = { Namespace + nameof(RA), "Float64", "OBJCTRA" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String", "OBJECT" };
            }

            public static class Time {
                public static readonly string Namespace = Observation.Namespace + "Time:";
                public static readonly string[] End = { Namespace + nameof(End), "TimePoint" };
                public static readonly string[] Start = { Namespace + nameof(Start), "TimePoint", "DATE-OBS" };
            }

            public static readonly string[] Title = { Namespace + nameof(Title), "String" };
        }

        public static class Instrument {
            public static readonly string Namespace = "Instrument:";
            public static readonly string[] ExposureTime = { Namespace + nameof(ExposureTime), "Float32", "EXPOSURE" };

            public static class Camera {
                public static readonly string Namespace = Instrument.Namespace + "Camera:";

                public static readonly string[] Gain = { Namespace + nameof(Gain), "Float32", "EGAIN" };
                public static readonly string[] ISOSpeed = { Namespace + nameof(ISOSpeed), "Int32" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String", "INSTRUME" };
                public static readonly string[] ReadoutNoise = { Namespace + nameof(ReadoutNoise), "Float32" };
                public static readonly string[] Rotation = { Namespace + nameof(Rotation), "Float32" };
                public static readonly string[] XBinning = { Namespace + nameof(XBinning), "Int32", "XBINNING" };
                public static readonly string[] YBinning = { Namespace + nameof(YBinning), "Int32", "YBINNING" };
            }

            public static class Filter {
                public static readonly string Namespace = Instrument.Namespace + "Filter:";
                public static readonly string[] Name = { Namespace + nameof(Name), "String", "FILTER" };
            }

            public static class Focuser {
                public static readonly string Namespace = Instrument.Namespace + "Focuser:";
                public static readonly string[] Position = { Namespace + nameof(Position), "Float32" };
            }

            public static class Sensor {
                public static readonly string Namespace = Instrument.Namespace + "Sensor:";
                public static readonly string[] TargetTemperature = { Namespace + nameof(TargetTemperature), "Float32" };
                public static readonly string[] Temperature = { Namespace + nameof(Temperature), "Float32", "CCD-TEMP" };
                public static readonly string[] XPixelSize = { Namespace + nameof(XPixelSize), "Float32", "XPIXSZ" };
                public static readonly string[] YPixelSize = { Namespace + nameof(YPixelSize), "Float32", "YPIXSZ" };
            }

            public static class Telescope {
                public static readonly string Namespace = Instrument.Namespace + "Telescope:";
                public static readonly string[] Aperture = { Namespace + nameof(Aperture), "Float32" };
                public static readonly string[] CollectingArea = { Namespace + nameof(CollectingArea), "Float32" };
                public static readonly string[] FocalLength = { Namespace + nameof(FocalLength), "Float32", "FOCALLEN" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String", "TELESCOP" };
            }
        }

        public static class Image {
            public static readonly string Namespace = "Image:";
            public static readonly string[] FrameNumber = { Namespace + nameof(FrameNumber), "UInt32" };
            public static readonly string[] GroupId = { Namespace + nameof(GroupId), "String" };
            public static readonly string[] SubgroupId = { Namespace + nameof(SubgroupId), "String" };
        }
    }

    public static class XISFMetaDataProperty {

        public static class XISF {
            public static readonly string Namespace = "XISF:";
            public static readonly string[] CreationTime = { Namespace + nameof(CreationTime), "TimePoint" };
            public static readonly string[] CreatorApplication = { Namespace + nameof(CreatorApplication), "String" };
        }
    }
}