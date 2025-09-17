using ASCOM.Alpaca.Discovery;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MySwitch.Ascom;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
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

namespace NINA.Equipment.Equipment.MyCamera {
    internal class AlpacaDirectCamera : ICamera, INotifyPropertyChanged {

        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private AscomCamera device;

        private void SetDevice(AscomCamera newDevice) {
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
        private readonly IExposureDataFactory exposureDataFactory;
        private AlpacaDirectSettings settings;

        public AlpacaDirectCamera(IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            settings = new AlpacaDirectSettings(new NINA.Profile.PluginOptionsAccessor(profileService, Guid.Parse(Id)));
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasSetupDialog => true;

        public string Id => "01E42001-1A8B-44AA-AD7E-CE8F5250F1F4";

        public string Name => device?.Name ?? "Alpaca Camera - Static IP";

        public string DisplayName => (device?.Name ?? "Alpaca Camera - Static IP") + $" @ {settings.IpAddress} #{settings.DeviceNumber}";

        public string Category => "ASCOM Alpaca";

        public async Task<bool> Connect(CancellationToken token) {
            var meta = new AscomDevice();
            meta.ServiceType = settings.ServiceType;
            meta.IpAddress = settings.IpAddress;
            meta.IpPort = settings.Port;
            meta.AlpacaDeviceNumber = settings.DeviceNumber;
            SetDevice(new AscomCamera(meta, profileService, exposureDataFactory));
            var connect = await ((IDevice)device).Connect(token);
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(DisplayName));
            return connect;
        }

        private IWindowService windowService = new WindowService();

        public void SetupDialog() {
            windowService.ShowDialog(settings, "ASCOM Alpaca IP Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }

        public bool HasShutter => ((ICamera)device).HasShutter;

        public double Temperature => ((ICamera)device).Temperature;

        public double TemperatureSetPoint { get => ((ICamera)device).TemperatureSetPoint; set => ((ICamera)device).TemperatureSetPoint = value; }
        public short BinX { get => ((ICamera)device).BinX; set => ((ICamera)device).BinX = value; }
        public short BinY { get => ((ICamera)device).BinY; set => ((ICamera)device).BinY = value; }

        public string SensorName => ((ICamera)device).SensorName;

        public SensorType SensorType => ((ICamera)device).SensorType;

        public short BayerOffsetX => ((ICamera)device).BayerOffsetX;

        public short BayerOffsetY => ((ICamera)device).BayerOffsetY;

        public int CameraXSize => ((ICamera)device).CameraXSize;

        public int CameraYSize => ((ICamera)device).CameraYSize;

        public double ExposureMin => ((ICamera)device).ExposureMin;

        public double ExposureMax => ((ICamera)device).ExposureMax;

        public short MaxBinX => ((ICamera)device).MaxBinX;

        public short MaxBinY => ((ICamera)device).MaxBinY;

        public double PixelSizeX => ((ICamera)device).PixelSizeX;

        public double PixelSizeY => ((ICamera)device).PixelSizeY;

        public bool CanSetTemperature => ((ICamera)device).CanSetTemperature;

        public bool CoolerOn { get => ((ICamera)device).CoolerOn; set => ((ICamera)device).CoolerOn = value; }

        public double CoolerPower => ((ICamera)device).CoolerPower;

        public bool HasDewHeater => ((ICamera)device).HasDewHeater;

        public bool DewHeaterOn { get => ((ICamera)device).DewHeaterOn; set => ((ICamera)device).DewHeaterOn = value; }

        public CameraStates CameraState => ((ICamera)device).CameraState;

        public bool CanSubSample => ((ICamera)device).CanSubSample;

        public bool EnableSubSample { get => ((ICamera)device).EnableSubSample; set => ((ICamera)device).EnableSubSample = value; }
        public int SubSampleX { get => ((ICamera)device).SubSampleX; set => ((ICamera)device).SubSampleX = value; }
        public int SubSampleY { get => ((ICamera)device).SubSampleY; set => ((ICamera)device).SubSampleY = value; }
        public int SubSampleWidth { get => ((ICamera)device).SubSampleWidth; set => ((ICamera)device).SubSampleWidth = value; }
        public int SubSampleHeight { get => ((ICamera)device).SubSampleHeight; set => ((ICamera)device).SubSampleHeight = value; }

        public bool CanShowLiveView => ((ICamera)device).CanShowLiveView;

        public bool LiveViewEnabled => ((ICamera)device).LiveViewEnabled;

        public bool HasBattery => ((ICamera)device).HasBattery;

        public int BatteryLevel => ((ICamera)device).BatteryLevel;

        public int BitDepth => ((ICamera)device).BitDepth;

        public bool CanSetOffset => ((ICamera)device).CanSetOffset;

        public int Offset { get => ((ICamera)device).Offset; set => ((ICamera)device).Offset = value; }

        public int OffsetMin => ((ICamera)device).OffsetMin;

        public int OffsetMax => ((ICamera)device).OffsetMax;

        public bool CanSetUSBLimit => ((ICamera)device).CanSetUSBLimit;

        public int USBLimit { get => ((ICamera)device).USBLimit; set => ((ICamera)device).USBLimit = value; }

        public int USBLimitMin => ((ICamera)device).USBLimitMin;

        public int USBLimitMax => ((ICamera)device).USBLimitMax;

        public int USBLimitStep => ((ICamera)device).USBLimitStep;

        public bool CanGetGain => ((ICamera)device).CanGetGain;

        public bool CanSetGain => ((ICamera)device).CanSetGain;

        public int GainMax => ((ICamera)device).GainMax;

        public int GainMin => ((ICamera)device).GainMin;

        public int Gain { get => ((ICamera)device).Gain; set => ((ICamera)device).Gain = value; }

        public double ElectronsPerADU => ((ICamera)device).ElectronsPerADU;

        public IList<string> ReadoutModes => ((ICamera)device).ReadoutModes;

        public short ReadoutMode { get => ((ICamera)device).ReadoutMode; set => ((ICamera)device).ReadoutMode = value; }
        public short ReadoutModeForSnapImages { get => ((ICamera)device).ReadoutModeForSnapImages; set => ((ICamera)device).ReadoutModeForSnapImages = value; }
        public short ReadoutModeForNormalImages { get => ((ICamera)device).ReadoutModeForNormalImages; set => ((ICamera)device).ReadoutModeForNormalImages = value; }

        public IList<int> Gains => ((ICamera)device).Gains;

        public AsyncObservableCollection<BinningMode> BinningModes => ((ICamera)device).BinningModes;

        public bool Connected => ((IDevice)device).Connected;

        public string Description => ((IDevice)device).Description;

        public string DriverInfo => ((IDevice)device).DriverInfo;

        public string DriverVersion => ((IDevice)device).DriverVersion;

        public IList<string> SupportedActions => ((IDevice)device).SupportedActions;

        public void AbortExposure() {
            ((ICamera)device).AbortExposure();
        }

        public string Action(string actionName, string actionParameters) {
            return ((IDevice)device).Action(actionName, actionParameters);
        }

        public void Disconnect() {
            ((IDevice)device).Disconnect();
        }

        public Task<IExposureData> DownloadExposure(CancellationToken token) {
            return ((ICamera)device).DownloadExposure(token);
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return ((ICamera)device).DownloadLiveView(token);
        }

        public void SendCommandBlind(string command, bool raw = true) {
            ((IDevice)device).SendCommandBlind(command, raw);
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return ((IDevice)device).SendCommandBool(command, raw);
        }

        public string SendCommandString(string command, bool raw = true) {
            return ((IDevice)device).SendCommandString(command, raw);
        }

        public void SetBinning(short x, short y) {
            ((ICamera)device).SetBinning(x, y);
        }

        public void StartExposure(CaptureSequence sequence) {
            ((ICamera)device).StartExposure(sequence);
        }

        public void StartLiveView(CaptureSequence sequence) {
            ((ICamera)device).StartLiveView(sequence);
        }

        public void StopExposure() {
            ((ICamera)device).StopExposure();
        }

        public void StopLiveView() {
            ((ICamera)device).StopLiveView();
        }

        public void UpdateSubSampleArea() {
            ((ICamera)device).UpdateSubSampleArea();
        }

        public Task WaitUntilExposureIsReady(CancellationToken token) {
            return ((ICamera)device).WaitUntilExposureIsReady(token);
        }
    }
}
