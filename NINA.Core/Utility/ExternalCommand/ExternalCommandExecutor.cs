#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility.Extensions;

namespace NINA.Core.Utility.ExternalCommand {

    public class ExternalCommandExecutor {
        private IProgress<ApplicationStatus> progress;

        public ExternalCommandExecutor(IProgress<ApplicationStatus> progress) {
            this.progress = progress;
        }

        public async Task<bool> RunSequenceCompleteCommandTask(string sequenceCompleteCommand, CancellationToken ct) {
            if (!CommandExists(sequenceCompleteCommand)) {
                Logger.Error($"Command not found: {sequenceCompleteCommand}");
                return false;
            }
            string src = Locale.Loc.Instance["LblExternalCommand"];
            try {
                sequenceCompleteCommand = sequenceCompleteCommand.Trim();
                string executableLocation = GetComandFromString(sequenceCompleteCommand);
                string args = GetArgumentsFromString(sequenceCompleteCommand);

                Process process = new Process();
                process.StartInfo.FileName = executableLocation;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;
                process.EnableRaisingEvents = true;

                DataReceivedEventHandler outputDataReceivedCallback = (object sender, DataReceivedEventArgs e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data)) {
                        StatusUpdate(src, e.Data);
                        Logger.Info($"STDOUT: {e.Data}");
                    }
                };
                process.OutputDataReceived += outputDataReceivedCallback;
                DataReceivedEventHandler errorDataReceivedCallback = (object sender, DataReceivedEventArgs e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data)) {
                        StatusUpdate(src, e.Data);
                        Logger.Error($"STDERR: {e.Data}");
                    }
                };
                process.ErrorDataReceived += errorDataReceivedCallback;

                if (!string.IsNullOrWhiteSpace(args)) {
                    process.StartInfo.Arguments = args;
                }

                Logger.Info($"Running - '{executableLocation}' with args '{args}'");
                process.Start();
                await process.WaitForExitAsync(ct);

                process.OutputDataReceived -= outputDataReceivedCallback;
                process.ErrorDataReceived -= errorDataReceivedCallback;

                return process.ExitCode == 0;
            } catch (Exception e) {
                Logger.Error($"Error running command {sequenceCompleteCommand}:", e);
            } finally {
                StatusUpdate(src, "");
            }
            return false;
        }

        private void StatusUpdate(string src, string data) {
            progress?.Report(new ApplicationStatus() {
                Source = src,
                Status = data,
            });
        }

        public static bool CommandExists(string commandLine) {
            try {
                string cmd = GetComandFromString(commandLine);
                FileInfo fi = new FileInfo(cmd);
                return fi.Exists;
            } catch (Exception e) { Logger.Trace(e.Message); }
            return false;
        }

        public static string GetComandFromString(string commandLine) {
            //if you enclose the command (with spaces) in quotes, then you must remove them
            return @"" + ParseArguments(commandLine)[0].Replace("\"", "").Trim();
        }

        public static string GetArgumentsFromString(string commandLine) {
            string[] args = ParseArguments(commandLine);
            if (args.Length > 1) {
                return string.Join(" ", new List<string>(args).GetRange(1, args.Length - 1).ToArray());
            }
            return null;
        }

        public static string[] ParseArguments(string commandLine) {
            char[] parmChars = commandLine.ToCharArray();
            bool inQuote = false;
            for (int index = 0; index < parmChars.Length; index++) {
                if (parmChars[index] == '"')
                    inQuote = !inQuote;
                if (!inQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split('\n');
        }
    }
}