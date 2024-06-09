#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using ASCOM.Common.DeviceInterfaces;
using ASCOM.Com.DriverAccess;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SensorType = NINA.Core.Enum.SensorType;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Utility;
using NINA.Core.Locale;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.Image.ImageData;
using NINA.Equipment.Interfaces;
using NINA.Core.Enum;
using ASCOM.Common.Alpaca;
using ASCOM.Alpaca.Discovery;

namespace NINA.Equipment.Equipment.MyCamera {

    public class AscomCamera : AscomDevice<ICameraV3>, ICamera, IDisposable {

        public AscomCamera(string cameraId, string name, IProfileService profileService, IExposureDataFactory exposureDataFactory) : base(cameraId, name) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
        }
        public AscomCamera(ASCOM.Alpaca.Discovery.AscomDevice deviceMeta, IProfileService profileService, IExposureDataFactory exposureDataFactory) : base(deviceMeta) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
        }

        private IProfileService profileService;
        private readonly IExposureDataFactory exposureDataFactory;

        private void Initialize() {
            _hasCooler = true;
            CanSetGain = true;
            CanGetGain = true;
            _canGetGainMinMax = true;
            _hasLastExposureInfo = true;
            BinningModes = new AsyncObservableCollection<BinningMode>();
            for (short i = 1; i <= MaxBinX; i++) {
                if (CanAsymmetricBin) {
                    for (short j = 1; j <= MaxBinY; j++) {
                        BinningModes.Add(new BinningMode(i, j));
                    }
                } else {
                    BinningModes.Add(new BinningMode(i, i));
                }
            }

            Gains.Clear();
            try {
                var gains = device.Gains;
                int idx = 0;
                foreach (object o in device.Gains) {
                    if (o is string) {
                        // Per the ASCOM spec, if we have Gains, then they are names, not values.
                        // Add an index for each value and write the mapping to the log.
                        // TODO - Look at how to carry the names to the UI
                        // eg by adding a GainsPreset string list to ICamera.
                        // Making Gains a string has too many ripple effects with the 
                        // UI for a quick fix. 
                        Logger.Info($"Gain {idx} Mapped to {o as string}");
                        Gains.Add(idx++);
                    }
                }
            } catch (Exception) {
            }

            //Determine Offset Capabilities
            try {
                //Check if Offset is implemented at all in ICameraV3 Driver
                _ = device.Offset;
                CanSetOffset = true;
            } catch (ASCOM.NotImplementedException) {
                Logger.Trace("Offset is not implemented in this driver");
                CanSetOffset = false;
            } catch (ASCOM.InvalidOperationException) {
                Logger.Trace("Offset is not implemented in this driver");
                CanSetOffset = false;
            } catch (Exception) {
                Logger.Trace("Offset is not implemented in this driver");
                CanSetOffset = false;
            }

            if (CanSetOffset) {
                try {
                    //Check if offset mode is in value mode
                    _ = device.OffsetMin;
                    offsetValueMode = true;
                } catch (ASCOM.NotImplementedException) {
                    Logger.Trace("Offset operates in index mode");
                    offsetValueMode = false;
                }

                if (!offsetValueMode) {
                    try {
                        //Check when mode is likely to be index mode if the offsets property is implemented
                        var arrayListOffsets = device.Offsets;

                        //Map offsets to integer values
                        offsets.Clear();
                        try {
                            var idx = 0;
                            foreach (object o in arrayListOffsets) {
                                if (o.GetType() == typeof(string)) {
                                    Logger.Info($"Offset {idx} Mapped to {o as string}");
                                    offsets.Add(idx++);
                                }
                            }
                        } catch (Exception) {
                        }
                    } catch (ASCOM.NotImplementedException) {
                        Logger.Error("Offset is implemented, but neither OffsetMin/Max nor Offsets are implemented.");
                        CanSetOffset = false;
                    }
                }
            }
        }

        public bool Create32BitData {
            get => false; //profileService.ActiveProfile.CameraSettings.ASCOMCreate32BitData;
            set {
                //profileService.ActiveProfile.CameraSettings.ASCOMCreate32BitData = value;
                //RaisePropertyChanged();
            }
        }

        public bool CanSubSample => true;

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        public short BayerOffsetX => GetProperty<short>(nameof(Camera.BayerOffsetX), 0);

        public short BayerOffsetY => GetProperty<short>(nameof(Camera.BayerOffsetY), 0);

        public short BinX {
            get => GetProperty<short>(nameof(Camera.BinX), 1);
            set => SetProperty(nameof(Camera.BinX), value);
        }

        public short BinY {
            get => GetProperty<short>(nameof(Camera.BinY), 1);
            set => SetProperty(nameof(Camera.BinY), value);
        }

        public Core.Enum.CameraStates CameraState => GetProperty<Core.Enum.CameraStates>(nameof(Camera.CameraState), 0);

        public int CameraXSize => GetProperty(nameof(Camera.CameraXSize), -1);

        public int CameraYSize => GetProperty(nameof(Camera.CameraYSize), -1);

        public bool CanAbortExposure => GetProperty(nameof(Camera.CanAbortExposure), false);

        public bool CanShowLiveView => false;

        public bool CanAsymmetricBin => GetProperty(nameof(Camera.CanAsymmetricBin), false);

        public bool CanFastReadout => GetProperty(nameof(Camera.CanFastReadout), false);

        public bool CanGetCoolerPower => GetProperty(nameof(Camera.CanGetCoolerPower), false);

        public bool CanPulseGuide => GetProperty(nameof(Camera.CanPulseGuide), false);

        public bool CanSetTemperature => GetProperty(nameof(Camera.CanSetCCDTemperature), false);

        public bool CanStopExposure => GetProperty(nameof(Camera.CanStopExposure), false);

        public double Temperature => GetProperty(nameof(Camera.CCDTemperature), double.NaN);

        private bool _hasCooler;

        public bool CoolerOn {
            get {
                bool val = false;
                try {
                    if (Connected && _hasCooler) {
                        val = device.CoolerOn;
                    }
                } catch (Exception) {
                    _hasCooler = false;
                }
                return val;
            }
            set {
                try {
                    if (Connected && _hasCooler) {
                        device.CoolerOn = value;
                        RaisePropertyChanged();
                    }
                } catch (Exception) {
                    _hasCooler = false;
                }
            }
        }

        public double CoolerPower {
            get {
                if (CanGetCoolerPower) {
                    return GetProperty(nameof(Camera.CoolerPower), -1d);
                }
                return -1;
            }
        }

        public bool HasDewHeater => false;

        public bool DewHeaterOn {
            get => false;
            set {
            }
        }

        public double ElectronsPerADU => GetProperty(nameof(Camera.ElectronsPerADU), double.NaN);

        public double ExposureMax => GetProperty(nameof(Camera.ExposureMax), double.NaN);

        public double ExposureMin => GetProperty(nameof(Camera.ExposureMin), double.NaN);

        public double ExposureResolution => GetProperty(nameof(Camera.ExposureResolution), double.NaN);

        public bool FastReadout {
            get {
                bool val = false;
                if (CanFastReadout) {
                    return GetProperty(nameof(Camera.FastReadout), false);
                }
                return val;
            }
            set {
                if (CanFastReadout) {
                    SetProperty(nameof(Camera.FastReadout), value);
                }
            }
        }

        public double FullWellCapacity => GetProperty(nameof(Camera.FullWellCapacity), -1d);

        private bool _canGetGain;

        public bool CanGetGain {
            get => _canGetGain;
            set {
                _canGetGain = value;
                RaisePropertyChanged();
            }
        }

        private bool _canSetGain;

        public bool CanSetGain {
            get => _canSetGain;
            set {
                _canSetGain = value;
                RaisePropertyChanged();
            }
        }

        public int Gain {
            get {
                int val = -1;
                if (Connected && CanGetGain) {
                    try {
                        if (Gains.Count > 0) {
                            val = (int)Gains[device.Gain];
                        } else {
                            val = device.Gain;
                        }
                    } catch (ASCOM.NotImplementedException) {
                        CanGetGain = false;
                        Logger.Info("ASCOM - Driver does not implement Gain GET");
                    }
                }
                return val;
            }
            set {
                if (Connected && CanSetGain) {
                    try {
                        if (Gains.Count > 0) {
                            short idx = (short)Gains.IndexOf(value);
                            if (idx >= 0) {
                                device.Gain = idx;
                            }
                        } else {
                            device.Gain = (short)value;
                        }
                    } catch (ASCOM.NotImplementedException) {
                        CanSetGain = false;
                        Logger.Info("ASCOM - Driver does not implement Gain SET");
                    } catch (InvalidValueException ex) {
                        Logger.Error(ex.Message);
                        Notification.ShowExternalWarning(ex.Message, Loc.Instance["LblASCOMDriverError"]);
                    } catch (Exception) {
                        CanSetGain = false;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public IList<string> ReadoutModes {
            get {
                IList<string> readoutModes = new List<string>();

                if (!CanFastReadout && ReadoutModesArrayList.Count > 1) {
                    foreach (string mode in ReadoutModesArrayList) {
                        readoutModes.Add(mode);
                    }
                } else if (CanFastReadout) {
                    readoutModes.Add(Loc.Instance["LblNormal"]);
                    readoutModes.Add(Loc.Instance["LblFast"]);
                } else {
                    readoutModes.Add("Default");
                }

                return readoutModes;
            }
        }

        private short readoutModeForSnapImages;

        public short ReadoutModeForSnapImages {
            get => readoutModeForSnapImages;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    readoutModeForSnapImages = value;
                } else {
                    readoutModeForSnapImages = 0;
                }

                RaisePropertyChanged();
            }
        }

        private short readoutModeForNormalImages;

        public short ReadoutModeForNormalImages {
            get => readoutModeForNormalImages;
            set {
                if (value >= 0 && value < ReadoutModes.Count) {
                    readoutModeForNormalImages = value;
                } else {
                    readoutModeForNormalImages = 0;
                }

                RaisePropertyChanged();
            }
        }

        private bool _canGetGainMinMax;

        public int GainMax {
            get {
                int val = -1;
                if (Connected) {
                    if (_canGetGainMinMax) {
                        try {
                            val = device.GainMax;
                        } catch (ASCOM.NotImplementedException) {
                            _canGetGainMinMax = false;
                        } catch (ASCOM.InvalidOperationException) {
                            _canGetGainMinMax = false;
                        }
                    }

                    if (!_canGetGainMinMax) {
                        if (this.Gains.Count > 0) {
                            val = Gains.Aggregate((l, r) => l > r ? l : r);
                        }
                    }
                }
                return val;
            }
        }

        public int GainMin {
            get {
                int val = -1;
                if (Connected) {
                    if (_canGetGainMinMax) {
                        try {
                            val = device.GainMin;
                        } catch (ASCOM.NotImplementedException) {
                            _canGetGainMinMax = false;                        
                        } catch (ASCOM.InvalidOperationException) {
                            _canGetGainMinMax = false;
                        }
                    }

                    if (!_canGetGainMinMax) {
                        if (this.Gains.Count > 0) {
                            val = Gains.Aggregate((l, r) => l < r ? l : r);
                        }
                    }
                }
                return val;
            }
        }

        private IList<int> _gains;

        public IList<int> Gains {
            get {
                if (_gains == null) {
                    _gains = new List<int>();
                }
                return _gains;
            }
        }

        public bool HasShutter => GetProperty(nameof(Camera.HasShutter), false);

        public double HeatSinkTemperature => GetProperty(nameof(Camera.HeatSinkTemperature), double.NaN);

        public object ImageArray => GetProperty<object>(nameof(Camera.ImageArray), null, cacheInterval: TimeSpan.Zero);

        public object ImageArrayVariant => GetProperty<object>(nameof(Camera.ImageArrayVariant), null, cacheInterval: TimeSpan.Zero);

        public bool ImageReady => GetProperty(nameof(Camera.ImageReady), false, cacheInterval: TimeSpan.Zero);

        public short InterfaceVersion => GetProperty<short>(nameof(Camera.InterfaceVersion), -1);

        public bool IsPulseGuiding => GetProperty(nameof(Camera.IsPulseGuiding), false);

        private bool _hasLastExposureInfo;

        public double LastExposureDuration {
            get {
                double val = -1;
                try {
                    if (Connected && _hasLastExposureInfo) {
                        val = device.LastExposureDuration;
                    }
                } catch (ASCOM.InvalidOperationException) {
                } catch (ASCOM.NotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }

        public string LastExposureStartTime {
            get {
                string val = string.Empty;
                try {
                    if (Connected && _hasLastExposureInfo) {
                        val = device.LastExposureStartTime;
                    }
                } catch (ASCOM.InvalidOperationException) {
                } catch (ASCOM.NotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }

        public int MaxADU => GetProperty(nameof(Camera.MaxADU), -1);

        public short MaxBinX => GetProperty<short>(nameof(Camera.MaxBinX), 1);

        public short MaxBinY => GetProperty<short>(nameof(Camera.MaxBinY), 1);

        public int NumX {
            get => GetProperty(nameof(Camera.NumX), -1);
            set {
                if (!profileService.ActiveProfile.CameraSettings.ASCOMAllowUnevenPixelDimension || SensorType != SensorType.Monochrome) {
                    if (value % 2 > 0) {
                        value--;
                    }
                }

                SetProperty(nameof(Camera.NumX), value);
            }
        }

        public int NumY {
            get => GetProperty(nameof(Camera.NumY), -1);
            set {
                if (!profileService.ActiveProfile.CameraSettings.ASCOMAllowUnevenPixelDimension || SensorType != SensorType.Monochrome) {
                    if (value % 2 > 0) {
                        value--;
                    }
                }
                SetProperty(nameof(Camera.NumY), value);
            }
        }

        public short PercentCompleted => GetProperty<short>(nameof(Camera.PercentCompleted), -1, cacheInterval: TimeSpan.Zero);

        public double PixelSizeX => GetProperty<double>(nameof(Camera.PixelSizeX), -1d);

        public double PixelSizeY => GetProperty<double>(nameof(Camera.PixelSizeY), -1d);

        public short ReadoutMode {
            get => GetProperty<short>(nameof(Camera.ReadoutMode), 0);
            set {
                if (Connected && (value != ReadoutMode) && (value < ReadoutModes.Count)) {
                    try {
                        device.ReadoutMode = value;
                    } catch (InvalidValueException ex) {
                        Logger.Error(ex);
                        Notification.ShowExternalError(ex.Message, Loc.Instance["LblASCOMDriverError"]);
                    } catch (ASCOM.NotImplementedException) {
                        ASCOMInteraction.LogComplianceIssue($"{nameof(ReadoutMode)} SET");
                    }
                }
            }
        }

        public IList<string> ReadoutModesArrayList {
            get {
                IList<string> val = new List<string>();
                if (!CanFastReadout) {
                    val = GetProperty(nameof(Camera.ReadoutModes), new List<string>());
                }
                return val;
            }
        }

        public string SensorName => GetProperty(nameof(Camera.SensorName), string.Empty);

        public SensorType SensorType {
            get {
                var deviceType = GetProperty(nameof(Camera.SensorType), ASCOM.Common.DeviceInterfaces.SensorType.Monochrome);

                SensorType type;
                switch (deviceType) {
                    case ASCOM.Common.DeviceInterfaces.SensorType.Monochrome:
                        type = SensorType.Monochrome;
                        break;

                    case ASCOM.Common.DeviceInterfaces.SensorType.Color:
                        type = SensorType.Color;
                        break;

                    case ASCOM.Common.DeviceInterfaces.SensorType.RGGB:
                        type = SensorType.RGGB;
                        break;

                    case ASCOM.Common.DeviceInterfaces.SensorType.CMYG:
                        type = SensorType.CMYG;
                        break;

                    case ASCOM.Common.DeviceInterfaces.SensorType.CMYG2:
                        type = SensorType.CMYG2;
                        break;

                    case ASCOM.Common.DeviceInterfaces.SensorType.LRGB:
                        type = SensorType.LRGB;
                        break;

                    default:
                        type = SensorType.Monochrome;
                        break;
                }

                return type;
            }
        }

        public double TemperatureSetPoint {
            get {
                double val = double.NaN;
                if (CanSetTemperature) {
                    val = GetProperty(nameof(Camera.SetCCDTemperature), double.NaN);
                }
                return val;
            }
            set {
                if (CanSetTemperature) {
                    SetProperty(nameof(Camera.SetCCDTemperature), value);
                }
            }
        }

        public int StartX {
            get => GetProperty(nameof(Camera.StartX), -1);
            set => SetProperty(nameof(Camera.StartX), value);
        }

        public int StartY {
            get => GetProperty(nameof(Camera.StartY), -1);
            set => SetProperty(nameof(Camera.StartY), value);
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; private set; }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                while (!ImageReady) {
                    await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), token);
                }
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            using (MyStopWatch.Measure("ASCOM Download")) {
                return await Task.Run(async () => {
                    try {
                        while (!ImageReady) {
                            await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), token);
                        }

                        var metaData = new ImageMetaData();
                        metaData.FromCamera(this);

                        return exposureDataFactory.CreateFlipped2DExposureData(
                            flipped2DArray: (Array)ImageArray,
                            bitDepth: BitDepth,
                            isBayered: SensorType != SensorType.Monochrome,
                            metaData: metaData);
                    } catch (OperationCanceledException) {
                    } catch (Exception ex) {
                        Notification.ShowExternalError(ex.Message, Loc.Instance["LblASCOMDriverError"]);
                        Logger.Error(ex);
                    }
                    return null;
                });
            }
        }

        public void StartExposure(CaptureSequence sequence) {
            if (EnableSubSample) {
                StartX = SubSampleX / BinX;
                StartY = SubSampleY / BinY;
                NumX = SubSampleWidth / BinX;
                NumY = SubSampleHeight / BinY;
            } else {
                StartX = 0;
                StartY = 0;
                NumX = CameraXSize / BinX;
                NumY = CameraYSize / BinY;
            }

            bool isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAPSHOT;

            if (CanFastReadout) {
                FastReadout = isSnap ? readoutModeForSnapImages != 0 : readoutModeForNormalImages != 0;
            } else {
                ReadoutMode =
                    isSnap
                        ? readoutModeForSnapImages
                        : readoutModeForNormalImages;
            }

            var isLightFrame = !(sequence.ImageType == CaptureSequence.ImageTypes.DARK ||
                              sequence.ImageType == CaptureSequence.ImageTypes.BIAS);

            device.StartExposure(sequence.ExposureTime, isLightFrame);
        }

        public void StopExposure() {
            if (CanStopExposure) {
                try {
                    device.StopExposure();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                }
            }
        }

        public void AbortExposure() {
            if (CanAbortExposure) {
                try {
                    device.AbortExposure();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                }
            }
        }

        public void SetBinning(short x, short y) {
            BinX = x;
            BinY = y;

            var newX = CameraXSize / x;
            var newY = CameraYSize / y;
            NumX = newX;
            NumY = newY;
        }

        private IList<int> offsets = new List<int>();
        private bool offsetValueMode = true;
        private bool canSetOffset = false;

        public bool CanSetOffset {
            get => canSetOffset;
            private set {
                canSetOffset = value;
                RaisePropertyChanged();
            }
        }

        public int OffsetMin {
            get {
                var offsetMin = 0;
                if (Connected && CanSetOffset) {
                    if (offsetValueMode) {
                        offsetMin = device.OffsetMin;
                    } else {
                        if (offsets.Count > 0) {
                            offsetMin = 0;
                        }
                    }
                }
                return offsetMin;
            }
        }

        public int OffsetMax {
            get {
                var offsetMax = 0;
                if (Connected && CanSetOffset) {
                    if (offsetValueMode) {
                        offsetMax = device.OffsetMax;
                    } else {
                        if (offsets.Count > 0) {
                            offsetMax = offsets.Count - 1;
                        }
                    }
                }
                return offsetMax;
            }
        }

        public bool CanSetUSBLimit => false;

        public int Offset {
            get {
                var offset = -1;
                if (Connected && CanSetOffset) {
                    if (offsetValueMode) {
                        offset = device.Offset;
                    } else {
                        offset = offsets[device.Offset];
                    }
                }
                return offset;
            }
            set {
                try {
                    if (Connected && CanSetOffset) {
                        if (offsetValueMode) {
                            if (value < OffsetMin) {
                                value = OffsetMin;
                            }
                            if (value > OffsetMax) {
                                value = OffsetMax;
                            }

                            device.Offset = value;
                        } else {
                            if (value < 0) {
                                value = 0;
                            }
                            if (value > offsets.Count - 1) {
                                value = offsets.Count - 1;
                            }
                            device.Offset = value;
                        }
                        RaisePropertyChanged();
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblASCOMDriverError"]);
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

        public void StartLiveView(CaptureSequence sequence) {
            throw new System.NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new System.NotImplementedException();
        }

        public void StopLiveView() {
            throw new System.NotImplementedException();
        }

        protected override Task PostConnect() {
            if(device.SensorType == ASCOM.Common.DeviceInterfaces.SensorType.Color) {
                Disconnect();
                throw new Exception(Loc.Instance["LblASCOMColorSensorTypeNotSupported"]);
            }
            Initialize();
            return Task.CompletedTask;
        }

        protected override ICameraV3 GetInstance() {
            if(deviceMeta == null) {
                return new Camera(this.Id);
            } else {
                return new ASCOM.Alpaca.Clients.AlpacaCamera(deviceMeta.ServiceType, deviceMeta.IpAddress, deviceMeta.IpPort, deviceMeta.AlpacaDeviceNumber, false, null);
            }
            
        }

        public bool LiveViewEnabled { get => false; set => throw new System.NotImplementedException(); }

        public int BatteryLevel => -1;

        public bool HasBattery => false;

        public int BitDepth => (int)profileService.ActiveProfile.CameraSettings.BitDepth;

        protected override string ConnectionLostMessage => Loc.Instance["LblCameraConnectionLost"];
    }
}