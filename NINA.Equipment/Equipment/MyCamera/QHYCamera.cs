#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using QHYCCD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Image.ImageData;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Exceptions;
using System.Collections;
using NINA.Astrometry;
using NINA.Equipment.Utility;

namespace NINA.Equipment.Equipment.MyCamera {

    public class QHYCamera : BaseINPC, ICamera {
        private static readonly TimeSpan COOLING_TIMEOUT = TimeSpan.FromSeconds(2);
        private AsyncObservableCollection<BinningMode> _binningModes;
        private bool _connected = false;
        private bool _liveViewEnabled = false;
        private short _readoutModeForNormalImages = 0;
        private short _readoutModeForSnapImages = 0;
        private Task coolerTask;
        private CancellationTokenSource coolerWorkerCts;
        private Task sensorStatsTask;
        private CancellationTokenSource sensorStatsCts;
        private QhySdk.QHYCCD_CAMERA_INFO Info;
        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;
        private CancellationTokenSource downloadExposureTaskCTS;
        private Task<IExposureData> downloadExposureTask;
        public IQhySdk Sdk { get; set; } = QhySdk.Instance;

        public QHYCamera(uint cameraIdx, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;

            var cameraId = new StringBuilder(QhySdk.QHYCCD_ID_LEN);
            var cameraModel = new StringBuilder(0);

            /*
             * Camera model long form, eg: "QHY183C-c915484fa76ea7552"
             * The QHY SDK uses this to internally identify connected cameras
             * and this we need this to create a handle for our camera.
             */
            Sdk.GetId(cameraIdx, cameraId);

            /*
             * Camera model short form, eg: "QHY183C"
             * We use this to put in the camera equipment selection menu
             * rather than the long form above.
             */
            Sdk.GetModel(cameraId, cameraModel);

            Name = cameraModel.ToString();
            Info.Index = cameraIdx;
            Info.Id = cameraId;

            Logger.Debug($"QHYCCD: Found camera {Info.Id}");
            _gpsSettings = [];
        }

        public string Category { get; } = "QHYCCD";

        private List<int> SupportedBinFactors {
            get {
                if (Info.SupportedBins == null) {
                    var supportedBins = new List<int>();
                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN1X1MODE)) {
                        supportedBins.Add(1);
                    }

                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN2X2MODE)) {
                        supportedBins.Add(2);
                    }

                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN3X3MODE)) {
                        supportedBins.Add(3);
                    }

                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN4X4MODE)) {
                        supportedBins.Add(4);
                    }

                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN6X6MODE)) {
                        supportedBins.Add(6);
                    }

                    if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_BIN8X8MODE)) {
                        supportedBins.Add(8);
                    }

                    Info.SupportedBins = supportedBins;
                }
                return Info.SupportedBins;
            }
        }

        public int BatteryLevel => -1;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (_binningModes == null) {
                    _binningModes = new AsyncObservableCollection<BinningMode>();
                    foreach (int f in SupportedBinFactors) {
                        /*
                         * QHY cameras are known to support only symmetrical bin modes
                         */
                        _binningModes.Add(new BinningMode((short)f, (short)f));
                    }
                }
                return _binningModes;
            }
        }

        public short BinX {
            get => Info.CurBin;
            set => Info.CurBin = value;
        }

        /// <summary>
        // Setting the Y pixel of a bin mode is redundant as QHY cameras
        // (currently) offer symmetrical bin modes only. Thus operations for
        // setting the Y pixes are no-ops, and getting Y pixel bin modes defer
        // to the getters for X pixels.
        /// </summary>
        public short BinY {
            get => Info.CurBin;
            set { }
        }

        public int BitDepth => (int)Info.Bpp;

        public CameraStates CameraState {
            get => Info.CamState;
            set {
                if (Info.CamState != value) {
                    Info.CamState = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int CameraXSize { get; private set; }
        public int CameraYSize { get; private set; }

        public bool CanGetGain {
            get {
                if (Connected) {
                    return Info.HasGain;
                }

                return false;
            }
        }

        public bool CanSetGain {
            get {
                if (Connected) {
                    return Info.HasGain;
                }

                return false;
            }
        }

        public bool CanSetOffset {
            get {
                if (Connected) {
                    return Info.HasOffset;
                }

                return false;
            }
        }

        public bool CanSetTemperature {
            get {
                if (Connected) {
                    return Info.HasCooler;
                }

                return false;
            }
        }

        public bool CanSetUSBLimit {
            get {
                if (Connected) {
                    return Info.HasUSBTraffic;
                }

                return false;
            }
        }

        public bool CanFastReadout {
            get => Info.HasReadoutSpeed;
            private set {
                Logger.Debug($"QHYCCD: Setting CanFastReadout to {value}");
                Info.HasReadoutSpeed = value;
            }
        }

        public bool CanShowLiveView => false;
        public bool CanSubSample => true;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public bool CoolerOn {
            get {
                if (internalReconnect) {
                    return tempCoolerOn;
                }

                return Info.CoolerOn;
            }
            set {
                if (Connected && CanSetTemperature) {
                    Logger.Debug(string.Format("QHYCCD: Cooler turned {0}", value ? "ON" : "OFF"));

                    Info.CoolerOn = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double CoolerPower {
            get {
                if (internalReconnect) { return tempCoolerPower; }

                double rv = double.NaN;                
                if (Connected && CanSetTemperature) {
                    if ((rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_CURPWM)) != QhySdk.QHYCCD_ERROR) {
                        /*
                         * This needs to be returned as a percentage of Info.CoolerPwmMax.
                         */
                        return (rv / Info.CoolerPwmMax) * 100;
                    }
                }

                return rv;
            }
        }

        public string Description => Info.Id.ToString();

        public bool DewHeaterOn {
            get => false;
            set {
            }
        }

        public string DriverInfo => "Native driver for QHYCCD cameras";
        public string DriverVersion => string.Empty;
        public bool EnableSubSample { get; set; }
        public double ExposureMax => Info.ExpMax / 1e6;

        public IList<string> SupportedActions => new List<string>();

        public double ElectronsPerADU => double.NaN;        

        /// <summary>
        // We store the camera's exposure times in microseconds
        // but we present this number as seconds to those who ask
        /// </summary>
        public double ExposureMin => Info.ExpMin / 1e6;

        public int Gain {
            get {
                if(internalReconnect) { return tempGain; }

                if (Connected && CanGetGain) {
                    double rv;
                    if (Info.HasUnreliableGain) {
                        rv = Info.CurGain;
                    } else {
                        rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN);
                    }

                    return unchecked((int)rv);
                }

                return 1;
            }
            set {
                if (Connected && CanSetGain) {
                    if (Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN, value)) {
                        Info.CurGain = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public int GainMax => Info.GainMax;
        public int GainMin => Info.GainMin;
        public IList<int> Gains => new List<int>();
        public bool HasBattery => false;
        public bool HasDewHeater => false;
        public bool HasSetupDialog => false;

        public bool HasShutter {
            get {
                if (Connected) {
                    return Info.HasShutter;
                }

                return false;
            }

            private set => Info.HasShutter = value;
        }

        public string Id => $"{Description}";

        public short MaxBinX => (short)SupportedBinFactors.DefaultIfEmpty(1).Max();
        public short MaxBinY => MaxBinX;

        public string Name {
            get => Info.Model.ToString();
            set {
                Info.Model = new StringBuilder(value);
                RaiseAllPropertiesChanged();
            }
        }

        public string DisplayName => $"{Name} ({(Id.Length > 8 ? Id[^8..] : Id)})";

        public int Offset {
            get {
                if (internalReconnect) { return tempOffset; }

                if (Connected) {
                    double rv;
                    if ((rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET)) != QhySdk.QHYCCD_ERROR) {
                        if (Info.InflatedOff != 0) {
                            return unchecked((int)(rv - Info.InflatedOff));
                        } else {
                            return unchecked((int)rv);
                        }
                    }
                }

                return 0;
            }
            set {
                if (Connected && CanSetOffset) {
                    if (Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET, value)) {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public int OffsetMax => Info.OffMax;
        public int OffsetMin => Info.OffMin;

        public double PixelSizeX {
            get => Info.PixelX;
            private set {
                Logger.Debug($"QHYCCD: Setting PixelSizeX to {value}");
                Info.PixelX = value;
                RaisePropertyChanged();
            }
        }

        public double PixelSizeY {
            get => Info.PixelY;
            private set {
                Logger.Debug($"QHYCCD: Setting PixelSizeY to {value}");
                Info.PixelY = value;
                RaisePropertyChanged();
            }
        }

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    _readoutModeForNormalImages = value;
                } else {
                    _readoutModeForNormalImages = 0;
                }

                RaisePropertyChanged();
            }
        }

        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    _readoutModeForSnapImages = value;
                } else {
                    _readoutModeForSnapImages = 0;
                }

                RaisePropertyChanged();
            }
        }

        public short ReadoutMode {
            get {
                uint mode = 0;
                uint rv;

                if (Connected) {
                    if (CanFastReadout) {
                        mode = (uint)Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_SPEED);
                    } else {
                        if ((rv = Sdk.GetReadMode(ref mode)) != QhySdk.QHYCCD_SUCCESS) {
                            Logger.Error($"QHYCCD: GetQHYCCDReadMode() failed. Returned {rv}");
                            return 0;
                        }
                    }

                    return (short)mode;
                } else {
                    return 0;
                }
            }
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    uint rv;
                    string modeName = ReadoutModes[value];
                    uint mode = (uint)value;

                    if (Connected) {
                        if (CanFastReadout) {
                            if (mode >= Info.ReadoutSpeedMin && value <= Info.ReadoutSpeedMax) {
                                Logger.Debug($"QHYCCD: ReadoutMode: Setting readout speed to {mode} ({modeName})");
                                Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_SPEED, mode);
                            } else {
                                Logger.Error($"QHYCCD: Invalid readout speed: {mode}");
                                return;
                            }
                        } else {
                            if (mode != ReadoutMode) {
                                if (mode < 0 && mode > ReadoutModes.Count - 1) {
                                    Logger.Error($"QHYCCD: Invalid readout mode {mode}");
                                    return;
                                }

                                Logger.Debug($"QHYCCD: ReadoutMode: Setting readout mode to {mode} ({modeName})");

                                if ((rv = Sdk.SetReadMode(mode)) != QhySdk.QHYCCD_SUCCESS) {
                                    Logger.Error($"QHYCCD: SetQHYCCDReadMode() failed. Returned {rv}");
                                    return;
                                }
                                if ((rv = Sdk.SetStreamMode((byte)QhySdk.QHYCCD_CAMERA_MODE.SINGLE_EXPOSURE)) != QhySdk.QHYCCD_SUCCESS) {
                                    Logger.Error($"QHYCCD: SetQHYCCDStreamMode() failed. Returned {rv}");
                                    return;
                                }

                                Sdk.InitCamera();
                                SetImageResolution();
                            }
                        }
                    }
                } else {
                    Logger.Warning($"QHYCCD: ReadoutMode: Index for readoutmode does not exist {value}");
                }
            }
        }

        public IList<string> ReadoutModes {
            get => Info.ReadoutModes;
            set => Info.ReadoutModes = value;
        }

        public string SensorName => string.Empty;

        public SensorType SensorType { get; private set; } = SensorType.Monochrome;

        public int SubSampleHeight { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }

        public double Temperature {
            get {
                if (internalReconnect) { return tempTemperature; }

                double rv = double.NaN;                
                if (Connected && Info.HasChipTemp) {
                    if ((rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_CURTEMP)) != QhySdk.QHYCCD_ERROR)
                        return rv;
                }

                return rv;
            }
        }

        public double TemperatureSetPoint {
            get {
                if (Connected && CanSetTemperature) {
                    return Info.CoolerTargetTemp;
                } else {
                    return double.NaN;
                }
            }
            set {
                if (Connected && CanSetTemperature) {
                    Logger.Debug($"QHYCCD: Cooler target temperature set to {value}");
                    Info.CoolerTargetTemp = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Hashtable _gpsSettings;

        public void SetGPS() {
            if ((string)_gpsSettings["SetGPS"] == "True") {
                Sdk.SetControlValue(QhySdk.CONTROL_ID.CAM_GPS, 0); // first turn it off
                if (!Sdk.SetControlValue(QhySdk.CONTROL_ID.CAM_GPS, 1)) {
                    Logger.Debug("Failed to set GPS");
                    _gpsSettings["SetGPS"] = "False";
                }
                if (!string.IsNullOrEmpty((string)_gpsSettings["SetQHYCCDGPSVCOXFreq"])) {
                    Sdk.SetQHYCCDGPSVCOXFreq(ushort.Parse((string)_gpsSettings["SetQHYCCDGPSVCOXFreq"]));
                }
                if (!string.IsNullOrEmpty((string)_gpsSettings["SetQHYCCDGPSLedCalMode"])) {
                    Sdk.SetQHYCCDGPSLedCalMode(byte.Parse((string)_gpsSettings["SetQHYCCDGPSLedCalMode"]));
                }
                if (!string.IsNullOrEmpty((string)_gpsSettings["SetQHYCCDGPSMasterSlave"])) {
                    Sdk.SetQHYCCDGPSMasterSlave(byte.Parse((string)_gpsSettings["SetQHYCCDGPSMasterSlave"]));
                }
                if (!string.IsNullOrEmpty((string)_gpsSettings["SetQHYCCDGPSPOSA"])) {
                    Sdk.SetQHYCCDGPSPOSA(uint.Parse((string)_gpsSettings["SetQHYCCDGPSPOSA"]), 40);
                }
                if (!string.IsNullOrEmpty((string)_gpsSettings["SetQHYCCDGPSPOSB"])) {
                    Sdk.SetQHYCCDGPSPOSB(uint.Parse((string)_gpsSettings["SetQHYCCDGPSPOSB"]), 40);
                }
            } else {
                if (!Sdk.SetControlValue(QhySdk.CONTROL_ID.CAM_GPS, 0)) {
                    Logger.Debug("Failed to unset GPS");
                }
            }
        }

        public int USBLimit {
            get {
                if (internalReconnect) { return tempUSBLimit; }

                double rv;
                if (Connected && ((rv = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC)) != QhySdk.QHYCCD_ERROR))
                    return unchecked((int)rv);

                return -1;
            }
            set {
                if (Connected && CanSetUSBLimit) {
                    if (Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC, value))
                        RaisePropertyChanged();
                }
            }
        }

        public int USBLimitMax => unchecked((int)Info.USBMax);

        public int USBLimitMin => unchecked((int)Info.USBMin);

        public int USBLimitStep => unchecked((int)Info.USBStep);

        /// <summary>
        // QHY cameras have two eras of firmware when it comes to managing cooling.
        // "Old" camera firmware requires that the target temperature or PWM be set at regular, constant
        // intervals, usually every 2 seconds. "New" camera firmware does not have this requirement, but
        // there is no programmatic way to determine which firmware is old and which firmware is new. As
        // a result, QHY's own suggestion is to just treat every camera as if it is running old firmware
        // and set the target temperature every 2 seconds.
        //
        // To manage this, we will create a task to run constantly in the background while CoolerOn = true.
        /// </summary>
        private async Task CoolerWorker(CancellationToken ct) {
            try {
                bool previous = Info.CoolerOn;

                Logger.Debug("QHYCCD: CoolerWorker task started");
                while (!ct.IsCancellationRequested) {
                    if (Info.CoolerOn) {
                        Logger.Trace($"QHYCCD: CoolerWorker setting camera target temp to {Info.CoolerTargetTemp}");
                        Sdk.ControlTemp(Info.CoolerTargetTemp);
                    } else if (previous == true) {
                        Logger.Debug("QHYCCD: CoolerWorker turning off TEC due user request");
                        _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_MANULPWM, 0);
                    }

                    previous = Info.CoolerOn;

                    /* sleep (cancelable) */
                    await Task.Delay(QhySdk.QHYCCD_COOLER_DELAY, ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("QHYCCD: CoolerWorker task cancelled");
            }
        }

        private async Task SensorStatsWorker(CancellationToken ct) {
            try {
                Logger.Debug("QHYCCD: SensorStatsWorker task started");

                while (!ct.IsCancellationRequested) {
                    if (QhyHasSensorAirPressure) {
                        QhySensorAirPressure = GetQhySensorAirPressure();
                    }

                    if (QhyHasSensorHumidity) {
                        QhySensorHumidity = GetQhySensorHumidity();
                    }

                    /* sleep (cancelable) */
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("QHYCCD: SensorStatsWorker task cancelled");
            }
        }

        private bool GetSensorType() {
            Info.BayerPattern = Sdk.GetBayerType();

            switch (Info.BayerPattern) {
                case QhySdk.BAYER_ID.BAYER_GB:
                    SensorType = SensorType.GBRG;
                    break;

                case QhySdk.BAYER_ID.BAYER_GR:
                    SensorType = SensorType.GRBG;
                    break;

                case QhySdk.BAYER_ID.BAYER_BG:
                    SensorType = SensorType.BGGR;
                    break;

                case QhySdk.BAYER_ID.BAYER_RG:
                    SensorType = SensorType.RGGB;
                    break;

                default:
                    return false;
            }
            return true;
        }

        public short BayerOffsetX => 0;
        public short BayerOffsetY => 0;

        public void AbortExposure() {
            StopExposure();
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task.Run(() => ConnectSync(), ct);
        }

        private void CancelCoolingSync() {
            if (!Connected || !Info.HasCooler) {
                return;
            }

            Logger.Debug("QHYCCD: Terminating CoolerWorker task");

            CoolerOn = false;
            var cts = coolerWorkerCts;
            try {
                cts?.Cancel();
            } catch { }
            try {
                using var timeoutSource = new CancellationTokenSource(COOLING_TIMEOUT);
                coolerTask?.Wait(timeoutSource.Token);
            } catch (Exception ex) {
                Logger.Error($"QHYCCD: Cooling thread failed to terminate within {COOLING_TIMEOUT}", ex);
            } finally {
                try { cts?.Dispose(); } finally { }                
                coolerWorkerCts = null;
                coolerTask = null;
            }

            /* CoolerWorker task was killed. Make sure the TEC is turned off before closing the camera. */
            Logger.Debug("QHYCCD: CoolerWorker task cancelled, turning off TEC");
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_MANULPWM, 0);
        }

        private void CancelSensorStatsSync() {
            if (!Connected && !QhyHasSensorHumidity && !QhyHasSensorAirPressure) {
                return;
            }

            Logger.Debug("QHYCCD: Terminating SensorStatsWorker task");

            var cts = sensorStatsCts;

            try {
                cts?.Cancel();
            } catch { }
            try {
                using var timeoutSource = new CancellationTokenSource(COOLING_TIMEOUT);
                sensorStatsTask?.Wait(timeoutSource.Token);
            } catch (Exception ex) {
                Logger.Error($"QHYCCD: SensorStats thread failed to terminate within {COOLING_TIMEOUT}", ex);
            } finally {
                try { cts?.Dispose(); } finally { }
                sensorStatsCts = null;
                sensorStatsTask = null;
            }
        }

        private static void ThrowOnFailure(string op, uint result) {
            if (result != QhySdk.QHYCCD_SUCCESS) {
                throw new Exception($"QHYCCD: {op} failed");
            }
        }

        public bool ConnectSync() {
            if (Connected && !internalReconnect) {
                return true;
            }

            var success = false;
            double min = 0, max = 0, step = 0;
            var modeList = new List<string>();
            var cameraID = new StringBuilder(QhySdk.QHYCCD_ID_LEN);
            var modeName = new StringBuilder(0);
            uint num_modes = 0;
            Info.HasReadoutSpeed = false;

            try {
                if (!internalReconnect) {
                    Sdk.InitSdk();
                    Logger.Info($"QHYCCD: Connecting to {Info.Id}");
                }


                /*
                 * Get our selected camera's ID from the SDK
                 */
                Sdk.GetId(Info.Index, cameraID);

                /*
                 * CameraP is the handle we use to reference this camera
                 * from now on.
                 */
                Sdk.Open(cameraID);
                if (LiveViewEnabled) {
                    ThrowOnFailure("SetQHYCCDStreamMode", Sdk.SetStreamMode((byte)QhySdk.QHYCCD_CAMERA_MODE.VIDEO_STREAM));
                } else {
                    ThrowOnFailure("SetQHYCCDStreamMode", Sdk.SetStreamMode((byte)QhySdk.QHYCCD_CAMERA_MODE.SINGLE_EXPOSURE));
                }

                /*
                 * Initialize the camera and make it available for use
                 */
                Sdk.InitCamera();

                if (CheckUvloIsActive()) {
                    Sdk.Close();
                    Notification.ShowError(Loc.Instance["LblQhyUvloActiveError"]);
                    return false;
                }

                if (!internalReconnect)
                    SetImageResolution();

                /*
                 * Is this a color sensor or not?
                 * If so, do not debayer the image data
                 */
                if (GetSensorType() == true) {
                    Logger.Info($"QHYCCD: Color camera detected (pattern = {Info.BayerPattern}). Setting debayering to off");
                    ThrowOnFailure("SetQHYCCDDebayerOnOff", Sdk.SetDebayerOnOff(false));
                    Info.IsColorCam = true;
                } else {
                    Info.IsColorCam = false;
                }

                /*
                 * See if the camera offers a fast readout speed
                 * Readout speeds and readout modes are mutually exclusive and no camera should have both.
                 * There is a special case for the QHY42 Pro. This can have both speeds and modes, but the speeds are not relevant in single exposure mode, so we ignore the advertisement of CONTROL_SPEED
                 */
                CanFastReadout = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_SPEED) && !Name.Equals("QHY42PRO");

                if (CanFastReadout) {
                    ThrowOnFailure("GetQHYCCDParamMinMaxStep", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_SPEED, ref min, ref max, ref step));

                    Info.ReadoutSpeedMin = (uint)min;
                    Info.ReadoutSpeedMax = (uint)max;
                    Info.ReadoutSpeedStep = (uint)step;
                    Logger.Debug($"QHYCCD: ReadoutSpeedMin={Info.ReadoutSpeedMin}, ReadoutSpeedMax={Info.ReadoutSpeedMax}, ReadoutSpeedStep={Info.ReadoutSpeedStep}");

                    modeList.Add(Loc.Instance["LblNormal"]);
                    modeList.Add(Loc.Instance["LblFast"]);
                } else {
                    /*
                     * See if this camera has any readout modes and build a list of their names if so
                     */
                    ThrowOnFailure("GetQHYCCDNumberOfReadModes", Sdk.GetNumberOfReadModes(ref num_modes));
                    Logger.Debug($"QHYCCD: Camera has {num_modes} readout mode(s)");

                    /*
                     * Every camera always has 1 readout mode. We are only interested in getting names for ones that have more than 1
                     */
                    if (num_modes > 1) {
                        for (uint i = 0; i < num_modes; i++) {
                            ThrowOnFailure("GetQHYCCDReadModeName", Sdk.GetReadModeName(i, modeName));
                            Logger.Debug($"QHYCCD: Found readout mode \"{modeName}\"");
                            modeList.Add(modeName.ToString());
                        }
                    }
                }

                if (modeList.Count == 0) {
                    modeList.Add("Default");
                }

                Logger.Debug($"QHYCCD: Readout mode names: {string.Join(", ", modeList)}");
                ReadoutModes = modeList;

                // Check camera for DDR memory and set to use it.
                Logger.Debug("QHYCCD: checking DDR memory.");
                if (Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_DDR)) {
                    Logger.Debug("QHYCCD: DDR memory checked");
                    if (Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_DDR, 1)) {
                        Logger.Debug("QHYCCD: start using DDR memory.");
                    } else {
                        Logger.Debug("QHYCCD: cannot start using DDR memory.");
                    }
                }

                var ddr_buffer = Sdk.GetControlValue(QhySdk.CONTROL_ID.DDR_BUFFER_CAPACITY);
                Logger.Debug("QHYCCD: DDR_BUFFER_CAPACITY " + ddr_buffer);
                var ddr_read_threshold = Sdk.GetControlValue(QhySdk.CONTROL_ID.DDR_BUFFER_READ_THRESHOLD);
                Logger.Debug("QHYCCD: DDR_BUFFER_READ_THRESHOLD " + ddr_read_threshold);

                /*
                 * Get our min and max shutter speed (exposure times)
                 * The QHY SDK reports this value in microseconds (us)
                 */
                ThrowOnFailure("GetQHYCCDParamMinMaxStep(Exposure)", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_EXPOSURE, ref min, ref max, ref step));

                Info.ExpMin = min;
                Info.ExpMax = max;
                Info.ExpStep = step;
                Logger.Debug($"QHYCCD: ExpMin={Info.ExpMin}, ExpMax={Info.ExpMax}, ExpStep={Info.ExpStep}");

                /*
                 * Get our min and max gain
                 */
                ThrowOnFailure("GetQHYCCDParamMinMaxStep(Gain)", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_GAIN, ref min, ref max, ref step));

                Info.GainMin = (short)min;
                Info.GainMax = (short)max;
                Info.GainStep = step;
                Logger.Debug($"QHYCCD: GainMin={Info.GainMin}, GainMax={Info.GainMax}, GainStep={Info.GainStep}");

                /*
                 * Check for gain setting bugs
                 */
                Info.HasUnreliableGain = QuirkUnreliableGain();

                /*
                 * If we have to keep track of gain ourselves, initialize the gain to Info.GainMin
                 */
                if (Info.HasUnreliableGain == true) {
                    Gain = Info.GainMin;
                }

                /*
                 * Get our min and max offset
                 */
                ThrowOnFailure("GetQHYCCDParamMinMaxStep(Offset)", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_OFFSET, ref min, ref max, ref step));

                Info.OffMin = (int)min;
                Info.OffMax = (int)max;
                Info.OffStep = step;
                Logger.Debug($"QHYCCD: OffMin={Info.OffMin}, OffMax={Info.OffMax}, OffStep={Info.OffStep}");

                QuirkInflatedOffset();

                /*
                 * Fetch our min and max PWM settings for
                 * the cooler.
                 */
                Info.HasCooler = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_COOLER);
                Info.HasChipTemp = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_CURTEMP);

                if (Info.HasCooler) {
                    // Ignore the return code due to a bug in the SDK causes this to return a failure even if it succeeds
                    Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_MANULPWM, ref min, ref max, ref step);

                    Info.CoolerPwmMin = min;
                    Info.CoolerPwmMax = max;
                    Info.CoolerPwmStep = step;
                    Logger.Debug($"QHYCCD: CoolerPwmMin={Info.CoolerPwmMin}, CoolerPwmMax={Info.CoolerPwmMax}, CoolerPwmStep={Info.CoolerPwmStep}");

                    /*
                     * Initialize cooler's target temperature to 0C
                     */
                    if (!internalReconnect) { 
                        Info.CoolerTargetTemp = 0;
                    }

                    /*
                     * Force any TEC cooler to off upon startup
                     */
                    if (!internalReconnect) {
                        CoolerOn = false;
                        CancelCoolingSync();
                    } else {
                        CoolerOn = tempCoolerOn;
                    }

                    /*
                     * Start the thread that operates the TEC
                     * This thread will operate the TEC in accordance with the user turning the cooler on or off
                     * and program the camera's TEC to cool to the desired temperature. This thread will operate
                     * for as long as the camera is connected.
                     */
                    Logger.Debug("QHYCCD: Starting CoolerWorker task");
                    coolerWorkerCts = new CancellationTokenSource();
                    coolerTask = CoolerWorker(coolerWorkerCts.Token);
                }

                /*
                 * QHY SDK offers no way to get the current bin mode! So we track
                 * it manually using QHYCCD_CAMERA_INFO.CurBin. We initialize the
                 * camera with 1x1 binning.
                 */
                if (!internalReconnect) { 
                    Info.CurBin = 1;
                }
                SetBinning(Info.CurBin, Info.CurBin);

                HasShutter = Sdk.IsControl(QhySdk.CONTROL_ID.CAM_MECHANICALSHUTTER);
                Info.HasGain = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_GAIN);
                Info.HasOffset = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_OFFSET);

                /*
                 * Fetch our min and max USB bandwidth settings. The QHY163M
                 * changes whether CONTROL_USBTRAFFIC based on the StreamMode,
                 * so recompute this when reconnecting.
                 */
                Info.HasUSBTraffic = Sdk.IsControl(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC);

                if (Info.HasUSBTraffic) {
                    ThrowOnFailure("GetQHYCCDParamMinMaxStep(USBTraffic)", Sdk.GetParamMinMaxStep(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC, ref min, ref max, ref step));

                    Info.USBMin = min;
                    Info.USBMax = max;
                    Info.USBStep = step;
                    Logger.Debug($"QHYCCD: USBMin={Info.USBMin}, USBMax={Info.USBMax}, USBStep={Info.USBStep}");

                    if (QuirkNoUSBTraffic()) {
                        Info.HasUSBTraffic = false;
                    }
                }

                /*
                 * Detect if this camera has sensor chamber air pressure and humidity sensors
                 */
                QhyHasSensorAirPressure = Sdk.IsControl(QhySdk.CONTROL_ID.CAM_PRESSURE);
                QhyHasSensorHumidity = Sdk.IsControl(QhySdk.CONTROL_ID.CAM_HUMIDITY);

                if (QhyHasSensorAirPressure || QhyHasSensorHumidity) {
                    Logger.Debug("QHYCCD: Starting SensorStatsWorker task");

                    sensorStatsCts = new CancellationTokenSource();
                    sensorStatsTask = SensorStatsWorker(sensorStatsCts.Token);
                }

                QhyFirmwareVersion = GetFirmwareVersion();
                QhyFPGAVersion = GetFPGAVersion();
                QhySdkVersion = GetSdkVersion();

                // Only check driver versions on new connection, because this is really slow.
                if (!internalReconnect) {
                    /*
                     * Check the USB driver version and emit a warning if it's below the recommended minimum version
                     */
                    DriverVersionCheck();

                    Logger.Info($"QHYCCD: SDK version: {QhySdkVersion}");
                    Logger.Info($"QHYCCD: USB driver version: {QhyUsbDriverVersion}");
                    Logger.Info($"QHYCCD: Camera firmware version: {QhyFirmwareVersion}");
                    Logger.Info($"QHYCCD: Camera FPGA versions: {QhyFPGAVersion}");
                }

                // Check and set GPS based on settings
                SetGPS();

                /*
                /*
                 * Announce that this camera is now initialized and ready
                 */
                CameraState = CameraStates.Idle;
                Connected = true;
                success = true;

                RaisePropertyChanged(nameof(Connected));
                RaiseAllPropertiesChanged();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                Disconnect();
            }
            return success;
        }

        public void Disconnect() {
            if (!Connected) {
                return;
            }

            try {
                if(!internalReconnect) {
                    Connected = false;
                    CancelCoolingSync();
                    CancelSensorStatsSync();
                    Logger.Info($"QHYCCD: Closing camera {Info.Id}");
                }

                Sdk.Close();

                // Do not release the SDK if we are reconnecting to switch between modes. Only need to close and reopen to do that.
                if (!internalReconnect) {
                    Sdk.ReleaseSdk();
                }
            } catch (Exception ex) {
                Logger.Error("QHYCCD: Failed to disconnect", ex);
            }
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            RaiseIfNotConnected();
            using (token.Register(() => { AbortExposure(); downloadExposureTaskCTS.Cancel(); })) {
                if (downloadExposureTask != null) {
                    await downloadExposureTask;
                }
            }
        }

        private Task<IExposureData> StartDownloadExposure(CancellationToken ct) {
            return Task.Run<IExposureData>(async () => {
                uint width = 0;
                uint height = 0;
                uint bpp = 0;
                uint channels = 0;
                uint rv;

                Logger.Debug("QHYCCD: Downloading exposure...");

                while (Sdk.GetExposureRemaining() > 0) {
                    await Task.Delay(10, ct);
                }

                /*
                 * Size the image data byte array for the image
                 */
                bool is16bit = Info.Bpp > 8;
                uint numPixels = is16bit ? ImageSize / 2U : ImageSize;
                ushort[] ImgData = new ushort[numPixels];

                /*
                 * Download the image from the camera
                 */
                CameraState = CameraStates.Download;
                ct.ThrowIfCancellationRequested();
                if (is16bit) {
                    Logger.Trace("GetSingleFrame start");
                    rv = Sdk.GetSingleFrame(ref width, ref height, ref bpp, ref channels, ImgData);
                    Logger.Trace("GetSingleFrame end");
                } else {
                    byte[] ImgDataBytes = new byte[numPixels];
                    Logger.Trace("GetSingleFrame start");
                    rv = Sdk.GetSingleFrame(ref width, ref height, ref bpp, ref channels, ImgDataBytes);
                    Logger.Trace("GetSingleFrame end");
                    for (int i = 0; i < ImgDataBytes.Length; i++) {
                        ImgData[i] = ImgDataBytes[i];
                    }
                }
                if (rv != QhySdk.QHYCCD_SUCCESS) {
                    Logger.Warning($"QHYCCD: Failed to download image from camera! rv = {rv }");
                    throw new CameraDownloadFailedException(Loc.Instance["LblASIImageDownloadError"]);
                }

                Logger.Debug($"QHYCCD: Downloaded image: {width}x{height}, {bpp} bpp, {channels} channels");

                // Try getting more info from the camera
                var metaData = new ImageMetaData();
                metaData.FromCamera(this);
                ExtractPreciseExposureInfo(metaData);
                // Add gps info to the metaData
                ExtractGpsMetaData(ImgData, metaData);

                CameraState = CameraStates.Idle;

                return exposureDataFactory.CreateImageArrayExposureData(
                    input: ImgData,
                    width: (int)width,
                    height: (int)height,
                    bitDepth: BitDepth,
                    isBayered: SensorType != SensorType.Monochrome && (BinX == 1 && BinY == 1),
                    metaData: metaData);
            }, ct);
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken ct) {
            using (ct.Register(() => downloadExposureTaskCTS.Cancel())) {
                return await downloadExposureTask.WaitAsync(ct);
            }
        }

        private void RaiseIfNotConnected() {
            // Check for connection status and raise exceptions explicitly since using invalid CameraP handles can cause hard application crashes
            if (!Connected) {
                throw new Exception("QHYCCD is not connected");
            }
        }

        public void SetBinning(short x, short y) {
            Logger.Debug($"QHYCCD: Setting bin mode to {x}x{y}");
            BinX = x;
        }

        public void SetupDialog() {
        }

        private bool SetResolution(out uint startx, out uint starty, out uint sizex, out uint sizey) {
            if (QhyIncludeOverscan) {
                StartPixelX = StartPixelY = 0;
            } else {
                StartPixelX = Info.EffectiveArea.StartX;
                StartPixelY = Info.EffectiveArea.StartY;
            }

            /* ROI coordinates and resolution */
            if (EnableSubSample == true) {
                startx = (StartPixelX + (uint)SubSampleX) / (uint)BinX;
                starty = (StartPixelY + (uint)SubSampleY) / (uint)BinY;
                uint subWidth = Math.Min((uint)SubSampleX + (uint)SubSampleWidth, QhyIncludeOverscan ? (uint)CameraXSize : Info.EffectiveArea.SizeX) - (uint)SubSampleX;
                uint subHeight = Math.Min((uint)SubSampleY + (uint)SubSampleHeight, QhyIncludeOverscan ? (uint)CameraYSize : Info.EffectiveArea.SizeY) - (uint)SubSampleY;
                sizex = (uint)subWidth / (uint)BinX;
                sizey = (uint)subHeight / (uint)BinY;
            } else {
                startx = StartPixelX / (uint)BinX;
                starty = StartPixelY / (uint)BinY;
                if (QhyIncludeOverscan) {
                    sizex = (uint)CameraXSize / (uint)BinX;
                    sizey = (uint)CameraYSize / (uint)BinY;
                } else {
                    sizex = (uint)Info.EffectiveArea.SizeX / (uint)BinX;
                    sizey = (uint)Info.EffectiveArea.SizeY / (uint)BinY;
                }
            }

            uint rv;
            Logger.Debug($"QHYCCD: Setting image resolution: startx={startx}, starty={starty}, sizex={sizex}, sizey={sizey}");
            if ((rv = Sdk.SetResolution(startx, starty, sizex, sizey)) != QhySdk.QHYCCD_SUCCESS) {
                Logger.Error($"QHYCCD: Failed to set exposure resolution: rv = {rv}");
                return false;
            }
            SetGPS(); // set gps again as work around for gps getting stuck
            return true;
        }


        public void StartExposure(CaptureSequence sequence) {
            RaiseIfNotConnected();
            uint rv;
            uint startx, starty, sizex, sizey;
            bool isSnap;
            short readoutMode;

            if (CheckUvloIsActive()) {
                throw new CameraExposureFailedException(Loc.Instance["LblQhyUvloActiveError"]);
            }

            /*
             * Setup camera with the desired exposure setttings
             */

            isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;

            /* Open or close the shutter if camera is equipped with one */
            if (HasShutter == true) {
                if (sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                        sequence.ImageType == CaptureSequence.ImageTypes.BIAS) {
                    Logger.Debug($"QHYCCD: Closing shutter for {sequence.ImageType} frame");
                    _ = Sdk.ControlShutter(1);
                } else {
                    Logger.Debug($"QHYCCD: Opening shutter for {sequence.ImageType} frame");
                    _ = Sdk.ControlShutter(0);
                }
            }

            /* Exposure readout mode */
            readoutMode = isSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

            if (ReadoutMode != readoutMode) {
                ReadoutMode = readoutMode;
            }

            /* Exposure bit depth */
            if (Sdk.SetBitsMode((uint)BitDepth) != QhySdk.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to set exposure bit depth. This may not be a fatal error.");
            }

            /* Exposure length (in microseconds) */
            if (!Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_EXPOSURE, sequence.ExposureTime * 1e6)) {
                Logger.Error("QHYCCD: Failed to set exposure time");
                return;
            }

            /*
             * Set binning
             * Certain models of camera require a 200ms pause after setting the bin mode.
             * SetQHYCCDBinMode() will return QHYCCD_DELAY_200MS if that is required.
             */
            if ((rv = Sdk.SetBinMode((uint)BinX, (uint)BinY)) != QhySdk.QHYCCD_SUCCESS) {
                if (rv == QhySdk.QHYCCD_DELAY_200MS) {
                    Thread.Sleep(200);
                } else {
                    Logger.Warning($"QHYCCD: Failed to set BIN mode {BinX}x{BinY}");
                }
            }

            if (!SetResolution(out startx, out starty, out sizex, out sizey)) {
                return;
            }

            /*
             * Calculate exposure array size, with overflow protection.
             * Strictly speaking, we should also multiply by the number of image channels (aka planes)
             * but since we do no debayer anything here in the driver, that number will always be 1 (monochrome).
             */
            ImageSize = (uint)((sizex * sizey * BitDepth) + (8 - 1)) / 8;

            /*
             * Initiate the exposure
             */
            Logger.Debug("QHYCCD: Starting exposure...");
            CameraState = CameraStates.Exposing;
            uint ret = Sdk.ExpSingleFrame();
            if (ret == QhySdk.QHYCCD_ERROR) {
                Logger.Error("QHYCCD: Failed to initiate the exposure!");
                CameraState = CameraStates.Idle;
                return;
            }

            downloadExposureTaskCTS?.Dispose();
            downloadExposureTaskCTS = new CancellationTokenSource();
            downloadExposureTask = StartDownloadExposure(downloadExposureTaskCTS.Token);
        }

        public void StopExposure() {
            RaiseIfNotConnected();
            if (Sdk.CancelExposingAndReadout() != QhySdk.QHYCCD_ERROR) {
                CameraState = CameraStates.Idle;
            }
        }

        public bool LiveViewEnabled {
            get => _liveViewEnabled;
            set {
                if (_liveViewEnabled != value) {
                    _liveViewEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        private void ReconnectForLiveView() {
            // Steps documented as required when changing video mode:
            // CloseQHYCCD
            // ReleaseQHYCCDResource
            // ScanQHYCCD
            // OpenQHYCCD
            // SetLiveStreamMode
            // It appears that ReleaseQHYCCDResource and ScanQHYCCD can be skipped in newer drivers?
            tempGain = Gain;
            tempOffset = Offset;
            tempUSBLimit = USBLimit;
            tempCoolerPower = CoolerPower;
            tempTemperature = Temperature;
            tempCoolerOn = CoolerOn;

            internalReconnect = true;
            Disconnect();
            ConnectSync();
            internalReconnect = false;
        }

        public void StartLiveView(CaptureSequence sequence) {
            Logger.Info("QHYCCD: Switching to video mode");
            RaiseIfNotConnected();
            LiveViewEnabled = true;
            ReconnectForLiveView();

            if (Sdk.SetStreamMode((byte)QhySdk.QHYCCD_CAMERA_MODE.VIDEO_STREAM) != QhySdk.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to switch to video mode");
                CameraState = CameraStates.Error;
                return;
            }

            if (Sdk.SetBitsMode((uint)16) != QhySdk.QHYCCD_SUCCESS) {
                CameraState = CameraStates.Error;
                return;
            }

            /*
             * Set binning
             * Certain models of camera require a 200ms pause after setting the bin mode.
             * SetQHYCCDBinMode() will return QHYCCD_DELAY_200MS if that is required.
             */
            uint rv;
            if ((rv = Sdk.SetBinMode((uint)BinX, (uint)BinY)) != QhySdk.QHYCCD_SUCCESS) {
                if (rv == QhySdk.QHYCCD_DELAY_200MS) {
                    Thread.Sleep(200);
                } else {
                    Logger.Warning($"QHYCCD: Failed to set BIN mode {BinX}x{BinY}");
                }
            }

            uint startx, starty, sizex, sizey;
            if (!SetResolution(out startx, out starty, out sizex, out sizey)) {
                Logger.Warning("QHYCCD: Failed to set resolution for video mode");
                CameraState = CameraStates.Error;
                return;
            }

            /* Exposure length (in microseconds) */
            if (!Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_EXPOSURE, sequence.ExposureTime * 1e6)) {
                Logger.Error("QHYCCD: Failed to set exposure time");
                return;
            }

            /*
             * Calculate exposure array size, with overflow protection.
             * Strictly speaking, we should also multiply by the number of image channels (aka planes)
             * but since we do no debayer anything here in the driver, that number will always be 1 (monochrome).
             */
            ImageSize = (uint)((sizex * sizey * BitDepth) + (8 - 1)) / 8;

            if (Sdk.BeginQHYCCDLive() != QhySdk.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to start video mode");
                CameraState = CameraStates.Error;
                LiveViewEnabled = false;
                ReconnectForLiveView();
                return;
            }
            CameraState = CameraStates.Exposing;

            Logger.Info("QHYCCD: Enabled video mode");
        }

        public void StopLiveView() {
            Logger.Info("QHYCCD: Switching to single exposure mode");
            RaiseIfNotConnected();
            if (Sdk.StopQHYCCDLive() != QhySdk.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to stop video mode");
                CameraState = CameraStates.Error;
                // Continue on to reconnecting the camera
            } else if (Sdk.SetStreamMode((byte)QhySdk.QHYCCD_CAMERA_MODE.SINGLE_EXPOSURE) != QhySdk.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to switch to single exposuremode");
                CameraState = CameraStates.Error;
                // Continue on to reconnecting the camera
            } else {
                CameraState = CameraStates.Idle;
            }

            LiveViewEnabled = false;
            ReconnectForLiveView();
            Logger.Info("QHYCCD: Switched to single exposure mode");
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken ct) {
            RaiseIfNotConnected();
            return Task.Run<IExposureData>(async () => {
                uint rv;
                uint width = 0;
                uint height = 0;
                uint bpp = 0;
                uint channels = 0;

                Logger.Trace("QHYCCD: Downloading exposure...");

                /*
                 * Size the image data byte array for the image
                 */
                bool is16bit = Info.Bpp > 8;
                uint numPixels = is16bit ? ImageSize / 2U : ImageSize;
                ushort[] ImgData = new ushort[numPixels];

                while (!ct.IsCancellationRequested) {
                    rv = Sdk.GetQHYCCDLiveFrame(ref width, ref height, ref bpp, ref channels, ImgData);
                    if (rv == uint.MaxValue) {
                        await Task.Yield();
                        // GetQHYCCDLiveFrame returns -1 when the data isn't available yet, requiring looping.
                        continue;
                    } else if (rv > numPixels) {
                        // rv returns how many bytes have been downloaded if there is still more to do. 0 indicates completion
                        Logger.Warning($"QHYCCD: Failed to download image from camera! rv = {rv}");
                        throw new CameraDownloadFailedException(Loc.Instance["LblASIImageDownloadError"]);
                    } else {
                        break;
                    }
                }
                Logger.Debug($"QHYCCD: Downloaded image: {width}x{height}, {bpp} bpp, {channels} channels");

                // Try getting more info from the camera
                var metaData = new ImageMetaData();
                metaData.FromCamera(this);
                ExtractPreciseExposureInfo(metaData);
                // Add gps info to the metaData
                ExtractGpsMetaData(ImgData, metaData);
                CameraState = CameraStates.Idle;

                return exposureDataFactory.CreateImageArrayExposureData(
                    input: ImgData,
                    width: (int)width,
                    height: (int)height,
                    bitDepth: BitDepth,
                    isBayered: SensorType != SensorType.Monochrome,
                    metaData: metaData);
            }, ct);
        }

        private void ExtractPreciseExposureInfo(ImageMetaData metaData) {
            uint pixelPeriod = 0, linePeriod = 0, framePeriod = 0, clocksPerLine = 0, linesPerFrame = 0, actualExposureTime = 0;
            byte isLongExposureMode = 0;
            var rv = Sdk.GetQHYCCDPreciseExposureInfo(ref pixelPeriod, ref linePeriod, ref framePeriod, ref clocksPerLine, ref linesPerFrame, ref actualExposureTime, ref isLongExposureMode);
            if (rv == QhySdk.QHYCCD_SUCCESS) {
                metaData.GenericHeaders.Add(new DoubleMetaDataHeader("QHY_EXP", actualExposureTime / 1e6, "[s] Actual exposure time"));
                metaData.GenericHeaders.Add(new DoubleMetaDataHeader("QHY_PP", pixelPeriod, "[ps] pixelPeriod"));
                metaData.GenericHeaders.Add(new DoubleMetaDataHeader("QHY_LP", linePeriod, "[ns] linePeriod"));
                metaData.GenericHeaders.Add(new DoubleMetaDataHeader("QHY_FP", framePeriod, "[us] framePeriod"));
                metaData.GenericHeaders.Add(new DoubleMetaDataHeader("QHY_CPL", clocksPerLine, "clocksPerLine"));
                metaData.GenericHeaders.Add(new DoubleMetaDataHeader("QHY_LPF", linesPerFrame, "linesPerFrame"));
                metaData.GenericHeaders.Add(new DoubleMetaDataHeader("QHY_AET", actualExposureTime, "[us] actualExposureTime"));
                metaData.GenericHeaders.Add(new BoolMetaDataHeader("QHY_LEM", isLongExposureMode == 1, "isLongExposureMode"));
            } else {
                Logger.Debug("QHY precise info not found.");
            }

            double offsetRow = 0d;
            rv = Sdk.GetQHYCCDRollingShutterEndOffset(0, ref offsetRow);
            if (rv == QhySdk.QHYCCD_SUCCESS) {
                metaData.GenericHeaders.Add(new DoubleMetaDataHeader("QHY_OFF0", offsetRow, "[us] RollingShutterEndOffset row 0"));
            }
        }

        private void ExtractGpsMetaData(ushort[] flatArray, ImageMetaData metaData) {
            if ((string)_gpsSettings["SetGPS"] != "True") return;
            //State of the GPS
            byte[] imgData = new byte[50];
            Buffer.BlockCopy(flatArray, 0, imgData, 0, imgData.Length);

            // Sanity check
            var now_sec = (256 * 256 * 256 * imgData[34]) + (256 * 256 * imgData[35]) + (256 * imgData[36]) + imgData[37];
            var now_us = ((256 * 256 * imgData[38]) + (256 * imgData[39]) + imgData[40]) / 10;
            var now_utc = JulianSecToDateTime(now_sec, now_us).ToUniversalTime();
            TimeSpan utc_diff = DateTime.UtcNow - now_utc;
            if (Math.Abs(utc_diff.TotalMinutes) > 1) {
                Logger.Warning("GPS is on, but extracted data is bad.");
                return; 
            }

            //PPS count
            var pps = 256 * 256 * imgData[41] + 256 * imgData[42] + imgData[43];
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_PPS", pps, "QHY pps"));
            //Frame number
            var seqNumber = 256 * 256 * 256 * imgData[0] + 256 * 256 * imgData[1] + 256 * imgData[2] + imgData[3];
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_SEQ", seqNumber, "QHY sequence nr"));
            //The width of the image
            var width = 256 * imgData[5] + imgData[6];
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_W", width, "QHY width"));
            //Height of the image
            var height = 256 * imgData[7] + imgData[8];
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_H", height, "QHY height"));
            //latitude
            var temp = 256 * 256 * 256 * imgData[9] + 256 * 256 * imgData[10] + 256 * imgData[11] + imgData[12];
            var south = temp > 1000000000;
            var deg = (temp % 1000000000) / 10000000;
            var min = (temp % 10000000) / 100000;
            var fractMin = (temp % 100000) / 100000.0;
            var latitude = (deg + (min + fractMin) / 60.0) * (south ? -1 : 1);
            metaData.GenericHeaders.Add(new DoubleMetaDataHeader("GPS_LAT", latitude, "latitude"));
            //longitude
            temp = 256 * 256 * 256 * imgData[13] + 256 * 256 * imgData[14] + 256 * imgData[15] + imgData[16];
            var west = temp > 1000000000;
            deg = (temp % 1000000000) / 1000000;
            min = (temp % 1000000) / 10000;
            fractMin = (temp % 10000) / 10000.0;
            var longitude = (deg + (min + fractMin) / 60.0) * (west ? -1 : 1);
            metaData.GenericHeaders.Add(new DoubleMetaDataHeader("GPS_LON", longitude, "longitude"));
            //Shutter start time
            var start_flag = (imgData[17] / 16) % 4;
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_SFLG", start_flag, "QHY start_flag"));
            metaData.GenericHeaders.Add(new StringMetaDataHeader("GPS_SST", ReceiverStatus(start_flag), "QHY start_flag status"));
            var start_sec = (256 * 256 * 256 * imgData[18]) + (256 * 256 * imgData[19]) + (256 * imgData[20]) + imgData[21];
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_SSEC", start_sec, "[s] QHY start"));
            var start_us = ((256 * 256 * imgData[22]) + (256 * imgData[23]) + imgData[24]) / 10;
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_SUS", start_us, "[us] QHY start"));
            metaData.GenericHeaders.Add(new DateTimeMetaDataHeader("GPS_SUTC", JulianSecToDateTime(start_sec, start_us).ToUniversalTime(), "QHY start_time"));
            //Shutter end time
            var end_flag = (imgData[25] / 16) % 4;
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_EFLG", end_flag, "QHY end_flag"));
            metaData.GenericHeaders.Add(new StringMetaDataHeader("GPS_EST", ReceiverStatus(end_flag), "QHY end_flag status"));
            var end_sec = (256 * 256 * 256 * imgData[26]) + (256 * 256 * imgData[27]) + (256 * imgData[28]) + imgData[29];
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_ESEC", end_sec, "[s] QHY end"));
            var end_us = ((256 * 256 * imgData[30]) + (256 * imgData[31]) + imgData[32]) / 10;
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_EUS", end_us, "[us] QHY end"));
            metaData.GenericHeaders.Add(new DateTimeMetaDataHeader("GPS_EUTC", JulianSecToDateTime(end_sec, end_us).ToUniversalTime(), "QHY end_time"));
            //The current time
            var now_flag = (imgData[33] / 16) % 4;
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_NFLG", now_flag, "QHY now_flag"));
            metaData.GenericHeaders.Add(new StringMetaDataHeader("GPS_NST", ReceiverStatus(now_flag), "QHY now_flag status"));
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_NSEC", now_sec, "[s] QHY now"));
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_NUS", now_us, "[us] QHY now"));
            metaData.GenericHeaders.Add(new DateTimeMetaDataHeader("GPS_NUTC", now_utc, "QHY now_time"));
            //Exposure time
            var exposure = ((end_sec - start_sec) * 1000 * 1000) + (end_us - start_us);
            metaData.GenericHeaders.Add(new IntMetaDataHeader("GPS_EXP", exposure, "[us] QHY exposure"));
        }

        private static DateTime JulianSecToDateTime(double sec, double us) {
            return NOVAS.JulianToDateTime(2450000.5d + ((sec + (us / 1e6d)) / 86400d));
        }

        private static string ReceiverStatus(int flag) {
            return flag switch {
                0 => "just powered on",
                1 => "not locked",
                2 => "not locked but data valid",// position and time
                3 => "locked and valid",
                _ => "Unknown",
            };
        }

        public bool QhyIncludeOverscan {
            get => profileService.ActiveProfile.CameraSettings.QhyIncludeOverscan;
            set {
                profileService.ActiveProfile.CameraSettings.QhyIncludeOverscan = value;
            }
        }

        public bool QhyHasSensorAirPressure {
            get => Info.HasSensorAirPressure;
            private set => Info.HasSensorAirPressure = value;
        }

        public bool QhyHasSensorHumidity {
            get => Info.HasSensorHumidity;
            private set => Info.HasSensorHumidity = value;
        }

        public double QhySensorAirPressure {
            get => Info.SensorAirPressure;
            private set {
                if (Info.SensorAirPressure != value) {
                    Info.SensorAirPressure = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double QhySensorHumidity {
            get => Info.SensorHumidity;
            private set {
                if (Info.SensorHumidity != value) {
                    Info.SensorHumidity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double GetQhySensorAirPressure() {
            double pressure = double.NaN;

            if (Connected && QhyHasSensorHumidity) {
                Sdk.GetPressure(ref pressure);
            }

            Logger.Trace($"QHYCCD: Sensor air pressure: {pressure} hPa");
            return pressure;
        }

        private double GetQhySensorHumidity() {
            double rh = double.NaN;

            if (Connected && QhyHasSensorHumidity) {
                Sdk.GetHumidity(ref rh);
            }

            Logger.Trace($"QHYCCD: Sensor humidity: {rh}%");
            return rh;
        }

        public string QhyFirmwareVersion {
            get => Info.FirmwareVersion;
            private set => Info.FirmwareVersion = value;
        }

        public string QhyFPGAVersion {
            get => Info.FPGAVersion;
            private set => Info.FPGAVersion = value;
        }

        private string GetFirmwareVersion() {
            return Sdk.GetFwVersion();
        }

        private string GetFPGAVersion() {
            return Sdk.GetFpgaVersion();
        }

        private string GetSdkVersion() {
            return Sdk.GetSdkVersion();
        }

        public string QhySdkVersion {
            get => Info.SdkVersion;
            private set => Info.SdkVersion = value;
        }

        public string QhyUsbDriverVersion {
            get => Info.UsbDriverVersion;
            private set => Info.UsbDriverVersion = value;
        }

        public string Action(string actionName, string actionParameters) {
            switch (actionName) {
                case "SetGPS":
                    Logger.Debug("Adding setting " + actionName + " to " + actionParameters);
                    _gpsSettings[actionName] = actionParameters;
                    SetGPS();
                    break;
                case "Reset":
                    _gpsSettings = [];
                    break;
                default:
                    Logger.Debug("Adding setting " + actionName + " to " + actionParameters);
                    _gpsSettings[actionName] = actionParameters;
                    break;
            }
            return "done";
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

        private void SetImageResolution() {
            double pixelx = 0, pixely = 0;

            /*
             * Get full sensor size and pixel dimensions
             */
            ThrowOnFailure("GetQHYCCDChipInfo", Sdk.GetChipInfo(
                ref Info.ChipX, ref Info.ChipY,
                ref Info.FullArea.SizeX, ref Info.FullArea.SizeY,
                ref pixelx, ref pixely,
                ref Info.Bpp));

            Info.FullArea.StartX = Info.FullArea.StartY = 0;
            Logger.Debug($"QHYCCD: Chip Info: ChipX={Info.ChipX}mm, ChipY={Info.ChipY}mm, SizeX={Info.FullArea.SizeX}, SizeY={Info.FullArea.SizeY}, PixelX={pixelx}um, PixelY={pixely}um, bpp={Info.Bpp}");
            CameraXSize = (int)Info.FullArea.SizeX;
            CameraYSize = (int)Info.FullArea.SizeY;

            /*
             * Update the pixel size if it has changed (eg; QHY294M/C Pro)
             * Other NINA processes depend on pixel size being accurate and source this info from the profile, so we must update that as well.
             */
            if (PixelSizeX != pixelx) {
                profileService.ActiveProfile.CameraSettings.PixelSize = PixelSizeX = pixelx;
                PixelSizeY = pixely;
            }
            // Make sure binning is set to 1 because it changes the effective area.
            uint rv;
            if ((rv = Sdk.SetBinMode(1, 1)) != QhySdk.QHYCCD_SUCCESS) {
                Logger.Error($"QHYCCD: SetQHYCCDBinMode() failed. Returned {rv}");
                return;
            }

            /*
             * The Effective Area is a sensor's real imaging area. On sensors that have an overscan area, the effective area will be smaller than
             * the sensor's dimensions that were reported by GetQHYCCDChipInfo(). If the sensor does not have an overscan area, the values should be equal.
             */
            ThrowOnFailure("GetQHYCCDEffectiveArea", Sdk.GetEffectiveArea(ref Info.EffectiveArea.StartX, ref Info.EffectiveArea.StartY, ref Info.EffectiveArea.SizeX, ref Info.EffectiveArea.SizeY));
            Logger.Debug($"QHYCCD: Effective Area: StartX={Info.EffectiveArea.StartX}, StartY={Info.EffectiveArea.StartY}, SizeX={Info.EffectiveArea.SizeX}, SizeY={Info.EffectiveArea.SizeY}");

            // Always include overscan. Use EffectiveArea to get the image without overscan. See SetResolution.
            Sdk.SetControlValue(QhySdk.CONTROL_ID.CAM_IGNOREOVERSCAN_INTERFACE, 0d);

            Logger.Debug($"QHYCCD: Sensor dimensions used: Overscan={QhyIncludeOverscan}, StartX={StartPixelX}, StartY{StartPixelY}, SizeX={CameraXSize}, SizeY={CameraYSize}");
        }

        private uint StartPixelX {
            get => Info.CurImage.StartX;
            set {
                Logger.Debug($"QHYCCD: Setting StartPixelX to {value}");
                Info.CurImage.StartX = value;
            }
        }

        private uint StartPixelY {
            get => Info.CurImage.StartY;
            set {
                Logger.Debug($"QHYCCD: Setting StartPixelY to {value}");
                Info.CurImage.StartY = value;
            }
        }

        private uint ImageSize {
            get => Info.ImageSize;
            set {
                Logger.Debug($"QHYCCD: Setting ImageSize to {value} bytes");
                Info.ImageSize = value;
            }
        }

        private void DriverVersionCheck() {
            // Minimum driver versions. Key: Driver name. Value: Minimum driver version
            var driverDatabase = new Dictionary<string, string> {
                { "QHY5IIISeries_IO", "21.10.18.0" },
                { "QHY5II_IO", "0.0.9.0" },
                { "QHY8LBASE", "0.0.9.0" },
                { "QHY8PBASE", "0.0.9.0" },
                { "QHY9BASE", "0.0.9.0" },
                { "QHY10BASE", "0.0.9.0" },
                { "QHY11BASE", "0.0.9.0" },
                { "QHY12BASE", "0.0.9.0" },
                { "QHY22BASE", "0.0.9.0" },
                { "IMG2PBASE", "0.0.9.0" },
                { "QHY90A_IO", "0.0.9.0" },
                { "QHY695A_IO", "0.0.9.0" },
                { "QHY814A_IO", "0.0.9.0" },
                { "QHY09000A_IO", "0.0.9.0" },
                { "QHY16200A_IO", "0.0.9.0" },
                { "QHY16803A_IO", "0.0.9.0" }
            };

            QhyUsbDriverVersion = string.Empty;

            try {
                ManagementObjectSearcher searchObject = null;
                ManagementObjectCollection objCollection = null;

                foreach (KeyValuePair<string, string> driverInfo in driverDatabase) {
                    string driverName = driverInfo.Key;
                    string minimumVersion = driverInfo.Value;

                    searchObject = new ManagementObjectSearcher($"SELECT DriverVersion FROM Win32_PnPSignedDriver WHERE DeviceName = '{driverName}'");
                    objCollection = searchObject.Get();

                    if (objCollection.Count > 0) {
                        // We are interested in only one instance if there are multiple cameras connected. They should all have the same version.
                        var obj = objCollection.OfType<ManagementObject>().FirstOrDefault();

                        if (!string.IsNullOrEmpty(obj["DriverVersion"].ToString())) {
                            QhyUsbDriverVersion = obj["DriverVersion"].ToString();
                            Logger.Debug($"QHYCCD: {driverName} driver version: {QhyUsbDriverVersion}");
                        }

                        // Emit a warning to the user if the USB3 driver is behind
                        // Per QHY, the USB2 drivers aren't of major concern
                        if (driverName.Equals("QHY5IIISeries_IO")) {
                            var minVer = new Version(minimumVersion);
                            var runVer = new Version(QhyUsbDriverVersion);
                            var compare = minVer.CompareTo(runVer);

                            if (compare > 0) {
                                Logger.Warning($"QHYCCD: Installed USB driver version {QhyUsbDriverVersion} is older than recommended version {minimumVersion}. Operation of the camera may not be reliable and updating is HIGHLY suggested.");
                                Notification.ShowWarning(string.Format(Loc.Instance["LblQhyccdDriverVersionWarning"], QhyUsbDriverVersion, minimumVersion));
                            }
                        }

                        // We found this camera's driver. Move on.
                        break;
                    }
                }

                objCollection.Dispose();
                searchObject.Dispose();
            } catch (Exception e) {
                Logger.Error($"QHYCCD: Fx3DriverVersionCheck failed: {e}");
            }
        }

        private bool CheckUvloIsActive() {
            bool uvloActive = false;

            if (Sdk.IsControl(QhySdk.CONTROL_ID.CAM_Sensor_ULVO_Status)) {
                var uvloState = Sdk.GetControlValue(QhySdk.CONTROL_ID.CAM_Sensor_ULVO_Status);
                Logger.Debug($"UVLO status: {uvloState:0}");

                switch (uvloState) {
                    case 2d:
                    case 9d:
                        Logger.Error($"UVLO is active! Status = {uvloState:0}");
                        uvloActive = true;
                        break;
                }
            }

            return uvloActive;
        }

        ///<summary>
        // Camera models usually accept CONTROL_GAIN values as a normal integer
        // in accordance with the accepted ranges as stated by the values returned
        // by GetQHYCCDParamMinMaxStep().
        // HOWEVER this is not always the case, as with the QHY163M (at least). The
        // QHY163M will state its gain range as being 0..580 inclusive and setting
        // CONTROL_GAIN with a value in that range is accepted by the SDK and acted
        // upon accordingly. But when asked to report its current CONTROL_GAIN value,
        // the QHY163M (and perhaps other models as well) will give the value, but divided
        // by 10. Ie, setting the gain to 420 will result in the gain being reported as 42.0
        // by the SDK.
        //
        // Other camera models (such as the QHY168C) will report absolutely false values for
        // their gain, so we must track that value internally because we will revcieve unreliable
        // answers from the camera.
        //
        // We will venture to detect this kind of situation by issuing a gain setting to the
        // camera and then testing the value returned by GetQHYCCDParam(). If we find that what
        // will flag it necessary to normalize Get'd values.
        //
        // Valid issue as of SDK V2019.02.11.0
        ///</summary>
        private bool QuirkUnreliableGain() {
            /* Save a copy of what our current gain is so we can restore it later */
            double saveGain = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN);

            /*
             * Set the gain to the maximum gain value minus one step for the camera. We will then query the camera and
             * test whether we get the value we set or not (which indicates the bug).
             */
            double wantGain = Info.GainMax - Info.GainStep;
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN, wantGain);

            double curGain = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN);

            /* Restore our original gain setting */
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_GAIN, saveGain);

            if (wantGain != curGain) {
                Logger.Debug("QHYCCD_QUIRK: This camera reports false gain values");
                return true;
            }

            return false;
        }

        ///<summary>
        // QHY Polemasters seem to offset the sensor Offset by +50 points.
        // An Offeset of 1 becomes 51, 80 becomes 130, etc. Here we try to detect this and account for it.
        //
        // Valid issue as of SDK V20180502_0
        ///</summary>
        private void QuirkInflatedOffset() {
            double saveOffset = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET);

            double wantOffset = 1;
            double gotOffset;

            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET, wantOffset);
            gotOffset = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET) - wantOffset;

            if (gotOffset != 0) {
                Logger.Debug($"QHYCCD_QUIRK: This camera inflates its Offset by {gotOffset}");
                Info.InflatedOff = (int)gotOffset;
            } else {
                Info.InflatedOff = 0;
            }

            /* Restore our original gain setting */
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_OFFSET, saveOffset);
        }

        ///<summary>
        // Some QHY cameras (such as the QHY163M) report that the CONTROL_USBTRAFFIC
        // control is available and the SDK will even tell you min and max values
        // for it, however it is managed by the camera and any attempt to set a value
        // for CONTROL_USBTRAFFIC will silently fail. Here we will detect this case so
        // that we can disable the UI control for USB bandwidth if this issue is present.
        //
        // Valid issue as of SDK V20180502_0
        ///</summary>
        private bool QuirkNoUSBTraffic() {
            double wantUSB;
            double saveUSB = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC);

            if (saveUSB != Info.USBMax) {
                wantUSB = Info.USBMax;
            } else {
                wantUSB = Info.USBMax - Info.USBStep;
            }
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC, wantUSB);
            double gotUSB = Sdk.GetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC);

            /* Check to see if it really changed */
            if (gotUSB == saveUSB) {
                Logger.Debug("QHYCCD_QUIRK: This camera does not really allow CONTROL_USBTRAFFIC settings");
                return true;
            }

            /* Restore the original USB traffic setting */
            _ = Sdk.SetControlValue(QhySdk.CONTROL_ID.CONTROL_USBTRAFFIC, saveUSB);
            return false;
        }

        /// <summary>
        /// This flag is used for switching between single mode and video mode 
        /// During this time the CoolerPower, Temperature, Gain, Offset and USBLimit values are returned from temporary stored values
        /// We also must remember the state of the cooler and whether it was on or off
        /// </summary>
        private bool internalReconnect = false;
        private int tempGain;
        private int tempOffset;
        private int tempUSBLimit;
        private double tempCoolerPower;
        private double tempTemperature;
        private bool tempCoolerOn;
    }
}