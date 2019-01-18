#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility.Enum;
using NINA.Utility.Profile;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace NINA.Utility {

    internal static class Logger {

        static Logger() {
            LOGDATE = DateTime.Now.ToString("yyyy-MM-dd");
            var logDir = Path.Combine(Utility.APPLICATIONTEMPPATH, "Logs");
            LOGFILEPATH = Path.Combine(logDir, LOGDATE + " - v" + Utility.Version + " - log.txt");

            if (!Directory.Exists(logDir)) {
                Directory.CreateDirectory(logDir);
            }

            InitiateLog();
        }

        private static void InitiateLog() {
            /* Initial log of App Version, OS Info, Ascom Version, .NET Version */
            if (!File.Exists(LOGFILEPATH)) {
                var os = Environment.OSVersion;
                Append(PadBoth("", 70, '-'));
                Append(PadBoth("NINA - Nighttime Imaging 'N' Astronomy", 70, '-'));
                Append(PadBoth(string.Format("Running NINA Version {0}", Utility.Version), 70, '-'));
                Append(PadBoth(DateTime.Now.ToString("s"), 70, '-'));
                try {
                    Append(PadBoth("ASCOM Platform {0}", 70, '-', ASCOMInteraction.GetVersion()));
                } catch (Exception) {
                    Append(PadBoth("ASCOM Platform {0}", 70, '-', "Not Installed"));
                }
                Append(PadBoth(".NET Version {0}", 70, '-', Environment.Version.ToString()));
                Append(PadBoth("Oparating System Information", 70, '-'));
                Append(PadBoth("Is 64bit OS {0}", 70, '-', Environment.Is64BitOperatingSystem.ToString()));
                Append(PadBoth("Is 64bit Process {0}", 70, '-', Environment.Is64BitProcess.ToString()));
                Append(PadBoth("Platform {0:G}", 70, '-', os.Platform.ToString()));
                Append(PadBoth("Version {0}", 70, '-', os.VersionString));
                Append(PadBoth("Major {0} Minor {1}", 70, '-', os.Version.Major.ToString(), os.Version.Minor.ToString()));
                Append(PadBoth("Service Pack {0}", 70, '-', os.ServicePack));
                Append(PadBoth("", 70, '-'));
            }
        }

        private static string PadBoth(string msg, int length, char paddingChar, params string[] msgParams) {
            var source = string.Format(msg, msgParams);
            int spaces = length - source.Length;
            int padLeft = spaces / 2 + source.Length;
            return source.PadLeft(padLeft, paddingChar).PadRight(length, paddingChar);
        }

        private static readonly object lockObj = new object();
        private static string LOGDATE;
        private static string LOGFILEPATH;

        private static void Append(string msg, params string[] msgParams) {
            try {
                lock (lockObj) {
                    using (StreamWriter writer = new StreamWriter(LOGFILEPATH, true)) {
                        writer.WriteLine(string.Format(msg, msgParams));
                        writer.Close();
                    }
                }
            } catch (Exception ex) {
                Notification.Notification.ShowError(ex.Message);
            }
        }

        private static void Error(
                string message,
                string stacktrace,
                string memberName,
                string sourceFilePath) {
            message = message + "\t" + stacktrace;
            Append(EnrichLogMessage(LogLevelEnum.ERROR, message, memberName, sourceFilePath));
        }

        public static LogLevelEnum LogLevel { get; private set; }

        public static void SetLogLevel(LogLevelEnum logLevel) {
            LogLevel = logLevel;
        }

        public static void Error(
                string customMsg,
                Exception ex,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "") {
            Error(customMsg + ex?.Message ?? string.Empty, ex?.StackTrace ?? string.Empty, memberName, sourceFilePath);
        }

        public static void Error(
                Exception ex,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "") {
            Error(ex.Message.Replace("{", "{{").Replace("}", "}}"), ex.StackTrace, memberName, sourceFilePath);
        }

        public static void Info(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "") {
            if ((int)LogLevel >= 1) {
                Append(EnrichLogMessage(LogLevelEnum.INFO, message, memberName, sourceFilePath));
            }
        }

        public static void Warning(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "") {
            if ((int)LogLevel >= 2) {
                Append(EnrichLogMessage(LogLevelEnum.WARNING, message, memberName, sourceFilePath));
            }
        }

        public static void Debug(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "") {
            if ((int)LogLevel >= 3) {
                Append(EnrichLogMessage(LogLevelEnum.DEBUG, message, memberName, sourceFilePath));
            }
        }

        public static void Trace(string message,
                             [CallerMemberName] string memberName = "",
                             [CallerFilePath] string sourceFilePath = "") {
            if ((int)LogLevel >= 4) {
                Append(EnrichLogMessage(LogLevelEnum.TRACE, message, memberName, sourceFilePath));
            }
        }

        private static string EnrichLogMessage(LogLevelEnum level, string message, string memberName, string sourceFilePath) {
            var sb = new StringBuilder();
            var d = DateTime.Now.ToString("s");
            var prefix = string.Format("[{0}] \t [{1}]", d, level.ToString());

            sb.AppendLine(string.Format("{0} \t [MemberName] {1}", prefix, memberName));
            sb.AppendLine(string.Format("{0} \t [FileName] {1}", prefix, sourceFilePath));
            sb.AppendLine(string.Format("{0} \t [Message] {1}", prefix, message));
            return sb.ToString();
        }
    }
}