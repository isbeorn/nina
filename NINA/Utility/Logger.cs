using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    static class Logger {

        static string LOGFILEPATH = Environment.GetEnvironmentVariable("LocalAppData") + "\\NINA\\tracelog.txt";

        
        private static void Append(string msg) {
            try {
                using (StreamWriter writer = new StreamWriter(LOGFILEPATH, true)) {
                    writer.WriteLine(msg);
                    //writer.Close();
                }
            }
            catch (Exception ex) {
                Notification.Notification.ShowError(ex.Message);
            }


        }

        public static void Error(string msg, string stacktrace = "") {
           
            Append(DateTime.Now.ToString("s") + " ERROR:\t" +  msg + '\t' + stacktrace);
        }

        public static void Info(string msg) {
            if(Settings.LogLevel >= 0) { 
                Append(DateTime.Now.ToString("s") + " INFO:\t" + msg);
            }
            
        }

        public static void Warning(string msg) {
            if (Settings.LogLevel >= 2) {
                Append(DateTime.Now.ToString("s") + " WARNING:\t" + msg);
            }
        }        

        public static void Trace(string msg) {
            if (Settings.LogLevel >= 3) {
                Append(DateTime.Now.ToString("s") + " TRACE:\t" + msg);
            }
        }
    }
}
