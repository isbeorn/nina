using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Net;
using NINA.Core.Locale;

namespace NINA.Equipment.Equipment {
    public class AlpacaDirectSettings : BaseINPC {
        private readonly IPluginOptionsAccessor settings;

        public AlpacaDirectSettings(IPluginOptionsAccessor settings) {
            this.settings = settings;
        }
        internal void Validate() {
            if (!IPAddress.TryParse(IpAddress, out _)) {
                throw new ArgumentException(Loc.Instance["LblAlpacaDirectInvalidAddress"]);
            }
            if (Port < 1 || Port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(Port), Loc.Instance["LblAlpacaDirectInvalidPort"]);
            }
            if (DeviceNumber < 0) {
                throw new ArgumentOutOfRangeException(nameof(DeviceNumber), Loc.Instance["LblAlpacaDirectInvalidDeviceNumber"]);
            }
            if (!Enum.IsDefined(ServiceType)) {
                throw new ArgumentOutOfRangeException(nameof(ServiceType));
            }
        }

        public ASCOM.Common.Alpaca.ServiceType ServiceType {
            get => Enum.Parse<ASCOM.Common.Alpaca.ServiceType>(settings.GetValueString(nameof(ServiceType), "Http"));
            set {
                settings.SetValueString(nameof(ServiceType), value.ToString());
                RaisePropertyChanged();
            }
        }

        public string IpAddress {
            get => settings.GetValueString(nameof(IpAddress), "127.0.0.1");
            set {
                settings.SetValueString(nameof(IpAddress), value?.Trim());
                RaisePropertyChanged();
            }
        }

        public int Port {
            get => settings.GetValueInt32(nameof(Port), 5000);
            set {
                settings.SetValueInt32(nameof(Port), value);
                RaisePropertyChanged();
            }
        }

        public int DeviceNumber {
            get => settings.GetValueInt32(nameof(DeviceNumber), 0);
            set {
                settings.SetValueInt32(nameof(DeviceNumber), value);
                RaisePropertyChanged();
            }
        }
    }
}
