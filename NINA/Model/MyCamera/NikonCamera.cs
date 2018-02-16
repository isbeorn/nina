using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using NINA.Utility;
using Nikon;
using NINA.Utility.DCRaw;
using System.IO;
using NINA.Utility.Notification;
using System.Globalization;
using NINA.Utility.Mediator;

namespace NINA.Model.MyCamera {
    public class NikonCamera : BaseINPC, ICamera {
        public NikonCamera() {
            /* NIKON */
            Name = "Nikon";
            _nikonManagers = new List<NikonManager>();
        }

        private List<NikonManager> _nikonManagers;
        private NikonManager _activeNikonManager;

        private void Mgr_DeviceRemoved(NikonManager sender, NikonDevice device) {
            Disconnect();
        }

        private void Mgr_DeviceAdded(NikonManager sender, NikonDevice device) {
            try {
                _activeNikonManager = sender;
                _activeNikonManager.DeviceRemoved += Mgr_DeviceRemoved;

                CleanupUnusedManagers(_activeNikonManager);

                Init(device);

                Connected = true;
                Name = _camera.Name;
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
                Logger.Error(ex);
            } finally {
                RaiseAllPropertiesChanged();
                _cameraConnected.TrySetResult(null);
            }
        }

        private void CleanupUnusedManagers(NikonManager activeManager) {
            foreach (NikonManager mgr in _nikonManagers) {
                if (mgr != activeManager) {
                    mgr.Shutdown();
                }
            }
            _nikonManagers.Clear();
        }


        public void Init(NikonDevice cam) {
            Logger.Debug("Initializing Nikon camera");
            _camera = cam;
            _camera.ImageReady += Camera_ImageReady;
            _camera.CaptureComplete += _camera_CaptureComplete;


            //Set to shoot in RAW
            Logger.Debug("Setting compression to RAW");
            var compression = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel);
            for (int i = 0; i < compression.Length; i++) {
                var val = compression.GetEnumValueByIndex(i);
                if (val.ToString() == "RAW") {
                    compression.Index = i;
                    _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel, compression);
                    break;
                }
            }

            GetShutterSpeeds();
            GetCapabilities();
        }

        private void GetCapabilities() {
            Logger.Debug("Getting Nikon capabilities");
            Capabilities.Clear();
            foreach (NkMAIDCapInfo info in _camera.GetCapabilityInfo()) {
                Capabilities.Add(info.ulID, info);

                var description = info.GetDescription();
                var canGet = info.CanGet();
                var canGetArray = info.CanGetArray();
                var canSet = info.CanSet();
                var canStart = info.CanStart();

                Logger.Debug(description);
                Logger.Debug("\t Id: " + info.ulID.ToString());
                Logger.Debug("\t CanGet: " + canGet.ToString());
                Logger.Debug("\t CanGetArray: " + canGetArray.ToString());
                Logger.Debug("\t CanSet: " + canSet.ToString());
                Logger.Debug("\t CanStart: " + canStart.ToString());

                if (info.ulID == eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed && !canSet) {
                    throw new NikonException("Cannot set shutterspeeds. Please make sure the camera dial is set to a position where bublb mode is possible and the mirror lock is turned off");
                }
            }
        }

        private Dictionary<eNkMAIDCapability, NkMAIDCapInfo> Capabilities = new Dictionary<eNkMAIDCapability, NkMAIDCapInfo>(); 

        private void GetShutterSpeeds() {
            Logger.Debug("Getting Nikon shutter speeds");
            _shutterSpeeds.Clear();
            var shutterSpeeds = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);
            Logger.Debug("Available Shutterspeeds: " + shutterSpeeds.Length);
            bool bulbFound = false;
            for (int i = 0; i < shutterSpeeds.Length; i++) {
                try {
                    var val = shutterSpeeds.GetEnumValueByIndex(i).ToString();
                    Logger.Debug("Found Shutter speed: " + val);
                    if (val.Contains("/")) {
                        var split = val.Split('/');
                        var convertedSpeed = double.Parse(split[0], CultureInfo.InvariantCulture) / double.Parse(split[1], CultureInfo.InvariantCulture);

                        _shutterSpeeds.Add(i, convertedSpeed);
                    } else if (val.ToLower() == "bulb") {
                        Logger.Debug("Bulb index: " + i);
                        _bulbShutterSpeedIndex = i;
                        bulbFound = true;
                    }
                } catch (Exception ex) {
                    Logger.Error("Unexpected Shutter Speed: ", ex);
                }
            }
            if (!bulbFound) {
                Logger.Error("No Bulb speed found!", null);
                throw new NikonException("Failed to find the 'Bulb' exposure mode");
            }
        }

        private TaskCompletionSource<object> _downloadExposure;
        private TaskCompletionSource<object> _cameraConnected;

        private void _camera_CaptureComplete(NikonDevice sender, int data) {
            Logger.Debug("Capture complete");
        }

        private string _fileExtension;

        private void Camera_ImageReady(NikonDevice sender, NikonImage image) {
            Logger.Debug("Image ready");
            _fileExtension = (image.Type == NikonImageType.Jpeg) ? ".jpg" : ".nef";
            var filename = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, DCRaw.FILEPREFIX + _fileExtension);

            Logger.Debug("Writing Image to temp folder");
            using (System.IO.FileStream s = new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                s.Write(image.Buffer, 0, image.Buffer.Length);
            }
            Logger.Debug("Setting Download Exposure Taks to complete");
            _downloadExposure.TrySetResult(null);
        }

        private NikonDevice _camera;


        public string Id {
            get {
                return "Nikon";
            }
        }

        private string _name;
        public string Name {
            get {
                return _name;
            }
            private set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public string Description {
            get {
                if (Connected) {
                    return _camera.Name;
                } else {
                    return string.Empty;
                }
            }
        }

        public bool HasShutter {
            get {
                return true;
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
                return double.NaN;
            }
        }

        public double SetCCDTemperature {
            get {
                return double.NaN;
            }

            set {

            }
        }

        public short BinX {
            get {
                return 1;
            }

            set {
            }
        }

        public short BinY {
            get {
                return 1;
            }
            set {
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
                return SensorType.RGGB;
            }
        }

        public int CameraXSize {
            get {
                return -1;
            }
        }

        public int CameraYSize {
            get {
                return -1;
            }
        }

        public double ExposureMin {
            get {
                return 0;
            }
        }

        public double ExposureMax {
            get {
                return double.PositiveInfinity;
            }
        }

        public short MaxBinX {
            get {
                return 1;
            }
        }

        public short MaxBinY {
            get {
                return 1;
            }
        }

        public double PixelSizeX {
            get {
                return -1;
            }
        }

        public double PixelSizeY {
            get {
                return -1;
            }
        }

        public bool CanSetCCDTemperature {
            get {
                return false;
            }
        }

        public bool CoolerOn {
            get {
                return false;
            }

            set {

            }
        }

        public double CoolerPower {
            get {
                return double.NaN;
            }
        }

        private string _cameraState;
        public string CameraState {
            get {
                return _cameraState;
            }
            set {
                _cameraState = value;
                RaisePropertyChanged();
            }
        }

        public int Offset {
            get {
                return -1;
            }
            set {

            }
        }

        public int USBLimit {
            get {
                return -1;
            }
            set {

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

        public bool CanGetGain {
            get {
                if (Connected) {
                    return _camera.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                } else {
                    return false;
                }
            }
        }

        public bool CanSetGain {
            get {
                if (Connected) {
                    return _camera.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                } else {
                    return false;
                }
            }
        }

        public short GainMax {
            get {
                if (Gains != null) {
                    return ISOSpeeds.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                } else {
                    return 0;
                }
            }
        }

        public short GainMin {
            get {
                if (Gains != null) {
                    return ISOSpeeds.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                } else {
                    return 0;
                }
            }
        }

        public short Gain {
            get {
                if (Connected) {
                    NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                    short iso;
                    if (short.TryParse(e.Value.ToString(), out iso)) {
                        return iso;
                    } else {
                        return -1;
                    }
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    var iso = ISOSpeeds.Where((x) => x.Key == value).FirstOrDefault().Value;
                    NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                    e.Index = iso;
                    _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity, e);
                    RaisePropertyChanged();
                }
            }
        }

        private Dictionary<short, int> ISOSpeeds = new Dictionary<short, int>();

        private ArrayList _gains;
        public ArrayList Gains {
            get {
                if (_gains == null) {
                    _gains = new ArrayList();
                }

                if (_gains.Count == 0 && Connected && CanGetGain) {
                    NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                    for (int i = 0; i < e.Length; i++) {
                        short iso;
                        if (short.TryParse(e.GetEnumValueByIndex(i).ToString(), out iso)) {
                            ISOSpeeds.Add(iso, i);
                            _gains.Add(iso);
                        }
                    }
                }
                return _gains;
            }
        }

        private AsyncObservableCollection<BinningMode> _binningModes;
        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (_binningModes == null) {
                    _binningModes = new AsyncObservableCollection<BinningMode>();
                    _binningModes.Add(new BinningMode(1, 1));
                }

                return _binningModes;
            }
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public void AbortExposure() {
            if (Connected) {
                _camera.StopBulbCapture();
            }
        }

        public void Disconnect() {
            Connected = false;
            _camera = null;
            _activeNikonManager?.Shutdown();
            _nikonManagers?.Clear();
            serialPortInteraction?.Close();
            serialPortInteraction = null;
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token) {
            Logger.Debug("Waiting for download of exposure");
            await _downloadExposure.Task;
            Logger.Debug("Downloading of exposure complete. Converting image to internal array");
            var iarr = await new DCRaw().ConvertToImageArray(_fileExtension, token);
            return iarr;
        }

        public void SetBinning(short x, short y) {

        }

        public void SetupDialog() {

        }

        private Dictionary<int, double> _shutterSpeeds = new Dictionary<int, double>();
        private int _bulbShutterSpeedIndex;

        public void StartExposure(double exposureTime, bool isLightFrame) {
            if (Connected) {
                Logger.Debug("Prepare start of exposure: " + exposureTime);
                _downloadExposure = new TaskCompletionSource<object>();

                var shutterspeed = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);

                if (Settings.CameraBulbMode == CameraBulbModeEnum.TELESCOPESNAPPORT) {
                    Logger.Debug("Use Telescope Snap Port");

                    BulbCapture(exposureTime, RequestSnapPortCaptureStart, RequestSnapPortCaptureStop);
                } else if (Settings.CameraBulbMode == CameraBulbModeEnum.SERIALPORT) {
                    Logger.Debug("Use Serial Port for camera");

                    BulbCapture(exposureTime, StartSerialPortCapture, StopSerialPortCapture);
                } else {
                    if (exposureTime <= 30.0) {
                        Logger.Debug("Exposuretime <= 30. Setting automatic shutter speed.");
                        var speed = _shutterSpeeds.Aggregate((x, y) => Math.Abs(x.Value - exposureTime) < Math.Abs(y.Value - exposureTime) ? x : y);
                        SetCameraShutterSpeed(speed.Key);

                        Logger.Debug("Start capture");
                        _camera.Capture();
                    } else {
                        Logger.Debug("Use Bulb capture");
                        BulbCapture(exposureTime, StartBulbCapture, StopBulbCapture);
                    }
                }
            }
        }

        private SerialPortInteraction serialPortInteraction;

        private void StartSerialPortCapture() {
            Logger.Debug("Serial port start of exposure");
            OpenSerialPort();
            serialPortInteraction.EnableRts(true);
        }

        private void StopSerialPortCapture() {
            Logger.Debug("Serial port stop of exposure");
            OpenSerialPort();
            serialPortInteraction.EnableRts(false);
        }

        private void OpenSerialPort() {
            if (serialPortInteraction?.PortName != Settings.CameraSerialPort) {
                serialPortInteraction = new SerialPortInteraction(Settings.CameraSerialPort);
            }
            if (!serialPortInteraction.Open()) {
                throw new Exception("Unable to open SerialPort " + Settings.CameraSerialPort);
            }
        }

        private void RequestSnapPortCaptureStart() {
            Logger.Debug("Request start of exposure");
            var success = Mediator.Instance.Request(new SendSnapPortMessage() { Start = true });
            if (!success) {
                throw new Exception("Request to telescope snap port failed");
            }
        }
        private void RequestSnapPortCaptureStop() {
            Logger.Debug("Request stop of exposure");
            var success = Mediator.Instance.Request(new SendSnapPortMessage() { Start = false });
            if (!success) {
                throw new Exception("Request to telescope snap port failed");
            }
        }

        private void BulbCapture(double exposureTime, Action capture, Action stopCapture) {
            

            SetCameraToManual();

            SetCameraShutterSpeed(_bulbShutterSpeedIndex);

            try {
                Logger.Debug("Starting bulb capture");
                capture();
            } catch (NikonException ex) {
                if (ex.ErrorCode != eNkMAIDResult.kNkMAIDResult_BulbReleaseBusy) {
                    throw;
                }
            }

            /*Stop Exposure after exposure time */
            Task.Run(async () => {
                await Utility.Utility.Wait(TimeSpan.FromSeconds(exposureTime));

                stopCapture();

                Logger.Debug("Restore previous shutter speed");
                // Restore original shutter speed
                SetCameraShutterSpeed(_prevShutterSpeed);                
            });
        }

        private void StartBulbCapture() {
            LockCamera(true);
            _camera.Capture();
        }

        private void StopBulbCapture() {
            LockCamera(false);
            Logger.Debug("Stopping Bulb Capture");
            // Terminate capture
            NkMAIDTerminateCapture terminate = new NkMAIDTerminateCapture();
            terminate.ulParameter1 = 0;
            terminate.ulParameter2 = 0;

            unsafe {
                IntPtr terminatePointer = new IntPtr(&terminate);

                _camera.Start(
                    eNkMAIDCapability.kNkMAIDCapability_TerminateCapture,
                    eNkMAIDDataType.kNkMAIDDataType_GenericPtr,
                    terminatePointer);
            }
        }

        private void LockCamera(bool lockIt) {
            Logger.Debug("Lock camera: " + lockIt);
            var lockCameraCap = eNkMAIDCapability.kNkMAIDCapability_LockCamera;
            _camera.SetBoolean(lockCameraCap, lockIt);
        }

        private void SetCameraToManual() {
            Logger.Debug("Set camera to manual exposure");
            if (Capabilities.ContainsKey(eNkMAIDCapability.kNkMAIDCapability_ExposureMode) && Capabilities[eNkMAIDCapability.kNkMAIDCapability_ExposureMode].CanSet()) {
                var exposureMode = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ExposureMode);
                var foundManual = false;
                for (int i = 0; i < exposureMode.Length; i++) {
                    if ((uint)exposureMode[i] == (uint)eNkMAIDExposureMode.kNkMAIDExposureMode_Manual) {
                        exposureMode.Index = i;
                        foundManual = true;
                        _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_ExposureMode, exposureMode);
                        break;
                    }
                }

                if (!foundManual) {
                    throw new NikonException("Failed to find the 'Manual' exposure mode");
                }
            } else {
                Logger.Debug("Cannot set to manual mode. Skipping...");
            }            
        }

        private int _prevShutterSpeed;

        private void SetCameraShutterSpeed(int index) {
            if (Capabilities.ContainsKey(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed) && Capabilities[eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed].CanSet()) {
                Logger.Debug("Setting shutter speed to index: " + index);
                var shutterspeed = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);
                _prevShutterSpeed = shutterspeed.Index;
                shutterspeed.Index = index;
                _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, shutterspeed);
            } else {
                Logger.Debug("Cannot set camera shutter speed. Skipping...");
            }
            
        }

        public void StopExposure() {
            if (Connected) {
                _camera.StopBulbCapture();
            }
        }

        public void UpdateValues() {
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                var connected = false;
                try {
                    serialPortInteraction = null;
                    _nikonManagers.Clear();

                    string folder = "x64";
                    if (DllLoader.IsX86()) {
                        folder = "x86";
                    }

                    foreach (string file in Directory.GetFiles(string.Format("External/{0}/Nikon", folder), "*.md3", SearchOption.AllDirectories)) {
                        NikonManager mgr = new NikonManager(file);
                        mgr.DeviceAdded += Mgr_DeviceAdded;
                        _nikonManagers.Add(mgr);
                    }

                    _cameraConnected = new TaskCompletionSource<object>();
                    var d = DateTime.Now;

                    do {
                        token.ThrowIfCancellationRequested();
                        Thread.Sleep(500);
                    } while (!_cameraConnected.Task.IsCompleted);
                    connected = true;
                } catch (OperationCanceledException) {
                    CleanupUnusedManagers(null);
                }
                return connected;
            });
        }
    }
}
