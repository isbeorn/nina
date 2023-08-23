#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

namespace NINA.Core.Utility {

    public static class Logger {
        private static LoggingLevelSwitch levelSwitch;

        static Logger() {
            var logDate = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var logDir = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs");
            var processId = Environment.ProcessId;
            var logFilePath = Path.Combine(logDir, $"{logDate}-{CoreUtil.Version}.{processId}.log");

            levelSwitch = new LoggingLevelSwitch();
            levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;

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
                    rollingInterval: RollingInterval.Infinite,
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
            sb.AppendLine(PadBoth("NINA - Nighttime Imaging 'N' Astronomy", 70, '-'));
            sb.AppendLine(PadBoth(string.Format("Running NINA Version {0}", CoreUtil.Version), 70, '-'));
            sb.AppendLine(PadBoth(DateTime.Now.ToString("s"), 70, '-'));
            sb.AppendLine(PadBoth(".NET Version {0}", 70, '-', Environment.Version.ToString()));
            sb.AppendLine(PadBoth("Oparating System Information", 70, '-'));
            sb.AppendLine(PadBoth("Is 64bit OS {0}", 70, '-', Environment.Is64BitOperatingSystem.ToString()));
            sb.AppendLine(PadBoth("Is 64bit Process {0}", 70, '-', Environment.Is64BitProcess.ToString()));
            sb.AppendLine(PadBoth("Platform {0:G}", 70, '-', os.Platform.ToString()));
            sb.AppendLine(PadBoth("Version {0}", 70, '-', os.VersionString));
            sb.AppendLine(PadBoth("Major {0} Minor {1}", 70, '-', os.Version.Major.ToString(), os.Version.Minor.ToString()));
            sb.AppendLine(PadBoth("Service Pack {0}", 70, '-', os.ServicePack));
            sb.AppendLine(PadBoth("", 70, '-'));
            sb.Append("DATE|LEVEL|SOURCE|MEMBER|LINE|MESSAGE");

            return sb.ToString();
        }

        private static string PadBoth(string msg, int length, char paddingChar, params string[] msgParams) {
            var source = string.Format(msg, msgParams);
            int spaces = length - source.Length;
            int padLeft = spaces / 2 + source.Length;
            return source.PadLeft(padLeft, paddingChar).PadRight(length, paddingChar);
        }

        public static void SetLogLevel(LogLevelEnum logLevel) {
            switch (logLevel) {
                case LogLevelEnum.TRACE:
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                    break;

                case LogLevelEnum.DEBUG:
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
                    break;

                case LogLevelEnum.INFO:
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                    break;

                case LogLevelEnum.WARNING:
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Warning;
                    break;

                case LogLevelEnum.ERROR:
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Error;
                    break;

                default:
                    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                    break;
            }
        }

        public static void CloseAndFlush() {
            Log.CloseAndFlush();
        }

        public static void Error(
                Exception ex,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Error(ex, "{source}|{member}|{line}", ExtractFileName(sourceFilePath), memberName, lineNumber);
        }

        public static void Error(
                string customMsg,
                Exception ex,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Error(ex, "{source}|{member}|{line}|{message}", ExtractFileName(sourceFilePath), memberName, lineNumber, customMsg);
        }

        public static void Error(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Error("{source}|{member}|{line}|{message}", ExtractFileName(sourceFilePath), memberName, lineNumber, message);
        }

        public static void Warning(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Warning("{source}|{member}|{line}|{message}", ExtractFileName(sourceFilePath), memberName, lineNumber, message);
        }

        private static string ExtractFileName(string sourceFilePath) {
            string file = string.Empty;
            try { file = Path.GetFileName(sourceFilePath); } catch (Exception) { }
            return file;
        }

        public static void Info(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Information("{source}|{member}|{line}|{message}", ExtractFileName(sourceFilePath), memberName, lineNumber, message);
        }

        public static void Debug(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Debug("{source}|{member}|{line}|{message}", ExtractFileName(sourceFilePath), memberName, lineNumber, message);
        }

        public static void Trace(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int lineNumber = 0) {
            Log.Verbose("{source}|{member}|{line}|{message}", ExtractFileName(sourceFilePath), memberName, lineNumber, message);
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

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
                string LegacyLogLevel = string.Empty;

                switch (logEvent.Level) {
                    case LogEventLevel.Verbose:
                        LegacyLogLevel = LogLevelEnum.TRACE.ToString();
                        break;

                    case LogEventLevel.Debug:
                        LegacyLogLevel = LogLevelEnum.DEBUG.ToString();
                        break;

                    case LogEventLevel.Information:
                        LegacyLogLevel = LogLevelEnum.INFO.ToString();
                        break;

                    case LogEventLevel.Warning:
                        LegacyLogLevel = LogLevelEnum.WARNING.ToString();
                        break;

                    case LogEventLevel.Error:
                        LegacyLogLevel = LogLevelEnum.ERROR.ToString();
                        break;

                    case LogEventLevel.Fatal:
                        LegacyLogLevel = "FATAL";
                        break;

                    default:
                        LegacyLogLevel = "UNKNOWN";
                        break;
                }
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LegacyLogLevel", LegacyLogLevel));
            }
        }
    }
}