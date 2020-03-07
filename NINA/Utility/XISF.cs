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

using Ionic.Zlib;
using LZ4;
using NINA.Model.ImageData;
using NINA.Utility.Enum;
using SHA3;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NINA.Utility {

    internal class XISF {
        public XISFHeader Header { get; private set; }

        public XISFData Data { get; private set; }

        public XISF(XISFHeader header) {
            Header = header;
        }

        // XISF0100
        private static readonly byte[] xisfSignature = new byte[] { 0x58, 0x49, 0x53, 0x46, 0x30, 0x31, 0x30, 0x30 };

        /// <summary>
        /// The header xml + padding will consist of a muliple of bytes from this size
        /// </summary>
        public int PaddedBlockSize => 1024;

        public static async Task<ImageData> Load(Uri filePath, bool isBayered) {
            XNamespace xmlns = XNamespace.Get("http://www.pixinsight.com/xisf");

            using (FileStream fs = new FileStream(filePath.LocalPath, FileMode.Open)) {
                // First make sure we are opening a XISF file by looking for the XISF signature at bytes 1-8
                byte[] fileSig = new byte[xisfSignature.Length];
                fs.Read(fileSig, 0, fileSig.Length);

                if (!fileSig.SequenceEqual(xisfSignature)) {
                    Logger.Error($"XISF: Opened file \"{filePath.LocalPath}\" is not a valid XISF file");
                    throw new InvalidDataException(Locale.Loc.Instance["LblXisfInvalidFile"]);
                }

                Logger.Debug($"XISF: Opening file \"{filePath.LocalPath}\"");

                // Get the header length info, bytes 9-12
                byte[] headerLengthInfo = new byte[4];
                fs.Read(headerLengthInfo, 0, headerLengthInfo.Length);
                uint headerLength = BitConverter.ToUInt32(headerLengthInfo, 0);

                // Skip the next 4 bytes as they are reserved space
                fs.Seek(4, SeekOrigin.Current);

                // XML document starts at byte 17
                byte[] bytes = new byte[headerLength];
                fs.Read(bytes, 0, (int)headerLength);
                string xmlString = Encoding.UTF8.GetString(bytes);

                /*
                 * Prior versions of NINA erroneously wrote out files with blank namespace definittions in the Image, Metadata, and Propery
                 * elements. There is no graceful way to deal with this, so we just remove these using a regex.
                 */
                string nsFilter = @"xmlns=""""";
                xmlString = Regex.Replace(xmlString, nsFilter, "");

                /*
                 * Find the <Image> element
                 */
                XElement xml = XElement.Parse(xmlString);
                XElement imageTag = xml.Element(xmlns + "Image");

                /*
                 * Retrieve the geometry attribute.
                 */
                int width = 0;
                int height = 0;

                try {
                    string[] geometry = imageTag.Attribute("geometry").Value.Split(':');
                    width = int.Parse(geometry[0]);
                    height = int.Parse(geometry[1]);
                } catch (Exception ex) {
                    Logger.Error($"XISF: Could not find image geometry: {ex}");
                    throw new InvalidDataException(Locale.Loc.Instance["LblXisfInvalidGeometry"]);
                }

                Logger.Debug($"XISF: File geometry: width={width}, height={height}");

                /*
                 * Retrieve the pixel data type
                 * Currently we support only UInt16 (ushort)
                 */
                int bitDepth;

                try {
                    string sampleFormat = imageTag.Attribute("sampleFormat").Value.ToString();

                    // TODO: support all other XISF sample formats (UInt8, UInt32, etc)
                    switch (sampleFormat) {
                        case "UInt16":
                            bitDepth = 16;
                            break;

                        default:
                            throw new InvalidDataException(string.Format(Locale.Loc.Instance["LblXisfUnsupportedFormat"], sampleFormat));
                    }
                } catch (InvalidDataException ex) {
                    Logger.Error($"XISF: Could not read image data: {ex}");
                    throw ex;
                } catch (Exception ex) {
                    Logger.Error($"XISF: Could not find image data type: {ex}");
                    throw new InvalidDataException("Could not find XISF image data type");
                }

                /*
                 * Determine if the data block is compressed and if a checksum is provided for it
                 */
                XISFCompressionInfo compressionInfo = new XISFCompressionInfo();
                string[] compression = null;

                try {
                    // [compression codec]:[uncompressed size]:[sizeof shuffled typedef]
                    compression = imageTag.Attribute("compression").Value.ToLower().Split(':');

                    if (!string.IsNullOrEmpty(compression[0])) {
                        compressionInfo = GetCompressionType(compression);
                    }
                } catch (InvalidDataException) {
                    Logger.Error($"XISF: Unknown compression codec encountered: {compression[0]}");
                    throw new InvalidDataException(string.Format(Locale.Loc.Instance["LblXisfUnsupportedCompression"], compression[0]));
                } catch {
                    Logger.Debug("XISF: Compressed data block was not encountered");
                }

                if (compressionInfo.CompressionType != XISFCompressionTypeEnum.NONE) {
                    Logger.Debug(string.Format("XISF: CompressionType: {0}, UncompressedSize: {1}, IsShuffled: {2}, ItemSize: {3}",
                        compressionInfo.CompressionType,
                        compressionInfo.UncompressedSize,
                        compressionInfo.IsShuffled,
                        compressionInfo.ItemSize));
                }

                /*
                 * Determine if a checksum is provided for the datablock.
                 * If the data block is compressed, the checksum is for the compressed form.
                 */
                XISFChecksumTypeEnum cksumType = XISFChecksumTypeEnum.NONE;
                string cksumHash = string.Empty;
                string[] cksum = null;

                try {
                    // [hash type]:[hash string]
                    cksum = imageTag.Attribute("checksum").Value.ToLower().Split(':');

                    if (!string.IsNullOrEmpty(cksum[0])) {
                        cksumType = GetChecksumType(cksum[0]);
                        cksumHash = cksum[1];
                    }
                } catch (InvalidDataException) {
                    Logger.Error($"XISF: Unknown checksum type: {cksum[0]}");
                    throw new InvalidDataException(string.Format(Locale.Loc.Instance["LblXisfUnsupportedChecksum"], cksum[0]));
                } catch {
                    Logger.Debug("XISF: Checksummed data block was not encountered");
                }

                if (cksumType != XISFChecksumTypeEnum.NONE) {
                    Logger.Debug($"XISF: Checksum type: {cksumType}, Hash: {cksumHash}");
                }

                /*
                 * Retrieve the attachment attribute to find the start and length of the data block.
                 * If the attachment attribute does not exist, we assume that the image data is
                 * inside a <Data> element and is base64-encoded.
                 */
                ImageData imageData;
                ushort[] img;

                if (imageTag.Attribute("location").Value.StartsWith("attachment")) {
                    string[] location = imageTag.Attribute("location").Value.Split(':');
                    int start = int.Parse(location[1]);
                    int size = int.Parse(location[2]);

                    Logger.Debug($"XISF: Data block type: attachment, Data block start: {start}, Data block size: {size}");

                    // Read the data block in, starting at the specified offset
                    byte[] raw = new byte[size];
                    fs.Seek(start, SeekOrigin.Begin);
                    fs.Read(raw, 0, size);

                    // Validate the data block's checksum
                    if (cksumType != XISFChecksumTypeEnum.NONE) {
                        if (!VerifyChecksum(raw, cksumType, cksumHash)) {
                            // Only emit a warning to the user about a bad checksum for now
                            Notification.Notification.ShowWarning(Locale.Loc.Instance["LblXisfBadChecksum"]);
                        }
                    }

                    // Uncompress the data block
                    if (compressionInfo.CompressionType != XISFCompressionTypeEnum.NONE) {
                        byte[] outArray = UncompressData(raw, compressionInfo.CompressionType, compressionInfo.UncompressedSize);

                        if (compressionInfo.IsShuffled) {
                            outArray = Unshuffle(outArray, compressionInfo.ItemSize);
                        }

                        img = new ushort[outArray.Length / sizeof(ushort)];
                        Buffer.BlockCopy(outArray, 0, img, 0, outArray.Length);
                    } else {
                        img = new ushort[raw.Length / sizeof(ushort)];
                        Buffer.BlockCopy(raw, 0, img, 0, raw.Length);
                    }

                    // TODO: Add parser for ImageMetaData
                    imageData = new ImageData(img, width, height, bitDepth, isBayered, new ImageMetaData());
                } else {
                    string base64Img = xml.Element(xmlns + "Image").Element("Data").Value;
                    byte[] encodedImg = Convert.FromBase64String(base64Img);
                    img = new ushort[(int)Math.Ceiling(encodedImg.Length / 2.0)];
                    Buffer.BlockCopy(encodedImg, 0, img, 0, encodedImg.Length);

                    // TODO: Add parser for ImageMetaData
                    imageData = new ImageData(img, width, height, bitDepth, isBayered, new ImageMetaData());
                }

                return imageData;
            }
        }

        private class XISFCompressionInfo {
            public XISFCompressionTypeEnum CompressionType { get; set; } = XISFCompressionTypeEnum.NONE;
            public bool IsShuffled { get; set; } = false;
            public int UncompressedSize { get; set; } = 0;
            public int ItemSize { get; set; } = 0;
        }

        private static XISFCompressionInfo GetCompressionType(string[] compression) {
            string codec = compression[0];

            XISFCompressionInfo info = new XISFCompressionInfo();
            info.UncompressedSize = int.Parse(compression[1]);

            switch (codec) {
                case "lz4":
                    info.CompressionType = XISFCompressionTypeEnum.LZ4;
                    break;

                case "lz4+sh":
                    info.CompressionType = XISFCompressionTypeEnum.LZ4;
                    info.ItemSize = int.Parse(compression[2]);
                    info.IsShuffled = true;
                    break;

                case "lz4hc":
                    info.CompressionType = XISFCompressionTypeEnum.LZ4HC;
                    break;

                case "lz4hc+sh":
                    info.CompressionType = XISFCompressionTypeEnum.LZ4HC;
                    info.ItemSize = int.Parse(compression[2]);
                    info.IsShuffled = true;
                    break;

                case "zlib":
                    info.CompressionType = XISFCompressionTypeEnum.ZLIB;
                    break;

                case "zlib+sh":
                    info.CompressionType = XISFCompressionTypeEnum.ZLIB;
                    info.ItemSize = int.Parse(compression[2]);
                    info.IsShuffled = true;
                    break;

                default:
                    throw new InvalidDataException();
            }

            return info;
        }

        private static XISFChecksumTypeEnum GetChecksumType(string cksum) {
            switch (cksum) {
                case "sha-1":
                case "sha1":
                    return XISFChecksumTypeEnum.SHA1;

                case "sha-256":
                case "sha256":
                    return XISFChecksumTypeEnum.SHA256;

                case "sha-512":
                case "sha512":
                    return XISFChecksumTypeEnum.SHA512;

                case "sha3-256":
                    return XISFChecksumTypeEnum.SHA3_256;

                case "sha3-512":
                    return XISFChecksumTypeEnum.SHA3_512;

                default:
                    throw new InvalidDataException();
            }
        }

        private static bool VerifyChecksum(byte[] raw, XISFChecksumTypeEnum cksumType, string providedCksum) {
            string computedCksum;
            SHA3Managed sha3;

            using (MyStopWatch.Measure($"XISF Checksum = {cksumType}")) {
                switch (cksumType) {
                    case XISFChecksumTypeEnum.SHA1:
                        SHA1 sha1 = new SHA1CryptoServiceProvider();
                        computedCksum = GetStringFromHash(sha1.ComputeHash(raw));
                        sha1.Dispose();
                        break;

                    case XISFChecksumTypeEnum.SHA256:
                        SHA256 sha256 = new SHA256CryptoServiceProvider();
                        computedCksum = GetStringFromHash(sha256.ComputeHash(raw));
                        sha256.Dispose();
                        break;

                    case XISFChecksumTypeEnum.SHA512:
                        SHA512 sha512 = new SHA512CryptoServiceProvider();
                        computedCksum = GetStringFromHash(sha512.ComputeHash(raw));
                        sha512.Dispose();
                        break;

                    case XISFChecksumTypeEnum.SHA3_256:
                        sha3 = new SHA3Managed(256);
                        computedCksum = GetStringFromHash(sha3.ComputeHash(raw));
                        sha3.Dispose();
                        break;

                    case XISFChecksumTypeEnum.SHA3_512:
                        sha3 = new SHA3Managed(512);
                        computedCksum = GetStringFromHash(sha3.ComputeHash(raw));
                        sha3.Dispose();
                        break;

                    default:
                        return false;
                }
            }
            if (computedCksum.Equals(providedCksum)) {
                return true;
            } else {
                Logger.Error($"XISF: Invalid data block checksum! Expected: {providedCksum} Got: {computedCksum}");
                return false;
            }
        }

        private static byte[] UncompressData(byte[] raw, XISFCompressionTypeEnum codec, int uncompressedSize) {
            byte[] outArray = null;

            if (codec != XISFCompressionTypeEnum.NONE) {
                outArray = new byte[uncompressedSize];

                using (MyStopWatch.Measure($"XISF Decompression = {codec}")) {
                    switch (codec) {
                        case XISFCompressionTypeEnum.LZ4:
                        case XISFCompressionTypeEnum.LZ4HC:
                            LZ4Codec.Decode(raw, 0, raw.Length, outArray, 0, uncompressedSize, true);
                            break;

                        case XISFCompressionTypeEnum.ZLIB:
                            outArray = ZlibStream.UncompressBuffer(raw);
                            break;
                    }
                }
            }

            return outArray;
        }

        private static string GetStringFromHash(byte[] hash) {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) {
                result.Append(hash[i].ToString("x2"));
            }
            return result.ToString();
        }

        private static byte[] Unshuffle(byte[] shuffled, int itemSize) {
            int size = shuffled.Length;
            byte[] unshuffled = new byte[size];
            int numberOfItems = size / itemSize;
            int s = 0;

            using (MyStopWatch.Measure("XISF Byte Unshuffle")) {
                for (int j = 0; j < itemSize; ++j) {
                    int u = 0 + j;
                    for (int i = 0; i < numberOfItems; ++i, ++s, u += itemSize) {
                        unshuffled[u] = shuffled[s];
                    }
                }
            }

            return unshuffled;
        }

        public void AddAttachedImage(ushort[] data, FileSaveInfo fileSaveInfo) {
            if (Header.Image == null) { throw new InvalidOperationException("No Image Header Information available for attaching image. Add Image Header first!"); }

            // Add Attached data location info to header
            Data = new XISFData(data, fileSaveInfo);

            if (fileSaveInfo.XISFChecksumType != XISFChecksumTypeEnum.NONE) {
                Header.Image.Add(new XAttribute("checksum", $"{Data.ChecksumName}:{Data.Checksum}"));
            }

            int headerLengthBytes = 4;
            int reservedBytes = 4;
            int attachmentInfoMaxBytes = 256; // Assume max 256 bytes for the attachment, compression, and checksum attributes.
            int currentHeaderSize = Header.ByteCount + xisfSignature.Length + headerLengthBytes + reservedBytes + attachmentInfoMaxBytes;

            int dataBlockStart = currentHeaderSize + (PaddedBlockSize - currentHeaderSize % PaddedBlockSize);

            if (fileSaveInfo.XISFCompressionType != XISFCompressionTypeEnum.NONE) {
                Header.Image.Add(new XAttribute("location", $"attachment:{dataBlockStart}:{Data.CompressedSize}"));

                if (fileSaveInfo.XISFByteShuffling == true) {
                    Header.Image.Add(new XAttribute("compression", $"{Data.CompressionName}:{Data.Size}:{Data.ShuffleItemSize}"));
                } else {
                    Header.Image.Add(new XAttribute("compression", $"{Data.CompressionName}:{Data.Size}"));
                }
            } else {
                Header.Image.Add(new XAttribute("location", $"attachment:{dataBlockStart}:{Data.Size}"));
            }
        }

        /// <summary>
        /// Writes monolithic XISF data to stream
        ///
        /// XISF Signature              - 8 bytes
        /// Header Length               - 4 bytes
        /// Reserved Space              - 4 bytes
        /// XISF Header                 - n bytes
        /// Padding                     - Fit the above into a multiple of PaddedBlockSize. Remaining space will be null-padded
        /// Attached XISF data block    - byte size of image data array
        /// </summary>
        /// <param name="s">Stream to write XISF data to</param>
        /// <returns></returns>
        /// <remarks>https://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html#monolithic_xisf_file</remarks>
        public bool Save(Stream s) {
            // XISF0100
            s.Write(xisfSignature, 0, xisfSignature.Length);

            // XML header length
            byte[] headerlength = BitConverter.GetBytes(Header.ByteCount);
            s.Write(headerlength, 0, headerlength.Length);

            // reserved space. 4 null bytes
            byte[] reserved = new byte[] { 0, 0, 0, 0 };
            s.Write(reserved, 0, reserved.Length);

            // XISF header XML document
            Header.Save(s);

            var location = Header.Image.Attribute("location");
            if (location == null) {
                throw new InvalidDataException("Header Image is missing location information");
            }

            // Pad space between the header and data blocks null bytes
            var remainingBlockPadding = long.Parse(location.Value.Split(':')[1]) - s.Position;

            for (int i = 0; i < remainingBlockPadding; i++) {
                s.WriteByte(0x0);
            }

            if (Data != null) {
                Data.Save(s);
            }

            return true;
        }
    }

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

        /*
         * Create Header with embedded Image
         */

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

        public int ByteCount => Encoding.UTF8.GetByteCount(Content.ToString());

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
                AddImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, metaData.Telescope.FocalLength.ToString(CultureInfo.InvariantCulture), "[mm] Focal length");

                if (!double.IsNaN(metaData.Telescope.FocalRatio) && metaData.Telescope.FocalRatio > 0) {
                    double aperture = metaData.Telescope.FocalLength / metaData.Telescope.FocalRatio;
                    AddImageProperty(XISFImageProperty.Instrument.Telescope.Aperture, aperture.ToString(CultureInfo.InvariantCulture), "[mm] Aperture", false);
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

    public class XISFData {

        /// <summary>
        /// Image data array
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Uncompressed array size in bytes
        /// </summary>
        public uint Size { get; } = 0;

        /// <summary>
        /// Array compression algorithm
        /// </summary>
        public XISFCompressionTypeEnum CompressionType { get; }

        /// <summary>
        /// Array compression algorithm textual name
        /// </summary>
        public string CompressionName { get; private set; }

        /// <summary>
        /// Compressed array size in bytes. -1 for an uncompressed array
        /// </summary>
        public uint CompressedSize { get; } = 0;

        /// <summary>
        /// Perform byte shuffling on the byte array prior to compression
        /// </summary>
        public bool ByteShuffling { get; private set; }

        /// <summary>
        /// Length in bytes of a data item for the shuffling algorithm
        /// </summary>
        public int ShuffleItemSize { get; private set; } = 0;

        /// <summary>
        /// XISF block checksum algorithm
        /// </summary>
        public XISFChecksumTypeEnum ChecksumType { get; }

        /// <summary>
        /// XISF block checksum algorithm textual name
        /// </summary>
        public string ChecksumName { get; private set; }

        /// <summary>
        /// XISF block checksum value. Empty if no checksum applied
        /// </summary>
        public string Checksum { get; private set; }

        public XISFData(ushort[] data, FileSaveInfo fileSaveInfo) {
            CompressionType = fileSaveInfo.XISFCompressionType;
            ChecksumType = fileSaveInfo.XISFChecksumType; ;
            ByteShuffling = fileSaveInfo.XISFByteShuffling;
            ShuffleItemSize = sizeof(ushort);

            Data = PrepareArray(data);
            Size = (uint)data.Length * sizeof(ushort);
            CompressedSize = CompressionType == XISFCompressionTypeEnum.NONE ? 0 : (uint)Data.Length;
        }

        /// <summary>
        /// Write image data to stream
        /// </summary>
        /// <param name="s"></param>
        /// <remarks>XISF's default endianess is little endian</remarks>
        internal void Save(Stream s) {
            s.Write(Data, 0, Data.Length);
        }

        /// <summary>
        /// Convert the ushort array to a byte arraay, compressing with the requested algorithm if required
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Uncompressed or compressed byte array</returns>
        private byte[] PrepareArray(ushort[] data) {
            byte[] outArray;

            /*
             * Convert the ushort[] into a byte[]
             * From here onwards we deal in byte arrays only
             */
            byte[] byteArray = new byte[data.Length * ShuffleItemSize];
            Buffer.BlockCopy(data, 0, byteArray, 0, data.Length * ShuffleItemSize);

            /*
             * Compress the data block as configured.
             */
            using (MyStopWatch.Measure($"XISF Compression = {CompressionType}")) {
                switch (CompressionType) {
                    case XISFCompressionTypeEnum.LZ4:
                        if (ByteShuffling) {
                            CompressionName = "lz4+sh";
                            byteArray = Shuffle(byteArray, ShuffleItemSize);
                        } else {
                            CompressionName = "lz4";
                        }

                        outArray = LZ4Codec.Encode(byteArray, 0, byteArray.Length);
                        break;

                    case XISFCompressionTypeEnum.LZ4HC:
                        if (ByteShuffling) {
                            CompressionName = "lz4hc+sh";
                            byteArray = Shuffle(byteArray, ShuffleItemSize);
                        } else {
                            CompressionName = "lz4hc";
                        }

                        outArray = LZ4Codec.EncodeHC(byteArray, 0, byteArray.Length);
                        break;

                    case XISFCompressionTypeEnum.ZLIB:
                        if (ByteShuffling) {
                            CompressionName = "zlib+sh";
                            byteArray = Shuffle(byteArray, ShuffleItemSize);
                        } else {
                            CompressionName = "zlib";
                        }

                        outArray = ZlibStream.CompressBuffer(byteArray);
                        break;

                    case XISFCompressionTypeEnum.NONE:
                    default:
                        outArray = byteArray;
                        CompressionName = null;
                        break;
                }
            }

            if (CompressionType != XISFCompressionTypeEnum.NONE) {
                double percentChanged = (1 - ((double)outArray.Length / (double)byteArray.Length)) * 100;
                Logger.Debug($"XISF: {CompressionType} compressed {byteArray.Length} bytes to {outArray.Length} bytes ({percentChanged.ToString("#.##")}%)");
            }

            /*
             * Checksum the data block as configured.
             * If the data block is compressed, we always checksum the compressed form, not the uncompressed form.
             */
            using (MyStopWatch.Measure($"XISF Checksum = {ChecksumType}")) {
                SHA3Managed sha3;

                switch (ChecksumType) {
                    case XISFChecksumTypeEnum.SHA1:
                        SHA1 sha1 = new SHA1CryptoServiceProvider();
                        Checksum = GetStringFromHash(sha1.ComputeHash(outArray));
                        ChecksumName = "sha-1";
                        sha1.Dispose();
                        break;

                    case XISFChecksumTypeEnum.SHA256:
                        SHA256 sha256 = new SHA256CryptoServiceProvider();
                        Checksum = GetStringFromHash(sha256.ComputeHash(outArray));
                        ChecksumName = "sha-256";
                        sha256.Dispose();
                        break;

                    case XISFChecksumTypeEnum.SHA512:
                        SHA512 sha512 = new SHA512CryptoServiceProvider();
                        Checksum = GetStringFromHash(sha512.ComputeHash(outArray));
                        ChecksumName = "sha-512";
                        sha512.Dispose();
                        break;

                    case XISFChecksumTypeEnum.SHA3_256:
                        sha3 = new SHA3Managed(256);
                        Checksum = GetStringFromHash(sha3.ComputeHash(outArray));
                        ChecksumName = "sha3-256";
                        sha3.Dispose();
                        break;

                    case XISFChecksumTypeEnum.SHA3_512:
                        sha3 = new SHA3Managed(512);
                        Checksum = GetStringFromHash(sha3.ComputeHash(outArray));
                        ChecksumName = "sha3-512";
                        sha3.Dispose();
                        break;

                    case XISFChecksumTypeEnum.NONE:
                    default:
                        Checksum = null;
                        ChecksumName = null;
                        break;
                }
            }

            return outArray;
        }

        private static string GetStringFromHash(byte[] hash) {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) {
                result.Append(hash[i].ToString("x2"));
            }
            return result.ToString();
        }

        private static byte[] Shuffle(byte[] unshuffled, int itemSize) {
            int size = unshuffled.Length;
            byte[] shuffled = new byte[size];
            int numberOfItems = size / itemSize;
            int s = 0;

            using (MyStopWatch.Measure("XISF Byte Shuffle")) {
                for (int j = 0; j < itemSize; ++j) {
                    int u = 0 + j;
                    for (int i = 0; i < numberOfItems; ++i, ++s, u += itemSize) {
                        shuffled[s] = unshuffled[u];
                    }
                }
            }

            return shuffled;
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