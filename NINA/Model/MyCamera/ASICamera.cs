using ASCOM.DeviceInterface;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ZWOptical.ASISDK;

namespace NINA.Model.MyCamera {

    public class ASICamera : BaseINPC, ICamera {

        public ASICamera(int cameraId, IProfileService profileService) {
            this.profileService = profileService;
            _cameraId = cameraId;
            Id = cameraId.ToString();
        }

        private IProfileService profileService;
        private int _cameraId;

        private string _id;

        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private ASICameraDll.ASI_CAMERA_INFO? _info;

        private ASICameraDll.ASI_CAMERA_INFO Info {
            // info is cached only while camera is open
            get {
                return _info ?? ASICameraDll.GetCameraProperties(_cameraId);
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
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

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
                    return (double)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP);
                } else {
                    return double.MinValue;
                }
            }
            set {
                if (CanSetTemperature) {
                    if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP, (int)value)) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private short bin = 1;

        public short BinX {
            get {
                return bin;
                /*return (short)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);*/
            }
            set {
                bin = value;
                RaisePropertyChanged();
                /*if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN, value)) {
                    RaisePropertyChanged();
                }*/
            }
        }

        public short BinY {
            get {
                return bin;
                //return (short)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
            }
            set {
                bin = value;
                RaisePropertyChanged();
                /*if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN, value)) {
                    RaisePropertyChanged();
                }*/
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

        public SensorType SensorType {
            get {
                if (Info.IsColorCam == ASICameraDll.ASI_BOOL.ASI_TRUE) {
                    return SensorType.RGGB;
                } else {
                    return SensorType.Monochrome;
                }
            }
        }

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

        public bool CanSetTemperature {
            get {
                var val = GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_ON);
                if (val > 0) {
                    return true;
                } else {
                    return false;
                }
            }
        }

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
                RaisePropertyChanged(nameof(Connected));
                RaiseAllPropertiesChanged();
            } catch (Exception ex) {
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
                ASICameraDll.SetStartPos(_cameraId, value.Start);
                ASICameraDll.SetROIFormat(_cameraId, value.Size, value.Binning, value.ImageType);
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

        public async Task<ImageArray> DownloadExposure(CancellationToken token) {
            return await Task.Run<ImageArray>(async () => {
                try {
                    var status = ExposureStatus;
                    while (status == ASICameraDll.ASI_EXPOSURE_STATUS.ASI_EXP_WORKING) {
                        await Task.Delay(10, token);
                        status = ExposureStatus;
                    }

                    var width = CaptureAreaInfo.Size.Width;
                    var height = CaptureAreaInfo.Size.Height;

                    int size = width * height * 2;
                    var pointer = Marshal.AllocHGlobal(size);
                    int buffersize = (width * height * 16 + 7) / 8;
                    if (GetExposureData(pointer, buffersize)) {
                        ushort[] arr = CopyToUShort(pointer, size / 2);
                        Marshal.FreeHGlobal(pointer);
                        return await ImageArray.CreateInstance(arr, width, height, BitDepth, SensorType != SensorType.Monochrome, true, profileService.ActiveProfile.ImageSettings.HistogramResolution);
                    } else {
                        Notification.ShowError(Locale.Loc.Instance["LblASIImageDownloadError"]);
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return null;
            });
        }

        private ushort[] CopyToUShort(IntPtr source, int length) {
            var destination = new ushort[length];
            unsafe {
                var sourcePtr = (ushort*)source;
                for (int i = 0; i < length; ++i) {
                    destination[i] = *sourcePtr++;
                }
            }
            return destination;
        }

        private bool GetExposureData(IntPtr buffer, int bufferSize) {
            return ASICameraDll.GetDataAfterExp(_cameraId, buffer, bufferSize);
        }

        public void SetBinning(short x, short y) {
            /*var binningControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
            binningControl.IsAuto = false;
            binningControl.Value = x;*/
            BinX = x;
            BinY = y;
        }

        public void StartExposure(double exposureTime, bool isLightFrame) {
            int exposureMs = (int)(exposureTime * 1000000);
            var exposureSettings = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE);
            exposureSettings.Value = exposureMs;
            exposureSettings.IsAuto = false;

            if (EnableSubSample) {
                this.CaptureAreaInfo = new CaptureAreaInfo(new Point(SubSampleX, SubSampleY), new Size(SubSampleWidth / BinX, SubSampleHeight / BinY), BinX, ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16);
            } else {
                this.CaptureAreaInfo = new CaptureAreaInfo(new Point(0, 0), new Size(this.Resolution.Width / BinX, this.Resolution.Height / BinY), BinX, ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16);
            }

            ASICameraDll.StartExposure(_cameraId, !isLightFrame);
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

        public short Gain {
            get {
                return (short)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);
            }
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN, value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public short GainMax {
            get {
                return (short)GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);
            }
        }

        public short GainMin {
            get {
                return (short)GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_GAIN);
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

        public ArrayList Gains {
            get {
                return new ArrayList();
            }
        }

        public void SetupDialog() {
        }

        public void Initialize() {
            throw new NotImplementedException();
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
                    RaisePropertyChanged(nameof(Connected));
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return success;
            });
        }

        public void StartLiveView() {
            ASICameraDll.StartVideoCapture(_cameraId);
        }

        public async Task<ImageArray> DownloadLiveView(CancellationToken token) {
            var width = CaptureAreaInfo.Size.Width;
            var height = CaptureAreaInfo.Size.Height;

            int size = width * height * 2;
            IntPtr pointer = Marshal.AllocHGlobal(size);
            int buffersize = (width * height * 16 + 7) / 8;
            ASICameraDll.GetVideoData(_cameraId, pointer, buffersize, -1);

            ushort[] arr = CopyToUShort(pointer, size / 2);
            Marshal.FreeHGlobal(pointer);
            return await ImageArray.CreateInstance(arr, width, height, BitDepth, SensorType != SensorType.Monochrome, true, profileService.ActiveProfile.ImageSettings.HistogramResolution);
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
                // todo: start liveview if possible
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
        public bool Writeable { get { return _props.IsWritable != ASICameraDll.ASI_BOOL.ASI_FALSE; } }

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