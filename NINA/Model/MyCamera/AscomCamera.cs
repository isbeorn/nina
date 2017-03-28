using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {
    class AscomCamera : BaseINPC, ICamera, IDisposable {
        public AscomCamera(string cameraId, string name)  {            
            Id = cameraId;
            Name = name;
        }

        private string _id;
        public string Id {
            get {
                return _id;
            } set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private Camera _camera;

        private void init() {
            _hasBayerOffset = true;
            _hasCCDTemperature = true;
            _hasCooler = true;
            CanSetGain = true;
            CanGetGain = true;
            _canGetGainMinMax = true;
        _hasLastExposureInfo = true;
            _hasPercentCompleted = true;
            BinningModes = new AsyncObservableCollection<BinningMode>();
            for (short i = 1; i <= MaxBinX; i++) {
                if (CanAsymmetricBin) {
                    for (short j = 1; j <= MaxBinY; j++) {
                        BinningModes.Add(new BinningMode(i, j));
                    }
                } else {
                    BinningModes.Add(new BinningMode(i, i));
                }


            }
        }

        private bool _hasBayerOffset;
        public short BayerOffsetX {
            get {
                short offset = -1;
                try {
                    if (Connected && _hasBayerOffset) {
                        offset = _camera.BayerOffsetX;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasBayerOffset = false;
                }
                return offset;
            }
        }
        public short BayerOffsetY {
            get {
                short offset = -1;
                try {
                    if (Connected && _hasBayerOffset) {
                        offset = _camera.BayerOffsetY;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasBayerOffset = false;
                }
                return offset;
            }
        }

        public short BinX {
            get {
                if(Connected) {
                    return _camera.BinX;
                } else {
                    return -1;
                }                
            }
            set {
                if(Connected) { 
                    try {
                        _camera.BinX = value;
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }
        public short BinY {
            get {
                if (Connected) {
                    return _camera.BinY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    try {
                        _camera.BinY = value;
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }


        public string CameraState {
            get {
                string state;
                try {
                    if (Connected) {
                        state = _camera.CameraState.ToString();
                    } else {
                        state = CameraStates.cameraIdle.ToString();
                    }
                } catch (NotConnectedException ex) {
                    Notification.ShowError(ex.Message);
                    state = CameraStates.cameraError.ToString();
                }
                return state;
            }
        }

        public int CameraXSize {
            get {
                int size = -1;
                if (Connected) {
                    size = _camera.CameraXSize;
                }
                return size;
            }
        }
        public int CameraYSize {
            get {
                int size = -1;
                if (Connected) {
                    size = _camera.CameraYSize;
                }
                return size;
            }
        }

        public bool CanAbortExposure {
            get {
                if(Connected) {
                    return _camera.CanAbortExposure;
                } else {
                    return false;
                }
            }
        }
        public bool CanAsymmetricBin {
            get {
                if (Connected) {
                    return _camera.CanAsymmetricBin;
                } else {
                    return false;
                }
            }
        }
        public bool CanFastReadout {
            get {
                if (Connected) {
                    return _camera.CanFastReadout;
                } else {
                    return false;
                }
            }
        }
        public bool CanGetCoolerPower {
            get {
                if (Connected) {
                    return _camera.CanGetCoolerPower;
                } else {
                    return false;
                }
            }
        }
        public bool CanPulseGuide {
            get {
                if (Connected) {
                    return _camera.CanPulseGuide;
                } else {
                    return false;
                }
            }
        }
        public bool CanSetCCDTemperature {
            get {
                if (Connected) {
                    return _camera.CanSetCCDTemperature;
                } else {
                    return false;
                }
            }
        }
        public bool CanStopExposure {
            get {
                if (Connected) {
                    return _camera.CanStopExposure;
                } else {
                    return false;
                }
            }
        }

        private bool _hasCCDTemperature;
        public double CCDTemperature {
            get {
                double val = -1;
                try {
                    if(Connected && _hasCCDTemperature) {
                        val = _camera.CCDTemperature;
                    }                    
                } catch(InvalidValueException) {
                    _hasCCDTemperature = false;
                }
                return val;
            }
        }

        private bool _connected;
        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _camera.Connected;
                        if (_connected != val) {
                            Notification.ShowWarning("Camera connection lost! Please reconnect camera!");
                            Disconnect();
                        }
                    } catch (Exception) {
                        Disconnect();
                    }
                    return val;

                } else {
                    return false;
                }
            }
            private set {
                try {
                    _connected = value;
                    _camera.Connected = value;

                } catch (Exception ex) {
                    Notification.ShowError(ex.Message + "\n Please reconnect camera!");
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }
        
        private bool _hasCooler;
        public bool CoolerOn {
            get {
                bool val = false;
                try {
                    if (Connected && _hasCooler) {
                        val = _camera.CoolerOn;
                    }
                } catch (Exception) {
                    _hasCooler = false;
                }
                return val;
            }
            set {
                try {
                    if(Connected && _hasCooler) { 
                        _camera.CoolerOn = value;
                        RaisePropertyChanged();
                    }
                } catch(Exception) {
                    _hasCooler = false;
                }                
            }
        }
                
        public double CoolerPower {
            get {                
                if(Connected && CanGetCoolerPower) {
                    return _camera.CoolerPower;
                } else {
                    return -1;
                }
            }
        }
        
        public string Description {
            get {
                string val = string.Empty;
                if (Connected) {
                    try { 
                        val = _camera.Description;
                    } catch(DriverException) {                        
                    }
                } 
                return val;
            }
        }

        public string DriverInfo {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = _camera.DriverInfo;
                    } catch (DriverException) {
                    }
                }
                return val;
            }
        }

        public string DriverVersion {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = _camera.DriverVersion;
                    } catch (DriverException) {
                    }
                }
                return val;
            }
        }

        public double ElectronsPerADU {
            get {
                double val = -1;
                if (Connected) {
                    val = _camera.ElectronsPerADU;
                }
                return val;
            }
        }

        public double ExposureMax {
            get {
                double val = -1;
                if (Connected) {
                    try { 
                    val = _camera.ExposureMax;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public double ExposureMin {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = _camera.ExposureMin;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public double ExposureResolution {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = _camera.ExposureResolution;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public bool FastReadout {
            get {
                bool val = false;
                if (Connected && CanFastReadout) {                    
                    val = _camera.FastReadout;
                }
                return val;
            }
            set {
                if (Connected && CanFastReadout) {
                    _camera.FastReadout = value;
                }
            }
        }

        public double FullWellCapacity {
            get {
                double val = -1;
                if (Connected) {
                    val = _camera.FullWellCapacity;
                }
                return val;
            }
        }

        private bool _canGetGain;
        public bool CanGetGain {
            get {
                return _canGetGain;
            }
            set {
                _canGetGain = value;
                RaisePropertyChanged();
            }
        }
        private bool _canSetGain;
        public bool CanSetGain {
            get {
                return _canSetGain;
            }
            set { _canSetGain = value;
                RaisePropertyChanged();
            }
        }
        public short Gain {
            get {
                short val = -1;
                if (Connected && CanGetGain) {
                    try { 
                        val = _camera.Gain;
                    } catch(PropertyNotImplementedException) {
                        CanGetGain = false;
                    }
                }
                return val;
            }
            set {
                if(Connected && CanSetGain) {
                    try { 
                        _camera.Gain = value;
                    } catch (PropertyNotImplementedException) {
                        CanSetGain = false;
                    } catch (InvalidValueException ex) {
                        Notification.ShowWarning(ex.Message);
                    } catch(Exception) {
                        CanSetGain = false;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private bool _canGetGainMinMax;
        public short GainMax {
            get {
                short val = -1;
                if (Connected && _canGetGainMinMax) {
                    try { 
                        val = _camera.GainMax;
                    } catch(PropertyNotImplementedException) {
                        _canGetGainMinMax = false;
                    }
                }
                return val;
            }
        }        
        public short GainMin {
            get {
                short val = -1;
                if (Connected && _canGetGainMinMax) {
                    try {
                        val = _camera.GainMin;
                    } catch (PropertyNotImplementedException) {
                        _canGetGainMinMax = false;
                    }
                }
                return val;
            }
        }
        public ArrayList Gains {
            get {
                ArrayList val = new ArrayList();
                if (Connected && CanGetGain) {
                    try {
                        val = _camera.Gains;
                    } catch (PropertyNotImplementedException) {
                        CanGetGain = false;
                    }
                }
                return val;
            }
        }

        public bool HasShutter {
            get {
                if(Connected) {
                    return _camera.HasShutter;
                } else {
                    return false;
                }
            }
        }

        public double HeatSinkTemperature {
            get {
                if (Connected) {
                    return _camera.HeatSinkTemperature;
                } else {
                    return double.MinValue;
                }
            }
        }

        public object ImageArray {
            get {
                if (Connected) {
                    return _camera.ImageArray;
                } else {
                    return null;
                }
            }
        }
        public object ImageArrayVariant {
            get {
                if (Connected) {
                    return _camera.ImageArrayVariant;
                } else {
                    return null;
                }
            }
        }
        public bool ImageReady {
            get {
                if(Connected) {
                    return _camera.ImageReady;
                } else {
                    return false;
                }
            }
        }

        public short InterfaceVersion {
            get {
                short val = -1;
                try {
                    val = _camera.InterfaceVersion;
                }catch(DriverException) {                    
                }
                return val;                
            }
        }
        public bool IsPulseGuiding {
            get {
                if (Connected) {
                    return _camera.IsPulseGuiding;
                } else {
                    return false;
                }
            }
        }

        private bool _hasLastExposureInfo;
        public double LastExposureDuration {
            get {
                double val = -1;
                try {
                    if (Connected && _hasLastExposureInfo) {
                        val = _camera.LastExposureDuration;
                    }
                } catch(ASCOM.InvalidOperationException) {

                } catch(PropertyNotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }
        public string LastExposureStartTime {
            get {
                string val = string.Empty;
                try {
                    if (Connected && _hasLastExposureInfo) {
                        val = _camera.LastExposureStartTime;
                    }
                } catch (ASCOM.InvalidOperationException) {

                } catch (PropertyNotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }

        public int MaxADU {
            get {
                if(Connected) {
                    return _camera.MaxADU;
                } else {
                    return -1;
                }
            }
        }
        public short MaxBinX {
            get {
                if (Connected) {
                    return _camera.MaxBinX;
                } else {
                    return -1;
                }
            }
        }
        public short MaxBinY {
            get {
                if (Connected) {
                    return _camera.MaxBinY;
                } else {
                    return -1;
                }
            }
        }

        private string _name;
        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public int NumX {
            get {
                if (Connected) {
                    return _camera.NumX;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    _camera.NumX = value;
                    RaisePropertyChanged();
                }
            }
        }
        public int NumY {
            get {
                if (Connected) {
                    return _camera.NumY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    _camera.NumY = value;
                    RaisePropertyChanged();
                }                
            }
        }

        private bool _hasPercentCompleted;
        public short PercentCompleted {
            get {
                short val = -1;
                try {
                    if(_hasPercentCompleted) {
                        val = _camera.PercentCompleted;
                    }                    
                } catch(ASCOM.InvalidOperationException) {

                } catch(PropertyNotImplementedException) {
                    _hasPercentCompleted = false;
                }
                return val;
            }
        }
        
        public double PixelSizeX {
            get {
                if(Connected) {
                    return _camera.PixelSizeX;
                } else {
                    return -1;
                }
            }
        }
        public double PixelSizeY {
            get {
                if (Connected) {
                    return _camera.PixelSizeY;
                } else {
                    return -1;
                }
            }
        }

        public short ReadoutMode {
            get {
                if(Connected) {
                    return _camera.ReadoutMode;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    _camera.ReadoutMode = value;
                } catch(InvalidValueException ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }
        public ArrayList ReadoutModes {
            get {
                ArrayList val = new ArrayList();
                if(Connected && !CanFastReadout) {
                    val =_camera.ReadoutModes;
                }
                return val;
            }
        }

        public string SensorName {
            get {
                string val = string.Empty;
                if(Connected) {
                    try {
                        val = _camera.SensorName;                        
                    } catch(PropertyNotImplementedException) {

                    }                    
                }
                return val;
            }
        }

        public SensorType SensorType {
            get {
                if (Connected) {
                    return _camera.SensorType;
                } else {
                    return SensorType.Monochrome;
                }
            }
        }

        public double SetCCDTemperature {
            get {
                double val = double.MinValue;
                if(Connected && CanSetCCDTemperature) {
                    val = _camera.SetCCDTemperature;
                }
                return val;
            }
            set {
                if(Connected && CanSetCCDTemperature) { 
                    try {
                        _camera.SetCCDTemperature = value;
                        RaisePropertyChanged();
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                }
            }
        }

        public int StartX {
            get {
                if (Connected) {
                    return _camera.StartX;
                } else {
                    return -1;
                }
            } 
            set {
                if(Connected) {
                    _camera.StartX = value;
                    RaisePropertyChanged();
                }
            }
        }
        public int StartY {
            get {
                if (Connected) {
                    return _camera.StartY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    _camera.StartY = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ArrayList SupportedActions {
            get {
                ArrayList val = new ArrayList();
                try {
                    val = _camera.SupportedActions;
                } catch(DriverException) {

                }
                return val;
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; private set; }

        public bool Connect() {
            try {
                _camera = new Camera(Id);
                Connected = true;
                if(Connected) { 
                    init();
                    RaiseAllPropertiesChanged();
                    Notification.ShowSuccess("Camera connected");
                }
            } catch (ASCOM.DriverAccessCOMException ex) {
                Notification.ShowError(ex.Message);
            } catch (Exception ex) {
                Notification.ShowError("Unable to connect to camera " + ex.Message);
            }
            return Connected;
        }

        public void Disconnect() {            
            Connected = false;
            _camera.Dispose();
        }


        public async Task<ImageArray> DownloadExposure(CancellationTokenSource tokenSource) {
            return await Task<ImageArray>.Run(async () => {
                try {
                    ASCOM.Utilities.Util U = new ASCOM.Utilities.Util();
                    while (!ImageReady && Connected) {
                        //Console.Write(".");
                        U.WaitForMilliseconds(10);
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }

                    Array arr;
                    if (ImageArray.GetType() == typeof(Int32[,])) {
                        arr = (Int32[,])ImageArray;
                        return await MyCamera.ImageArray.CreateInstance(arr);
                        
                    } else {
                        arr = (Int32[,,])ImageArray;
                        return await MyCamera.ImageArray.CreateInstance(arr);
                    }
                } catch (OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                } catch {

                }
                return null;

            });
        }

        public void StartExposure(double exposureTime, bool isLightFrame) {
            _camera.StartExposure(exposureTime, isLightFrame);
        }

        public void StopExposure() {
            if (CanStopExposure) {
                try {
                    StopExposure();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }

            }
        }

        public void AbortExposure() {
            if (CanAbortExposure) {
                try {
                    _camera.AbortExposure();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }

            }
        }

        public void UpdateValues() {
            RaisePropertyChanged("CCDTemperature");
            RaisePropertyChanged("FullWellCapacity");
            RaisePropertyChanged("HeatSinkTemperature");
            RaisePropertyChanged("CoolerPower");
            RaisePropertyChanged("IsPulseGuiding");
            
        }

        public void SetBinning(short x, short y) {
            BinX = x;
            BinY = y;
            NumX = CameraXSize / x;
            NumY = CameraYSize / y;
        }

        public void Dispose() {
            _camera.Dispose();
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public bool CanSetOffset {
            get {
                return false;
            }
        }
        public bool CanSetUSBLimit {
            get {
                return false;
            }
        }

        public void SetupDialog() {
            if(HasSetupDialog) {
                try {               
                _camera = new Camera(Id);
                _camera.SetupDialog();
                _camera.Dispose();
                _camera = null;
                } catch(Exception ex) {
                    Notification.ShowError(ex.Message);
                }
            }            
        }
    }
}
