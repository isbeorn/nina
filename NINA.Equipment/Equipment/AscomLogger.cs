using ASCOM.Common.Interfaces;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment {
    public class AscomLogger : ILogger {
        public LogLevel LoggingLevel { get; set; }

        public void Log(LogLevel level, string message) {
            switch (level) {
                case LogLevel.Fatal:
                    Logger.Error(message);
                    break;
                case LogLevel.Error:
                    Logger.Error(message);
                    break;
                case LogLevel.Debug:
                    Logger.Debug(message);
                    break;
                case LogLevel.Verbose:
                    Logger.Trace(message);
                    break;
                case LogLevel.Warning:
                    Logger.Warning(message);
                    break;
                case LogLevel.Information:
                    // Info logs of ASCOM calls are for debugging purposes only
                    Logger.Debug(message);
                    break;
                default:
                    Logger.Trace(message);
                    break;
            }
        }

        public void SetMinimumLoggingLevel(LogLevel level) {
            LoggingLevel = level;
        }
    }
}
