#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model.Equipment;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MyCamera {

    public class SimulatorCamera : BaseINPC, ICamera {

        public SimulatorCamera(IProfileService profileService, IImageDataFactory imageDataFactory, IExposureDataFactory exposureDataFactory) {
            this.profileService = profileService;
            this.imageDataFactory = imageDataFactory;
            this.exposureDataFactory = exposureDataFactory;
            CameraState = CameraStates.Idle;
        }

        public string Category { get; } = "Simulator";

        public bool HasShutter => false;

        public bool Connected { get; private set; }

        public double CCDTemperature => double.NaN;

        public double SetCCDTemperature {
            get => double.NaN;

            set {
            }
        }

        public short BinX {
            get => 1;

            set {
            }
        }

        public short BinY {
            get => 1;

            set {
            }
        }

        public string Description => "Simulator Camera";

        public string DriverInfo => string.Empty;

        public string DriverVersion => CoreUtil.Version;

        public string SensorName => "";

        public SensorType SensorType => SensorType.Monochrome;

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        public int CameraXSize => 9568;

        public int CameraYSize => 6380;

        public double ExposureMin => 0.0001;

        public double ExposureMax => 3600;

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX => 1;

        public short MaxBinY => 1;

        public double PixelSizeX => 3.76;

        public double PixelSizeY => 3.76;

        public bool CanSetCCDTemperature => false;

        public bool CoolerOn {
            get => false;

            set {
            }
        }

        public double CoolerPower => double.NaN;

        private CameraStates cameraState;

        public CameraStates CameraState {
            get => cameraState;
            set {
                cameraState = value;
                RaisePropertyChanged();
            }
        }

        public int Offset {
            get => 0;

            set {
            }
        }

        public int USBLimit {
            get => 0;

            set {
            }
        }

        public int USBLimitMax => 0;
        public int USBLimitMin => 0;
        public int USBLimitStep => 0;

        public IList<string> SupportedActions => new List<string>();

        public bool CanSetOffset => false;

        public int OffsetMin => 0;

        public int OffsetMax => 0;

        public bool CanSetUSBLimit => false;

        public bool CanGetGain => true;

        public bool CanSetGain => false;

        public int GainMax => 100;

        public int GainMin => 100;

        public int Gain {
            get => 100;

            set {
            }
        }

        public IList<int> Gains => new List<int>();

        private AsyncObservableCollection<BinningMode> binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (binningModes == null) {
                    binningModes = new AsyncObservableCollection<BinningMode> {
                        new BinningMode(1,1)
                    };
                }
                return binningModes;
            }
        }

        public bool HasSetupDialog => false;

        public string Id => "12345678";

        public string Name => "Simulator Camera";
        public string DisplayName => Name;

        public double Temperature => double.NaN;

        public double TemperatureSetPoint {
            get => double.NaN;

            set => throw new NotImplementedException();
        }

        public bool CanSetTemperature => false;

        public bool CanSubSample => false;

        public bool EnableSubSample {
            get => false;

            set {
            }
        }

        public int SubSampleX { get; set; }

        public int SubSampleY { get; set; }

        public int SubSampleWidth { get; set; }

        public int SubSampleHeight { get; set; }

        public bool CanShowLiveView => false;

        public bool LiveViewEnabled {
            get => false;
            set {
            }
        }

        public bool HasDewHeater => false;

        public bool DewHeaterOn {
            get => false;

            set {
            }
        }

        public bool HasBattery => false;

        public int BatteryLevel => 0;

        public int BitDepth => (int)profileService.ActiveProfile.CameraSettings.BitDepth;

        public IList<string> ReadoutModes => new List<string> { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

        public short ReadoutModeForSnapImages {
            get => 0;

            set {
            }
        }

        public short ReadoutModeForNormalImages {
            get => 0;

            set {
            }
        }

        public void AbortExposure() {
        }

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Connected = false;
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                var remaining = exposureTime - (DateTime.Now - exposureStart);
                if (remaining > TimeSpan.Zero) {
                    await Task.Delay(remaining, token);
                }
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            try {
                CameraState = CameraStates.LoadingFile;
                var tries = 0;
                while (true) {
                    tries++;
                    try {
                        var image = await imageDataFactory.CreateFromFile("/tmp/simulator.fits", 16, false, profileService.ActiveProfile.CameraSettings.RawConverter, token);
                        return exposureDataFactory.CreateCachedExposureData(image);
                    } catch (Exception ex) {
                        if (tries > 3) {
                            Logger.Error(ex);
                            throw;
                        }
                        await CoreUtil.Wait(TimeSpan.FromSeconds(1), token);
                    }
                }
            } finally {
                CameraState = CameraStates.LoadingFile;
            }
        }

        private IProfileService profileService;
        private readonly IImageDataFactory imageDataFactory;
        private readonly IExposureDataFactory exposureDataFactory;

        public void SetBinning(short x, short y) {
        }

        public void SetupDialog() {
        }

        private DateTime exposureStart;
        private TimeSpan exposureTime;

        public void StartExposure(CaptureSequence captureSequence) {
            exposureStart = DateTime.Now;
            exposureTime = TimeSpan.FromSeconds(captureSequence.ExposureTime);
            CameraState = CameraStates.Exposing;
        }

        public void StopExposure() {
            CameraState = CameraStates.Idle;
        }

        public void StartLiveView(CaptureSequence sequence) {
            throw new System.NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new System.NotImplementedException();
        }

        public void StopLiveView() {
            throw new System.NotImplementedException();
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
    }
}
