#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZWOptical.ASISDK;
using NINA.Model.ImageData;

namespace NINA.Model.MyCamera {

    public class ASICamera : BaseINPC, ICamera {

        public ASICamera(int cameraId, IProfileService profileService) {
            this.profileService = profileService;
            _cameraId = cameraId;
        }

        private IProfileService profileService;
        private int _cameraId;

        public string Category { get; } = "ZWOptical";
        public string Id => $"{Name} #{_cameraId}";

        private ASICameraDll.ASI_CAMERA_INFO? _info;

        private ASICameraDll.ASI_CAMERA_INFO Info {
            // [obsolete] info is cached only while camera is open
            get {
                if (_info == null) {
                    // this needs to be called otherwise GetCameraProperties shuts down other instances of the camera
                    ASICameraDll.OpenCamera(_cameraId);
                    // at this point we might as well cache the properties anyway
                    _info = ASICameraDll.GetCameraProperties(_cameraId);
                }

                return _info.Value;
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
                        _controls.Add(new CameraControl(_cameraId, i));
                    }
                }

                return _controls;
            }
        }

        public bool CanSubSample {
            get {
                return true;
            }
        }

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

        public string Name {
            get {
                return Info.Name;
            }
        }

        public bool HasShutter {
            get {
                return Info.MechanicalShutter != ASICameraDll.ASI_BOOL.ASI_FALSE;
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                return _connected;
            }
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public bool CanShowLiveView {
            get {
                return true;
            }
        }

        public double Temperature {
            get {
                return GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TEMPERATURE) / 10.0; //ASI driver gets temperature in Celsius * 10
            }
        }

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
            get {
                return bin;
            }
            set {
                bin = value;
                RaisePropertyChanged();
            }
        }

        public short BinY {
            get {
                return bin;
            }
            set {
                bin = value;
                RaisePropertyChanged();
            }
        }

        public string Description {
            get {
                return string.Empty;
            }
        }

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

        public string SensorName {
            get {
                return string.Empty;
            }
        }

        public SensorType SensorType { get; private set; } = SensorType.Monochrome;

        public short BayerOffsetX { get; } = 0;
        public short BayerOffsetY { get; } = 0;

        public int CameraXSize {
            get {
                return Info.MaxWidth;
            }
        }

        public int CameraYSize {
            get {
                return Info.MaxHeight;
            }
        }

        public double ExposureMin {
            get {
                return (double)GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE) / 1000000;
            }
        }

        public double ExposureMax {
            get {
                return (double)GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE) / 1000000;
            }
        }

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

        public double PixelSizeX {
            get {
                return Info.PixelSize;
            }
        }

        public double PixelSizeY {
            get {
                return Info.PixelSize;
            }
        }

        private int minTemperatureSetpoint = 0;
        private int maxTemperatureSetpoint = 0;

        public bool CanSetTemperature { get; private set; }

        public int BitDepth {
            get {
                //currently ASI camera values are stretched to fit 16 bit
                return 16;
            }
        }

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

        public double CoolerPower {
            get {
                return (double)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_POWER_PERC);
            }
        }

        public bool HasDewHeater {
            get {
                return GetControlIsWritable(ASICameraDll.ASI_CONTROL_TYPE.ASI_ANTI_DEW_HEATER) == true ? true : false;
            }
        }

        public bool DewHeaterOn {
            get {
                return GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_ANTI_DEW_HEATER) == 0 ? false : true;
            }
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

        [System.Obsolete("Use async Connect")]
        public bool Connect() {
            var success = false;
            try {
                ASICameraDll.OpenCamera(_cameraId);
                ASICameraDll.InitCamera(_cameraId);
                _info = ASICameraDll.GetCameraProperties(_cameraId);
                Connected = true;
                success = true;

                var raw16 = from types in SupportedImageTypes where types == ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16 select types;
                if (raw16.Count() == 0) {
                    Notification.ShowError(Locale.Loc.Instance["LblASIOnly16BitMono"]);
                    return false;
                }
                this.CaptureAreaInfo = new CaptureAreaInfo(new Point(0, 0), this.Resolution, 1, ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16);

                Initialize();
                RaisePropertyChanged(nameof(Connected));
                RaiseAllPropertiesChanged();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }
            return success;
        }

        private List<ASICameraDll.ASI_IMG_TYPE> SupportedImageTypes {
            get { return Info.SupportedVideoFormat.TakeWhile(x => x != ASICameraDll.ASI_IMG_TYPE.ASI_IMG_END).ToList(); }
        }

        private List<int> SupportedBinFactors {
            get { return Info.SupportedBins.TakeWhile(x => x != 0).ToList(); }
        }

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

        private ASICameraDll.ASI_EXPOSURE_STATUS ExposureStatus {
            get {
                return ASICameraDll.GetExposureStatus(_cameraId);
            }
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                var status = ExposureStatus;
                while (status == ASICameraDll.ASI_EXPOSURE_STATUS.ASI_EXP_WORKING) {
                    await Task.Delay(100, token);
                    status = ExposureStatus;
                }
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            return await Task.Run<IExposureData>(async () => {
                try {
                    var status = ExposureStatus;
                    while (status == ASICameraDll.ASI_EXPOSURE_STATUS.ASI_EXP_WORKING) {
                        await Task.Delay(10, token);
                        status = ExposureStatus;
                    }

                    var width = CaptureAreaInfo.Size.Width;
                    var height = CaptureAreaInfo.Size.Height;

                    int size = width * height;
                    ushort[] arr = new ushort[size];
                    int buffersize = width * height * 2;
                    if (!GetExposureData(arr, buffersize)) {
                        throw new Exception(Locale.Loc.Instance["LblASIImageDownloadError"]);
                    }

                    var metadata = new ImageMetaData();

                    return new ImageArrayExposureData(
                        input: arr,
                        width: width,
                        height: height,
                        bitDepth: BitDepth,
                        isBayered: SensorType != SensorType.Monochrome,
                        metaData: new ImageMetaData());
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
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
                               sequence.ImageType == CaptureSequence.ImageTypes.BIAS ||
                               sequence.ImageType == CaptureSequence.ImageTypes.DARKFLAT;

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

            ASICameraDll.StartExposure(_cameraId, isDarkFrame);
        }

        public void StopExposure() {
            ASICameraDll.StopExposure(_cameraId);
        }

        private CameraControl GetControl(ASICameraDll.ASI_CONTROL_TYPE controlType) {
            return Controls.FirstOrDefault(x => x.ControlType == controlType);
        }

        public bool CanGetGain {
            get {
                return true;
            }
        }

        public bool CanSetGain {
            get {
                return true;
            }
        }

        public int Gain {
            get {
                return GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);
            }
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN, value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int GainMax {
            get {
                return GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);
            }
        }

        public int GainMin {
            get {
                return GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);
            }
        }

        public bool CanSetOffset {
            get {
                return true;
            }
        }

        public bool CanSetUSBLimit {
            get {
                return true;
            }
        }

        public int Offset {
            get {
                return GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BRIGHTNESS);
            }
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BRIGHTNESS, value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int OffsetMin {
            get {
                return GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BRIGHTNESS);
            }
        }

        public int OffsetMax {
            get {
                return GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BRIGHTNESS);
            }
        }

        public int USBLimit {
            get {
                return GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD);
            }
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD, value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public int USBLimitMin {
            get {
                return GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD);
            }
        }

        public int USBLimitMax {
            get {
                return GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_BANDWIDTHOVERLOAD);
            }
        }

        public int USBLimitStep {
            get {
                return 1;
            }
        }

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
            var control = GetControl(type);
            if (control != null && value <= control.MaxValue && value >= control.MinValue) {
                control.Value = value;
                return true;
            } else {
                Logger.Warning(string.Format("Failed to set ASI Control Value {0} with value {1}", type, value));
                return false;
            }
        }

        public IEnumerable ReadoutModes => new List<string> { "Default" };

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

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                _readoutModeForNormalImages = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public string CameraState {
            get {
                return ExposureStatus.ToString();
            }
        }

        public IList<int> Gains {
            get {
                return new List<int>();
            }
        }

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
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                var success = false;
                try {
                    ASICameraDll.OpenCamera(_cameraId);
                    ASICameraDll.InitCamera(_cameraId);
                    _info = ASICameraDll.GetCameraProperties(_cameraId);
                    Connected = true;
                    success = true;

                    var raw16 = from types in SupportedImageTypes where types == ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16 select types;
                    if (raw16.Count() == 0) {
                        Notification.ShowError("Only 16 bit Monochrome sensors supported currently");
                        return false;
                    }
                    this.CaptureAreaInfo = new CaptureAreaInfo(new Point(0, 0), this.Resolution, 1, ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16);
                    Initialize();
                    RaisePropertyChanged(nameof(Connected));
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
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

        public void StartLiveView() {
            ASICameraDll.StartVideoCapture(_cameraId);
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return Task.Run<IExposureData>(() => {
                var width = CaptureAreaInfo.Size.Width;
                var height = CaptureAreaInfo.Size.Height;

                int size = width * height;

                ushort[] arr = new ushort[size];
                int buffersize = width * height * 2;
                if (!GetVideoData(arr, buffersize)) {
                    throw new Exception(Locale.Loc.Instance["LblASIImageDownloadError"]);
                }

                return new ImageArrayExposureData(
                    input: arr,
                    width: width,
                    height: height,
                    bitDepth: this.BitDepth,
                    isBayered: this.SensorType != SensorType.Monochrome,
                    metaData: new ImageMetaData());
            });
        }

        private bool GetVideoData(ushort[] buffer, int bufferSize) {
            return ASICameraDll.GetVideoData(_cameraId, buffer, bufferSize, -1);
        }

        public void StopLiveView() {
            ASICameraDll.StopVideoCapture(_cameraId);
        }

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
            }
        }

        public bool HasBattery => false;
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

        public string Name { get { return _props.Name; } }
        public string Description { get { return _props.Description; } }
        public int MinValue { get { return _props.MinValue; } }
        public int MaxValue { get { return _props.MaxValue; } }
        public int DefaultValue { get { return _props.DefaultValue; } }
        public ASICameraDll.ASI_CONTROL_TYPE ControlType { get { return _props.ControlType; } }
        public bool IsAutoAvailable { get { return _props.IsAutoSupported != ASICameraDll.ASI_BOOL.ASI_FALSE; } }
        public bool IsWritable { get { return _props.IsWritable != ASICameraDll.ASI_BOOL.ASI_FALSE; } }

        public int Value {
            get {
                return ASICameraDll.GetControlValue(_cameraId, _props.ControlType, out var isAuto);
            }
            set {
                ASICameraDll.SetControlValue(_cameraId, _props.ControlType, value, IsAuto);
            }
        }

        public bool IsAuto {
            get {
                return _auto;
            }
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
