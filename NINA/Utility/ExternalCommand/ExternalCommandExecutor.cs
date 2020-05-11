#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility.Extensions;

namespace NINA.Utility.ExternalCommand {

    internal class ExternalCommandExecutor  {
        private IProgress<ApplicationStatus> progress;

        public ExternalCommandExecutor(IProgress<ApplicationStatus> progress)  {
            this.progress = progress;
        }

        public async Task<bool> RunSequenceCompleteCommandTask(string sequenceCompleteCommand, CancellationToken ct) {
            if (!CommandExists(sequenceCompleteCommand)) {
                    Logger.Error($"Command not found: {sequenceCompleteCommand}");
                    return false;
            }
            try {
                string executableLocation = GetComandFromString(sequenceCompleteCommand);
                string args = GetArgumentsFromString(sequenceCompleteCommand);
                string src = Locale.Loc.Instance["LblSequenceCommandSource"];
                string completeMsg = string.Format(Locale.Loc.Instance["LblSequenceCommandAtCompletion"], executableLocation);
                Logger.Info($"Running - {executableLocation}");

                Process process = new Process();
                process.StartInfo.FileName = executableLocation;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data)) {
                        StatusUpdate(src, e.Data);
                        Logger.Info($"STDOUT: {e.Data}");
                    }
                };
                process.ErrorDataReceived += (object sender,  DataReceivedEventArgs e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data)) {
                        StatusUpdate(src, e.Data);
                        Logger.Error($"STDERR: {e.Data}");
                    }
                };
                if (args != null)
                    process.StartInfo.Arguments = args;

                Logger.Debug($"Starting process '{executableLocation}' with args '{args}'");
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(ct);

                //there is currently no automatism to clear a message other than 
                //sending a status update with an empty status for the same source.
                StatusUpdate(src, completeMsg);
                await Task.Run(async () => {
                    await Task.Delay(5000);
                    StatusUpdate(src, "");
                });
                return true;
            } catch (Exception e) {
                Logger.Error($"Error running command {sequenceCompleteCommand}: {e.Message}", e);
            }
            return false;
        }

        private void StatusUpdate(string src, string data) {
            progress.Report(new ApplicationStatus() {
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

        public  static string GetComandFromString(string commandLine) {
            //if you enclose the command (with spaces) in quotes, then you must remove them
            return @"" + ParseArguments(commandLine)[0].Replace("\"", "").Trim();
        }

        public static string GetArgumentsFromString(string commandLine) {
            string[] args = ParseArguments(commandLine);
            if (args.Length>1){
                return string.Join(" ", new List<string>(args).GetRange(1, args.Length-1).ToArray());
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