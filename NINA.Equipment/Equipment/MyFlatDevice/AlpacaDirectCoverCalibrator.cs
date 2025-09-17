using ASCOM.Alpaca.Discovery;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyDome;
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

namespace NINA.Equipment.Equipment.MyFlatDevice {
    internal class AlpacaDirectCoverCalibrator : IFlatDevice, INotifyPropertyChanged {

        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private AscomCoverCalibrator device;

        private void SetDevice(AscomCoverCalibrator newDevice) {
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

        public AlpacaDirectCoverCalibrator(IProfileService profileService) {
            this.profileService = profileService;
            settings = new AlpacaDirectSettings(new NINA.Profile.PluginOptionsAccessor(profileService, Guid.Parse(Id)));
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasSetupDialog => true;

        public string Id => "DE702D3C-8D66-462A-A4AB-19FA5A0C7E84";

        public string Name => device?.Name ?? "Alpaca Cover Calibrator - Static IP";

        public string DisplayName => (device?.Name ?? "Alpaca Cover Calibrator - Static IP") + $" @ {settings.IpAddress} #{settings.DeviceNumber}";

        public string Category => "ASCOM Alpaca";

        public async Task<bool> Connect(CancellationToken token) {
            var meta = new AscomDevice();
            meta.ServiceType = settings.ServiceType;
            meta.IpAddress = settings.IpAddress;
            meta.IpPort = settings.Port;
            meta.AlpacaDeviceNumber = settings.DeviceNumber;
            SetDevice(new AscomCoverCalibrator(meta));
            var connect = await ((IDevice)device).Connect(token);
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(DisplayName));
            return connect;
        }

        private IWindowService windowService = new WindowService();

        public void SetupDialog() {
            windowService.ShowDialog(settings, "ASCOM Alpaca IP Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }

        public CoverState CoverState => ((IFlatDevice)device).CoverState;

        public int MaxBrightness => ((IFlatDevice)device).MaxBrightness;

        public int MinBrightness => ((IFlatDevice)device).MinBrightness;

        public bool LightOn { get => ((IFlatDevice)device).LightOn; set => ((IFlatDevice)device).LightOn = value; }
        public int Brightness { get => ((IFlatDevice)device).Brightness; set => ((IFlatDevice)device).Brightness = value; }
        public string PortName { get => ((IFlatDevice)device).PortName; set => ((IFlatDevice)device).PortName = value; }

        public bool SupportsOpenClose => ((IFlatDevice)device).SupportsOpenClose;

        public bool SupportsOnOff => ((IFlatDevice)device).SupportsOnOff;

        public bool Connected => ((IDevice)device).Connected;

        public string Description => ((IDevice)device).Description;

        public string DriverInfo => ((IDevice)device).DriverInfo;

        public string DriverVersion => ((IDevice)device).DriverVersion;

        public IList<string> SupportedActions => ((IDevice)device).SupportedActions;

        public Task<bool> Open(CancellationToken ct, int delay = 300) {
            return ((IFlatDevice)device).Open(ct, delay);
        }

        public Task<bool> Close(CancellationToken ct, int delay = 300) {
            return ((IFlatDevice)device).Close(ct, delay);
        }

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
