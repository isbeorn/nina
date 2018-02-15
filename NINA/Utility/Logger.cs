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
            Append(PadBoth(string.Format("ASCOM Platform Version {0}", Utility.AscomUtil.PlatformVersion), 70, '-'));
            Append(PadBoth(string.Format(".NET Version {0}", Environment.Version.ToString()), 70, '-'));
            Append(PadBoth(string.Format("Oparating System Information"), 70, '-'));
            Append(PadBoth(string.Format("Is 64bit OS {0}", Environment.Is64BitOperatingSystem), 70, '-'));
            Append(PadBoth(string.Format("Is 64bit Process {0}", Environment.Is64BitProcess), 70, '-'));
            Append(PadBoth(string.Format("Platform {0:G}", os.Platform), 70, '-'));
            Append(PadBoth(string.Format("Version {0}", os.VersionString), 70, '-'));
            Append(PadBoth(string.Format("Major {0} Minor {1}", os.Version.Major, os.Version.Minor), 70, '-'));
            Append(PadBoth(string.Format("Service Pack {0}", os.ServicePack), 70, '-'));
            Append(PadBoth("", 70, '-'));
        }

        private static string PadBoth(string source, int length, char paddingChar) {
            int spaces = length - source.Length;
            int padLeft = spaces / 2 + source.Length;
            return source.PadLeft(padLeft, paddingChar).PadRight(length, paddingChar);

        }

        static readonly object lockObj = new object();
        static string LOGDATE;
        static string LOGFILEPATH;
        

        private static void Append(string msg) {
            try {
                lock (lockObj) {
                    using (StreamWriter writer = new StreamWriter(LOGFILEPATH, true)) {
                        writer.WriteLine(msg);
                        writer.Close();
                    }
                }
                    
            } catch (Exception ex) {
                Notification.Notification.ShowError(ex.Message);
            }


        }

        public static void Error(string msg, string stacktrace = "") {

            Append(DateTime.Now.ToString("s") + " ERROR:\t" + msg + '\t' + stacktrace);
        }

        public static void Error(Exception ex) {
            Error(ex.Message, ex.StackTrace);
        }

        public static void Info(string msg) {
            if (Settings.LogLevel >= 1) {
                Append(DateTime.Now.ToString("s") + " INFO:\t" + msg);
            }

        }

        public static void Warning(string msg) {
            if (Settings.LogLevel >= 2) {
                Append(DateTime.Now.ToString("s") + " WARNING:\t" + msg);
            }
        }

        public static void Debug(string msg) {
            if (Settings.LogLevel >= 3) {
                Append(DateTime.Now.ToString("s") + " DEBUG:\t" + msg);
            }
        }

        public static void Trace(string msg) {
            if (Settings.LogLevel >= 4) {
                Append(DateTime.Now.ToString("s") + " TRACE:\t" + msg);
            }
        }
    }
}
