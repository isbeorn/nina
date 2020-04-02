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
        private bool _liveViewEnabled = false;
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

            Logger.Debug($"QHYCCD: Found camera {Info.Id}");
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
                    Logger.Warning($"QHYCCD: Failed to set BIN mode {value}x{value}");
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
            get => (int)Info.Bpp;
        }

        public string CameraState {
            get => Info.CamState;
            set => Info.CamState = value;
        }

        public int CameraXSize {
            get => unchecked((int)Info.CurImage.SizeX);
            private set {
                Logger.Debug($"QHYCCD: Setting CameraXSize to {value}");
                Info.CurImage.SizeX = unchecked((uint)value);
                RaisePropertyChanged();
            }
        }

        public int CameraYSize {
            get => unchecked((int)Info.CurImage.SizeY);
            private set {
                Logger.Debug($"QHYCCD: Setting CameraYSize to {value}");
                Info.CurImage.SizeY = unchecked((uint)value);
                RaisePropertyChanged();
            }
        }

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

        public bool CanShowLiveView => true;
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
                double rv = double.NaN;

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

        public int Gain {
            get {
                if (Connected && CanGetGain) {
                    double rv;

                    if (Info.HasUnreliableGain) {
                        rv = Info.CurGain;
                    } else {
                        rv = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN);
                    }

                    return unchecked((int)rv);
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

        public int GainMax => Info.GainMax;
        public int GainMin => Info.GainMin;
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
            get => _liveViewEnabled;
            set {
                if (_liveViewEnabled != value) {
                    _liveViewEnabled = value;
                    RaisePropertyChanged();
                }
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

        public short ReadoutMode {
            get {
                uint mode = 0;
                uint rv;

                if (Connected) {
                    if ((rv = LibQHYCCD.GetQHYCCDReadMode(CameraP, ref mode)) != LibQHYCCD.QHYCCD_SUCCESS) {
                        Logger.Error($"QHYCCD: GetQHYCCDReadMode() failed. Returned {rv}");

                        return -1;
                    }

                    Logger.Debug($"QHYCCD: Current readout mode: {mode} ({ReadoutModes.Cast<string>().ToArray()[mode]})");

                    return (short)mode;
                } else {
                    return -1;
                }
            }
            set {
                uint rv;

                if (Connected && (value != ReadoutMode)) {
                    string modeName = ReadoutModes.Cast<string>().ToArray()[value];
                    Logger.Debug($"QHYCCD: ReadoutMode: Setting readout mode to {value} ({modeName})");

                    if ((rv = LibQHYCCD.SetQHYCCDReadMode(CameraP, (uint)value)) != LibQHYCCD.QHYCCD_SUCCESS) {
                        Logger.Error($"QHYCCD: SetQHYCCDReadMode() failed. Returned {rv}");
                    }
                }
            }
        }

        public IEnumerable ReadoutModes {
            get => Info.ReadoutModes;
            set => Info.ReadoutModes = (List<string>)value;
        }

        public string SensorName => string.Empty;

        public SensorType SensorType { get; private set; } = SensorType.Monochrome;

        public int SubSampleHeight { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }

        public double Temperature {
            get {
                double rv = double.NaN;

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
                    Logger.Debug($"QHYCCD: Cooler target temperature set to {value}");
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
                        Logger.Debug($"QHYCCD: CoolerWorker setting camera target temp to {Info.CoolerTargetTemp}");
                        LibQHYCCD.ControlQHYCCDTemp(CameraP, Info.CoolerTargetTemp);
                    } else if (previous == true) {
                        Logger.Debug("QHYCCD: CoolerWorker turning off TEC due user request");
                        _ = SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_MANULPWM, 0);
                    }

                    previous = Info.CoolerOn;

                    /* sleep (cancelable) */
                    await Task.Delay(LibQHYCCD.QHYCCD_COOLER_DELAY, ct);
                }
            } catch (OperationCanceledException) {
                Logger.Debug("QHYCCD: CoolerWorker task cancelled");
            }
        }

        private double GetControlValue(LibQHYCCD.CONTROL_ID type) {
            double rv;

            if ((rv = LibQHYCCD.GetQHYCCDParam(CameraP, type)) != LibQHYCCD.QHYCCD_ERROR) {
                Logger.Debug($"QHYCCD: Got Control {type} = {rv}");
                return rv;
            } else {
                Logger.Error($"QHYCCD: Failed to Get value for control {type}");
                return LibQHYCCD.QHYCCD_ERROR;
            }
        }

        private bool GetSensorType() {
            Info.BayerPattern = (LibQHYCCD.BAYER_ID)LibQHYCCD.IsQHYCCDControlAvailable(CameraP, LibQHYCCD.CONTROL_ID.CAM_COLOR);

            switch (Info.BayerPattern) {
                case LibQHYCCD.BAYER_ID.BAYER_GB:
                    SensorType = SensorType.GBRG;
                    BayerOffsetX = 1;
                    BayerOffsetY = 1;
                    break;

                case LibQHYCCD.BAYER_ID.BAYER_GR:
                    SensorType = SensorType.GRBG;
                    BayerOffsetX = 1;
                    BayerOffsetY = 0;
                    break;

                case LibQHYCCD.BAYER_ID.BAYER_BG:
                    SensorType = SensorType.BGGR;
                    BayerOffsetX = 0;
                    BayerOffsetY = 1;
                    break;

                case LibQHYCCD.BAYER_ID.BAYER_RG:
                    SensorType = SensorType.RGGB;
                    BayerOffsetX = 0;
                    BayerOffsetY = 0;
                    break;

                default:
                    return false;
            }

            return true;
        }

        public short BayerOffsetX { get; set; } = 0;
        public short BayerOffsetY { get; set; } = 0;

        private bool IsQHYControl(LibQHYCCD.CONTROL_ID type) {
            if (LibQHYCCD.IsQHYCCDControlAvailable(CameraP, type) == LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Debug($"QHYCCD: Control {type} exists");
                return true;
            } else {
                Logger.Debug($"QHYCCD: Control Value {type} is not available");
                return false;
            }
        }

        private bool SetControlValue(LibQHYCCD.CONTROL_ID type, double value) {
            if (LibQHYCCD.SetQHYCCDParam(CameraP, type, value) == LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Debug($"QHYCCD: Setting Control {type} to {value}");
                return true;
            } else {
                Logger.Warning($"QHYCCD: Failed to Set Control {type} with value {value}");
                return false;
            }
        }

        public void AbortExposure() {
            StopExposure();
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task.Run(() => ConnectSync(false), ct);
        }

        public bool ConnectSync(bool isReconnecting) {
            var success = false;
            double min = 0, max = 0, step = 0;
            List<string> modeList = new List<string>();
            StringBuilder cameraID = new StringBuilder(LibQHYCCD.QHYCCD_ID_LEN);
            StringBuilder modeName = new StringBuilder(0);
            uint num_modes = 0;

            try {
                Logger.Info($"QHYCCD: Connecting to {Info.Id}");

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
                 * Set whether we will call GetQHYCCDSingleFrame and friends, or GetQHYCCDLiveFrame and friends.
                 * Note that changing this value requires completely disconnecting and reconnecting the camera,
                 * which is handled in ReconnectForLiveView.
                 */
                if (LiveViewEnabled) {
                    Logger.Debug($"QHYCCD: Stream mode is video stream");
                    LibQHYCCD.SetQHYCCDStreamMode(CameraP, (byte)LibQHYCCD.QHYCCD_CAMERA_MODE.VIDEO_STREAM);
                } else {
                    Logger.Debug($"QHYCCD: Stream mode is single exposure");
                    LibQHYCCD.SetQHYCCDStreamMode(CameraP, (byte)LibQHYCCD.QHYCCD_CAMERA_MODE.SINGLE_EXPOSURE);
                }

                /*
                 * Initialize the camera and make it available for use
                 */
                LibQHYCCD.N_InitQHYCCD(CameraP);

                if (!isReconnecting) {
                    _ = LibQHYCCD.GetQHYCCDChipInfo(CameraP,
                        ref Info.ChipX, ref Info.ChipY,
                        ref Info.ImageX, ref Info.ImageY,
                        ref Info.PixelX, ref Info.PixelY,
                        ref Info.Bpp);

                    Logger.Debug($"QHYCCD: Chip Info: ChipX={Info.ChipX}mm, ChipY={Info.ChipY}mm, ImageX={Info.ImageX}, ImageY={Info.ImageY}, PixelX={Info.PixelX}um, PixelY={Info.PixelY}um, bpp={Info.Bpp}");

                    /*
                     * The Effective Area is a sensor's real imaging area. On sensors that have an overscan area, the effective area will be smaller than
                     * the sensor's dimensions that were reported by GetQHYCCDChipInfo(). If the sensor does not have an overscan area, the values should be equal.
                     */
                    _ = LibQHYCCD.GetQHYCCDEffectiveArea(CameraP, ref Info.EffectiveArea.StartX, ref Info.EffectiveArea.StartY, ref Info.EffectiveArea.SizeX, ref Info.EffectiveArea.SizeY);
                    Logger.Debug($"QHYCCD: Effective Area: StartX={Info.EffectiveArea.StartX}, StartY={Info.EffectiveArea.StartY}, SizeX={Info.EffectiveArea.SizeX}, SizeY={Info.EffectiveArea.SizeY}");

                    StartPixelX = Info.EffectiveArea.StartX;
                    StartPixelY = Info.EffectiveArea.StartY;
                    CameraXSize = (int)Info.EffectiveArea.SizeX;
                    CameraYSize = (int)Info.EffectiveArea.SizeY;

                    /*
                     * Is this a color sensor or not?
                     * If so, do not debayer the image data
                     */
                    if (GetSensorType() == true) {
                        Logger.Info($"QHYCCD: Color camera detected (pattern = {Info.BayerPattern.ToString()}). Setting debayering to off");
                        _ = LibQHYCCD.SetQHYCCDDebayerOnOff(CameraP, false);
                        Info.IsColorCam = true;
                    } else {
                        Info.IsColorCam = false;
                    }

                    /*
                     * See if this camera has any readout modes and build a list of their names if so
                     */
                    _ = LibQHYCCD.GetQHYCCDNumberOfReadModes(CameraP, ref num_modes);
                    Logger.Debug($"QHYCCD: Camera has {num_modes} readout mode(s)");

                    /*
                     * Every camera always has 1 readout mode. We are only interested in ones that have more than that
                     *
                     * There is also a special case for the QHY42PRO: different readout modes on this camera will have
                     * different image dimensions. Until we can properly support that camera, we will skip readout mode
                     * support for it.
                     */
                    if (num_modes > 1 && Info.Model.ToString() != "QHY42PRO") {
                        for (uint i = 0; i < num_modes; i++) {
                            _ = LibQHYCCD.GetQHYCCDReadModeName(CameraP, i, modeName);
                            Logger.Debug($"QHYCCD: Found readout mode \"{modeName.ToString()}\"");
                            modeList.Add(modeName.ToString());
                        }
                    } else {
                        modeList.Add("Default");
                    }

                    ReadoutModes = modeList;

                    /*
                     * Get our min and max shutter speed (exposure times)
                     * The QHY SDK reports this value in microseconds (us)
                     */
                    _ = LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                        LibQHYCCD.CONTROL_ID.CONTROL_EXPOSURE, ref min, ref max, ref step);

                    Info.ExpMin = min;
                    Info.ExpMax = max;
                    Info.ExpStep = step;
                    Logger.Debug($"QHYCCD: ExpMin={Info.ExpMin}, ExpMax={Info.ExpMax}, ExpStep={Info.ExpStep}");

                    /*
                     * Get our min and max gain
                     */
                    _ = LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                        LibQHYCCD.CONTROL_ID.CONTROL_GAIN, ref min, ref max, ref step);

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
                    _ = LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                        LibQHYCCD.CONTROL_ID.CONTROL_OFFSET,
                        ref min, ref max, ref step);

                    Info.OffMin = (int)min;
                    Info.OffMax = (int)max;
                    Info.OffStep = step;
                    Logger.Debug($"QHYCCD: OffMin={Info.OffMin}, OffMax={Info.OffMax}, OffStep={Info.OffStep}");

                    QuirkInflatedOffset();

                    /*
                     * Fetch our min and max PWM settings for
                     * the cooler.
                     */
                    Info.HasCooler = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_COOLER);
                    Info.HasChipTemp = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_CURTEMP);

                    if (Info.HasCooler == true) {
                        _ = LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                            LibQHYCCD.CONTROL_ID.CONTROL_MANULPWM, ref min, ref max, ref step);

                        Info.CoolerPwmMin = min;
                        Info.CoolerPwmMax = max;
                        Info.CoolerPwmStep = step;
                        Logger.Debug($"QHYCCD: CoolerPwmMin={Info.CoolerPwmMin}, CoolerPwmMax={Info.CoolerPwmMax}, CoolerPwmStep={Info.CoolerPwmStep}");

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
                     * QHY SDK offers no way to get the current bin mode! So we track
                     * it manually using QHYCCD_CAMERA_INFO.CurBin. We initialize the
                     * camera with 1x1 binning.
                     */
                    Info.CurBin = 1;
                    SetBinning(Info.CurBin, Info.CurBin);

                    Info.HasShutter = IsQHYControl(LibQHYCCD.CONTROL_ID.CAM_MECHANICALSHUTTER);
                    Info.HasGain = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_GAIN);
                    Info.HasOffset = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET);
                }

                /*
                 * Fetch our min and max USB bandwidth settings. The QHY163M
                 * changes whether CONTROL_USBTRAFFIC based on the StreamMode,
                 * so recompute this when reconnecting.
                 */
                Info.HasUSBTraffic = IsQHYControl(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC);

                if (Info.HasUSBTraffic == true) {
                    _ = LibQHYCCD.GetQHYCCDParamMinMaxStep(CameraP,
                        LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC, ref min, ref max, ref step);

                    Info.USBMin = min;
                    Info.USBMax = max;
                    Info.USBStep = step;
                    Logger.Debug($"QHYCCD: USBMin={Info.USBMin}, USBMax={Info.USBMax}, USBStep={Info.USBStep}");

                    if (QuirkNoUSBTraffic())
                        Info.HasUSBTraffic = false;
                }

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
        }

        public void Disconnect() => Disconnect(false);

        private void Disconnect(bool willReconnect) {
            if (Connected == false)
                return;

            if (!willReconnect) {
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
                    _ = SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_MANULPWM, 0);
                }

                Connected = false;
            }

            Logger.Info($"QHYCCD: Closing camera {Info.Id}");
            LibQHYCCD.N_CloseQHYCCD(CameraP);
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                /* Wait for exposure to finish */
                while (LibQHYCCD.GetQHYCCDExposureRemaining(CameraP) > 0) {
                    await Task.Delay(100, token);
                }
            }
        }

        public Task<IExposureData> DownloadExposure(CancellationToken ct) {
            return Task.Run<IExposureData>(async () => {
                uint width = 0;
                uint height = 0;
                uint bpp = 0;
                uint channels = 0;
                uint rv;

                Logger.Debug("QHYCCD: Downloading exposure...");

                /* Wait for exposure to finish */
                while (LibQHYCCD.GetQHYCCDExposureRemaining(CameraP) > 0) {
                    await Task.Delay(100, ct);
                }

                bool is16bit = Info.Bpp > 8;

                /*
                 * Size the image data byte array for the image
                 */
                uint numPixels = is16bit ? ImageSize / 2U : ImageSize;
                ushort[] ImgData = new ushort[numPixels];

                /*
                 * Download the image from the camera
                 */
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.DOWNLOADING.ToString();
                if (is16bit) {
                    rv = LibQHYCCD.GetQHYCCDSingleFrame(CameraP, ref width, ref height, ref bpp, ref channels, ImgData);
                } else {
                    byte[] ImgDataBytes = new byte[numPixels];
                    rv = LibQHYCCD.GetQHYCCDSingleFrame(CameraP, ref width, ref height, ref bpp, ref channels, ImgDataBytes);
                    for (int i = 0; i < ImgDataBytes.Length; i++) {
                        ImgData[i] = ImgDataBytes[i];
                    }
                }
                if (rv != LibQHYCCD.QHYCCD_SUCCESS) {
                    Logger.Warning($"QHYCCD: Failed to download image from camera! rv = {rv }");
                    throw new Exception(Locale.Loc.Instance["LblASIImageDownloadError"]);
                }

                Logger.Debug($"QHYCCD: Downloaded image: {width}x{height}, {bpp} bpp, {channels} channels");

                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.IDLE.ToString();

                return new ImageArrayExposureData(
                    input: ImgData,
                    width: (int)width,
                    height: (int)height,
                    bitDepth: BitDepth,
                    isBayered: SensorType != SensorType.Monochrome && (BinX == 1 && BinY == 1),
                    metaData: new ImageMetaData());
            }, ct);
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken ct) {
            return Task.Run<IExposureData>(async () => {
                uint rv;
                uint width = 0;
                uint height = 0;
                uint bpp = 0;
                uint channels = 0;

                Logger.Debug("QHYCCD: Downloading exposure...");

                /*
                 * Ask the SDK how big the exposure will be
                 */
                var size = LibQHYCCD.GetQHYCCDMemLength(CameraP);

                if (size == 0) {
                    Logger.Warning("QHYCCD: SDK reported a 0-length image buffer!");
                    throw new Exception(Locale.Loc.Instance["LblASIImageDownloadError"]);
                }
                Logger.Debug($"QHYCCD: Image size will be {size} bytes");

                /*
                 * Size the image data byte array for the image
                 */
                var ImgData = new byte[size];

                while (true) {
                    rv = LibQHYCCD.GetQHYCCDLiveFrame(CameraP, ref width, ref height, ref bpp, ref channels, ImgData);
                    if (rv == uint.MaxValue) {
                        await Task.Yield();
                        // GetQHYCCDLiveFrame returns -1 when the data isn't available yet, requiring looping.
                        continue;
                    } else if (rv != LibQHYCCD.QHYCCD_SUCCESS) {
                        Logger.Warning($"QHYCCD: Failed to download image from camera! rv = {rv}");
                        throw new Exception(Locale.Loc.Instance["LblASIImageDownloadError"]);
                    }
                    break;
                }

                Logger.Debug($"QHYCCD: Downloaded image: {width}x{height}, {bpp} bpp, {channels} channels");

                /*
                 * Copy the image byte array to an allocated buffer
                 */
                IntPtr buf = Marshal.AllocHGlobal(ImgData.Length);
                Marshal.Copy(ImgData, 0, buf, ImgData.Length);
                var cameraDataToManaged = new CameraDataToManaged(buf, (int)width, (int)height, (int)bpp, bitScaling: false);
                var arr = cameraDataToManaged.GetData();
                ImgData = null;
                Marshal.FreeHGlobal(buf);

                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.IDLE.ToString();

                return new ImageArrayExposureData(
                    input: arr,
                    width: (int)width,
                    height: (int)height,
                    bitDepth: this.BitDepth,
                    isBayered: this.SensorType != SensorType.Monochrome,
                    metaData: new ImageMetaData());
            }, ct);
        }

        public void SetBinning(short x, short y) {
            Logger.Debug($"QHYCCD: Setting bin mode to {x}x{y}");
            BinX = x;
        }

        public void SetupDialog() {
        }

        public void StartExposure(CaptureSequence sequence) {
            uint rv;
            uint startx, starty, sizex, sizey;
            bool isSnap;

            /*
             * Setup camera with the desired exposure setttings
             */

            isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;

            /* ROI coordinates and resolution */
            if (EnableSubSample == true) {
                startx = (uint)SubSampleX / (uint)BinX;
                starty = (uint)SubSampleY / (uint)BinY;
                sizex = (uint)SubSampleWidth / (uint)BinX;
                sizey = (uint)SubSampleHeight / (uint)BinY;
            } else {
                startx = StartPixelX / (uint)BinX;
                starty = StartPixelY / (uint)BinY;
                sizex = (uint)CameraXSize / (uint)BinX;
                sizey = (uint)CameraYSize / (uint)BinY;
            }

            Logger.Debug($"QHYCCD: Setting image resolution: startx={startx}, starty={starty}, sizex={sizex}, sizey={sizey}");

            if ((rv = LibQHYCCD.SetQHYCCDResolution(CameraP, startx, starty, sizex, sizey)) != LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Warning($"QHYCCD: Failed to set exposure resolution: rv = {rv}");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            /* Exposure bit depth */
            if (LibQHYCCD.SetQHYCCDBitsMode(CameraP, (uint)BitDepth) != LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to set exposure bit depth. This may not be a fatal error.");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
            }

            /* Exposure length (in microseconds) */
            if (!SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_EXPOSURE, sequence.ExposureTime * 1e6)) {
                Logger.Warning("QHYCCD: Failed to set exposure time");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            /* Exposure readout mode */
            ReadoutMode = isSnap ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

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

            if (LibQHYCCD.ExpQHYCCDSingleFrame(CameraP) == LibQHYCCD.QHYCCD_ERROR) {
                Logger.Warning("QHYCCD: Failed to initiate the exposure!");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.EXPOSING.ToString();
        }

        private void ReconnectForLiveView() {
            // Steps documented as required when changing live view:
            // CloseQHYCCD
            // ReleaseQHYCCDResource
            // ScanQHYCCD
            // OpenQHYCCD
            // SetLiveStreamMode
            // It appears that ReleaseQHYCCDResource and ScanQHYCCD can be skipped in newer drivers?
            Disconnect(true);
            ConnectSync(true);
        }

        public void StartLiveView() {
            LiveViewEnabled = true;
            ReconnectForLiveView();

            // TODO: Use controls on the exposure tab to adjust gain and exposure
            if (!SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_EXPOSURE, 1e6)) {
                Logger.Warning("QHYCCD: Failed to set exposure time");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            if (!SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN, ((double)Info.GainMin + Info.GainMax) / 2)) {
                Logger.Warning("QHYCCD: Failed to set gain");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                return;
            }

            if (LibQHYCCD.BeginQHYCCDLive(CameraP) != LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to start live view");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                LiveViewEnabled = false;
                ReconnectForLiveView();
                return;
            }
            CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.EXPOSING.ToString();

            Logger.Debug("QHYCCD: Enabled live view");
        }

        public void StopExposure() {
            if (LibQHYCCD.CancelQHYCCDExposingAndReadout(CameraP) != LibQHYCCD.QHYCCD_ERROR)
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.IDLE.ToString();
        }

        public void StopLiveView() {
            if (LibQHYCCD.StopQHYCCDLive(CameraP) != LibQHYCCD.QHYCCD_SUCCESS) {
                Logger.Warning("QHYCCD: Failed to stop live view");
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.ERROR.ToString();
                // Continue on to reconnecting the camera
            } else {
                CameraState = LibQHYCCD.QHYCCD_CAMERA_STATE.IDLE.ToString();
            }
            LiveViewEnabled = false;
            ReconnectForLiveView();
            Logger.Debug("QHYCCD: Disabled live view");
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
            _ = SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN, wantGain);

            double curGain = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN);

            /* Restore our original gain setting */
            _ = SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_GAIN, saveGain);

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

            _ = SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET, wantOffset);
            gotOffset = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET) - wantOffset;

            if (gotOffset != 0) {
                Logger.Debug($"QHYCCD_QUIRK: This camera inflates its Offset by {gotOffset}");
                Info.InflatedOff = (int)gotOffset;
            } else {
                Info.InflatedOff = 0;
            }

            /* Restore our original gain setting */
            _ = SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_OFFSET, saveOffset);
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
            _ = SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC, wantUSB);
            double gotUSB = GetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC);

            /* Check to see if it really changed */
            if (gotUSB == saveUSB) {
                Logger.Debug("QHYCCD_QUIRK: This camera does not really allow CONTROL_USBTRAFFIC settings");
                return true;
            }

            /* Restore the original USB traffic setting */
            _ = SetControlValue(LibQHYCCD.CONTROL_ID.CONTROL_USBTRAFFIC, saveUSB);

            return false;
        }

        #endregion "Quirks"
    }
}