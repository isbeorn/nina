#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility;
using NINA.Utility.AtikSDK;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    internal class AtikCamera : BaseINPC, ICamera {

        public AtikCamera(int id, IProfileService profileService) {
            this.profileService = profileService;
            _cameraId = id;
            _info = AtikCameraDll.GetCameraProperties(_cameraId);
        }

        private int _cameraId;
        private IntPtr _cameraP;

        private AtikCameraDll.ArtemisPropertiesStruct _info;

        private AtikCameraDll.ArtemisPropertiesStruct Info {
            get {
                return _info;
            }
        }

        public bool HasShutter {
            get {
                var bitNumber = 5;
                var bit = (Info.cameraflags & (1 << bitNumber - 1)) != 0;
                return bit;
            }
        }

        public bool Connected {
            get {
                return AtikCameraDll.IsConnected(_cameraP);
            }
        }

        public double Temperature {
            get {
                return AtikCameraDll.GetTemperature(_cameraP);
            }
        }

        public bool CanShowLiveView {
            get {
                return false;
            }
        }

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

        public bool CanSubSample {
            get {
                var bitNumber = 5;
                var bit = (Info.cameraflags & (1 << bitNumber - 1)) != 0;
                return bit;
            }
        }

        public bool EnableSubSample { get; set; }
        public int SubSampleX { get; set; }
        public int SubSampleY { get; set; }
        public int SubSampleWidth { get; set; }
        public int SubSampleHeight { get; set; }

        private bool _coolerOn;

        public bool CoolerOn {
            get {
                return _coolerOn;
            }
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

        public string Description {
            get {
                return CleanedUpString(Info.Manufacturer) + " " + CleanedUpString(Info.Description) + " (SerialNo: " + AtikCameraDll.GetSerialNumber(_cameraP) + ")";
            }
        }

        public string DriverInfo {
            get {
                return AtikCameraDll.DriverName;
            }
        }

        public string DriverVersion {
            get {
                return AtikCameraDll.DriverVersion;
            }
        }

        public string SensorName {
            get {
                return string.Empty;
            }
        }

        public SensorType SensorType {
            get {
                return AtikCameraDll.GetColorInformation(_cameraP);
            }
        }

        public int CameraXSize {
            get {
                return Info.nPixelsX;
            }
        }

        public int CameraYSize {
            get {
                return Info.nPixelsY;
            }
        }

        public double ExposureMin {
            get {
                return 0;
            }
        }

        public double ExposureMax {
            get {
                return double.PositiveInfinity;
            }
        }

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

        public double PixelSizeX {
            get {
                return Info.PixelMicronsX;
            }
        }

        public double PixelSizeY {
            get {
                return Info.PixelMicronsY;
            }
        }

        public bool CanSetTemperature {
            get {
                return AtikCameraDll.HasCooler(_cameraP);
            }
        }

        public double CoolerPower {
            get {
                if (CanSetTemperature) {
                    return AtikCameraDll.CoolerPower(_cameraP);
                } else {
                    return double.NaN;
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

        public string CameraState {
            get {
                return AtikCameraDll.CameraState(_cameraP).ToString();
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

        public bool CanGetGain {
            get {
                return false;
            }
        }

        public bool CanSetGain {
            get {
                return false;
            }
        }

        public short GainMax {
            get {
                return -1;
            }
        }

        public short GainMin {
            get {
                return -1;
            }
        }

        public short Gain {
            get {
                return -1;
            }

            set {
            }
        }

        public ArrayList Gains {
            get {
                return new ArrayList();
            }
        }

        public ICollection ReadoutModes => new List<string> { "Default" };

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

        public int BitDepth {
            get {
                //currently unknown if the values are stretched to 16 bit or not. Take profile value
                return (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            }
        }

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

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public string Id {
            get {
                return CleanedUpString(Info.Description);
            }
        }

        public string Name {
            get {
                return CleanedUpString(Info.Description);
            }
        }

        private string CleanedUpString(char[] values) {
            return string.Join("", values.Take(Array.IndexOf(values, '\0')));
        }

        public void AbortExposure() {
            AtikCameraDll.StopExposure(_cameraP);
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                var success = false;
                try {
                    _cameraP = AtikCameraDll.Connect(_cameraId);
                    _info = AtikCameraDll.GetCameraProperties(_cameraP);

                    if (CanSetTemperature) {
                        TemperatureSetPoint = 20;
                    }

                    RaisePropertyChanged(nameof(BinningModes));
                    RaisePropertyChanged(nameof(Connected));
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
            _binningModes = null;
            RaisePropertyChanged(nameof(Connected));
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token, bool calculateStatistics) {
            using (MyStopWatch.Measure("ATIK Download")) {
                return await Task.Run<ImageArray>(async () => {
                    try {
                        do {
                            await Task.Delay(100, token);
                        } while (!AtikCameraDll.ImageReady(_cameraP));

                        return await AtikCameraDll.DownloadExposure(_cameraP, BitDepth, SensorType != SensorType.Monochrome, calculateStatistics, profileService.ActiveProfile.ImageSettings.HistogramResolution);
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
                System.Threading.Thread.Sleep(100);
            } while (AtikCameraDll.CameraState(_cameraP) != AtikCameraDll.ArtemisCameraStateEnum.CAMERA_IDLE);
            if (EnableSubSample) {
                AtikCameraDll.SetSubFrame(_cameraP, SubSampleX, SubSampleY, SubSampleWidth, SubSampleHeight);
            } else {
                AtikCameraDll.SetSubFrame(_cameraP, 0, 0, CameraXSize, CameraYSize);
            }
            AtikCameraDll.StartExposure(_cameraP, sequence.ExposureTime);
        }

        public void StopExposure() {
            AtikCameraDll.StopExposure(_cameraP);
        }

        public void StartLiveView() {
            throw new NotImplementedException();
        }

        public Task<ImageArray> DownloadLiveView(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }

        private bool _liveViewEnabled;
        private IProfileService profileService;

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
            }
        }

        public int BatteryLevel => -1;

        public bool HasBattery => false;
    }
}