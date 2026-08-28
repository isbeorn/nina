#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_LoadImagingLayout_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_LoadImagingLayout_Description")]
    [ExportMetadata("Icon", "LoadSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class LoadImagingLayout : SequenceItem, IValidatable {
        private readonly IApplicationMediator applicationMediator;

        [ImportingConstructor]
        public LoadImagingLayout(IApplicationMediator applicationMediator) {
            this.applicationMediator = applicationMediator;
        }

        private LoadImagingLayout(LoadImagingLayout copyMe) : this(copyMe.applicationMediator) {
            CopyMetaData(copyMe);
            filePath = copyMe.FilePath;
        }

        public override object Clone() {
            return new LoadImagingLayout(this);
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return applicationMediator.LoadImagingLayout(FilePath, token);
        }

        [ObservableProperty]
        [property: JsonProperty]
        private string filePath = string.Empty;

        partial void OnFilePathChanged(string value) {
            Validate();
        }

        [ObservableProperty]
        private IList<string> issues = ReadOnlyCollection<string>.Empty;

        [RelayCommand]
        private void OpenDialog() {
            var dialog = new Microsoft.Win32.OpenFileDialog {
                Title = Loc.Instance["Lbl_SequenceItem_Utility_LoadImagingLayout_Name"],
                FileName = FilePath,
                DefaultExt = ".dock.config",
                Filter = "Dock Config|*.dock.config",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true) {
                FilePath = dialog.FileName;
            }
        }

        public bool Validate() {
            var validationIssues = new List<string>();

            if (string.IsNullOrWhiteSpace(FilePath) || !Path.IsPathFullyQualified(FilePath) || !File.Exists(FilePath)) {
                validationIssues.Add(Loc.Instance["Lbl_SequenceItem_Utility_LoadImagingLayout_Validation_InvalidPath"]);
            } else {
                try {
                    using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        var document = XDocument.Load(stream);
                        if (document.Root?.Name.LocalName != "LayoutRoot") {
                            validationIssues.Add(Loc.Instance["Lbl_SequenceItem_Utility_LoadImagingLayout_Validation_InvalidLayout"]);
                        }
                    }
                } catch {
                    validationIssues.Add(Loc.Instance["Lbl_SequenceItem_Utility_LoadImagingLayout_Validation_InvalidLayout"]);
                }
            }

            Issues = validationIssues.Count == 0 ? ReadOnlyCollection<string>.Empty : validationIssues;
            return validationIssues.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(LoadImagingLayout)}, Path: {FilePath}";
        }
    }
}
