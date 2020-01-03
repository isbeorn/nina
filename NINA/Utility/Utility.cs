#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

        public static async Task<TimeSpan> Wait(TimeSpan t, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = default) {
            TimeSpan elapsed = new TimeSpan(0);
            do {
                var delta = await Delay(100, token);
                elapsed += delta;
                progress?.Report(new ApplicationStatus() { MaxProgress = (int)t.TotalSeconds, Progress = (int)elapsed.TotalSeconds, Status = Locale.Loc.Instance["LblWaiting"], ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue });
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
    }
}