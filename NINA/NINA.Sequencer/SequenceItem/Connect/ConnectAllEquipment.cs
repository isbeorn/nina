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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Connect {
    [ExportMetadata("Name", "Lbl_SequenceItem_Connector_ConnectAllEquipment_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Connector_ConnectAllEquipment_Description")]
    [ExportMetadata("Icon", "ConnectAllSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Connect")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ConnectAllEquipment : SequenceItem, IValidatable {
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

        [ImportingConstructor]
        public ConnectAllEquipment(IProfileService profileService,
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
                "Telescope",
                "Guider",
                "Switch",
                "Flat Panel",
                "Weather",
                "Dome",
                "Safety Monitor"
            };
        }

        public List<string> Devices { get; }

        private object GetMediator(string device) {
            switch (device) {
                case "Camera": return cameraMediator;
                case "Filter Wheel": return fwMediator;
                case "Focuser": return focuserMediator;
                case "Rotator": return rotatorMediator;
                case "Telescope": return telescopeMediator;
                case "Guider": return guiderMediator;
                case "Switch": return switchMediator;
                case "Flat Panel": return flatDeviceMediator;
                case "Weather": return weatherDataMediator;
                case "Dome": return domeMediator;
                case "Safety Monitor": return safetyMonitorMediator;
                default: return null;
            }
        }

        public string GetProfileId(string device) {
            switch (device) {
                case "Camera": return profileService.ActiveProfile.CameraSettings.Id;
                case "Filter Wheel": return profileService.ActiveProfile.FilterWheelSettings.Id;
                case "Focuser": return profileService.ActiveProfile.FocuserSettings.Id;
                case "Rotator": return profileService.ActiveProfile.RotatorSettings.Id;
                case "Telescope": return profileService.ActiveProfile.TelescopeSettings.Id;
                case "Guider": return profileService.ActiveProfile.GuiderSettings.GuiderName;
                case "Switch": return profileService.ActiveProfile.SwitchSettings.Id;
                case "Flat Panel": return profileService.ActiveProfile.FlatDeviceSettings.Id;
                case "Weather": return profileService.ActiveProfile.WeatherDataSettings.Id;
                case "Dome": return profileService.ActiveProfile.DomeSettings.Id;
                case "Safety Monitor": return profileService.ActiveProfile.SafetyMonitorSettings.Id;
                default: return null;
            }
        }

        private ConnectAllEquipment(ConnectAllEquipment copyMe) : this(copyMe.profileService,
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
            var clone = new ConnectAllEquipment(this) {
            };

            return clone;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var errors = new List<Exception>();

            foreach (var device in Devices) {
                if (!IsConnected(device)) {
                    var profileId = GetProfileId(device);
                    if (!(profileId == "No_Device" || profileId == "No_Guider")) {
                        var mediator = GetMediator(device);

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
                                errors.Add(new Exception($"Failed to connect to {device}"));
                            }
                        } else {
                            errors.Add(new Exception($"Failed to connect to {device} as it was not found"));
                        }

                    }
                } else {
                    Logger.Info($"{device} is already connected");
                }
            }

            if (errors.Count > 0) {
                throw new AggregateException(errors);
            }
        }

        public bool IsConnected(string device) {
            var mediator = GetMediator(device);

            var type = mediator.GetType();
            var GetInfo = type.GetMethod("GetInfo");
            DeviceInfo info = (DeviceInfo)GetInfo.Invoke(mediator, null);
            return info.Connected;
        }

        public bool Validate() {
            return true;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ConnectAllEquipment)}";
        }
    }
}
