using NINA.Model.ImageData;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.RawConverter;
using NINA.Utility.WindowService;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NINA.Model.MyCamera {

    internal class FileCamera : BaseINPC, ICamera {

        public FileCamera(IProfileService profileService, ITelescopeMediator telescopeMediator) {
            OpenFolderDiagCommand = new RelayCommand(OpenFolderDiag);
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            CameraState = "Idle";
        }

        private void OpenFolderDiag(object obj) {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {
                dialog.SelectedPath = FolderPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    FolderPath = dialog.SelectedPath;
                }
            }
        }

        private object lockObj = new object();
        private ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();
        private FileSystemWatcher fileWatcher;
        public ICommand OpenFolderDiagCommand { get; }

        public string FolderPath {
            get => profileService.ActiveProfile.CameraSettings.FileCameraFolder;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraFolder = value;
                RaisePropertyChanged();
            }
        }

        public string Category { get; } = "N.I.N.A.";

        public bool HasShutter {
            get {
                return false;
            }
        }

        public bool Connected { get; private set; }

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
                return -1;
            }

            set {
            }
        }

        public short BinY {
            get {
                return -1;
            }

            set {
            }
        }

        public string Description {
            get {
                return Locale.Loc.Instance["LblFileCameraDescription"];
            }
        }

        public string DriverInfo {
            get {
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                return Utility.Utility.Version;
            }
        }

        public string SensorName {
            get {
                return "";
            }
        }

        public SensorType SensorType {
            get {
                return SensorType.Monochrome;
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
                return double.MaxValue;
            }
        }

        public double ElectronsPerADU => double.NaN;

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
                return 3.8;
            }
        }

        public double PixelSizeY {
            get {
                return 3.8;
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

        private string cameraState;

        public string CameraState {
            get {
                return cameraState;
            }
            set {
                cameraState = value;
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
                return false;
            }
        }

        public bool CanSetGain {
            get {
                return false;
            }
        }

        public short GainMax {
            get {
                return -1;
            }
        }

        public short GainMin {
            get {
                return -1;
            }
        }

        public short Gain {
            get {
                return -1;
            }

            set {
            }
        }

        public ArrayList Gains {
            get {
                return null;
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                return null;
            }
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Id {
            get {
                return "209D6981-1E09-438C-A1B6-7452F5C34A59";
            }
        }

        public string Name {
            get {
                return "N.I.N.A. File Camera";
            }
        }

        public double Temperature {
            get {
                return double.NaN;
            }
        }

        public double TemperatureSetPoint {
            get {
                return double.NaN;
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanSetTemperature {
            get {
                return false;
            }
        }

        public bool CanSubSample {
            get {
                return false;
            }
        }

        public bool EnableSubSample {
            get {
                return false;
            }

            set {
            }
        }

        public int SubSampleX {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleY {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleWidth {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleHeight {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanShowLiveView {
            get {
                return false;
            }
        }

        public bool LiveViewEnabled {
            get {
                return false;
            }
            set {
            }
        }

        public bool HasDewHeater {
            get {
                return false;
            }
        }

        public bool DewHeaterOn {
            get {
                return false;
            }

            set {
            }
        }

        public bool HasBattery {
            get {
                return false;
            }
        }

        public int BatteryLevel {
            get {
                return -1;
            }
        }

        public int BitDepth {
            get {
                return (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        public ICollection ReadoutModes {
            get {
                return new List<string>() { "Default" };
            }
        }

        public short ReadoutModeForSnapImages {
            get {
                return 0;
            }

            set {
            }
        }

        public short ReadoutModeForNormalImages {
            get {
                return 0;
            }

            set {
            }
        }

        public void AbortExposure() {
        }

        public Task<bool> Connect(CancellationToken token) {
            if (string.IsNullOrWhiteSpace(FolderPath)) {
                throw new Exception("No Folder for camera to watch was specified!");
            }

            fileQueue = new ConcurrentQueue<string>();

            if (fileWatcher != null) fileWatcher.Dispose();

            fileWatcher = new FileSystemWatcher() {
                Path = FolderPath,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.*",
                EnableRaisingEvents = false
            };

            fileWatcher.Created += FileWatcher_Created;
            fileWatcher.Renamed += FileWatcher_Renamed;

            fileWatcher.EnableRaisingEvents = true;

            Connected = true;
            return Task.FromResult(true);
        }

        private void FileWatcher_Renamed(object sender, RenamedEventArgs e) {
            Logger.Trace($"File renaming detected. New file path {e.FullPath} - old file path {e.OldFullPath}");

            lock (lockObj) {
                var list = fileQueue.ToList();
                if (list.Contains(e.OldFullPath)) {
                    list.Remove(e.OldFullPath);
                }
                fileQueue = new ConcurrentQueue<string>(list);
                AddQueueItem(e.FullPath);
            }
        }

        private void AddQueueItem(string path) {
            lock (lockObj) {
                var fileExt = Path.GetExtension(path).ToLower();
                if (Regex.IsMatch(fileExt, @"\.tiff|\.tif|\.png|\.gif|\.jpg|\.jpeg|\.png|\.cr2|\.nef|\.raw|\.raf|\.xisf|\.fit|\.fits")) {
                    Logger.Trace($"Added file to Queue at {path}");
                    fileQueue.Enqueue(path);
                } else {
                    Logger.Trace($"Invalid file for Queue at {path}");
                }
            }
        }

        private void FileWatcher_Created(object sender, FileSystemEventArgs e) {
            Logger.Trace($"New file detected at {e.FullPath}");
            AddQueueItem(e.FullPath);
        }

        public void Disconnect() {
            fileWatcher.EnableRaisingEvents = false;

            Connected = false;
        }

        public async Task<IImageData> DownloadExposure(CancellationToken token) {
            try {
                while (fileQueue.Count == 0) {
                    CameraState = "Waiting for file";
                    await Utility.Utility.Wait(TimeSpan.FromSeconds(1), token);
                }
                if (fileQueue.TryDequeue(out var path)) {
                    CameraState = "Loading from file";
                    var tries = 0;
                    while (true) {
                        tries++;
                        try {
                            var image = await ImageData.ImageData.FromFile(path, BitDepth, IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter, token);
                            return image;
                        } catch (Exception ex) {
                            if (tries > 3) {
                                throw ex;
                            }
                            await Utility.Utility.Wait(TimeSpan.FromSeconds(1), token);
                        }
                    }
                } else {
                    throw new Exception($"Unable to load file {path}");
                }
            } finally {
                CameraState = "Idle";
            }
        }

        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;

        public void SetBinning(short x, short y) {
        }

        private IWindowService windowService;

        public IWindowService WindowService {
            get {
                if (windowService == null) {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set {
                windowService = value;
            }
        }

        public void SetupDialog() {
            WindowService.ShowDialog(this, "File Camera Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public bool IsBayered {
            get => profileService.ActiveProfile.CameraSettings.FileCameraIsBayered;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraIsBayered = value;
                RaisePropertyChanged();
            }
        }

        public bool UseBulbMode {
            get => profileService.ActiveProfile.CameraSettings.FileCameraUseBulbMode;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraUseBulbMode = value;
                RaisePropertyChanged();
            }
        }

        public void StartExposure(CaptureSequence captureSequence) {
            if (UseBulbMode) {
                var exposureTime = captureSequence.ExposureTime;
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
                    throw new NotSupportedException("The file camera does not support the selected BulbMode");
                }
            }
        }

        public void StopExposure() {
        }

        private void BulbCapture(double exposureTime, Action capture, Action stopCapture) {
            Logger.Debug("Starting bulb capture");
            capture();

            /*Stop Exposure after exposure time */
            Task.Run(async () => {
                await Utility.Utility.Wait(TimeSpan.FromSeconds(exposureTime));

                stopCapture();

                Logger.Debug("Restore previous shutter speed");
            });
        }

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

        private SerialPortInteraction serialPortInteraction;
        private SerialRelayInteraction serialRelayInteraction;

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

        public void StartLiveView() {
        }

        public Task<IImageData> DownloadLiveView(CancellationToken token) {
            return null;
        }

        public void StopLiveView() {
        }
    }
}