using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Camera {
    [ExportMetadata("Name", "Lbl_SequenceItem_Camera_SetUSBLimit_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Camera_SetUSBLimit_Description")]
    [ExportMetadata("Icon", "USBSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetUSBLimit : SequenceItem, IValidatable {
        [ImportingConstructor]
        public SetUSBLimit(ICameraMediator cameraMediator) {
            this.cameraMediator = cameraMediator;
        }

        private SetUSBLimit(SetUSBLimit cloneMe) : this(cloneMe.cameraMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SetUSBLimit(this) {
                USBLimit = USBLimit
            };
        }

        private ICameraMediator cameraMediator;

        [JsonProperty]
        public int USBLimit{ get; set; } = 0;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            cameraMediator.SetUSBLimit(USBLimit);
            return Task.CompletedTask;
        }

        public bool Validate() {
            var i = new List<string>();
            var info = cameraMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else if (!info.CanSetUSBLimit) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Validation_CannotSetUSBLimit"]);
            } else if (USBLimit < info.USBLimitMin || USBLimit > info.USBLimitMax) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Validation_InvalidUSBLimit"]);
            }

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.Zero;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetUSBLimit)}, USBLimit: {USBLimit}";
        }
    }
}
