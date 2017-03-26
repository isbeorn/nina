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

namespace NINA.Model.MyCamera {
    public class ASICamera : BaseINPC, ICamera{
        
        public ASICamera(int cameraId) {
            _cameraId = cameraId;
        }

        private int _cameraId;

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
                var tempControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_TEMPERATURE);
                return (double)tempControl.Value / 10;
            }
        }

    public double SetCCDTemperature {
            get {
                if (CanSetCCDTemperature) {
                    var tempControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP);
                    return (double)tempControl.Value;
                } else {
                    return double.MinValue;
                }
            } 
            set {
                if(CanSetCCDTemperature) {
                    var tempControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_TARGET_TEMP);
                    tempControl.Value = (int)value;
                }                
            }
        }
        public short BinX {
            get {
                var binningControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
                return (short)binningControl.Value;
            }
            set {
                var binningControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
                binningControl.Value = value;
                RaisePropertyChanged();
            }
        }
        public short BinY {
            get {
                var binningControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
                return (short)binningControl.Value;
            }
            set {
                var binningControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
                binningControl.Value = value;
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
                //TODO
                return SensorType.Monochrome;
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
                var exposureSettings = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE);
                return (double)exposureSettings.MinValue / 1000000;
            }            
        }
        public double ExposureMax {
            get {
                var exposureSettings = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE);
                return (double)exposureSettings.MaxValue / 1000000;
            }
        }

        public short MaxBinX {
            get {
                var binningControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
                return (short)binningControl.MaxValue;
            }
        }
        public short MaxBinY {
            get {
                var binningControl = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_HARDWARE_BIN);
                return (short)binningControl.MaxValue;
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
                var control = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_ON);
                if (control != null && control.MaxValue > 0) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        public bool CoolerOn { get {
                var control = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_ON);
                return control.Value == 0 ? false : true ;
            }
            set {
                var control = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_ON);
                control.Value = value ? 1 : 0;
                RaisePropertyChanged();
            }
        }

        public double CoolerPower {
            get {
                var control = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_COOLER_POWER_PERC);
                return (double)control.Value;
            }
        }

        private AsyncObservableCollection<BinningMode> _binningModes;
        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if(_binningModes == null) {
                    _binningModes = new AsyncObservableCollection<BinningMode>();
                    foreach(int f in SupportedBinFactors) {
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
                RaisePropertyChanged("Connected");
                RaiseAllPropertiesChanged();
                Notification.ShowSuccess("Camera connected");
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
                    Copy(pointer, arr, 0, size / 2);

                    return await ImageArray.CreateInstance(arr, Resolution.Width, Resolution.Height);


                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
                return null;
            });

        }

        private void Copy(IntPtr source, ushort[] destination, int startIndex, int length) {
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
            int exposureMs = (int)exposureTime * 1000000;
            var exposureSettings = GetControl(ASICameraDll.ASI_CONTROL_TYPE.ASI_EXPOSURE);
            exposureSettings.Value = exposureMs;
            exposureSettings.IsAuto = false;

            ASICameraDll.StartExposure(_cameraId, !isLightFrame);
        }

        public void StopExposure() {
            ASICameraDll.StopExposure(_cameraId);
        }

        public void UpdateValues() {
            RaisePropertyChanged("CCDTemperature");
            RaisePropertyChanged("CoolerPower");
            RaisePropertyChanged("CoolerOn");
            RaisePropertyChanged("SetCCDTemperature");
            
        }

        private CameraControl GetControl(ASICameraDll.ASI_CONTROL_TYPE controlType) {
            return Controls.FirstOrDefault(x => x.ControlType == controlType);
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
