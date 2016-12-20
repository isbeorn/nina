using Newtonsoft.Json.Linq;
using nom.tam.fits;
using nom.tam.util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility {
    public static class Utility {
        
        private static ASCOM.Utilities.Util _ascomUtil;
        public static ASCOM.Utilities.Util AscomUtil {
            get {
                if(_ascomUtil == null) {
                    _ascomUtil = new ASCOM.Utilities.Util();
                }
                return _ascomUtil;

            }
        }

        public class ImageArray {
            public const ushort HistogramResolution = 200;

            public Array SourceArray;
            public ushort[] FlatArray;
            public int X;
            public int Y;
            public ushort minStDev;
            public ushort maxStDev;
            private Dictionary<ushort, int> _histogram;

            public Dictionary<ushort, int> Histogram {
                get {
                    return _histogram;
                }

                set {
                    _histogram = value;
                }
            }
        }

        public class TImageArray<T> {
            public Array SourceArray;
            public T[] FlatArray;
            public int X;
            public int Y;
            public T minStDev;
            public T maxStDev;
            private Dictionary<T, int> _histogram;

            public Dictionary<T, int> Histogram {
                get {
                    return _histogram;
                }

                set {
                    _histogram = value;
                }
            }
        }

        private static PHD2Client _pHDClient;
        public static PHD2Client PHDClient {
            get {
                if (_pHDClient == null) {
                    _pHDClient = new PHD2Client();
                }
                return _pHDClient;
            }
            set {
                _pHDClient = value;
            }
        }

        public static async Task<ImageArray> convert2DArray(Int32[,] arr) {
            return await Task<ImageArray>.Run(() => {
                ImageArray iarr = new ImageArray();
                iarr.SourceArray = arr;
                int width = arr.GetLength(0);
                int height = arr.GetLength(1);
                //iarr.Y = width;
                //iarr.X = height;
                iarr.X = width;
                iarr.Y = height;
                ushort[] flatArray = new ushort[arr.Length];
                ushort value, histogramkey;
                Dictionary<ushort, int> histogram = new Dictionary<ushort, int>();
                unsafe
                {
                    fixed (Int32* ptr = arr)
                    {
                        int idx = 0, row = 0;
                        for (int i = 0; i < arr.Length; i++) {
                            value = (ushort)ptr[i];

                            
                           

                            idx = ((i % height) * width) + row;
                            if ((i % (height)) == (height - 1)) row++;

                            histogramkey = Convert.ToUInt16(Math.Round(((double)ImageArray.HistogramResolution / ushort.MaxValue) * value));
                            if (histogram.ContainsKey(histogramkey)) {
                                histogram[histogramkey] += 1;
                            }else {
                                histogram.Add(histogramkey, 1);
                            }

                            ushort b = value;
                            flatArray[idx] = b;
                        
                            
                        }
                    }
                }

                /*Calculate StDev and Min/Max Values for Stretch */
                double average = flatArray.Average(x => x);
                double sumOfSquaresOfDifferences = flatArray.Select(val => (val - average) * (val - average)).Sum();
                double sd = Math.Sqrt(sumOfSquaresOfDifferences / flatArray.Length);
                ushort min = 0, max = 0;
                double factor = 2.5;

                if (average - factor * sd < 0) {
                    min = 0;
                }
                else {
                    min = (ushort)(average - factor * sd);
                }

                if (average + factor * sd > ushort.MaxValue) {
                    max = ushort.MaxValue;
                }
                else {
                    max = (ushort)(average + factor * sd);
                }


                
                iarr.FlatArray = flatArray;
                iarr.minStDev = min;
                iarr.maxStDev = max;
                iarr.Histogram = histogram;                                    
                return iarr;
            });           
        }

        public static async Task<ushort[]> stretchArray(ImageArray source) {
            return await Task.Run<ushort[]>(() => {
                ushort maxVal = source.maxStDev;
                ushort minVal = source.minStDev;
                ushort dynamic = (ushort)(maxVal - minVal);

                ushort[] stretchedArr = new ushort[source.FlatArray.Length];

                for (int i = 0; i < source.FlatArray.Length; i++) {

                    float val = (((float)(source.FlatArray[i] - minVal) / dynamic) * (ushort.MaxValue));
                    if (val > ushort.MaxValue) {
                        val = ushort.MaxValue;
                    }
                    stretchedArr[i] = (ushort)val;

                }
                return stretchedArr;
            });
        }
        
        public static async Task<T[]> TstretchArray<T>(TImageArray<T> source) {
            return await Task.Run<T[]>(() => {
                dynamic maxVal = source.maxStDev;
                dynamic minVal = source.minStDev;
                dynamic d = (T)Convert.ChangeType((maxVal - minVal), typeof(T));

                FieldInfo maxValueField = typeof(T).GetField("MaxValue", BindingFlags.Public | BindingFlags.Static);
                if (maxValueField == null)
                    throw new NotSupportedException(typeof(T).Name);
                dynamic maxValue = (T)maxValueField.GetValue(null);
                

                T[] stretchedArr = new T[source.FlatArray.Length];

                for (int i = 0; i < source.FlatArray.Length; i++) {

                    dynamic val = (((float)(source.FlatArray[i] - minVal) / d) * (maxValue));
                    if (val > maxValue) {
                        val = maxValue;
                    }
                    stretchedArr[i] = (T)Convert.ChangeType(val, typeof(T));

                }
                return stretchedArr;
            });
        }

        public static void saveFits(ImageArray iarr, string path, string imagetype, double duration, string filter, Model.CameraModel.BinningMode binning, double temp) {
            try {
                Header h = new Header();
                h.AddValue("SIMPLE", "T", "C# FITS");
                h.AddValue("BITPIX", 16, "");
                h.AddValue("NAXIS", 2, "Dimensionality");
                h.AddValue("NAXIS1", iarr.X, "");
                h.AddValue("NAXIS2", iarr.Y, "");
                h.AddValue("BZERO", 32768, "");
                h.AddValue("EXTEND", "T", "Extensions are permitted");

                if(!string.IsNullOrEmpty(filter)) {
                    h.AddValue("FILTER", filter, "");
                }

                h.AddValue("CCDXBIN", binning.X, "");
                h.AddValue("CCDYBIN", binning.Y, "");
                h.AddValue("XBINNING", binning.X, "");
                h.AddValue("YBINNING", binning.Y, "");

                h.AddValue("TEMPERAT", temp, "");

                h.AddValue("IMAGETYP", imagetype, "");

                h.AddValue("EXPOSURE", duration, "");
                /*
                 
                 h.AddValue("OBJECT", 32768, "");
                 */

                short[][] curl = new short[iarr.Y][];
                int idx = 0;
                for (int i = 0; i < iarr.Y; i++) {
                    curl[i] = new short[iarr.X];
                    for (int j = 0; j < iarr.X; j++) {
                        curl[i][j] = (short)(short.MinValue + iarr.FlatArray[idx]);
                        idx++;
                    }
                }
                ImageData d = new ImageData(curl);

                Fits fits = new Fits();
                BasicHDU hdu = FitsFactory.HDUFactory(h, d);
                fits.AddHDU(hdu);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (FileStream fs = new FileStream(path + ".fits", FileMode.Create)) {
                    fits.Write(fs);
                }

            }
            catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.error(ex.Message);

            }
        }

        public static void saveTiff(ImageArray iarr, String path) {         
            
            try {
                BitmapSource bmpSource = createSourceFromArray(iarr.FlatArray, iarr.X, iarr.Y, System.Windows.Media.PixelFormats.Gray16);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (FileStream fs = new FileStream(path + ".tif", FileMode.Create)) {
                    TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                    encoder.Compression = TiffCompressOption.None;
                    encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                    encoder.Save(fs);
                }
            } catch(Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.error(ex.Message);

            }
        }


        //public static BitmapSource NormalizeTiffTo8BitImage(BitmapSource source) {
        //    // allocate buffer & copy image bytes.
        //    var rawStride = source.PixelWidth * source.Format.BitsPerPixel / 8;
        //    var rawImage = new byte[rawStride * source.PixelHeight];
        //    source.CopyPixels(rawImage, rawStride, 0);

        //    // get both max values of first & second byte of pixel as scaling bounds.
        //    var max1 = 0;
        //    int max2 = 1;
        //    for (int i = 0; i < rawImage.Length; i++) {
        //        if ((i & 1) == 0) {
        //            if (rawImage[i] > max1)
        //                max1 = rawImage[i];
        //        }
        //        else if (rawImage[i] > max2)
        //            max2 = rawImage[i];
        //    }

        //    // determine normalization factors.
        //    var normFactor = max2 == 0 ? 0.0d : 128.0d / max2;
        //    var factor = max1 > 0 ? 255.0d / max1 : 0.0d;
        //    max2 = Math.Max(max2, 1);

        //    // normalize each pixel to output buffer.
        //    var buffer8Bit = new byte[rawImage.Length / 2];
        //    for (int src = 0, dst = 0; src < rawImage.Length; dst++) {
        //        int value16 = rawImage[src++];
        //        double value8 = ((value16 * factor) / max2) - normFactor;

        //        if (rawImage[src] > 0) {
        //            int b = rawImage[src] << 8;
        //            value8 = ((value16 + b) / max2) - normFactor;
        //        }
        //        buffer8Bit[dst] = (byte)Math.Min(255, Math.Max(value8, 0));
        //        src++;
        //    }

        //    // return new bitmap source.
        //    return BitmapSource.Create(
        //        source.PixelWidth, source.PixelHeight,
        //        source.DpiX, source.DpiY,
        //        PixelFormats.Gray8, BitmapPalettes.Gray256,
        //        buffer8Bit, rawStride / 2);
        //}

        public static BitmapSource createSourceFromArray(Array flatArray, int x, int y, System.Windows.Media.PixelFormat pf) {
            
            //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
            int stride = (x * pf.BitsPerPixel + 7) / 8;
            double dpi = 96;

            BitmapSource source = BitmapSource.Create(x, y, dpi, dpi, pf, null, flatArray, stride);
            return source;
        }

        public static string getImageFileString(ICollection<ViewModel.OptionsVM.ImagePattern> patterns) {
            string s = Settings.ImageFilePattern;
            foreach(ViewModel.OptionsVM.ImagePattern p in patterns) {
                s = s.Replace(p.Key, p.Value);
            }
            return s;
        }


        public static async Task<string> httpGetRequest(CancellationTokenSource canceltoken, string url, params object[] parameters) {
            string result = string.Empty;

            url = string.Format(url, parameters);
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (canceltoken.Token.Register(() => request.Abort(), useSynchronizationContext: false)) {
                try {
                    request = (HttpWebRequest)WebRequest.Create(url);
                    
                    response = (HttpWebResponse)await request.GetResponseAsync();
             
                    using (var streamReader = new StreamReader(response.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                }
                catch (Exception ex) {
                    if (canceltoken.Token.IsCancellationRequested) {
                        throw new OperationCanceledException(ex.Message, ex, canceltoken.Token);
                    }
                    Logger.error(ex.Message);
                    Notification.ShowError(string.Format("Unable to connect to {0}", url));
                    if (response != null) {
                        response.Close();
                        response = null;
                    }
                    
                    
                } finally {
                    request = null;
                }
            }

            return result;
        }

        public static string encodeUrl(string s) {
            return HttpUtility.UrlEncode(s);
        }


        public static async Task<BitmapImage> httpGetImage(CancellationTokenSource canceltoken, string url, params object[] parameters) {
            BitmapImage bitmap = null;

            url = string.Format(url, parameters);
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (canceltoken.Token.Register(() => request.Abort(), useSynchronizationContext: false)) {
                try {
                    request = (HttpWebRequest)WebRequest.Create(url);

                    response = (HttpWebResponse)await request.GetResponseAsync();                    

                    using (BinaryReader reader = new BinaryReader(response.GetResponseStream())) {
                        Byte[] lnByte = reader.ReadBytes(1 * 1024 * 1024 * 10);
                        using (var stream = new MemoryStream(lnByte)) {
                            bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }
                    }
                }
                catch (Exception ex) {
                    if (canceltoken.Token.IsCancellationRequested) {
                        throw new OperationCanceledException(ex.Message, ex, canceltoken.Token);
                    }
                    Logger.error(ex.Message);
                    Notification.ShowError(string.Format("Unable to connect to {0}", url));
                    if (response != null) {
                        response.Close();
                        response = null;
                    }
                    
                }
                finally {
                    request = null;
                }
            }
            return bitmap;
        }

        public static async Task<string> httpPostRequest(string url, string body, CancellationTokenSource canceltoken) {
            string result = string.Empty;
            
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (canceltoken.Token.Register(() => request.Abort(), useSynchronizationContext: false)) {
                try {
                    request = (HttpWebRequest)WebRequest.Create(url);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Method = "POST";

                    using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                        streamWriter.Write(body);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    response = (HttpWebResponse)await request.GetResponseAsync();
                    using (var streamReader = new StreamReader(response.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                }
                catch (Exception ex) {
                    if (canceltoken.Token.IsCancellationRequested) {
                        throw new OperationCanceledException(ex.Message, ex, canceltoken.Token);
                    }
                    Logger.error(ex.Message);
                    Notification.ShowError(string.Format("Unable to connect to {0}", url));
                    if (response != null) {
                        response.Close();
                        response = null;
                    }
                }
                finally {
                    request = null;
                }
            }

            return result;

        }

        public static async Task<string> httpUploadFile(string url, MemoryStream file, string paramName, string contentType, NameValueCollection nvc, CancellationTokenSource canceltoken) {
            string result = string.Empty;            
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            //wr.KeepAlive = true;
            //wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys) {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, contentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            // FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = file.Read(buffer, 0, buffer.Length)) != 0) {
                rs.Write(buffer, 0, bytesRead);
                canceltoken.Token.ThrowIfCancellationRequested();
            }
            file.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();
            canceltoken.Token.ThrowIfCancellationRequested();
            WebResponse wresp = null;
            using (canceltoken.Token.Register(() => wr.Abort(), useSynchronizationContext: false)) {
                try {
                    wresp = await wr.GetResponseAsync();
                    using (var streamReader = new StreamReader(wresp.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                }
                catch (Exception ex) {
                    if (canceltoken.Token.IsCancellationRequested) {
                        throw new OperationCanceledException(ex.Message, ex, canceltoken.Token);
                    }
                    Logger.error(ex.Message);
                    Notification.ShowError(string.Format("Unable to connect to {0}", url));
                    if (wresp != null) {
                        wresp.Close();
                        wresp = null;
                    }
                }
                finally {
                    wr = null;
                }
            }
            return result;
        }

    }
    public enum FileTypeEnum {
        TIFF,
        FITS
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum PlateSolverEnum {
        [Description("Astrometry.net")]
        ASTROMETRY_NET,
        [Description("Ansvr")]
        ANSVR
    }




}
