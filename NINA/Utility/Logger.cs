using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    static class Logger {



        
        private static void append(string msg) {
            try {
                using (StreamWriter writer = new StreamWriter("tracelog.txt", true)) {
                    writer.WriteLine(msg);
                    writer.Close();
                }
            }
            catch (Exception ex) {
                System.Windows.MessageBox.Show(ex.Message, "Error writing log file", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }


        }

        public static void error(string msg) {
           
            append(DateTime.Now.ToString("s") + " ERROR:\t" +  msg);
        }

        public static void info(string msg) {
            if(Settings.LogLevel >= 0) { 
                append(DateTime.Now.ToString("s") + " INFO:\t" + msg);
            }
            
        }

        public static void warning(string msg) {
            if (Settings.LogLevel >= 2) {
                append(DateTime.Now.ToString("s") + " WARNING:\t" + msg);
            }
        }        

        public static void trace(string msg) {
            if (Settings.LogLevel >= 3) {
                append(DateTime.Now.ToString("s") + " TRACE:\t" + msg);
            }
        }
    }
}
