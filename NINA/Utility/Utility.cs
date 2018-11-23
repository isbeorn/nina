using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;

namespace NINA.Utility {

    public static class Utility {
        public static char[] PATHSEPARATORS = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        public static string APPLICATIONTEMPPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA");

        public static string Version {
            get {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;
                return version;
            }
        }

        private static readonly Lazy<ASCOM.Utilities.Util> lazyAscomUtil =
            new Lazy<ASCOM.Utilities.Util>(() => new ASCOM.Utilities.Util());

        public static ASCOM.Utilities.Util AscomUtil { get { return lazyAscomUtil.Value; } }

        public static string GetUniqueFilePath(string fullPath) {
            int count = 1;

            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string path = Path.GetDirectoryName(fullPath);
            string newFullPath = fullPath;

            while (File.Exists(newFullPath)) {
                string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                newFullPath = Path.Combine(path, tempFileName + extension);
            }
            return newFullPath;
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

        public static async Task<TimeSpan> Wait(TimeSpan t, CancellationToken token = new CancellationToken()) {
            TimeSpan elapsed = new TimeSpan(0);
            do {
                var delta = await Delay(100, token);
                elapsed += delta;
            } while (elapsed < t);
            return elapsed;
        }
    }
}