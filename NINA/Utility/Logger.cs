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
                //Append(PadBoth("ASCOM Platform Version {0}", 70, '-', Utility.AscomUtil.PlatformVersion));
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