#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using GrpcDotNetNamedPipes;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using NINA.Equipment.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera {

    public class SBIGCamera : BaseINPC, ICamera {
        private readonly DeviceQueryInfo queriedCameraInfo;
        private readonly ISbigSdk sdk;
        private readonly SBIG.CCD exposureCcd;
        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;
        private SDK.CameraSDKs.SBIGSDK.DeviceInfo? connectedDevice;

        public SBIGCamera(ISbigSdk sdk, SBIG.CCD exposureCcd, DeviceQueryInfo queriedCameraInfo, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.sdk = sdk;
            this.exposureCcd = exposureCcd;
            this.BinningModes = new AsyncObservableCollection<BinningMode>();
            this.queriedCameraInfo = queriedCameraInfo;
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            this.Id = queriedCameraInfo.SerialNumber;
            this.Name = queriedCameraInfo.Name;
            this.DriverVersion = sdk.GetSdkVersion();
            this.Description = $"{queriedCameraInfo.Name} on {queriedCameraInfo.DeviceId} - {Id}";
        }

        public enum SBIGCameraStatus {
            IDLE,
            WAITING,
            EXPOSING,
            DOWNLOAD,
            ERROR
        }

        public bool HasSetupDialog => false;

        public string Id { get; private set; }

        public string Name { get; private set; }
        public string DisplayName => $"{Name} ({(Id.Length > 8 ? Id[^8..] : Id)})";

        public string Category => "SBIG Legacy";

        private string _description;

        public string Description {
            get => _description;
            private set {
                if (_description != value) {
                    _description = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string DriverInfo => "Native driver for legacy SBIG cameras";

        public string DriverVersion { get; private set; }

        private bool _connected = false;

        public bool Connected {
            get => _connected;
            set {
                if (_connected != value) {
                    _connected = value;
                    RaiseAllPropertiesChanged();
                }
            }
        }

        public async Task<bool> Connect(CancellationToken ct) {
            if (Connected) {
                return true;
            }

            Logger.Info($"SBIGCCD: Attempting to connect {this.queriedCameraInfo.DeviceId}");
            try {
                ConnectedDevice = sdk.OpenDevice(this.queriedCameraInfo.DeviceId);
                if (ConnectedDevice.CameraType == SBIG.CameraType.NoCamera) {
                    throw new InvalidOperationException($"SBIGCCD: Cannot connect {this.queriedCameraInfo.DeviceId} since it is not a camera");
                }

                CcdCameraInfo? cameraInfo = null;
                if (this.exposureCcd == SBIG.CCD.Imaging) {
                    cameraInfo = ConnectedDevice.CameraInfo;
                } else if (this.exposureCcd == SBIG.CCD.Tracking) {
                    cameraInfo = ConnectedDevice.TrackingCameraInfo;
                } else if (this.exposureCcd == SBIG.CCD.ExternalTrackingInStxOrStl) {
                    cameraInfo = ConnectedDevice.ExternalTrackingCameraInfo;
                }
                if (cameraInfo.HasValue) {
                    SetCameraProperties(cameraInfo.Value);
                }

                if (exposureCcd == SBIG.CCD.Imaging) {
                    HasTrackingCcd = ConnectedDevice.TrackingCameraInfo.HasValue || ConnectedDevice.ExternalTrackingCameraInfo.HasValue;
                    if (HasTrackingCcd) {
                        _trackingCamera?.Disconnect();
                        SBIG.CCD trackingCcd;
                        if (!ConnectedDevice.TrackingCameraInfo.HasValue) {
                            Logger.Info("SBIG: Only external tracker detected. Using that");
                            HasDualTracking = false;
                            trackingCcd = SBIG.CCD.ExternalTrackingInStxOrStl;
                        } else if (!ConnectedDevice.ExternalTrackingCameraInfo.HasValue) {
                            Logger.Info("SBIG: Only internal tracker detected. Using that");

                            HasDualTracking = false;
                            trackingCcd = SBIG.CCD.Tracking;
                        } else {
                            Logger.Info($"SBIG: Two trackers detected. UseExternalTrackingCcd={UseExternalTrackingCcd}");

                            HasDualTracking = true;
                            trackingCcd = UseExternalTrackingCcd ? SBIG.CCD.ExternalTrackingInStxOrStl : SBIG.CCD.Tracking;
                        }

                        _trackingCamera = new SBIGCamera(this.sdk, trackingCcd, this.queriedCameraInfo, this.profileService, this.exposureDataFactory);
                        if (TrackingCcdAscomServerEnabled) {
                            await TurnOnTrackingASCOMServer(ct);
                        }
                    } else {
                        Logger.Debug($"SBIGCCD: {this.queriedCameraInfo.DeviceId} does not have a tracking camera");
                    }
                }

                Connected = true;
                if (CanSetTemperature) {
                    CoolerOn = false;
                    var tempStatus = sdk.QueryTemperatureStatus(ConnectedDevice.DeviceId);
                    TemperatureSetPoint = this.exposureCcd == SBIG.CCD.Imaging ? tempStatus.ccdSetpoint : tempStatus.trackingCCDSetpoint;
                }

                Logger.Info($"SBIGCCD: Successfully connected {this.queriedCameraInfo.DeviceId}");
                return true;
            } catch (Exception e) {
                Logger.Error($"SBIGCCD: Failed to connect {this.queriedCameraInfo.DeviceId}", e);
                Notification.ShowError(String.Format(Loc.Instance["LblFailedToConnectDevice"], this.queriedCameraInfo.DeviceId, e));
                if (connectedDevice.HasValue) {
                    sdk.CloseDevice(connectedDevice.Value.DeviceId);
                    connectedDevice = null;
                }
                Connected = false;
                return false;
            }
        }

        private SDK.CameraSDKs.SBIGSDK.DeviceInfo ConnectedDevice {
            get {
                if (connectedDevice.HasValue) {
                    return connectedDevice.Value;
                }
                throw new Exception($"No connected SBIG device");
            }
            set => connectedDevice = value;
        }

        public void Disconnect() {
            if (!Connected) {
                return;
            }

            try {
                TurnOffTrackingASCOMServer();
                _trackingCamera?.Disconnect();
                _trackingCamera = null;

                if (connectedDevice.HasValue) {
                    CoolerOn = false;
                    sdk.CloseDevice(connectedDevice.Value.DeviceId);
                }
            } catch (Exception e) {
                Logger.Error($"SBIGCCD: Failed while trying to close device {this.queriedCameraInfo.DeviceId}. Ignoring", e);
            } finally {
                connectedDevice = null;
                Connected = false;
            }
        }

        private void SetCameraProperties(CcdCameraInfo cameraInfo) {
            Description = $"{queriedCameraInfo.Name} on {queriedCameraInfo.DeviceId}, Firmware Version={cameraInfo.FirmwareVersion}";
            SensorName = cameraInfo.Name;
            HasShutter = cameraInfo.HasMechanicalShutter;
            if (cameraInfo.CcdType == CcdType.MONO) {
                SensorType = SensorType.Monochrome;
            } else if (cameraInfo.CcdType == CcdType.BAYER_MATRIX) {
                SensorType = SensorType.BGGR;
            } else {
                SensorType = SensorType.LRGB;
            }
            BitDepth = cameraInfo.AdcBits;

            var unbinnedMode = SbigSdk.GetReadoutModeConfig(cameraInfo.ReadoutModeConfigs, SBIG.ReadoutMode.NoBinning);
            if (!unbinnedMode.HasValue) {
                throw new NotSupportedException($"SBIG camera doesn't have an unbinned mode, which is not expected");
            }

            BinX = 1;
            BinY = 1;
            PixelSizeX = unbinnedMode.Value.PixelWidthMicrons;
            PixelSizeY = unbinnedMode.Value.PixelHeightMicrons;
            CameraXSize = unbinnedMode.Value.Width;
            CameraYSize = unbinnedMode.Value.Height;

            // For simplicity, only support on chip 2x2 and 3x3 binning modes
            // If there's user demand we could enable off chip, non-square, and greater than 3 binning modes
            var bin2Mode = SbigSdk.GetReadoutModeConfig(cameraInfo.ReadoutModeConfigs, SBIG.ReadoutMode.Bin2x2);
            var bin3Mode = SbigSdk.GetReadoutModeConfig(cameraInfo.ReadoutModeConfigs, SBIG.ReadoutMode.Bin3x3);
            if (bin3Mode.HasValue) {
                MaxBinX = MaxBinY = 3;
            } else if (bin2Mode.HasValue) {
                MaxBinX = MaxBinY = 2;
            }

            BinningModes.Clear();
            for (short i = 1; i <= MaxBinX; ++i) {
                BinningModes.Add(new BinningMode(i, i));
            }
        }

        private bool _hasShutter = false;

        public bool HasShutter {
            get => _hasShutter;
            private set {
                Logger.Debug($"SBIGCCD: Setting HasShutter to {value}");
                if (_hasShutter != value) {
                    _hasShutter = value;
                    RaisePropertyChanged();
                }
            }
        }

        private short _binning = 1;

        public short BinX {
            get => _binning;
            set {
                Logger.Debug($"SBIGCCD: Setting BinX to {value}");
                if (value < 1 || value > MaxBinX) {
                    throw new InvalidOperationException($"BinX {value} must be between 1 and {MaxBinX} inclusive");
                }
                if (_binning != value) {
                    _binning = value;
                    RaisePropertyChanged();
                }
            }
        }

        public short BinY {
            get => _binning;
            set {
                if (value < 1 || value > MaxBinX) {
                    throw new InvalidOperationException($"BinY {value} must be between 1 and {MaxBinY} inclusive");
                }
                // For simplification, we only support symmetric binning modes. The camera may support MxN binning though
                BinX = value;
            }
        }

        private string _sensorName;

        public string SensorName {
            get => _sensorName;
            set {
                if (value != _sensorName) {
                    _sensorName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private SensorType _sensorType = SensorType.Monochrome;

        public SensorType SensorType {
            get => _sensorType;
            private set {
                Logger.Debug($"SBIGCCD: Setting SensorType to {value}");
                if (_sensorType != value) {
                    _sensorType = value;
                    RaisePropertyChanged();
                }
            }
        }

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        private int _cameraXSize = 0;

        public int CameraXSize {
            get => _cameraXSize;
            private set {
                if (_cameraXSize != value) {
                    Logger.Debug($"SBIGCCD: Setting CameraXSize to {value}");
                    _cameraXSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _cameraYSize = 0;

        public int CameraYSize {
            get => _cameraYSize;
            private set {
                if (_cameraYSize != value) {
                    Logger.Debug($"SBIGCCD: Setting CameraYSize to {value}");
                    _cameraYSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Exposures are in hundredths of a second, so this is the shortest it can go
        // NOTE: Some cameras may support millisecond precision for exposures shorter than 500ms, but it is not implemented here yet
        public double ExposureMin => 0.01d;

        // Exposures are in hundredths of a second using 24 bits
        public double ExposureMax => (1 << 24) / 100.0d;

        private short _maxBinning = 1;

        public short MaxBinX {
            get => _maxBinning;
            private set {
                if (value != _maxBinning) {
                    Logger.Debug($"SBIGCCD: Setting MaxBinning to {value}");
                    _maxBinning = value;
                    RaisePropertyChanged();
                }
            }
        }

        public short MaxBinY {
            get => MaxBinX;
            private set => MaxBinX = value;
        }

        private double _pixelSizeX;

        public double PixelSizeX {
            get => _pixelSizeX;
            private set {
                Logger.Debug($"SBIGCCD: Setting PixelSizeX to {value}");
                if (value != _pixelSizeX) {
                    _pixelSizeX = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _pixelSizeY;

        public double PixelSizeY {
            get => _pixelSizeY;
            private set {
                Logger.Debug($"SBIGCCD: Setting PixelSizeY to {value}");
                if (value != _pixelSizeY) {
                    _pixelSizeY = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool HasDewHeater => false;

        public IList<string> SupportedActions => new List<string>();

        private SBIGCameraStatus _cameraStatus = SBIGCameraStatus.IDLE;

        public SBIGCameraStatus CameraStatus {
            get => _cameraStatus;
            private set {
                if (value != _cameraStatus) {
                    _cameraStatus = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(CameraState));
                }
            }
        }

        public CameraStates CameraState {
            get {
                CameraStates cameraState;

                switch (CameraStatus) {
                    case SBIGCameraStatus.EXPOSING:
                        cameraState = CameraStates.Exposing;
                        break;

                    case SBIGCameraStatus.DOWNLOAD:
                        cameraState = CameraStates.Download;
                        break;

                    case SBIGCameraStatus.WAITING:
                        cameraState = CameraStates.Waiting;
                        break;

                    case SBIGCameraStatus.ERROR:
                        cameraState = CameraStates.Error;
                        break;

                    case SBIGCameraStatus.IDLE:
                        cameraState = CameraStates.Idle;
                        break;

                    default:
                        cameraState = CameraStates.NoState;
                        break;
                }

                return cameraState;
            }
        }

        public bool CanSubSample => true;
        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        private int _bitDepth = 16;

        public int BitDepth {
            get => _bitDepth;
            private set {
                Logger.Debug($"SBIGCCD: Setting BitDepth to {value}");
                if (value != _bitDepth) {
                    this._bitDepth = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _electronsPerADU;

        public double ElectronsPerADU {
            get => _electronsPerADU;
            private set {
                Logger.Debug($"SBIGCCD: Setting ElectronsPerADU to {value}");
                if (value != _electronsPerADU) {
                    this._electronsPerADU = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IList<string> ReadoutModes => new List<string>() { "Default" };

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

        public AsyncObservableCollection<BinningMode> BinningModes { get; }

        public bool CanSetTemperature => true;

        public double Temperature {
            get {
                if (!Connected) {
                    return double.NaN;
                }
                var tempStatus = sdk.QueryTemperatureStatus(ConnectedDevice.DeviceId);
                if (this.exposureCcd == SBIG.CCD.Imaging) {
                    return tempStatus.imagingCCDTemperature;
                } else {
                    return tempStatus.trackingCCDTemperature;
                }
            }
        }

        public double AmbientTemperature {
            get {
                if (!Connected) {
                    return double.NaN;
                }
                var tempStatus = sdk.QueryTemperatureStatus(ConnectedDevice.DeviceId);
                return tempStatus.ambientTemperature;
            }
        }

        private double _temperatureSetPoint = 0.0d;

        public double TemperatureSetPoint {
            get {
                if (!Connected) {
                    return double.NaN;
                }

                // NOTE: The SBIG SDK doesn't correctly report the current set point, so we just return the last set point used
                // We also initialize set point on connection to whatever the driver reports, which is often zero
                /*
                if (CoolerOn) {
                    var tempStatus = sdk.QueryTemperatureStatus(ConnectedDevice.DeviceId);
                    var ccdSetPoint = this.exposureCcd == SBIG.CCD.Imaging ? tempStatus.ccdSetpoint : tempStatus.trackingCCDSetpoint;
                    if (ccdSetPoint != _temperatureSetPoint) {
                        _temperatureSetPoint = ccdSetPoint;
                        RaisePropertyChanged();
                    }
                }*/
                return _temperatureSetPoint;
            }
            set {
                if (Connected && _temperatureSetPoint != value) {
                    _temperatureSetPoint = value;
                    if (CoolerOn) {
                        sdk.RegulateTemperature(ConnectedDevice.DeviceId, this.exposureCcd, value);
                    }

                    RaisePropertyChanged();
                }
            }
        }

        public bool CoolerOn {
            get {
                if (Connected) {
                    var tempStatus = sdk.QueryTemperatureStatus(ConnectedDevice.DeviceId);
                    return tempStatus.coolingEnabled == 1;
                } else {
                    return false;
                }
            }
            set {
                if (Connected) {
                    if (value) {
                        sdk.RegulateTemperature(ConnectedDevice.DeviceId, this.exposureCcd, TemperatureSetPoint);
                    } else {
                        sdk.DisableTemperatureRegulation(ConnectedDevice.DeviceId, this.exposureCcd);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public double CoolerPower {
            get {
                if (!Connected) {
                    return double.NaN;
                }
                var tempStatus = sdk.QueryTemperatureStatus(ConnectedDevice.DeviceId);
                if (this.exposureCcd == SBIG.CCD.Imaging) {
                    return tempStatus.imagingCCDPower;
                } else {
                    return tempStatus.trackingCCDPower;
                }
            }
        }

        public void AbortExposure() {
            StopExposure();
        }

        public void SetBinning(short x, short y) {
            if (x != y) {
                throw new NotSupportedException($"Cannot set binning to {x}, {y}. Only symmetric binning modes are supported.");
            }

            BinX = x;
            BinY = y;
        }

        public void StartExposure(CaptureSequence sequence) {
            var isDarkFrame = sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                               sequence.ImageType == CaptureSequence.ImageTypes.BIAS;
            var readoutMode = SDK.CameraSDKs.SBIGSDK.ReadoutMode.Create(BinX);
            Point exposureStart;
            Size exposureSize;
            if (EnableSubSample) {
                exposureStart = new Point(SubSampleX / BinX, SubSampleY / BinY);
                exposureSize = new Size(SubSampleWidth / BinX, SubSampleHeight / BinY);
            } else {
                exposureStart = new Point(0, 0);
                exposureSize = new Size(CameraXSize / BinX, CameraYSize / BinY);
            }

            sdk.StartExposure(ConnectedDevice.DeviceId, this.exposureCcd, readoutMode, isDarkFrame, sequence.ExposureTime, exposureStart, exposureSize);
            CameraStatus = SBIGCameraStatus.EXPOSING;
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken ct) {
            return await Task.Run(() => {
                try {
                    CameraStatus = SBIGCameraStatus.DOWNLOAD;
                    var exposureData = sdk.DownloadExposure(ConnectedDevice.DeviceId, this.exposureCcd, ct);
                    var metaData = new ImageMetaData();
                    metaData.FromCamera(this);
                    return exposureDataFactory.CreateImageArrayExposureData(
                        input: exposureData.Data,
                        width: exposureData.Width,
                        height: exposureData.Height,
                        bitDepth: BitDepth,
                        isBayered: SensorType != SensorType.Monochrome && BinX == 1 && BinY == 1,
                        metaData: metaData);
                } catch (OperationCanceledException) {
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                } finally {
                    CameraStatus = SBIGCameraStatus.IDLE;
                }
                return null;
            }, ct);
        }

        public void StopExposure() {
            if (Connected) {
                CameraStatus = SBIGCameraStatus.IDLE;
                sdk.EndExposure(ConnectedDevice.DeviceId, this.exposureCcd);
            }
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                try {
                    while (sdk.GetExposureState(ConnectedDevice.DeviceId, this.exposureCcd) == CommandState.IN_PROGRESS) {
                        await Task.Delay(100, token);
                    }
                } finally {
                    sdk.EndExposure(ConnectedDevice.DeviceId, this.exposureCcd);
                    if (CameraStatus == SBIGCameraStatus.EXPOSING) {
                        CameraStatus = SBIGCameraStatus.WAITING;
                    }
                }
                token.ThrowIfCancellationRequested();
            }
        }

        private bool _hasTrackingCcd = false;

        public bool HasTrackingCcd {
            get => _hasTrackingCcd;
            private set {
                if (this._hasTrackingCcd != value) {
                    this._hasTrackingCcd = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _hasDualTracking = false;

        public bool HasDualTracking {
            get => _hasDualTracking;
            private set {
                if (this._hasDualTracking != value) {
                    this._hasDualTracking = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool UseExternalTrackingCcd {
            get => profileService.ActiveProfile.CameraSettings.SBIGUseExternalCcdTracker;
            set {
                if (profileService.ActiveProfile.CameraSettings.SBIGUseExternalCcdTracker != value) {
                    if (_trackingCamera != null) {
                        Notification.ShowInformation(Loc.Instance["LblSBIGUseExternalTrackingCCDChanged"]);
                    }

                    profileService.ActiveProfile.CameraSettings.SBIGUseExternalCcdTracker = value;
                    RaisePropertyChanged();
                }
            }
        }

        private NamedPipeServer _trackingCcdAscomServer;
        private SBIGCamera _trackingCamera;

        private async Task TurnOnTrackingASCOMServer(CancellationToken ct) {
            if (!HasTrackingCcd) {
                throw new NotSupportedException("No tracking CCD");
            }

            if (_trackingCcdAscomServer != null) {
                throw new InvalidOperationException("Tracking CCD ASCOM server already running");
            }

            bool connected = false;
            try {
                connected = await _trackingCamera.Connect(ct);
                if (!connected) {
                    throw new Exception("Failed to connect to tracking camera");
                }

                _trackingCcdAscomServer = new NamedPipeServer(this.profileService.ActiveProfile.CameraSettings.TrackingCameraASCOMServerPipeName);
                var loggingWrapped = SBIGServerLoggingProxy<Core.API.ASCOM.Camera.CameraService.CameraServiceBase>.Wrap(new SBIGCameraASCOMService(_trackingCamera), _trackingCamera);
                var grpcWrapped = GrpcErrorPropagatingProxy<Core.API.ASCOM.Camera.CameraService.CameraServiceBase>.Wrap(loggingWrapped);
                Core.API.ASCOM.Camera.CameraService.BindService(_trackingCcdAscomServer.ServiceBinder, grpcWrapped);
                _trackingCcdAscomServer.Start();
                Notification.ShowInformation(Loc.Instance["LblTrackingASCOMServerStarted"]);
            } catch (Exception e) {
                Logger.Error($"SBIGCCD: Failed to started tracking CCD ASCOM server", e);
                Notification.ShowError(String.Format(Loc.Instance["LblTrackingASCOMServerStartFailed"], e.Message));
                _trackingCcdAscomServer?.Dispose();
                _trackingCcdAscomServer = null;
                if (connected) {
                    _trackingCamera.Disconnect();
                }
            }
        }

        private void TurnOffTrackingASCOMServer() {
            if (_trackingCcdAscomServer == null) {
                return;
            }

            try {
                _trackingCcdAscomServer.Kill();
                _trackingCcdAscomServer.Dispose();
                _trackingCamera.Disconnect();
            } catch (Exception e) {
                Logger.Error($"SBIGCCD: Failed to stop tracking CCD ASCOM server", e);
            } finally {
                _trackingCcdAscomServer = null;
                Notification.ShowInformation(Loc.Instance["LblTrackingASCOMServerStopped"]);
            }
        }

        public bool TrackingCcdAscomServerEnabled {
            get => profileService.ActiveProfile.CameraSettings.TrackingCameraASCOMServerEnabled == true;
            set {
                if (profileService.ActiveProfile.CameraSettings.TrackingCameraASCOMServerEnabled != value) {
                    Task.Run(async () => {
                        if (value) {
                            await TurnOnTrackingASCOMServer(CancellationToken.None);
                        } else {
                            TurnOffTrackingASCOMServer();
                        }
                        profileService.ActiveProfile.CameraSettings.TrackingCameraASCOMServerEnabled = value;
                        RaisePropertyChanged(nameof(TrackingCcdAscomServerEnabled));
                    });
                }
            }
        }

        public bool TrackingCcdAscomServerLoggingEnabled {
            get => profileService.ActiveProfile.CameraSettings.TrackingCameraASCOMServerLoggingEnabled == true;
            set {
                if (profileService.ActiveProfile.CameraSettings.TrackingCameraASCOMServerLoggingEnabled != value) {
                    profileService.ActiveProfile.CameraSettings.TrackingCameraASCOMServerLoggingEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        #region Unsupported Operations

        public bool CanShowLiveView => false;

        public bool LiveViewEnabled => false;

        public bool HasBattery => false;

        public int BatteryLevel => 0;

        public bool CanSetOffset => false;

        public int Offset { get => 0; set => throw new InvalidOperationException(); }

        public int OffsetMin => 0;

        public int OffsetMax => 0;

        public bool CanSetUSBLimit => false;

        public int USBLimit { get => 0; set => throw new InvalidOperationException(); }

        public int USBLimitMin => 0;

        public int USBLimitMax => 0;

        public int USBLimitStep => 1;

        public bool CanGetGain => false;

        public bool CanSetGain => false;

        public int GainMax => -1;

        public int GainMin => -1;

        public int Gain { get => -1; set => throw new InvalidOperationException(); }

        public IList<int> Gains => new List<int>() { };

        public bool DewHeaterOn { get => false; set => throw new InvalidOperationException(); }

        public void StartLiveView(CaptureSequence sequence) {
            throw new InvalidOperationException();
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }

        public void UpdateSubSampleArea() {
            throw new NotImplementedException();
        }

        public void SetupDialog() {
            throw new NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken ct) {
            throw new NotImplementedException();
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

        #endregion Unsupported Operations
    }
}