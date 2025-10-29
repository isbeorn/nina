using ASCOM.Alpaca.Discovery;
using NINA.Core.Locale;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MySwitch.Ascom;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFocuser {
    internal class AlpacaDirectFocuser : IFocuser, INotifyPropertyChanged {

        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private AscomFocuser device;

        private void SetDevice(AscomFocuser newDevice) {
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

        public AlpacaDirectFocuser(IProfileService profileService) {
            this.profileService = profileService;
            settings = new AlpacaDirectSettings(new NINA.Profile.PluginOptionsAccessor(profileService, Guid.Parse(Id)));
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasSetupDialog => true;

        public string Id => "75ABC27F-85F6-4993-B42C-C66E1F5726E3";

        public string Name => device?.Name ?? "Alpaca Focuser - Static IP";

        public string DisplayName => (device?.Name ?? "Alpaca Focuser - Static IP") + $" @ {settings.IpAddress} #{settings.DeviceNumber}";

        public string Category => "ASCOM Alpaca";

        public async Task<bool> Connect(CancellationToken token) {
            var meta = new AscomDevice();
            meta.ServiceType = settings.ServiceType;
            meta.IpAddress = settings.IpAddress;
            meta.IpPort = settings.Port;
            meta.AlpacaDeviceNumber = settings.DeviceNumber;
            SetDevice(new AscomFocuser(meta));
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

        public bool IsMoving => ((IFocuser)device).IsMoving;

        public int MaxIncrement => ((IFocuser)device).MaxIncrement;

        public int MaxStep => ((IFocuser)device).MaxStep;

        public int Position => ((IFocuser)device).Position;

        public double StepSize => ((IFocuser)device).StepSize;

        public bool TempCompAvailable => ((IFocuser)device).TempCompAvailable;

        public bool TempComp { get => ((IFocuser)device).TempComp; set => ((IFocuser)device).TempComp = value; }

        public double Temperature => ((IFocuser)device).Temperature;

        public bool Connected => ((IDevice)device).Connected;

        public string Description => ((IDevice)device).Description;

        public string DriverInfo => ((IDevice)device).DriverInfo;

        public string DriverVersion => ((IDevice)device).DriverVersion;

        public IList<string> SupportedActions => ((IDevice)device).SupportedActions;

        public Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            return ((IFocuser)device).Move(position, ct, waitInMs);
        }

        public void Halt() {
            ((IFocuser)device).Halt();
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
