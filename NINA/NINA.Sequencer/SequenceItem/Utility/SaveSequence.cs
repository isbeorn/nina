using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace NINA.Sequencer.SequenceItem.Utility {
    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_SaveSequence_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_SaveSequence_Description")]
    [ExportMetadata("Icon", "SaveSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class SaveSequence : SequenceItem, IValidatable {
        private ISequenceMediator sequenceMediator;
        [ImportingConstructor]
        public SaveSequence(ISequenceMediator sequenceMediator) {
            this.sequenceMediator = sequenceMediator;
        }

        public SaveSequence(SaveSequence copyMe) : this(copyMe.sequenceMediator) {
            CopyMetaData(copyMe);
            filePath = copyMe.FilePath;
        }

        public override object Clone() {
            return new SaveSequence(this);
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var drive = Path.GetPathRoot(FilePath);
            if(!Directory.Exists(drive)) {
                throw new SequenceEntityFailedException($"Drive {drive} specified in file path {FilePath} not found");
            }

            var dir = Path.GetDirectoryName(FilePath);
            if(!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            var root = ItemUtility.GetRootContainer(this.Parent);
            await sequenceMediator.SaveContainer(root, FilePath, token);
        }

        [ObservableProperty]
        [property: JsonProperty]
        private string filePath = "";

        [ObservableProperty]
        private IList<string> issues = ReadOnlyCollection<string>.Empty;

        [RelayCommand]
        private void OpenDialog() {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = Loc.Instance["Lbl_SequenceItem_Utility_SaveSequence_Name"];
            dialog.FileName = "";
            dialog.DefaultExt = ".json";
            dialog.Filter = "N.I.N.A.sequence JSON | *.json";

            if (dialog.ShowDialog() == true) {
                FilePath = dialog.FileName;
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SequenceItem)}, Path: {FilePath}";
        }

        public bool Validate() {
            try {
                Path.GetFullPath(FilePath);
                var drive = Path.GetPathRoot(FilePath);
                if (!Directory.Exists(drive)) {
                    Issues = new List<string>() { Loc.Instance["Lbl_SequenceItem_Utility_SaveSequence_Validation_InvalidPath"] };
                    return false;
                }
                Issues = ReadOnlyCollection<string>.Empty;
                return true;
            } catch {
                Issues = new List<string>() { Loc.Instance["Lbl_SequenceItem_Utility_SaveSequence_Validation_InvalidPath"] };
                return false;
            }
        }
    }
}
