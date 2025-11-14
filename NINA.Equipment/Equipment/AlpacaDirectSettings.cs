using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;

namespace NINA.Equipment.Equipment {
    public class AlpacaDirectSettings : BaseINPC {
        private readonly IPluginOptionsAccessor settings;

        public AlpacaDirectSettings(IPluginOptionsAccessor settings) {
            this.settings = settings;
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
                if (value < 0) value = 0;
                settings.SetValueInt32(nameof(Port), value);
                RaisePropertyChanged();
            }
        }

        public int DeviceNumber {
            get => settings.GetValueInt32(nameof(DeviceNumber), 0);
            set {
                if (value < 0) value = 0;
                settings.SetValueInt32(nameof(DeviceNumber), value);
                RaisePropertyChanged();
            }
        }
    }
}
