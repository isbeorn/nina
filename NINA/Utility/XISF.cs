#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

#endregion "copyright"

using NINA.Model.MyCamera;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NINA.Utility {

    internal class XISF {
        private XISFHeader Header { get; set; }

        public XISF(XISFHeader header) {
            this.Header = header;
        }

        public static async Task<ImageArray> LoadImageArrayFromFile(Uri filePath, bool isBayered, int histogramResolution) {
            using (FileStream fs = new FileStream(filePath.LocalPath, FileMode.Open)) {
                //Skip to the header length info starting at byte 9
                fs.Seek(8, SeekOrigin.Current);
                byte[] headerLengthInfo = new byte[8];
                fs.Read(headerLengthInfo, 0, 8);
                var headerLength = BitConverter.ToInt32(headerLengthInfo, 0);

                byte[] bytes = new byte[headerLength];
                fs.Read(bytes, 0, headerLength);
                string xmlString = System.Text.UTF8Encoding.UTF8.GetString(bytes);

                var xml = XElement.Parse(xmlString);
                var imageTag = xml.Element("Image");
                var geometry = imageTag.Attribute("geometry").Value.Split(':');
                int width = Int32.Parse(geometry[0]);
                int height = Int32.Parse(geometry[1]);

                //Seems to be no attribute to identify the bit depth. Assume 16.
                var bitDepth = 16;
                if (imageTag.Attribute("location").Value.StartsWith("attachment")) {
                    var location = imageTag.Attribute("location").Value.Split(':');
                    var start = int.Parse(location[1]);
                    var size = int.Parse(location[2]);

                    byte[] raw = new byte[size];
                    fs.Seek(start, SeekOrigin.Begin);
                    fs.Read(raw, 0, size);
                    ushort[] img = new ushort[raw.Length / 2];
                    Buffer.BlockCopy(raw, 0, img, 0, raw.Length);

                    return await ImageArray.CreateInstance(img, width, height, bitDepth, isBayered, true, histogramResolution);
                } else {
                    var base64Img = xml.Element("Image").Element("Data").Value;
                    byte[] encodedImg = Convert.FromBase64String(base64Img);
                    ushort[] img = new ushort[(int)Math.Ceiling(encodedImg.Length / 2.0)];
                    Buffer.BlockCopy(encodedImg, 0, img, 0, encodedImg.Length);

                    return await ImageArray.CreateInstance(img, width, height, bitDepth, isBayered, true, histogramResolution);
                }
            }
        }

        public void AddAttachedImage(ImageArray arr, string imageType) {
            var currentHeaderSize = System.Text.ASCIIEncoding.UTF8.GetByteCount(Header.Header.ToString()) + 100;

            var targetHeaderBlock = currentHeaderSize + (1024 - currentHeaderSize % 1024);

            Header.Image.Add(new XAttribute("location", $"attachment:{targetHeaderBlock}:{arr.FlatArray.Length * sizeof(ushort)}"));

            _imageData = arr.FlatArray;
        }

        private ushort[] _imageData;

        public bool Save(Stream s) {
            Header.Save(s);
            if (this._imageData != null) {
                /* Write image data
                 * XISF's default endianess is little endian
                 */
                for (int i = 0; i < this._imageData.Length; i++) {
                    var val = this._imageData[i];
                    s.WriteByte((byte)val);
                    s.WriteByte((byte)(val >> 8));
                }
            }
            return true;
        }
    }

    /**
     * Specifications: http://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html#xisf_header
     */

    public class XISFHeader {
        public XDocument Header { get; set; }

        public XElement MetaData { get; set; }
        public XElement Image { get; set; }
        private XElement Xisf;

        private XNamespace xmlns = XNamespace.Get("http://www.pixinsight.com/xisf");
        private XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        private XNamespace propertyns = "XISF";

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
            AddMetaDataProperty(XISFMetaDataProperty.XISF.CreatorApplication, "Nighttime Imaging 'N' Astronomy");

            Xisf.Add(MetaData);

            Header = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                Xisf
            );
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
            AddProperty(Image, property, value, comment);
            if (property.Length > 2 && autoaddfits) {
                AddImageFITSKeyword(property[2], value, comment);
            }
        }

        public void AddImageFITSKeyword(string name, string value, string comment = "") {
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
        /// <param name="arr"></param>
        /// <param name="imageType"></param>
        public void AddImageMetaData(ImageArray arr, string imageType) {
            var image = new XElement("Image",
                    new XAttribute("geometry", arr.Statistics.Width + ":" + arr.Statistics.Height + ":" + "1"),
                    new XAttribute("sampleFormat", "UInt16"),
                    new XAttribute("imageType", imageType),
                    new XAttribute("colorSpace", "Gray")
                    );

            Image = image;
            Xisf.Add(image);

            AddImageFITSKeyword("IMAGETYP", imageType);
        }

        /// <summary>
        /// Adds the image tage to the Header and embedds the image as base64 inside the header image as a data block
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="imageType"></param>
        public void AddEmbeddedImage(ImageArray arr, string imageType) {
            var image = new XElement("Image",
                    new XAttribute("geometry", arr.Statistics.Width + ":" + arr.Statistics.Height + ":" + "1"),
                    new XAttribute("sampleFormat", "UInt16"),
                    new XAttribute("imageType", imageType),
                    new XAttribute("location", "embedded"),
                    new XAttribute("colorSpace", "Gray")
                    );

            byte[] result = new byte[arr.FlatArray.Length * sizeof(ushort)];
            Buffer.BlockCopy(arr.FlatArray, 0, result, 0, result.Length);

            var base64 = Convert.ToBase64String(result);

            var data = new XElement("Data", new XAttribute("encoding", "base64"), base64);

            image.Add(data);
            Image = image;
            Xisf.Add(image);

            AddImageFITSKeyword("IMAGETYP", imageType);
        }

        public void Save(Stream s) {
            /*XISF0100*/
            byte[] monolithicsignature = new byte[] { 88, 73, 83, 70, 48, 49, 48, 48 };
            s.Write(monolithicsignature, 0, monolithicsignature.Length);

            /*Xml header length */
            var headerlength = BitConverter.GetBytes(System.Text.ASCIIEncoding.UTF8.GetByteCount(Header.ToString()));
            s.Write(headerlength, 0, headerlength.Length);

            /*reserved space 4 byte must be 0 */
            var reserved = new byte[] { 0, 0, 0, 0 };
            s.Write(reserved, 0, reserved.Length);

            using (System.Xml.XmlWriter sw = System.Xml.XmlWriter.Create(s, new System.Xml.XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.UTF8 })) {
                Header.Save(sw);
            }

            long remainingBlockPadding = (long)Math.Ceiling((double)s.Position / (double)1024) * (long)1024 - s.Position;
            byte zeroByte = 0;
            //Pad remaining XISF block with zero values
            for (int i = 0; i < remainingBlockPadding; i++) {
                s.WriteByte(zeroByte);
            }
        }
    }

    public class XISFData {
        public ushort[] Data;

        public XISFData(ushort[] data) {
            this.Data = data;
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
                public static readonly string[] Dec = { Namespace + nameof(Dec), "Float64", "OBJCTDEC", "DEC" };
                public static readonly string[] RA = { Namespace + nameof(RA), "Float64", "OBJCTRA", "RA" };
                public static readonly string[] X = { Namespace + nameof(X), "Float64" };
                public static readonly string[] Y = { Namespace + nameof(Y), "Float64" };
            }

            public static readonly string[] Description = { Namespace + nameof(Description), "String" };
            public static readonly string[] Equinox = { Namespace + nameof(Equinox), "Float64" };
            public static readonly string[] GeodeticReferenceSystem = { Namespace + nameof(GeodeticReferenceSystem), "String" };

            public static class Location {
                public static readonly string Namespace = Observation.Namespace + "Location:";
                public static readonly string[] Elevation = { Namespace + nameof(Elevation), "Float64" };
                public static readonly string[] Latitude = { Namespace + nameof(Latitude), "Float64", "SITELAT" };
                public static readonly string[] Longitude = { Namespace + nameof(Longitude), "Float64", "SITELONG" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String" };
            }

            public static class Meteorology {
                public static readonly string Namespace = Observation.Namespace + "Meteorology:";
                public static readonly string[] AmbientTemperature = { Namespace + nameof(AmbientTemperature), "Float32" };
                public static readonly string[] AtmosphericPressure = { Namespace + nameof(AtmosphericPressure), "Float32" };
                public static readonly string[] RelativeHumidity = { Namespace + nameof(RelativeHumidity), "Float32" };
                public static readonly string[] WindDirection = { Namespace + nameof(WindDirection), "Float32" };
                public static readonly string[] WindGust = { Namespace + nameof(WindGust), "Float32" };
                public static readonly string[] WindSpeed = { Namespace + nameof(WindSpeed), "Float32" };
            }

            public static class Object {
                public static readonly string Namespace = Observation.Namespace + "Object:";
                public static readonly string[] Dec = { Namespace + nameof(Dec), "Float64" };
                public static readonly string[] RA = { Namespace + nameof(RA), "Float64" };
                public static readonly string[] Name = { Namespace + nameof(Name), "String" };
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
                public static readonly string[] Name = { Namespace + nameof(Name), "String" };
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