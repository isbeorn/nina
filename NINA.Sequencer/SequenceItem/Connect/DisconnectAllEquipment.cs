using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Connect {
    [ExportMetadata("Name", "Lbl_SequenceItem_Connector_DisconnectAllEquipment_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Connector_DisconnectAllEquipment_Description")]
    [ExportMetadata("Icon", "DisconnectAllSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Connect")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    class DisconnectAllEquipment : SequenceItem, IValidatable {
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
        public DisconnectAllEquipment(ICameraMediator cameraMediator,
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
        }

        public List<string> Devices { get; }


        private object GetMediator(string device) {
            switch (device) {
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

        private DisconnectAllEquipment(DisconnectAllEquipment copyMe) : this(copyMe.cameraMediator,
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
            var clone = new DisconnectAllEquipment(this) {
            };

            return clone;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var errors = new List<Exception>();

            foreach (var device in Devices) {
                var mediator = GetMediator(device);

                var type = mediator.GetType();
                var GetInfo = type.GetMethod("GetInfo");
                DeviceInfo info = (DeviceInfo)GetInfo.Invoke(mediator, null);


                if (info.Connected) {
                    var Disconnect = type.GetMethod("Disconnect");
                    await (Task)Disconnect.Invoke(mediator, null);

                    DeviceInfo infoAfterConnect = (DeviceInfo)GetInfo.Invoke(mediator, null);
                    var success = !infoAfterConnect.Connected;
                    if (!success) {
                        errors.Add(new Exception($"Failed to disconnect from {device}"));
                    }
                } else {
                    Logger.Info($"{device} is already disconnected");
                }
            }

            if (errors.Count > 0) {
                throw new AggregateException(errors);
            }
        }

        public bool Validate() {
            return true;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(DisconnectAllEquipment)}";
        }
    }
}
