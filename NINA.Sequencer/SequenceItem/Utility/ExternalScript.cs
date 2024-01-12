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
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility.ExternalCommand;
using NINA.Core.Locale;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_ExternalScript_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_ExternalScript_Description")]
    [ExportMetadata("Icon", "ScriptSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ExternalScript : SequenceItem, IValidatable {
        public System.Windows.Input.ICommand OpenDialogCommand { get; private set; }

        public ExternalScript() {
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
        }

        private ExternalScript(ExternalScript cloneMe) : this() {
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
            get => script;
            set {
                script = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            string sequenceCompleteCommand = Script;
            ExternalCommandExecutor externalCommandExecutor = new ExternalCommandExecutor(progress);
            var success = await externalCommandExecutor.RunSequenceCompleteCommandTask(sequenceCompleteCommand, token);
            if (!success) {
                throw new SequenceEntityFailedException("External script was unable to run successfully");
            }
        }

        public bool Validate() {
            var i = new List<string>();
            var sequenceCompleteCommand = Script;
            if (!string.IsNullOrWhiteSpace(sequenceCompleteCommand) && !ExternalCommandExecutor.CommandExists(sequenceCompleteCommand)) {
                i.Add(string.Format(Loc.Instance["LblSequenceCommandAtCompletionNotFound"], ExternalCommandExecutor.GetComandFromString(sequenceCompleteCommand)));
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
    }
}