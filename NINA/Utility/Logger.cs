using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    static class Logger {
        static Logger() {
            LOGDATE = DateTime.Now.ToString("yyyy-MM-dd");
            var logDir = Path.Combine(Utility.APPLICATIONTEMPPATH, "Logs");
            LOGFILEPATH = Path.Combine(logDir, LOGDATE + " - tracelog.txt");

            if(!Directory.Exists(logDir)) {
                Directory.CreateDirectory(logDir);
            }

            var os = Environment.OSVersion;
            
            Append(PadBoth("", 70, '-'));
            Append(PadBoth("NINA - Nighttime Imaging 'N' Astronomy", 70, '-'));
            Append(PadBoth(string.Format("Running NINA Version {0}" , Utility.Version), 70, '-'));
            Append(PadBoth(DateTime.Now.ToString("s"), 70, '-'));
            Append(PadBoth("ASCOM Platform Version {0}", 70, '-', Utility.AscomUtil.PlatformVersion));
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

        private static string PadBoth(string msg, int length, char paddingChar, params string[] msgParams) {
            var source = string.Format(msg, msgParams);
            int spaces = length - source.Length;
            int padLeft = spaces / 2 + source.Length;
            return source.PadLeft(padLeft, paddingChar).PadRight(length, paddingChar);

        }

        static readonly object lockObj = new object();
        static string LOGDATE;
        static string LOGFILEPATH;
        

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

        public static void Error(string msg, string stacktrace = "", params string[] msgParams) {
            msg = string.Format(msg, msgParams);
            Append("{0} ERROR: \t {1} \t {2}", DateTime.Now.ToString("s"), msg, stacktrace);
        }

        public static void Error(Exception ex) {
            Error(ex.Message, ex.StackTrace);
        }

        public static void Info(string msg, params string[] msgParams) {
            if (Settings.LogLevel >= 1) {
                msg = string.Format(msg, msgParams);
                Append("{0} INFO: \t {1}", DateTime.Now.ToString("s"), msg);
            }

        }

        public static void Warning(string msg, params string[] msgParams) {
            if (Settings.LogLevel >= 2) {
                msg = string.Format(msg, msgParams);
                Append("{0} WARNING: \t {1}", DateTime.Now.ToString("s"), msg);
            }
        }

        public static void Debug(string msg, params string[] msgParams) {
            if (Settings.LogLevel >= 3) {
                msg = string.Format(msg, msgParams);
                Append("{0} DEBUG: \t {1}", DateTime.Now.ToString("s"), msg);
            }
        }

        public static void Trace(string msg, params string[] msgParams) {
            if (Settings.LogLevel >= 4) {
                msg = string.Format(msg, msgParams);
                Append("{0} TRACE: \t {1}", DateTime.Now.ToString("s"), msg);
            }
        }
    }
}
