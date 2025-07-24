#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZWOptical.ASISDK;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Exceptions;
using NINA.Core.Locale;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;

namespace NINA.Equipment.Equipment.MyCamera {

    public class ASICamera : BaseINPC, ICamera {

        public ASICamera(int cameraId, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            _cameraId = cameraId;
        }

        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;
        private readonly int _cameraId;
        private bool _liveViewEnabled = false;

        public string Category { get; } = "ZWOptical";
        public string Id => string.IsNullOrEmpty(CameraAlias) ? Name : $"{Name} #{_cameraId}";

        private ASICameraDll.ASI_CAMERA_INFO? _info;

        private ASICameraDll.ASI_CAMERA_INFO Info {
            // [obsolete] info is cached only while camera is open
            get {
                if (_info == null) {
                    // this needs to be called otherwise GetCameraProperties shuts down other instances of the camera
                    ASICameraDll.OpenCamera(_cameraId);
                    // at this point we might as well cache the properties anyway
                    RefreshCameraInfoCache();
                }

                return _info.Value;
            }
        }

        private void RefreshCameraInfoCache() {
            using (MyStopWatch.Measure()) {
                _info = ASICameraDll.GetCameraProperties(_cameraId);
            }
        }

        private string _cachedName;
        private List<CameraControl> _controls;

        public List<CameraControl> Controls {
            get {
                if (_controls == null || _cachedName != Name) {
                    _cachedName = Name;
                    int cc = ASICameraDll.GetNumOfControls(_cameraId);
                    _controls = new List<CameraControl>();
                    for (int i = 0; i < cc; i++) {
                        try {
                            var control = new CameraControl(_cameraId, i);
                            _ = control.Value; // check if querying a value is possible
                            _controls.Add(control);
                        } catch(Exception ex) {
                            Logger.Debug($"Control capability at index {i} threw an exception: " + ex.Message);
                        }
                    }
                }

                return _controls;
            }
        }

        public bool CanSubSample => true;

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }

        private int subSampleWidth = 0;

        public int SubSampleWidth {
            get {
                if (subSampleWidth == 0) {
                    subSampleWidth = Info.MaxWidth;
                }

                return subSampleWidth;
            }
            set => subSampleWidth = value;
        }

        private int subSampleHeight = 0;

        public int SubSampleHeight {
            get {
                if (subSampleHeight == 0) {
                    subSampleHeight = Info.MaxHeight;
                }

                return subSampleHeight;
            }
            set => subSampleHeight = value;
        }

        public string Name => Info.Name;

        public string DisplayName {
            get {
                if (!string.IsNullOrEmpty(CameraAlias)) {
                    return $"{Info.Name} ({CameraAlias})";
                }

                return Info.Name;
            }
        }

        // ZWO camera alias is limited to 8 ASCII characters. Initialize with something longer to know we haven't yet asked the camera for it
        private string cameraAlias = "%%UNINITIALIZED%%";

        public string CameraAlias {
            get {
                if (cameraAlias.Equals("%%UNINITIALIZED%%")) {
                    // We must connect to the camera to get its ID. Quickly do this if we are not (such as during building the CameraChooser list)
                    if (!Connected) {
                        ASICameraDll.OpenCamera(_cameraId);
                    }

                    cameraAlias = ASICameraDll.GetId(_cameraId);

                    if (!Connected) {
                        ASICameraDll.CloseCamera(_cameraId);
                    }

                    Logger.Debug($"ASI: Camera ID/Alias: {cameraAlias}");
                }

                return cameraAlias;
            }

            set {
                Logger.Debug($"ASI: Setting Camera ID/Alias to: {value}");

                ASICameraDll.SetId(_cameraId, value);
                cameraAlias = ASICameraDll.GetId(_cameraId);

                Logger.Info($"ASI: Camera ID/Alias set to: {cameraAlias}");

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(Id));
                profileService.ActiveProfile.CameraSettings.Id = Id;
            }
        }

        public bool HasShutter => Info.MechanicalShutter != ASICameraDll.ASI_BOOL.ASI_FALSE;

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public bool CanShowLiveView => false;

        public double Temperature => GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TEMPERATURE) / 10.0; //ASI driver gets temperature in Celsius * 10

        public double TemperatureSetPoint {
            get {
                if (CanSetTemperature) {
                    return GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP);
                } else {
                    return double.NaN;
                }
            }
            set {
                if (CanSetTemperature) {
                    //need to be an integer for ASI cameras
                    var nearest = (int)Math.Round(value);

                    if (nearest > maxTemperatureSetpoint) {
                        nearest = maxTemperatureSetpoint;
                    } else if (nearest < minTemperatureSetpoint) {
                        nearest = minTemperatureSetpoint;
                    }
                    if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP, nearest)) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private short bin = 1;

        public short BinX {
            get => bin;
            set {
                bin = value;
                RaisePropertyChanged();
            }
        }

        public short BinY {
            get => bin;
            set {
                bin = value;
                RaisePropertyChanged();
            }
        }

        public string Description => CameraAlias;

        public string DriverInfo {
            get {
                string s = "ZWO ASICamera2";
                return s;
            }
        }

        public string DriverVersion {
            get {
                string version = ASICameraDll.GetSDKVersion();
                return version;
            }
        }

        public string SensorName => string.Empty;

        public SensorType SensorType { get; private set; } = SensorType.Monochrome;

        private bool hasZwoAsiMonoBinMode = false;

        public bool HasZwoAsiMonoBinMode {
            get => hasZwoAsiMonoBinMode;
            set {
                hasZwoAsiMonoBinMode = value;
                RaisePropertyChanged();
            }
        }

        public bool ZwoAsiMonoBinMode {
            get {
                var value = GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_MONO_BIN);
                return value != 0;
            }
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_MONO_BIN, value ? 1 : 0)) {
                    profileService.ActiveProfile.CameraSettings.ZwoAsiMonoBinMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        public short BayerOffsetX { get; } = 0;
        public short BayerOffsetY { get; } = 0;

        public int CameraXSize => Info.MaxWidth;

        public int CameraYSize => Info.MaxHeight;

        public double ExposureMin => (double)GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE) / 1000000;

        public double ExposureMax => (double)GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE) / 1000000;

        public double ElectronsPerADU => Info.ElecPerADU;

        public short MaxBinX {
            get {
                int[] binlist = Info.SupportedBins;
                return (short)binlist.Max();
            }
        }

        public short MaxBinY {
            get {
                int[] binlist = Info.SupportedBins;
                return (short)binlist.Max();
            }
        }

        public double PixelSizeX => Info.PixelSize;

        public double PixelSizeY => Info.PixelSize;

        public string LensName => string.Empty;

        public double LensFocalLength => double.NaN;

        public double LensFocalRatio => double.NaN;

        private int minTemperatureSetpoint = 0;
        private int maxTemperatureSetpoint = 0;

        public bool CanSetTemperature { get; private set; }

        public IList<string> SupportedActions => new List<string>();

        public int BitDepth =>
                //currently ASI camera values are stretched to fit 16 bit
                16;

        public bool CoolerOn {
            get {
                var value = GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_ON);
                return value == 0 ? false : true;
            }
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_ON, value ? 1 : 0)) {
                    RaisePropertyChanged();
                }
            }
        }

        public double CoolerPower => (double)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_POWER_PERC);

        public bool HasDewHeater => GetControlIsWritable(ASICameraDll.ASI_CONTROL_TYPE.ASI_ANTI_DEW_HEATER) == true ? true : false;

        public bool DewHeaterOn {
            get => GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_ANTI_DEW_HEATER) == 0 ? false : true;
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_ANTI_DEW_HEATER, value ? 1 : 0)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int BatteryLevel => -1;

        private AsyncObservableCollection<BinningMode> _binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (_binningModes == null) {
                    _binningModes = new AsyncObservableCollection<BinningMode>();
                    foreach (int f in SupportedBinFactors) {
                        _binningModes.Add(new BinningMode((short)f, (short)f));
                    }
                }
                return _binningModes;
            }
            private set {
            }
        }

        public void AbortExposure() {
            ASICameraDll.StopExposure(_cameraId);
        }

        private List<ASICameraDll.ASI_IMG_TYPE> SupportedImageTypes => Info.SupportedVideoFormat.TakeWhile(x => x != ASICameraDll.ASI_IMG_TYPE.ASI_IMG_END).ToList();

        private List<int> SupportedBinFactors => Info.SupportedBins.TakeWhile(x => x != 0).ToList();

        public void Disconnect() {
            _info = null;
            _controls = null;
            Connected = false;
            ASICameraDll.CloseCamera(_cameraId);
        }

        public CaptureAreaInfo CaptureAreaInfo {
            get {
                var p = ASICameraDll.GetStartPos(_cameraId);
                var res = ASICameraDll.GetROIFormat(_cameraId, out var bin, out var imageType);
                return new CaptureAreaInfo(p, res, bin, imageType);
            }
            set {
                ASICameraDll.SetROIFormat(_cameraId, value.Size, value.Binning, value.ImageType);
                ASICameraDll.SetStartPos(_cameraId, value.Start);
            }
        }

        public Size Resolution {
            get {
                var info = Info;
                return new Size(info.MaxWidth, info.MaxHeight);
            }
        }

        private ASICameraDll.ASI_EXPOSURE_STATUS ExposureStatus => ASICameraDll.GetExposureStatus(_cameraId);

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                while (ExposureStatus == ASICameraDll.ASI_EXPOSURE_STATUS.ASI_EXP_WORKING) {
                    await Task.Delay(10, token);
                }
                lastExposureEndTime = DateTime.UtcNow;
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            return await Task.Run(() => {
                try {
                    var status = ExposureStatus;
                    if (status != ASICameraDll.ASI_EXPOSURE_STATUS.ASI_EXP_SUCCESS) {
                        Logger.Error($"ASI: Camera reported unsuccessful exposure: {status}");
                        throw new CameraDownloadFailedException(Loc.Instance["LblASIImageDownloadError"]);
                    }

                    var width = CaptureAreaInfo.Size.Width;
                    var height = CaptureAreaInfo.Size.Height;

                    int size = width * height;
                    ushort[] arr = new ushort[size];
                    int buffersize = width * height * 2;
                    if (!GetExposureData(arr, buffersize)) {
                        Logger.Error("ASI: Download of exposure data failed");
                        throw new CameraDownloadFailedException(Loc.Instance["LblASIImageDownloadError"]);
                    }

                    var metaData = new ImageMetaData();
                    metaData.FromCamera(this);
                    metaData.Image.SetExposureTimes(lastExposureStartTime, lastExposureEndTime);

                    if (HasZwoAsiMonoBinMode && ZwoAsiMonoBinMode && BinX > 1) {
                        metaData.Camera.BayerPattern = BayerPatternEnum.None;
                    }

                    return exposureDataFactory.CreateImageArrayExposureData(
                        input: arr,
                        width: width,
                        height: height,
                        bitDepth: BitDepth,
                        isBayered: SensorType != SensorType.Monochrome && metaData.Camera.BayerPattern != BayerPatternEnum.None,
                        metaData: metaData);
                } catch (OperationCanceledException) {
                } catch (CameraDownloadFailedException ex) {
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblZWODriverError"]);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblZWODriverError"]);
                }
                return null;
            });
        }

        private bool GetExposureData(ushort[] buffer, int bufferSize) {
            return ASICameraDll.GetDataAfterExp(_cameraId, buffer, bufferSize);
        }

        public void SetBinning(short x, short y) {
            BinX = x;
            BinY = y;
        }

        public void StartExposure(CaptureSequence sequence) {
            int exposureMs = (int)(sequence.ExposureTime * 1000000);
            var exposureSettings = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE);
            exposureSettings.Value = exposureMs;
            exposureSettings.IsAuto = false;

            var isDarkFrame = sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                               sequence.ImageType == CaptureSequence.ImageTypes.BIAS;

            if (EnableSubSample) {
                CaptureAreaInfo = new CaptureAreaInfo(
                    new Point(SubSampleX / BinX, SubSampleY / BinY),
                    new Size(SubSampleWidth / BinX - (SubSampleWidth / BinX % 8),
                             SubSampleHeight / BinY - (SubSampleHeight / BinY % 2)
                    ),
                    BinX,
                    ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16
                );
            } else {
                CaptureAreaInfo = new CaptureAreaInfo(
                    new Point(0, 0),
                    new Size((Resolution.Width / BinX) - (Resolution.Width / BinX % 8),
                              Resolution.Height / BinY - (Resolution.Height / BinY % 2)
                    ),
                    BinX,
                    ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16
                );
            }

            lastExposureStartTime = DateTime.UtcNow;
            ASICameraDll.StartExposure(_cameraId, isDarkFrame);
        }

        public void StopExposure() {
            ASICameraDll.StopExposure(_cameraId);
        }

        private CameraControl GetControl(ASICameraDll.ASI_CONTROL_TYPE controlType) {
            return Controls.FirstOrDefault(x => x.ControlType == controlType);
        }

        public bool CanGetGain => true;

        public bool CanSetGain => true;

        public int Gain {
            get => GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN, value)) {
                    RefreshCameraInfoCache();
                    RaisePropertyChanged();
                }
            }
        }

        public int GainMax => GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);

        public int GainMin => GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);

        public bool CanSetOffset => true;

        public bool CanSetUSBLimit => true;

        public int Offset {
            get => GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_OFFSET);
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_OFFSET, value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int OffsetMin => GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_OFFSET);

        public int OffsetMax => GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_OFFSET);

        public int USBLimit {
            get => GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD);
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD, value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int USBLimitMin => GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD);

        public int USBLimitMax => GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD);

        public int USBLimitStep => 1;

        private int GetControlValue(ASICameraDll.ASI_CONTROL_TYPE type) {
            var control = GetControl(type);
            return control?.Value ?? 0;
        }

        private int GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE type) {
            var control = GetControl(type);
            return control?.MaxValue ?? 0;
        }

        private int GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE type) {
            var control = GetControl(type);
            return control?.MinValue ?? 0;
        }

        private bool GetControlIsWritable(ASICameraDll.ASI_CONTROL_TYPE type) {
            var control = GetControl(type);
            return control?.IsWritable ?? false;
        }

        private bool SetControlValue(ASICameraDll.ASI_CONTROL_TYPE type, int value) {
            try {
                var control = GetControl(type);
                if (control != null && value <= control.MaxValue && value >= control.MinValue) {
                    control.Value = value;
                    return true;
                } else {
                    Logger.Warning(string.Format("Failed to set ASI Control Value {0} with value {1}", type, value));
                    return false;
                }
            } catch (Exception ex) {
                Logger.Error($"Error occurred during set of ASI Control Value {type}:", ex);
                return false;
            }
        }

        public IList<string> ReadoutModes => new List<string> { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

        private short _readoutModeForSnapImages;

        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set {
                _readoutModeForSnapImages = value;
                RaisePropertyChanged();
            }
        }

        private short _readoutModeForNormalImages;
        private DateTime lastExposureEndTime;
        private DateTime lastExposureStartTime;

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                _readoutModeForNormalImages = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => false;

        public CameraStates CameraState {
            get {
                CameraStates state = CameraStates.Idle;

                switch (ExposureStatus) {
                    case ASICameraDll.ASI_EXPOSURE_STATUS.ASI_EXP_WORKING:
                        state = CameraStates.Exposing;
                        break;
                }

                return state;
            }
        }

        public IList<int> Gains => new List<int>();

        public void SetupDialog() {
        }

        public void Initialize() {
            DetermineAndSetSensorType();
            //Check if camera can set temperature
            CanSetTemperature = false;
            var val = GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_ON);
            if (val > 0) {
                CanSetTemperature = true;
                maxTemperatureSetpoint = GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP);
                minTemperatureSetpoint = GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP);
            }

            var flip = (ASICameraDll.ASI_FLIP_STATUS)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_FLIP);
            if (flip != ASICameraDll.ASI_FLIP_STATUS.ASI_FLIP_NONE) {
                Logger.Info($"Resetting ASI Flip Status to NONE. It was {flip}");
                SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_FLIP, (int)ASICameraDll.ASI_FLIP_STATUS.ASI_FLIP_NONE);
            }

            USBLimit = 40;

            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_WB_B, 50);
            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_WB_R, 50);
            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAMMA, 50);
            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HIGH_SPEED_MODE, 0);
            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN, 0);
            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_OVERCLOCK, 0);
            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_PATTERN_ADJUST, 0);

            // Assumption that all color models support mono binning mode
            HasZwoAsiMonoBinMode = Info.IsColorCam == ASICameraDll.ASI_BOOL.ASI_TRUE;

            if (HasZwoAsiMonoBinMode && profileService.ActiveProfile.CameraSettings.ZwoAsiMonoBinMode == true) {
                ZwoAsiMonoBinMode = true;
            } else {
                ZwoAsiMonoBinMode = false;
            }

            var id = ASICameraDll.GetId(_cameraId);
            Logger.Info($"Camera ID: {id}");
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                var success = false;
                try {
                    ASICameraDll.OpenCamera(_cameraId);
                    ASICameraDll.InitCamera(_cameraId);
                    RefreshCameraInfoCache();
                    Connected = true;
                    success = true;

                    var raw16 = from types in SupportedImageTypes where types == ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16 select types;
                    if (!raw16.Any()) {
                        Logger.Error("Camera does not support 16 bit mode");
                        Notification.ShowError(Loc.Instance["LblCameraNoSupport16bit"]);
                        return false;
                    }
                    this.CaptureAreaInfo = new CaptureAreaInfo(new Point(0, 0), this.Resolution, 1, ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16);
                    Initialize();
                    RaisePropertyChanged(nameof(Connected));
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblZWODriverError"]);
                }
                return success;
            });
        }

        private void DetermineAndSetSensorType() {
            if (Info.IsColorCam == ASICameraDll.ASI_BOOL.ASI_TRUE) {
                switch (Info.BayerPattern) {
                    case ASICameraDll.ASI_BAYER_PATTERN.ASI_BAYER_GB:
                        SensorType = SensorType.GBRG;
                        break;

                    case ASICameraDll.ASI_BAYER_PATTERN.ASI_BAYER_GR:
                        SensorType = SensorType.GRBG;
                        break;

                    case ASICameraDll.ASI_BAYER_PATTERN.ASI_BAYER_BG:
                        SensorType = SensorType.BGGR;
                        break;

                    case ASICameraDll.ASI_BAYER_PATTERN.ASI_BAYER_RG:
                        SensorType = SensorType.RGGB;
                        break;

                    default:
                        SensorType = SensorType.Monochrome;
                        break;
                };
            } else {
                SensorType = SensorType.Monochrome;
            }
        }

        public void StartLiveView(CaptureSequence sequence) {
            int exposureMs = (int)(sequence.ExposureTime * 1000000);
            var exposureSettings = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE);
            exposureSettings.Value = exposureMs;
            exposureSettings.IsAuto = false;

            if (EnableSubSample) {
                CaptureAreaInfo = new CaptureAreaInfo(
                    new Point(SubSampleX / BinX, SubSampleY / BinY),
                    new Size(SubSampleWidth / BinX - (SubSampleWidth / BinX % 8),
                             SubSampleHeight / BinY - (SubSampleHeight / BinY % 2)
                    ),
                    BinX,
                    ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16
                );
            } else {
                CaptureAreaInfo = new CaptureAreaInfo(
                    new Point(0, 0),
                    new Size((Resolution.Width / BinX) - (Resolution.Width / BinX % 8),
                              Resolution.Height / BinY - (Resolution.Height / BinY % 2)
                    ),
                    BinX,
                    ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16
                );
            }

            ASICameraDll.StartVideoCapture(_cameraId);
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return Task.Run<IExposureData>(() => {
                try {
                    var width = CaptureAreaInfo.Size.Width;
                    var height = CaptureAreaInfo.Size.Height;

                    int size = width * height;

                    ushort[] arr = new ushort[size];
                    int buffersize = width * height * 2;
                    DateTime startDateTime = DateTime.UtcNow;
                    if (!GetVideoData(arr, buffersize)) {
                        throw new CameraDownloadFailedException(Loc.Instance["LblASIImageDownloadError"]);
                    }

                    DateTime endDateTime = DateTime.UtcNow;
                    var metaData = new ImageMetaData();
                    metaData.FromCamera(this);
                    metaData.Image.SetExposureTimes(startDateTime, endDateTime);

                    if (HasZwoAsiMonoBinMode && ZwoAsiMonoBinMode && BinX > 1) {
                        metaData.Camera.BayerPattern = BayerPatternEnum.None;
                    }

                    // get dropped frames
                    DroppedFrames = ASICameraDll.GetDroppedFrames(_cameraId);

                    return exposureDataFactory.CreateImageArrayExposureData(
                        input: arr,
                        width: width,
                        height: height,
                        bitDepth: BitDepth,
                        isBayered: SensorType != SensorType.Monochrome && metaData.Camera.BayerPattern != BayerPatternEnum.None,
                        metaData: metaData);
                } catch (OperationCanceledException) {
                } catch (CameraDownloadFailedException ex) {
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblZWODriverError"]);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblZWODriverError"]);
                }
                return null;
            });
        }

        private bool GetVideoData(ushort[] buffer, int bufferSize) {
            return ASICameraDll.GetVideoData(_cameraId, buffer, bufferSize, -1);
        }

        public void StopLiveView() {
            ASICameraDll.StopVideoCapture(_cameraId);
        }

        public bool LiveViewEnabled {
            get => _liveViewEnabled;
            set {
                if (_liveViewEnabled != value) {
                    _liveViewEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }
        public int DroppedFrames { get; private set; }

        public void UpdateSubSampleArea() {
            // This is not fully working yet, still needs testing, might not be possible or needs some other approach
            // I did see it working when taking images 0.5s images, but not consistently
            if (EnableSubSample) {
                CaptureAreaInfo = new CaptureAreaInfo(
                    new Point(SubSampleX / BinX, SubSampleY / BinY),
                    new Size(SubSampleWidth / BinX - (SubSampleWidth / BinX % 8),
                             SubSampleHeight / BinY - (SubSampleHeight / BinY % 2)
                    ),
                    BinX,
                    ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16
                );
            } else {
                CaptureAreaInfo = new CaptureAreaInfo(
                    new Point(0, 0),
                    new Size((Resolution.Width / BinX) - (Resolution.Width / BinX % 8),
                              Resolution.Height / BinY - (Resolution.Height / BinY % 2)
                    ),
                    BinX,
                    ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16
                );
            }
        }

        public bool HasBattery => false;

        public string Action(string actionName, string actionParameters) {
            switch (actionName) {
                case "GetDroppedFrames": { 
                    return DroppedFrames.ToString();
                    }
                case "HighSpeedMode": {
                        var value = StringToBoolean(actionParameters);
                        if(value.HasValue) { 
                            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HIGH_SPEED_MODE, value.Value ? 1 : 0);
                        }
                        return "";
                    }
                case "HardwareBin": {
                        var value = StringToBoolean(actionParameters);
                        if (value.HasValue) {
                            SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN, value.Value ? 1 : 0);
                        }
                        return "";
                    }
                default:
                    return "";
            }
        }

        private bool? StringToBoolean(string input) {
            if (string.IsNullOrWhiteSpace(input)) { return null; }

            string[] booleanFalse = { "0", "off", "no", "false", "f" };
            string[] booleanTrue = { "1", "on", "yes", "true", "t" };

            if (booleanFalse.Contains(input.ToLower())) {
                return false;
            }
            if (booleanTrue.Contains(input.ToLower())) {
                return true;
            }
            return null;
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
    }

    public class CaptureAreaInfo {
        public Point Start { get; set; }
        public Size Size { get; set; }
        public int Binning { get; set; }
        public ASICameraDll.ASI_IMG_TYPE ImageType { get; set; }

        public CaptureAreaInfo(Point start, Size size, int binning, ASICameraDll.ASI_IMG_TYPE imageType) {
            Start = start;
            Size = size;
            Binning = binning;
            ImageType = imageType;
        }
    }

    public class CameraControl {
        private readonly int _cameraId;
        private ASICameraDll.ASI_CONTROL_CAPS _props;
        private bool _auto;

        public CameraControl(int cameraId, int controlIndex) {
            _cameraId = cameraId;

            _props = ASICameraDll.GetControlCaps(_cameraId, controlIndex);
            _auto = GetAutoSetting();
        }

        public string Name => _props.Name;
        public string Description => _props.Description;
        public int MinValue => _props.MinValue;
        public int MaxValue => _props.MaxValue;
        public int DefaultValue => _props.DefaultValue;
        public ASICameraDll.ASI_CONTROL_TYPE ControlType => _props.ControlType;
        public bool IsAutoAvailable => _props.IsAutoSupported != ASICameraDll.ASI_BOOL.ASI_FALSE;
        public bool IsWritable => _props.IsWritable != ASICameraDll.ASI_BOOL.ASI_FALSE;

        public int Value {
            get => ASICameraDll.GetControlValue(_cameraId, _props.ControlType, out var isAuto);
            set => ASICameraDll.SetControlValue(_cameraId, _props.ControlType, value, IsAuto);
        }

        public bool IsAuto {
            get => _auto;
            set {
                _auto = value;
                ASICameraDll.SetControlValue(_cameraId, _props.ControlType, Value, value);
            }
        }

        private bool GetAutoSetting() {
            ASICameraDll.GetControlValue(_cameraId, _props.ControlType, out var isAuto);
            return isAuto;
        }
    }
}