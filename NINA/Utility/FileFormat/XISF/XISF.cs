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
using NINA.Utility.FileFormat.XISF.DataConverter;
using SHA3;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NINA.Utility.FileFormat.XISF {

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
                 * Create the header for ease of access
                 */
                XElement xml = XElement.Parse(xmlString);
                var header = new XISFHeader(xml);

                var metaData = new ImageMetaData();
                try {
                    metaData = header.ExtractMetaData();
                } catch (Exception ex) {
                    Logger.Error($"XISF: Error during metadata extraction {ex.Message}");
                }

                /*
                 * Retrieve the geometry attribute.
                 */
                int width = 0;
                int height = 0;

                try {
                    string[] geometry = header.Image.Attribute("geometry").Value.Split(':');
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
                string sampleFormat = "UInt16";
                try {
                    sampleFormat = header.Image.Attribute("sampleFormat").Value.ToString();
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
                    compression = header.Image.Attribute("compression").Value.ToLower().Split(':');

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
                    if (header.Image.Attribute("checksum") != null) {
                        // [hash type]:[hash string]
                        cksum = header.Image.Attribute("checksum").Value.ToLower().Split(':');

                        if (!string.IsNullOrEmpty(cksum[0])) {
                            cksumType = GetChecksumType(cksum[0]);
                            cksumHash = cksum[1];
                        }
                    } else {
                        Logger.Debug("XISF: Checksummed data block was not encountered");
                    }
                } catch (InvalidDataException) {
                    Logger.Error($"XISF: Unknown checksum type: {cksum[0]}");
                    throw new InvalidDataException(string.Format(Locale.Loc.Instance["LblXisfUnsupportedChecksum"], cksum[0]));
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

                if (header.Image.Attribute("location").Value.StartsWith("attachment")) {
                    string[] location = header.Image.Attribute("location").Value.Split(':');
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
                        raw = UncompressData(raw, compressionInfo.CompressionType, compressionInfo.UncompressedSize);

                        if (compressionInfo.IsShuffled) {
                            raw = XISFData.Unshuffle(raw, compressionInfo.ItemSize);
                        }
                    }

                    var converter = GetConverter(sampleFormat);
                    var img = converter.Convert(raw);

                    imageData = new ImageData(img, width, height, 16, isBayered, metaData);
                } else {
                    string base64Img = header.Image.Element("Data").Value;
                    byte[] encodedImg = Convert.FromBase64String(base64Img);

                    var converter = GetConverter(sampleFormat);
                    var img = converter.Convert(encodedImg);

                    imageData = new ImageData(img, width, height, 16, isBayered, metaData);
                }

                return imageData;
            }
        }

        private static IDataConverter GetConverter(string sampleFormat) {
            switch (sampleFormat) {
                case "UInt8":
                    return new UInt8Converter();

                case "UInt16":
                    return new UInt16Converter();

                case "UInt32":
                    return new UInt32Converter();

                case "UInt64":
                    return new UInt64Converter();

                case "Float32":
                    return new Float32Converter();

                case "Float64":
                    return new Float64Converter();

                default: throw new InvalidDataException(string.Format(Locale.Loc.Instance["LblXisfUnsupportedFormat"], sampleFormat));
            }
        }

        private static unsafe ushort GetConvertedPixelValue(byte[] rawData, int offset, string sampleFormat) {
            switch (sampleFormat) {
                case "UInt8":
                    return (ushort)(((rawData[offset]) / (double)byte.MaxValue) * ushort.MaxValue);

                case "UInt16":
                    return (ushort)((rawData[(offset * 2) + 1] << 8) | (rawData[offset * 2]));

                case "UInt32":
                    return (ushort)(((rawData[(offset * 4) + 3] << 24) | (rawData[(offset * 4) + 2] << 16) | (rawData[(offset * 4) + 1] << 8) | (rawData[offset * 4])) / (double)int.MaxValue * ushort.MaxValue);

                case "UInt64":
                    return (ushort)((((long)rawData[(offset * 8) + 7] << 56) | ((long)rawData[(offset * 8) + 6] << 48) | ((long)rawData[(offset * 8) + 5] << 40) | ((long)rawData[(offset * 8) + 4] << 32) | ((long)rawData[(offset * 8) + 3] << 24) | ((long)rawData[(offset * 8) + 2] << 16) | ((long)rawData[(offset * 8) + 1] << 8) | ((long)rawData[offset * 8])) / (double)long.MaxValue * ushort.MaxValue);

                case "Float32":
                    var integer = ((rawData[(offset * 4) + 3] << 24) | (rawData[(offset * 4) + 2] << 16) | (rawData[(offset * 4) + 1] << 8) | (rawData[offset * 4]));
                    return (ushort)((*(float*)&integer) * ushort.MaxValue);

                case "Float64":
                    var l = (((long)rawData[(offset * 8) + 7] << 56) | ((long)rawData[(offset * 8) + 6] << 48) | ((long)rawData[(offset * 8) + 5] << 40) | ((long)rawData[(offset * 8) + 4] << 32) | ((long)rawData[(offset * 8) + 3] << 24) | ((long)rawData[(offset * 8) + 2] << 16) | ((long)rawData[(offset * 8) + 1] << 8) | ((long)rawData[offset * 8]));
                    return (ushort)((*(double*)&l) * ushort.MaxValue);

                default: throw new InvalidDataException(string.Format(Locale.Loc.Instance["LblXisfUnsupportedFormat"], sampleFormat));
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

        public void AddAttachedImage(ushort[] data, FileSaveInfo fileSaveInfo) {
            if (Header.Image == null) { throw new InvalidOperationException("No Image Header Information available for attaching image. Add Image Header first!"); }

            // Add Attached data location info to header
            Data = new XISFData(data, fileSaveInfo);

            if (Data.ChecksumType != XISFChecksumTypeEnum.NONE) {
                Header.Image.Add(new XAttribute("checksum", $"{Data.ChecksumName}:{Data.Checksum}"));
            }

            int headerLengthBytes = 4;
            int reservedBytes = 4;
            int attachmentInfoMaxBytes = 256; // Assume max 256 bytes for the attachment, compression, and checksum attributes.
            int currentHeaderSize = Header.ByteCount + xisfSignature.Length + headerLengthBytes + reservedBytes + attachmentInfoMaxBytes;

            int dataBlockStart = currentHeaderSize + (PaddedBlockSize - currentHeaderSize % PaddedBlockSize);

            if (Data.CompressionType != XISFCompressionTypeEnum.NONE) {
                Header.Image.Add(new XAttribute("location", $"attachment:{dataBlockStart}:{Data.CompressedSize}"));

                if (Data.ByteShuffling == true) {
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
}