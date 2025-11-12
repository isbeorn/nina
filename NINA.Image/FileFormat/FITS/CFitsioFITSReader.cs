using NINA.Core.Utility;
using NINA.Image.ImageData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static NINA.Image.FileFormat.FITS.CfitsioNative;

namespace NINA.Image.FileFormat.FITS {
    public class CFitsioFITSReader : IDisposable {
        private nint filePtr;
        private string tempFile;

        public CFitsioFITSReader(string filePath) {
            CfitsioNative.fits_open_file(out filePtr, filePath, CfitsioNative.IOMODE.READONLY, out var status);
            CfitsioNative.CheckStatus("fits_open_file", status);

            try {
                CfitsioNative.fits_read_key_long(filePtr, "NAXIS1");
            } catch {
                // When NAXIS1 does not exist, try at the last HDU - e.g. when the image is tile compressed
                CfitsioNative.fits_get_num_hdus(filePtr, out int hdunum, out status);
                CfitsioNative.CheckStatus("fits_get_num_hdus", status);
                if (hdunum > 1) {
                    CfitsioNative.fits_movabs_hdu(filePtr, hdunum, out var hdutypenow, out status);
                    CfitsioNative.CheckStatus("fits_movabs_hdu", status);
                }
            }

            var compressionFlag = CfitsioNative.fits_is_compressed_image(filePtr, out status);
            CfitsioNative.CheckStatus("fits_is_compressed_image", status);
            if (compressionFlag > 0) {
                // When the image is compresse, we decompress it into a temporary file
                tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".fits");
                CfitsioNative.fits_create_file(out var ptr, tempFile, out status);
                CfitsioNative.CheckStatus("fits_create_file", status);

                CfitsioNative.fits_img_decompress(filePtr, ptr, out status);
                CfitsioNative.CheckStatus("fits_img_decompress", status);

                // Free resources for current file
                if (filePtr != IntPtr.Zero) {
                    CfitsioNative.fits_close_file(filePtr, out status);
                    CfitsioNative.CheckStatus("fits_close_file", status);
                    CfitsioNative.fits_close_file(ptr, out status);
                    CfitsioNative.CheckStatus("fits_close_file", status);
                }

                CfitsioNative.fits_open_file(out filePtr, tempFile, CfitsioNative.IOMODE.READONLY, out status);
                CfitsioNative.CheckStatus("fits_open_file", status);
            }

            var dimensions = CfitsioNative.fits_read_key_long(filePtr, "NAXIS");
            if (dimensions > 2) {
                throw new InvalidOperationException("Reading debayered FITS images not supported.");
            }

            Width = (int)CfitsioNative.fits_read_key_long(filePtr, "NAXIS1");
            Height = (int)CfitsioNative.fits_read_key_long(filePtr, "NAXIS2");
            BitPix = (CfitsioNative.BITPIX)(int)CfitsioNative.fits_read_key_long(filePtr, "BITPIX");
        }

        public int Width { get; }
        public int Height { get; }
        public BITPIX BitPix { get; }

        public T[] ReadPixelRow<T>(int row) {
            const int nelem = 2;
            var firstpix = new int[nelem] { 1, row + 1 };

            var datatype = GetDataType(typeof(T));

            unsafe {
                var resultBuffer = new T[Width];

                var nulVal = default(T);
                var nulValRef = &nulVal;
                fixed (T* fixedBuffer = resultBuffer) {
                    var result = fits_read_pix(filePtr, datatype, firstpix, Width, (IntPtr)nulValRef, (IntPtr)fixedBuffer, out var nullCount, out var status);
                    CheckStatus("fits_read_pix", status);
                }
                return resultBuffer;
            }
        }

        public T[] ReadAllPixels<T>() {
            const int nelem = 2;
            var firstpix = new int[nelem] { 1, 1 };

            var datatype = GetDataType(typeof(T));

            unsafe {
                var resultBuffer = new T[Width * Height];

                var nulVal = default(T);
                var nulValRef = &nulVal;
                fixed (T* fixedBuffer = resultBuffer) {
                    var result = fits_read_pix(filePtr, datatype, firstpix, Width * Height, (IntPtr)nulValRef, (IntPtr)fixedBuffer, out var nullCount, out var status);
                    CheckStatus("fits_read_pix", status);
                }
                return resultBuffer;
            }
        }

        public float[] ReadAllPixelsAsFloat() {
            var naxes = 2;
            var nelem = Width * Height;
            return CfitsioNative.read_float_pixels(filePtr, BitPix, naxes, nelem);
        }

        public ushort[] ReadAllPixelsAsUshort() {
            var naxes = 2;
            var nelem = Width * Height;
            return CfitsioNative.read_ushort_pixels(filePtr, BitPix, naxes, nelem);
        }

        public double ReadDoubleHeader(string keyname) {
            return CfitsioNative.fits_read_key_double(filePtr, keyname);
        }

        public FITSHeader ReadHeader() {
            FITSHeader header = new FITSHeader(Width, Height);
            CfitsioNative.fits_get_hdrspace(filePtr, out var numKeywords, out var numMoreKeywords, out var status);
            CfitsioNative.CheckStatus("fits_get_hdrspace", status);
            for (int headerIdx = 1; headerIdx <= numKeywords; ++headerIdx) {
                CfitsioNative.fits_read_keyn(filePtr, headerIdx, out var keyName, out var keyValue, out var keyComment);

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
            return header;
        }

        public static DATATYPE GetDataType(Type T) {
            if (T == typeof(ushort)) {
                return DATATYPE.TUSHORT;
            }

            if (T == typeof(uint)) {
                return DATATYPE.TUINT;
            }

            if (T == typeof(int)) {
                return DATATYPE.TINT;
            }

            if (T == typeof(short)) {
                return DATATYPE.TSHORT;
            }

            if (T == typeof(float)) {
                return DATATYPE.TFLOAT;
            }

            if (T == typeof(double)) {
                return DATATYPE.TDOUBLE;
            }

            throw new ArgumentException("Invalid cfitsio data type " + T.Name);
        }

        internal ImageMetaData TranslateToMetaData() {
            //Translate CFITSio into N.I.N.A. FITSHeader
            FITSHeader header = new FITSHeader(Width, Height);
            CfitsioNative.fits_get_hdrspace(filePtr, out var numKeywords, out var numMoreKeywords, out var status);
            CfitsioNative.CheckStatus("fits_get_hdrspace", status);
            for (int headerIdx = 1; headerIdx <= numKeywords; ++headerIdx) {
                CfitsioNative.fits_read_keyn(filePtr, headerIdx, out var keyName, out var keyValue, out var keyComment);

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
            return metaData;
        }

        private bool _disposed = false;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                }

                if (filePtr != IntPtr.Zero) {
                    CfitsioNative.fits_close_file(filePtr, out var status);
                    filePtr = IntPtr.Zero;
                }

                if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile)) {
                    try {
                        File.Delete(tempFile);
                        tempFile = null; // Clear reference after deletion
                    } catch (Exception ex) {
                        // Log the exception or handle it as needed
                        Logger.Error($"Failed to delete temp file", ex);
                    }
                }

                _disposed = true;
            }
        }
    }
}
