#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Altair;
using NINA.Core.Enum;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model.Equipment;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces;
using System.Drawing;
using System.Collections;
using System.Linq;
using NINA.Equipment.Utility;
using System.Diagnostics.Eventing.Reader;
using NINA.Equipment.Equipment.MyCamera.ToupTekAlike;

namespace NINA.Equipment.Equipment.MyCamera {

    public class ToupTekAlikeCamera : BaseINPC, ICamera {
        private ToupTekAlikeFlag flags;
        private IToupTekAlikeCameraSDK sdk;
        private string internalId;

        public ToupTekAlikeCamera(ToupTekAlikeDeviceInfo deviceInfo, IToupTekAlikeCameraSDK sdk, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            Category = sdk.Category;

            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            this.sdk = sdk;
            this.internalId = deviceInfo.id;
            if (sdk is ToupTekAlike.AltairSDKWrapper || sdk is ToupTekAlike.ToupTekSDKWrapper) {
                // Altair cams hava a distinct id in contrast to other touptek brands and the original touptek brand doesn't need the category filter
                this.Id = deviceInfo.id;
            } else {
                this.Id = Category + "_" + deviceInfo.id;
            }

            this.Name = deviceInfo.displayname;
            this.Description = deviceInfo.id;
            this.MaxFanSpeed = (int)deviceInfo.model.maxfanspeed;
            this.PixelSizeX = Math.Round(deviceInfo.model.xpixsz, 2);
            this.PixelSizeY = Math.Round(deviceInfo.model.ypixsz, 2);

            this.flags = (ToupTekAlikeFlag)deviceInfo.model.flag;
        }

        private IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;

        public string Category { get; }

        public bool HasShutter => false;

        public double Temperature {
            get {
                sdk.get_Temperature(out var temp);
                return temp / 10.0;
            }
        }

        public double TemperatureSetPoint {
            get {
                if (CanSetTemperature) {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_TECTARGET, out var target);
                    return target / 10.0;
                } else {
                    return double.NaN;
                }
            }
            set {
                if (CanSetTemperature) {
                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_TECTARGET, (int)(value * 10))) {
                        Logger.Error($"{Category} - Could not set TemperatureSetPoint to {value * 10}");
                    } else {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public bool BinAverageEnabled {
            get => profileService.ActiveProfile.CameraSettings.BinAverageEnabled == true;
            set {
                if (profileService.ActiveProfile.CameraSettings.BinAverageEnabled != value) {
                    profileService.ActiveProfile.CameraSettings.BinAverageEnabled = value;
                    RaisePropertyChanged();
                    // Force binning mode to be set again
                    BinX = BinX;
                }
            }
        }

        public short BinX {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_BINNING, out var bin);
                return (short)(bin & 0x0F);
            }
            set {
                int binValue = value;
                if (binValue > 1 && BinAverageEnabled) {
                    binValue |= 0x80;
                }

                if (!sdk.put_Option(ToupTekAlikeOption.OPTION_BINNING, binValue)) {
                    Logger.Error($"{Category} - Could not set Binning to {binValue}");
                } else {
                    RaisePropertyChanged(nameof(BinX));
                    RaisePropertyChanged(nameof(BinY));
                }
            }
        }

        public short BinY {
            get => BinX;
            set => BinX = value;
        }

        public string SensorName => string.Empty;

        public SensorType SensorType { get; private set; }

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        public int CameraXSize { get; private set; }

        public int CameraYSize { get; private set; }

        public double ExposureMin {
            get {
                sdk.get_ExpTimeRange(out var min, out var max, out var def);
                return min / 1000000.0;
            }
        }

        public double ExposureMax {
            get {
                sdk.get_ExpTimeRange(out var min, out var max, out var def);
                return max / 1000000.0;
            }
        }

        public IList<string> SupportedActions { get; } = new List<string>();

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX { get; private set; }

        public short MaxBinY { get; private set; }

        public double PixelSizeX { get; }

        public double PixelSizeY { get; }

        public int MaxFanSpeed { get; }

        public int FanSpeed {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_FAN, out var fanSpeed);
                return fanSpeed;
            }
            set {
                var currentFanSpeed = FanSpeed;
                var targetFanSpeed = Math.Max(0, Math.Min(MaxFanSpeed, value));
                if (currentFanSpeed != targetFanSpeed) {
                    if (sdk.put_Option(ToupTekAlikeOption.OPTION_FAN, value)) {
                        RaisePropertyChanged();
                    } else {
                        Logger.Error($"{Category} - Could not set Fan to {value}");
                    }
                }
            }
        }

        private bool canGetTemperature;

        public bool CanGetTemperature {
            get => canGetTemperature;
            private set {
                canGetTemperature = value;
                RaisePropertyChanged();
            }
        }

        private bool canSetTemperature;

        public bool CanSetTemperature {
            get => canSetTemperature;
            private set {
                canSetTemperature = value;
                RaisePropertyChanged();
            }
        }

        public bool CoolerOn {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_TEC, out var cooler);
                return cooler == 1;
            }

            set {
                if (sdk.put_Option(ToupTekAlikeOption.OPTION_TEC, value ? 1 : 0)) {
                    if(value) {
                        // If fan is currently off, set it to its minimum speed
                        if (MaxFanSpeed > 0 && FanSpeed == 0) {
                            FanSpeed = 1;
                        }
                    } else {
                        // If turning the TEC off, turn the fan off too
                            FanSpeed = 0;
                    }
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(FanSpeed));
                } else {
                    Logger.Error($"{Category} - Could not set Cooler to {value}");
                }
            }
        }

        private double coolerPower = 0.0;

        public double CoolerPower {
            get => coolerPower;
            private set {
                coolerPower = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource coolerPowerReadoutCts;

        /// <summary>
        /// This task will update cooler power based on TEC Volatage readout every three seconds
        /// Due to the fact that this value must not be updated more than every two seconds according to the documentation
        /// a helper method is required in case the device polling interval is faster than that.
        /// </summary>
        private void CoolerPowerUpdateTask() {
            Task.Run(async () => {
                coolerPowerReadoutCts?.Dispose();
                coolerPowerReadoutCts = new CancellationTokenSource();
                try {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_TEC_VOLTAGE_MAX, out var maxVoltage);
                    while (true) {
                        coolerPowerReadoutCts.Token.ThrowIfCancellationRequested();

                        sdk.get_Option(ToupTekAlikeOption.OPTION_TEC_VOLTAGE, out var voltage);

                        CoolerPower = 100 * voltage / (double)maxVoltage;

                        //Recommendation to not readout CoolerPower in less than two seconds.
                        await Task.Delay(TimeSpan.FromSeconds(3), coolerPowerReadoutCts.Token);
                    }
                } catch (OperationCanceledException) {
                }
            });
        }

        private bool hasDewHeater;

        public bool HasDewHeater {
            get => hasDewHeater;
            private set {
                hasDewHeater = value;
                RaisePropertyChanged();
            }
        }

        public int MaxDewHeaterStrength {
            get {
                if(HasDewHeater) {

                    sdk.get_Option(ToupTekAlikeOption.OPTION_HEAT_MAX, out var max);
                    return max;
                }
                return 0;
            }
        }

        public int TargetDewHeaterStrength {
            get => profileService.ActiveProfile.CameraSettings.TouptekAlikeDewHeaterStrength;
            set {
                var max = MaxDewHeaterStrength;
                if(value < 1) { value = 1; }
                if(value > max) { value = max; }
                profileService.ActiveProfile.CameraSettings.TouptekAlikeDewHeaterStrength = value;
                if (DewHeaterOn) {
                    sdk.put_Option(ToupTekAlikeOption.OPTION_HEAT, value);
                }
                RaisePropertyChanged();
            }
        }

        public bool DewHeaterOn {
            get {
                if (HasDewHeater) {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_HEAT, out var heat);
                    return heat > 0;
                } else {
                    return false;
                }
            }
            set {
                if (HasDewHeater) {
                    if (value) {
                        sdk.put_Option(ToupTekAlikeOption.OPTION_HEAT, TargetDewHeaterStrength);
                    } else {
                        sdk.put_Option(ToupTekAlikeOption.OPTION_HEAT, 0);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public CameraStates CameraState => CameraStates.NoState;

        public bool CanSubSample => true;

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }
        public bool CanShowLiveView => false;
        public bool LiveViewEnabled { get; set; }

        public bool HasBattery => false;

        public int BatteryLevel => -1;

        public int Offset {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_BLACKLEVEL, out var level);
                return level;
            }
            set {
                if (!sdk.put_Option(ToupTekAlikeOption.OPTION_BLACKLEVEL, value)) {
                    Logger.Error($"{Category} - Could not set Offset to {value}");
                } else {
                    RaisePropertyChanged();
                }
            }
        }

        public int OffsetMin => 0;

        public int OffsetMax => 31 * (1 << nativeBitDepth - 8);

        public int USBLimit {
            get {
                sdk.get_Speed(out var speed);
                return speed;
            }
            set {
                if (value >= USBLimitMin && value <= USBLimitMax) {
                    if (!sdk.put_Speed((ushort)value)) {
                        Logger.Error($"{Category} - Could not set USBLimit to {value}");
                    } else {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public int USBLimitMin => 0;

        public int USBLimitMax => (int)sdk.MaxSpeed;

        private bool canSetOffset;

        public bool CanSetOffset {
            get => canSetOffset;
            set {
                canSetOffset = value;
                RaisePropertyChanged();
            }
        }

        public bool CanSetUSBLimit => true;

        public bool CanGetGain => sdk.get_ExpoAGain(out var gain);

        public bool CanSetGain => GainMax != GainMin;

        public int GainMax {
            get {
                sdk.get_ExpoAGainRange(out var min, out var max, out var def);
                return max;
            }
        }

        public int GainMin {
            get {
                sdk.get_ExpoAGainRange(out var min, out var max, out var def);
                return min;
            }
        }

        public int Gain {
            get {
                sdk.get_ExpoAGain(out var gain);
                return gain;
            }

            set {
                if (value >= GainMin && value <= GainMax) {
                    if (!sdk.put_ExpoAGain((ushort)value)) {
                        Logger.Error($"{Category} - Could not set Gain to {value}");
                    } else {
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public IList<string> ReadoutModes { get; private set; }

        public short ReadoutMode {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_CG, out var value);
                Logger.Trace($"{Category} - Conversion Gain is set to {value}");
                return (short)value;            }
            set {
                Logger.Trace($"{Category} - Setting Conversion Gain to {value}");
                if (!sdk.put_Option(ToupTekAlikeOption.OPTION_CG, value)) {
                    Logger.Error($"{Category} - Could not set HighGainMode to {value}");
                } else {
                    RaisePropertyChanged();
                }
            }
        }


        private short _readoutModeForNormalImages = 0;
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

        private short _readoutModeForSnapImages = 0;
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

        public IList<int> Gains => new List<int>();

        private AsyncObservableCollection<BinningMode> binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (binningModes == null) {
                    binningModes = new AsyncObservableCollection<BinningMode>();
                }
                return binningModes;
            }
            private set {
                binningModes = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => false;

        private string id;

        public string Id {
            get => id;
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string name;

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }
        public string DisplayName => $"{Name} ({(Id.Length > 8 ? Id[^8..] : Id)})";

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                if (!_connected) {
                    try { coolerPowerReadoutCts?.Cancel(); } catch { }
                }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSetupDialog));
            }
        }

        private string description;

        public string Description {
            get => description;
            set {
                description = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo => $"{Category} SDK";

        public string DriverVersion => sdk?.Version() ?? string.Empty;

        public void AbortExposure() {
            StopExposure();
        }

        private void ReadOutBinning() {
            /* Found no way to readout available binning modes. Assume 4x4 for all cams for now */
            BinningModes.Clear();
            MaxBinX = 4;
            MaxBinY = 4;
            for (short i = 1; i <= MaxBinX; i++) {
                BinningModes.Add(new BinningMode(i, i));
            }
        }

        public Task<bool> Connect(CancellationToken ct) {
            return Task<bool>.Run(() => {
                var success = false;
                try {
                    SupportedActions.Clear();
                    imageReadyTCS?.TrySetCanceled();
                    imageReadyTCS = null;

                    sdk = sdk.Open(this.internalId);
                    success = true;
                    var profile = profileService.ActiveProfile.CameraSettings;

                    /* Use maximum bit depth */
                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_BITDEPTH, 1)) {
                        throw new Exception($"{Category} - Could not set bit depth");
                    }

                    /* Use RAW Mode */
                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_RAW, 1)) {
                        throw new Exception($"{Category} - Could not set RAW mode");
                    }

                    if (!sdk.put_AutoExpoEnable(false)) {
                        Logger.Error($"{Category} - Could not disable Auto Exposure mode");
                    }

                    ReadOutBinning();
                    SupportedActions.Add(ToupTekActions.BinAverage);

                    sdk.get_Size(out var width, out var height);
                    this.CameraXSize = width;
                    this.CameraYSize = height;

                    /* Readout flags */
                    if ((this.flags & ToupTekAlikeFlag.FLAG_TEC_ONOFF) != 0) {
                        /* Can set Target Temp */
                        CanSetTemperature = true;
                        sdk.get_Option(ToupTekAlikeOption.OPTION_TECTARGET, out var target);
                        if (target >= -280 && target <= 100) {
                            TemperatureSetPoint = target;
                        } else {
                            TemperatureSetPoint = 20;
                        }
                        // Start with cooler disabled
                        CoolerOn = false;
                        CoolerPowerUpdateTask();
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_GETTEMPERATURE) != 0) {
                        /* Can get Target Temp */
                        CanGetTemperature = true;
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_BLACKLEVEL) != 0) {
                        CanSetOffset = true;
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_HEAT) != 0) {
                        HasDewHeater = true;
                        SupportedActions.Add(ToupTekActions.DewHeaterStrength);
                        if (profile.TouptekAlikeDewHeaterStrength < 0) {
                            TargetDewHeaterStrength = MaxDewHeaterStrength;
                        } else {
                            TargetDewHeaterStrength = profile.TouptekAlikeDewHeaterStrength;
                        }                        
                    }

                    if ((this.flags & ToupTekAlikeFlag.FLAG_LOW_NOISE) != 0) {
                        HasLowNoiseMode = true;
                        LowNoiseMode = profile.TouptekAlikeUltraMode;
                        SupportedActions.Add(ToupTekActions.LowNoiseMode);
                    }

                    if((this.flags & ToupTekAlikeFlag.FLAG_HIGH_FULLWELL) != 0) {
                        HasHighFullwell = true;
                        SupportedActions.Add(ToupTekActions.HighFullwellMode);
                        HighFullwellMode = profile.TouptekAlikeHighFullwell;
                    } else {
                        HasHighFullwell = false;
                    }

                    if(CanSetLEDLights) {
                        SupportedActions.Add(ToupTekActions.LEDLights);
                        LEDLights = profile.TouptekAlikeLEDLights;
                    }                    

                    ReadoutModes = new List<string> { "Low Conversion Gain" };

                    if ((this.flags & ToupTekAlikeFlag.FLAG_CG) != 0) {
                        ReadoutModes.Add("High Conversion Gain");
                        Logger.Debug($"{Category} - Camera has High Conversion Gain option");

                        if ((this.flags & ToupTekAlikeFlag.FLAG_CGHDR) != 0) {
                            ReadoutModes.Add("High Dynamic Range");
                            Logger.Debug($"{Category} - Camera has HDR Gain option");
                        }
                    }  

                    if ((this.flags & ToupTekAlikeFlag.FLAG_TRIGGER_SOFTWARE) == 0) {
                        throw new Exception($"{Category} - This camera is not capable to be triggered by software and is not supported");
                    }

                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_FRAME_DEQUE_LENGTH, 2)) {
                        throw new Exception($"{Category} - Could not set deque length");
                    }

                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_TRIGGER, 1)) {
                        throw new Exception($"{Category} - Could not set Trigger manual mode");
                    }

                    if (!sdk.StartPullModeWithCallback(new ToupTekAlikeCallback(OnEventCallback))) {
                        throw new Exception($"{Category} - Could not start pull mode");
                    }

                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_FLUSH, 3)) {
                        Logger.Debug($"{Category} - Unable to flush camera");
                    }

                    if (!sdk.get_RawFormat(out var fourCC, out var bitDepth)) {
                        throw new Exception($"{Category} - Unable to get format information");
                    } else {
                        if (sdk.MonoMode) {
                            SensorType = SensorType.Monochrome;
                        } else {
                            SensorType = GetSensorType(fourCC);
                        }
                    }

                    this.nativeBitDepth = (int)bitDepth;

                    Connected = true;
                    RaiseAllPropertiesChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
                return success;
            });
        }

        private SensorType GetSensorType(uint fourCC) {
            var bytes = BitConverter.GetBytes(fourCC);
            if (!BitConverter.IsLittleEndian) { Array.Reverse(bytes); }

            var sensor = System.Text.Encoding.ASCII.GetString(bytes);
            if (Enum.TryParse(sensor, true, out SensorType sensorType)) {
                return sensorType;
            }
            return SensorType.RGGB;
        }

        private bool _hasLowNoiseMode;

        public bool HasLowNoiseMode {
            get => _hasLowNoiseMode;
            set {
                _hasLowNoiseMode = value;
                RaisePropertyChanged();
            }
        }

        public bool LowNoiseMode {
            get {
                if (HasLowNoiseMode) {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_LOW_NOISE, out var value);
                    Logger.Trace($"{Category} - Low Noise Mode is set to {value}");
                    return value == 1;
                } else {
                    return false;
                }
            }
            set {
                if (HasLowNoiseMode) {
                    Logger.Debug($"{Category} - Setting Low Noise Mode to {value}");
                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_LOW_NOISE, value ? 1 : 0)) {
                        Logger.Error($"{Category} - Could not set LowNoiseMode to {value}");
                    } else {
                        profileService.ActiveProfile.CameraSettings.TouptekAlikeUltraMode = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        private bool hasHighFullwell;

        public bool HasHighFullwell {
            get => hasHighFullwell;
            set {
                hasHighFullwell = value;
                RaisePropertyChanged();
            }
        }

        public bool HighFullwellMode {
            get {
                if (HasHighFullwell) {
                    sdk.get_Option(ToupTekAlikeOption.OPTION_HIGH_FULLWELL, out var value);
                    Logger.Trace($"{Category} - High Fullwell mode is set to {value}");
                    return value == 1 ? true : false;
                } else {
                    return false;
                }
            }
            set {
                if (HasHighFullwell) {
                    Logger.Trace($"{Category} - High Fullwell mode to {value}");
                    if (!sdk.put_Option(ToupTekAlikeOption.OPTION_HIGH_FULLWELL, value ? 1 : 0)) {
                        Logger.Error($"{Category} - Could not set High Fullwell mode to {value}");
                    } else {
                        profileService.ActiveProfile.CameraSettings.TouptekAlikeHighFullwell = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public bool CanSetLEDLights {
            // Disabled due to SDK issue
            get => false;
        }

        public bool LEDLights {
            get {
                sdk.get_Option(ToupTekAlikeOption.OPTION_TAILLIGHT, out var value);
                Logger.Trace($"{Category} - LED Lights option is set to {value}");
                return value == 1 ? true : false;
            }
            set {
                Logger.Trace($"{Category} - Set LED Lights option to {value}");
                if (!sdk.put_Option(ToupTekAlikeOption.OPTION_TAILLIGHT, value ? 1 : 0)) {
                    Logger.Error($"{Category} - Could not set LED Lights option to {value}");
                } else {
                    profileService.ActiveProfile.CameraSettings.TouptekAlikeLEDLights = value;
                    RaisePropertyChanged();
                }
            }
        }

        private void OnEventCallback(ToupTekAlikeEvent nEvent) {
            Logger.Trace($"{Category} - OnEventCallback {nEvent}");
            switch (nEvent) {
                // We should get an EVENT_IMAGE every time that the camera tells us an image is ready
                case ToupTekAlikeEvent.EVENT_IMAGE:
                    var id = imageReadyTCS?.Task?.Id ?? -1;
                    if (id != -1) {
                        Logger.Trace($"{Category} - Setting DownloadExposure Result on Task {id}");
                        var success = imageReadyTCS?.TrySetResult(true);
                        Logger.Trace($"{Category} - DownloadExposure Result on Task {id} set successfully: {success}");
                    } else {
                        Logger.Trace($"{Category} - unexpected EVENT_IMAGE returned by camera, likely buggy vendor SDK");
                        // retrieve the data and ignore it -- workaround for 269C
                        PullImage();
                    }
                    break;

                // This should never crop up - it's only for still images from live view
                case ToupTekAlikeEvent.EVENT_STILLIMAGE:
                    Logger.Warning($"{Category} - Still image event received, but not expected to get one!");
                    imageReadyTCS?.TrySetResult(true);
                    break;

                case ToupTekAlikeEvent.EVENT_NOFRAMETIMEOUT:
                    Logger.Error($"{Category} - Timout event occurred!");
                    break;

                case ToupTekAlikeEvent.EVENT_TRIGGERFAIL:
                    Logger.Error($"{Category} - Trigger Fail event received!");
                    break;

                case ToupTekAlikeEvent.EVENT_ERROR: // Error
                    Logger.Error($"{Category} - Camera reported a generic error!");
                    Notification.ShowError("Camera reported a generic error and needs to be reconnected!");
                    Disconnect();
                    break;

                case ToupTekAlikeEvent.EVENT_DISCONNECTED:
                    Logger.Warning($"{Category} - Camera disconnected! Maybe USB connection was interrupted.");
                    Notification.ShowError("Camera disconnected! Maybe USB connection was interrupted.");
                    OnEventDisconnected();
                    break;
            }
        }

        private IExposureData PullImage() {
            /* peek the width and height */
            var binning = BinX;
            var width = CameraXSize / binning;
            var height = CameraYSize / binning;

            if (roiInfo.HasValue) {
                width = roiInfo.Value.Width / binning;
                height = roiInfo.Value.Height / binning;
            }
            width -= width % 2;
            height -= height % 2;

            var size = width * height;
            var data = new ushort[size];

            if (!sdk.PullImageV2(data, nativeBitDepth, out var info)) {
                Logger.Error($"{Category} - Failed to pull image");
                return null;
            }

            if (!sdk.put_Option(ToupTekAlikeOption.OPTION_FLUSH, 2)) {
                Logger.Error($"{Category} - Unable to flush camera");
            }

            var bitScaling = this.profileService.ActiveProfile.CameraSettings.BitScaling;
            if (bitScaling) {
                var shift = 16 - nativeBitDepth;
                for (var i = 0; i < data.Length; i++) {
                    data[i] = (ushort)(data[i] << shift);
                }
            }

            var metaData = new ImageMetaData();
            metaData.FromCamera(this);
            var imageData = exposureDataFactory.CreateImageArrayExposureData(
                    input: data,
                    width: width,
                    height: height,
                    bitDepth: this.BitDepth,
                    isBayered: this.SensorType != SensorType.Monochrome,
                    metaData: metaData);

            return imageData;
        }

        public void Disconnect() {
            try { coolerPowerReadoutCts?.Cancel(); } catch { }
            Connected = false;
            sdk.Close();
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                await imageReadyTCS.Task;
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            if (imageReadyTCS?.Task.IsCanceled != false) { return null; }
            IExposureData exposureData;
            using (token.Register(() => imageReadyTCS.TrySetCanceled())) {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15))) {
                    using (cts.Token.Register(() => { Logger.Error($"{Category} - No Image Callback Event received"); imageReadyTCS.TrySetResult(true); })) {
                        var imageReady = await imageReadyTCS.Task;
                        exposureData = PullImage();
                    }
                }
            }
            if (LiveViewEnabled) {
                imageReadyTCS?.TrySetCanceled();
                imageReadyTCS = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            return exposureData;
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            var localCTS = CancellationTokenSource.CreateLinkedTokenSource(token);
            return DownloadExposure(localCTS.Token);
        }

        public void SetBinning(short x, short y) {
            if (x <= MaxBinX) {
                BinX = x;
                RaisePropertyChanged(nameof(BinY));
            }
        }

        public void SetupDialog() {
        }

        /// <summary>
        /// Sets the exposure time. When given exposure time is out of bounds it will set it to nearest bound.
        /// </summary>
        /// <param name="time">Time in seconds</param>
        private void SetExposureTime(double time) {
            if (time < ExposureMin) {
                time = ExposureMin;
            }
            if (time > ExposureMax) {
                time = ExposureMax;
            }

            var usTime = (uint)(time * 1000000);
            if (!sdk.put_ExpoTime(usTime)) {
                throw new Exception($"{Category} - Could not set exposure time");
            }
        }

        private Rectangle GetROI() {
            var x = SubSampleX;
            x -= x % 2;
            var y = (CameraYSize - SubSampleY - SubSampleHeight);
            y -= y % 2;
            var width = Math.Max(SubSampleWidth, 16);
            width -= width % 2;
            var height = Math.Max(SubSampleHeight, 16);
            height -= height % 2;
            return new Rectangle(x, y, width, height);
        }

        private Rectangle? roiInfo;

        public void StartExposure(CaptureSequence sequence) {
            imageReadyTCS?.TrySetCanceled();
            imageReadyTCS = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Logger.Trace($"{Category} - created new downloadExposure Task with Id {imageReadyTCS.Task.Id}");

            ReadoutMode = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT ? ReadoutModeForSnapImages : ReadoutModeForNormalImages;

            if (EnableSubSample) {
                var rect = GetROI();
                roiInfo = rect;
                if (!sdk.put_ROI((uint)rect.X, (uint)rect.Y, (uint)rect.Width, (uint)rect.Height)) {
                    throw new Exception($"{Category} - Failed to set ROI to {rect.X}x{rect.Y}x{rect.Width}x{rect.Height}");
                }
            } else {
                roiInfo = null;
                // 0,0,0,0 resets the ROI to original size
                if (!sdk.put_ROI(0, 0, 0, 0)) {
                    throw new Exception($"{Category} - Failed to reset ROI");
                }
            }

            SetExposureTime(sequence.ExposureTime);

            if (!sdk.Trigger(1)) {
                throw new Exception($"{Category} - Failed to trigger camera");
            }
        }

        private TaskCompletionSource<bool> imageReadyTCS;
        private int nativeBitDepth;
        public int BitDepth => profileService.ActiveProfile.CameraSettings.BitScaling ? 16 : nativeBitDepth;

        private void OnEventDisconnected() {
            StopExposure();
            Disconnect();
        }

        public void StartLiveView(CaptureSequence sequence) {
            imageReadyTCS?.TrySetCanceled();
            imageReadyTCS = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Logger.Trace($"{Category} - starting live view Task with Id {imageReadyTCS.Task.Id}");

            if (EnableSubSample) {
                var rect = GetROI();
                roiInfo = rect;
                if (!sdk.put_ROI((uint)rect.X, (uint)rect.Y, (uint)rect.Width, (uint)rect.Height)) {
                    throw new Exception($"{Category} - Failed to set ROI to {rect.X}x{rect.Y}x{rect.Width}x{rect.Height}");
                }
            } else {
                roiInfo = null;
                // 0,0,0,0 resets the ROI to original size
                if (!sdk.put_ROI(0, 0, 0, 0)) {
                    throw new Exception($"{Category} - Failed to reset ROI");
                }
            }

            SetExposureTime(sequence.ExposureTime);
            LiveViewEnabled = true;

            if (!sdk.put_Option(ToupTekAlikeOption.OPTION_TRIGGER, 0)) {
                throw new Exception("Could not set Trigger video mode");
            }
        }

        public void StopExposure() {
            if (!sdk.Trigger(0)) {
                Logger.Warning($"{Category} - Could not stop exposure");
            }
            imageReadyTCS?.TrySetCanceled();
        }

        public void StopLiveView() {
            imageReadyTCS.Task.ContinueWith((Task<bool> o) => {
                if (!sdk.put_Option(ToupTekAlikeOption.OPTION_TRIGGER, 1)) {
                    Disconnect();
                    throw new Exception("Could not set Trigger manual mode. Reconnect Camera!");
                }
                LiveViewEnabled = false;
            });
        }

        public int USBLimitStep => 1;

        public string Action(string actionName, string actionParameters) {
            switch (actionName) {
                case ToupTekActions.LowNoiseMode:
                    if (HasLowNoiseMode) {
                        var flag = StringToBoolean(actionParameters);
                        if(flag.HasValue) {
                            Logger.Info($"Device Action {actionName}: {flag.Value}");
                            LowNoiseMode = flag.Value;
                            return "1";
                        } else {
                            Logger.Error($"Unrecognized parameter [{actionParameters}] for action [{actionName}].");
                            return "0";
                        }
                    }
                    break;
                case ToupTekActions.HighFullwellMode: 
                    if (HasHighFullwell) {
                        var flag = StringToBoolean(actionParameters);
                        if (flag.HasValue) {
                            Logger.Info($"Device Action {actionName}: {flag.Value}");
                            HighFullwellMode = flag.Value;
                            return "1";
                        } else {
                            Logger.Error($"Unrecognized parameter [{actionParameters}] for action [{actionName}].");
                            return "0";
                        }
                    }
                    break;
                case ToupTekActions.BinAverage: { 
                        var flag = StringToBoolean(actionParameters);
                        if (flag.HasValue) {
                            Logger.Info($"Device Action {actionName}: {flag.Value}");
                            BinAverageEnabled = flag.Value;
                            return "1";
                        } else {
                            Logger.Error($"Unrecognized parameter [{actionParameters}] for action [{actionName}].");
                            return "0";
                        }
                    }
                case ToupTekActions.FanSpeed:
                    if (MaxFanSpeed > 0) {
                        if(int.TryParse(actionParameters, out var flag)) {
                            Logger.Info($"Device Action {actionName}: {flag}");
                            FanSpeed = flag;
                            return "1";

                        } else {
                            Logger.Error($"Unrecognized parameter [{actionParameters}] for action [{actionName}].");
                            return "0";
                        }
                    }
                    break;
                case ToupTekActions.DewHeaterStrength:
                    if (HasDewHeater) {
                        if (int.TryParse(actionParameters, out var flag)) {
                            Logger.Info($"Device Action {actionName}: {flag}");
                            TargetDewHeaterStrength = flag;
                            return "1";

                        } else {
                            Logger.Error($"Unrecognized parameter [{actionParameters}] for action [{actionName}].");
                            return "0";
                        }
                    }
                    break;
                case ToupTekActions.LEDLights:
                    if(CanSetLEDLights) {
                        var flag = StringToBoolean(actionParameters);
                        if (flag.HasValue) {
                            Logger.Info($"Device Action {actionName}: {flag.Value}");
                            LEDLights = flag.Value;
                            return "1";
                        } else {
                            Logger.Error($"Unrecognized parameter [{actionParameters}] for action [{actionName}].");
                            return "0";
                        }
                    }
                    break;
            }

            Logger.Error($"Unsupported action [{actionName}]");
            return "0";
            
        }

        private bool? StringToBoolean(string input) {
            if(string.IsNullOrWhiteSpace(input)) { return null; }

            string[] booleanFalse = { "0", "off", "no", "false", "f" };
            string[] booleanTrue = { "1", "on", "yes", "true", "t" };

            if(booleanFalse.Contains(input.ToLower())) {
                return false;
            }
            if(booleanTrue.Contains(input.ToLower())) {
                return true;
            }
            return null;
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

        private static class ToupTekActions {
            public const string LowNoiseMode = "Ultra Mode";
            public const string HighFullwellMode = "High Fullwell Mode";
            public const string BinAverage = "Bin Average";
            public const string DewHeaterStrength = "Dew Heater Strength";
            public const string FanSpeed = "Fan Speed";
            public const string LEDLights = "LED Lights";
        }
    }
}