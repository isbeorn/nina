#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * Copyright (c) 2019 Dale Ghent <daleg@elemental.org> All rights reserved.
 */

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Profile;
using FLI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Model.ImageData;
using NINA.Utility.WindowService;

namespace NINA.Model.MyCamera {

    public class FLICamera : BaseINPC, ICamera {
        private uint CameraH;
        private bool _connected = false;
        private LibFLI.FLICameraInfo Info;
        private IProfileService profileService;

        public FLICamera(string camera, IProfileService profileService) {
            this.profileService = profileService;
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

        private AsyncObservableCollection<BinningMode> _binningModes = new AsyncObservableCollection<BinningMode>();

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

        public string CameraState {
            get {
                string statusString;
                uint status = (uint)LibFLI.CameraStatus.UNKNOWN;
                uint rv;

                if (Connected) {
                    if ((rv = LibFLI.FLIGetDeviceStatus(CameraH, ref status)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGetDeviceStatus() failed. Returned {rv}");

                        status = (uint)LibFLI.CameraStatus.UNKNOWN;
                    }
                }

                switch (status & (long)LibFLI.CameraStatus.MASK) {
                    case (long)LibFLI.CameraStatus.IDLE:
                        statusString = "Idle";
                        break;

                    case (long)LibFLI.CameraStatus.EXPOSING:
                        statusString = "Exposing";
                        break;

                    case (long)LibFLI.CameraStatus.READING_CCD:
                        statusString = "Reading Sensor";
                        break;

                    case (long)LibFLI.CameraStatus.DATA_READY:
                        statusString = "Data Ready";
                        break;

                    case (long)LibFLI.CameraStatus.WAITING_FOR_TRIGGER:
                        statusString = "Awaiting Trigger";
                        break;

                    default:
                        statusString = "Unknown";
                        break;
                }

                return statusString;
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
        public bool CanSubSample => false;

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

        public string Description => string.Format($"{Name} ({Id}) SN: {Info.Serial} HWRev: {Info.HWrev} FWRev: {Info.FWrev}");

        public bool DewHeaterOn {
            get => false;
            set {
            }
        }

        private string driverInfo = string.Empty;

        public string DriverInfo {
            get {
                StringBuilder version = new StringBuilder(128);

                if (Connected && (string.IsNullOrEmpty(driverInfo))) {
                    if ((LibFLI.FLIGetLibVersion(version, 128)) == 0) {
                        driverInfo = version.ToString();
                    }
                }

                return driverInfo;
            }
        }

        public string DriverVersion => string.Empty;

        public double ElectronsPerADU => double.NaN;
        public bool EnableSubSample { get; set; }

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

        public short Gain {
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

        public short GainMax => 0;
        public short GainMin => 0;
        public ArrayList Gains => new ArrayList();
        public bool HasBattery => false;
        public bool HasDewHeater => false;
        public bool HasSetupDialog => Connected ? true : false;

        /*
         * All FLI cameras have shutters
         */
        public bool HasShutter => true;

        public string Id => Info.Id;

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

                        return -1;
                    }

                    Logger.Debug($"FLI: Current readout mode: {mode} ({ReadoutModes.Cast<string>().ToArray()[mode]})");

                    return (short)mode;
                } else {
                    return -1;
                }
            }
            set {
                byte[] rowData;
                uint timeLeft = 0;
                uint rv;

                if (Connected && (value != ReadoutMode)) {
                    string modeName = ReadoutModes.Cast<string>().ToArray()[value];

                    Logger.Debug($"FLI: ReadoutMode: Setting readout mode to {value} ({modeName})");

                    if ((rv = LibFLI.FLISetCameraMode(CameraH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetCameraMode() failed. Returned {rv}");
                    }

                    /*
                     * After changing the readout mode, we must do a 0-second dark frame exposure (the data from which we do not care about)
                     * This serves to flush the electronics
                     */
                    Logger.Debug($"FLI: ReadoutMode: Flushing sensor after changing readout mode to {value} ({modeName})");

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

                    rowData = new byte[GetFullRowSize(Info.ExposureWidth)];

                    Logger.Debug($"FLI: ReadoutMode: Reading out DARK frame following readout mode change");

                    for (int r = 0; r < CameraYSize; r++) {
                        if ((rv = LibFLI.FLIGrabRow(CameraH, rowData, GetFullRowSize(Info.ExposureWidth))) != LibFLI.FLI_SUCCESS) {
                            Logger.Error($"FLI: FLIGrabRow() failed at row {r}. Returned {rv}");
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
                Info.ReadoutModeNormal = value;
                RaisePropertyChanged();
            }
        }

        public short ReadoutModeForSnapImages {
            get => Info.ReadoutModeSnap;
            set {
                Info.ReadoutModeSnap = value;
                RaisePropertyChanged();
            }
        }

        public ICollection ReadoutModes {
            get => Info.ReadoutModes;
            set => Info.ReadoutModes = (List<string>)value;
        }

        public string SensorName => string.Empty;

        public SensorType SensorType => SensorType.Monochrome;

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

        public int USBLimitMax => 0;
        public int USBLimitMin => 0;
        public int USBLimitStep => 0;

        public void AbortExposure() {
            StopExposure();
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task.Run(() => {
                uint ul_x = 0, ul_y = 0, lr_x = 0, lr_y = 0;
                List<string> modeList = new List<string>();
                StringBuilder modeString = new StringBuilder(32);
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
                     * Get sensor pixel dimensions
                     */
                    if ((rv = LibFLI.FLIGetVisibleArea(CameraH, ref ul_x, ref ul_y, ref lr_x, ref lr_y)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGetVisibleArea() failed. Returned {rv}");
                        return success;
                    }

                    Logger.Debug($"FLI: Visible Area: ul_X={ul_x}, ul_Y={ul_y}, lr_X={lr_x}, lr_Y={lr_y}");

                    Info.ExposureOriginPixelX = ul_x;
                    Info.ExposureOriginPixelY = ul_y;
                    Info.ExposureEndPixelX = lr_x;
                    Info.ExposureEndPixelY = lr_y;
                    Info.ExposureWidth = Info.ExposureEndPixelY - Info.ExposureOriginPixelY;
                    Info.ExposureHeight = Info.ExposureEndPixelX - Info.ExposureOriginPixelX;
                    CameraXSize = (int)Info.ExposureHeight;
                    CameraYSize = (int)Info.ExposureWidth;

                    /*
                     * Query the camera for a list of readout modes
                     */
                    while (LibFLI.FLIGetCameraModeString(CameraH, modeIndex, modeString, 32) == LibFLI.FLI_SUCCESS) {
                        Logger.Debug($"FLI: Adding mode index {modeIndex} (\"{modeString.ToString()}\") to list of modes");
                        modeList.Add(modeString.ToString());

                        modeIndex++;
                    }
                    Logger.Debug($"FLI: FLIGetCameraModeString() stopped at mode_index={modeIndex}. No more modes available.");

                    ReadoutModes = new List<string>(modeList);

                    /*
                     * Set the configured RBI flood bin mode
                     */
                    if (FLIFloodBin == null) {
                        /* 2x2 is the default */
                        FLIFloodBin = BinningModes.Single(x => x.Name == "2x2");
                    } else {
                        FLIFloodBin = BinningModes.Single(x => x.Name == FLIFloodBin?.Name);
                    }

                    /*
                     * Attempt to turn on background flushing. Save the result for later attempts
                     * Background flushing is automatically disabled whenever an exposure is made or
                     * the shutter is opened, so we must turn it on again after those events.
                     *
                     * We set the camera to execute the desired number of pre-exposure flushes.
                     */
                    if (FLIEnableFloodFlush) {
                        BGFlushStart();

                        if (FLIFlushCount > 0) {
                            Logger.Debug($"FLI: Setting the camera to execute {FLIFlushCount} pre-exposure flushes");
                            if ((rv = LibFLI.FLISetNFlushes(CameraH, FLIFlushCount)) != LibFLI.FLI_SUCCESS) {
                                Logger.Error($"FLI: FLISetNFlushes() failed. Returned {rv}");
                                return success;
                            }
                        }
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

            CoolerOn = false;
            Connected = false;

            LibFLI.FLIClose(CameraH);
        }

        public Task<IImageData> DownloadExposure(CancellationToken ct) {
            return Task.Run(() => {
                int width;
                int height;
                int rowSize;
                int imgSize;
                byte[] rowData;
                byte[] imgData;
                IntPtr buff;
                uint rv;

                width = (int)Info.ExposureWidth;
                height = (int)Info.ExposureHeight;
                rowSize = GetFullRowSize(Info.ExposureWidth);
                imgSize = rowSize * height;

                rowData = new byte[rowSize];
                imgData = new byte[imgSize];

                Logger.Debug($"FLI: Fetching {height} rows from the camera");

                for (int r = 0; r < height; r++) {
                    if ((rv = LibFLI.FLIGrabRow(CameraH, rowData, rowSize)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGrabRow() failed at row {r + 1}. Returned {rv}");
                    }

                    Logger.Trace($"FLI: Normal exposure, fetched row {r + 1} of {height}");
                    Buffer.BlockCopy(rowData, 0, imgData, r * rowSize, rowSize);
                }

                BGFlushStart();

                Logger.Debug($"FLI: Marshalling {imgSize} bytes");

                buff = Marshal.AllocHGlobal(imgSize);
                Marshal.Copy(imgData, 0, buff, imgSize);

                var cameraDataToManaged = new CameraDataToManaged(buff, width, height, BitDepth);
                var arr = cameraDataToManaged.GetData();

                rowData = imgData = null;
                Marshal.FreeHGlobal(buff);

                return Task.FromResult<IImageData>(new ImageData.ImageData(arr, width, height, BitDepth, SensorType != SensorType.Monochrome));
            }, ct);
        }

        public Task<IImageData> DownloadLiveView(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public void SetBinning(short x, short y) {
            Logger.Debug($"FLI: Setting binning to {x}x{y}");

            BinX = x;
            BinY = y;
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
            WindowService.ShowDialog(this, Locale.Loc.Instance["LblFLICameraSetup"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public void StartExposure(CaptureSequence sequence) {
            bool isSnap;
            bool isDarkFrame;
            uint timeLeft = 0;
            uint ul_x, ul_y, lr_x, lr_y;
            uint rv;

            isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;

            /*
             * First do any pre-expsoure RBI mananagement
             */
            if (FLIEnableFloodFlush == true) {
                if ((isSnap && FLIEnableSnapshotFloodFlush) || !isSnap) {
                    if (!FloodControl()) {
                        Logger.Error("FLI: RBI flood failed. Aborting exposure.");
                        return;
                    }
                }
            }

            /*
             * Set the desired readout mode
             */
            ReadoutMode = isSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

            /*
             * Set the frame type for this exposure
             * Darks and Bias frames both get set to same type.
             */
            isDarkFrame = sequence.ImageType == CaptureSequence.ImageTypes.BIAS ||
                sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                sequence.ImageType == CaptureSequence.ImageTypes.DARKFLAT;

            FrameType = isDarkFrame ? LibFLI.FLIFrameType.DARK : LibFLI.FLIFrameType.NORMAL;

            /*
             * Convert exposure seconds to milliseconds and set the camera with it.
             */
            ExposureLength = (uint)sequence.ExposureTime * 1000;

            /*
             * Set bin levels
             */
            SetBinning(sequence.Binning.X, sequence.Binning.Y);

            /*
             * Set the sensor area we want to capture
             */
            ul_x = Info.ExposureOriginPixelX;
            ul_y = Info.ExposureOriginPixelY;
            lr_x = Info.ExposureEndPixelX = (uint)((CameraXSize + Info.ExposureOriginPixelX) / BinY);
            lr_y = Info.ExposureEndPixelY = (uint)((CameraYSize + Info.ExposureOriginPixelY) / BinX);
            Info.ExposureWidth = Info.ExposureEndPixelX - Info.ExposureOriginPixelX;
            Info.ExposureHeight = Info.ExposureEndPixelY - Info.ExposureOriginPixelY;

            if ((rv = LibFLI.FLISetImageArea(CameraH, ul_x, ul_y, lr_x, lr_y)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLISetImageArea() failed. Returned {rv}");
            }

            Logger.Debug($"FLI: Setting exposure area to: ul_X={ul_x}, ul_Y={ul_y}, lr_X={lr_x}, lr_Y={lr_y} ({Info.ExposureWidth}x{Info.ExposureHeight})");

            /*
             * Initiate the exposure. This blocks until finished.
             */
            Logger.Debug($"FLI: Triggering exposure");

            if ((rv = LibFLI.FLIExposeFrame(CameraH)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIExposeFrame() failed. Returned {rv}");
                return;
            }

            if ((rv = LibFLI.FLIGetExposureStatus(CameraH, ref timeLeft)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetExposureStatus() failed. Returned {rv}");
            }

            Logger.Debug($"FLI: StartExposure() pausing for {timeLeft}ms for exposure completion.");
            Thread.Sleep((int)timeLeft);
        }

        public void StopExposure() {
            uint rv;

            Logger.Debug($"FLI: Cancelling exposure");

            if (Connected) {
                if ((rv = LibFLI.FLIEndExposure(CameraH)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI: FLIEndExposure() failed. Returned {rv}");
                }
            }
        }

        public void StartLiveView() {
        }

        public void StopLiveView() {
        }

        private bool FloodControl() {
            uint timeLeft = 0;
            byte[] rowData;
            uint rows;
            uint ul_x, ul_y, lr_x, lr_y;
            uint rv;

            /*
             * Execute the configured number of flush operations
             */
            Logger.Debug($"FLI: RBI: Flooding for {FLIFloodDuration * 1e3}ms and flushing {FLIFlushCount} times at {FLIFloodBin.Name} binning");

            FrameType = LibFLI.FLIFrameType.RBI_FLUSH;
            ExposureLength = (uint)FLIFloodDuration * 1000;
            ReadoutMode = ReadoutModeForSnapImages;
            SetBinning(FLIFloodBin.X, FLIFloodBin.Y);

            ul_x = Info.ExposureOriginPixelX;
            ul_y = Info.ExposureOriginPixelY;
            lr_x = Info.ExposureEndPixelX = (uint)((CameraXSize + Info.ExposureOriginPixelX) / BinY);
            lr_y = Info.ExposureEndPixelY = (uint)((CameraYSize + Info.ExposureOriginPixelY) / BinX);
            Info.ExposureWidth = Info.ExposureEndPixelX - Info.ExposureOriginPixelX;
            Info.ExposureHeight = Info.ExposureEndPixelY - Info.ExposureOriginPixelY;

            rowData = new byte[GetFullRowSize(Info.ExposureWidth)];

            if ((rv = LibFLI.FLISetImageArea(CameraH, ul_x, ul_y, lr_x, lr_y)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLISetImageArea() failed. Returned {rv}");
            }

            Logger.Debug($"FLI: RBI: Setting exposure area to: ul_X={ul_x}, ul_Y={ul_y}, lr_X={lr_x}, lr_Y={lr_y} ({Info.ExposureWidth}x{Info.ExposureHeight})");

            if ((rv = LibFLI.FLIExposeFrame(CameraH)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIExposeFrame() failed. Returned {rv}");
                return false;
            }

            if ((rv = LibFLI.FLIGetExposureStatus(CameraH, ref timeLeft)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetExposureStatus() failed. Returned {rv}");
            }

            Logger.Debug($"FLI: RBI Flood exposure pausing for {timeLeft}ms for completion.");
            Thread.Sleep((int)timeLeft);

            rows = Info.ExposureHeight;
            Logger.Debug($"FLI: RBI: Downloading RBI flood ({rows} rows)");

            for (int r = 0; r < rows; r++) {
                if ((rv = LibFLI.FLIGrabRow(CameraH, rowData, GetFullRowSize(Info.ExposureWidth))) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI: FLIGrabRow() failed at row {r + 1}. Returned {rv}");
                }

                Logger.Trace($"FLI: RBI: Flood exposure fetched row {r + 1} of {rows}");
            }

            BGFlushStart();

            Logger.Debug("FLI: RBI operation completed.");
            return true;
        }

        private void BGFlushStart() {
            uint rv;

            if (Info.FWrev >= 0x0110) {
                Logger.Debug("FLI: Starting background flushing...");

                if ((rv = LibFLI.FLIControlBackgroundFlush(CameraH, LibFLI.FLIBGFlush.START)) != LibFLI.FLI_SUCCESS) {
                    Logger.Info($"FLI: FLIControlBackgroundFlush() failed. Returned {rv}");
                } else {
                    Logger.Debug("FLI: Background flushing started!");
                }
            } else {
                Logger.Debug("FLI: Background flushing is not supported. Camera firmware is too old.");
            }
        }

        private void BGFlushStop() {
            uint rv;

            if (Info.FWrev >= 0x0110) {
                Logger.Debug("FLI: Stopping background flushing...");

                if ((rv = LibFLI.FLIControlBackgroundFlush(CameraH, LibFLI.FLIBGFlush.STOP)) != LibFLI.FLI_SUCCESS) {
                    Logger.Info($"FLI: FLIControlBackgroundFlush() failed.  Returned {rv}");
                } else {
                    Logger.Debug("FLI: Background flushing stopped!");
                }
            }
        }

        private int GetFullRowSize(uint rowPixels) {
            return (int)(rowPixels * BitDepth) / 8;
        }

        public bool FLIEnableFloodFlush {
            get => profileService.ActiveProfile.CameraSettings.FLIEnableFloodFlush;
            set {
                uint rv;

                if (FLIEnableFloodFlush == value) {
                    return;
                }

                if (value == true) {
                    BGFlushStart();

                    if (FLIFlushCount > 0) {
                        Logger.Debug($"FLI: Setting the camera to execute {FLIFlushCount} pre-exposure flushes");
                        if ((rv = LibFLI.FLISetNFlushes(CameraH, FLIFlushCount)) != LibFLI.FLI_SUCCESS) {
                            Logger.Error($"FLI: FLISetNFlushes() failed. Returned {rv}");
                        }
                    }
                } else {
                    Logger.Debug("FLI: Attempting to stop background flushing...");
                    BGFlushStop();

                    Logger.Debug("FLI: Setting the camera to execute 0 pre-exposure flushes");
                    if ((rv = LibFLI.FLISetNFlushes(CameraH, 0)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetNFlushes() failed. Returned {rv}");
                    }
                }

                profileService.ActiveProfile.CameraSettings.FLIEnableFloodFlush = value;
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

        public uint FLIFlushCount {
            get => profileService.ActiveProfile.CameraSettings.FLIFlushCount;
            set {
                uint rv;

                profileService.ActiveProfile.CameraSettings.FLIFlushCount = value;

                if ((rv = LibFLI.FLISetNFlushes(CameraH, FLIFlushCount)) != LibFLI.FLI_SUCCESS) {
                    Logger.Error($"FLI: FLISetNFlushes() failed. Returned {rv}");
                }

                RaisePropertyChanged();
            }
        }

        public BinningMode FLIFloodBin {
            get => profileService.ActiveProfile.CameraSettings.FLIFloodBin;
            set {
                profileService.ActiveProfile.CameraSettings.FLIFloodBin = value;
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
    }
}