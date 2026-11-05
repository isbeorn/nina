#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FLI;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera {

    public class FLICamera : BaseINPC, ICamera {
        private uint CameraH;
        private bool _connected = false;
        private LibFLI.FLICameraInfo Info;
        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;
        private DateTime lastExposureStartTime;
        private double lastExposureDuration;
        private RbiExposureSettings lastRbiSettings = RbiExposureSettings.None;

        /*
         * Set by StopExposure() so that an in-progress RBI flood/flush sequence stops instead of
         * carrying on into the actual exposure
         */
        private volatile bool rbiAbortRequested = false;

        /*
         * Background flushing requires camera firmware 0x0110 or later. Determined at connect time.
         */
        private const uint BGFlushMinFWRev = 0x0110;
        private bool canBackgroundFlush = false;

        /*
         * Polling interval and timeout margin used while waiting on the RBI flood frame
         */
        private const int RbiPollIntervalMs = 200;
        private const int RbiFrameTimeoutMarginMs = 60000;

        public FLICamera(string camera, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            string[] cameraInfo;
            StringBuilder cameraSerial = new StringBuilder(64);
            uint rv;

            cameraInfo = camera.Split(';');
            Info.Id = cameraInfo[0];
            Info.Model = cameraInfo[1];

            Info.FWrev = 0x0;
            Info.HWrev = 0x0;

            if ((rv = LibFLI.FLIOpen(out CameraH, Info.Id, LibFLI.FLIDomains.DEV_CAMERA | LibFLI.FLIDomains.IF_USB)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIOpen() failed. Returned {rv}");
            }

            if ((rv = LibFLI.FLIGetSerialString(CameraH, cameraSerial, 64)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetSerialString() failed. Returned {rv}");
            }

            if ((rv = LibFLI.FLIGetFWRevision(CameraH, out Info.FWrev)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetFWRevision() failed. Returned {rv}");
            }

            if ((rv = LibFLI.FLIGetHWRevision(CameraH, out Info.HWrev)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetHWRevision() failed. Returned {rv}");
            }

            Info.Serial = cameraSerial.ToString();

            Logger.Debug($"FLI Camera: Found camera: {Description}");

            if ((rv = LibFLI.FLIClose(CameraH)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIClose() failed. Returned {rv}");
            }
        }

        public string Category => "Finger Lakes Instrumentation";

        public int BatteryLevel => -1;

        private readonly AsyncObservableCollection<BinningMode> _binningModes = [];

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                /*
                 * FLI cameras support asymmetrical binning modes, but with 16^2=256 possible binning combinations, we are going to keep
                 * drop-down list of bin modes limited to symmetrical ones. Assymmetrical bin modes are unlikely in an astrophoto context.
                 */
                if (_binningModes.Count == 0) {
                    for (short i = 1; i <= MaxBinX; i++) {
                        _binningModes.Add(new BinningMode(i, i));
                    }
                }

                return _binningModes;
            }
        }

        public short BinX {
            get => Info.BinX;
            set {
                uint rv;

                if (value < 1 || value > MaxBinX) {
                    throw new ArgumentOutOfRangeException(nameof(BinX), value, $"FLI: Horizontal binning must be between 1 and {MaxBinX}");
                }

                if (Connected) {
                    if ((rv = LibFLI.FLISetHBin(CameraH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: BinX.set failed. Returned {rv}");
                        return;
                    }

                    Info.BinX = value;
                }
            }
        }

        public short BinY {
            get => Info.BinY;
            set {
                uint rv;

                if (value < 1 || value > MaxBinY) {
                    throw new ArgumentOutOfRangeException(nameof(BinY), value, $"FLI: Vertical binning must be between 1 and {MaxBinY}");
                }

                if (Connected) {
                    if ((rv = LibFLI.FLISetVBin(CameraH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: BinY.set failed. Returned {rv}");
                        return;
                    }

                    Info.BinY = value;
                }
            }
        }

        /*
         * FLI cameras are always 16bit
         */
        public int BitDepth {
            get => 16;
            set {
                profileService.ActiveProfile.CameraSettings.BitDepth = value;
                RaisePropertyChanged();
            }
        }

        public CameraStates CameraState {
            get {
                CameraStates status;
                uint camstatus = (uint)LibFLI.CameraStatus.UNKNOWN;
                uint rv;

                if (Connected) {
                    if ((rv = LibFLI.FLIGetDeviceStatus(CameraH, ref camstatus)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGetDeviceStatus() failed. Returned {rv}");

                        camstatus = (uint)LibFLI.CameraStatus.UNKNOWN;
                    }
                }

                switch (camstatus & (long)LibFLI.CameraStatus.MASK) {
                    case (long)LibFLI.CameraStatus.EXPOSING:
                        status = CameraStates.Exposing;
                        break;

                    case (long)LibFLI.CameraStatus.READING_CCD:
                        status = CameraStates.Reading;
                        break;

                    case (long)LibFLI.CameraStatus.WAITING_FOR_TRIGGER:
                    case (long)LibFLI.CameraStatus.DATA_READY:
                        status = CameraStates.Waiting;
                        break;

                    default:
                        status = CameraStates.Idle;
                        break;
                }

                return status;
            }
        }

        public int CameraXSize {
            get => Info.ImageX;
            private set => Info.ImageX = value;
        }

        public int CameraYSize {
            get => Info.ImageY;
            private set => Info.ImageY = value;
        }

        public bool CanGetGain => false;
        public bool CanSetGain => false;
        public bool CanSetOffset => false;

        /*
         * All FLI cameras have cooling controls
         */

        public bool CanSetTemperature => true;

        public bool CanSetUSBLimit => false;
        public bool CanShowLiveView => false;
        public bool CanSubSample => true;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaiseAllPropertiesChanged();
            }
        }

        public bool CoolerOn {
            get => Info.CoolerOn;
            set {
                uint rv;

                if (Connected && CanSetTemperature) {
                    if (value == false) {
                        /*
                         * There is no concept of "off" with FLI camera cooling. To effect an off state, we set the target temperature
                         * to 40C. This will effectively turn the cooler off. We do this directly and not through TemperatureSetPoint
                         * in order to maintain the user-specified set point.
                         */
                        if ((rv = LibFLI.FLISetTemperature(CameraH, 40.0)) != LibFLI.FLI_SUCCESS) {
                            Logger.Error($"FLI: FLISetTemperature failed. Returned {rv}");
                        }
                    } else {
                        /*
                         * To effect an "on" we need to kick thing into action ourselves.
                         */
                        TemperatureSetPoint = TemperatureSetPoint;
                    }

                    Info.CoolerOn = value;
                }
            }
        }

        public double CoolerPower {
            get {
                double power = double.NaN;
                uint rv;

                if (Connected && CanSetTemperature) {
                    if ((rv = LibFLI.FLIGetCoolerPower(CameraH, ref power)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: CoolerPower.get failed. Returned {rv}");
                    }
                }

                return power;
            }
        }

        public string Description => $"{Name} ({Id}) SN: {Info.Serial}";

        public string FirmwareVersion => Info.FWrev.ToString();
        public string HardwareVersion => Info.HWrev.ToString();

        public bool DewHeaterOn {
            get => false;
            set {
            }
        }

        private string driverInfo = string.Empty;

        public string DriverInfo {
            get {
                StringBuilder version = new StringBuilder(128);

                if (Connected && string.IsNullOrEmpty(driverInfo)) {
                    if (LibFLI.FLIGetLibVersion(version, 128) == 0) {
                        driverInfo = version.ToString();
                    }
                }

                return driverInfo;
            }
        }

        public string DriverVersion => string.Empty;

        public double ElectronsPerADU => double.NaN;
        public bool EnableSubSample { get; set; }

        public IList<string> SupportedActions => new List<string> { SetShutterAction };

        public uint ExposureLength {
            get => Info.ExposureLength;
            set {
                uint rv;

                if (Connected) {
                    Logger.Debug($"FLI: Setting exposure time to {value}ms");

                    if ((rv = LibFLI.FLISetExposureTime(CameraH, value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetExposureTime() failed. Returned {rv}");
                    }

                    Info.ExposureLength = value;
                }
            }
        }

        /*
         * FLI SDK says the minimum exposure time is 1ms and does not specify a maximum.
         * We will set 1000 seconds (2h 46m 40s) as the maximum.
         */
        public double ExposureMax => 1e4;
        public double ExposureMin => 1e-3;

        public int Gain {
            get => -1;
            set {
            }
        }

        public LibFLI.FLIFrameType FrameType {
            get => Info.FrameType;
            set {
                uint rv;

                if (Connected) {
                    Logger.Debug($"FLI: Setting frame type to {value}");

                    if ((rv = LibFLI.FLISetFrameType(CameraH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetFrameType() failed. Returned {rv}");
                    }

                    Info.FrameType = value;
                }
            }
        }

        public int GainMax => 0;
        public int GainMin => 0;
        public IList<int> Gains => new List<int>();
        public bool HasBattery => false;
        public bool HasDewHeater => false;
        public bool HasSetupDialog => false;

        /*
         * All FLI cameras have shutters
         */
        public bool HasShutter => true;

        public string Id => Info.Serial;

        public bool LiveViewEnabled {
            get => false;
            set {
            }
        }

        /*
         * FLI SDK specifies that 16 is the maximum bin level for both X and Y axis
         * regardless of camera model.
         */
        public short MaxBinX => 16;
        public short MaxBinY => 16;

        public string Name => Info.Model;
        public string DisplayName => $"{Name} ({(Id.Length > 8 ? Id[^8..] : Id)})";

        public int Offset {
            get => -1;
            set {
            }
        }

        public int OffsetMax => 0;
        public int OffsetMin => 0;

        private double pixelSizeX = 0;

        public double PixelSizeX {
            get {
                double junk = 0;

                if (Connected && (pixelSizeX == 0)) {
                    LibFLI.FLIGetPixelSize(CameraH, ref pixelSizeX, ref junk);
                    Info.PixelWidthX = pixelSizeX * 1e6;
                }
                return Info.PixelWidthX;
            }
        }

        private double pixelSizeY = 0;

        public double PixelSizeY {
            get {
                double junk = 0;

                if (Connected && (pixelSizeY == 0)) {
                    LibFLI.FLIGetPixelSize(CameraH, ref junk, ref pixelSizeY);
                    Info.PixelWidthY = pixelSizeY * 1e6;
                }
                return Info.PixelWidthY;
            }
        }

        public short ReadoutMode {
            get {
                uint mode = 0;
                uint rv;

                if (Connected) {
                    if ((rv = LibFLI.FLIGetCameraMode(CameraH, ref mode)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGetCameraMode() failed. Returned {rv}");

                        return 0;
                    }

                    Logger.Trace($"FLI: Current readout mode: {mode} ({ReadoutModes.Cast<string>().ToArray()[mode]})");

                    return (short)mode;
                } else {
                    return 0;
                }
            }
            set {
                uint timeLeft = 0;
                uint rv;

                if (Connected && (value != ReadoutMode) && (value < ReadoutModes.Count)) {
                    string modeName = ReadoutModes[value];

                    Logger.Debug($"FLI: ReadoutMode: Setting readout mode to {value} ({modeName})");

                    if ((rv = LibFLI.FLISetCameraMode(CameraH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetCameraMode() failed. Returned {rv}");
                    }

                    /*
                     * After changing the readout mode, we must do a 0-second dark frame exposure (the data from which we do not care about)
                     * This serves to flush the electronics
                     */
                    Logger.Debug($"FLI: ReadoutMode: Flushing sensor after changing readout mode to {value} ({modeName})");

                    /*
                     * Read the flush frame as the full visible area at 1x1 binning, so the number of rows and columns
                     * read out matches what the camera delivers. StartExposure() sets binning and area for the real exposure.
                     */
                    uint flushWidth = (uint)CameraXSize;
                    uint flushHeight = (uint)CameraYSize;
                    uint flushUlX = Info.ExposureOriginPixelX;
                    uint flushUlY = Info.ExposureOriginPixelY;

                    SetBinning(1, 1);

                    if ((rv = LibFLI.FLISetImageArea(CameraH, flushUlX, flushUlY, flushUlX + flushWidth, flushUlY + flushHeight)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: ReadoutMode: FLISetImageArea() failed. Returned {rv}");
                    }

                    FrameType = LibFLI.FLIFrameType.DARK;
                    ExposureLength = 0;

                    Logger.Debug($"FLI: ReadoutMode: Exposing DARK frame following readout mode change");
                    if ((rv = LibFLI.FLIExposeFrame(CameraH)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIExposeFrame() failed. Returned {rv}");
                    }

                    if ((rv = LibFLI.FLIGetExposureStatus(CameraH, ref timeLeft)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGetExposureStatus() failed. Returned {rv}");
                    }

                    Logger.Debug($"FLI: ReadoutMode: Pausing for {timeLeft}ms after DARK frame exposure");
                    Thread.Sleep((int)timeLeft);

                    ushort[] rowData = new ushort[flushWidth];

                    Logger.Debug($"FLI: ReadoutMode: Reading out DARK frame following readout mode change ({flushWidth}x{flushHeight})");

                    for (int r = 0; r < flushHeight; r++) {
                        if ((rv = LibFLI.FLIGrabRow(CameraH, rowData, (nuint)flushWidth)) != LibFLI.FLI_SUCCESS) {
                            Logger.Error($"FLI: FLIGrabRow() failed at row {r + 1}. Returned {rv}");

                            if ((rv = LibFLI.FLICancelExposure(CameraH)) != LibFLI.FLI_SUCCESS) {
                                Logger.Error($"FLI: FLICancelExposure() failed. Returned {rv}");
                            }

                            break;
                        }
                    }

                    BGFlushStart();

                    Logger.Debug($"FLI: ReadoutMode: Flush completed. Readout mode changed to {value} ({modeName})");
                }
            }
        }

        public short ReadoutModeForNormalImages {
            get => Info.ReadoutModeNormal;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    Info.ReadoutModeNormal = value;
                } else {
                    Info.ReadoutModeNormal = 0;
                }

                RaisePropertyChanged();
            }
        }

        public short ReadoutModeForSnapImages {
            get => Info.ReadoutModeSnap;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    Info.ReadoutModeSnap = value;
                } else {
                    Info.ReadoutModeSnap = 0;
                }

                RaisePropertyChanged();
            }
        }

        public IList<string> ReadoutModes {
            get => Info.ReadoutModes;
            set => Info.ReadoutModes = value;
        }

        public string SensorName => string.Empty;

        public SensorType SensorType => SensorType.Monochrome;

        public short BayerOffsetX => 0;
        public short BayerOffsetY => 0;

        public int SubSampleHeight { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }

        public double Temperature {
            get {
                double ccdtemp = double.NaN;
                uint rv;

                if (Connected) {
                    if ((rv = LibFLI.FLIReadTemperature(CameraH, (uint)LibFLI.FLIChannel.CCD, ref ccdtemp)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIReadTemperature (CCD) failed. Returned {rv}");
                    }
                }

                return ccdtemp;
            }
        }

        public double TemperatureSetPoint {
            get => Info.CoolerTargetTemp;
            set {
                uint rv;

                if (Connected) {
                    if ((rv = LibFLI.FLISetTemperature(CameraH, value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetTemperature failed. Returned {rv}");
                        return;
                    }

                    Info.CoolerTargetTemp = value;
                    RaisePropertyChanged();
                }
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

        public void AbortExposure() {
            uint rv;

            Logger.Debug($"FLI: Aborting exposure");

            rbiAbortRequested = true;

            if (Connected) {
                if ((rv = LibFLI.FLICancelExposure(CameraH)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI: FLICancelExposure() failed. Returned {rv}");
                }
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task.Run(() => {
                uint ul_x = 0, ul_y = 0, lr_x = 0, lr_y = 0;
                var modeList = new List<string>();
                var modeString = new StringBuilder(32);
                uint modeIndex = 0;
                bool success = false;
                uint rv;

                try {
                    /*
                     * Try to open the camera
                     */
                    if ((rv = LibFLI.FLIOpen(out CameraH, Info.Id, LibFLI.FLIDomains.DEV_CAMERA | LibFLI.FLIDomains.IF_USB)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIOpen() failed. Returned {rv}");
                        return success;
                    }

                    /*
                     * Determine if the camera firmware supports background flushing
                     */
                    if ((rv = LibFLI.FLIGetFWRevision(CameraH, out Info.FWrev)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGetFWRevision() failed. Returned {rv}");
                    }

                    canBackgroundFlush = Info.FWrev >= BGFlushMinFWRev;
                    Logger.Debug($"FLI: Firmware revision 0x{Info.FWrev:X4}. Background flushing is {(canBackgroundFlush ? "supported" : "not supported")}");

                    /*
                     * Get sensor pixel dimensions
                     */
                    if ((rv = LibFLI.FLIGetVisibleArea(CameraH, ref ul_x, ref ul_y, ref lr_x, ref lr_y)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGetVisibleArea() failed. Returned {rv}");
                        return success;
                    }

                    Logger.Debug($"FLI: Visible Area: ul_X={ul_x}, ul_Y={ul_y}, lr_X={lr_x}, lr_Y={lr_y}");
                    visibleArea = (ul_x, ul_y, lr_x, lr_y);

                    if ((rv = LibFLI.FLIGetArrayArea(CameraH, ref ul_x, ref ul_y, ref lr_x, ref lr_y)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGetArrayArea() failed. Returned {rv}. Overscan will not be available");
                        arrayArea = visibleArea;
                    } else {
                        Logger.Debug($"FLI: Array Area: ul_X={ul_x}, ul_Y={ul_y}, lr_X={lr_x}, lr_Y={lr_y}");
                        arrayArea = (ul_x, ul_y, lr_x, lr_y);
                    }

                    ApplySensorArea();

                    /*
                     * Query the camera for a list of readout modes
                     */
                    while (LibFLI.FLIGetCameraModeString(CameraH, modeIndex, modeString, 32) == LibFLI.FLI_SUCCESS) {
                        var modeName = modeString.ToString().Trim();
                        Logger.Debug($"FLI: Adding mode index {modeIndex} (\"{modeName}\") to list of modes");
                        modeList.Add(modeName);

                        modeIndex++;
                    }
                    Logger.Debug($"FLI: FLIGetCameraModeString() stopped at mode_index={modeIndex}. No more modes available.");

                    ReadoutModes = [.. modeList];

                    /*
                     * Attempt to turn on background flushing. Save the result for later attempts
                     * Background flushing is automatically disabled whenever an exposure is made or
                     * the shutter is opened, so we must turn it on again after those events.
                     *
                     * RBI flushes are performed explicitly by FloodControl(), so firmware pre-exposure flushes are disabled.
                     */
                    BGFlushStart();

                    if (!ApplyFirmwareFlushCount(0)) {
                        return success;
                    }

                    /*
                     * Hold the shutter open for the session if configured to
                     */
                    if (FLIKeepShutterOpen) {
                        ControlShutter(LibFLI.FLIShutter.OPEN);
                    }

                    /*
                     * We can now say that we are connected!
                     */
                    Connected = true;
                    success = true;

                    /*
                     * Set some defaults
                     */
                    CoolerOn = false;
                    BitDepth = 16;

                    RaisePropertyChanged(nameof(Connected));
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return success;
            });
        }

        public void Disconnect() {
            if (Connected == false)
                return;

            BGFlushStop();

            /*
             * As in the FLI Legacy driver, always close the shutter on disconnect
             */
            ControlShutter(LibFLI.FLIShutter.CLOSE);

            CoolerOn = false;
            Connected = false;

            LibFLI.FLIClose(CameraH);
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                /*
                 * As in the FLI Legacy driver, the frame is complete when the camera is idle (or cannot report
                 * its status) with no exposure time left, or when it reports data ready.
                 */
                while (true) {
                    if (!IsFrameComplete(out uint timeLeft)) {
                        await Task.Delay(timeLeft > RbiPollIntervalMs ? RbiPollIntervalMs : (int)timeLeft + 1, token);
                        continue;
                    }

                    return;
                }
            }
        }

        public Task<IExposureData> DownloadExposure(CancellationToken ct) {
            return Task.Run<IExposureData>(async () => {
                uint rv;

                int width = (int)Info.ExposureWidth;
                int height = (int)Info.ExposureHeight;
                int imgSize = width * height;
                uint timeLeft = 0;

                /*
                 * Make sure to wait for any residual time to allow the camera to complete the exposure
                 */
                if ((rv = LibFLI.FLIGetExposureStatus(CameraH, ref timeLeft)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI: FLIGetExposureStatus() failed. Returned {rv}");
                }

                if (timeLeft > 0) {
                    Logger.Debug($"FLI: DownloadExposure() pausing for {timeLeft}ms for exposure completion.");
                    await Task.Delay((int)timeLeft, ct);
                }

                Logger.Debug($"FLI: Fetching {height} rows from the camera");

                // FLI cameras are always 16bit
                ushort[] imgData = GC.AllocateUninitializedArray<ushort>(imgSize);

                BGFlushStop();

                try {
                    int failedRow = GrabRows(imgData, width, height, ct, out rv);

                    if (failedRow >= 0) {
                        bool cancelled = rv == LibFLI.FLI_SUCCESS;

                        if (cancelled) {
                            Logger.Info($"FLI: Image download cancelled at row {failedRow + 1} of {height}");
                        } else {
                            Logger.Error($"FLI: FLIGrabRow() failed at row {failedRow + 1}. Returned {rv}");
                        }

                        /*
                         * Discard the rest of the frame so that the camera is ready for the next exposure
                         */
                        if ((rv = LibFLI.FLICancelExposure(CameraH)) != LibFLI.FLI_SUCCESS) {
                            Logger.Error($"FLI: FLICancelExposure() failed. Returned {rv}");
                        }

                        if (cancelled) {
                            throw new OperationCanceledException(ct);
                        }

                        throw new Exception($"FLI: Image download failed reading row {failedRow + 1} of {height}");
                    }
                } finally {
                    BGFlushStart();
                }

                var metaData = new ImageMetaData();
                metaData.FromCamera(this);
                metaData.Image.SetExposureTimes(lastExposureStartTime, lastExposureStartTime + TimeSpan.FromSeconds(lastExposureDuration));

                /*
                 * RBI headers come from the settings captured when the exposure was started, not the current
                 * settings, which may have been changed while the exposure was in progress.
                 */
                var rbi = lastRbiSettings;

                var rbiFlushKeyword = new BoolMetaDataHeader("RBIFLUSH", rbi.Performed, "RBI flushing occurred prior to exposure");
                metaData.GenericHeaders.Add(rbiFlushKeyword);

                if (rbi.Performed) {
                    var rbiFlushCountKeyword = new UIntMetaDataHeader("RBICOUNT", rbi.FlushCount, "Number of RBI flushes performed");
                    metaData.GenericHeaders.Add(rbiFlushCountKeyword);

                    var rbiFloodDurationKeyword = new DoubleMetaDataHeader("RBILEN", rbi.FloodDuration * 1000, "[ms] Length of RBI flood exposure");
                    metaData.GenericHeaders.Add(rbiFloodDurationKeyword);

                    var rbiFlushBinXKeyword = new IntMetaDataHeader("RBIXBIN", rbi.BinX, "X axis binning used for RBI flush readout");
                    metaData.GenericHeaders.Add(rbiFlushBinXKeyword);

                    var rbiFlushBinYKeyword = new IntMetaDataHeader("RBIYBIN", rbi.BinY, "Y axis binning used for RBI flush readout");
                    metaData.GenericHeaders.Add(rbiFlushBinYKeyword);
                }

                return exposureDataFactory.CreateImageArrayExposureData(
                    input: imgData,
                    width: width,
                    height: height,
                    bitDepth: this.BitDepth,
                    isBayered: this.SensorType != SensorType.Monochrome,
                    metaData: metaData);
            }, ct);
        }

        /*
         * Read the frame row by row directly into the destination buffer, avoiding an intermediate row buffer
         * and copy. Returns the zero-based index of the row that failed, or -1 on success. If cancellation is
         * requested, reading stops and the index of the next unread row is returned with rv set to FLI_SUCCESS.
         */
        private unsafe int GrabRows(ushort[] dest, int width, int height, CancellationToken ct, out uint rv) {
            const int CancellationCheckInterval = 64;
            rv = LibFLI.FLI_SUCCESS;

            fixed (ushort* p = dest) {
                ushort* row = p;

                for (int r = 0; r < height; r++, row += width) {
                    if (r % CancellationCheckInterval == 0 && ct.IsCancellationRequested) {
                        return r;
                    }

                    if ((rv = LibFLI.FLIGrabRow(CameraH, row, (nuint)width)) != LibFLI.FLI_SUCCESS) {
                        return r;
                    }
                }
            }

            return -1;
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public void SetBinning(short x, short y) {
            Logger.Debug($"FLI: Setting binning to {x}x{y}");

            BinX = x;
            BinY = y;
        }

        /*
         * Reject exposure parameters the camera cannot honor before anything (RBI flood included) is sent to it.
         */
        private void ValidateExposureParameters(CaptureSequence sequence) {
            short binX = sequence.Binning.X;
            short binY = sequence.Binning.Y;

            if (binX < 1 || binX > MaxBinX || binY < 1 || binY > MaxBinY) {
                throw new ArgumentOutOfRangeException(nameof(sequence.Binning), $"FLI: Binning {binX}x{binY} is outside the supported range of 1x1 to {MaxBinX}x{MaxBinY}");
            }

            if (double.IsNaN(sequence.ExposureTime) || sequence.ExposureTime < 0 || sequence.ExposureTime > ExposureMax) {
                throw new ArgumentOutOfRangeException(nameof(sequence.ExposureTime), sequence.ExposureTime, $"FLI: Exposure time must be between 0 and {ExposureMax} seconds");
            }

            if (EnableSubSample) {
                if (SubSampleWidth < binX || SubSampleHeight < binY) {
                    throw new ArgumentOutOfRangeException(nameof(SubSampleWidth), $"FLI: Subframe {SubSampleWidth}x{SubSampleHeight} is smaller than one {binX}x{binY} binned pixel");
                }

                if (SubSampleX < 0 || SubSampleY < 0 ||
                    SubSampleX + SubSampleWidth > CameraXSize || SubSampleY + SubSampleHeight > CameraYSize) {
                    throw new ArgumentOutOfRangeException(nameof(SubSampleX), $"FLI: Subframe X={SubSampleX}, Y={SubSampleY}, W={SubSampleWidth}, H={SubSampleHeight} lies outside the {CameraXSize}x{CameraYSize} sensor area");
                }
            }
        }

        public void SetupDialog() {
        }

        private bool IsSnap { get; set; } = false;
        private bool IsFlat { get; set; } = false;

        public void StartExposure(CaptureSequence sequence) {
            bool isDarkFrame;
            uint ul_x, ul_y, lr_x, lr_y;
            uint rv;

            IsSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;
            IsFlat = sequence.ImageType == CaptureSequence.ImageTypes.FLAT;
            rbiAbortRequested = false;

            ValidateExposureParameters(sequence);

            /*
             * Set the desired readout mode. This is done before any RBI management so that the flood frame
             * is read out in the same mode as the exposure that follows it, and so that a readout mode change
             * (with its own flush frame) cannot happen between the RBI flood and the exposure.
             */
            ReadoutMode = IsSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

            /*
             * Pre-exposure RBI management. As in the FLI Legacy driver, the flood readout counts as the
             * first flush, so a flush count of 0 means no RBI flood is performed.
             * All RBI settings are captured once here so that the flood and the image headers use the same values.
             */
            var rbiSettings = RbiExposureSettings.None;

            if (FLIEnableFloodFlush && (!IsSnap || FLIEnableSnapshotFloodFlush) && (!IsFlat || FLIEnableFlatFloodFlush)) {
                rbiSettings = new RbiExposureSettings(
                    Performed: true,
                    FlushCount: FLIFlushCount,
                    FloodDuration: FLIFloodDuration,
                    BinX: FLIFloodBinAuto ? sequence.Binning.X : (short)FLIFloodBinX,
                    BinY: FLIFloodBinAuto ? sequence.Binning.Y : (short)FLIFloodBinY);
            }

            lastRbiSettings = rbiSettings;

            if (rbiSettings.Performed) {
                if (!FloodControl(rbiSettings)) {
                    if (rbiAbortRequested) {
                        Logger.Info("FLI: RBI sequence aborted. Exposure not started.");
                        return;
                    }

                    throw new Exception("FLI: RBI flood failed. Exposure not started.");
                }
            }

            /*
             * Set the frame type for this exposure
             * Darks and Bias frames both get set to same type.
             */
            isDarkFrame = sequence.ImageType == CaptureSequence.ImageTypes.BIAS ||
                          sequence.ImageType == CaptureSequence.ImageTypes.DARK;

            FrameType = isDarkFrame ? LibFLI.FLIFrameType.DARK : LibFLI.FLIFrameType.NORMAL;

            /*
             * Convert exposure seconds to milliseconds and set the camera with it.
             */
            ExposureLength = (uint)Math.Round(Math.Clamp(sequence.ExposureTime, ExposureMin, ExposureMax) * 1000);

            /*
             * Set bin levels
             */
            SetBinning(sequence.Binning.X, sequence.Binning.Y);

            /*
             * Set the sensor area we want to capture
             */
            GetImageArea(BinX, BinY, out ul_x, out ul_y, out uint width, out uint height);
            Info.ExposureWidth = width;
            Info.ExposureHeight = height;
            lr_x = Info.ExposureEndPixelX = ul_x + width;
            lr_y = Info.ExposureEndPixelY = ul_y + height;

            if ((rv = LibFLI.FLISetImageArea(CameraH, ul_x, ul_y, lr_x, lr_y)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLISetImageArea() failed. Returned {rv}");
            }

            Logger.Debug($"FLI: Setting exposure area to: ul_X={ul_x}, ul_Y={ul_y}, lr_X={lr_x}, lr_Y={lr_y} ({Info.ExposureWidth}x{Info.ExposureHeight})");

            /*
             * Initiate the exposure. This blocks until finished.
             */
            DisableTDI();

            Logger.Debug($"FLI: Triggering exposure");

            lastExposureDuration = ExposureLength / 1000d;
            lastExposureStartTime = DateTime.UtcNow;

            if ((rv = LibFLI.FLIExposeFrame(CameraH)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIExposeFrame() failed. Returned {rv}");
                return;
            }
        }

        public void StopExposure() {
            uint rv;

            Logger.Debug($"FLI: Cancelling exposure");

            rbiAbortRequested = true;

            if (Connected) {
                if ((rv = LibFLI.FLIEndExposure(CameraH)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI: FLIEndExposure() failed. Returned {rv}");
                }
            }
        }

        public void StartLiveView(CaptureSequence sequence) {
            throw new System.NotImplementedException();
        }

        /*
         * Compute the image area for FLISetImageArea(), as in the FLI Legacy driver:
         *   - The start position is the binned start (StartX/StartY) multiplied by the bin factor (unbinned
         *     pixels), offset by the visible area origin.
         *   - The lower right corner is the upper left corner plus the binned width and height.
         * Subframe values from NINA are in unbinned pixels and are converted to binned start and size first.
         */
        private void GetImageArea(short binX, short binY, out uint ul_x, out uint ul_y, out uint width, out uint height) {
            int startX = 0, startY = 0;
            int numX = CameraXSize / binX;
            int numY = CameraYSize / binY;

            if (EnableSubSample) {
                startX = SubSampleX / binX;
                startY = SubSampleY / binY;
                numX = SubSampleWidth / binX;
                numY = SubSampleHeight / binY;

                /*
                 * Keep the subframe within the visible area
                 */
                startX = Math.Clamp(startX, 0, Math.Max((CameraXSize / binX) - 1, 0));
                startY = Math.Clamp(startY, 0, Math.Max((CameraYSize / binY) - 1, 0));
                numX = Math.Clamp(numX, 1, (CameraXSize / binX) - startX);
                numY = Math.Clamp(numY, 1, (CameraYSize / binY) - startY);

                Logger.Debug($"FLI: Subframe requested: X={SubSampleX}, Y={SubSampleY}, W={SubSampleWidth}, H={SubSampleHeight} -> binned StartX={startX}, StartY={startY}, NumX={numX}, NumY={numY}");
            }

            ul_x = Info.ExposureOriginPixelX + (uint)(startX * binX);
            ul_y = Info.ExposureOriginPixelY + (uint)(startY * binY);
            width = (uint)numX;
            height = (uint)numY;
        }

        public void StopLiveView() {
            throw new System.NotImplementedException();
        }

        /*
         * RBI (residual bulk image) mitigation:
         *
         *   1. Flood the sensor with the NIR pre-flash (RBI_FLUSH frame) for FLIFloodDuration, then read it out.
         *      This readout counts as the first of the FLIFlushCount flushes.
         *   2. The remaining FLIFlushCount - 1 flushes are zero-length dark frames that are each exposed and
         *      read out, as in the FLI Legacy driver. Firmware pre-exposure flushes (FLISetNFlushes) are not
         *      honored by all camera families, so they are not relied upon.
         *
         * Each frame is polled
         * As in the FLI Legacy driver, it is taken at the pending exposure's binning and image area. StartExposure()
         * re-applies binning, frame type, exposure time and image area for the real exposure afterwards.
         */
        private bool FloodControl(RbiExposureSettings rbi) {
            uint rv;
            short binX = rbi.BinX;
            short binY = rbi.BinY;

            GetImageArea(binX, binY, out uint ul_x, out uint ul_y, out uint width, out uint height);
            uint lr_x = ul_x + width;
            uint lr_y = ul_y + height;

            Logger.Debug($"FLI: RBI: {rbi.FlushCount} total flush(es): 1 flood frame ({rbi.FloodDuration * 1e3}ms) + {rbi.DarkFlushCount} dark frame(s), at {binX}x{binY} binning");

            SetBinning(binX, binY);

            if ((rv = LibFLI.FLISetImageArea(CameraH, ul_x, ul_y, lr_x, lr_y)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: RBI: FLISetImageArea() failed. Returned {rv}");
                return false;
            }

            Logger.Debug($"FLI: RBI: Setting exposure area to: ul_X={ul_x}, ul_Y={ul_y}, lr_X={lr_x}, lr_Y={lr_y} ({width}x{height})");

            /*
             * Flood
             */
            FrameType = LibFLI.FLIFrameType.RBI_FLUSH;
            ExposureLength = (uint)Math.Round(rbi.FloodDuration * 1000);

            if (!RbiExposeAndDiscard("flood", width, height)) {
                return false;
            }

            /*
             * Flushes
             */
            FrameType = LibFLI.FLIFrameType.DARK;
            ExposureLength = 0;

            for (uint i = 1; i <= rbi.DarkFlushCount; i++) {
                if (!RbiExposeAndDiscard($"flush {i}/{rbi.DarkFlushCount}", width, height)) {
                    return false;
                }
            }

            Logger.Debug("FLI: RBI: Operation completed.");
            return true;
        }

        /*
         * Expose one RBI frame, wait for the camera to finish it, then read it out and discard it
         */
        private bool RbiExposeAndDiscard(string step, uint width, uint height) {
            uint rv;

            if (rbiAbortRequested) {
                Logger.Info($"FLI: RBI: Aborted before {step}");
                return false;
            }

            Logger.Debug($"FLI: RBI: Exposing {step} frame");

            DisableTDI();

            if ((rv = LibFLI.FLIExposeFrame(CameraH)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: RBI: FLIExposeFrame() failed for {step}. Returned {rv}");
                return false;
            }

            if (!RbiWaitForFrame(step)) {
                if ((rv = LibFLI.FLICancelExposure(CameraH)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI: RBI: FLICancelExposure() failed for {step}. Returned {rv}");
                }

                return false;
            }

            Logger.Debug($"FLI: RBI: Downloading {step} frame ({height} rows)");

            ushort[] rowData = new ushort[width];

            for (int r = 0; r < height; r++) {
                if ((rv = LibFLI.FLIGrabRow(CameraH, rowData, (nuint)width)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI: RBI: FLIGrabRow() failed for {step} at row {r + 1}. Returned {rv}");
                    return false;
                }
            }

            return true;
        }

        /*
         * Poll the camera until the current RBI frame is complete. The frame is complete when the camera is
         * idle (or cannot report its status) with no exposure time left, or when it reports data ready.
         */
        private bool RbiWaitForFrame(string step) {
            uint timeLeft;
            int timeoutMs = (int)ExposureLength + RbiFrameTimeoutMarginMs;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (true) {
                if (rbiAbortRequested) {
                    Logger.Info($"FLI: RBI: Aborted during {step}");
                    return false;
                }

                if (IsFrameComplete(out timeLeft)) {
                    return true;
                }

                if (stopwatch.ElapsedMilliseconds > timeoutMs) {
                    Logger.Error($"FLI: RBI: Timed out after {stopwatch.ElapsedMilliseconds}ms waiting for {step} frame (timeLeft={timeLeft}ms)");
                    return false;
                }

                Thread.Sleep(timeLeft > RbiPollIntervalMs ? RbiPollIntervalMs : (int)timeLeft + 1);
            }
        }

        /*
         * A frame is complete when the camera is idle (or cannot report its status) with no exposure time left,
         * or when it reports data ready. If the status cannot be read, the frame is treated as complete so that
         * callers do not wait forever.
         */
        private bool IsFrameComplete(out uint timeLeft) {
            uint rv;
            uint camstatus = (uint)LibFLI.CameraStatus.UNKNOWN;
            timeLeft = 0;

            if ((rv = LibFLI.FLIGetDeviceStatus(CameraH, ref camstatus)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetDeviceStatus() failed. Returned {rv}");
                return true;
            }

            if ((rv = LibFLI.FLIGetExposureStatus(CameraH, ref timeLeft)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetExposureStatus() failed. Returned {rv}");
                timeLeft = 0;
                return true;
            }

            bool isUnknown = camstatus == (uint)LibFLI.CameraStatus.UNKNOWN;
            bool isIdle = isUnknown || (camstatus & (uint)LibFLI.CameraStatus.MASK) == (uint)LibFLI.CameraStatus.IDLE;
            bool isDataReady = !isUnknown && (camstatus & (uint)LibFLI.CameraStatus.DATA_READY) != 0;

            return (isIdle && timeLeft == 0) || isDataReady;
        }

        private void DisableTDI() {
            uint rv;

            if ((rv = LibFLI.FLISetTDI(CameraH, 0, 0)) != LibFLI.FLI_SUCCESS) {
                Logger.Debug($"FLI: FLISetTDI() failed. Returned {rv}");
            }
        }

        private void BGFlushStart() {
            uint rv;

            if (!canBackgroundFlush || !FLIEnableFloodFlush || !RBIBackgroundFlush) {
                return;
            }

            Logger.Debug("FLI: Starting background flushing...");

            if ((rv = LibFLI.FLIControlBackgroundFlush(CameraH, LibFLI.FLIBGFlush.START)) != LibFLI.FLI_SUCCESS) {
                Logger.Info($"FLI: FLIControlBackgroundFlush() failed. Returned {rv}");
            } else {
                Logger.Debug("FLI: Background flushing started!");
            }
        }

        private void BGFlushStop() {
            uint rv;

            if (!canBackgroundFlush) {
                return;
            }

            Logger.Debug("FLI: Stopping background flushing...");

            if ((rv = LibFLI.FLIControlBackgroundFlush(CameraH, LibFLI.FLIBGFlush.STOP)) != LibFLI.FLI_SUCCESS) {
                Logger.Info($"FLI: FLIControlBackgroundFlush() failed.  Returned {rv}");
            } else {
                Logger.Debug("FLI: Background flushing stopped.");
            }
        }

        public bool CanBackgroundFlush => canBackgroundFlush;

        public bool RBIBackgroundFlush {
            get => profileService.ActiveProfile.CameraSettings.RBIBackgroundFlush;
            set {
                if (RBIBackgroundFlush == value) {
                    return;
                }

                profileService.ActiveProfile.CameraSettings.RBIBackgroundFlush = value;

                if (Connected) {
                    if (value) {
                        if (CameraState == CameraStates.Idle) {
                            BGFlushStart();
                        }
                    } else {
                        BGFlushStop();
                    }
                }

                RaisePropertyChanged();
            }
        }

        public bool FLIEnableFloodFlush {
            get => profileService.ActiveProfile.CameraSettings.FLIEnableFloodFlush;
            set {
                if (FLIEnableFloodFlush == value) {
                    return;
                }

                profileService.ActiveProfile.CameraSettings.FLIEnableFloodFlush = value;

                if (Connected) {
                    if (value) {
                        if (CameraState == CameraStates.Idle) {
                            BGFlushStart();
                        }
                    } else {
                        BGFlushStop();
                    }
                }

                RaisePropertyChanged();
            }
        }

        public double FLIFloodDuration {
            get => profileService.ActiveProfile.CameraSettings.FLIFloodDuration;
            set {
                double time = value;

                time = Math.Max(time, ExposureMin);
                time = Math.Min(time, ExposureMax);

                profileService.ActiveProfile.CameraSettings.FLIFloodDuration = time;
                RaisePropertyChanged();
            }
        }

        public bool FLIFloodBinAuto {
            get => profileService.ActiveProfile.CameraSettings.FLIFloodBinAuto;
            set {
                profileService.ActiveProfile.CameraSettings.FLIFloodBinAuto = value;
                RaisePropertyChanged();
            }
        }

        public BinningMode FLIFloodBin {
            get => profileService.ActiveProfile.CameraSettings.FLIFloodBin ?? new BinningMode(1, 4);
            set {
                profileService.ActiveProfile.CameraSettings.FLIFloodBin = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FLIFloodBinX));
                RaisePropertyChanged(nameof(FLIFloodBinY));
            }
        }

        public int FLIFloodBinX {
            get => Math.Clamp((int)FLIFloodBin.X, 1, MaxBinX);
            set => FLIFloodBin = new BinningMode((short)Math.Clamp(value, 1, MaxBinX), (short)FLIFloodBinY);
        }

        public int FLIFloodBinY {
            get => Math.Clamp((int)FLIFloodBin.Y, 1, MaxBinY);
            set => FLIFloodBin = new BinningMode((short)FLIFloodBinX, (short)Math.Clamp(value, 1, MaxBinY));
        }

        public uint FLIFlushCount {
            get => Math.Max(profileService.ActiveProfile.CameraSettings.FLIFlushCount, 1u);
            set {
                /*
                 * Total RBI flushes, counting the flood readout as the first (as in the FLI Legacy driver).
                 * The flood readout always happens, so the minimum is 1.
                 */
                profileService.ActiveProfile.CameraSettings.FLIFlushCount = Math.Max(value, 1u);
                RaisePropertyChanged();
            }
        }

        /*
         * The RBI settings used for a single exposure, captured when the exposure is started. The flood readout
         * is the first RBI flush; the rest are zero-length dark frames read out by FloodControl().
         */
        private sealed record RbiExposureSettings(bool Performed, uint FlushCount, double FloodDuration, short BinX, short BinY) {
            public static readonly RbiExposureSettings None = new(false, 0, 0d, 1, 1);

            public uint DarkFlushCount => FlushCount > 0 ? FlushCount - 1 : 0;
        }

        private bool ApplyFirmwareFlushCount(uint nflushes) {
            uint rv;

            Logger.Debug($"FLI: Setting the camera to execute {nflushes} pre-exposure flushes");

            if ((rv = LibFLI.FLISetNFlushes(CameraH, nflushes)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLISetNFlushes() failed. Returned {rv}");
                return false;
            }

            return true;
        }

        public bool FLIEnableFlatFloodFlush {
            get => profileService.ActiveProfile.CameraSettings.FLIEnableFlatFloodFlush;
            set {
                profileService.ActiveProfile.CameraSettings.FLIEnableFlatFloodFlush = value;
                RaisePropertyChanged();
            }
        }

        public bool FLIEnableSnapshotFloodFlush {
            get => profileService.ActiveProfile.CameraSettings.FLIEnableSnapshotFloodFlush;
            set {
                profileService.ActiveProfile.CameraSettings.FLIEnableSnapshotFloodFlush = value;
                RaisePropertyChanged();
            }
        }

        private (uint ulX, uint ulY, uint lrX, uint lrY) visibleArea;
        private (uint ulX, uint ulY, uint lrX, uint lrY) arrayArea;

        /*
         * Select the sensor area used for exposures: the visible area, or the full array area including overscan.
         */
        private void ApplySensorArea() {
            var (ulX, ulY, lrX, lrY) = FLIIncludeOverscan ? arrayArea : visibleArea;

            /*
             * Only the sensor geometry is updated here. The per-exposure area (ExposureWidth/Height and end pixels)
             * is set by StartExposure() and must not change while an exposure is in progress or being downloaded.
             */
            Info.ExposureOriginPixelX = ulX;
            Info.ExposureOriginPixelY = ulY;
            CameraXSize = (int)(lrX - ulX);
            CameraYSize = (int)(lrY - ulY);

            Logger.Debug($"FLI: Using {(FLIIncludeOverscan ? "array (with overscan)" : "visible")} area: ul_X={ulX}, ul_Y={ulY}, lr_X={lrX}, lr_Y={lrY} ({CameraXSize}x{CameraYSize})");

            RaisePropertyChanged(nameof(CameraXSize));
            RaisePropertyChanged(nameof(CameraYSize));
        }

        public bool FLIIncludeOverscan {
            get => profileService.ActiveProfile.CameraSettings.FLIIncludeOverscan;
            set {
                profileService.ActiveProfile.CameraSettings.FLIIncludeOverscan = value;

                if (Connected) {
                    ApplySensorArea();
                }

                RaisePropertyChanged();
            }
        }

        public bool FLIKeepShutterOpen {
            get => profileService.ActiveProfile.CameraSettings.FLIKeepShutterOpen;
            set {
                profileService.ActiveProfile.CameraSettings.FLIKeepShutterOpen = value;

                if (Connected) {
                    ControlShutter(value ? LibFLI.FLIShutter.OPEN : LibFLI.FLIShutter.CLOSE);
                }

                RaisePropertyChanged();
            }
        }

        private const string SetShutterAction = "SetShutter";

        private bool ControlShutter(LibFLI.FLIShutter state) {
            uint rv;

            Logger.Debug($"FLI: Setting shutter to {state}");

            if ((rv = LibFLI.FLIControlShutter(CameraH, (uint)state)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIControlShutter({state}) failed. Returned {rv}");
                return false;
            }

            return true;
        }

        /*
         * Custom actions, mirroring the FLI Legacy driver:
         *   SetShutter: "open" or "close"
         */
        public string Action(string actionName, string actionParameters) {
            if (string.Equals(actionName, SetShutterAction, StringComparison.OrdinalIgnoreCase)) {
                LibFLI.FLIShutter state = (actionParameters ?? string.Empty).Trim().ToLowerInvariant() switch {
                    "open" => LibFLI.FLIShutter.OPEN,
                    "close" => LibFLI.FLIShutter.CLOSE,
                    _ => throw new ArgumentException($"FLI: {SetShutterAction} parameter must be \"open\" or \"close\"", nameof(actionParameters)),
                };

                if (!Connected) {
                    throw new InvalidOperationException("FLI: Camera is not connected");
                }

                if (!ControlShutter(state)) {
                    throw new Exception($"FLI: Failed to set shutter to {state}");
                }

                return string.Empty;
            }

            throw new NotImplementedException($"FLI: Action {actionName} is not implemented");
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
}