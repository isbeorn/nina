using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Connect;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;

namespace NINA.Sequencer.Trigger.Connect {
    [ExportMetadata("Name", "Lbl_SequenceTrigger_Connector_ReconnectTrigger_Name")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_Connector_ReconnectTrigger_Description")]
    [ExportMetadata("Icon", "ConnectSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Connect")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ReconnectTrigger : SequenceTrigger, IValidatable {
        private IProfileService profileService;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator fwMediator;
        private IFocuserMediator focuserMediator;
        private IRotatorMediator rotatorMediator;
        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;
        private ISwitchMediator switchMediator;
        private IFlatDeviceMediator flatDeviceMediator;
        private IWeatherDataMediator weatherDataMediator;
        private IDomeMediator domeMediator;
        private ISafetyMonitorMediator safetyMonitorMediator;
        private ConnectEquipment connectEquipmentInstruction;

        [ImportingConstructor]
        public ReconnectTrigger(IProfileService profileService,
                ICameraMediator cameraMediator,
                IFilterWheelMediator fwMediator,
                IFocuserMediator focuserMediator,
                IRotatorMediator rotatorMediator,
                ITelescopeMediator telescopeMediator,
                IGuiderMediator guiderMediator,
                ISwitchMediator switchMediator,
                IFlatDeviceMediator flatDeviceMediator,
                IWeatherDataMediator weatherDataMediator,
                IDomeMediator domeMediator,
                ISafetyMonitorMediator safetyMonitorMediator) {

            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.fwMediator = fwMediator;
            this.focuserMediator = focuserMediator;
            this.rotatorMediator = rotatorMediator;
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            this.switchMediator = switchMediator;
            this.flatDeviceMediator = flatDeviceMediator;
            this.weatherDataMediator = weatherDataMediator;
            this.domeMediator = domeMediator;
            this.safetyMonitorMediator = safetyMonitorMediator;

            connectEquipmentInstruction = new ConnectEquipment(profileService, cameraMediator, fwMediator, focuserMediator, rotatorMediator, telescopeMediator, guiderMediator, switchMediator, flatDeviceMediator, weatherDataMediator, domeMediator, safetyMonitorMediator);
            TriggerRunner.Add(connectEquipmentInstruction);
        }


        [JsonProperty]
        public string SelectedDevice {
            get => connectEquipmentInstruction.SelectedDevice;
            set {
                connectEquipmentInstruction.SelectedDevice = value;
                Validate();
                RaisePropertyChanged();
            }
        }

        public override bool AllowMultiplePerSet => true;

        public ConnectEquipment ConnectEquipmentInstruction { get => connectEquipmentInstruction; }

        private IList<string> issues = new List<string>();
        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        private ReconnectTrigger(ReconnectTrigger copyMe) : this(copyMe.profileService,
                                                                 copyMe.cameraMediator,
                                                                 copyMe.fwMediator,
                                                                 copyMe.focuserMediator,
                                                                 copyMe.rotatorMediator,
                                                                 copyMe.telescopeMediator,
                                                                 copyMe.guiderMediator,
                                                                 copyMe.switchMediator,
                                                                 copyMe.flatDeviceMediator,
                                                                 copyMe.weatherDataMediator,
                                                                 copyMe.domeMediator,
                                                                 copyMe.safetyMonitorMediator) {
            CopyMetaData(copyMe);
            SelectedDevice = copyMe.SelectedDevice;
        }
        public override object Clone() {
            return new ReconnectTrigger(this);
        }

        public bool Validate() {
            var i = new List<string>();
            if (this.Parent is ISequenceRootContainer) {
                i.Add("The Reconnect Equipment Trigger should not be put into the global trigger section, but rather into specific sections where reconnection should be attempted");
            }
            connectEquipmentInstruction.Validate();
            i.AddRange(connectEquipmentInstruction.Issues);
            Issues = i;
            return Issues.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ReconnectTrigger)}, Selected device: {connectEquipmentInstruction.SelectedDevice}, Selected device id: {connectEquipmentInstruction.GetProfileId()}";
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            var isConnected = connectEquipmentInstruction.IsConnected();
            Logger.Debug($"The {connectEquipmentInstruction.SelectedDevice} is ${(isConnected ? "connected" : "not connected. Trigger should fire.")}");
            return !isConnected;
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            await TriggerRunner.Run(progress, token);
            if (ConnectEquipmentInstruction.Status == Core.Enum.SequenceEntityStatus.FAILED) {
                throw new Exception($"Failed to connect to {ConnectEquipmentInstruction.SelectedDevice}");
            }
        }
    }
}
