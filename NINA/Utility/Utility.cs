#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility {

    public static class Utility {
        public static char[] PATHSEPARATORS = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        public static string APPLICATIONTEMPPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA");
        public static DateTime ApplicationStartDate = DateTime.Now;

        public static string Version {
            get {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;
                return version;
            }
        }

        public static string Title {
            get {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.Reflection.AssemblyTitleAttribute[] o = (System.Reflection.AssemblyTitleAttribute[])assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false);
                return o[0].Title;
            }
        }

        public static void HandleAscomCOMException(Exception ex) {
            Logger.Error(ex);
            var architecture = DllLoader.IsX86() ? "x86" : "x64";
            var invertedArchitecture = DllLoader.IsX86() ? "x64" : "x86";
            Notification.Notification.ShowError(string.Format(Locale.Loc.Instance["LblAscomInterOpDriverException"], invertedArchitecture, architecture));
        }

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
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp) {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Convert datetime to unix timestamp
        /// </summary>
        /// <param name="unixTimeStamp">Milliseconds after 1970</param>
        /// <returns>DateTime</returns>
        public static long DateTimeToUnixTimeStamp(DateTime date) {
            return (int)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds; ;
        }

        public static async Task<TimeSpan> Delay(int milliseconds, CancellationToken token) {
            var t = TimeSpan.FromMilliseconds(milliseconds);
            return await Delay(t, token);
        }

        public static async Task<TimeSpan> Delay(TimeSpan span, CancellationToken token) {
            var now = DateTime.UtcNow;
            await Task.Delay(span, token);
            return DateTime.UtcNow.Subtract(now);
        }

        public static async Task<TimeSpan> Wait(TimeSpan t, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = default, string status = "") {
            TimeSpan elapsed = new TimeSpan(0);
            do {
                var delta = await Delay(100, token);
                elapsed += delta;
                progress?.Report(new ApplicationStatus() { MaxProgress = (int)t.TotalSeconds, Progress = (int)elapsed.TotalSeconds, Status = string.IsNullOrWhiteSpace(status) ? Locale.Loc.Instance["LblWaiting"] : status, ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue });
            } while (elapsed < t);
            return elapsed;
        }

        public static void DirectoryCleanup(string directory, TimeSpan deleteFromNow) {
            try {
                foreach (var file in Directory.GetFiles(directory)) {
                    FileInfo fi = new FileInfo(file);
                    if (fi.LastWriteTime < DateTime.Now.Add(deleteFromNow)) {
                        try {
                            fi.Delete();
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Formats a byte value to a string with the highest logical unit
        /// </summary>
        /// <param name="bytes">byte count</param>
        /// <returns>a string representing the converted byte unit</returns>
        /// <example>
        /// 5000 => "4.88 KiB"
        /// 5000000000 => "4.65 GiB"
        /// </example>
        public static string FormatBytes(long bytes) {
            const int scale = 1024;
            var orders = new string[] { "TiB", "GiB", "MiB", "KiB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);
            foreach (string order in orders) {
                if (bytes > max) {
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);
                }

                max /= scale;
            }
            return "0 Bytes";
        }
    }
}
