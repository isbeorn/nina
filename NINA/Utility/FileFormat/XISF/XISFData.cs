#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Ionic.Zlib;
using LZ4;
using NINA.Utility.Enum;
using SHA3;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NINA.Utility.FileFormat.XISF {

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
        public XISFCompressionTypeEnum CompressionType { get; private set; }

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

            // Revert to the original data array in case the compression is bigger than uncompressed
            if (outArray.Length > byteArray.Length) {
                if (ByteShuffling) {
                    //As the original array is shuffled it needs to be unshuffled again - this scenario should be highly unlikely anyways
                    outArray = Unshuffle(byteArray, ShuffleItemSize);
                } else {
                    outArray = byteArray;
                }
                CompressionType = XISFCompressionTypeEnum.NONE;

                Logger.Debug("XISF output array is larger after compression. Image will be prepared uncompressed instead.");
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

        public static byte[] Shuffle(byte[] unshuffled, int itemSize) {
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

        public static byte[] Unshuffle(byte[] shuffled, int itemSize) {
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
    }
}
