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
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model.Equipment;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MyCamera {

    public class PersistSettingsCameraDecorator : BaseINPC, ICamera {
        public ICamera Camera { get; }
        private readonly IProfileService profileService;

        public PersistSettingsCameraDecorator(IProfileService profileService, ICamera camera) {
            this.profileService = profileService;
            this.Camera = camera;
            this.Camera.PropertyChanged += Camera_PropertyChanged;
        }

        private void Camera_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged(e.PropertyName);
        }

        private void RestoreCameraProfileDefaults() {
            if (this.profileService.ActiveProfile.CameraSettings.BinningX.HasValue && this.profileService.ActiveProfile.CameraSettings.BinningY.HasValue) {
                try {
                    this.Camera.SetBinning(this.profileService.ActiveProfile.CameraSettings.BinningX.Value, this.profileService.ActiveProfile.CameraSettings.BinningY.Value);
                } catch (Exception e) {
                    Logger.Debug(e.Message);
                    this.profileService.ActiveProfile.CameraSettings.BinningX = null;
                    this.profileService.ActiveProfile.CameraSettings.BinningY = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.Gain.HasValue && this.Camera.CanSetGain) {
                try {
                    this.Camera.Gain = this.profileService.ActiveProfile.CameraSettings.Gain.Value;
                } catch (Exception e) {
                    Logger.Debug(e.Message);
                    this.profileService.ActiveProfile.CameraSettings.Gain = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.Offset.HasValue && this.Camera.CanSetOffset) {
                try {
                    this.Camera.Offset = this.profileService.ActiveProfile.CameraSettings.Offset.Value;
                } catch (Exception e) {
                    Logger.Debug(e.Message);
                    this.profileService.ActiveProfile.CameraSettings.Offset = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.USBLimit.HasValue && this.Camera.CanSetUSBLimit) {
                try {
                    this.Camera.USBLimit = this.profileService.ActiveProfile.CameraSettings.USBLimit.Value;
                } catch (Exception e) {
                    Logger.Debug(e.Message);
                    this.profileService.ActiveProfile.CameraSettings.USBLimit = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.ReadoutMode.HasValue) {
                try {
                    this.Camera.ReadoutMode = this.profileService.ActiveProfile.CameraSettings.ReadoutMode.Value;
                } catch (Exception e) {
                    Logger.Debug(e.Message);
                    this.profileService.ActiveProfile.CameraSettings.ReadoutMode = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages.HasValue) {
                try {
                    if (this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages.Value < this.Camera.ReadoutModes.Count) {
                        this.Camera.ReadoutModeForSnapImages = this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages.Value;
                    }
                } catch (Exception e) {
                    Logger.Debug(e.Message);
                    this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages.HasValue) {
                try {
                    if (this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages.Value < this.Camera.ReadoutModes.Count) {
                        this.Camera.ReadoutModeForNormalImages = this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages.Value;
                    }
                } catch (Exception e) {
                    Logger.Debug(e.Message);
                    this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages = null;
                }
            }
            if (this.profileService.ActiveProfile.CameraSettings.DewHeaterOn.HasValue) {
                try {
                    this.Camera.DewHeaterOn = this.profileService.ActiveProfile.CameraSettings.DewHeaterOn.Value;
                } catch (Exception e) {
                    Logger.Debug(e.Message);
                    this.profileService.ActiveProfile.CameraSettings.DewHeaterOn = null;
                }
            }
        }

        public bool HasShutter => this.Camera.HasShutter;

        public double Temperature => this.Camera.Temperature;

        public double TemperatureSetPoint {
            get => this.Camera.TemperatureSetPoint;
            set => this.Camera.TemperatureSetPoint = value;
        }

        public short BinX {
            get => this.Camera.BinX;
            set {
                this.Camera.BinX = value;
                this.profileService.ActiveProfile.CameraSettings.BinningX = value;
            }
        }

        public short BinY {
            get => this.Camera.BinY;
            set {
                this.Camera.BinY = value;
                this.profileService.ActiveProfile.CameraSettings.BinningY = value;
            }
        }

        public string SensorName => this.Camera.SensorName;

        public string DisplayName => this.Camera.DisplayName;

        public SensorType SensorType => this.Camera.SensorType;

        public short BayerOffsetX => this.Camera.BayerOffsetX;

        public short BayerOffsetY => this.Camera.BayerOffsetY;

        public int CameraXSize => this.Camera.CameraXSize;

        public int CameraYSize => this.Camera.CameraYSize;

        public double ExposureMin => this.Camera.ExposureMin;

        public double ExposureMax => this.Camera.ExposureMax;

        public short MaxBinX => this.Camera.MaxBinX;

        public short MaxBinY => this.Camera.MaxBinY;

        public double PixelSizeX => this.Camera.PixelSizeX;

        public double PixelSizeY => this.Camera.PixelSizeY;

        public string LensName => this.Camera.LensName;

        public double LensFocalLength => this.Camera.LensFocalLength;

        public double LensFocalRatio => this.Camera.LensFocalRatio;

        public bool CanSetTemperature => this.Camera.CanSetTemperature;

        public bool CoolerOn { get => this.Camera.CoolerOn; set => this.Camera.CoolerOn = value; }

        public double CoolerPower => this.Camera.CoolerPower;

        public bool HasDewHeater => this.Camera.HasDewHeater;

        public bool DewHeaterOn {
            get => this.Camera.DewHeaterOn;
            set {
                this.Camera.DewHeaterOn = value;
                this.profileService.ActiveProfile.CameraSettings.DewHeaterOn = value;
            }
        }

        public CameraStates CameraState => this.Camera.CameraState;

        public bool CanSubSample => this.Camera.CanSubSample;

        public bool EnableSubSample { get => this.Camera.EnableSubSample; set => this.Camera.EnableSubSample = value; }
        public int SubSampleX { get => this.Camera.SubSampleX; set => this.Camera.SubSampleX = value; }
        public int SubSampleY { get => this.Camera.SubSampleY; set => this.Camera.SubSampleY = value; }
        public int SubSampleWidth { get => this.Camera.SubSampleWidth; set => this.Camera.SubSampleWidth = value; }
        public int SubSampleHeight { get => this.Camera.SubSampleHeight; set => this.Camera.SubSampleHeight = value; }

        public bool CanShowLiveView => this.Camera.CanShowLiveView;

        public bool LiveViewEnabled => this.Camera.LiveViewEnabled;

        public bool HasBattery => this.Camera.HasBattery;

        public int BatteryLevel => this.Camera.BatteryLevel;

        public int BitDepth => this.Camera.BitDepth;

        public int Offset {
            get => this.Camera.Offset;
            set {
                if (this.Camera.CanSetOffset) {
                    this.Camera.Offset = value;
                }
            }
        }

        public int USBLimit {
            get => this.Camera.USBLimit;
            set {
                if (this.Camera.CanSetUSBLimit) {
                    this.Camera.USBLimit = value;
                    this.profileService.ActiveProfile.CameraSettings.USBLimit = value;
                }
            }
        }

        public int USBLimitMax => this.Camera.USBLimitMax;

        public int USBLimitMin => this.Camera.USBLimitMin;

        public int USBLimitStep => this.Camera.USBLimitStep;

        public bool CanSetOffset => this.Camera.CanSetOffset;

        public int OffsetMin => this.Camera.OffsetMin;

        public int OffsetMax => this.Camera.OffsetMax;

        public bool CanSetUSBLimit => this.Camera.CanSetUSBLimit;

        public bool CanGetGain => this.Camera.CanGetGain;

        public bool CanSetGain => this.Camera.CanSetGain;

        public int GainMax => this.Camera.GainMax;

        public int GainMin => this.Camera.GainMin;

        public int Gain {
            get {
                if (CanGetGain) {
                    return this.Camera.Gain;
                }
                return -1;
            }
            set {
                if (CanSetGain) {
                    this.Camera.Gain = value;
                }
            }
        }

        public double ElectronsPerADU => this.Camera.ElectronsPerADU;

        public IList<string> ReadoutModes => this.Camera.ReadoutModes;

        public short ReadoutMode {
            get => this.Camera.ReadoutMode;
            set {
                this.Camera.ReadoutMode = value;
                this.profileService.ActiveProfile.CameraSettings.ReadoutMode = value;
            }
        }

        public short ReadoutModeForSnapImages {
            get => this.Camera.ReadoutModeForSnapImages;
            set {
                this.Camera.ReadoutModeForSnapImages = value;
                this.profileService.ActiveProfile.CameraSettings.ReadoutModeForSnapImages = value;
            }
        }

        public short ReadoutModeForNormalImages {
            get => this.Camera.ReadoutModeForNormalImages;
            set {
                this.Camera.ReadoutModeForNormalImages = value;
                this.profileService.ActiveProfile.CameraSettings.ReadoutModeForNormalImages = value;
            }
        }

        public IList<int> Gains => this.Camera.Gains;

        public AsyncObservableCollection<BinningMode> BinningModes => this.Camera.BinningModes;

        public bool HasSetupDialog => this.Camera.HasSetupDialog;

        public string Id => this.Camera.Id;

        public string Name => this.Camera.Name;

        public string Category => this.Camera.Category;

        public bool Connected => this.Camera.Connected;

        public string Description => this.Camera.Description;

        public string DriverInfo => this.Camera.DriverInfo;

        public string DriverVersion => this.Camera.DriverVersion;

        public void AbortExposure() {
            this.Camera.AbortExposure();
        }

        public async Task<bool> Connect(CancellationToken token) {
            var result = await this.Camera.Connect(token);
            if (result) {
                RestoreCameraProfileDefaults();
            }
            return result;
        }

        public void Disconnect() {
            this.Camera.Disconnect();
        }

        public Task<IExposureData> DownloadExposure(CancellationToken token) {
            return this.Camera.DownloadExposure(token);
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return this.Camera.DownloadLiveView(token);
        }

        public void SetBinning(short x, short y) {
            this.Camera.SetBinning(x, y);
        }

        public void SetupDialog() {
            this.Camera.SetupDialog();
        }

        public void StartExposure(CaptureSequence sequence) {
            this.Camera.StartExposure(sequence);
        }

        public void StartLiveView(CaptureSequence sequence) {
            this.Camera.StartLiveView(sequence);
        }

        public void StopExposure() {
            this.Camera.StopExposure();
        }

        public void StopLiveView() {
            this.Camera.StopLiveView();
        }

        public Task WaitUntilExposureIsReady(CancellationToken token) {
            return this.Camera.WaitUntilExposureIsReady(token);
        }

        public IList<string> SupportedActions => this.Camera.SupportedActions;

        public string Action(string actionName, string actionParameters) {
            return this.Camera.Action(actionName, actionParameters);
        }

        public string SendCommandString(string command, bool raw) {
            return this.Camera.SendCommandString(command, raw);
        }

        public bool SendCommandBool(string command, bool raw) {
            return this.Camera.SendCommandBool(command, raw);
        }

        public void SendCommandBlind(string command, bool raw) {
            this.Camera.SendCommandBlind(command, raw);
        }

        public void UpdateSubSampleArea() {
            this.Camera.UpdateSubSampleArea();
        }
    }
}