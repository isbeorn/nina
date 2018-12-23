#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

#endregion "copyright"

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    internal class AscomCamera : BaseINPC, ICamera, IDisposable {

        public AscomCamera(string cameraId, string name, IProfileService profileService) {
            this.profileService = profileService;
            Id = cameraId;
            Name = name;
        }

        private string _id;
        private IProfileService profileService;

        public string Id {
            get {
                return _id;
            }
            set {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private Camera _camera;

        private void init() {
            _hasBayerOffset = true;
            _hasCCDTemperature = true;
            _hasCooler = true;
            CanSetGain = true;
            CanGetGain = true;
            _canGetGainMinMax = true;
            _hasLastExposureInfo = true;
            _hasPercentCompleted = true;
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
                var gains = _camera.Gains;
                foreach (object o in _camera.Gains) {
                    if (o.GetType() == typeof(string)) {
                        var gain = Regex.Match(o.ToString(), @"\d+").Value;
                        Gains.Add(short.Parse(gain, CultureInfo.InvariantCulture));
                    }
                }
            } catch (Exception) {
            }
        }

        public bool CanSubSample {
            get {
                return true;
            }
        }

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        private bool _hasBayerOffset;

        public short BayerOffsetX {
            get {
                short offset = -1;
                try {
                    if (Connected && _hasBayerOffset) {
                        offset = _camera.BayerOffsetX;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasBayerOffset = false;
                }
                return offset;
            }
        }

        public short BayerOffsetY {
            get {
                short offset = -1;
                try {
                    if (Connected && _hasBayerOffset) {
                        offset = _camera.BayerOffsetY;
                    }
                } catch (PropertyNotImplementedException) {
                    _hasBayerOffset = false;
                }
                return offset;
            }
        }

        public short BinX {
            get {
                if (Connected) {
                    return _camera.BinX;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    try {
                        _camera.BinX = value;
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public short BinY {
            get {
                if (Connected) {
                    return _camera.BinY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    try {
                        _camera.BinY = value;
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public string CameraState {
            get {
                string state;
                try {
                    if (Connected) {
                        state = _camera.CameraState.ToString();
                    } else {
                        state = CameraStates.cameraIdle.ToString();
                    }
                } catch (NotConnectedException ex) {
                    Notification.ShowError(ex.Message);
                    state = CameraStates.cameraError.ToString();
                }
                return state;
            }
        }

        public int CameraXSize {
            get {
                int size = -1;
                if (Connected) {
                    size = _camera.CameraXSize;
                }
                return size;
            }
        }

        public int CameraYSize {
            get {
                int size = -1;
                if (Connected) {
                    size = _camera.CameraYSize;
                }
                return size;
            }
        }

        public bool CanAbortExposure {
            get {
                if (Connected) {
                    return _camera.CanAbortExposure;
                } else {
                    return false;
                }
            }
        }

        public bool CanShowLiveView {
            get {
                return false;
            }
        }

        public bool CanAsymmetricBin {
            get {
                if (Connected) {
                    return _camera.CanAsymmetricBin;
                } else {
                    return false;
                }
            }
        }

        public bool CanFastReadout {
            get {
                if (Connected) {
                    return _camera.CanFastReadout;
                } else {
                    return false;
                }
            }
        }

        public bool CanGetCoolerPower {
            get {
                if (Connected) {
                    return _camera.CanGetCoolerPower;
                } else {
                    return false;
                }
            }
        }

        public bool CanPulseGuide {
            get {
                if (Connected) {
                    return _camera.CanPulseGuide;
                } else {
                    return false;
                }
            }
        }

        public bool CanSetTemperature {
            get {
                if (Connected) {
                    return _camera.CanSetCCDTemperature;
                } else {
                    return false;
                }
            }
        }

        public bool CanStopExposure {
            get {
                if (Connected) {
                    return _camera.CanStopExposure;
                } else {
                    return false;
                }
            }
        }

        private bool _hasCCDTemperature;

        public double Temperature {
            get {
                double val = -1;
                try {
                    if (Connected && _hasCCDTemperature) {
                        val = _camera.CCDTemperature;
                    }
                } catch (InvalidValueException) {
                    _hasCCDTemperature = false;
                }
                return val;
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    bool val = false;
                    try {
                        val = _camera.Connected;
                        if (_connected != val) {
                            Notification.ShowError(Locale.Loc.Instance["LblCameraConnectionLost"]);
                            Disconnect();
                        }
                    } catch (Exception) {
                        Disconnect();
                    }
                    return val;
                } else {
                    return false;
                }
            }
            private set {
                try {
                    _connected = value;
                    _camera.Connected = value;
                } catch (Exception ex) {
                    Notification.ShowError(Locale.Loc.Instance["LblReconnectCamera"] + Environment.NewLine + ex.Message);
                    _connected = false;
                }
                RaisePropertyChanged();
            }
        }

        private bool _hasCooler;

        public bool CoolerOn {
            get {
                bool val = false;
                try {
                    if (Connected && _hasCooler) {
                        val = _camera.CoolerOn;
                    }
                } catch (Exception) {
                    _hasCooler = false;
                }
                return val;
            }
            set {
                try {
                    if (Connected && _hasCooler) {
                        _camera.CoolerOn = value;
                        RaisePropertyChanged();
                    }
                } catch (Exception) {
                    _hasCooler = false;
                }
            }
        }

        public double CoolerPower {
            get {
                if (Connected && CanGetCoolerPower) {
                    return _camera.CoolerPower;
                } else {
                    return -1;
                }
            }
        }

        public bool HasDewHeater {
            get {
                return false;
            }
        }

        public bool DewHeaterOn {
            get {
                return false;
            }
            set {
            }
        }

        public string Description {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = _camera.Description;
                    } catch (DriverException) {
                    }
                }
                return val;
            }
        }

        public string DriverInfo {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = _camera.DriverInfo;
                    } catch (DriverException) {
                    }
                }
                return val;
            }
        }

        public string DriverVersion {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = _camera.DriverVersion;
                    } catch (DriverException) {
                    }
                }
                return val;
            }
        }

        public double ElectronsPerADU {
            get {
                double val = -1;
                if (Connected) {
                    val = _camera.ElectronsPerADU;
                }
                return val;
            }
        }

        public double ExposureMax {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = _camera.ExposureMax;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public double ExposureMin {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = _camera.ExposureMin;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public double ExposureResolution {
            get {
                double val = -1;
                if (Connected) {
                    try {
                        val = _camera.ExposureResolution;
                    } catch (InvalidValueException) {
                    }
                }
                return val;
            }
        }

        public bool FastReadout {
            get {
                bool val = false;
                if (Connected && CanFastReadout) {
                    val = _camera.FastReadout;
                }
                return val;
            }
            set {
                if (Connected && CanFastReadout) {
                    _camera.FastReadout = value;
                }
            }
        }

        public double FullWellCapacity {
            get {
                double val = -1;
                if (Connected) {
                    val = _camera.FullWellCapacity;
                }
                return val;
            }
        }

        private bool _canGetGain;

        public bool CanGetGain {
            get {
                return _canGetGain;
            }
            set {
                _canGetGain = value;
                RaisePropertyChanged();
            }
        }

        private bool _canSetGain;

        public bool CanSetGain {
            get {
                return _canSetGain;
            }
            set {
                _canSetGain = value;
                RaisePropertyChanged();
            }
        }

        public short Gain {
            get {
                short val = -1;
                if (Connected && CanGetGain) {
                    try {
                        if (Gains.Count > 0) {
                            val = (short)Gains[_camera.Gain];
                        } else {
                            val = _camera.Gain;
                        }
                    } catch (PropertyNotImplementedException) {
                        CanGetGain = false;
                    }
                }
                return val;
            }
            set {
                if (Connected && CanSetGain) {
                    try {
                        if (Gains.Count > 0) {
                            _camera.Gain = (short)Gains.IndexOf(value);
                        } else {
                            _camera.Gain = value;
                        }
                    } catch (PropertyNotImplementedException) {
                        CanSetGain = false;
                    } catch (InvalidValueException ex) {
                        Notification.ShowWarning(ex.Message);
                    } catch (Exception) {
                        CanSetGain = false;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public ICollection ReadoutModes {
            get {
                if (!CanFastReadout) {
                    return ReadoutModesArrayList;
                }

                return new List<string>() { "Default", "Fast Readout" };
            }
        }

        private short readoutModeForSnapImages;

        public short ReadoutModeForSnapImages {
            get => readoutModeForSnapImages;
            set {
                readoutModeForSnapImages = value;
                RaisePropertyChanged();
            }
        }

        private short readoutModeForNormalImages;

        public short ReadoutModeForNormalImages {
            get => readoutModeForNormalImages;
            set {
                readoutModeForNormalImages = value;
                RaisePropertyChanged();
            }
        }

        private bool _canGetGainMinMax;

        public short GainMax {
            get {
                short val = -1;
                if (Connected && _canGetGainMinMax) {
                    try {
                        val = _camera.GainMax;
                    } catch (PropertyNotImplementedException) {
                        _canGetGainMinMax = false;
                    }
                }
                return val;
            }
        }

        public short GainMin {
            get {
                short val = -1;
                if (Connected && _canGetGainMinMax) {
                    try {
                        val = _camera.GainMin;
                    } catch (PropertyNotImplementedException) {
                        _canGetGainMinMax = false;
                    }
                }
                return val;
            }
        }

        private ArrayList _gains;

        public ArrayList Gains {
            get {
                if (_gains == null) {
                    _gains = new ArrayList();
                }
                return _gains;
            }
        }

        public bool HasShutter {
            get {
                if (Connected) {
                    return _camera.HasShutter;
                } else {
                    return false;
                }
            }
        }

        public double HeatSinkTemperature {
            get {
                if (Connected) {
                    return _camera.HeatSinkTemperature;
                } else {
                    return double.MinValue;
                }
            }
        }

        public object ImageArray {
            get {
                if (Connected) {
                    return _camera.ImageArray;
                } else {
                    return null;
                }
            }
        }

        public object ImageArrayVariant {
            get {
                if (Connected) {
                    return _camera.ImageArrayVariant;
                } else {
                    return null;
                }
            }
        }

        public bool ImageReady {
            get {
                if (Connected) {
                    return _camera.ImageReady;
                } else {
                    return false;
                }
            }
        }

        public short InterfaceVersion {
            get {
                short val = -1;
                try {
                    val = _camera.InterfaceVersion;
                } catch (DriverException) {
                }
                return val;
            }
        }

        public bool IsPulseGuiding {
            get {
                if (Connected) {
                    return _camera.IsPulseGuiding;
                } else {
                    return false;
                }
            }
        }

        private bool _hasLastExposureInfo;

        public double LastExposureDuration {
            get {
                double val = -1;
                try {
                    if (Connected && _hasLastExposureInfo) {
                        val = _camera.LastExposureDuration;
                    }
                } catch (ASCOM.InvalidOperationException) {
                } catch (PropertyNotImplementedException) {
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
                        val = _camera.LastExposureStartTime;
                    }
                } catch (ASCOM.InvalidOperationException) {
                } catch (PropertyNotImplementedException) {
                    _hasLastExposureInfo = false;
                }
                return val;
            }
        }

        public int MaxADU {
            get {
                if (Connected) {
                    return _camera.MaxADU;
                } else {
                    return -1;
                }
            }
        }

        public short MaxBinX {
            get {
                if (Connected) {
                    return _camera.MaxBinX;
                } else {
                    return -1;
                }
            }
        }

        public short MaxBinY {
            get {
                if (Connected) {
                    return _camera.MaxBinY;
                } else {
                    return -1;
                }
            }
        }

        private string _name;

        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public int NumX {
            get {
                if (Connected) {
                    return _camera.NumX;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    if (value % 2 > 0) {
                        value--;
                    }
                    _camera.NumX = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int NumY {
            get {
                if (Connected) {
                    return _camera.NumY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    if (value % 2 > 0) {
                        value--;
                    }
                    _camera.NumY = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _hasPercentCompleted;

        public short PercentCompleted {
            get {
                short val = -1;
                try {
                    if (_hasPercentCompleted) {
                        val = _camera.PercentCompleted;
                    }
                } catch (ASCOM.InvalidOperationException) {
                } catch (PropertyNotImplementedException) {
                    _hasPercentCompleted = false;
                }
                return val;
            }
        }

        public double PixelSizeX {
            get {
                if (Connected) {
                    return _camera.PixelSizeX;
                } else {
                    return -1;
                }
            }
        }

        public double PixelSizeY {
            get {
                if (Connected) {
                    return _camera.PixelSizeY;
                } else {
                    return -1;
                }
            }
        }

        public short ReadoutMode {
            get {
                if (Connected) {
                    return _camera.ReadoutMode;
                } else {
                    return -1;
                }
            }
            set {
                try {
                    _camera.ReadoutMode = value;
                } catch (InvalidValueException ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public ArrayList ReadoutModesArrayList {
            get {
                ArrayList val = new ArrayList();
                if (Connected && !CanFastReadout) {
                    val = _camera.ReadoutModes;
                }
                return val;
            }
        }

        public string SensorName {
            get {
                string val = string.Empty;
                if (Connected) {
                    try {
                        val = _camera.SensorName;
                    } catch (PropertyNotImplementedException) {
                    }
                }
                return val;
            }
        }

        public SensorType SensorType {
            get {
                if (Connected) {
                    SensorType type;
                    switch (_camera.SensorType) {
                        case ASCOM.DeviceInterface.SensorType.Monochrome:
                            type = SensorType.Monochrome;
                            break;

                        case ASCOM.DeviceInterface.SensorType.Color:
                            type = SensorType.Color;
                            break;

                        case ASCOM.DeviceInterface.SensorType.RGGB:
                            type = SensorType.RGGB;
                            break;

                        case ASCOM.DeviceInterface.SensorType.CMYG:
                            type = SensorType.CMYG;
                            break;

                        case ASCOM.DeviceInterface.SensorType.CMYG2:
                            type = SensorType.CMYG2;
                            break;

                        case ASCOM.DeviceInterface.SensorType.LRGB:
                            type = SensorType.LRGB;
                            break;

                        default:
                            type = SensorType.Monochrome;
                            break;
                    }

                    return type;
                } else {
                    return SensorType.Monochrome;
                }
            }
        }

        public double TemperatureSetPoint {
            get {
                double val = double.MinValue;
                if (Connected && CanSetTemperature) {
                    val = _camera.SetCCDTemperature;
                }
                return val;
            }
            set {
                if (Connected && CanSetTemperature) {
                    try {
                        _camera.SetCCDTemperature = value;
                        RaisePropertyChanged();
                    } catch (InvalidValueException ex) {
                        Notification.ShowError(ex.Message);
                    }
                }
            }
        }

        public int StartX {
            get {
                if (Connected) {
                    return _camera.StartX;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    _camera.StartX = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int StartY {
            get {
                if (Connected) {
                    return _camera.StartY;
                } else {
                    return -1;
                }
            }
            set {
                if (Connected) {
                    _camera.StartY = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ArrayList SupportedActions {
            get {
                ArrayList val = new ArrayList();
                try {
                    val = _camera.SupportedActions;
                } catch (DriverException) {
                }
                return val;
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes { get; private set; }

        [System.Obsolete("Use async Connect")]
        public bool Connect() {
            try {
                _camera = new Camera(Id);
                Connected = true;
                if (Connected) {
                    init();
                    RaiseAllPropertiesChanged();
                }
            } catch (ASCOM.DriverAccessCOMException ex) {
                Notification.ShowError(ex.Message);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblUnableToConnectCamera"] + ex.Message);
            }
            return Connected;
        }

        public void Disconnect() {
            Connected = false;
            _camera?.Dispose();
            _camera = null;
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token, bool calculateStatistics) {
            using (MyStopWatch.Measure("ASCOM Download")) {
                return await Task.Run(async () => {
                    try {
                        using (MyStopWatch.Measure("ASCOM WaitForCamera")) {
                            while (!ImageReady && Connected) {
                                //Console.Write(".");
                                await Utility.Utility.Wait(TimeSpan.FromMilliseconds(100), token);
                            }
                        }

                        if (SensorType != SensorType.Color) {
                            return await MyCamera.ImageArray.CreateInstance((Int32[,])ImageArray, BitDepth, SensorType != SensorType.Monochrome, calculateStatistics, profileService.ActiveProfile.ImageSettings.HistogramResolution);
                        } else {
                            return await MyCamera.ImageArray.CreateInstance((Int32[,,])ImageArray, BitDepth, false, calculateStatistics, profileService.ActiveProfile.ImageSettings.HistogramResolution);
                        }
                    } catch (OperationCanceledException) {
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                    return null;
                });
            }
        }

        public void StartExposure(CaptureSequence sequence, bool isLightFrame) {
            if (EnableSubSample) {
                StartX = SubSampleX;
                StartY = SubSampleY;
                NumX = SubSampleWidth / BinX;
                NumY = SubSampleHeight / BinY;
            } else {
                StartX = 0;
                StartY = 0;
                NumX = CameraXSize / BinX;
                NumY = CameraYSize / BinY;
            }

            bool isSnap = sequence.ImageType == CaptureSequence.ImageTypes.SNAP;

            if (CanFastReadout) {
                _camera.FastReadout = isSnap ? readoutModeForSnapImages != 0 : readoutModeForNormalImages != 0;
            } else {
                _camera.ReadoutMode =
                    isSnap
                        ? readoutModeForSnapImages
                        : readoutModeForNormalImages;
            }

            _camera.StartExposure(sequence.ExposureTime, isLightFrame);
        }

        public void StopExposure() {
            if (CanStopExposure) {
                try {
                    StopExposure();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
                }
            }
        }

        public void AbortExposure() {
            if (CanAbortExposure) {
                try {
                    _camera.AbortExposure();
                } catch (Exception e) {
                    Notification.ShowError(e.Message);
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

        public void Dispose() {
            _camera.Dispose();
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public bool CanSetOffset {
            get {
                return false;
            }
        }

        public bool CanSetUSBLimit {
            get {
                return false;
            }
        }

        public int Offset {
            get {
                return -1;
            }
            set {
            }
        }

        public int USBLimit {
            get {
                return -1;
            }
            set {
            }
        }

        public void SetupDialog() {
            if (HasSetupDialog) {
                try {
                    bool dispose = false;
                    if (_camera == null) {
                        _camera = new Camera(Id);
                        dispose = true;
                    }

                    _camera.SetupDialog();
                    if (dispose) {
                        _camera.Dispose();
                        _camera = null;
                    }
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                }
            }
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task<bool>.Run(() => {
                try {
                    _camera = new Camera(Id);
                    Connected = true;
                    if (Connected) {
                        init();
                        RaiseAllPropertiesChanged();
                    }
                } catch (ASCOM.DriverAccessCOMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (System.Runtime.InteropServices.COMException ex) {
                    Utility.Utility.HandleAscomCOMException(ex);
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("Unable to connect to camera " + ex.Message);
                }
                return Connected;
            });
        }

        public void StartLiveView() {
            throw new System.NotImplementedException();
        }

        public Task<ImageArray> DownloadLiveView(CancellationToken token) {
            throw new System.NotImplementedException();
        }

        public void StopLiveView() {
            throw new System.NotImplementedException();
        }

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
                // todo: code to start liveview if possible
            }
        }

        public int BatteryLevel => -1;

        public bool HasBattery => false;

        public int BitDepth {
            get {
                return (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            }
        }
    }
}