#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.ExternalCommand;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_ExternalScript_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_ExternalScript_Description")]
    [ExportMetadata("Icon", "ScriptSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ExternalScript : SequenceItem, IValidatable {
        public System.Windows.Input.ICommand OpenDialogCommand { get; private set; }
        private ISymbolBroker _symbolBroker;
        private ISymbolProvider _ninaProvider;

        [ImportingConstructor]
        public ExternalScript(ISymbolBroker symbolBroker) {
            OpenDialogCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>((object o) => {
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = Loc.Instance["Lbl_SequenceItem_Utility_ExternalScript_Name"];
                dialog.FileName = "";
                dialog.DefaultExt = ".*";
                dialog.Filter = "Any executable command |*.*";

                if (dialog.ShowDialog() == true) {
                    Script = "\"" + dialog.FileName + "\"";
                }
            });
            _symbolBroker = symbolBroker;
            _ninaProvider = (_symbolBroker as ISymbolBrokerProviderApi)?.GetInternalProvider("NINA");
        }

        private ExternalScript(ExternalScript cloneMe) : this(cloneMe._symbolBroker) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new ExternalScript(this) {
                Script = Script
            };
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private string script;

        [JsonProperty]
        public string Script {
            get => script?.Trim();
            set {
                script = value?.Trim();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ProcessedScript));
                RaisePropertyChanged(nameof(ProcessedScriptAnnotated));
            }
        }

        public string ProcessedScriptAnnotated {
            get { return "As processed: " + iProcessedScript; }
            set { }
        }


        private string iProcessedScript;
        private static readonly Regex ExpressionPattern = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);

        public string ProcessedScript {
            get {
                string value = Script;
                if (string.IsNullOrEmpty(value)) {
                    return value;
                }

                ProcessedScriptError = null;

                try {
                    value = ExpressionPattern.Replace(value, match => {
                        string toReplace = match.Groups[1].Value;
                        Expression ex = new Expression(toReplace, Parent);
                        ex.SymbolBroker = _symbolBroker;    
                        ex.Evaluate(true);
                        if (ex.Error != null) {
                            ProcessedScriptError = ex.Error;
                            return "Error";
                        } else if (ex.StringValue != null) {
                            return ex.StringValue;
                        } else {
                            return ex.ValueString;
                        }
                    });
                } catch (InvalidOperationException) {
                    value = "Error";
                }

                iProcessedScript = value;
                RaisePropertyChanged(nameof(ProcessedScriptAnnotated));
                return value;
            }
            set { }
        }
        
        public string ProcessedScriptError { get; set; } = null;
        
        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info("External Script, script = " + Script + ", processed script = " + ProcessedScript);
            string sequenceCompleteCommand = ProcessedScript;
            var success = await RunCommand(sequenceCompleteCommand, progress, token);
            if (!success) {
                throw new SequenceEntityFailedException(Loc.Instance["LblExternalCommandFailed"]);
            }
        }

        public bool Validate() {
            var i = new List<string>();
            var sequenceCompleteCommand = ProcessedScript;
            if (ProcessedScriptError != null) {
                i.Add(ProcessedScriptError);
            } else if (!string.IsNullOrWhiteSpace(sequenceCompleteCommand) && !CommandExists(sequenceCompleteCommand)) {
                i.Add(string.Format(Loc.Instance["LblExternalCommandNotFound"], GetCommandFromString(sequenceCompleteCommand)));
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ExternalScript)}, Script: {Script}";
        }

        // Code below is from the obsolete ExternalCommandExecutor class
        public async Task<bool> RunCommand(string sequenceCompleteCommand, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (!CommandExists(sequenceCompleteCommand)) {
                Logger.Error($"Command not found: {sequenceCompleteCommand}");
                _ninaProvider?.AddOrUpdateSymbol("LastExternalScriptExitCode", -1);
                return false;
            }
            string src = Loc.Instance["LblExternalCommand"];
            try {
                sequenceCompleteCommand = sequenceCompleteCommand.Trim();
                string executableLocation = GetCommandFromString(sequenceCompleteCommand);
                string args = GetArgumentsFromString(sequenceCompleteCommand);

                Process process = new Process();
                process.StartInfo.FileName = executableLocation;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;
                process.EnableRaisingEvents = true;

                DataReceivedEventHandler outputDataReceivedCallback = (object sender, DataReceivedEventArgs e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data)) {
                        StatusUpdate(progress, src, e.Data);
                        Logger.Info($"STDOUT: {e.Data}");
                    }
                };
                process.OutputDataReceived += outputDataReceivedCallback;
                DataReceivedEventHandler errorDataReceivedCallback = (object sender, DataReceivedEventArgs e) => {
                    if (!string.IsNullOrWhiteSpace(e.Data)) {
                        StatusUpdate(progress, src, e.Data);
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

                // Set Symbol here
                _ninaProvider?.AddOrUpdateSymbol("LastExternalScriptExitCode", process.ExitCode);
                return process.ExitCode == 0;
            } catch (Exception e) {
                Logger.Error($"Error running command {sequenceCompleteCommand}:", e);
                // Set Symbol here as well (-1)
                _ninaProvider?.AddOrUpdateSymbol("LastExternalScriptExitCode", -1);
            } finally {
                StatusUpdate(progress, src, "");
            }
            return false;
        }

        private void StatusUpdate(IProgress<ApplicationStatus> progress, string src, string data) {
            progress?.Report(new ApplicationStatus() {
                Source = src,
                Status = data,
            });
        }

        public static bool CommandExists(string commandLine) {
            try {
                string cmd = GetCommandFromString(commandLine);
                FileInfo fi = new FileInfo(cmd);
                return fi.Exists;
            } catch (Exception e) { Logger.Trace(e.Message); }
            return false;
        }

        public static string GetCommandFromString(string commandLine) {
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