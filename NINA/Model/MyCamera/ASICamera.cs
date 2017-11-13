using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using System.Threading;
using ZWOptical.ASISDK;
using System.Drawing;
using System.Runtime.InteropServices;
using NINA.Utility.Notification;
using System.Collections;

namespace NINA.Model.MyCamera {
    public class ASICamera : BaseINPC, ICamera {

        public ASICamera(int cameraId) {
            _cameraId = cameraId;
            Id = cameraId.ToString();
        }

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

        public double CCDTemperature {
            get {
                return (double)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TEMPERATURE) / 10; //ASI driver gets temperature in Celsius * 10
            }
        }

        public double SetCCDTemperature {
            get {
                if (CanSetCCDTemperature) {
                    return (double)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP);
                } else {
                    return double.MinValue;
                }
            }
            set {
                if (CanSetCCDTemperature) {
                    if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP, (int)value)) {
                        RaisePropertyChanged();
                    }
                }
            }
        }
        public short BinX {
            get {
                return (short)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
            }
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN, value)) {
                    RaisePropertyChanged();
                }
            }
        }
        public short BinY {
            get {
                return (short)GetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
            }
            set {
                if (SetControlValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN, value)) {
                    RaisePropertyChanged();
                }
            }
        }

        public string Description {
            get {
                return string.Empty;
            }
        }

        public string DriverInfo {
            get {
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                return string.Empty;
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
                return (short)GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
            }
        }
        public short MaxBinY {
            get {
                return (short)GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
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

        public bool CanSetCCDTemperature {
            get {
                var val = GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_ON);
                if (val > 0) {
                    return true;
                } else {
                    return false;
                }
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
                    Notification.ShowError("Only 16 bit Monochrome sensors supported currently");
                    return false;
                }
                this.CaptureAreaInfo = new CaptureAreaInfo(this.Resolution, 1, ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16);
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
                int bin;
                ASICameraDll.ASI_IMG_TYPE imageType;
                var res = ASICameraDll.GetROIFormat(_cameraId, out bin, out imageType);
                return new CaptureAreaInfo(res, bin, imageType);
            }
            set {
                ASICameraDll.SetROIFormat(_cameraId, value.Size, value.Binning, value.ImageType);
            }
        }

        public Size Resolution {
            get {
                var info = Info;
                return new Size(info.MaxWidth, info.MaxHeight);
            }
        }

        private ASICameraDll.ExposureStatus ExposureStatus {
            get {
                return ASICameraDll.GetExposureStatus(_cameraId);
            }
        }

        public async Task<ImageArray> DownloadExposure(CancellationTokenSource tokenSource) {
            return await Task.Run<ImageArray>(async () => {
                try {
                    ASICameraDll.ExposureStatus status;
                    do {
                        await Task.Delay(100, tokenSource.Token);
                        status = ExposureStatus;
                    } while (status == ASICameraDll.ExposureStatus.ExpWorking);

                    int size = Resolution.Width * Resolution.Height * 2;
                    IntPtr pointer = Marshal.AllocHGlobal(size);
                    int buffersize = (Resolution.Width * Resolution.Height * 16 + 7) / 8;
                    GetExposureData(pointer, buffersize);

                    ushort[] arr = new ushort[size / 2];
                    CopyToUShort(pointer, arr, 0, size / 2);
                    Marshal.FreeHGlobal(pointer);
                    return await ImageArray.CreateInstance(arr, Resolution.Width, Resolution.Height, SensorType != SensorType.Monochrome);


                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
                return null;
            });

        }

        private void CopyToUShort(IntPtr source, ushort[] destination, int startIndex, int length) {
            unsafe
            {
                var sourcePtr = (ushort*)source;
                for (int i = startIndex; i < startIndex + length; ++i) {
                    destination[i] = *sourcePtr++;
                }
            }
        }

        private bool GetExposureData(IntPtr buffer, int bufferSize) {
            return ASICameraDll.GetDataAfterExp(_cameraId, buffer, bufferSize);
        }

        public void SetBinning(short x, short y) {
            var binningControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
            binningControl.IsAuto = false;
            binningControl.Value = x;

        }

        public void StartExposure(double exposureTime, bool isLightFrame) {
            int exposureMs = (int)(exposureTime * 1000000);
            var exposureSettings = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE);
            exposureSettings.Value = exposureMs;
            exposureSettings.IsAuto = false;

            ASICameraDll.StartExposure(_cameraId, !isLightFrame);
        }

        public void StopExposure() {
            ASICameraDll.StopExposure(_cameraId);
        }

        public void UpdateValues() {
            RaisePropertyChanged(nameof(CCDTemperature));
            RaisePropertyChanged(nameof(CoolerPower));
            RaisePropertyChanged(nameof(CoolerOn));
            RaisePropertyChanged(nameof(SetCCDTemperature));
            RaisePropertyChanged(nameof(CameraState));
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
            return control.Value;
        }

        private int GetControlMaxValue(ASICameraDll.ASI_CONTROL_TYPE type) {
            var control = GetControl(type);
            return control.MaxValue;
        }

        private int GetControlMinValue(ASICameraDll.ASI_CONTROL_TYPE type) {
            var control = GetControl(type);
            return control.MinValue;
        }

        private bool SetControlValue(ASICameraDll.ASI_CONTROL_TYPE type, int value) {
            var control = GetControl(type);
            if (value <= control.MaxValue && value >= control.MinValue) {
                control.Value = value;
                return true;
            } else {
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
                    this.CaptureAreaInfo = new CaptureAreaInfo(this.Resolution, 1, ASICameraDll.ASI_IMG_TYPE.ASI_IMG_RAW16);
                    RaisePropertyChanged(nameof(Connected));
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
                return success;
            });
        }
    }


    public class CaptureAreaInfo {
        public Size Size { get; set; }
        public int Binning { get; set; }
        public ASICameraDll.ASI_IMG_TYPE ImageType { get; set; }

        public CaptureAreaInfo(Size size, int binning, ASICameraDll.ASI_IMG_TYPE imageType) {
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
                bool isAuto;
                return ASICameraDll.GetControlValue(_cameraId, _props.ControlType, out isAuto);
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
            bool isAuto;
            ASICameraDll.GetControlValue(_cameraId, _props.ControlType, out isAuto);
            return isAuto;
        }
    }
}
