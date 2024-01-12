#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Nikon;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Image.ImageData;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Model.Equipment;
using NINA.Image.RawConverter;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;

namespace NINA.Equipment.Equipment.MyCamera {

    public class NikonCamera : BaseINPC, ICamera {

        public NikonCamera(IProfileService profileService, ITelescopeMediator telescopeMediator, IExposureDataFactory exposureDataFactory) {
            this.telescopeMediator = telescopeMediator;
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            /* NIKON */
            Name = "Nikon";
            _nikonManagers = new List<NikonManager>();
        }

        public string Category { get; } = "Nikon";

        private ITelescopeMediator telescopeMediator;
        private IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;
        private List<NikonManager> _nikonManagers;
        private NikonManager _activeNikonManager;

        private void Mgr_DeviceRemoved(NikonManager sender, NikonDevice device) {
            Disconnect();
        }

        private void Mgr_DeviceAdded(NikonManager sender, NikonDevice device) {
            var connected = false;
            try {
                _activeNikonManager = sender;
                _activeNikonManager.DeviceRemoved += Mgr_DeviceRemoved;

                Init(device);

                connected = true;
                Name = _camera.Name;
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
                Logger.Error(ex);
            } finally {
                Connected = connected;
                RaiseAllPropertiesChanged();
                _cameraConnected.TrySetResult(connected);
            }
        }

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get => _liveViewEnabled;
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        public int BitDepth => (int)profileService.ActiveProfile.CameraSettings.BitDepth;

        public void StartLiveView(CaptureSequence sequence) {
            _camera.LiveViewEnabled = true;
            LiveViewEnabled = true;
        }

        public void StopLiveView() {
            _camera.LiveViewEnabled = false;
            LiveViewEnabled = false;
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return Task.Run<IExposureData>(() => {
                byte[] buffer = _camera.GetLiveViewImage().JpegBuffer;
                using (var memStream = new MemoryStream(buffer)) {
                    memStream.Position = 0;

                    JpegBitmapDecoder decoder = new JpegBitmapDecoder(memStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);

                    FormatConvertedBitmap bitmap = new FormatConvertedBitmap();
                    bitmap.BeginInit();
                    bitmap.Source = decoder.Frames[0];
                    bitmap.DestinationFormat = System.Windows.Media.PixelFormats.Gray16;
                    bitmap.EndInit();

                    ushort[] outArray = new ushort[bitmap.PixelWidth * bitmap.PixelHeight];
                    bitmap.CopyPixels(outArray, 2 * bitmap.PixelWidth, 0);

                    var metaData = new ImageMetaData();
                    metaData.FromCamera(this);
                    return exposureDataFactory.CreateImageArrayExposureData(
                            input: outArray,
                            width: bitmap.PixelWidth,
                            height: bitmap.PixelHeight,
                            bitDepth: 16,
                            isBayered: false,
                            metaData: metaData);
                }
            });
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

            GetCapabilities();

            if (Capabilities.TryGetValue(eNkMAIDCapability.kNkMAIDCapability_CompressionLevel, out var compressionCapability)) {
                if (compressionCapability.CanGet() && compressionCapability.CanSet()) {
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
                } else {
                    Logger.Trace($"Cannot set compression level: CanGet {compressionCapability.CanGet()} - CanSet {compressionCapability.CanSet()}");
                }
            } else {
                Logger.Trace("Compression Level capability not available");
            }

            GetShutterSpeeds();

            /* Setting SaveMedia when supported, to save images via SDRAM and not to the internal memory card */
            if (Capabilities.ContainsKey(eNkMAIDCapability.kNkMAIDCapability_SaveMedia) && Capabilities[eNkMAIDCapability.kNkMAIDCapability_SaveMedia].CanSet()) {
                _camera.SetUnsigned(eNkMAIDCapability.kNkMAIDCapability_SaveMedia, (uint)eNkMAIDSaveMedia.kNkMAIDSaveMedia_SDRAM);
            } else {
                Logger.Trace("Setting SaveMedia Capability not available. This has to be set manually or is not supported by this model.");
            }
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
                    throw new NikonException("Cannot set shutterspeeds. Please make sure the camera dial is set to a position where bulb mode is possible and the mirror lock is turned off");
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
                    } else if (val.ToLower() == "time") {
                        //currently unused
                    } else {
                        _shutterSpeeds.Add(i, double.Parse(val, CultureInfo.InvariantCulture));
                    }
                } catch (Exception ex) {
                    Logger.Error("Unexpected Shutter Speed: ", ex);
                }
            }
            if (!bulbFound) {
                Logger.Error("No Bulb speed found!");
                throw new NikonException("Failed to find the 'Bulb' exposure mode");
            }
        }

        private TaskCompletionSource<object> _downloadExposure;
        private TaskCompletionSource<bool> _cameraConnected;

        private void _camera_CaptureComplete(NikonDevice sender, int data) {
            Logger.Debug("Capture complete");
        }

        private void Camera_ImageReady(NikonDevice sender, NikonImage image) {
            Logger.Debug("Image ready");
            _memoryStream = new MemoryStream(image.Buffer);
            Logger.Debug("Setting Download Exposure Task to complete");
            _downloadExposure.TrySetResult(null);
        }

        private NikonDevice _camera;

        public string Id => "Nikon";

        private string _name;

        public string Name {
            get => _name;
            private set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public bool CanShowLiveView => _camera.SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_GetLiveViewImage);

        public string Description {
            get {
                if (Connected) {
                    return _camera.Name;
                } else {
                    return string.Empty;
                }
            }
        }

        public bool HasShutter => true;

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public double Temperature => double.NaN;

        public double TemperatureSetPoint {
            get => double.NaN;

            set {
            }
        }

        public short BinX {
            get => 1;

            set {
            }
        }

        public short BinY {
            get => 1;
            set {
            }
        }

        public string DriverInfo => string.Empty;

        public string DriverVersion => string.Empty;

        public string SensorName => string.Empty;

        public SensorType SensorType => SensorType.RGGB;

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        public int CameraXSize => -1;

        public int CameraYSize => -1;

        public double ExposureMin => 0;

        public double ExposureMax => double.PositiveInfinity;

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX => 1;

        public short MaxBinY => 1;

        public double PixelSizeX => double.NaN;

        public double PixelSizeY => double.NaN;

        public bool CanSetTemperature => false;

        public bool CoolerOn {
            get => false;
            set {
            }
        }

        public double CoolerPower => double.NaN;

        public bool HasDewHeater => false;

        public bool DewHeaterOn {
            get => false;
            set {
            }
        }

        public CameraStates CameraState => CameraStates.NoState;

        public int Offset {
            get => -1;
            set {
            }
        }

        public int USBLimit {
            get => -1;
            set {
            }
        }

        public int USBLimitMax => -1;
        public int USBLimitMin => -1;
        public int USBLimitStep => -1;

        public IList<string> SupportedActions => new List<string>();

        public bool CanSetOffset => false;

        public int OffsetMin => 0;

        public int OffsetMax => 0;

        public bool CanSetUSBLimit => false;

        public bool CanSubSample => false;

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

        public int GainMax {
            get {
                if (Gains != null) {
                    return ISOSpeeds.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                } else {
                    return 0;
                }
            }
        }

        public int GainMin {
            get {
                if (Gains != null) {
                    return ISOSpeeds.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                } else {
                    return 0;
                }
            }
        }

        public int Gain {
            get {
                if (Connected) {
                    NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                    int iso;
                    if (int.TryParse(e.Value.ToString(), out iso)) {
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

        private Dictionary<int, int> _isoSpeeds = default;

        private Dictionary<int, int> ISOSpeeds {
            get {
                if (_isoSpeeds != default(Dictionary<int, int>)) return _isoSpeeds;
                _isoSpeeds = new Dictionary<int, int>();
                NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                for (int i = 0; i < e.Length; i++) {
                    if (int.TryParse(e.GetEnumValueByIndex(i).ToString(), out int iso)) {
                        _isoSpeeds.Add(iso, i);
                    }
                }

                return _isoSpeeds;
            }
        }

        private IList<int> _gains;

        public IList<int> Gains {
            get {
                if (_gains == null) {
                    _gains = new List<int>();
                }

                if (_gains.Count == 0 && Connected && CanGetGain) {
                    NikonEnum e = _camera.GetEnum(eNkMAIDCapability.kNkMAIDCapability_Sensitivity);
                    for (int i = 0; i < e.Length; i++) {
                        int iso;
                        if (int.TryParse(e.GetEnumValueByIndex(i).ToString(), out iso)) {
                            _gains.Add(iso);
                        }
                    }
                }
                return _gains;
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

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                _readoutModeForNormalImages = value;
                RaisePropertyChanged();
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

        public bool HasSetupDialog => false;

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        public int BatteryLevel {
            get {
                try {
                    return _camera.GetInteger(eNkMAIDCapability.kNkMAIDCapability_BatteryLevel);
                } catch (NikonException ex) {
                    Logger.Error(ex);
                    return -1;
                }
            }
        }

        public bool HasBattery => true;

        public void AbortExposure() {
            if (Connected) {
                _camera.StopBulbCapture();
            }
        }

        public void Disconnect() {
            Connected = false;
            _camera = null;
            _activeNikonManager?.Shutdown();
            _activeNikonManager = null;
            CleanupUnusedManagers(null);
            serialPortInteraction?.Close();
            serialPortInteraction?.Dispose();
            serialPortInteraction = null;
            serialRelayInteraction?.Dispose();
            serialRelayInteraction = null;
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                await _downloadExposure.Task;
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            if (_downloadExposure.Task.IsCanceled) { return null; }
            Logger.Debug("Waiting for download of exposure");
            await _downloadExposure.Task;
            Logger.Debug("Downloading of exposure complete. Converting image to internal array");

            try {
                var rawImageData = _memoryStream.ToArray();
                var metaData = new ImageMetaData();
                metaData.FromCamera(this);
                return exposureDataFactory.CreateRAWExposureData(
                    converter: profileService.ActiveProfile.CameraSettings.RawConverter,
                    rawBytes: rawImageData,
                    rawType: "nef",
                    bitDepth: this.BitDepth,
                    metaData: metaData);
            } finally {
                if (_memoryStream != null) {
                    _memoryStream.Dispose();
                    _memoryStream = null;
                }
            }
        }

        public void SetBinning(short x, short y) {
        }

        public void SetupDialog() {
        }

        private Dictionary<int, double> _shutterSpeeds = new Dictionary<int, double>();
        private int _bulbShutterSpeedIndex;

        public void StartExposure(CaptureSequence sequence) {
            if (Connected) {
                if (_downloadExposure != null && _downloadExposure.Task.Status <= TaskStatus.Running) {
                    Notification.ShowWarning("Another exposure still in progress. Cancelling it to start another.");
                    Logger.Warning("An exposure was still in progress. Cancelling it to start another.");
                    try { bulbCompletionCTS?.Cancel(); } catch { }
                    _downloadExposure.TrySetCanceled();
                }

                double exposureTime = sequence.ExposureTime;
                Logger.Debug("Prepare start of exposure: " + sequence);
                _downloadExposure = new TaskCompletionSource<object>();

                if (exposureTime <= 30.0) {
                    Logger.Debug("Exposuretime <= 30. Setting automatic shutter speed.");
                    var speed = _shutterSpeeds.Aggregate((x, y) => Math.Abs(x.Value - exposureTime) < Math.Abs(y.Value - exposureTime) ? x : y);
                    SetCameraShutterSpeed(speed.Key);

                    Logger.Debug("Start capture");
                    Task.Run(() => _camera.Capture());
                } else {
                    if (profileService.ActiveProfile.CameraSettings.BulbMode == CameraBulbModeEnum.TELESCOPESNAPPORT) {
                        Logger.Debug("Use Telescope Snap Port");

                        BulbCapture(exposureTime, RequestSnapPortCaptureStart, RequestSnapPortCaptureStop);
                    } else if (profileService.ActiveProfile.CameraSettings.BulbMode == CameraBulbModeEnum.SERIALPORT) {
                        Logger.Debug("Use Serial Port for camera");

                        BulbCapture(exposureTime, StartSerialPortCapture, StopSerialPortCapture);
                    } else if (profileService.ActiveProfile.CameraSettings.BulbMode == CameraBulbModeEnum.SERIALRELAY) {
                        Logger.Debug("Use serial relay for camera");

                        BulbCapture(exposureTime, StartSerialRelayCapture, StopSerialRelayCapture);
                    } else {
                        Logger.Debug("Use Bulb capture");
                        BulbCapture(exposureTime, StartBulbCapture, StopBulbCapture);
                    }
                }
            }
        }

        private SerialPortInteraction serialPortInteraction;
        private SerialRelayInteraction serialRelayInteraction;

        private void StartSerialRelayCapture() {
            Logger.Debug("Serial relay start of exposure");
            OpenSerialRelay();
            serialRelayInteraction.Send(new byte[] { 0xFF, 0x01, 0x01 });
        }

        private void StopSerialRelayCapture() {
            Logger.Debug("Serial relay stop of exposure");
            OpenSerialRelay();
            serialRelayInteraction.Send(new byte[] { 0xFF, 0x01, 0x00 });
        }

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
            if (serialPortInteraction?.PortName != profileService.ActiveProfile.CameraSettings.SerialPort) {
                serialPortInteraction = new SerialPortInteraction(profileService.ActiveProfile.CameraSettings.SerialPort);
            }
            if (!serialPortInteraction.Open()) {
                throw new Exception("Unable to open SerialPort " + profileService.ActiveProfile.CameraSettings.SerialPort);
            }
        }

        private void OpenSerialRelay() {
            if (serialRelayInteraction?.PortName != profileService.ActiveProfile.CameraSettings.SerialPort) {
                serialRelayInteraction = new SerialRelayInteraction(profileService.ActiveProfile.CameraSettings.SerialPort);
            }
            if (!serialRelayInteraction.Open()) {
                throw new Exception("Unable to open SerialPort " + profileService.ActiveProfile.CameraSettings.SerialPort);
            }
        }

        private void RequestSnapPortCaptureStart() {
            Logger.Debug("Request start of exposure");
            var success = telescopeMediator.SendToSnapPort(true);
            if (!success) {
                throw new Exception("Request to telescope snap port failed");
            }
        }

        private void RequestSnapPortCaptureStop() {
            Logger.Debug("Request stop of exposure");
            var success = telescopeMediator.SendToSnapPort(false);
            if (!success) {
                throw new Exception("Request to telescope snap port failed");
            }
        }

        private Task bulbCompletionTask = null;
        private CancellationTokenSource bulbCompletionCTS = null;

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

            /*Stop Exposure after exposure time or upon cancellation*/
            try { bulbCompletionCTS?.Cancel(); } catch { }
            bulbCompletionCTS = new CancellationTokenSource();
            bulbCompletionTask = Task.Run(async () => {
                await CoreUtil.Wait(TimeSpan.FromSeconds(exposureTime), bulbCompletionCTS.Token);
                if (!bulbCompletionCTS.IsCancellationRequested) {
                    stopCapture();
                    Logger.Debug("Restore previous shutter speed");
                    // Restore original shutter speed
                    SetCameraShutterSpeed(_prevShutterSpeed);
                }
            }, bulbCompletionCTS.Token);
        }

        private void StartBulbCapture() {
            _camera.StartBulbCapture();
        }

        private void StopBulbCapture() {
            _camera.StopBulbCapture();
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
        private MemoryStream _memoryStream;

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

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                var connected = false;
                try {
                    serialPortInteraction = null;
                    _nikonManagers.Clear();

                    string architecture = "x64";
                    if (DllLoader.IsX86()) {
                        architecture = "x86";
                    }

                    var md3Folder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "External", architecture, "Nikon");

                    foreach (string file in Directory.GetFiles(md3Folder, "*.md3", SearchOption.AllDirectories)) {
                        NikonManager mgr = new NikonManager(file);
                        mgr.DeviceAdded += Mgr_DeviceAdded;
                        _nikonManagers.Add(mgr);
                    }

                    _cameraConnected = new TaskCompletionSource<bool>();
                    var d = DateTime.Now;

                    do {
                        token.ThrowIfCancellationRequested();
                        Thread.Sleep(500);
                    } while (!_cameraConnected.Task.IsCompleted);

                    connected = _cameraConnected.Task.Result;
                } catch (OperationCanceledException) {
                    _activeNikonManager = null;
                } finally {
                    CleanupUnusedManagers(_activeNikonManager);
                }

                Connected = connected;
                return connected;
            });
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
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
}