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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Model.ImageData;

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

            if ((rv = LibFLI.FLIOpen(out CameraH, Info.Id, LibFLI.FLIDomains.DEV_CAMERA | LibFLI.FLIDomains.IF_USB)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIOpen() failed. Returned {rv}");
            }

            if ((rv = LibFLI.FLIGetSerialString(CameraH, cameraSerial, 64)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetSerialString() failed. Returned {rv}");
            }

            Info.Serial = cameraSerial.ToString();
            Info.FWrev = 0x0;
            Info.HWrev = 0x0;

            if ((rv = LibFLI.FLIGetFWRevision(CameraH, out Info.FWrev)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetFWRevision() failed. Returned {rv}");
            }

            if ((rv = LibFLI.FLIGetHWRevision(CameraH, out Info.HWrev)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLIGetHWRevision() failed. Returned {rv}");
            }

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
                RaisePropertyChanged();
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

        public long ExposureLength {
            get => Info.ExposureLength;
            set {
                uint rv;

                if (Connected) {
                    Logger.Debug($"FLI: Setting exposure time to {value}ms");

                    if ((rv = LibFLI.FLISetExposureTime(CameraH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetExposureTime() failed. Returned {rv}");
                    }

                    Info.ExposureLength = (uint)value;
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

        public long FrameType {
            get => Info.FrameType;
            set {
                uint rv;

                if (Connected) {
                    Logger.Debug($"FLI: Setting frame type to {value}");

                    if ((rv = LibFLI.FLISetFrameType(CameraH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetFrameType() failed. Returned {rv}");
                    }

                    Info.FrameType = (uint)value;
                }
            }
        }

        public short GainMax => 0;
        public short GainMin => 0;
        public ArrayList Gains => new ArrayList();
        public bool HasBattery => false;
        public bool HasDewHeater => false;
        public bool HasSetupDialog => false;

        /*
         * All FLI cammera have shutters
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
                    Info.PixelX = pixelSizeX * 1e6;
                }
                return Info.PixelX;
            }
        }

        private double pixelSizeY = 0;

        public double PixelSizeY {
            get {
                double junk = 0;

                if (Connected && (pixelSizeY == 0)) {
                    LibFLI.FLIGetPixelSize(CameraH, ref junk, ref pixelSizeY);
                    Info.PixelY = pixelSizeY * 1e6;
                }
                return Info.PixelY;
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

                    Logger.Debug($"FLI: Readout Mode={mode}");

                    return (short)mode;
                } else {
                    return -1;
                }
            }
            set {
                uint rv;

                if (Connected && (value != ReadoutMode)) {
                    Logger.Debug($"FLI: Setting readout mode to {value}");

                    if ((rv = LibFLI.FLISetCameraMode(CameraH, (uint)value)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLISetCameraMode() failed. Returned {rv}");
                    }

                    /*
                     * After changing the readout mode, we must do a 0-second bias frame exposure (the data from which we do not care about)
                     * This serves to flush the electronics
                     */
                    Logger.Debug($"FLI: Flushing sensor after changing readout mode to {value}");

                    FrameType = (long)LibFLI.FLIFrameType.DARK;
                    ExposureLength = 0;

                    Logger.Debug($"FLI: Exposing DARK frame following readout mode change.");
                    if ((rv = LibFLI.FLIExposeFrame(CameraH)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIExposeFrame() failed. Returned {rv}");
                    }

                    Logger.Debug($"FLI: Pausing for {LibFLI.FLI_MODECHANGE_PAUSE} seconds following readout mode change.");
                    Thread.Sleep(LibFLI.FLI_MODECHANGE_PAUSE * 1000);

                    Logger.Debug("FLI: Flush completed.");
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

                    SubSampleWidth = CameraXSize = (int)(lr_x - ul_x);
                    SubSampleHeight = CameraYSize = (int)(lr_y - ul_y);
                    SubSampleX = (int)ul_x;
                    SubSampleY = (int)ul_y;

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
                     * We can now say that we are connected!
                     */
                    Connected = true;
                    success = true;

                    /*
                     * Set some defaults
                     */
                    SetBinning(1, 1);
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

            CoolerOn = false;

            LibFLI.FLIClose(CameraH);
            Connected = false;
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

                width = SubSampleWidth / BinX;
                height = SubSampleHeight / BinY;
                rowSize = width * BitDepth / 8;
                imgSize = rowSize * height;
                rowData = new byte[rowSize];
                imgData = new byte[imgSize];

                Logger.Debug($"FLI: Fetching {height} rows from the camera");

                for (int i = 0; i < height; i++) {
                    if ((rv = LibFLI.FLIGrabRow(CameraH, rowData, rowSize)) != LibFLI.FLI_SUCCESS) {
                        Logger.Error($"FLI: FLIGrabRow() failed at row {i}. Returned {rv}");
                    }

                    Buffer.BlockCopy(rowData, 0, imgData, i * rowSize, rowSize);
                }

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
            BinX = x;
            BinY = y;
        }

        public void SetupDialog() {
        }

        public async void StartExposure(CaptureSequence sequence) {
            bool isSnap;
            bool isDarkFrame;
            uint timeLeft = 0;
            uint x, y, w, h;
            uint rv;

            /*
             * Set the desired readout mode
             */
            isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;
            ReadoutMode = isSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

            /*
             * Set the frame type for this exposure
             * Darks and Bias frames both get set to same type.
             */
            isDarkFrame = sequence.ImageType == CaptureSequence.ImageTypes.BIAS ||
                sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                sequence.ImageType == CaptureSequence.ImageTypes.DARKFLAT;

            FrameType = isDarkFrame ? (long)LibFLI.FLIFrameType.DARK : (long)LibFLI.FLIFrameType.NORMAL;

            /*
             * Convert exposure seconds to milliseconds and set the camera with it.
             */
            ExposureLength = (long)sequence.ExposureTime * 1000;

            /*
             * Set the sensor area we want to capture if subsampling is activated.
             */
            x = (uint)SubSampleX;
            y = (uint)SubSampleY;
            w = (uint)(SubSampleWidth + SubSampleX) / (uint)BinX;
            h = (uint)(SubSampleHeight + SubSampleY) / (uint)BinY;

            if ((rv = LibFLI.FLISetImageArea(CameraH, x, y, w, h)) != LibFLI.FLI_SUCCESS) {
                Logger.Error($"FLI: FLISetImageArea() failed. Returned {rv}");
            }

            Logger.Debug($"FLI: Set exposure area to: ul_X={x}, ul_Y={y}, lr_X={w}, lr_Y={h}");

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

            await Task.Delay(TimeSpan.FromMilliseconds(timeLeft));
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
    }
}