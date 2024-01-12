#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.File;
using System.Text;
using Serilog.Events;
using System.Globalization;
using System.Management;

namespace NINA.Core.Utility {

    public static class Logger {
        private static LoggingLevelSwitch levelSwitch;

        static Logger() {
            var logDate = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var logDir = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs");
            var processId = Environment.ProcessId;
            var logFilePath = Path.Combine(logDir, $"{logDate}-{CoreUtil.Version}.{processId}-.log");

            levelSwitch = new LoggingLevelSwitch();
            levelSwitch.MinimumLevel = LogEventLevel.Information;

            if (!Directory.Exists(logDir)) {
                Directory.CreateDirectory(logDir);
            } else {
                CoreUtil.DirectoryCleanup(logDir, TimeSpan.FromDays(-90));
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.With<LegacyLogLevelMappingEnricher>()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ss.ffff}|{LegacyLogLevel}|{Message:lj}{NewLine}{Exception}")
                .WriteTo.File(logFilePath,
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Month,
                    outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ss.ffff}|{LegacyLogLevel}|{Message:lj}{NewLine}{Exception}",
                    shared: false,
                    buffered: false,
                    hooks: new HeaderWriter(GenerateHeader),
                    flushToDiskInterval: TimeSpan.FromSeconds(1),
                    retainedFileCountLimit: null)
                .CreateLogger();
        }

        private static string GenerateHeader() {
            /* Initial log of App Version, OS Info, Ascom Version, .NET Version */
            var sb = new StringBuilder();
            var os = Environment.OSVersion;
            sb.AppendLine(PadBoth("", 70, '-'));
            sb.AppendLine(PadBoth(CoreUtil.Title, 70, '-'));
            sb.AppendLine(PadBoth(string.Format("Version {0}", CoreUtil.Version), 70, '-'));
            sb.AppendLine(PadBoth(DateTime.Now.ToString("s"), 70, '-'));
            sb.AppendLine(PadBoth("Is 64bit {0}", 70, '-', Environment.Is64BitProcess.ToString()));
            sb.AppendLine(PadBoth("", 70, '-'));
            sb.AppendLine(PadBoth("{0}", 70, '-', os.VersionString));
            sb.AppendLine(PadBoth("Platform {0:G}", 70, '-', os.Platform.ToString()));
            sb.AppendLine(PadBoth("Major {0} Minor {1}", 70, '-', os.Version.Major.ToString(), os.Version.Minor.ToString()));
            sb.AppendLine(PadBoth("Service Pack {0}", 70, '-', os.ServicePack));
            sb.AppendLine(PadBoth("Is 64bit OS {0}", 70, '-', Environment.Is64BitOperatingSystem.ToString()));
            sb.AppendLine(PadBoth(".NET Version {0}", 70, '-', Environment.Version.ToString()));
            sb.AppendLine(PadBoth("", 70, '-'));
            sb.AppendLine(PadBoth("Processor Count {0}", 70, '-', Environment.ProcessorCount.ToString()));
            sb.AppendLine(PadBoth("Total Physical Memory {0} GB", 70, '-', Math.Round(GetTotalPhysicalMemory() / 1024d / 1024d / 1024d, 2).ToString()));
            foreach(var drive in DriveInfo.GetDrives()) {
                if(drive.IsReady) {
                    sb.AppendLine(PadBoth("Available Space on Drive {0}: {1} GB", 70, '-', drive.Name, Math.Round(drive.AvailableFreeSpace / (1024d * 1024d * 1024d), 2).ToString(CultureInfo.InvariantCulture)));
                } else {
                    sb.AppendLine(PadBoth("Drive {0} is not ready", 70, '-', drive.Name));
                }
            }
            sb.AppendLine(PadBoth("", 70, '-'));
            sb.Append("DATE|LEVEL|SOURCE|MEMBER|LINE|MESSAGE");

            return sb.ToString();
        }

        private static long GetTotalPhysicalMemory() {
            ObjectQuery winQuery = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery);

            foreach (ManagementObject item in searcher.Get()) {
                return Convert.ToInt64(item["TotalPhysicalMemory"]);
            }

            return 0; // Return 0 if the information could not be retrieved
        }

        private static string PadBoth(string msg, int length, char paddingChar, params string[] msgParams) {
            var source = string.Format(msg, msgParams);
            int spaces = length - source.Length;
            int padLeft = spaces / 2 + source.Length;
            return source.PadLeft(padLeft, paddingChar).PadRight(length, paddingChar);
        }

        public static void SetLogLevel(LogLevelEnum logLevel) {
            levelSwitch.MinimumLevel = logLevel switch {
                LogLevelEnum.TRACE => LogEventLevel.Verbose,
                LogLevelEnum.DEBUG => LogEventLevel.Debug,
                LogLevelEnum.INFO => LogEventLevel.Information,
                LogLevelEnum.WARNING => LogEventLevel.Warning,
                LogLevelEnum.ERROR => LogEventLevel.Error,
                _ => LogEventLevel.Information,
            };
        }

        public static void CloseAndFlush() {
            Log.CloseAndFlush();
        }

        private const string ErrorTemplate = "{source}|{member}|{line}";
        private const string MessageTemplate = "{source}|{member}|{line}|{message}";

        public static void Error(
                Exception ex,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Error(ex, ErrorTemplate, Path.GetFileName(sourceFilePath), memberName, lineNumber);
        }

        public static void Error(
                string customMsg,
                Exception ex,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Error(ex, MessageTemplate, Path.GetFileName(sourceFilePath), memberName, lineNumber, customMsg);
        }

        public static void Error(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Error(MessageTemplate, Path.GetFileName(sourceFilePath), memberName, lineNumber, message);
        }

        public static void Warning(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Warning(MessageTemplate, Path.GetFileName(sourceFilePath), memberName, lineNumber, message);
        }

        public static void Info(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Information(MessageTemplate, Path.GetFileName(sourceFilePath), memberName, lineNumber, message);
        }

        public static void Debug(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Debug(MessageTemplate, Path.GetFileName(sourceFilePath), memberName, lineNumber, message);
        }

        public static void Trace(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Verbose(MessageTemplate, Path.GetFileName(sourceFilePath), memberName, lineNumber, message);
        }

        private class HeaderWriter : FileLifecycleHooks {

            // Factory method to generate the file header
            private readonly Func<string> headerFactory;

            public HeaderWriter(Func<string> headerFactory) {
                this.headerFactory = headerFactory;
            }

            public override Stream OnFileOpened(Stream underlyingStream, Encoding encoding) {
                using (var writer = new StreamWriter(underlyingStream, encoding, 1024, true)) {
                    var header = this.headerFactory();

                    writer.WriteLine(header);
                    writer.Flush();
                    underlyingStream.Flush();
                }

                return base.OnFileOpened(underlyingStream, encoding);
            }
        }

        private class LegacyLogLevelMappingEnricher : ILogEventEnricher {
            private static readonly string LEGACYLOGLEVELPROPERTY = "LegacyLogLevel";
            private static readonly string TRACE = LogLevelEnum.TRACE.ToString();
            private static readonly string DEBUG = LogLevelEnum.DEBUG.ToString();
            private static readonly string INFO = LogLevelEnum.INFO.ToString();
            private static readonly string WARNING = LogLevelEnum.WARNING.ToString();
            private static readonly string ERROR = LogLevelEnum.ERROR.ToString();
            private static readonly string FATAL = "FATAL";
            private static readonly string UNKNOWN = "UNKNOWN";

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
                
                var legacyLogLevel = logEvent.Level switch {
                    LogEventLevel.Verbose => TRACE,
                    LogEventLevel.Debug => DEBUG,
                    LogEventLevel.Information => INFO,
                    LogEventLevel.Warning => WARNING,
                    LogEventLevel.Error => ERROR,
                    LogEventLevel.Fatal => FATAL,
                    _ => UNKNOWN
                };
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LEGACYLOGLEVELPROPERTY, legacyLogLevel));
            }
        }
    }
}