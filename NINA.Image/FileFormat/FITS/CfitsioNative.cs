using NINA.Core.Utility;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NINA.Image.FileFormat.FITS {
    public class CfitsioNative {

        public class cfitsioException : Exception {
            public string Op { get; private set; }
            public int StatusCode { get; private set; }
            public cfitsioException(string op, int statusCode) : base($"{op} failed with {statusCode} = {fits_get_errstatus(statusCode)}") {
                this.Op = op;
                this.StatusCode = statusCode;
            }
        }

        private const string DLLNAME = "cfitsionative.dll";
        static CfitsioNative() {
            DllLoader.LoadDll(Path.Combine("Cfitsio", DLLNAME));
        }

        #region CONSTANTS
        // max length of a filename
        private const int FLEN_FILENAME = 1025;

        // length of a FITS header card
        private const int FLEN_CARD = 81;

        // max length of a keyword (HIERARCH convention)
        private const int FLEN_KEYWORD = 75;

        // max length of a FITSIO error message
        private const int FLEN_ERRMSG = 81;

        // max length of a keyword value string
        private const int FLEN_VALUE = 71;

        // max length of a keyword comment string
        private const int FLEN_COMMENT = 73;
        #endregion

        #region ENUMS
        public enum IOMODE : int {
            READONLY = 0,
            READWRITE = 1
        }

        public enum BITPIX : int {
            BYTE_IMG = 8,
            SHORT_IMG = 16,
            USHORT_IMG = 20,
            LONG_IMG = 32,
            ULONG_IMG = 40,
            LONGLONG_IMG = 64,
            FLOAT_IMG = -32,
            DOUBLE_IMG = -64
        }

        public enum DATATYPE : int {
            TBIT = 1,
            TBYTE = 11,
            TSBYTE = 12,
            TLOGICAL = 14,
            TSTRING = 16,
            TUSHORT = 20,
            TSHORT = 21,
            TUINT = 30,
            TINT = 31,
            TULONG = 40,
            TLONG = 41,
            TFLOAT = 42,
            TULONGLONG = 80,
            TLONGLONG = 81,
            TDOUBLE = 82,
            TCOMPLEX = 83,
            TDBLCOMPLEX = 163
        }

        public enum COMPRESSION : int {
            RICE_1 = 11,
            GZIP_1 = 21,
            GZIP_2 = 22,
            PLIO_1 = 31,
            HCOMPRESS_1 = 41,
            NOCOMPRESS = -1
        }

        public enum HDUTYPE : int {
            IMAGE_HDU = 0,
            ASCII_TBL = 1,
            BINARY_TBL = 2,
            ANY_HDU = -1
        }
        #endregion

        public static void CheckStatus(string op, int statusCode) {
            if (statusCode != 0) {
                throw new cfitsioException(op, statusCode);
            }
        }

        public static void LogErrorStatus(string op, int statusCode) {
            if(statusCode != 0) {
                Logger.Error($"{op} failed with {statusCode} = {fits_get_errstatus(statusCode)}");
            }
        }

        // int CFITS_API ffomem(itsfile **fptr, const char* name, int mode, void** buffptr, size_t *buffsize, size_t deltasize, void* (* mem_realloc) (void* p, size_t newsize), int* status);
        [DllImport(DLLNAME, EntryPoint = "ffomem", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_open_memory(out IntPtr fptr, string filename, IOMODE iomode, ref IntPtr buffer, ref UIntPtr size, ref UIntPtr deltaSize, IntPtr fnMemRealloc, out int status);

        // int CFITS_API ffopen(fitsfile **fptr, const char *filename, int iomode, int *status);
        [DllImport(DLLNAME, EntryPoint = "ffopen", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_open_file(out IntPtr fptr, string filename, IOMODE iomode, out int status);

        // int CFITS_API ffghsp(fitsfile *fptr, int *nexist, int *nmore, int *status);
        [DllImport(DLLNAME, EntryPoint = "ffghsp", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_get_hdrspace(IntPtr fptr, out int nexist, out int nmore, out int status);

        // int CFITS_API ffgrec(fitsfile *fptr, int nrec,      char *card, int *status);
        [DllImport(DLLNAME, CharSet = CharSet.Ansi, EntryPoint = "ffgrec", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_read_record(
            IntPtr fptr,
            int nrec,
            [Out] StringBuilder card,
            out int status);

        // int CFITS_API ffclos(fitsfile *fptr, int *status);
        [DllImport(DLLNAME, EntryPoint = "ffclos", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_close_file(IntPtr fptr, out int status);

        // int CFITS_API ffgidt(fitsfile *fptr, int *imgtype, int *status);
        [DllImport(DLLNAME, EntryPoint = "ffgidt", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_get_img_type(IntPtr fptr, out BITPIX imgtype, out int status);

        // int CFITS_API ffgidm(fitsfile *fptr, int *naxis,  int *status);
        [DllImport(DLLNAME, EntryPoint = "ffgidm", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_get_img_dim(IntPtr fptr, out int naxis, out int status);

        // int CFITS_API ffgisz(fitsfile *fptr, int nlen, long *naxes, int *status);
        [DllImport(DLLNAME, EntryPoint = "ffgisz", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_get_img_size(IntPtr fptr, out int nlen, out int[] naxes, out int status);

        // int CFITS_API ffpcks(fitsfile *fptr, int *status);
        [DllImport(DLLNAME, EntryPoint = "ffpcks", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_write_chksum(IntPtr fptr, out int status);

        // int CFITS_API ffgpxv(fitsfile *fptr, int  datatype, long *firstpix, LONGLONG nelem, void* nulval, void* array, int* anynul, int* status);
        [DllImport(DLLNAME, EntryPoint = "ffgpxv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_read_pix(
            IntPtr fptr,
            DATATYPE datatype,
            int[] firstpix,
            long nelem,
            IntPtr nulval,
            IntPtr array,
            out int anynul,
            out int status);

        // fits_read_key_long      ffgkyj
        // int CFITS_API ffgkyj(fitsfile *fptr, const char *keyname, long *value, char *comm, int *status);
        [DllImport(DLLNAME, CharSet = CharSet.Ansi, EntryPoint = "ffgkyj", CallingConvention = CallingConvention.Cdecl)]
        private static extern int _fits_read_key_long(
            IntPtr fptr,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_KEYWORD)] string keyname,
            out long value,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_COMMENT)] StringBuilder comm,
            out int status);
        public static long fits_read_key_long(IntPtr fptr, string keyname) {
            _fits_read_key_long(fptr, keyname, out var value, null, out var status);
            CheckStatus("fits_read_key_long", status);
            return value;
        }
        // fits_read_key_double      ffgkye
        [DllImport("cfitsionative.dll", CharSet = CharSet.Ansi, EntryPoint = "ffgkyd", CallingConvention = CallingConvention.Cdecl)]
        private static extern int _fits_read_key_double(
            IntPtr fptr,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_KEYWORD)] string keyname,
            out double value,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_COMMENT)] StringBuilder comm,
            out int status);

        public static double fits_read_key_double(IntPtr fptr, string keyname) {
            _fits_read_key_double(fptr, keyname, out var value, null, out var status);
            CheckStatus("fits_read_key_long", status);
            return value;
        }

        // void CFITS_API ffgerr(int status, char *errtext);
        [DllImport(DLLNAME, CharSet = CharSet.Ansi, EntryPoint = "ffgerr", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _fits_get_errstatus(
            int status,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_ERRMSG)] StringBuilder errtext);

        public static string fits_get_errstatus(int status) {
            var sb = new StringBuilder(FLEN_ERRMSG);
            _fits_get_errstatus(status, sb);
            return sb.ToString();
        }

        public static DATATYPE GetDataType(Type T) {
            if (T == typeof(ushort)) {
                return DATATYPE.TUSHORT;
            } else if (T == typeof(uint)) {
                return DATATYPE.TUINT;
            } else if (T == typeof(int)) {
                return DATATYPE.TINT;
            } else if (T == typeof(short)) {
                return DATATYPE.TSHORT;
            } else if (T == typeof(float)) {
                return DATATYPE.TFLOAT;
            } else if (T == typeof(double)) {
                return DATATYPE.TDOUBLE;
            }
            throw new ArgumentException($"Invalid cfitsio data type {T.Name}");
        }

        public static ushort[] read_ushort_pixels(IntPtr fptr, BITPIX bitPix, int naxes, int nelem) {
            if (bitPix == BITPIX.BYTE_IMG) {
                var pixels = read_pixels<byte>(fptr, naxes, nelem);
                return ToUshortArray(pixels);
            } else if (bitPix == BITPIX.DOUBLE_IMG) {
                var pixels = read_pixels<double>(fptr, naxes, nelem);
                return ToUshortArray(pixels);
            } else if (bitPix == BITPIX.FLOAT_IMG) {
                var pixels = read_pixels<float>(fptr, naxes, nelem);
                return ToUshortArray(pixels);
            } else if (bitPix == BITPIX.LONGLONG_IMG) {
                var pixels = read_pixels<long>(fptr, naxes, nelem);
                return ToUshortArray(pixels);
            } else if (bitPix == BITPIX.LONG_IMG) {
                var pixels = read_pixels<int>(fptr, naxes, nelem);
                return ToUshortArray(pixels);
            } else if (bitPix == BITPIX.SHORT_IMG) {
                var pixels = read_pixels<ushort>(fptr, naxes, nelem);
                return pixels;
            } else {
                throw new ArgumentException($"Invalid BITPIX {bitPix}");
            }
        }

        private static ushort[] ToUshortArray(byte[] src) {
            ushort[] pixels = new ushort[src.Length];
            for (int i = 0; i < src.Length; i++) {
                pixels[i] = (ushort)((src[i] / (double)byte.MaxValue) * ushort.MaxValue);
            }
            return pixels;
        }

        private static ushort[] ToUshortArray(double[] src) {
            ushort[] pixels = new ushort[src.Length];
            for (int i = 0; i < src.Length; i++) {
                pixels[i] = (ushort)(src[i] * ushort.MaxValue);
            }
            return pixels;
        }

        private static ushort[] ToUshortArray(float[] src) {
            ushort[] pixels = new ushort[src.Length];
            for (int i = 0; i < src.Length; i++) {
                pixels[i] = (ushort)(src[i] * ushort.MaxValue);
            }
            return pixels;
        }

        private static ushort[] ToUshortArray(long[] src) {
            ushort[] pixels = new ushort[src.Length];
            for (int i = 0; i < src.Length; i++) {
                pixels[i] = (ushort)((((double)src[i] - long.MinValue) / ((double)long.MaxValue - long.MinValue)) * ushort.MaxValue);
            }
            return pixels;
        }

        private static ushort[] ToUshortArray(int[] src) {
            ushort[] pixels = new ushort[src.Length];
            for (int i = 0; i < src.Length; i++) {
                pixels[i] = (ushort)((((double)src[i] - int.MinValue) / ((double)int.MaxValue - int.MinValue)) * ushort.MaxValue);
            }
            return pixels;
        }

        public static T[] read_pixels<T>(IntPtr fptr, int naxes, int nelem) where T : unmanaged {
            var firstpix = new int[naxes];
            for (int i = 0; i < naxes; i++) {
                firstpix[i] = 1;
            }

            var datatype = GetDataType(typeof(T));
            unsafe {
                var resultBuffer = new T[nelem];

                var nulVal = default(T);
                var nulValRef = &nulVal;
                fixed (T* fixedBuffer = resultBuffer) {
                    var result = fits_read_pix(fptr, datatype, firstpix, nelem, (IntPtr)nulValRef, (IntPtr)fixedBuffer, out var nullCount, out var status);
                    CheckStatus("fits_read_pix", status);
                }
                return resultBuffer;
            }
        }

        // int CFITS_API ffgkys(fitsfile *fptr, const char *keyname, char *value, char *comm, int *status);
        [DllImport(DLLNAME, CharSet = CharSet.Ansi, EntryPoint = "ffgkys", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _fits_read_key_str(
            IntPtr fptr,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_KEYWORD)] string keyname,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_VALUE)] StringBuilder value,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_COMMENT)] StringBuilder comm,
            out int status);

        // int CFITS_API ffgkyn(fitsfile *fptr, int nkey, char *keyname, char *keyval, char *comm, int* status);
        [DllImport(DLLNAME, CharSet = CharSet.Ansi, EntryPoint = "ffgkyn", CallingConvention = CallingConvention.Cdecl)]
        private static extern int _fits_read_keyn(
            IntPtr fptr,
            int nkey,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_KEYWORD)] StringBuilder keyname,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_VALUE)] StringBuilder value,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_COMMENT)] StringBuilder comm,
            out int status);
        public static void fits_read_keyn(IntPtr fptr, int nkey, out string keyname, out string value, out string comm) {
            var keynamesb = new StringBuilder(FLEN_KEYWORD);
            var valuesb = new StringBuilder(FLEN_VALUE);
            var commsb = new StringBuilder(FLEN_COMMENT);

            _fits_read_keyn(fptr, nkey, keynamesb, valuesb, commsb, out var status);
            CheckStatus("fits_read_keyn", status);
            keyname = keynamesb.ToString();
            value = valuesb.ToString();
            comm = commsb.ToString();
        }

        // ffpkys(fitsfile *fptr, const char *keyname, const char *value, const char *comm,int *status);
        [DllImport(DLLNAME, CharSet = CharSet.Ansi, EntryPoint = "ffpkys", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_write_key_str(
            IntPtr fptr,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_KEYWORD)] string keyname,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_VALUE)] string value,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_COMMENT)] string comment,
            out int status);

        // int CFITS_API ffcrim(fitsfile *fptr, int bitpix, int naxis, long *naxes, int *status);
        [DllImport(DLLNAME, EntryPoint = "ffcrim", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_create_img(IntPtr fptr, BITPIX bitpix, int naxis, int[] naxes, out int status);

        // int CFITS_API ffinit(  fitsfile **fptr, const char *filename, int *status);
        [DllImport(DLLNAME, CharSet = CharSet.Ansi, EntryPoint = "ffinit", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_create_file(
            out IntPtr fptr,
            [MarshalAs(UnmanagedType.LPStr, SizeConst = FLEN_FILENAME)] string filename,
            out int status);

        [DllImport(DLLNAME, EntryPoint = "ffppr", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_write_img(IntPtr fptr, DATATYPE datatype, long fpixel, long nelements, ushort[] array, out int status);
        [DllImport(DLLNAME, EntryPoint = "ffppr", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_write_img_int(IntPtr fptr, DATATYPE datatype, long fpixel, long nelements, int[] array, out int status);
        [DllImport(DLLNAME, EntryPoint = "ffppr", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_write_img_uint(IntPtr fptr, DATATYPE datatype, long fpixel, long nelements, uint[] array, out int status);
        [DllImport(DLLNAME, EntryPoint = "ffppr", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_write_img_float(IntPtr fptr, DATATYPE datatype, long fpixel, long nelements, float[] array, out int status);
        [DllImport(DLLNAME, EntryPoint = "ffppr", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_write_img_double(IntPtr fptr, DATATYPE datatype, long fpixel, long nelements, double[] array, out int status);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr MemReallocDelegate(IntPtr ptr, IntPtr newSize);

        [DllImport(DLLNAME, EntryPoint = "ffimem", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_create_memfile(out IntPtr fptr, ref IntPtr memptr, ref IntPtr memsize, IntPtr deltasize, MemReallocDelegate memRealloc, out int status);

        public static IntPtr ReallocateMemory(IntPtr ptr, IntPtr newSize) {
            // Reallocate the block of memory to the new size
            return Marshal.ReAllocHGlobal(ptr, newSize);
        }

        public static int fits_update_key_dbl(IntPtr fptr, string keyname, ref double value, string comment, out int status) {
            return fits_update_key(fptr, DATATYPE.TDOUBLE, keyname, ref value, comment, out status);
        }

        [DllImport(DLLNAME, EntryPoint = "ffuky", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_update_key(IntPtr fptr, DATATYPE datatype, string keyname, ref double value, string comment, out int status);

        [DllImport(DLLNAME, EntryPoint = "ffukyj", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_update_key_lng(IntPtr fptr, string keyname, long value, string comment, out int status);

        [DllImport(DLLNAME, EntryPoint = "ffukys", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_update_key_str(IntPtr fptr, string keyname, string value, string comment, out int status);

        //[DllImport(DLLNAME, EntryPoint = "ffukyd", CallingConvention = CallingConvention.Cdecl)]
        //public static extern int fits_update_key_dbl(IntPtr fptr, string keyname, double value, int decimals, string comment, out int status);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_set_compression_type(IntPtr fptr, COMPRESSION compress_type, out int status);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_is_compressed_image(IntPtr fptr, out int status);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_get_compression_type(IntPtr fptr, out COMPRESSION compress_type, out int status);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_img_decompress(IntPtr fptr, IntPtr outfptr, out int status);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_img_decompress_header(IntPtr fptr, IntPtr outfptr, out int status);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_uncompress_table(IntPtr fptr, out IntPtr outfptr, out int status);

        [DllImport(DLLNAME, EntryPoint = "ffthdu", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_get_num_hdus(IntPtr fptr, out int hdunum, out int status);

        [DllImport(DLLNAME, EntryPoint = "ffghdt", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_get_hdu_type(IntPtr fptr, out int hdutype, out int status);

        [DllImport(DLLNAME, EntryPoint = "ffmahd", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_movabs_hdu(IntPtr fptr, int movenr, out int hdutype, out int status);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int fits_is_reentrant();
    }
}
