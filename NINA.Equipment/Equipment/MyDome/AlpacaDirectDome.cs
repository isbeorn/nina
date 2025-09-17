using ASCOM.Alpaca.Discovery;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyFilterWheel;
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

namespace NINA.Equipment.Equipment.MyDome {
    internal class AlpacaDirectDome : IDome, INotifyPropertyChanged {
        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private AscomDome device;

        private void SetDevice(AscomDome newDevice) {
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

        public AlpacaDirectDome(IProfileService profileService) {
            this.profileService = profileService;
            settings = new AlpacaDirectSettings(new NINA.Profile.PluginOptionsAccessor(profileService, Guid.Parse(Id)));
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasSetupDialog => true;

        public string Id => "C09F1563-4B7A-4300-B460-058ACD7952ED";

        public string Name => device?.Name ?? "Alpaca Dome - Static IP";

        public string DisplayName => (device?.Name ?? "Alpaca Dome - Static IP") + $" @ {settings.IpAddress} #{settings.DeviceNumber}";

        public string Category => "ASCOM Alpaca";

        public async Task<bool> Connect(CancellationToken token) {
            var meta = new AscomDevice();
            meta.ServiceType = settings.ServiceType;
            meta.IpAddress = settings.IpAddress;
            meta.IpPort = settings.Port;
            meta.AlpacaDeviceNumber = settings.DeviceNumber;
            SetDevice(new AscomDome(meta));
            var connect = await ((IDevice)device).Connect(token);
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(DisplayName));
            return connect;
        }

        private IWindowService windowService = new WindowService();

        public void SetupDialog() {
            windowService.ShowDialog(settings, "ASCOM Alpaca IP Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }

        public ShutterState ShutterStatus => ((IDome)device).ShutterStatus;

        public bool DriverCanFollow => ((IDome)device).DriverCanFollow;

        public bool CanSetShutter => ((IDome)device).CanSetShutter;

        public bool CanSetPark => ((IDome)device).CanSetPark;

        public bool CanSetAzimuth => ((IDome)device).CanSetAzimuth;

        public bool CanSyncAzimuth => ((IDome)device).CanSyncAzimuth;

        public bool CanPark => ((IDome)device).CanPark;

        public bool CanFindHome => ((IDome)device).CanFindHome;

        public double Azimuth => ((IDome)device).Azimuth;

        public double Altitude => ((IDome)device).Altitude;

        public bool AtPark => ((IDome)device).AtPark;

        public bool AtHome => ((IDome)device).AtHome;

        public bool DriverFollowing { get => ((IDome)device).DriverFollowing; set => ((IDome)device).DriverFollowing = value; }

        public bool Slewing => ((IDome)device).Slewing;

        public bool Connected => ((IDevice)device).Connected;

        public string Description => ((IDevice)device).Description;

        public string DriverInfo => ((IDevice)device).DriverInfo;

        public string DriverVersion => ((IDevice)device).DriverVersion;

        public IList<string> SupportedActions => ((IDevice)device).SupportedActions;

        public Task SlewToAzimuth(double azimuth, CancellationToken ct) {
            return ((IDome)device).SlewToAzimuth(azimuth, ct);
        }

        public Task StopSlewing() {
            return ((IDome)device).StopSlewing();
        }

        public Task StopShutter() {
            return ((IDome)device).StopShutter();
        }

        public Task StopAll() {
            return ((IDome)device).StopAll();
        }

        public Task OpenShutter(CancellationToken ct) {
            return ((IDome)device).OpenShutter(ct);
        }

        public Task CloseShutter(CancellationToken ct) {
            return ((IDome)device).CloseShutter(ct);
        }

        public Task FindHome(CancellationToken ct) {
            return ((IDome)device).FindHome(ct);
        }

        public Task Park(CancellationToken ct) {
            return ((IDome)device).Park(ct);
        }

        public void SetPark() {
            ((IDome)device).SetPark();
        }

        public void SyncToAzimuth(double azimuth) {
            ((IDome)device).SyncToAzimuth(azimuth);
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
