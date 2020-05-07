using NINA.Profile;
using NINA.Utility;
using Nito.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    internal class PersistSettingsCameraDecorator : BaseINPC, ICamera {
        private readonly ICamera camera;
        private readonly IProfileService profileService;

        public PersistSettingsCameraDecorator(IProfileService profileService, ICamera camera) {
            this.profileService = profileService;
            this.camera = camera;
            this.camera.PropertyChanged += Camera_PropertyChanged;
        }

        private void Camera_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged(e.PropertyName);
        }

        private void RestoreCameraProfileDefaults() {
            if (this.profileService.ActiveProfile.CameraSettings.BinningX.HasValue && this.profileService.ActiveProfile.CameraSettings.BinningY.HasValue) {
                try {
                    this.camera.SetBinning(this.profileService.ActiveProfile.CameraSettings.BinningX.Value, this.profileService.ActiveProfile.CameraSettings.BinningY.Value);
                } catch (Exception e) {
                    this.profileService.ActiveProfile.CameraSettings.BinningX = null;
                    this.profileService.ActiveProfile.CameraSettings.BinningY = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.Gain.HasValue) {
                try {
                    this.camera.Gain = this.profileService.ActiveProfile.CameraSettings.Gain.Value;
                } catch (Exception e) {
                    this.profileService.ActiveProfile.CameraSettings.Gain = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.Offset.HasValue) {
                try {
                    this.camera.Offset = this.profileService.ActiveProfile.CameraSettings.Offset.Value;
                } catch (Exception e) {
                    this.profileService.ActiveProfile.CameraSettings.Offset = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.USBLimit.HasValue) {
                try {
                    this.camera.USBLimit = this.profileService.ActiveProfile.CameraSettings.USBLimit.Value;
                } catch (Exception e) {
                    this.profileService.ActiveProfile.CameraSettings.USBLimit = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.Temperature.HasValue) {
                try {
                    this.camera.TemperatureSetPoint = this.profileService.ActiveProfile.CameraSettings.Temperature.Value;
                } catch (Exception e) {
                    this.profileService.ActiveProfile.CameraSettings.Temperature = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.ReadoutMode.HasValue) {
                try {
                    this.camera.ReadoutMode = this.profileService.ActiveProfile.CameraSettings.ReadoutMode.Value;
                } catch (Exception e) {
                    this.profileService.ActiveProfile.CameraSettings.ReadoutMode = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages.HasValue) {
                try {
                    this.camera.ReadoutModeForSnapImages = this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages.Value;
                } catch (Exception e) {
                    this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages.HasValue) {
                try {
                    this.camera.ReadoutModeForNormalImages = this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages.Value;
                } catch (Exception e) {
                    this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages = null;
                }
            }
        }

        public bool HasShutter => this.camera.HasShutter;

        public double Temperature => this.camera.Temperature;

        public double TemperatureSetPoint {
            get => this.camera.TemperatureSetPoint;
            set {
                this.camera.TemperatureSetPoint = value;
                this.profileService.ActiveProfile.CameraSettings.Temperature = value;
            }
        }

        public short BinX {
            get => this.camera.BinX;
            set {
                this.camera.BinX = value;
                this.profileService.ActiveProfile.CameraSettings.BinningX = value;
            }
        }

        public short BinY {
            get => this.camera.BinY;
            set {
                this.camera.BinY = value;
                this.profileService.ActiveProfile.CameraSettings.BinningY = value;
            }
        }

        public string SensorName => this.camera.SensorName;

        public SensorType SensorType => this.camera.SensorType;

        public short BayerOffsetX => this.camera.BayerOffsetX;

        public short BayerOffsetY => this.camera.BayerOffsetY;

        public int CameraXSize => this.camera.CameraXSize;

        public int CameraYSize => this.camera.CameraYSize;

        public double ExposureMin => this.camera.ExposureMin;

        public double ExposureMax => this.camera.ExposureMax;

        public short MaxBinX => this.camera.MaxBinX;

        public short MaxBinY => this.camera.MaxBinY;

        public double PixelSizeX => this.camera.PixelSizeX;

        public double PixelSizeY => this.camera.PixelSizeY;

        public bool CanSetTemperature => this.camera.CanSetTemperature;

        public bool CoolerOn { get => this.camera.CoolerOn; set => this.camera.CoolerOn = value; }

        public double CoolerPower => this.camera.CoolerPower;

        public bool HasDewHeater => this.camera.HasDewHeater;

        public bool DewHeaterOn { get => this.camera.DewHeaterOn; set => this.camera.DewHeaterOn = value; }

        public string CameraState => this.camera.CameraState;

        public bool CanSubSample => this.camera.CanSubSample;

        public bool EnableSubSample { get => this.camera.EnableSubSample; set => this.camera.EnableSubSample = value; }
        public int SubSampleX { get => this.camera.SubSampleX; set => this.camera.SubSampleX = value; }
        public int SubSampleY { get => this.camera.SubSampleY; set => this.camera.SubSampleY = value; }
        public int SubSampleWidth { get => this.camera.SubSampleWidth; set => this.camera.SubSampleWidth = value; }
        public int SubSampleHeight { get => this.camera.SubSampleHeight; set => this.camera.SubSampleHeight = value; }

        public bool CanShowLiveView => this.camera.CanShowLiveView;

        public bool LiveViewEnabled => this.camera.LiveViewEnabled;

        public bool HasBattery => this.camera.HasBattery;

        public int BatteryLevel => this.camera.BatteryLevel;

        public int BitDepth => this.camera.BitDepth;

        public int Offset {
            get {
                return this.camera.Offset;
            }
            set {
                this.camera.Offset = value;
                this.profileService.ActiveProfile.CameraSettings.Offset = value;
            }
        }

        public int USBLimit {
            get => this.camera.USBLimit;
            set {
                this.camera.USBLimit = value;
                this.profileService.ActiveProfile.CameraSettings.USBLimit = value;
            }
        }

        public bool CanSetOffset => this.camera.CanSetOffset;

        public int OffsetMin => this.camera.OffsetMin;

        public int OffsetMax => this.camera.OffsetMax;

        public bool CanSetUSBLimit => this.camera.CanSetUSBLimit;

        public bool CanGetGain => this.camera.CanGetGain;

        public bool CanSetGain => this.camera.CanSetGain;

        public int GainMax => this.camera.GainMax;

        public int GainMin => this.camera.GainMin;

        public int Gain {
            get => this.camera.Gain;
            set {
                this.camera.Gain = value;
                this.profileService.ActiveProfile.CameraSettings.Gain = value;
            }
        }

        public double ElectronsPerADU => this.camera.ElectronsPerADU;

        public IEnumerable ReadoutModes => this.camera.ReadoutModes;

        public short ReadoutMode {
            get => this.camera.ReadoutMode;
            set {
                this.camera.ReadoutMode = value;
                this.profileService.ActiveProfile.CameraSettings.ReadoutMode = value;
            }
        }

        public short ReadoutModeForSnapImages {
            get => this.camera.ReadoutModeForSnapImages;
            set {
                this.camera.ReadoutModeForSnapImages = value;
                this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages = value;
            }
        }

        public short ReadoutModeForNormalImages {
            get => this.camera.ReadoutModeForNormalImages;
            set {
                this.camera.ReadoutModeForNormalImages = value;
                this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages = value;
            }
        }

        public ArrayList Gains => this.camera.Gains;

        public AsyncObservableCollection<BinningMode> BinningModes => this.camera.BinningModes;

        public bool HasSetupDialog => this.camera.HasSetupDialog;

        public string Id => this.camera.Id;

        public string Name => this.camera.Name;

        public string Category => this.camera.Category;

        public bool Connected => this.camera.Connected;

        public string Description => this.camera.Description;

        public string DriverInfo => this.camera.DriverInfo;

        public string DriverVersion => this.camera.DriverVersion;

        public void AbortExposure() {
            this.camera.AbortExposure();
        }

        public async Task<bool> Connect(CancellationToken token) {
            var result = await this.camera.Connect(token);
            if (result) {
                RestoreCameraProfileDefaults();
            }
            return result;
        }

        public void Disconnect() {
            this.camera.Disconnect();
        }

        public Task<IExposureData> DownloadExposure(CancellationToken token) {
            return this.camera.DownloadExposure(token);
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return this.camera.DownloadLiveView(token);
        }

        public void SetBinning(short x, short y) {
            this.camera.SetBinning(x, y);
        }

        public void SetupDialog() {
            this.camera.SetupDialog();
        }

        public void StartExposure(CaptureSequence sequence) {
            this.camera.StartExposure(sequence);
        }

        public void StartLiveView() {
            this.camera.StartLiveView();
        }

        public void StopExposure() {
            this.camera.StopExposure();
        }

        public void StopLiveView() {
            this.camera.StopLiveView();
        }

        public Task WaitUntilExposureIsReady(CancellationToken token) {
            return this.camera.WaitUntilExposureIsReady(token);
        }
    }
}