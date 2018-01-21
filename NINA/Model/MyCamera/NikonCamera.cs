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
                Logger.Error(ex.Message, ex.StackTrace);
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
            Logger.Trace("Initializing Nikon camera");
            _camera = cam;
            _camera.ImageReady += Camera_ImageReady;
            _camera.CaptureComplete += _camera_CaptureComplete;


            //Set to shoot in RAW
            Logger.Trace("Setting compression to RAW");
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
            Logger.Trace("Getting Nikon capabilities");
            foreach (NkMAIDCapInfo info in _camera.GetCapabilityInfo()) {
                var description = info.GetDescription();
                var canGet = info.CanGet();
                var canGetArray = info.CanGetArray();
                var canSet = info.CanSet();

                Logger.Trace(description);
                Logger.Trace("\t CanGet: " + canGet.ToString());
                Logger.Trace("\t CanGetArray: " + canGetArray.ToString());
                Logger.Trace("\t CanSet: " + canSet.ToString());
                
            }
        }


        private void GetShutterSpeeds() {
            Logger.Trace("Getting Nikon shutter speeds");
            _shutterSpeeds.Clear();
            var shutterSpeeds = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);
            for (int i = 0; i < shutterSpeeds.Length; i++) {
                try {
                    var val = shutterSpeeds.GetEnumValueByIndex(i).ToString();
                    Logger.Trace("Found Shutter speed: " + val);
                    if (val.Contains("/")) {
                        var split = val.Split('/');
                        var convertedSpeed = double.Parse(split[0], CultureInfo.InvariantCulture) / double.Parse(split[1], CultureInfo.InvariantCulture);

                        _shutterSpeeds.Add(i, convertedSpeed);
                    } else if (val == "Bulb") {
                        _bulbShutterSpeedIndex = i;
                    }
                } catch (Exception ex) {
                    Logger.Error("Unexpected Shutter Speed: " + ex.Message, ex.StackTrace);
                }
            }
        }

        private TaskCompletionSource<object> _downloadExposure;
        private TaskCompletionSource<object> _cameraConnected;

        private void _camera_CaptureComplete(NikonDevice sender, int data) {
            _downloadExposure.TrySetResult(null);
        }

        private string _fileExtension;

        private void Camera_ImageReady(NikonDevice sender, NikonImage image) {
            _fileExtension = (image.Type == NikonImageType.Jpeg) ? ".jpg" : ".nef";
            string filename = DCRaw.TMPIMGFILEPATH + _fileExtension;

            using (System.IO.FileStream s = new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                s.Write(image.Buffer, 0, image.Buffer.Length);
            }
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

        [System.Obsolete("Use async Connect")]
        public bool Connect() {
            _nikonManagers.Clear();
            foreach (string file in Directory.GetFiles("External/Nikon", "*.md3")) {
                NikonManager mgr = new NikonManager(file);
                mgr.DeviceAdded += Mgr_DeviceAdded;
                _nikonManagers.Add(mgr);
            }

            _cameraConnected = new TaskCompletionSource<object>();
            var d = DateTime.Now;
            //Wait maximum 30 seconds for a camera to connect;
            do {
                if (_cameraConnected.Task.IsCompleted) {
                    break;
                }
                Thread.Sleep(500);
            } while ((DateTime.Now - d).TotalMilliseconds < TimeSpan.FromSeconds(20).TotalMilliseconds);

            if (!_cameraConnected.Task.IsCompleted) {
                CleanupUnusedManagers(null);
                Notification.ShowError(Locale.Loc.Instance["LblNikonNotFound"]);
                return false;
            }

            return true;
        }

        public void Disconnect() {
            Connected = false;
            _camera = null;
            _activeNikonManager?.Shutdown();
            _nikonManagers?.Clear();
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token) {
            await _downloadExposure.Task;
            Logger.Trace("Downloading of exposure complete. Converting image to internal array");
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
                Logger.Trace("Prepare start of exposure");
                _downloadExposure = new TaskCompletionSource<object>();

                var shutterspeed = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);

                if (Settings.UseTelescopeSnapPort) {
                    shutterspeed.Index = _bulbShutterSpeedIndex;
                    _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, shutterspeed);


                    var success = Mediator.Instance.Request(new SendSnapPortMessage() { Start = true });
                    if(!success) { return; }
                    DateTime d = DateTime.Now;

                    /*Stop Exposure after exposure time */
                    Task.Run(() => {
                        exposureTime = exposureTime * 1000;
                        do {
                            Thread.Sleep(100);
                        } while ((DateTime.Now - d).TotalMilliseconds < exposureTime);

                        Mediator.Instance.Request(new SendSnapPortMessage() { Start = false });
                    });

                } else {


                    if (exposureTime < 1.0) {
                        Logger.Trace("Exposuretime < 1. Setting automatic shutter speed.");
                        var speed = _shutterSpeeds.Aggregate((x, y) => Math.Abs(x.Value - exposureTime) < Math.Abs(y.Value - exposureTime) ? x : y);

                        shutterspeed.Index = speed.Key;
                        Logger.Trace("Setting key to: " + speed.Key.ToString());
                        _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, shutterspeed);
                        Logger.Trace("Start Capture");
                        _camera.Capture();
                    } else {
                        //Set Camera to bulb
                        Logger.Trace("Exposuretime > 1. Setting to bulb mode.");
                        shutterspeed.Index = _bulbShutterSpeedIndex;
                        Logger.Trace("Setting key to: " + _bulbShutterSpeedIndex.ToString());
                        _camera.SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, shutterspeed);

                        DateTime d = DateTime.Now;
                        Logger.Trace("Start Bulb Capture");
                        _camera.StartBulbCapture();

                        /*Stop Exposure after exposure time */
                        Task.Run(async () => {
                            exposureTime = exposureTime * 1000;
                            var elapsed = 0.0d;
                            do {
                                var delta = await Utility.Utility.Delay(100, new CancellationToken());
                                elapsed += delta.TotalMilliseconds;
                            } while (elapsed < exposureTime);
                            Logger.Trace("Stop Bulb Capture");
                            _camera.StopBulbCapture();
                        });
                    }
                }
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
                    _nikonManagers.Clear();

                    string folder = "x64";
                    if(DllLoader.IsX86()) {
                        folder = "x86";
                    }

                    foreach (string file in Directory.GetFiles(string.Format("External/{0}/Nikon", folder), "*.md3")) {
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
