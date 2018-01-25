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
using System.Net.Cache;
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


        private static readonly Lazy<ASCOM.Utilities.Util> lazyAscomUtil =
            new Lazy<ASCOM.Utilities.Util>(() => new ASCOM.Utilities.Util());

        public static ASCOM.Utilities.Util AscomUtil { get { return lazyAscomUtil.Value; } }

        /// <summary>
        /// Replaces makros from Settings.ImageFilePattern into actual values based on input
        /// e.g.: $$Filter$$ -> "Red"
        /// </summary>
        /// <param name="patterns">KeyValue Collection of Makro -> Makrovalue</param>
        /// <returns></returns>
        public static string GetImageFileString(ICollection<ViewModel.OptionsVM.ImagePattern> patterns) {
            string s = Settings.ImageFilePattern;
            foreach (ViewModel.OptionsVM.ImagePattern p in patterns) {
                s = s.Replace(p.Key, p.Value);
            }
            return s;
        }


        public static async Task<string> HttpGetRequest(CancellationToken canceltoken, string url, params object[] parameters) {
            string result = string.Empty;

            if (parameters != null) {
                url = string.Format(url, parameters);
            }
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (canceltoken.Register(() => request.Abort(), useSynchronizationContext: false)) {
                try {
                    request = (HttpWebRequest)WebRequest.Create(url);
                    HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                    request.CachePolicy = noCachePolicy;

                    response = (HttpWebResponse)await request.GetResponseAsync();

                    using (var streamReader = new StreamReader(response.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                } catch (Exception ex) {
                    canceltoken.ThrowIfCancellationRequested();

                    //Logger.Error(ex.Message);
                    //Notification.ShowError(string.Format("Unable to connect to {0}", url));
                    Notification.Notification.ShowError(ex.Message);
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

        public static string EncodeUrl(string s) {
            return HttpUtility.UrlEncode(s);
        }

        /// <summary>
        /// Get Image from url
        /// </summary>
        /// <param name="canceltoken"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task<BitmapImage> HttpGetImage(CancellationToken canceltoken, string url, params object[] parameters) {
            BitmapImage bitmap = null;

            url = string.Format(url, parameters);
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (canceltoken.Register(() => request.Abort(), useSynchronizationContext: false)) {
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
                } catch (Exception ex) {
                    canceltoken.ThrowIfCancellationRequested();
                    Logger.Error(ex.Message, ex.StackTrace);
                    Notification.Notification.ShowError(string.Format("Unable to connect to {0}", url));

                    response?.Close();
                    response = null;


                } finally {
                    request = null;
                }
            }
            return bitmap;
        }


        /// <summary>
        /// Send a post request that is encoded in application/x-www-form-urlencoded
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="canceltoken"></param>
        /// <returns>result body of post request</returns>
        public static async Task<string> HttpPostRequest(string url, string body, CancellationToken canceltoken) {
            string result = string.Empty;

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (canceltoken.Register(() => request.Abort(), useSynchronizationContext: false)) {
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
                } catch (Exception ex) {
                    canceltoken.ThrowIfCancellationRequested();
                    Logger.Error(ex.Message, ex.StackTrace);
                    Notification.Notification.ShowError(string.Format("Unable to connect to {0}", url));

                    response?.Close();
                    response = null;

                } finally {
                    request = null;
                }
            }

            return result;

        }

        public static async Task HttpDownloadFile(Uri url, string targetLocation, CancellationToken canceltoken, IProgress<int> progress = null) {
            using (var client = new WebClient()) {
                using (canceltoken.Register(() => client.CancelAsync(), useSynchronizationContext: false)) {
                    client.DownloadProgressChanged += (s, e) => {
                        progress?.Report(e.ProgressPercentage);
                    };
                    await client.DownloadFileTaskAsync(url, targetLocation);
                }
            }
        }


        /// <summary>
        /// Upload a multipart file that is expected from astrometry.net
        /// </summary>
        /// <param name="url"></param>
        /// <param name="file"></param>
        /// <param name="paramName"></param>
        /// <param name="contentType"></param>
        /// <param name="nvc"></param>
        /// <param name="canceltoken"></param>
        /// <returns></returns>
        public static async Task<string> HttpUploadFile(string url, MemoryStream file, string paramName, string contentType, NameValueCollection nvc, CancellationToken canceltoken) {
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
                canceltoken.ThrowIfCancellationRequested();
            }
            file.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();
            canceltoken.ThrowIfCancellationRequested();
            WebResponse wresp = null;
            using (canceltoken.Register(() => wr.Abort(), useSynchronizationContext: false)) {
                try {
                    wresp = await wr.GetResponseAsync();
                    using (var streamReader = new StreamReader(wresp.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                } catch (Exception ex) {
                    canceltoken.ThrowIfCancellationRequested();
                    Logger.Error(ex.Message, ex.StackTrace);
                    Notification.Notification.ShowError(string.Format("Unable to connect to {0}", url));

                    wresp?.Close();
                    wresp = null;

                } finally {
                    wr = null;
                }
            }
            return result;
        }

        /// <summary>
        /// Convert unix timestamp to datetime
        /// </summary>
        /// <param name="unixTimeStamp">Milliseconds after 1970</param>
        /// <returns>DateTime</returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static async Task<TimeSpan> Delay(int milliseconds, CancellationToken token) {
            var t = new TimeSpan(0, 0, 0, 0, milliseconds);
            return await Delay(t, token);
        }

        public static async Task<TimeSpan> Delay(TimeSpan span, CancellationToken token) {
            var now = DateTime.Now;
            await Task.Delay(span, token);
            return DateTime.Now.Subtract(now);
        }
    }
    public enum FileTypeEnum {
        TIFF,
        FITS,
        XISF
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum PlateSolverEnum {
        [Description("LblAstrometryNet")]
        ASTROMETRY_NET,
        [Description("LblLocalPlatesolver")]
        LOCAL,
        [Description("LblPlatesolve2")]
        PLATESOLVE2
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum WeatherDataEnum {
        [Description("LblOpenWeatherMapOrg")]
        OPENWEATHERMAP
    }


    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum LogLevelEnum {
        [Description("LblError")]
        ERROR,
        [Description("LblInfo")]
        INFO,
        [Description("LblWarning")]
        WARNING,
        [Description("LblDebug")]
        DEBUG,
        [Description("LblTrace")]
        TRACE
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum CameraBulbModeEnum {
        [Description("LblNative")]
        NATIVE,
        [Description("LblSerialPort")]
        SERIALPORT,
        [Description("LblTelescopeSnapPort")]
        TELESCOPESNAPPORT
    }

}
