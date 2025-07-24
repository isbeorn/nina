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
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.SDK.CameraSDKs.AtikSDK;
using NINA.Equipment.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static NINA.Equipment.SDK.CameraSDKs.AtikSDK.AtikCameraDll;

namespace NINA.Equipment.Equipment.MyCamera {

    public class AtikCamera : BaseINPC, ICamera {

        private List<PresetInformation> presetInformation;
        private TaskCompletionSource<bool> fastExposureSpeedTCS;
        private AtikCameraDll.ArtemisSetFastCallback fastModeCallback;
        private readonly object exposureDataLock = new object();
        private IExposureData exposureData = null;

        public AtikCamera(int id, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            _cameraId = id;
            Name = GetDeviceName(_cameraId);
            Id = GetDeviceSerialNumber(_cameraId);
        }

        private readonly int _cameraId;
        private IntPtr _cameraP = IntPtr.Zero;

        public string Category => "Atik";

        private ArtemisPropertiesStruct _info;

        private ArtemisPropertiesStruct Info => _info;

        private bool _hasShutter = false;

        public bool HasShutter {
            get => _hasShutter;
            private set {
                _hasShutter = value;
                Logger.Trace($"HasShutter = {_hasShutter}");
                RaisePropertyChanged();
            }
        }

        public bool Connected => _cameraP != IntPtr.Zero && IsConnected(_cameraP);

        public double Temperature => GetTemperature(_cameraP);

        public bool CanShowLiveView => false;

        private double _temperature;

        public double TemperatureSetPoint {
            get {
                _temperature = GetSetpoint(_cameraP);
                return _temperature;
            }

            set {
                if (CanSetTemperature) {
                    _temperature = value;
                    if (CoolerOn) {
                        SetCooling(_cameraP, _temperature);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public bool CanSubSample => true;
        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        public IList<string> SupportedActions => new List<string>();

        private bool _coolerOn;

        public bool CoolerOn {
            get => _coolerOn;
            set {
                try {
                    if (Connected) {
                        if (_coolerOn != value) {
                            _coolerOn = value;
                            if (_coolerOn == false) {
                                SetWarmup(_cameraP);
                            } else {
                                SetCooling(_cameraP, _temperature);
                            }
                        }
                        RaisePropertyChanged();
                    }
                } catch (Exception) {
                    _coolerOn = false;
                }
            }
        }

        public short BinX {
            get {
                GetBinning(_cameraP, out var x, out _);
                return (short)x;
            }

            set {
                if (value < MaxBinX) {
                    AtikCameraDll.SetBinning(_cameraP, value, value);
                    RaisePropertyChanged();
                }
            }
        }

        public short BinY {
            get {
                GetBinning(_cameraP, out _, out var y);
                return (short)y;
            }

            set {
                if (value < MaxBinY) {
                    AtikCameraDll.SetBinning(_cameraP, value, value);
                    RaisePropertyChanged();
                }
            }
        }

        public string Description => CleanedUpString(Info.Manufacturer) + " " + Name + " (SerialNo: " + Id + ")";

        public string DriverInfo => DriverName;

        public string DriverVersion => AtikCameraDll.DriverVersion;

        public string SensorName => string.Empty;

        public SensorType SensorType { get; private set; } = SensorType.Monochrome;

        public short BayerOffsetX { get; private set; } = 0;

        public short BayerOffsetY { get; private set; } = 0;

        public int CameraXSize {
            get {
                if(SensorType != SensorType.Monochrome) {
                    return Info.nPixelsX - Info.nPixelsX % 8; 
                }
                return Info.nPixelsX; 
            }
        }

        public int CameraYSize {
            get {
                if (SensorType != SensorType.Monochrome) {
                    return Info.nPixelsY - Info.nPixelsY % 2;
                }
                return Info.nPixelsY;
            }
        }

        private double _exposureMin = 0d;
        public double ExposureMin {
            get => _exposureMin;
            private set {
                _exposureMin = value;
                Logger.Debug($"ExposureMin = {_exposureMin}");
                RaisePropertyChanged();
            }
        }

        public double ExposureMax => 3600d;

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX {
            get {
                GetMaxBinning(_cameraP, out var x, out _);
                return (short)x > 10 ? (short)10 : (short)x;
            }
        }

        public short MaxBinY {
            get {
                GetMaxBinning(_cameraP, out _, out var y);
                return (short)y > 10 ? (short)10 : (short)y;
            }
        }

        public double PixelSizeX => Info.PixelMicronsX;

        public double PixelSizeY => Info.PixelMicronsY;

        public string LensName => string.Empty;

        public double LensFocalLength => double.NaN;

        public double LensFocalRatio => double.NaN;

        public bool canSetTemperature = false;

        public bool CanSetTemperature {
            get => canSetTemperature;
            private set {
                canSetTemperature = value;
                Logger.Trace($"CanSetTemperature = {canSetTemperature}");
                RaisePropertyChanged();
            }
        }

        private bool canGetCoolerPower = false;

        private bool CanGetCoolerPower {
            get => canGetCoolerPower;
            set {
                canGetCoolerPower = value;
                Logger.Trace($"CanGetCoolerPower = {canGetCoolerPower}");
            }
        }

        public double CoolerPower => CanGetCoolerPower ? AtikCameraDll.CoolerPower(_cameraP) : double.NaN;

        private bool _hasDewHeater = false;

        public bool HasDewHeater {
            get => _hasDewHeater;
            private set {
                _hasDewHeater = value;
                Logger.Trace($"HasDewHeater = {_hasDewHeater}");
                RaisePropertyChanged();
            }
        }

        private bool _dewHeaterOn = false;

        public bool DewHeaterOn {
            get => _dewHeaterOn;
            set {
                if (_dewHeaterOn != value) {
                    if (value) {
                        SetWindowHeaterPower(WindowHeaterPowerLevel);
                    } else {
                        SetWindowHeaterPower(0);
                    }

                    _dewHeaterOn = value;
                }
            }
        }

        public CameraStates CameraState {
            get {
                CameraStates state = CameraStates.Idle;

                switch (AtikCameraDll.CameraState(_cameraP)) {
                    case ArtemisCameraStateEnum.CAMERA_EXPOSING:
                        state = CameraStates.Exposing;
                        break;

                    case ArtemisCameraStateEnum.CAMERA_WAITING:
                    case ArtemisCameraStateEnum.CAMERA_FLUSHING:
                        state = CameraStates.Waiting;
                        break;

                    case ArtemisCameraStateEnum.CAMERA_DOWNLOADING:
                        state = CameraStates.Download;
                        break;

                    case ArtemisCameraStateEnum.CAMERA_ERROR:
                        state = CameraStates.Error;
                        break;
                }

                return state;
            }
        }

        private bool _canSetOffset = false;

        public bool CanSetOffset {
            get => _canSetOffset;
            private set {
                _canSetOffset = value;
                RaisePropertyChanged();
            }
        }

        private int _offsetMax = -1;

        public int OffsetMax {
            get => _offsetMax;
            private set {
                _offsetMax = value;
                RaisePropertyChanged();
            }
        }

        private int _offsetMin = -1;

        public int OffsetMin {
            get => _offsetMin;
            private set {
                _offsetMin = value;
                RaisePropertyChanged();
            }
        }

        public int Offset {
            get {
                if (!CanSetOffset) { return -1; }

                if (GainPreset == 0) {
                    return GetOffset();
                } else {
                    return presetInformation[GainPreset].Offset;
                }
            }
            set {
                if (CanSetOffset == false || value > ushort.MaxValue) { return; }

                if (GainPreset == 0) {
                    SetOffset(value);
                }

                RaisePropertyChanged();
            }
        }

        public bool CanSetUSBLimit => false;

        public int USBLimit {
            get => -1;
            set { }
        }

        public int USBLimitMax => -1;
        public int USBLimitMin => -1;
        public int USBLimitStep => -1;

        private bool _canGetGain = false;

        public bool CanGetGain {
            get => _canGetGain;
            private set {
                _canGetGain = value;
                RaisePropertyChanged();
            }
        }

        private bool _canSetGain = false;

        public bool CanSetGain {
            get => _canSetGain;
            private set {
                _canSetGain = value;
                RaisePropertyChanged();
            }
        }

        private int _gainMax = -1;

        public int GainMax {
            get => _gainMax;
            private set {
                _gainMax = value;
                RaisePropertyChanged();
            }
        }

        private int _gainMin = -1;

        public int GainMin {
            get => _gainMin;
            private set {
                _gainMin = value;
                RaisePropertyChanged();
            }
        }

        public int Gain {
            get {
                if (!CanGetGain) { return -1; }

                if (GainPreset == 0) {
                    return GetGain();
                } else {
                    return presetInformation[GainPreset].Gain;
                }
            }
            set {
                if (!CanSetGain || value > ushort.MaxValue) { return; }

                if (GainPreset == 0) {
                    SetGain(value);
                }

                RaisePropertyChanged();
            }
        }

        public IList<int> Gains => new List<int>();

        private IList<string> readoutModes = new List<string>() { "Normal" };

        public IList<string> ReadoutModes => readoutModes;

        private short readoutMode = 0;

        public short ReadoutMode {
            get => readoutMode;
            set {
                if (readoutModes.Count > 1 && value <= (readoutModes.Count - 1)) {
                    Logger.Trace($"Setting camera readout mode to {readoutModes[value]} ({value})");

                    if (readoutModes[value].Equals("Fast")) {
                        SetArtemisPreview(_cameraP, true);
                    } else {
                        SetArtemisPreview(_cameraP, false);
                    }

                    readoutMode = value;
                }
            }
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

        public int BitDepth => 16;

        private AsyncObservableCollection<BinningMode> _binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (_binningModes == null) {
                    _binningModes = new AsyncObservableCollection<BinningMode>();
                    for (short i = 1; i <= MaxBinX; i++) {
                        _binningModes.Add(new BinningMode(i, i));
                    }
                }
                return _binningModes;
            }
        }

        public bool HasExposureSpeed { get; private set; }

        public List<string> ExposureSpeeds { get; } = [
            "Power Save",
            "Normal",
        ];

        public ushort ExposureSpeed {
            get => (ushort)profileService.ActiveProfile.CameraSettings.AtikExposureSpeed;
            set {
                CameraSpecificOptionSetData(_cameraP, AtikCameraSpecificOptions.ID_ExposureSpeed, BitConverter.GetBytes(value));
                profileService.ActiveProfile.CameraSettings.AtikExposureSpeed = value;
                RaisePropertyChanged();
            }
        }

        public bool HasGainPresets { get; private set; }
        public List<string> GainPresets => presetInformation.Select(preset => preset.Name).ToList();

        private bool CanCustomGain { get; set; }
        private bool CanCustomOffset { get; set; }

        public ushort GainPreset {
            get => (ushort)profileService.ActiveProfile.CameraSettings.AtikGainPreset;
            set {
                SetPresetMode(presetInformation[value].Id);
                profileService.ActiveProfile.CameraSettings.AtikGainPreset = value;

                if (value == 0) { // Custom gain/offset
                    Gain = profileService.ActiveProfile.CameraSettings.Gain ?? presetInformation[value].Gain;
                    Offset = profileService.ActiveProfile.CameraSettings.Offset ?? presetInformation[value].Offset;
                } else { // Preset gain/offset
                    Gain = presetInformation[value].Gain;
                    Offset = presetInformation[value].Offset;
                }

                RaisePropertyChanged();
            }
        }

        public int WindowHeaterPowerLevel {
            get => (int)profileService.ActiveProfile.CameraSettings.AtikWindowHeaterPowerLevel;
            set {
                if (value != profileService.ActiveProfile.CameraSettings.AtikWindowHeaterPowerLevel) {
                    if (DewHeaterOn) {
                        SetWindowHeaterPower(value);
                    }
                    profileService.ActiveProfile.CameraSettings.AtikWindowHeaterPowerLevel = value;
                    RaisePropertyChanged();
                }
            }

        }

        private void SetWindowHeaterPower(int level) {
            AtikCameraDll.SetWindowHeaterPower(_cameraP, level);
        }

        public bool HasSetupDialog => false;

        public string Id { get; }

        public string Name { get; private set; }
        public string DisplayName => $"{Name} ({(Id.Length > 8 ? Id[^8..] : Id)})";

        private static string CleanedUpString(char[] values) {
            return string.Join("", values.Take(Array.IndexOf(values, '\0')));
        }

        private static bool HasBit(int flags, int bit) {
            return (flags & bit) == bit;
        }

        private static ushort OptionBytesToUShort(byte[] data, int byte1, int byte2) {
            return BitConverter.ToUInt16([data[byte1], data[byte2]], 0);
        }

        public void AbortExposure() {
            AtikCameraDll.AbortExposure(_cameraP);
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                var success = false;
                fastModeCallback = ServiceFastModeCb;

                try {
                    fastExposureSpeedTCS?.TrySetCanceled();
                    fastExposureSpeedTCS = null;

                    _cameraP = AtikCameraDll.Connect(_cameraId);
                    _info = GetCameraProperties(_cameraP);
                    var coolingFlags = GetCoolingFlags(_cameraP);
                    var colorInfo = GetColorInformation(_cameraP);

                    Logger.Debug($"CameraFlags = 0x{Info.cameraflags:X}");
                    Logger.Debug($"CoolingFlags = 0x{coolingFlags:X}");

                    // Determine if the camera has various features
                    HasShutter = HasBit(Info.cameraflags, (int)ArtemisPropertiesCameraFlags.HasShutter);
                    HasDewHeater = HasBit(Info.cameraflags, (int)ArtemisPropertiesCameraFlags.HasWindowHeater);

                    CanGetCoolerPower = HasBit(coolingFlags, (int)ArtemisCoolingInfoFlags.PowerLeveLControl) ||
                                        HasBit(coolingFlags, (int)ArtemisCoolingInfoFlags.SetpointControl);

                    CanSetTemperature = HasBit(coolingFlags, (int)ArtemisCoolingInfoFlags.SetpointControl);

                    HasExposureSpeed = HasCameraSpecificOption(_cameraP, AtikCameraSpecificOptions.ID_ExposureSpeed);

                    if (HasExposureSpeed) {
                        ExposureSpeed = ExposureSpeed;

                        if (AtikCameraDll.HasFastMode(_cameraP)) {
                            Logger.Debug($"Setting FastMode callback: {fastModeCallback}");

                            AtikCameraDll.SetFastCallbackEx(_cameraP, fastModeCallback);
                            ExposureSpeeds.Add("Fast");
                        }
                    }

                    // Cameras with shutters have a longer minimum exposure time. ref: Atik ASCOM driver
                    ExposureMin = HasShutter ? 0.2 : 0.001;

                    // "Preview" is Atik's term for "Fast" readout mode
                    if (HasBit(Info.cameraflags, (int)ArtemisPropertiesCameraFlags.Preview)) {
                        ReadoutModes.Add("Fast");
                    }

                    // Check if camera supports gain presets. This pertains to the CMOS models
                    HasGainPresets = HasCameraSpecificOption(_cameraP, AtikCameraSpecificOptions.ID_GOPresetMode);

                    // Build a list of presets and their parameters. The items in the GainPresets list must matain their order because they are referenced by index elsewhere
                    if (HasGainPresets) {
                        AtikCameraSpecificOptions presetType;
                        presetInformation = [];

                        CanCustomGain = HasCameraSpecificOption(_cameraP, AtikCameraSpecificOptions.ID_GOCustomGain);
                        CanCustomOffset = HasCameraSpecificOption(_cameraP, AtikCameraSpecificOptions.ID_GOCustomOffset);

                        // Get custom gain/offset limits and setting
                        if (CanCustomGain && CanCustomOffset) {
                            var gainData = new byte[6];
                            var offsetData = new byte[6];

                            CameraSpecificOptionGetData(_cameraP, AtikCameraSpecificOptions.ID_GOCustomGain, ref gainData);
                            GainMin = OptionBytesToUShort(gainData, 0, 1);
                            GainMax = OptionBytesToUShort(gainData, 2, 3);

                            CameraSpecificOptionGetData(_cameraP, AtikCameraSpecificOptions.ID_GOCustomOffset, ref offsetData);
                            OffsetMin = OptionBytesToUShort(offsetData, 0, 1);
                            OffsetMax = OptionBytesToUShort(offsetData, 2, 3);

                            presetInformation.Add(new PresetInformation() {
                                Id = AtikCameraSpecificOptions.ID_GOCustomGain,
                                Name = "Custom",
                                Gain = OptionBytesToUShort(gainData, 4, 5),
                                Offset = OptionBytesToUShort(offsetData, 4, 5),
                            });
                        }

                        //
                        // Get "Low", "Medium", and "High" preset gain/offset values
                        //
                        presetType = AtikCameraSpecificOptions.ID_GOPresetLow;
                        if (HasCameraSpecificOption(_cameraP, presetType)) {
                            var data = new byte[5];

                            CameraSpecificOptionGetData(_cameraP, presetType, ref data);

                            presetInformation.Add(new PresetInformation() {
                                Id = presetType,
                                Name = "Low",
                                Gain = OptionBytesToUShort(data, 1, 2),
                                Offset = OptionBytesToUShort(data, 3, 4),
                            });
                        }

                        presetType = AtikCameraSpecificOptions.ID_GOPresetMed;
                        if (HasCameraSpecificOption(_cameraP, presetType)) {
                            var data = new byte[5];

                            CameraSpecificOptionGetData(_cameraP, presetType, ref data);

                            presetInformation.Add(new PresetInformation() {
                                Id = presetType,
                                Name = "Medium",
                                Gain = OptionBytesToUShort(data, 1, 2),
                                Offset = OptionBytesToUShort(data, 3, 4),
                            });
                        }

                        presetType = AtikCameraSpecificOptions.ID_GOPresetHigh;
                        if (HasCameraSpecificOption(_cameraP, presetType)) {
                            var data = new byte[5];

                            CameraSpecificOptionGetData(_cameraP, presetType, ref data);

                            presetInformation.Add(new PresetInformation() {
                                Id = presetType,
                                Name = "High",
                                Gain = OptionBytesToUShort(data, 1, 2),
                                Offset = OptionBytesToUShort(data, 3, 4),
                            });
                        }

                        foreach (var preset in presetInformation) {
                            Logger.Debug($"Preset \"{preset.Id}\": Name={preset.Name}, Gain={preset.Gain}, Offset={preset.Offset}");
                        }

                        CanSetGain = CanSetOffset = CanGetGain = true;
                        GainPreset =  profileService.ActiveProfile.CameraSettings.AtikGainPreset ?? 0;
                    }

                    // Set camera to send 16bit data if it supports setting BitsSendMode
                    if (HasCameraSpecificOption(_cameraP, AtikCameraSpecificOptions.ID_BitSendMode)) {
                        Logger.Debug($"Setting BitSendMode to 16bit");
                        CameraSpecificOptionSetData(_cameraP, AtikCameraSpecificOptions.ID_BitSendMode, BitConverter.GetBytes(0));
                    }

                    if (CanSetTemperature) {
                        TemperatureSetPoint = 20;
                    }

                    SensorType = colorInfo.SensorType;

                    if (SensorType != SensorType.Monochrome) {
                        BayerOffsetX = colorInfo.BayerOffsetX;
                        BayerOffsetY = colorInfo.BayerOffsetY;
                    }

                    RaiseAllPropertiesChanged();
                    success = true;
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowError(e.Message);
                }

                return success;
            });
        }

        public void Disconnect() {
            try {
                ArtemisCoolerWarmUp(_cameraP);
            } catch (Exception) { }
            AtikCameraDll.Disconnect(_cameraP);
            _cameraP = IntPtr.Zero;
            _binningModes = null;
            RaisePropertyChanged(nameof(Connected));
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                while (!ImageReady(_cameraP)) {
                    await Task.Delay(100, token);
                }
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            IExposureData exposureData = null;

            if (AtikCameraDll.HasFastMode(_cameraP) && ExposureSpeed == 2 && fastExposureSpeedTCS?.Task.IsCanceled != false) { return null; }

            using (MyStopWatch.Measure("ATIK Download")) {
                return await Task.Run<IExposureData>(async () => {

                    if (AtikCameraDll.HasFastMode(_cameraP) && ExposureSpeed == 2) {
                        using (token.Register(() => fastExposureSpeedTCS.TrySetCanceled())) {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                            using (cts.Token.Register(() => { Logger.Error($"{Category} - No Image Callback Event received"); fastExposureSpeedTCS.TrySetResult(true); })) {
                                var imageReady = await fastExposureSpeedTCS.Task;
                                exposureData = GetExposureData();
                                AtikCameraDll.StopExposure(_cameraP);
                            }
                        }
                    } else {
                        try {
                            while (!ImageReady(_cameraP)) {
                                await Task.Delay(100, token);
                            }

                            exposureData = AtikCameraDll.DownloadExposure(_cameraP, BitDepth, SensorType != SensorType.Monochrome, exposureDataFactory);
                        } catch (OperationCanceledException) {
                        } catch (Exception ex) {
                            Logger.Error(ex);
                            Notification.ShowError(ex.Message);
                        }
                    }

                    exposureData.MetaData.FromCamera(this);

                    if (HasGainPresets) {
                        exposureData.MetaData.GenericHeaders.Add(new StringMetaDataHeader("GAINPSET", GainPresets[GainPreset], "Atik gain preset"));
                    }

                    if (HasExposureSpeed) {
                        exposureData.MetaData.GenericHeaders.Add(new StringMetaDataHeader("EXPSPEED", ExposureSpeeds[ExposureSpeed], "Atik exposure speed setting"));
                    }

                    return exposureData;
                });
            }
        }

        public void SetBinning(short x, short y) {
            AtikCameraDll.SetBinning(_cameraP, x, y);
        }

        public void SetupDialog() {
        }

        public void StartExposure(CaptureSequence sequence) {
            do {
                Thread.Sleep(100);
            } while (AtikCameraDll.CameraState(_cameraP) != ArtemisCameraStateEnum.CAMERA_IDLE);

            if (EnableSubSample && CanSubSample) {
                SetSubFrame(_cameraP, SubSampleX, SubSampleY, SubSampleWidth, SubSampleHeight);
            } else {
                SetSubFrame(_cameraP, 0, 0, CameraXSize, CameraYSize);
            }

            var isLightFrame = !(sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                  sequence.ImageType == CaptureSequence.ImageTypes.BIAS);

            ReadoutMode = readoutModes.Count > 1 && sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT
                ? ReadoutModeForSnapImages
                : ReadoutModeForNormalImages;

            SetAmplifierSwitched(_cameraP, sequence.ExposureTime > 2.5);

            if (HasShutter) {
                SetDarkMode(_cameraP, !isLightFrame);
            }

            if (AtikCameraDll.HasFastMode(_cameraP) && ExposureSpeed == 2) {
                fastExposureSpeedTCS?.TrySetCanceled();
                fastExposureSpeedTCS = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Logger.Debug($"{Category} - created new downloadExposure Task with Id {fastExposureSpeedTCS.Task.Id}");

                AtikCameraDll.StartFastExposure(_cameraP, (int)sequence.ExposureTime * 1000);
            } else {
                AtikCameraDll.StartExposure(_cameraP, sequence.ExposureTime);
            }
        }

        private AtikCameraSpecificOptions GetPresetMode() {
            var presetMode = new byte[2];
            CameraSpecificOptionGetData(_cameraP, AtikCameraSpecificOptions.ID_GOPresetMode, ref presetMode);
            var atikMode = (AtikCameraSpecificOptions)BitConverter.ToUInt16(presetMode, 0);
            Logger.Debug($"PresetMode = {atikMode}");

            return atikMode;
        }

        private void SetPresetMode(AtikCameraSpecificOptions presetMode) {
            CameraSpecificOptionSetData(_cameraP, AtikCameraSpecificOptions.ID_GOPresetMode, BitConverter.GetBytes((ushort)presetMode));
        }

        private ushort GetGain() {
            var presetMode = GainPreset;
            ushort gain;

            if (presetMode == 0) {
                var presetValue = new byte[6];
                CameraSpecificOptionGetData(_cameraP, AtikCameraSpecificOptions.ID_GOCustomGain, ref presetValue);
                gain = OptionBytesToUShort(presetValue, 4, 5);

                return gain;
            } else {
                var presetValue = new byte[5];
                CameraSpecificOptionGetData(_cameraP, presetInformation[presetMode].Id, ref presetValue);
                gain = OptionBytesToUShort(presetValue, 1, 2);
            }

            Logger.Debug($"{presetMode} gain = {gain}");
            return gain;
        }

        private void SetGain(int gain) {
            CameraSpecificOptionSetData(_cameraP, AtikCameraSpecificOptions.ID_GOCustomGain, BitConverter.GetBytes((ushort)gain));
        }

        private ushort GetOffset() {
            var presetMode = GainPreset;
            ushort offset;

            if (presetMode == 0) {
                var presetValue = new byte[6];
                CameraSpecificOptionGetData(_cameraP, AtikCameraSpecificOptions.ID_GOCustomOffset, ref presetValue);
                offset = OptionBytesToUShort(presetValue, 4, 5);
            } else {
                var presetValue = new byte[5];
                CameraSpecificOptionGetData(_cameraP, presetInformation[presetMode].Id, ref presetValue);
                offset = OptionBytesToUShort(presetValue, 1, 2);
            }

            Logger.Debug($"{presetMode} offset = {offset}");
            return offset;
        }

        private void SetOffset(int offset) {
            CameraSpecificOptionSetData(_cameraP, AtikCameraSpecificOptions.ID_GOCustomOffset, BitConverter.GetBytes((ushort)offset));
        }

        public void StopExposure() {
            AtikCameraDll.StopExposure(_cameraP);
            fastExposureSpeedTCS?.TrySetCanceled();
        }

        private void ServiceFastModeCb(IntPtr _cameraP, int x, int y, int w, int h, int binx, int biny, IntPtr imageBuffer, IntPtr info) {
            Logger.Debug($"{Category} - FastModeCb: bufferLen={IntPtr.Size}, origin={x},{y}, width={w}, height={h}, binX={binx}, binY={biny}");

            // Do this no-op get to clear the camera's internal buffer?
            AtikCameraDll.ArtemisGetImageData(_cameraP, out var _, out var _, out var _, out var _, out var _, out var _);

            var ptr = AtikCameraDll.ArtemisImageBuffer(_cameraP);

            var cameraDataToManaged = new CameraDataToManaged(ptr, w, h, BitDepth, bitScaling: false);
            var arr = cameraDataToManaged.GetData();

            SetExposureData(exposureDataFactory.CreateImageArrayExposureData(
                    input: arr,
                    width: w,
                    height: h,
                    bitDepth: BitDepth,
                    isBayered: SensorType == SensorType.RGGB,
                    metaData: new ImageMetaData()));

            fastExposureSpeedTCS.TrySetResult(true);
        }

        private IExposureData GetExposureData() {
            lock (exposureDataLock) {
                Logger.Debug($"{Category} - GetExposureData");
                return exposureData;
            }
        }

        private void SetExposureData(IExposureData data) {
            lock (exposureDataLock) {
                exposureData = data;
            }
        }

        public void StartLiveView(CaptureSequence sequence) {
            throw new NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StopLiveView() {
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

        public void UpdateSubSampleArea() {
            throw new NotImplementedException();
        }

        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;
        public bool LiveViewEnabled { get => false; set => throw new NotImplementedException(); }
        public int BatteryLevel => -1;
        public bool HasBattery => false;
    }
}