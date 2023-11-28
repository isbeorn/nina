#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.SDK.CameraSDKs.AtikSDK;
using NINA.Equipment.Utility;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera {

    public class AtikCamera : BaseINPC, ICamera {

        public AtikCamera(int id, IProfileService profileService, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            _cameraId = id;
            Name = AtikCameraDll.GetDeviceName(_cameraId);
        }

        private readonly int _cameraId;
        private IntPtr _cameraP = IntPtr.Zero;

        public string Category => "Atik";

        private AtikCameraDll.ArtemisPropertiesStruct _info;

        private AtikCameraDll.ArtemisPropertiesStruct Info => _info;

        private bool _hasShutter = false;

        public bool HasShutter {
            get => _hasShutter;
            private set {
                _hasShutter = value;
                Logger.Trace($"HasShutter = {_hasShutter}");
                RaisePropertyChanged();
            }
        }

        public bool Connected => _cameraP != IntPtr.Zero && AtikCameraDll.IsConnected(_cameraP);

        public double Temperature => AtikCameraDll.GetTemperature(_cameraP);

        public bool CanShowLiveView => false;

        private double _temperature;

        public double TemperatureSetPoint {
            get {
                _temperature = AtikCameraDll.GetSetpoint(_cameraP);
                return _temperature;
            }

            set {
                if (CanSetTemperature) {
                    _temperature = value;
                    if (CoolerOn) {
                        AtikCameraDll.SetCooling(_cameraP, _temperature);
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
                                AtikCameraDll.SetWarmup(_cameraP);
                            } else {
                                AtikCameraDll.SetCooling(_cameraP, _temperature);
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
                AtikCameraDll.GetBinning(_cameraP, out var x, out var y);
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
                AtikCameraDll.GetBinning(_cameraP, out var x, out var y);
                return (short)y;
            }

            set {
                if (value < MaxBinY) {
                    AtikCameraDll.SetBinning(_cameraP, value, value);
                    RaisePropertyChanged();
                }
            }
        }

        public string Description => CleanedUpString(Info.Manufacturer) + " " + Name + " (SerialNo: " + AtikCameraDll.GetSerialNumber(_cameraP) + ")";

        public string DriverInfo => AtikCameraDll.DriverName;

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
                AtikCameraDll.GetMaxBinning(_cameraP, out var x, out var y);
                return (short)x > 10 ? (short)10 : (short)x;
            }
        }

        public short MaxBinY {
            get {
                AtikCameraDll.GetMaxBinning(_cameraP, out var x, out var y);
                return (short)y > 10 ? (short)10 : (short)y;
            }
        }

        public double PixelSizeX => Info.PixelMicronsX;

        public double PixelSizeY => Info.PixelMicronsY;

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
                    case AtikCameraDll.ArtemisCameraStateEnum.CAMERA_EXPOSING:
                        state = CameraStates.Exposing;
                        break;

                    case AtikCameraDll.ArtemisCameraStateEnum.CAMERA_WAITING:
                    case AtikCameraDll.ArtemisCameraStateEnum.CAMERA_FLUSHING:
                        state = CameraStates.Waiting;
                        break;

                    case AtikCameraDll.ArtemisCameraStateEnum.CAMERA_DOWNLOADING:
                        state = CameraStates.Download;
                        break;

                    case AtikCameraDll.ArtemisCameraStateEnum.CAMERA_ERROR:
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

        public int OffsetMin => 0;

        public int Offset {
            get {
                if (!CanSetOffset) {
                    return -1;
                }

                var offset = new byte[6];
                AtikCameraDll.CameraSpecificOptionGetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOCustomOffset, ref offset);
                return (offset[5] << 8) + offset[4];
            }
            set {
                var offset = new byte[2];
                offset[0] = (byte)value;
                AtikCameraDll.CameraSpecificOptionSetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOCustomOffset, offset);
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
                var gain = new byte[6];
                AtikCameraDll.CameraSpecificOptionGetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOCustomGain, ref gain);
                return (gain[5] << 8) + gain[4];
            }
            set {
                var gain = new byte[2];
                gain[0] = (byte)value;
                AtikCameraDll.CameraSpecificOptionSetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOCustomGain, gain);
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
                        AtikCameraDll.SetArtemisPreview(_cameraP, true);
                    } else {
                        AtikCameraDll.SetArtemisPreview(_cameraP, false);
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

        public List<string> ExposureSpeeds { get; } = new List<string>() {
            "Power Save",
            "Normal",
        };

        public ushort ExposureSpeed {
            get => (ushort)profileService.ActiveProfile.CameraSettings.AtikExposureSpeed;
            set {
                var data = new byte[2] { (byte)value, 0 };
                AtikCameraDll.CameraSpecificOptionSetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_ExposureSpeed, data);
                profileService.ActiveProfile.CameraSettings.AtikExposureSpeed = value;
                RaisePropertyChanged();
            }
        }

        public bool HasGainPresets { get; private set; }

        private bool CanCustomGain { get; set; }
        private bool CanCustomOffset { get; set; }

        public List<string> GainPresets { get; private set; } = new List<string>();

        public ushort GainPreset {
            get => (ushort)profileService.ActiveProfile.CameraSettings.AtikGainPreset;
            set {
                var data = new byte[2] { (byte)value, 0 };
                AtikCameraDll.CameraSpecificOptionSetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOPresetMode, data);
                profileService.ActiveProfile.CameraSettings.AtikGainPreset = value;

                // Custom gain/offset
                if (value == 0) {
                    Gain = (int)profileService.ActiveProfile.CameraSettings.Gain;
                    Offset = (int)profileService.ActiveProfile.CameraSettings.Offset;
                    CanSetGain = CanGetGain = CanSetOffset = true;
                } else {
                    CanSetGain = CanGetGain = CanSetOffset = false;
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

        public string Id => Name;

        public string Name { get; private set; }

        private string CleanedUpString(char[] values) {
            return string.Join("", values.Take(Array.IndexOf(values, '\0')));
        }

        private static bool HasBit(int flags, int bit) {
            return (flags & bit) == bit;
        }

        public void AbortExposure() {
            AtikCameraDll.AbortExposure(_cameraP);
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                var success = false;

                try {
                    _cameraP = AtikCameraDll.Connect(_cameraId);
                    _info = AtikCameraDll.GetCameraProperties(_cameraP);
                    var coolingFlags = AtikCameraDll.GetCoolingFlags(_cameraP);
                    var colorInfo = AtikCameraDll.GetColorInformation(_cameraP);

                    Logger.Trace($"CameraFlags = 0x{Info.cameraflags:X}");
                    Logger.Trace($"CoolingFlags = 0x{coolingFlags:X}");

                    HasShutter = HasBit(Info.cameraflags, (int)AtikCameraDll.ArtemisPropertiesCameraFlags.HasShutter);

                    HasDewHeater = HasBit(Info.cameraflags, (int)AtikCameraDll.ArtemisPropertiesCameraFlags.HasWindowHeater);

                    CanGetCoolerPower = HasBit(coolingFlags, (int)AtikCameraDll.ArtemisCoolingInfoFlags.PowerLeveLControl) ||
                                        HasBit(coolingFlags, (int)AtikCameraDll.ArtemisCoolingInfoFlags.SetpointControl);

                    CanSetTemperature = HasBit(coolingFlags, (int)AtikCameraDll.ArtemisCoolingInfoFlags.SetpointControl);

                    HasExposureSpeed = AtikCameraDll.HasCameraSpecificOption(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_ExposureSpeed);

                    if (HasExposureSpeed) {
                        ExposureSpeed = ExposureSpeed;
                    }

                    ExposureMin = HasShutter ? 0.2 : 0.001;

                    if (HasBit(Info.cameraflags, (int)AtikCameraDll.ArtemisPropertiesCameraFlags.Preview)) {
                        ReadoutModes.Add("Fast");
                    }

                    HasGainPresets = AtikCameraDll.HasCameraSpecificOption(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOPresetMode);

                    // The items in the GainPresets list must have exact ordering.
                    if (HasGainPresets) {
                        CanCustomGain = AtikCameraDll.HasCameraSpecificOption(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOCustomGain);
                        CanCustomOffset = AtikCameraDll.HasCameraSpecificOption(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOCustomOffset);

                        if (CanCustomGain && CanCustomOffset) {
                            var data = new byte[6];

                            AtikCameraDll.CameraSpecificOptionGetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOCustomGain, ref data);
                            GainMin = 0;
                            GainMax = (data[3] << 8) + data[2];

                            AtikCameraDll.CameraSpecificOptionGetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOCustomOffset, ref data);
                            OffsetMax = (data[3] << 8) + data[2];

                            GainPresets.Add("Custom");
                        }

                        if (AtikCameraDll.HasCameraSpecificOption(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOPresetLow)) {
                            GainPresets.Add("Low");
                        }

                        if (AtikCameraDll.HasCameraSpecificOption(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOPresetMed)) {
                            GainPresets.Add("Medium");
                        }

                        if (AtikCameraDll.HasCameraSpecificOption(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_GOPresetHigh)) {
                            GainPresets.Add("High");
                        }

                        if (GainPreset < GainPresets.Count) {
                            GainPreset = GainPreset;
                        }
                    }

                    // Set camera to send 16bit data if it supports setting BitsSendMode
                    if (AtikCameraDll.HasCameraSpecificOption(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_BitSendMode)) {
                        var mode = new byte[2] { 0, 0 };

                        Logger.Trace($"Setting BitSendMode to 0x{BitConverter.ToString(mode).Replace("-", string.Empty)}");
                        AtikCameraDll.CameraSpecificOptionSetData(_cameraP, (ushort)AtikCameraDll.AtikCameraSpecificOptions.ID_BitSendMode, mode);
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
                AtikCameraDll.ArtemisCoolerWarmUp(_cameraP);
            } catch (Exception) { }
            AtikCameraDll.Disconnect(_cameraP);
            _cameraP = IntPtr.Zero;
            _binningModes = null;
            RaisePropertyChanged(nameof(Connected));
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                while (!AtikCameraDll.ImageReady(_cameraP)) {
                    await Task.Delay(100, token);
                }
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            using (MyStopWatch.Measure("ATIK Download")) {
                return await Task.Run<IExposureData>(async () => {
                    try {
                        while (!AtikCameraDll.ImageReady(_cameraP)) {
                            await Task.Delay(100, token);
                        }

                        var data = AtikCameraDll.DownloadExposure(_cameraP, BitDepth, SensorType != SensorType.Monochrome, exposureDataFactory);
                        data.MetaData.FromCamera(this);
                        return data;
                    } catch (OperationCanceledException) {
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    }
                    return null;
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
            } while (AtikCameraDll.CameraState(_cameraP) != AtikCameraDll.ArtemisCameraStateEnum.CAMERA_IDLE);

            if (EnableSubSample && CanSubSample) {
                AtikCameraDll.SetSubFrame(_cameraP, SubSampleX, SubSampleY, SubSampleWidth, SubSampleHeight);
            } else {
                AtikCameraDll.SetSubFrame(_cameraP, 0, 0, CameraXSize, CameraYSize);
            }

            var isLightFrame = !(sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                  sequence.ImageType == CaptureSequence.ImageTypes.BIAS ||
                  sequence.ImageType == CaptureSequence.ImageTypes.DARKFLAT);

            ReadoutMode = readoutModes.Count > 1 && sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT
                ? ReadoutModeForSnapImages
                : ReadoutModeForNormalImages;

            AtikCameraDll.SetAmplifierSwitched(_cameraP, sequence.ExposureTime > 2.5);

            if (HasShutter) {
                AtikCameraDll.SetDarkMode(_cameraP, !isLightFrame);
            }

            AtikCameraDll.StartExposure(_cameraP, sequence.ExposureTime);
        }

        public void StopExposure() {
            AtikCameraDll.StopExposure(_cameraP);
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

        private readonly IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;
        public bool LiveViewEnabled { get => false; set => throw new NotImplementedException(); }

        public int BatteryLevel => -1;

        public bool HasBattery => false;
    }
}