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
    class AscomCamera : Camera, INotifyPropertyChanged, ICamera {
        public AscomCamera(string cameraId) : base(cameraId) {
        }

        private void init() {
            _hasBayerOffset = true;
            _hasCCDTemperature = true;
            _hasCooler = true;
            _canSetGain = true;
            _canGetGain = true;
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
        public new short BayerOffsetX {
            get {
                short offset = -1;
                try {
                    if (Connected && _hasBayerOffset) {
                        offset = base.BayerOffsetX;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasBayerOffset = false;
                }
                return offset;
            }
        }
        public new short BayerOffsetY {
            get {
                short offset = -1;
                try {
                    if (Connected && _hasBayerOffset) {
                        offset = base.BayerOffsetY;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasBayerOffset = false;
                }
                return offset;
            }
        }

        public new short BinX {
            get {
                if(Connected) {
                    return base.BinX;
                } else {
                    return -1;
                }                
            }
            set {
                if(Connected) { 
                    try {
                        base.BinX = value;
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }
        public new short BinY {
            get {
                if (Connected) {
                    return base.BinY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    try {
                        base.BinY = value;
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }


        public new CameraStates CameraState {
            get {
                CameraStates state;
                try {
                    if (Connected) {
                        state = base.CameraState;
                    } else {
                        state = CameraStates.cameraIdle;
                    }
                } catch (NotConnectedException ex) {
                    Notification.ShowError(ex.Message);
                    state = CameraStates.cameraError;
                }
                return state;
            }
        }

        public new int CameraXSize {
            get {
                int size = -1;
                if (Connected) {
                    size = base.CameraXSize;
                }
                return size;
            }
        }
        public new int CameraYSize {
            get {
                int size = -1;
                if (Connected) {
                    size = base.CameraYSize;
                }
                return size;
            }
        }

        public new bool CanAbortExposure {
            get {
                if(Connected) {
                    return base.CanAbortExposure;
                } else {
                    return false;
                }
            }
        }
        public new bool CanAsymmetricBin {
            get {
                if (Connected) {
                    return base.CanAsymmetricBin;
                } else {
                    return false;
                }
            }
        }
        public new bool CanFastReadout {
            get {
                if (Connected) {
                    return base.CanFastReadout;
                } else {
                    return false;
                }
            }
        }
        public new bool CanGetCoolerPower {
            get {
                if (Connected) {
                    return base.CanGetCoolerPower;
                } else {
                    return false;
                }
            }
        }
        public new bool CanPulseGuide {
            get {
                if (Connected) {
                    return base.CanPulseGuide;
                } else {
                    return false;
                }
            }
        }
        public new bool CanSetCCDTemperature {
            get {
                if (Connected) {
                    return base.CanSetCCDTemperature;
                } else {
                    return false;
                }
            }
        }
        public new bool CanStopExposure {
            get {
                if (Connected) {
                    return base.CanStopExposure;
                } else {
                    return false;
                }
            }
        }

        private bool _hasCCDTemperature;
        public new double CCDTemperature {
            get {
                double val = -1;
                try {
                    if(Connected && _hasCCDTemperature) {
                        val = base.CCDTemperature;
                    }                    
                } catch(InvalidValueException) {
                    _hasCCDTemperature = false;
                }
                return val;
            }
        }

        private bool _connected;
        public new bool Connected {
            get {
                bool val = false;
                try {
                    val = base.Connected;
                    if (_connected != val) {
                        Notification.ShowWarning("Camera connection lost! Please reconnect camera!");
                        Disconnect();
                    }
                } catch (Exception) {
                    if (_connected) {
                        Disconnect();
                    }
                }
                return val;
            }
            private set {
                try {
                    _connected = value;
                    base.Connected = value;

                } catch (Exception ex) {
                    Notification.ShowError(ex.Message + "\n Please reconnect camera!");
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }
        
        private bool _hasCooler;
        public new bool CoolerOn {
            get {
                bool val = false;
                try {
                    if (Connected && _hasCooler) {
                        val = base.CoolerOn;
                    }
                } catch (Exception) {
                    _hasCooler = false;
                }
                return val;
            }
            set {
                try {
                    if(Connected && _hasCooler) { 
                        base.CoolerOn = value;
                        RaisePropertyChanged();
                    }
                } catch(Exception) {
                    _hasCooler = false;
                }                
            }
        }
                
        public new double CoolerPower {
            get {                
                if(Connected && CanGetCoolerPower) {
                    return base.CoolerPower;
                } else {
                    return -1;
                }
            }
        }
        
        public new string Description {
            get {
                string val = string.Empty;
                if (Connected) {
                    try { 
                        val = base.Description;
                    } catch(DriverException) {                        
                    }
                } 
                return val;
            }
        }

        public new string DriverInfo {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = base.DriverInfo;
                    } catch (DriverException) {
                    }
                }
                return val;
            }
        }

        public new string DriverVersion {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = base.DriverVersion;
                    } catch (DriverException) {
                    }
                }
                return val;
            }
        }

        public new double ElectronsPerADU {
            get {
                double val = -1;
                if (Connected) {
                    val = base.ElectronsPerADU;
                }
                return val;
            }
        }

        public new double ExposureMax {
            get {
                double val = -1;
                if (Connected) {
                    try { 
                    val = base.ExposureMax;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public new double ExposureMin {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = base.ExposureMin;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public new double ExposureResolution {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = base.ExposureResolution;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public new bool FastReadout {
            get {
                bool val = false;
                if (Connected && CanFastReadout) {                    
                    val = base.FastReadout;
                }
                return val;
            }
            set {
                if (Connected && CanFastReadout) {
                    base.FastReadout = value;
                }
            }
        }

        public new double FullWellCapacity {
            get {
                double val = -1;
                if (Connected) {
                    val = base.FullWellCapacity;
                }
                return val;
            }
        }

        private bool _canSetGain;
        public new short Gain {
            get {
                short val = -1;
                if (Connected && _canSetGain) {
                    val = base.Gain;
                }
                return val;
            }
            set {
                if(Connected && _canSetGain) {
                    try { 
                        base.Gain = value;
                    } catch (PropertyNotImplementedException) {
                        _canSetGain = false;
                    } catch (InvalidValueException ex) {
                        Notification.ShowWarning(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private bool _canGetGain;
        public new short GainMax {
            get {
                short val = -1;
                if (Connected && _canGetGain) {
                    try { 
                        val = base.GainMax;
                    } catch(PropertyNotImplementedException) {
                        _canGetGain = false;
                    }
                }
                return val;
            }
        }        
        public new short GainMin {
            get {
                short val = -1;
                if (Connected && _canGetGain) {
                    try {
                        val = base.GainMin;
                    } catch (PropertyNotImplementedException) {
                        _canGetGain = false;
                    }
                }
                return val;
            }
        }
        public new ArrayList Gains {
            get {
                ArrayList val = new ArrayList();
                if (Connected && _canGetGain) {
                    try {
                        val = base.Gains;
                    } catch (PropertyNotImplementedException) {
                        _canGetGain = false;
                    }
                }
                return val;
            }
        }

        public new bool HasShutter {
            get {
                if(Connected) {
                    return base.HasShutter;
                } else {
                    return false;
                }
            }
        }

        public new double HeatSinkTemperature {
            get {
                if (Connected) {
                    return base.HeatSinkTemperature;
                } else {
                    return double.MinValue;
                }
            }
        }

        public new object ImageArray {
            get {
                if (Connected) {
                    return base.ImageArray;
                } else {
                    return null;
                }
            }
        }
        public new object ImageArrayVariant {
            get {
                if (Connected) {
                    return base.ImageArrayVariant;
                } else {
                    return null;
                }
            }
        }
        public new bool ImageReady {
            get {
                if(Connected) {
                    return base.ImageReady;
                } else {
                    return false;
                }
            }
        }

        public new short InterfaceVersion {
            get {
                short val = -1;
                try {
                    val = base.InterfaceVersion;
                }catch(DriverException) {                    
                }
                return val;                
            }
        }
        public new bool IsPulseGuiding {
            get {
                if (Connected) {
                    return base.IsPulseGuiding;
                } else {
                    return false;
                }
            }
        }

        private bool _hasLastExposureInfo;
        public new double LastExposureDuration {
            get {
                double val = -1;
                try {
                    val = base.LastExposureDuration;
                } catch(ASCOM.InvalidOperationException) {

                } catch(PropertyNotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }
        public new string LastExposureStartTime {
            get {
                string val = string.Empty;
                try {
                    val = base.LastExposureStartTime;
                } catch (ASCOM.InvalidOperationException) {

                } catch (PropertyNotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }

        public new int MaxADU {
            get {
                if(Connected) {
                    return base.MaxADU;
                } else {
                    return -1;
                }
            }
        }
        public new short MaxBinX {
            get {
                if (Connected) {
                    return base.MaxBinX;
                } else {
                    return -1;
                }
            }
        }
        public new short MaxBinY {
            get {
                if (Connected) {
                    return base.MaxBinY;
                } else {
                    return -1;
                }
            }
        }

        public new string Name {
            get {
                string val = string.Empty;
                try {
                    if(Connected) {
                        val = base.Name;
                    }                    
                } catch(DriverException) {
                }
                return val;
            }
        }

        public new int NumX {
            get {
                if (Connected) {
                    return base.NumX;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    base.NumX = value;
                    RaisePropertyChanged();
                }
            }
        }
        public new int NumY {
            get {
                if (Connected) {
                    return base.NumY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    base.NumY = value;
                    RaisePropertyChanged();
                }                
            }
        }

        private bool _hasPercentCompleted;
        public new short PercentCompleted {
            get {
                short val = -1;
                try {
                    if(_hasPercentCompleted) {
                        val = base.PercentCompleted;
                    }                    
                } catch(ASCOM.InvalidOperationException) {

                } catch(PropertyNotImplementedException) {
                    _hasPercentCompleted = false;
                }
                return val;
            }
        }
        
        public new double PixelSizeX {
            get {
                if(Connected) {
                    return base.PixelSizeX;
                } else {
                    return -1;
                }
            }
        }
        public new double PixelSizeY {
            get {
                if (Connected) {
                    return base.PixelSizeY;
                } else {
                    return -1;
                }
            }
        }

        public new short ReadoutMode {
            get {
                if(Connected) {
                    return base.ReadoutMode;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    base.ReadoutMode = value;
                } catch(InvalidValueException ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }
        public new ArrayList ReadoutModes {
            get {
                ArrayList val = new ArrayList();
                if(Connected && !CanFastReadout) {
                    val =base.ReadoutModes;
                }
                return val;
            }
        }

        public new string SensorName {
            get {
                if(Connected) {
                    return base.SensorName;
                } else {
                    return string.Empty;
                }
            }
        }

        public new SensorType SensorType {
            get {
                if (Connected) {
                    return base.SensorType;
                } else {
                    return SensorType.Monochrome;
                }
            }
        }

        public new double SetCCDTemperature {
            get {
                double val = double.MinValue;
                if(Connected && CanSetCCDTemperature) {
                    val = base.SetCCDTemperature;
                }
                return val;
            }
            set {
                if(Connected && CanSetCCDTemperature) { 
                    try {
                        base.SetCCDTemperature = value;
                        RaisePropertyChanged();
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                }
            }
        }

        public new int StartX {
            get {
                if (Connected) {
                    return base.StartX;
                } else {
                    return -1;
                }
            } 
            set {
                if(Connected) {
                    base.StartX = value;
                    RaisePropertyChanged();
                }
            }
        }
        public new int StartY {
            get {
                if (Connected) {
                    return base.StartY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    base.StartY = value;
                    RaisePropertyChanged();
                }
            }
        }

        public new ArrayList SupportedActions {
            get {
                ArrayList val = new ArrayList();
                try {
                    val = base.SupportedActions;
                } catch(DriverException) {

                }
                return val;
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; private set; }

        public bool Connect() {
            try {              
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
            this.Dispose();
        }


        public async Task<Array> DownloadExposure(CancellationTokenSource tokenSource) {
            return await Task<Array>.Run(() => {
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
                        return arr;
                    } else {
                        arr = (Int32[,,])ImageArray;
                        return (Int32[,,])ImageArray;
                    }
                } catch (OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                } catch {

                }
                return null;

            });
        }

        public new void StartExposure(double exposureTime, bool isLightFrame) {
            base.StartExposure(exposureTime, isLightFrame);
        }

        public new void StopExposure() {
            if (CanStopExposure) {
                try {
                    StopExposure();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }

            }
        }

        public new void AbortExposure() {
            if (CanAbortExposure) {
                try {
                    base.AbortExposure();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            var handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }



        protected void RaiseAllPropertiesChanged() {
            var handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(null));
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
    }
}
