using ASCOM.DriverAccess;
using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Model.MyCamera
{
    public class AscomCameraModel :  BaseINPC, ICamera {

        public AscomCameraModel() {
            init();
        }

        private void init() {
            //CameraStateString = "disconnected";
            //ProgId = string.Empty;

            AscomCamera = null;

            CanAbortExposure = false;
            CanAsymmetricBin = false;
            CanFastReadout = false;
            CanGetCoolerPower = false;
            CanPulseGuide = false;
            CanCoolerOn = false;
            CanStopExposure = false;
            CanSetCCDTemperature = false;

            BayerOffsetX = -1;
            BayerOffsetY = -1;
            CameraState = ASCOM.DeviceInterface.CameraStates.cameraIdle;
            CameraXSize = -1;
            CameraYSize = -1;
            CCDTemperature = double.MinValue;
            _prevcCDTemperature = double.MinValue;

            HasShutter = false;
            

            CoolerPower = -1;
            _prevCoolerPower = -1;
            Description = string.Empty;
            DriverInfo = string.Empty;
            DriverVersion = string.Empty;
            ElectronsPerADU = -1;
            ExposureMax = -1;
            ExposureMin = -1;
            ExposureResolution = -1;
            FullWellCapacity = -1;
            GainMax = -1;
            GainMin = -1;
            Gains = null;
            HeatSinkTemperature = double.MinValue;
            ImageArray = null;
            ImageArrayVariant = null;
            InterfaceVersion = -1;
            IsPulseGuiding = false;
            LastExposureDuration = -1;
            LastExposureStartTime = string.Empty;
            MaxADU = -1;
            MaxBinX = -1;
            MaxBinY = -1;
            Name = string.Empty;
            PercentCompleted = -1;
            PixelSizeX = -1;
            PixelSizeY = -1;
            ReadoutModes = null;
            SensorName = string.Empty;
            SensorType = ASCOM.DeviceInterface.SensorType.Monochrome;
            SupportedActions = null;

            BinX = -1;
            BinY = -1;

            BinningModes = new ObservableCollection<BinningMode>();
            //CCDTemperatureHistory.Clear();
            //CoolerPowerHistory.Clear();
            
           CoolerOn = false;
            FastReadout = false;
            Gain = -1;
            NumX = -1;
            NumY = -1;
            ReadoutMode = -1;
            SetCCDTemperature = double.MinValue;
            StartX = -1;
            StartY = -1;
            HasCCDTemperature = false;
            HasFullWellCapacity = false;
            HasHeatSinkTemperature = false;

            CoolerPowerChange = int.MinValue;
            CCDTemperatureChange = int.MinValue;
        }


        #region "Properties"
        
        private string _progId;
        public string ProgId {
            get {
                return _progId;
            } set {
                _progId = value;
                RaisePropertyChanged();
            }
        }

        private Camera _ascomCamera;
        public Camera AscomCamera {
            get {
                return _ascomCamera;
            } set {
                _ascomCamera = value;
                RaisePropertyChanged();
            }
        }

        //get
        private short _bayerOffsetX;
        public short BayerOffsetX {
            get {
                return _bayerOffsetX;
            } 
            set {
                _bayerOffsetX = value;
                RaisePropertyChanged();
            }
        }
        private short _bayerOffsetY;
        public short BayerOffsetY {
            get {
                return _bayerOffsetY;
            }
            set {
                _bayerOffsetY = value;
                RaisePropertyChanged();
            }
        }

        ASCOM.DeviceInterface.CameraStates _cameraState;
        ASCOM.DeviceInterface.CameraStates CameraState {
            get {                
                return _cameraState;
            } set {
                _cameraState = value;
                //CameraStateString = _cameraState.ToString();
                RaisePropertyChanged();
                RaisePropertyChanged("CameraStateString");
            }
        }

        public void setBinning(short x, short y) {
            BinX = x;
            BinY = y;
        }

        //private string _cameraStateString;
        public string CameraStateString {
            get {
                return CameraState.ToString();
            } /*set {
                _cameraStateString = value;
                RaisePropertyChanged();   
            }*/
        }



        private int _cameraXSize;
        public int CameraXSize {
            get {
                return _cameraXSize;
            }
            set {
                _cameraXSize = value;
                RaisePropertyChanged();
            }
        }
        private int _cameraYSize;
        public int CameraYSize {
            get {
                return _cameraYSize;
            }
            set {
                _cameraYSize = value;
                RaisePropertyChanged();
            }
        }

        double _prevcCDTemperature;
        double _cCDTemperature;
        public double CCDTemperature {
            get {
                return _cCDTemperature;
            } set {
                if(_prevcCDTemperature < value) {
                    CCDTemperatureChange = 1;
                } else if(_prevcCDTemperature > value) {
                    CCDTemperatureChange = -1;
                } else {
                    CCDTemperatureChange = 0;
                }

                _cCDTemperature = value;
                RaisePropertyChanged();
            }
        }

        private int _cCDTemperatureChange;
        public int CCDTemperatureChange {
            get {
                return _cCDTemperatureChange;
            }
            set {
                _cCDTemperatureChange = value;
                RaisePropertyChanged();
            }
        }

        bool _canAbortExposure;
        public bool CanAbortExposure {
            get {
                return _canAbortExposure;
            } 
            set {
                _canAbortExposure = value;
                RaisePropertyChanged();
            }
        }
        bool _canAsymmetricBin;
        public bool CanAsymmetricBin {
            get {
                return _canAsymmetricBin;
            }
            set {
                _canAsymmetricBin = value;
                RaisePropertyChanged();
            }
        }
        bool _canFastReadout;
        public bool CanFastReadout {
            get {
                return _canFastReadout;
            }
            set {
                _canFastReadout = value;
                RaisePropertyChanged();
            }
        }
        bool _canGetCoolerPower;
        public bool CanGetCoolerPower {
            get {
                return _canGetCoolerPower;
            }
            set {
                _canGetCoolerPower = value;
                RaisePropertyChanged();
            }
        }
        bool _canPulseGuide;
        public bool CanPulseGuide {
            get {
                return _canPulseGuide;
            }
            set {
                _canPulseGuide = value;
                RaisePropertyChanged();
            }
        }
        bool _canSetCCDTemperature;
        public bool CanSetCCDTemperature {
            get {
                return _canSetCCDTemperature;
            }
            set {
                _canSetCCDTemperature = value;
                RaisePropertyChanged();
            }
        }
        bool _canStopExposure;
        public bool CanStopExposure {
            get {
                return _canStopExposure;
            }
            set {
                _canStopExposure = value;
                RaisePropertyChanged();
            }
        }
        bool _hasShutter;
        public bool HasShutter {
            get {
                return _hasShutter;
            }
            set {
                _hasShutter = value;
                RaisePropertyChanged();
            }
        }

        double _prevCoolerPower;        

        double _coolerPower;
        public double CoolerPower {
            get {
                return _coolerPower;
            }
            set {
                if(CanGetCoolerPower) {
                    if (_prevCoolerPower < value) {
                        CoolerPowerChange = 1;
                    }
                    else if (_prevCoolerPower > value) {
                        CoolerPowerChange = -1;
                    }
                    else {
                        CoolerPowerChange = 0;
                    }

                    _coolerPower = value;
                    
                    RaisePropertyChanged();
                }

            }
        }

        private int _coolerPowerChange;
        public int CoolerPowerChange {
            get {
                return _coolerPowerChange;
            }
            set {
                _coolerPowerChange = value;
                RaisePropertyChanged();
            }
        }

        /*ObservableCollection<KeyValuePair<DateTime, double>> _cCDTemperatureHistory;
        public ObservableCollection<KeyValuePair<DateTime, double>> CCDTemperatureHistory {
            get {
                if (_cCDTemperatureHistory == null) {
                    _cCDTemperatureHistory = new ObservableCollection<KeyValuePair<DateTime, double>>();
                }
                return _cCDTemperatureHistory;
            }
            set {
                _cCDTemperatureHistory = value;
                RaisePropertyChanged();
            }
        }
       
        ObservableCollection<KeyValuePair<DateTime, double>> _coolerPowerHistory;            
        public ObservableCollection<KeyValuePair<DateTime, double>> CoolerPowerHistory {
            get {
                if(_coolerPowerHistory == null) {
                    _coolerPowerHistory = new ObservableCollection<KeyValuePair<DateTime, double>>();
                }
                return _coolerPowerHistory;
            }
            set {
                _coolerPowerHistory = value;
                RaisePropertyChanged();
            }
        }*/

        string _description;
        public string Description {
            get {
                return _description;
            }
            set {
                _description = value;
                RaisePropertyChanged();
            }
        }
        string _driverInfo;
        public string DriverInfo {
            get {
                return _driverInfo;
            }
            set {
                _driverInfo = value;
                RaisePropertyChanged();
            }
        }
        string _driverVersion;
        public string DriverVersion {
            get {
                return _driverVersion;
            }
            set {
                _driverVersion = value;
                RaisePropertyChanged();
            }
        }
        double _electronsPerADU;
        public double ElectronsPerADU {
            get {
                return _electronsPerADU;
            }
            set {
                _electronsPerADU = value;
                RaisePropertyChanged();
            }
        }
        double _exposureMax;
        public double ExposureMax {
            get {
                return _exposureMax;
            } 
            set {
                _exposureMax = value;
                RaisePropertyChanged();
            }
        }
        double _exposureMin;
        public double ExposureMin {
            get {
                return _exposureMin;
            }
            set {
                _exposureMin = value;
                RaisePropertyChanged();
            }
        }
        double _exposureResolution;
        public double ExposureResolution {
            get {
                return _exposureResolution;
            } 
            set {
                _exposureResolution = value;
                RaisePropertyChanged();
            }
        }
        double _fullWellCapacity;
        public double FullWellCapacity {
            get {
                return _fullWellCapacity;
            }
            set {
                _fullWellCapacity = value;
                RaisePropertyChanged();
            }
        }
        short _gainMax;
        public short GainMax {
            get {
                return _gainMax;
            } 
            set {
                _gainMax = value;
                RaisePropertyChanged();
            }
        }
        short _gainMin;
        public short GainMin {
            get {
                return _gainMin;
            }
            set {
                _gainMin = value;
                RaisePropertyChanged();
            }
        }
        ArrayList _gains;
        public ArrayList Gains {
            get {
                if (_gains == null) {
                    _gains = new ArrayList();
                }
                return _gains;
            }
            set {
                _gains = value;
                RaisePropertyChanged();
            }
        }
            
        double _heatSinkTemperature;
        public double HeatSinkTemperature {
            get {
                return _heatSinkTemperature;
            }
            set {
                _heatSinkTemperature = value;
                RaisePropertyChanged();
            }
        }
       
        short _interfaceVersion;
        public short InterfaceVersion {
            get {
                return _interfaceVersion;
            }
            set {
                _interfaceVersion = value;
                RaisePropertyChanged();
            }
        }
        bool _isPulseGuiding;
        public bool IsPulseGuiding {
            get {
                return _isPulseGuiding;
            }
            set {
                _isPulseGuiding = value;
                RaisePropertyChanged();
            }
        }
        
        int _maxADU;
        public int MaxADU {
            get {
                return _maxADU;
            }
            set {
                _maxADU = value;
                RaisePropertyChanged();
            }
        }

        ObservableCollection<BinningMode> _binningModes;
        public ObservableCollection<BinningMode> BinningModes {
            get {
                if(_binningModes == null) {
                    _binningModes = new ObservableCollection<BinningMode>();
                }
                return _binningModes;
            }

            set {
                _binningModes = value;
                RaisePropertyChanged();
            }
        }

        short _maxBinX;
        public short MaxBinX {
            get {
                return _maxBinX;
            }
            set {
                _maxBinX = value;
                RaisePropertyChanged();
            }
        }
        short _maxBinY;
        public short MaxBinY {
            get {
                return _maxBinY;
            }
            set {
                _maxBinY = value;
                RaisePropertyChanged();
            }
        }
        string _name;
       public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }
        double _pixelSizeX;
        public double PixelSizeX {
            get {
                return _pixelSizeX;
            }
            set {
                _pixelSizeX = value;
                RaisePropertyChanged();
            }
        }
        double _pixelSizeY;
        public double PixelSizeY {
            get {
                return _pixelSizeY;
            }
            set {
                _pixelSizeY = value;
                RaisePropertyChanged();
            }
        }
        ArrayList _readoutModes;
        public ArrayList ReadoutModes {
            get {
                if (_readoutModes == null) {
                    _readoutModes = new ArrayList();
                }
                return _readoutModes;
            }
            set {
                _readoutModes = value;
                RaisePropertyChanged();
            }
        }
        string _sensorName;
        public string SensorName {
            get {
                return _sensorName;
            }
            set {
                _sensorName = value;
                RaisePropertyChanged();
            }
        }
        ASCOM.DeviceInterface.SensorType _sensorType;
        public ASCOM.DeviceInterface.SensorType SensorType {
            get {
                return _sensorType;
            }
            set {
                _sensorType = value;
                RaisePropertyChanged();
            }
        }
        ArrayList _supportedActions;
        public ArrayList SupportedActions {
            get {
                return _supportedActions;
            }
            set {
                _supportedActions = value;
                RaisePropertyChanged();
            }
        }


        /*Settable Attributes */
        bool _connected;
        public bool Connected {
            get {
                return _connected;
            }
            set {
                _connected = value;
                if (AscomCamera != null) { 
                    AscomCamera.Connected = value;
                }
                RaisePropertyChanged();
            }
        }

        private short _binX;
        
        public short BinX {
            get {
                return _binX;
            }

            set {
                _binX = value;
                if(AscomCamera != null && AscomCamera.Connected) {
                    AscomCamera.BinX = value;
                    AscomCamera.NumX = AscomCamera.CameraXSize / value;
                }
                RaisePropertyChanged();
            }
        }
        private short _binY;
        

        public short BinY {
            get {
                return _binY;
            }

            set {
                _binY = value;
                if (AscomCamera != null && AscomCamera.Connected) {
                    AscomCamera.BinY = value;
                    
                    AscomCamera.NumY = AscomCamera.CameraYSize / value;
                }
                RaisePropertyChanged();
            }
        }
        bool _coolerOn;
        
        public bool CoolerOn {
            get {
                return _coolerOn;
            }

            set {
                if(CanCoolerOn) { 
                    _coolerOn = value;
                    AscomCamera.CoolerOn = value;
                    RaisePropertyChanged();
                }
            }
        }

        bool _canCoolerOn;
        public bool CanCoolerOn {
            get {
                return _canCoolerOn;
            }
            set {
                _canCoolerOn = value;
                RaisePropertyChanged();
            }
        }

        bool _fastReadout;
        
        public bool FastReadout {
            get {
                return _fastReadout;
            }

            set {
                _fastReadout = value;
                RaisePropertyChanged();
            }
        }
        short _gain;
        
        public short Gain {
            get {
                return _gain;
            }

            set {
                _gain = value;
                RaisePropertyChanged();
            }
        }
        int _numX;
       
        public int NumX {
            get {
                return _numX;
            }

            set {
                _numX = value;
                RaisePropertyChanged();
            }
        }
        int _numY;
        
        public int NumY {
            get {
                return _numY;
            }

            set {
                _numY = value;
                RaisePropertyChanged();
            }
        }
        short _readoutMode;
        
        public short ReadoutMode {
            get {
                return _readoutMode;
            }

            set {
                _readoutMode = value;
                RaisePropertyChanged();
            }
        }
        double _setCCDTemperature;
        
        public double SetCCDTemperature {
            get {
                return _setCCDTemperature;
            }

            set {

                _setCCDTemperature = value;
                if (CanSetCCDTemperature) {
                    AscomCamera.SetCCDTemperature = value;                    
                }
                RaisePropertyChanged();
            }
        }
        int _startX;
        
        public int StartX {
            get {
                return _startX;
            }

            set {
                _startX = value;
                RaisePropertyChanged();
            }
        }
        int _startY;
        
        public int StartY {
            get {
                return _startY;
            }

            set {
                _startY = value;
                RaisePropertyChanged();
            }
        }
        private bool _hasCCDTemperature;
       
        public bool HasCCDTemperature {
            get {
                return _hasCCDTemperature;
            }

            set {
                _hasCCDTemperature = value;
                RaisePropertyChanged();
            }
        }
        private bool _hasFullWellCapacity;
        
        public bool HasFullWellCapacity {
            get {
                return _hasFullWellCapacity;
            }

            set {
                _hasFullWellCapacity = value;
                RaisePropertyChanged();
            }
        }
        private bool _hasHeatSinkTemperature;
        public bool HasHeatSinkTemperature {
            get {
                return _hasHeatSinkTemperature;
            }

            set {
                _hasHeatSinkTemperature = value;
                RaisePropertyChanged();
            }
        }

        #endregion



        /*Imaging objects*/
        object _imageArray;
        
        public object ImageArray {
            get {
                return _imageArray;
            }

            set {
                _imageArray = value;
                RaisePropertyChanged();
            }
        }
        object _imageArrayVariant;
        
        public object ImageArrayVariant {
            get {
                return _imageArrayVariant;
            }

            set {
                _imageArrayVariant = value;
                RaisePropertyChanged();
            }
        }
        
        public bool ImageReady {
            get {
                if(AscomCamera == null) {
                    return false;
                } else {
                    return AscomCamera.ImageReady; 
                }
                
            }            
        }
        double _lastExposureDuration;
        
        public double LastExposureDuration {
            get {
                return _lastExposureDuration;
            }

            set {
                _lastExposureDuration = value;
                RaisePropertyChanged();
            }
        }
        string _lastExposureStartTime;
        
        public string LastExposureStartTime {
            get {
                return _lastExposureStartTime;
            }

            set {
                _lastExposureStartTime = value;
                RaisePropertyChanged();
            }
        }
        short _percentCompleted;
        public short PercentCompleted {
            get {
                return _percentCompleted;
            }

            set {
                _percentCompleted = value;
                RaisePropertyChanged();
            }
        }

        

        public void getCameraInfo () {
            try {
                 BayerOffsetX = AscomCamera.BayerOffsetX;
                 BayerOffsetY = AscomCamera.BayerOffsetY;
            } catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement BayerOffsets");
                Logger.trace(ex.Message);
                 BayerOffsetX = -1;
                BayerOffsetY = -1;
            }
            
           
            
            try {
                 CameraXSize = AscomCamera.CameraXSize;
                 CameraYSize = AscomCamera.CameraYSize;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement CameraSizes");
                Logger.trace(ex.Message);
                CameraXSize = -1;
                CameraYSize = -1;
            }
            
            try {
                CanAbortExposure = AscomCamera.CanAbortExposure;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement CanAbortExposure");
                Logger.trace(ex.Message);
                CanAbortExposure = false;
            }

            try {
                CanAsymmetricBin = AscomCamera.CanAsymmetricBin;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement CanAsymmetricBin");
                Logger.trace(ex.Message);
                CanAsymmetricBin = false;
            }

            

            try {
                CanStopExposure = AscomCamera.CanStopExposure;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement CanStopExposure");
                Logger.trace(ex.Message);
                CanStopExposure = false;
            }

            try {
                HasShutter = AscomCamera.HasShutter;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement HasShutter");
                Logger.trace(ex.Message);
                HasShutter = false;
            }
                        
            try {
                 Description = AscomCamera.Description;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement Description");
                Logger.trace(ex.Message);
                Description = "n.A.";
            }

            try {
                 DriverInfo = AscomCamera.DriverInfo;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement DriverInfo");
                Logger.trace(ex.Message);
                DriverInfo = "n.A.";
            }

            try {
                 DriverVersion = AscomCamera.DriverVersion;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement DriverVersion");
                Logger.trace(ex.Message);
                DriverVersion = "n.A.";
            }

            try {
                 ElectronsPerADU = AscomCamera.ElectronsPerADU;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement ElectronsPerADU");
                Logger.trace(ex.Message);
                ElectronsPerADU = -1;
            }

            try {
                 ExposureMax = AscomCamera.ExposureMax;
                 ExposureMin = AscomCamera.ExposureMin;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement ExposureMax/ExposureMin");
                Logger.trace(ex.Message);
                ExposureMax = -1;
                 ExposureMin = -1;
            }

            try {
                 ExposureResolution = AscomCamera.ExposureResolution;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement ExposureResolution");
                Logger.trace(ex.Message);
                ExposureResolution = -1;
            }
                        
            
            try {
                 InterfaceVersion = AscomCamera.InterfaceVersion;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement InterfaceVersion");
                Logger.trace(ex.Message);
                InterfaceVersion = -1;
            }
            
            try {
                 MaxADU = AscomCamera.MaxADU;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement MaxADU");
                Logger.trace(ex.Message);
                MaxADU = -1;
            }
            try {
                 MaxBinX = AscomCamera.MaxBinX;
                 MaxBinY = AscomCamera.MaxBinY;
                for(short i = 1; i<=MaxBinX; i++) {
                    if(CanAsymmetricBin) {
                        for (short j = 1; j <= MaxBinY; j++) {
                            BinningModes.Add(new BinningMode(i, j));
                        }
                    } else {
                        BinningModes.Add(new BinningMode(i, i));
                    }
                    
                    
                }
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement MaxBinning");
                Logger.trace(ex.Message);
                BinningModes.Add(new BinningMode(1, 1));
                 MaxBinX = -1;
                 MaxBinY = -1;
            }
            try {
                 Name = AscomCamera.Name;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement Name");
                Logger.trace(ex.Message);
                Name = "n.A.";
            }
                        
            try {
                 PixelSizeX = AscomCamera.PixelSizeX;
                 PixelSizeY = AscomCamera.PixelSizeY;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement PixelSize");
                Logger.trace(ex.Message);
                PixelSizeX = -1;
                 PixelSizeY = -1;
            }
            try {
                 ReadoutMode = AscomCamera.ReadoutMode;
                 ReadoutModes = AscomCamera.ReadoutModes;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement Readoutmodes");
                Logger.trace(ex.Message);
                ReadoutMode = -1;
                 ReadoutModes = null;
            }
            
            try {
                 SensorName = AscomCamera.SensorName;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement SensorName");
                Logger.trace(ex.Message);
                SensorName = "n.A.";
            }
            try {
                 SensorType = AscomCamera.SensorType;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement SensorType");
                Logger.trace(ex.Message);
                SensorType = ASCOM.DeviceInterface.SensorType.Monochrome;
            }

            try {
                 StartX = AscomCamera.StartX;
                 StartY = AscomCamera.StartY;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement StartX/StartY");
                Logger.trace(ex.Message);
                StartX = -1;
                 StartY = -1;
            }
            try {
                 SupportedActions = AscomCamera.SupportedActions;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement SupportedActions");
                Logger.trace(ex.Message);
                SupportedActions = null;
            }

            /*----*/

            try {
                 BinX = AscomCamera.BinX;
                 BinY = AscomCamera.BinY;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement Binning");
                Logger.trace(ex.Message);
                BinX = -1;
                 BinY = -1;
            }

            try {
                 NumX = AscomCamera.NumX;
                 NumY = AscomCamera.NumY;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement NumX/NumY");
                Logger.trace(ex.Message);
                NumX = -1;
                 NumY = -1;
            }

            try {
                 FastReadout = AscomCamera.FastReadout;
                CanFastReadout = AscomCamera.CanFastReadout;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement FastReadout");
                Logger.trace(ex.Message);
                FastReadout = false;
                CanFastReadout = false;
            }

            try {
                 Gain = AscomCamera.Gain;                 
                 Gains = AscomCamera.Gains;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement Gain");
                Logger.trace(ex.Message);
                Gain = -1;
                 Gains = null;
            }

            try {
                GainMax = AscomCamera.GainMax;
                GainMin = AscomCamera.GainMin;
            } catch (ASCOM.InvalidOperationException ex) {
                Logger.warning("Used Camera AscomDriver does not implement Gain");
                Logger.trace(ex.Message);
                GainMax = -1;
                GainMin = -1;
            } catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement Gain");
                Logger.trace(ex.Message);
                GainMax = -1;
                GainMin = -1;
            }

            try {
                CameraState = AscomCamera.CameraState;/*Watch!*/
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement CameraState");
                Logger.trace(ex.Message);
                CameraState = ASCOM.DeviceInterface.CameraStates.cameraError;
            }

            try {
                CCDTemperature = AscomCamera.CCDTemperature; /*Watch!*/
                HasCCDTemperature = true;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement CCDTemperature");
                Logger.trace(ex.Message);
                CCDTemperature = double.MinValue;
                HasCCDTemperature = false;
            }


            try {

                CanSetCCDTemperature = AscomCamera.CanSetCCDTemperature;
                
                if(HasCCDTemperature) { 
                SetCCDTemperature = AscomCamera.CCDTemperature;
                } else {
                    SetCCDTemperature = AscomCamera.SetCCDTemperature;
                }

            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement Temperature Control");
                Logger.trace(ex.Message);
                CanSetCCDTemperature = false;
                SetCCDTemperature = double.MinValue;

            }

            try {
                 FullWellCapacity = AscomCamera.FullWellCapacity;/*Watch!*/
                 HasFullWellCapacity = true;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement FullWellCapacity");
                Logger.trace(ex.Message);
                FullWellCapacity = -1;
                 HasFullWellCapacity = false;
            }

            try {
                CanCoolerOn = true;
                CoolerOn = AscomCamera.CoolerOn;
                CanGetCoolerPower = AscomCamera.CanGetCoolerPower;
                CoolerPower = AscomCamera.CoolerPower;/*Watch!*/
                
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement Cooler Info");
                Logger.trace(ex.Message);
                CanCoolerOn = false;
                CoolerOn = false;
                CanGetCoolerPower = false;
                CoolerPower = -1;                
                
            }

            try {
                CanPulseGuide = AscomCamera.CanPulseGuide;
                IsPulseGuiding = AscomCamera.IsPulseGuiding; /*Watch*/
                
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement PulseGuiding");
                Logger.trace(ex.Message);
                CanPulseGuide = false;
                IsPulseGuiding = false;
                
            }
            try {
                 HeatSinkTemperature = AscomCamera.HeatSinkTemperature;/*Watch*/
                 HasHeatSinkTemperature = true;
            }
            catch (Exception ex) {
                Logger.warning("Used Camera AscomDriver does not implement HeatSink");
                Logger.trace(ex.Message);
                HeatSinkTemperature = double.MinValue;
                 HasHeatSinkTemperature = false;
            }
        }

        public void updateValues() {
            try {
            
                CameraState = AscomCamera.CameraState;
                if( HasCCDTemperature) {
                    _prevcCDTemperature = CCDTemperature;
                    CCDTemperature = AscomCamera.CCDTemperature;                    
                    
                }
            
                if( HasFullWellCapacity) {
                     FullWellCapacity = AscomCamera.FullWellCapacity;
                }

                if ( HasHeatSinkTemperature) {
                     HeatSinkTemperature = AscomCamera.HeatSinkTemperature;
                }
            
            

                if ( CanGetCoolerPower) {
                    _prevCoolerPower = CoolerPower;
                     CoolerPower = AscomCamera.CoolerPower;
                    
                }

                if (CanPulseGuide) {
                     IsPulseGuiding = AscomCamera.IsPulseGuiding;
                }
            }
            catch (Exception e) {
                Notification.ShowError(e.Message);
            }

        }

        public void disconnect() {
            if(AscomCamera != null && Connected) { 
                Connected = false;            
                AscomCamera.Dispose();
                init();
                //CameraStateString = "disconnected";
            }
        }

        public bool connect() {
            bool con = false;
            string oldProgId = this.ProgId;
            string cameraId = Settings.CameraId;
            ProgId = Camera.Choose(cameraId);
            if((!Connected || oldProgId != ProgId) && ProgId != "") {

                init();
                try {
                    AscomCamera = new Camera(ProgId);
                    
                    //AscomCamera.Connected = true;
                    Connected = true;
                    Settings.CameraId = ProgId;

                    try {

                    
                        if (AscomCamera.SensorType == ASCOM.DeviceInterface.SensorType.Color) {
                            Notification.ShowError("Sorry! This sensor type is not supported");                        
                            disconnect();
                            return false; ;
                        }
                    } catch(ASCOM.PropertyNotImplementedException) {

                    }


                    getCameraInfo();
                    Notification.ShowSuccess("Camera connected.");
                    con = true;
                }
                catch (ASCOM.DriverAccessCOMException ex) {
                    Logger.error("Unable to connect to camera");                    
                    Notification.ShowError("Unable to connect to camera");
                    Logger.trace(ex.Message);
                    //CameraStateString = "Unable to connect to camera";
                    Connected = false;
                }
                catch (Exception ex) {
                    Logger.error("Unable to connect to camera");
                    Notification.ShowError("Unable to connect to camera");
                    Logger.trace(ex.Message);
                    Connected = false;
                }

            }
            return con;
        }

        public void startExposure(double exposureTime, bool isLightFrame) {
            AscomCamera.StartExposure(exposureTime, isLightFrame);
        }

        public void stopExposure() {
            if(AscomCamera.CanStopExposure) { 
                try {
                    AscomCamera.StopExposure();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
                
            }
        }

        public async Task<Array> downloadExposure(CancellationTokenSource tokenSource) {
            return await Task<Array>.Run(() => {
                try {
                    ASCOM.Utilities.Util U = new ASCOM.Utilities.Util();
                    while (!ImageReady && Connected) {
                        //Console.Write(".");
                        U.WaitForMilliseconds(10);
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }

                    Array arr;

                    if (AscomCamera.ImageArray.GetType() == typeof(Int32[,])) {
                        arr = (Int32[,])AscomCamera.ImageArray;
                        return arr;
                    }
                    else {
                        arr = (Int32[,,])AscomCamera.ImageArray;
                        return (Int32[,,])AscomCamera.ImageArray;
                    }
                } catch (OperationCanceledException ex) {
                    Logger.trace(ex.Message);
                } catch {
                    
                }
                return null;
                
            });            
        }

/*
        public Int32[,] snap(double exposureTime, bool isLightFrame) {
            ASCOM.Utilities.Util U = new ASCOM.Utilities.Util();
            AscomCamera.StartExposure(exposureTime, isLightFrame);
                
            while (!ImageReady && Connected) {
                //Console.Write(".");
                U.WaitForMilliseconds(100);
            }
            
            Int32[,] camArray = (Int32[,])AscomCamera.ImageArray;
            
            
            return camArray;
        }

        */


        //public BitmapSource NormalizeTiffTo8BitImage(BitmapSource source) {
        //    // allocate buffer & copy image bytes.
        //    var rawStride = source.PixelWidth * source.Format.BitsPerPixel / 8;
        //    var rawImage = new byte[rawStride * source.PixelHeight];
        //    source.CopyPixels(rawImage, rawStride, 0);

        //    // get both max values of first & second byte of pixel as scaling bounds.
        //    var max1 = 0;
        //    int max2 = 1;
        //    for (int i = 0; i < rawImage.Length; i++) {
        //        if ((i & 1) == 0) {
        //            if (rawImage[i] > max1)
        //                max1 = rawImage[i];
        //        }
        //        else if (rawImage[i] > max2)
        //            max2 = rawImage[i];
        //    }

        //    // determine normalization factors.
        //    var normFactor = max2 == 0 ? 0.0d : 128.0d / max2;
        //    var factor = max1 > 0 ? 255.0d / max1 : 0.0d;
        //    max2 = Math.Max(max2, 1);

        //    // normalize each pixel to output buffer.
        //    var buffer8Bit = new byte[rawImage.Length / 2];
        //    for (int src = 0, dst = 0; src < rawImage.Length; dst++) {
        //        int value16 = rawImage[src++];
        //        double value8 = ((value16 * factor) / max2) - normFactor;

        //        if (rawImage[src] > 0) {
        //            int b = rawImage[src] << 8;
        //            value8 = ((value16 + b) / max2) - normFactor;
        //        }
        //        buffer8Bit[dst] = (byte)Math.Min(255, Math.Max(value8, 0));
        //        src++;
        //    }

        //    // return new bitmap source.
        //    return BitmapSource.Create(
        //        source.PixelWidth, source.PixelHeight,
        //        source.DpiX, source.DpiY,
        //        PixelFormats.Gray8, BitmapPalettes.Gray256,
        //        buffer8Bit, rawStride / 2);
        //}
    }
}
