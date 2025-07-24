using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera {
    public class GenericCamera : BaseINPC, ICamera {
        private IGenericCameraSDK sdk;
        protected IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;

        public GenericCamera(string id, string name, string category, string driverVersion, bool supportBitScaling, IGenericCameraSDK sdk, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.Name = name;
            this.sdk = sdk;
            this.Category = category;
            this.supportBitScaling = supportBitScaling;
            this.Description = $"{id}";
            this.DriverInfo = $"Native driver implementation for {category} Cameras";
            this.DriverVersion = driverVersion;
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            this.Id = Category + "_" + id;
        }

        public bool Connected => sdk.Connected;

        protected virtual void Initialize() {
            BinningModes = new AsyncObservableCollection<BinningMode>();
            var binnings = sdk.GetBinningInfo();
            if (binnings.Length > 0) {
                Array.Sort(binnings);
                MaxBinX = (short)binnings.Max();
                MaxBinY = (short)binnings.Max();

                foreach (var bin in binnings) {
                    BinningModes.Add(new BinningMode((short)bin, (short)bin));
                }
            } else {
                MaxBinX = 1;
                MaxBinY = 1;
                BinningModes.Add(new BinningMode(1, 1));
            }

            var size = sdk.GetPixelSize();
            if (size <= 0) {
                PixelSizeX = double.NaN;
            } else {
                PixelSizeX = size;
            }

            (CameraXSize, CameraYSize) = sdk.GetDimensions();

            SensorType = sdk.GetSensorInfo();

            CanGetTemperature = sdk.HasTemperatureReadout();
            CanSetTemperature = sdk.HasTemperatureControl();
            HasDewHeater = sdk.HasDewHeater();

            ReadoutModes = sdk.GetReadoutModes();            
        }

        private bool supportBitScaling;
        public int BitDepth => supportBitScaling && profileService.ActiveProfile.CameraSettings.BitScaling ? 16 : sdk.GetBitDepth();

        public SensorType SensorType { get; private set; }

        public int CameraXSize { get; private set; }

        public int CameraYSize { get; private set; }

        public double PixelSizeX { get; private set; }

        public double PixelSizeY => PixelSizeX;

        public string LensName => string.Empty;

        public double LensFocalLength => double.NaN;

        public double LensFocalRatio => double.NaN;

        public double ExposureMin => sdk.GetMinExposureTime();

        public double ExposureMax => Math.Ceiling(sdk.GetMaxExposureTime());

        private short bin = 1;

        public short BinX {
            get => bin;
            set {
                if (value <= 0) { value = 1; }
                if (value > MaxBinX) { value = MaxBinX; }
                bin = value;
                RaisePropertyChanged();
            }
        }

        public short BinY {
            get => bin;
            set {
                if (value <= 0) { value = 1; }
                if (value > MaxBinY) { value = MaxBinY; }
                bin = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; private set; }

        public short MaxBinX { get; private set; }

        public short MaxBinY { get; private set; }

        public bool CanGetGain => true;

        public bool CanSetGain => true;

        public int Gain {
            get => sdk.GetGain();
            set {
                if (sdk.SetGain(value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int GainMax => sdk.GetMaxGain();

        public int GainMin => sdk.GetMinGain();

        public bool CanSetOffset => true;

        public bool CanSetUSBLimit => true;

        public int Offset {
            get => sdk.GetOffset();
            set {
                if (sdk.SetOffset(value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int OffsetMin => sdk.GetMinOffset();

        public int OffsetMax => sdk.GetMaxOffset();

        public int USBLimit {
            get => sdk.GetUSBLimit();
            set {
                if (sdk.SetUSBLimit(value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int USBLimitMin => sdk.GetMinUSBLimit();

        public int USBLimitMax => sdk.GetMaxUSBLimit();

        public int USBLimitStep => 1;

        public IList<string> SupportedActions => new List<string>();

        public void SetBinning(short x, short y) {
            BinX = x;
            BinY = y;
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                try {
                    sdk.Connect();
                    if (Connected) {
                        Initialize();
                        RaisePropertyChanged(nameof(Connected));
                        RaiseAllPropertiesChanged();
                        return true;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                return false;
            });
        }

        public void Disconnect() {
            try {
                sdk.Disconnect();
                RaisePropertyChanged(nameof(Connected));
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public void StartExposure(CaptureSequence sequence) {
            var isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;
            var readoutMode = isSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;
            ReadoutMode = readoutMode;

            if (EnableSubSample) {
                sdk.SetROI(SubSampleX, SubSampleY, SubSampleWidth, SubSampleHeight, BinX);
            } else {
                sdk.SetROI(0, 0, CameraXSize, CameraYSize, BinX);
            }
            var (x, y, width, height, binning) = sdk.GetROI();
            exposureTaskWidth = width;
            exposureTaskHeight = height;
            exposureTaskTime = sequence.ExposureTime;
            lastExposureStartTime = sdk.StartExposure(sequence.ExposureTime, width, height);

        }

        private int exposureTaskWidth;
        private int exposureTaskHeight;
        private double exposureTaskTime;
        private DateTime lastExposureStartTime;
        private DateTime lastExposureEndTime;

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                while (!sdk.IsExposureReady()) {
                    await CoreUtil.Wait(TimeSpan.FromMilliseconds(10), token);
                }
                lastExposureEndTime = DateTime.UtcNow;
            }
        }

        public void StopExposure() {
            try {
                sdk.StopExposure();
            } catch (Exception) { }
        }

        public void AbortExposure() {
            sdk.StopExposure();
            try {
                sdk.StopExposure();
            } catch (Exception) { }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            ushort[] data = null;
            using (var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                try {
                    downloadCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(60, profileService.ActiveProfile.CameraSettings.Timeout)));
                    data = await sdk.GetExposure(exposureTaskTime, exposureTaskWidth, exposureTaskHeight, downloadCts.Token);
                } catch { }                
            }
            if (data == null) { return null; }

            var bitScaling = supportBitScaling && this.profileService.ActiveProfile.CameraSettings.BitScaling;
            if (bitScaling) {
                var nativeBitDepth = sdk.GetBitDepth();
                var shift = 16 - nativeBitDepth;
                for (var i = 0; i < data.Length; i++) {
                    data[i] = (ushort)(data[i] << shift);
                }
            }

            var (x, y, width, height, binning) = sdk.GetROI();

            var metaData = new ImageMetaData();
            metaData.FromCamera(this);
            metaData.Image.SetExposureTimes(lastExposureStartTime, lastExposureEndTime);
            return exposureDataFactory.CreateImageArrayExposureData(
                        input: data,
                        width: width,
                        height: height,
                        bitDepth: this.BitDepth,
                        isBayered: SensorType != SensorType.Monochrome,
                        metaData: metaData);
        }

        #region "Temperature Control"
        public bool HasDewHeater { get; private set; }

        public bool DewHeaterOn {
            get => sdk.IsDewHeaterOn();
            set {
                var strength = value ? profileService.ActiveProfile.CameraSettings.GenericCameraDewHeaterStrength : 0;
                if (sdk.SetDewHeater(strength)) {
                    RaisePropertyChanged();
                }
            }
        }

        public bool CanGetTemperature { get; private set; }
        public bool CanSetTemperature { get; private set; }

        public bool CoolerOn {
            get {
                if (CanSetTemperature) {
                    return sdk.GetCoolerOnOff();
                }
                return false;
            }
            set {
                if (CanSetTemperature) {
                    if (sdk.SetCooler(value)) {
                        if(value && HasAdjustableFan) { 
                            sdk.SetFanPercentage(profileService.ActiveProfile.CameraSettings.GenericCameraFanSpeed); 
                        }
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public double CoolerPower {
            get {
                if (CanSetTemperature) {
                    return sdk.GetCoolerPower();
                }
                return double.NaN;
            }
        }

        public double Temperature {
            get {
                if (CanGetTemperature) {
                    return sdk.GetTemperature();
                }
                return double.NaN;
            }
        }

        public double TemperatureSetPoint {
            get {
                if (CanSetTemperature) {
                    return sdk.GetTargetTemperature();
                }
                return double.NaN;
            }
            set {
                if (CanSetTemperature) {
                    if (sdk.SetTargetTemperature(value)) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public bool HasAdjustableFan => sdk.HasAdjustableFan();

        public int FanSpeed {
            get {
                if(HasAdjustableFan) { 
                    return sdk.GetFanPercentage();
                } else {
                    return 0;
                }
            }
            set {
                if (HasAdjustableFan) {
                    var currentFanSpeed = FanSpeed;
                    var targetFanSpeed = Math.Max(0, Math.Min(100, value));
                    if (currentFanSpeed != targetFanSpeed) {
                        if (sdk.SetFanPercentage(value)) {
                            RaisePropertyChanged();
                        } else {
                            Logger.Error($"{Category} - Could not set Fan to {value}");
                        }
                    }
                }
            }
        }


        public int TargetFanSpeed {
            get => profileService.ActiveProfile.CameraSettings.GenericCameraFanSpeed;
            set {
                profileService.ActiveProfile.CameraSettings.GenericCameraFanSpeed = Math.Max(10, Math.Min(100, value));
                if (sdk.GetFanPercentage() > 0) {
                    if (!sdk.SetFanPercentage(profileService.ActiveProfile.CameraSettings.GenericCameraFanSpeed)) {
                        Logger.Error($"{Category} - Could not set Fan to {value}");
                    }
                }
                RaisePropertyChanged();
            }
        }

        public int TargetDewHeaterStrength {
            get => profileService.ActiveProfile.CameraSettings.GenericCameraDewHeaterStrength;
            set {
                profileService.ActiveProfile.CameraSettings.GenericCameraDewHeaterStrength = value;
                if(DewHeaterOn) {
                    sdk.SetDewHeater(profileService.ActiveProfile.CameraSettings.GenericCameraDewHeaterStrength);
                }
                RaisePropertyChanged();
            }
        }

        #endregion "Temperature Control"

        #region "Meta Data"

        public string Id { get; }
        public string Name { get; }
        public string DisplayName => $"{Name} ({(Id.Length > 8 ? Id[^8..] : Id)})";
        public string Category { get; }
        public string Description { get; }
        public string DriverInfo { get; }
        public string DriverVersion { get; }

        #endregion "Meta Data"

        #region "Subsample"

        public bool CanSubSample => true;
        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        #endregion "Subsample"

        #region "LiveView"

        // Live view is deprecated
        public bool CanShowLiveView => false;

        public void StartLiveView(CaptureSequence sequence) {
            if (EnableSubSample) {
                sdk.SetROI(SubSampleX, SubSampleY, SubSampleWidth, SubSampleHeight, BinX);
            } else {
                sdk.SetROI(0, 0, CameraXSize, CameraYSize, BinX);
            }
            var (x, y, width, height, binning) = sdk.GetROI();

            exposureTaskWidth = width;
            exposureTaskHeight = height;
            exposureTaskTime = sequence.ExposureTime;

            sdk.StartVideoCapture(sequence.ExposureTime, width, height);
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return Task.Run<IExposureData>(async () => {
                try {
                    var lastLiveViewExposureStart = DateTime.UtcNow;
                    var data = await sdk.GetVideoCapture(exposureTaskTime, exposureTaskWidth, exposureTaskHeight, token);
                    var lastLiveViewExposureEnd = DateTime.UtcNow;
                    if (data == null) { return null; }

                    var (x, y, width, height, binning) = sdk.GetROI();

                    var metaData = new ImageMetaData();
                    metaData.FromCamera(this);
                    metaData.Image.SetExposureTimes(lastLiveViewExposureStart, lastLiveViewExposureEnd);
                    return exposureDataFactory.CreateImageArrayExposureData(
                                input: data,
                                width: width,
                                height: height,
                                bitDepth: this.BitDepth,
                                isBayered: SensorType != SensorType.Monochrome,
                                metaData: metaData);
                } catch (OperationCanceledException) {
                }
                return null;
            });
        }

        public void StopLiveView() {
            sdk.StopVideoCapture();
        }


        private bool _liveViewEnabled = false;
        public bool LiveViewEnabled {
            get => _liveViewEnabled;
            set {
                if (_liveViewEnabled != value) {
                    _liveViewEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion "LiveView"

        #region "Readout Modes"


        private IList<string> readoutModes = new List<string> { "Default" };
        public IList<string> ReadoutModes {
            get => readoutModes;
            private set {
                readoutModes = value;
                RaisePropertyChanged();
            }
        }

        public short ReadoutMode {
            get => (short)sdk.GetReadoutMode();
            set {
                sdk.SetReadoutMode(value);
                RaisePropertyChanged();
            }
        }

        private short _readoutModeForNormalImages = 0;
        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    _readoutModeForNormalImages = value;
                } else {
                    _readoutModeForNormalImages = 0;
                }

                RaisePropertyChanged();
            }
        }

        private short _readoutModeForSnapImages = 0;
        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    _readoutModeForSnapImages = value;
                } else {
                    _readoutModeForSnapImages = 0;
                }

                RaisePropertyChanged();
            }
        }

        #endregion

        #region "Unsupported Features"

        public CameraStates CameraState => CameraStates.NoState;

        public bool HasSetupDialog => false;

        public void SetupDialog() {
        }

        public string SensorName => string.Empty;

        public bool HasShutter => false;

        public bool HasBattery => false;
        public int BatteryLevel => -1;
        public double ElectronsPerADU => double.NaN;
        public short BayerOffsetX { get; } = 0;
        public short BayerOffsetY { get; } = 0;

        public IList<int> Gains => new List<int>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void UpdateSubSampleArea() {
            throw new NotImplementedException();
        }
        #endregion "Unsupported Features"
    }
}
