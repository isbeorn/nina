using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Connect {
    [ExportMetadata("Name", "Lbl_SequenceItem_Connector_ConnectEquipment_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Connector_ConnectEquipment_Description")]
    [ExportMetadata("Icon", "ConnectSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Connect")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ConnectEquipment : SequenceItem, IValidatable {
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

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (SelectedDevice == "Telescope") {
                SelectedDevice = "Mount";
            }
        }

        [ImportingConstructor]
        public ConnectEquipment(IProfileService profileService,
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
            Devices = new List<string>() {
                "Camera",
                "Filter Wheel",
                "Focuser",
                "Rotator",
                "Mount",
                "Guider",
                "Switch",
                "Flat Panel",
                "Weather",
                "Dome",
                "Safety Monitor"
            };
            SelectedDevice = "Camera";
        }

        public List<string> Devices { get; }
        private string selectedDevice;

        [JsonProperty]
        public string SelectedDevice {
            get => selectedDevice;
            set {
                selectedDevice = value;
                Validate();
                RaisePropertyChanged();
            }
        }

        private object GetMediator() {
            switch (SelectedDevice) {
                case "Camera": return cameraMediator;
                case "Filter Wheel": return fwMediator;
                case "Focuser": return focuserMediator;
                case "Rotator": return rotatorMediator;
                case "Telescope": return telescopeMediator;
                case "Mount": return telescopeMediator;
                case "Guider": return guiderMediator;
                case "Switch": return switchMediator;
                case "Flat Panel": return flatDeviceMediator;
                case "Weather": return weatherDataMediator;
                case "Dome": return domeMediator;
                case "Safety Monitor": return safetyMonitorMediator;
                default: return null;
            }
        }

        public string GetProfileId() {
            switch (SelectedDevice) {
                case "Camera": return profileService.ActiveProfile.CameraSettings.Id;
                case "Filter Wheel": return profileService.ActiveProfile.FilterWheelSettings.Id;
                case "Focuser": return profileService.ActiveProfile.FocuserSettings.Id;
                case "Rotator": return profileService.ActiveProfile.RotatorSettings.Id;
                case "Telescope": return profileService.ActiveProfile.TelescopeSettings.Id;
                case "Mount": return profileService.ActiveProfile.TelescopeSettings.Id;
                case "Guider": return profileService.ActiveProfile.GuiderSettings.GuiderName;
                case "Switch": return profileService.ActiveProfile.SwitchSettings.Id;
                case "Flat Panel": return profileService.ActiveProfile.FlatDeviceSettings.Id;
                case "Weather": return profileService.ActiveProfile.WeatherDataSettings.Id;
                case "Dome": return profileService.ActiveProfile.DomeSettings.Id;
                case "Safety Monitor": return profileService.ActiveProfile.SafetyMonitorSettings.Id;
                default: return null;
            }
        }

        private ConnectEquipment(ConnectEquipment copyMe) : this(copyMe.profileService,
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
        }

        private IList<string> issues = new List<string>();
        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public override object Clone() {
            var clone = new ConnectEquipment(this) {
                SelectedDevice = this.SelectedDevice
            };

            return clone;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {


            if (!IsConnected()) {
                var profileId = GetProfileId();
                var mediator = GetMediator();

                var type = mediator.GetType();
                var GetInfo = type.GetMethod("GetInfo");
                var Rescan = type.GetMethod("Rescan");
                var devices = await (Task<IList<string>>)Rescan.Invoke(mediator, null);

                if (devices.Contains(profileId)) {
                    var Connect = type.GetMethod("Connect");
                    var success = await (Task<bool>)Connect.Invoke(mediator, null);

                    DeviceInfo infoAfterConnect = (DeviceInfo)GetInfo.Invoke(mediator, null);
                    success = success && infoAfterConnect.Connected;
                    if (!success) {
                        throw new Exception($"Failed to connect to {SelectedDevice}");
                    }
                } else {
                    throw new Exception($"Failed to connect to {SelectedDevice} as it was not found");
                }
            } else {
                Logger.Info($"{SelectedDevice} is already connected");
            }
        }

        public bool IsConnected() {
            var mediator = GetMediator();

            var type = mediator.GetType();
            var GetInfo = type.GetMethod("GetInfo");
            DeviceInfo info = (DeviceInfo)GetInfo.Invoke(mediator, null);
            return info.Connected;
        }

        public bool Validate() {
            var i = new List<string>();

            var profileId = GetProfileId();
            if (profileId == "No_Device" || profileId == "No_Guider") {
                i.Add($"There is no device id stored in the profile for the {SelectedDevice}. Make sure to manually connect a {SelectedDevice} once, so that a device id for a {SelectedDevice} is stored in the profile.");
            }

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ConnectEquipment)}, Selected device: {SelectedDevice}, Selected device id: {GetProfileId()}";
        }
    }
}
