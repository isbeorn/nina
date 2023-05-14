#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Image.FileFormat.FITS {

    /// <summary>
    /// Specification:
    /// https://fits.gsfc.nasa.gov/fits_standard.html
    /// http://archive.stsci.edu/fits/fits_standard/fits_standard.html
    /// </summary>
    public class FITS {

        public FITS(ushort[] data, int width, int height) : this(data, width, height, FITSRowOrder.TOP_DOWN) {
        }

        public FITS(ushort[] data, int width, int height, FITSRowOrder rowOrder) {
            this.Header = new FITSHeader(width, height, rowOrder);
            this.Data = new FITSData(data, width, height, rowOrder);
        }


        public static Task<IImageData> Load(Uri filePath, bool isBayered, IImageDataFactory imageDataFactory, CancellationToken ct) {
            return Task.Run<IImageData>(() => LoadInternal(filePath, isBayered, imageDataFactory, ct), ct);
        }

        [SecurityCritical]
        private static IImageData LoadInternal(Uri filePath, bool isBayered, IImageDataFactory imageDataFactory, CancellationToken ct) {
            IntPtr fitsPtr = IntPtr.Zero;
            IntPtr buffer = IntPtr.Zero;
            try {
                var bytes = File.ReadAllBytes(filePath.LocalPath);

                buffer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, buffer, bytes.Length);

                UIntPtr size = new UIntPtr((uint)bytes.Length);
                UIntPtr deltaSize = UIntPtr.Zero;

                CfitsioNative.fits_open_memory(out fitsPtr, string.Empty, CfitsioNative.IOMODE.READONLY, ref buffer, ref size, ref deltaSize, IntPtr.Zero, out var status);
                CfitsioNative.CheckStatus("fits_open_memory", status);

                var dimensions = CfitsioNative.fits_read_key_long(fitsPtr, "NAXIS");
                if (dimensions > 2) {
                    Logger.Warning("Reading debayered FITS images not supported. Reading the first 2 axes to get a monochrome image");
                }

                var width = (int)CfitsioNative.fits_read_key_long(fitsPtr, "NAXIS1");
                var height = (int)CfitsioNative.fits_read_key_long(fitsPtr, "NAXIS2");
                var bitPix = (CfitsioNative.BITPIX)(int)CfitsioNative.fits_read_key_long(fitsPtr, "BITPIX");
                var pixels = CfitsioNative.read_ushort_pixels(fitsPtr, bitPix, 2, width * height);

                //Translate CFITSio into N.I.N.A. FITSHeader
                FITSHeader header = new FITSHeader(width, height);
                CfitsioNative.fits_get_hdrspace(fitsPtr, out var numKeywords, out var numMoreKeywords, out status);
                CfitsioNative.CheckStatus("fits_get_hdrspace", status);
                for (int headerIdx = 1; headerIdx <= numKeywords; ++headerIdx) {
                    CfitsioNative.fits_read_keyn(fitsPtr, headerIdx, out var keyName, out var keyValue, out var keyComment);

                    if (string.IsNullOrEmpty(keyValue) || keyName.Equals("COMMENT") || keyName.Equals("HISTORY")) {
                        continue;
                    }

                    if (keyValue.Equals("T")) {
                        header.Add(keyName, true, keyComment);
                    } else if (keyValue.Equals("F")) {
                        header.Add(keyName, false, keyComment);
                    } else if (keyValue.StartsWith("'")) {
                        // Treat as a string
                        keyValue = $"{keyValue.TrimStart('\'').TrimEnd('\'', ' ').Replace(@"''", @"'")}";
                        header.Add(keyName, keyValue, keyComment);

                    } else if (keyValue.Contains(".")) {
                        if (double.TryParse(keyValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
                            header.Add(keyName, value, keyComment);
                        }
                    } else {
                        if (int.TryParse(keyValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) {
                            header.Add(keyName, value, keyComment);
                        } else {
                            // Treat as a string
                            keyValue = $"{keyValue.TrimStart('\'').TrimEnd('\'', ' ').Replace(@"''", @"'")}";
                            header.Add(keyName, keyValue, keyComment);
                        }
                    }
                }

                var metaData = new ImageMetaData();
                try {
                    metaData = header.ExtractMetaData();
                } catch (Exception ex) {
                    Logger.Error(ex.Message);
                }
                return imageDataFactory.CreateBaseImageData(pixels, width, height, 16, isBayered, metaData);

            } catch (AccessViolationException ex) {
                Logger.Error($"{nameof(FITS)} - Access Violation Exception occurred during cfitsio load!", ex);
                // Finally blocks are not executed after corrupted state exception
                if (fitsPtr != IntPtr.Zero) {
                    try {
                        CfitsioNative.fits_close_file(fitsPtr, out var status);
                    } catch(Exception) { }                    
                }
                if (buffer != IntPtr.Zero) {
                    Marshal.FreeHGlobal(buffer);
                }
                throw new Exception($"Unable to load FITS file from {filePath.LocalPath}");
            } finally {
                if (fitsPtr != IntPtr.Zero) {
                    CfitsioNative.fits_close_file(fitsPtr, out var status);
                }
                if (buffer != IntPtr.Zero) {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        public FITSHeader Header { get; }

        public FITSData Data { get; }

        /// <summary>
        /// Fills FITS Header Cards using all available ImageMetaData information
        /// </summary>
        /// <param name="metaData"></param>
        public void PopulateHeaderCards(ImageMetaData metaData) {
            this.Header.PopulateFromMetaData(metaData);
        }

        public void Write(Stream s) {
            this.Header.Write(s);

            this.Data.Write(s);

            long remainingBlockPadding = (long)Math.Ceiling((double)s.Position / (double)BLOCKSIZE) * (long)BLOCKSIZE - s.Position;
            byte zeroByte = 0;
            //Pad remaining FITS block with zero values
            for (int i = 0; i < remainingBlockPadding; i++) {
                s.WriteByte(zeroByte);
            }
        }

        /* Header card size Specification: http://archive.stsci.edu/fits/fits_standard/node29.html#SECTION00912100000000000000 */
        public const int HEADERCARDSIZE = 80;
        /* Blocksize specification: http://archive.stsci.edu/fits/fits_standard/node13.html#SECTION00810000000000000000 */
        public const int BLOCKSIZE = 2880;
        public const int BITPIX_BYTE = 8;
        public const int BITPIX_SHORT = 16;
        public const int BITPIX_INT = 32;
        public const int BITPIX_LONG = 64;
        public const int BITPIX_FLOAT = -32;
        public const int BITPIX_DOUBLE = -64;
    }
}