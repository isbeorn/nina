using ASCOM.Alpaca.Discovery;
using NINA.Core.Locale;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch.Ascom;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MySwitch.Ascom {
    internal class AlpacaDirectSwitch : ISwitchHub, INotifyPropertyChanged {

        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private AscomSwitchHub device;

        private void SetDevice(AscomSwitchHub newDevice) {
            if (ReferenceEquals(device, newDevice)) return;

            if (device != null)
                device.PropertyChanged -= OnDevicePropertyChanged;

            device = newDevice;

            if (device != null)
                device.PropertyChanged += OnDevicePropertyChanged;
        }

        private void OnDevicePropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(IDevice.Name) || e.PropertyName == "Name") {
                RaisePropertyChanged(nameof(Name));
            }
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(IDevice.DisplayName) || e.PropertyName == "DisplayName") {
                RaisePropertyChanged(nameof(DisplayName));
            }

            RaisePropertyChanged(e.PropertyName);
        }
        
        private IProfileService profileService;
        private AlpacaDirectSettings settings;

        public AlpacaDirectSwitch(IProfileService profileService) {
            this.profileService = profileService;
            settings = new AlpacaDirectSettings(new NINA.Profile.PluginOptionsAccessor(profileService, Guid.Parse(Id)));
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasSetupDialog => true;

        public string Id => "F889ED92-DC36-4A8F-A24E-788E3AD3E780";

        public string Name => device?.Name ?? "Alpaca Switch - Static IP";

        public string DisplayName => (device?.Name ?? "Alpaca Switch - Static IP") + $" @ {settings.IpAddress} #{settings.DeviceNumber}";

        public string Category => "ASCOM Alpaca";

        public async Task<bool> Connect(CancellationToken token) {
            var meta = new AscomDevice();
            meta.ServiceType = settings.ServiceType;
            meta.IpAddress = settings.IpAddress;
            meta.IpPort = settings.Port;
            meta.AlpacaDeviceNumber = settings.DeviceNumber;
            SetDevice(new AscomSwitchHub(meta));
            var connect = await ((IDevice)device).Connect(token);
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(DisplayName));
            return connect;
        }

        private IWindowService windowService = new WindowService();

        public void SetupDialog() {
            windowService.OnDialogResultChanged -= WindowService_OnDialogResultChanged;
            windowService.ShowDialog(settings, Loc.Instance["LblAlpacaDirectIPSetup"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
            windowService.OnDialogResultChanged += WindowService_OnDialogResultChanged;
        }

        private void WindowService_OnDialogResultChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(DisplayName));
        }

        public ICollection<ISwitch> Switches => ((ISwitchHub)device).Switches;

        public bool Connected => ((IDevice)device).Connected;

        public string Description => ((IDevice)device).Description;

        public string DriverInfo => ((IDevice)device).DriverInfo;

        public string DriverVersion => ((IDevice)device).DriverVersion;

        public IList<string> SupportedActions => ((IDevice)device).SupportedActions;

        public void Disconnect() {
            ((IDevice)device).Disconnect();
        }

        public string Action(string actionName, string actionParameters) {
            return ((IDevice)device).Action(actionName, actionParameters);
        }

        public string SendCommandString(string command, bool raw = true) {
            return ((IDevice)device).SendCommandString(command, raw);
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return ((IDevice)device).SendCommandBool(command, raw);
        }

        public void SendCommandBlind(string command, bool raw = true) {
            ((IDevice)device).SendCommandBlind(command, raw);
        }
    }
}
