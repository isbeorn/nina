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
using QHYCCD;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Model.ImageData;

namespace NINA.Model.MyCamera {

    public class QHYCamera : BaseINPC, ICamera {
        private static IntPtr CameraP;
        private AsyncObservableCollection<BinningMode> _binningModes;
        private bool _connected = false;
        private short _readoutModeForNormalImages;
        private short _readoutModeForSnapImages;
        private Task coolerTask;
        private CancellationTokenSource coolerWorkerCts;
        private LibQHYCCD.QHYCCD_CAMERA_INFO Info;
        private IProfileService profileService;

        public QHYCamera(uint cameraIdx, IProfileService profileService) {
            this.profileService = profileService;
            StringBuilder cameraId = new StringBuilder(LibQHYCCD.QHYCCD_ID_LEN);
            StringBuilder cameraModel = new StringBuilder(0);

            /*
             * Camera model long form, eg: "QHY183C-c915484fa76ea7552"
             * The QHY SDK uses this to internally identify connected cameras
             * and this we need this to create a handle for our camera.
             */
            LibQHYCCD.N_GetQHYCCDId(cameraIdx, cameraId);

            /*
             * Camera model short form, eg: "QHY183C"
             * We use this to put in the camera equipment selection menu
             * rather than the long form above.
             */
            LibQHYCCD.N_GetQHYCCDModel(cameraId, cameraModel);

            Name = cameraModel.ToString();
            Info.Index = cameraIdx;
            Info.Id = cameraId;

            Logger.Debug(string.Format("QHYCCD: Found camera {0}", Info.Id));
        }

        public string Category { get; } = "QHYCCD";

        private List<int> SupportedBinFactors {
            get {
                if (Info.SupportedBins == null) {
                    Info.SupportedBins = new List<int>();

                    if (IsQHYControl(LibQHYCCD.CONTROL_ID.CAM_BIN1X1MODE))
                        Info.SupportedBins.Add(1);

                    if (IsQHYControl(LibQHYCCD.CONTROL_ID.CAM_BIN2X2MODE))
                        Info.SupportedBins.Add(2);

                    if (IsQHYControl(LibQHYCCD.CONTROL_ID.CAM_BIN3X3MODE))
                        Info.SupportedBins.Add(3);

                    if (IsQHYControl(LibQHYCCD.CONTROL_ID.CAM_BIN4X4MODE))
                        Info.SupportedBins.Add(4);
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
            set {
                if (LibQHYCCD.SetQHYCCDBinMode(CameraP, (uint)value, (uint)value) == LibQHYCCD.QHYCCD_SUCCESS) {
                    Info.CurBin = value;
                } else {
                    Logger.Warning(string.Format("QHYCCD: Failed to set BIN mode {0}x{1}", value, value));
                }
            }
        }

        /// <summary>
        // Setting the Y pixel of a bin mode is redundant as QHY cameras
        // (currently) offer symmetrical bin modes only. Thus operations for
        // setting the Y pixes are no-ops, and getting Y pixel bin modes defer
        // to the getters for X pixels.
        /// </summary>
        public short BinY {
            get => BinX;
            set {
            }
        }

        public int BitDepth {
            get {
                return IsQHYControl(LibQHYCCD.CONTROL_ID.CAM_16BITS) ? 16 : 8;
            }
        }

        public string CameraState {
            get => Info.CamState;
            set => Info.CamState = value;
        }

        public int CameraXSize => unchecked((int)Info.ImageX);
        public int CameraYSize => unchecked((int)Info.ImageY);

        public bool CanGetGain {
            get {
                if (Connected) {
                    if (IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_GAIN))
                        return true;
                }

                return false;
            }
        }

        public bool CanSetGain {
            get {
                if (Connected) {
                    if (IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_GAIN))
                        return true;
                }

                return false;
            }
        }

        public bool CanSetOffset {
            get {
                if (Connected)
                    return Info.HasOffset;

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
            get => Info.CoolerOn;
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
                double rv = Double.NaN;

                if (Connected && CanSetTemperature) {
                    if ((rv = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_CURPWM)) != LibQHYCCD.QHYCCD_ERROR) {
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

        public string DriverInfo => "QHYCCD SDK";
        public string DriverVersion => LibQHYCCD.GetSDKFormattedVersion();
        public bool EnableSubSample { get; set; }
        public double ExposureMax => Info.ExpMax / 1e6;
        public double ElectronsPerADU => double.NaN;

        /// <summary>
        // We store the camera's exposure times in microseconds
        // but we present this number as seconds to those who ask
        /// </summary>
        public double ExposureMin => Info.ExpMin / 1e6;

        public short Gain {
            get {
                if (Connected && CanGetGain) {
                    double rv;

                    if (Info.HasUnreliableGain) {
                        rv = Info.CurGain;
                    } else {
                        rv = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN);
                    }

                    return unchecked((short)rv);
                }

                return 1;
            }
            set {
                if (Connected && CanSetGain) {
                    if (SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN, value)) {
                        Info.CurGain = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public short GainMax => Info.GainMax;
        public short GainMin => Info.GainMin;
        public ArrayList Gains => new ArrayList();
        public bool HasBattery => false;
        public bool HasDewHeater => false;
        public bool HasSetupDialog => false;

        public bool HasShutter {
            get {
                if (Connected)
                    return Info.HasShutter;

                return false;
            }
        }

        public string Id => $"{Description}";

        public bool LiveViewEnabled {
            get => false;
            set {
            }
        }

        public short MaxBinX => (short)Info.SupportedBins.Max();
        public short MaxBinY => MaxBinX;

        public string Name {
            get => Info.Model.ToString();
            set {
                Info.Model = new StringBuilder(value);
                RaiseAllPropertiesChanged();
            }
        }

        public int Offset {
            get {
                if (Connected) {
                    double rv;

                    if ((rv = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET)) != LibQHYCCD.QHYCCD_ERROR) {
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
                    if (SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET, value))
                        RaisePropertyChanged();
                }
            }
        }

        public int OffsetMax => Info.OffMax;
        public int OffsetMin => Info.OffMin;
        public double PixelSizeX => Info.PixelX;
        public double PixelSizeY => Info.PixelY;

        public short ReadoutModeForNormalImages {
            get => _readoutModeForNormalImages;
            set {
                _readoutModeForNormalImages = value;
                RaisePropertyChanged();
            }
        }

        public short ReadoutModeForSnapImages {
            get => _readoutModeForSnapImages;
            set {
                _readoutModeForSnapImages = value;
                RaisePropertyChanged();
            }
        }

        public ICollection ReadoutModes => new List<string> { "Default" };
        public string SensorName => String.Empty;

        public SensorType SensorType {
            get {
                SensorType stype = SensorType.Monochrome;

                if (Connected) {
                    if (Info.IsColorCam == true)
                        stype = SensorType.RGGB;
                }
                return stype;
            }
        }

        public int SubSampleHeight { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }

        public double Temperature {
            get {
                double rv = Double.NaN;

                if (Connected && Info.HasChipTemp) {
                    if ((rv = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_CURTEMP)) != LibQHYCCD.QHYCCD_ERROR)
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
                    Logger.Debug(string.Format("QHYCCD: Cooler target temperature set to {0}", value));
                    Info.CoolerTargetTemp = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int USBLimit {
            get {
                double rv;

                if (Connected && ((rv = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC)) != LibQHYCCD.QHYCCD_ERROR))
                    return unchecked((int)rv);

                return -1;
            }
            set {
                if (Connected && CanSetUSBLimit) {
                    if (SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC, value))
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
                while (true) {
                    if (Info.CoolerOn) {
                        Logger.Debug(string.Format("QHYCCD: CoolerWorker setting camera target temp to {0}", Info.CoolerTargetTemp));
                        LibQHYCCD.ControlQHYCCDTemp(CameraP, Info.CoolerTargetTemp);
                    } else if (previous == true) {
                        Logger.Debug("QHYCCD: CoolerWorker turning off TEC due user request");
                        SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_MANULPWM, 0);
                    }

                    previous = Info.CoolerOn;

                    /* sleep 2 seconds (cancelable) */
                    await Task.Delay(LibQHYCCD.QHYCCD_COOLER_DELAY, ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("QHYCCD: CoolerWorker task cancelled");
            }
        }

        private double GetControlValue(LibQHYCCD.CONTROL_ID type) {
            double rv;

            if ((rv = LibQHYCCD.GetQHYCCDParam(CameraP, type)) != LibQHYCCD.QHYCCD_ERROR) {
                Logger.Debug(string.Format("QHYCCD: Got Control {0} = {1}", type, rv));
                return rv;
            } else {
                Logger.Error(string.Format("QHYCCD: Failed to Get value for control {0}", type));
                return LibQHYCCD.QHYCCD_ERROR;
            }
        }

        private bool IsColorCam() {
            Info.BayerPattern = (LibQHYCCD.BAYER_ID)LibQHYCCD.IsQHYCCDControlAvailable(CameraP, LibQHYCCD.CONTROL_ID.CAM_COLOR);

            switch (Info.BayerPattern) {
                case LibQHYCCD.BAYER_ID.BAYER_GB:
                case LibQHYCCD.BAYER_ID.BAYER_GR:
                case LibQHYCCD.BAYER_ID.BAYER_BG:
                case LibQHYCCD.BAYER_ID.BAYER_RG:
                    return true;

                default:
                    return false;
            }
        }

        private bool IsQHYControl(LibQHYCCD.CONTROL_ID type) {
            if (LibQHYCCD.IsQHYCCDControlAvailable(CameraP, type) == LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Debug(string.Format("QHYCCD: Control {0} exists", type));
                return true;
            } else {
                Logger.Debug(string.Format("QHYCCD: Control Value {0} is not available", type));
                return false;
            }
        }

        private bool SetControlValue(LibQHYCCD.CONTROL_ID type, double value) {
            if (LibQHYCCD.SetQHYCCDParam(CameraP, type, value) == LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Debug(string.Format("QHYCCD: Setting Control {0} to {1}", type, value));
                return true;
            } else {
                Logger.Warning(string.Format("QHYCCD: Failed to Set Control {0} with value {1}", type, value));
                return false;
            }
        }

        public void AbortExposure() {
            StopExposure();
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task<bool>.Run(() => {
                var success = false;
                double min = 0, max = 0, step = 0;
                StringBuilder cameraID = new StringBuilder(LibQHYCCD.QHYCCD_ID_LEN);

                try {
                    Logger.Info(string.Format("QHYCCD: Connecting to {0}", Info.Id));

                    /*
                     * Get our selected camera's ID from the SDK
                     */
                    LibQHYCCD.N_GetQHYCCDId(Info.Index, cameraID);

                    /*
                     * CameraP is the handle we use to reference this camera
                     * from now on.
                     */
                    CameraP = LibQHYCCD.N_OpenQHYCCD(cameraID);

                    /*
                     * We initalize to single exposure mode.
                     */
                    LibQHYCCD.SetQHYCCDStreamMode(CameraP, (byte)LibQHYCCD.QHYCCD_CAMERA_MODE.SINGLE_EXPOSURE);

                    /*
                     * Initialize the camera and make it available for use
                     */
                    LibQHYCCD.N_InitQHYCCD(CameraP);

                    LibQHYCCD.GetQHYCCDChipInfo(CameraP,
                        ref Info.ChipX, ref Info.ChipY,
                        ref Info.ImageX, ref Info.ImageY,
                        ref Info.PixelX, ref Info.PixelY,
                        ref Info.Bpp);

                    /*
                     * Is this a color sensor or not?
                     * If so, do not debayer the image data
                     */
                    if (IsColorCam() == true) {
                        Logger.Info(string.Format("QHYCCD: Color camera detected (pattern = {0}). Setting debayering to off", Info.BayerPattern.ToString()));
                        LibQHYCCD.SetQHYCCDDebayerOnOff(CameraP, false);
                        Info.IsColorCam = true;
                    } else {
                        Info.IsColorCam = false;
                    }

                    /*
                     * Get our min and max shutter speed (exposure times)
                     * The QHY SDK reports this value in microseconds (us)
                     */
                    LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                        LibQHYCCD.CONTROL_ID.CONTROL_EXPOSURE, ref min, ref max, ref step);

                    Info.ExpMin = min;
                    Info.ExpMax = max;
                    Info.ExpStep = step;
                    Logger.Debug(string.Format("QHYCCD: ExpMin={0}, ExpMax={1}, ExpStep={2}",
                        Info.ExpMin, Info.ExpMax, Info.ExpStep));

                    /*
                     * Get our min and max gain
                     */
                    LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                        LibQHYCCD.CONTROL_ID.CONTROL_GAIN, ref min, ref max, ref step);

                    Info.GainMin = (short)min;
                    Info.GainMax = (short)max;
                    Info.GainStep = step;
                    Logger.Debug(string.Format("QHYCCD: GainMin={0}, GainMax={1}, GainStep={2}",
                        Info.GainMin, Info.GainMax, Info.GainStep));

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
                    LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                        LibQHYCCD.CONTROL_ID.CONTROL_OFFSET,
                        ref min, ref max, ref step);

                    Info.OffMin = (int)min;
                    Info.OffMax = (int)max;
                    Info.OffStep = step;
                    Logger.Debug(string.Format("QHYCCD: OffMin={0}, OffMax={1}, OffStep={2}",
                        Info.OffMin, Info.OffMax, Info.OffStep));

                    QuirkInflatedOffset();

                    /*
                     * Fetch our min and max PWM settings for
                     * the cooler.
                     */
                    Info.HasCooler = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_COOLER);
                    Info.HasChipTemp = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_CURTEMP);

                    if (Info.HasCooler == true) {
                        LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                            LibQHYCCD.CONTROL_ID.CONTROL_MANULPWM, ref min, ref max, ref step);

                        Info.CoolerPwmMin = min;
                        Info.CoolerPwmMax = max;
                        Info.CoolerPwmStep = step;
                        Logger.Debug(string.Format("QHYCCD: CoolerPwmMin={0}, CoolerPwmMax={1}, CoolerPwmStep={2}",
                            Info.CoolerPwmMin, Info.CoolerPwmMax, Info.CoolerPwmStep));

                        /*
                         * Initialize cooler's target temperature to 0C
                         */
                        Info.CoolerTargetTemp = 0;

                        /*
                         * Force any TEC cooler to off upon startup
                         */
                        CoolerOn = false;

                        /*
                         * Start the thread that operates the TEC
                         * This thread will operate the TEC in accordance with the user turning the cooler on or off
                         * and program the camera's TEC to cool to the desired temperature. This thread will operate
                         * for as long as the camera is connected.
                         */
                        Logger.Debug("QHYCCD: Starting CoolerWorker task");
                        coolerWorkerCts?.Dispose();
                        coolerWorkerCts = new CancellationTokenSource();
                        coolerTask = CoolerWorker(coolerWorkerCts.Token);
                    }

                    /*
                     * Fetch our min and max USB bandwidth settings
                     */
                    Info.HasUSBTraffic = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC);

                    if (Info.HasUSBTraffic == true) {
                        LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                            LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC, ref min, ref max, ref step);

                        Info.USBMin = min;
                        Info.USBMax = max;
                        Info.USBStep = step;
                        Logger.Debug(string.Format("QHYCCD: USBMin={0}, USBMax={1}, USBStep={2}",
                            Info.USBMin, Info.USBMax, Info.USBStep));

                        if (QuirkNoUSBTraffic())
                            Info.HasUSBTraffic = false;
                    }

                    /*
                     * Amplifier noise control. Undocumented but we will set
                     * it to 0, which means automatic.
                     * 0 = auto
                     * 1 = on
                     * 2 = off
                     */
                    if (IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_AMPV) == true) {
                        LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                            LibQHYCCD.CONTROL_ID.CONTROL_AMPV, ref min, ref max, ref step);

                        Logger.Debug(string.Format("QHYCCD: AMPVmin={0}, AMPVmax={1}, AMPVstep={2}",
                            min, max, step));

                        Logger.Debug("QHYCCD: Setting amplifier control to 0 (automatic)");
                        SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_AMPV, 0);
                    }

                    /*
                     * QHY SDK offers no way to get the current bin mode! So we track
                     * it manually using QHYCCD_CAMERA_INFO.CurBin. We initialize the
                     * camera with 1x1 binning.
                     */
                    Info.CurBin = 1;
                    SetBinning(Info.CurBin, Info.CurBin);

                    Info.HasShutter = IsQHYControl(LibQHYCCD.CONTROL_ID.CAM_MECHANICALSHUTTER);
                    Info.HasGain = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_GAIN);
                    Info.HasOffset = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET);

                    /*
                     * Announce that this camera is now initialized and ready
                     */
                    CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.IDLE.ToString();
                    Connected = true;
                    success = true;

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

            /*
             * Terminate the cooler task.
             */
            if (Info.HasCooler) {
                Logger.Debug("QHYCCD: Terminating CoolerWorker task");
                CoolerOn = false;
                coolerWorkerCts.Cancel();
                coolerWorkerCts.Dispose();

                /* CoolerWorker task was killed. Make sure the TEC is turned off before closing the camera. */
                Logger.Debug("QHYCCD: CoolerWorker task cancelled, turning off TEC");
                SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_MANULPWM, 0);
            }

            Connected = false;

            Logger.Info(string.Format("QHYCCD: Closing camera {0}", Info.Id));
            LibQHYCCD.N_CloseQHYCCD(CameraP);
        }

        public Task<IImageData> DownloadExposure(CancellationToken ct) {
            return Task.Run(() => {
                uint width = 0;
                uint height = 0;
                uint bpp = 0;
                uint channels = 0;
                byte[] ImgData;
                uint rv;

                /*
                 * Ask the SDK how big the exposure will be
                 */
                uint size = LibQHYCCD.GetQHYCCDMemLength(CameraP);

                if (size == 0) {
                    Logger.Warning("QHYCCD: SDK reported a 0-length image buffer!");
                    throw new Exception(Locale.Loc.Instance["LblASIImageDownloadError"]);
                }
                Logger.Debug(string.Format("QHYCCD: Image size will be {0} bytes", size));

                /*
                 * Size the image data byte array for the image
                 */
                ImgData = new byte[size];

                /*
                 * Download the image from the camera
                 */
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.DOWNLOADING.ToString();
                if ((rv = LibQHYCCD.C_GetQHYCCDSingleFrame(CameraP, ref width, ref height, ref bpp, ref channels, ImgData)) != LibQHYCCD.QHYCCD_SUCCESS) {
                    Logger.Warning(string.Format("QHYCCD: Failed to download image from camera! rv = {0}", rv));
                    throw new Exception(Locale.Loc.Instance["LblASIImageDownloadError"]);
                }

                /*
                 * Copy the image byte array to an allocated buffer
                 */
                IntPtr buf = Marshal.AllocHGlobal(ImgData.Length);
                Marshal.Copy(ImgData, 0, buf, ImgData.Length);
                var cameraDataToManaged = new CameraDataToManaged(buf, (int)width, (int)height, (int)bpp);
                var arr = cameraDataToManaged.GetData();
                ImgData = null;
                Marshal.FreeHGlobal(buf);

                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.IDLE.ToString();

                return Task.FromResult<IImageData>(new ImageData.ImageData(arr, (int)width, (int)height, (int)bpp, SensorType != SensorType.Monochrome));
            }, ct);
        }

        public Task<IImageData> DownloadLiveView(CancellationToken ct) {
            throw new NotImplementedException();
        }

        public void SetBinning(short x, short y) {
            BinX = x;
        }

        public void SetupDialog() {
        }

        public void StartExposure(CaptureSequence sequence) {
            uint rv;
            /*
             * Setup camera with the desired exposure setttings
             */

            /* ROI coordinates and resolution */
            if (EnableSubSample) {
                rv = LibQHYCCD.SetQHYCCDResolution(CameraP, (uint)SubSampleX / (uint)BinX, (uint)SubSampleY / (uint)BinY, (uint)SubSampleWidth / (uint)BinX, (uint)SubSampleHeight / (uint)BinY);
            } else {
                rv = LibQHYCCD.SetQHYCCDResolution(CameraP, 0, 0, Info.ImageX / (uint)BinX, Info.ImageY / (uint)BinY);
            }

            if (rv != LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to set exposure resolution");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            /* Exposure bit depth */
            if (LibQHYCCD.SetQHYCCDBitsMode(CameraP, (uint)BitDepth) != LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to set exposure bit depth");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            /* Exposure length (in microseconds) */
            if (!SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_EXPOSURE, sequence.ExposureTime * 1e6)) {
                Logger.Warning("QHYCCD: Failed to set exposure time");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            /*
             * Initiate the exposure
             */
            if (LibQHYCCD.ExpQHYCCDSingleFrame(CameraP) == LibQHYCCD.QHYCCD_ERROR) {
                Logger.Warning("QHYCCD: Failed to initiate the exposure!");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.EXPOSING.ToString();
        }

        public void StartLiveView() {
        }

        public void StopExposure() {
            if (LibQHYCCD.CancelQHYCCDExposingAndReadout(CameraP) != LibQHYCCD.QHYCCD_ERROR)
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.IDLE.ToString();
        }

        public void StopLiveView() {
        }

        #region "Quirks"

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
            double saveGain = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN);

            /*
             * Set the gain to the maximum gain value minus one step for the camera. We will then query the camera and
             * test whether we get the value we set or not (which indicates the bug).
             */
            double wantGain = Info.GainMax - Info.GainStep;
            SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN, wantGain);

            double curGain = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN);

            /* Restore our original gain setting */
            SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN, saveGain);

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
            double saveOffset = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET);

            double wantOffset = 1;
            double gotOffset;

            SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET, wantOffset);
            gotOffset = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET) - wantOffset;

            if (gotOffset != 0) {
                Logger.Debug(string.Format("QHYCCD_QUIRK: This camera inflates its Offset by {0}", gotOffset));
                Info.InflatedOff = (int)gotOffset;
            } else {
                Info.InflatedOff = 0;
            }

            /* Restore our original gain setting */
            SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET, saveOffset);
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
            double saveUSB = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC);

            if (saveUSB != Info.USBMax) {
                wantUSB = Info.USBMax;
            } else {
                wantUSB = Info.USBMax - Info.USBStep;
            }
            SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC, wantUSB);
            double gotUSB = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC);

            /* Check to see if it really changed */
            if (gotUSB == saveUSB) {
                Logger.Debug("QHYCCD_QUIRK: This camera does not really allow CONTROL_USBTRAFFIC settings");
                return true;
            }

            /* Restore the original USB traffic setting */
            SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC, saveUSB);

            return false;
        }

        #endregion "Quirks"
    }
}