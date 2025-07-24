#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Image.ImageData;
using NINA.Equipment.Interfaces;
using Microsoft.Win32;

namespace NINA.Equipment.Equipment.MyCamera {

    public class FileCamera : BaseINPC, ICamera {

        public FileCamera(IProfileService profileService, ITelescopeMediator telescopeMediator, IImageDataFactory imageDataFactory, IExposureDataFactory exposureDataFactory) {
            OpenFolderDiagCommand = new RelayCommand(OpenFolderDiag);
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.imageDataFactory = imageDataFactory;
            this.exposureDataFactory = exposureDataFactory;
            CameraState = CameraStates.Idle;
            SelectedFileExtension = FileExtensions.FirstOrDefault(x => x.Name == profileService.ActiveProfile.CameraSettings.FileCameraExtension) ?? FileExtensions.First();
        }

        private void OpenFolderDiag(object obj) {
            var dialog = new OpenFolderDialog();
            dialog.InitialDirectory = FolderPath;

            if (dialog.ShowDialog() == true) {
                FolderPath = dialog.FolderName;
            }
        }

        public ICommand OpenFolderDiagCommand { get; }
        public FileCameraFolderWatcher folderWatcher;

        public string FolderPath {
            get => profileService.ActiveProfile.CameraSettings.FileCameraFolder;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraFolder = value;
                RaisePropertyChanged();
            }
        }

        public string Category { get; } = "N.I.N.A.";

        public bool HasShutter => false;

        public bool Connected { get; private set; }

        public double CCDTemperature => double.NaN;

        public double SetCCDTemperature {
            get => double.NaN;

            set {
            }
        }

        public short BinX {
            get => -1;

            set {
            }
        }

        public short BinY {
            get => -1;

            set {
            }
        }

        public string Description => Loc.Instance["LblFileCameraDescription"];

        public string DriverInfo => string.Empty;

        public string DriverVersion => CoreUtil.Version;

        public string SensorName => "";

        public SensorType SensorType => SensorType.Monochrome;

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        public int CameraXSize => -1;

        public int CameraYSize => -1;

        public double ExposureMin => 0;

        public double ExposureMax => double.MaxValue;

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX => 1;

        public short MaxBinY => 1;

        public double PixelSizeX => profileService.ActiveProfile.CameraSettings.PixelSize;

        public double PixelSizeY => profileService.ActiveProfile.CameraSettings.PixelSize;

        public string LensName => string.Empty;

        public bool CanSetCCDTemperature => false;

        public bool CoolerOn {
            get => false;

            set {
            }
        }

        public double CoolerPower => double.NaN;

        private CameraStates cameraState;

        public CameraStates CameraState {
            get => cameraState;
            set {
                cameraState = value;
                RaisePropertyChanged();
            }
        }

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

        public bool CanGetGain => false;

        public bool CanSetGain => false;

        public int GainMax => -1;

        public int GainMin => -1;

        public int Gain {
            get => -1;

            set {
            }
        }

        public IList<int> Gains => new List<int>();

        private AsyncObservableCollection<BinningMode> binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (binningModes == null) {
                    binningModes = new AsyncObservableCollection<BinningMode> {
                        new BinningMode(1,1)
                    };
                }
                return binningModes;
            }
        }

        public bool HasSetupDialog => true;

        public string Id => "209D6981-1E09-438C-A1B6-7452F5C34A59";

        public string Name => "N.I.N.A. File Camera";
        public string DisplayName => Name;

        public double Temperature => double.NaN;

        public double TemperatureSetPoint {
            get => double.NaN;

            set => throw new NotImplementedException();
        }

        public bool CanSetTemperature => false;

        public bool CanSubSample => false;

        public bool EnableSubSample {
            get => false;

            set {
            }
        }

        public int SubSampleX { get; set; }

        public int SubSampleY { get; set; }

        public int SubSampleWidth { get; set; }

        public int SubSampleHeight { get; set; }

        public bool CanShowLiveView => false;

        public bool LiveViewEnabled {
            get => false;
            set {
            }
        }

        public bool HasDewHeater => false;

        public bool DewHeaterOn {
            get => false;

            set {
            }
        }

        public bool HasBattery => false;

        public int BatteryLevel => -1;

        public int BitDepth => (int)profileService.ActiveProfile.CameraSettings.BitDepth;

        public IList<string> ReadoutModes => new List<string> { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

        public short ReadoutModeForSnapImages {
            get => 0;

            set {
            }
        }

        public short ReadoutModeForNormalImages {
            get => 0;

            set {
            }
        }

        public void AbortExposure() {
            if (!AlwaysListen) {
                folderWatcher.Suspend();
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            folderWatcher = new FileCameraFolderWatcher(FolderPath, SelectedFileExtension);
            if (AlwaysListen) {
                folderWatcher.Start();
            }
            Connected = true;
            return Task.FromResult(true);
        }

        public FileExtension selectedFileExtension;

        public FileExtension SelectedFileExtension {
            get => selectedFileExtension;
            set {
                selectedFileExtension = value;
                profileService.ActiveProfile.CameraSettings.FileCameraExtension = selectedFileExtension.Name;
                RaisePropertyChanged();
            }
        }

        public ICollection<FileExtension> FileExtensions { get; } = new List<FileExtension>() {
            new FileExtension ("ALL", @"\.tiff|\.tif|\.png|\.gif|\.jpg|\.jpeg|\.png|\.cr2|\.cr3|\.nef|\.raw|\.raf|\.xisf|\.fit|\.fits|\.fts|\.pef|\.dng|\.arw|\.orf"),
            new FileExtension ("Canon", @"\.cr2|\.cr3"),
            new FileExtension ("NEF", @"\.nef"),
            new FileExtension ("RAW", @"\.raw"),
            new FileExtension ("RAF", @"\.raf"),
            new FileExtension ("PEF", @"\.pef"),
            new FileExtension ("DNG", @"\.dng"),
            new FileExtension ("ARW", @"\.arw"),
            new FileExtension ("ORF", @"\.orf"),
            new FileExtension ("TIFF", @"\.tiff|\.tif"),
            new FileExtension ("PNG", @"\.png"),
            new FileExtension ("JPG", @"\.jpg|\.jpeg"),
            new FileExtension ("GIF", @"\.gif"),
            new FileExtension ("XISF", @"\.xisf"),
            new FileExtension ("FITS", @"\.fit|\.fits|\.fts"),
        };

        public void Disconnect() {
            if (AlwaysListen) {
                folderWatcher.Suspend();
            }
            folderWatcher?.Dispose();
            serialPortInteraction?.Dispose();
            serialRelayInteraction?.Dispose();
            serialPortInteraction = null;
            serialRelayInteraction = null;
            Connected = false;
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                var remaining = exposureTime - (DateTime.Now - exposureStart);
                await Task.Delay(remaining, token);
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            try {
                string path;
                while ((path = folderWatcher.GetNextItem()) == null) {
                    CameraState = CameraStates.Waiting;
                    await CoreUtil.Wait(TimeSpan.FromSeconds(1), token);
                }

                if (!AlwaysListen) {
                    folderWatcher.Suspend();
                }

                // Delay download for certain DSLR cameras
                await CoreUtil.Wait(TimeSpan.FromSeconds(DownloadDelay), token);

                CameraState = CameraStates.LoadingFile;
                var tries = 0;
                while (true) {
                    tries++;
                    try {
                        var image = await imageDataFactory.CreateFromFile(path, BitDepth, IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter, token);
                        return exposureDataFactory.CreateCachedExposureData(image);
                    } catch (Exception ex) {
                        if (tries > 3) {
                            Logger.Error(ex);
                            throw;
                        }
                        await CoreUtil.Wait(TimeSpan.FromSeconds(1), token);
                    }
                }
            } finally {
                CameraState = CameraStates.LoadingFile;
            }
        }

        public int DownloadDelay {
            get => profileService.ActiveProfile.CameraSettings.FileCameraDownloadDelay;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraDownloadDelay = value;
                RaisePropertyChanged();
            }
        }

        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;
        private readonly IImageDataFactory imageDataFactory;
        private readonly IExposureDataFactory exposureDataFactory;

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
            set => windowService = value;
        }

        public void SetupDialog() {
            var task = WindowService.ShowDialog(this, "File Camera Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
            task.Task.ContinueWith(t => {
                if (Connected) {
                    folderWatcher?.Dispose();
                    folderWatcher = new FileCameraFolderWatcher(FolderPath, SelectedFileExtension);
                    if (AlwaysListen) {
                        folderWatcher.Start();
                    }
                }
            });
        }

        public bool IsBayered {
            get => profileService.ActiveProfile.CameraSettings.FileCameraIsBayered;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraIsBayered = value;
                RaisePropertyChanged();
            }
        }

        public bool AlwaysListen {
            get => profileService.ActiveProfile.CameraSettings.FileCameraAlwaysListen;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraAlwaysListen = value;
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

        private DateTime exposureStart;
        private TimeSpan exposureTime;

        public void StartExposure(CaptureSequence captureSequence) {
            if (!AlwaysListen) {
                folderWatcher.Start();
            }
            exposureStart = DateTime.Now;
            exposureTime = TimeSpan.FromSeconds(captureSequence.ExposureTime);
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
            if (!AlwaysListen) {
                try {
                    folderWatcher.Suspend();
                    try { bulbCompletionCTS?.Cancel(); } catch { }
                } catch (Exception) { };
            }
        }

        private CancellationTokenSource bulbCompletionCTS = null;

        private void BulbCapture(double exposureTime, Action capture, Action stopCapture) {
            Logger.Debug("Starting bulb capture");
            capture();

            /**Stop Exposure after exposure time or upon cancellation*/
            try { bulbCompletionCTS?.Cancel(); } catch { }
            bulbCompletionCTS = new CancellationTokenSource();
            Task.Run(async () => {
                await CoreUtil.Wait(TimeSpan.FromSeconds(exposureTime), bulbCompletionCTS.Token);
                if (!bulbCompletionCTS.IsCancellationRequested) {
                    stopCapture();
                }
            }, bulbCompletionCTS.Token);
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

        public void StartLiveView(CaptureSequence sequence) {
            throw new System.NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new System.NotImplementedException();
        }

        public void StopLiveView() {
            throw new System.NotImplementedException();
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

        public void UpdateSubSampleArea() {
            throw new NotImplementedException();
        }
    }

    public class FileCameraFolderWatcher : IDisposable {
        private ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();
        private FileSystemWatcher fileWatcher;
        private string watchedFolder;
        private FileExtension fileExtension;
        private object lockObj = new object();

        public string GetNextItem() {
            if (fileQueue.IsEmpty) {
                return null;
            }
            fileQueue.TryDequeue(out var path);
            return path;
        }

        public FileCameraFolderWatcher(string folder, FileExtension fileExtension) {
            if (string.IsNullOrWhiteSpace(folder)) {
                throw new Exception("No Folder for camera to watch was specified!");
            }
            this.fileExtension = fileExtension;
            watchedFolder = folder;
            fileQueue = new ConcurrentQueue<string>();
            fileWatcher = new FileSystemWatcher() {
                Path = watchedFolder,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.*",
                EnableRaisingEvents = false,
                IncludeSubdirectories = false
            };

            fileWatcher.Created += FileWatcher_Created;
            fileWatcher.Renamed += FileWatcher_Renamed;
        }

        public void Start() {
            fileQueue = new ConcurrentQueue<string>();
            fileWatcher.EnableRaisingEvents = true;
        }

        public void Suspend() {
            fileWatcher.EnableRaisingEvents = false;
        }

        private void AddQueueItem(string path) {
            lock (lockObj) {
                var fileExt = Path.GetExtension(path).ToLower();
                if (Regex.IsMatch(fileExt, fileExtension.Pattern)) {
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

        public void Dispose() {
            if (fileWatcher != null) fileWatcher.Dispose();
        }
    }

    public class FileExtension {

        public FileExtension(string name, string pattern) {
            Name = name;
            Pattern = pattern;
        }

        public string Name { get; }
        public string Pattern { get; }

        public override string ToString() {
            return $"{Name} - {Pattern}";
        }
    }
}