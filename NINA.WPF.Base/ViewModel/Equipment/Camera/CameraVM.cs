#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyCamera;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Accord.Statistics.Models.Regression.Linear;
using Dasync.Collections;
using NINA.Equipment.Utility;
using NINA.Core.Model;
using NINA.Core.Locale;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.MyMessageBox;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using Nito.AsyncEx;
using NINA.Core.Enum;
using NINA.Equipment.Exceptions;
using NINA.Core.Utility.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.WPF.Base.ViewModel.Equipment.Camera {

    public partial class CameraVM : DockableVM, ICameraVM {

        public CameraVM(IProfileService profileService,
                        ICameraMediator cameraMediator,
                        IApplicationStatusMediator applicationStatusMediator,
                        IDeviceChooserVM cameraChooserVM) : base(profileService) {
            Title = Loc.Instance["LblCamera"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CameraSVG"];
            HasSettings = true;

            DeviceChooserVM = cameraChooserVM;

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;

            ConnectCommand = new AsyncCommand<bool>(() => Task.Run(ChooseCamera), (object o) => DeviceChooserVM.SelectedDevice != null);
            CancelConnectCommand = new RelayCommand(CancelConnectCamera);
            DisconnectCommand = new AsyncCommand<bool>(() => Task.Run(DisconnectDiag));
            CoolCamCommand = new AsyncCommand<bool>(() => {
                _cancelChangeTemperatureCts?.Dispose();
                _cancelChangeTemperatureCts = new CancellationTokenSource();
                return Task.Run(() => CoolCamera(TargetTemp, TimeSpan.FromMinutes(CoolingDuration), new Progress<ApplicationStatus>(p => Status = p), _cancelChangeTemperatureCts.Token));
            }, (object o) => !TempChangeRunning);
            WarmCamCommand = new AsyncCommand<bool>(() => {
                _cancelChangeTemperatureCts?.Dispose();
                _cancelChangeTemperatureCts = new CancellationTokenSource();
                return Task.Run(() => WarmCamera(TimeSpan.FromMinutes(WarmingDuration), new Progress<ApplicationStatus>(p => Status = p), _cancelChangeTemperatureCts.Token));
            }, (object o) => !TempChangeRunning);
            CancelCoolCamCommand = new RelayCommand(CancelCoolCamera);
            RescanDevicesCommand = new AsyncCommand<bool>(async o => { await Task.Run(Rescan); return true; }, o => !CameraInfo.Connected);
            _ = RescanDevicesCommand.ExecuteAsync(null);

            TempChangeRunning = false;
            CoolerHistory = new AsyncObservableLimitedSizedStack<CameraCoolingStep>(100);
            CoolerHistoryMax = 20;
            coolerHistoryMin = -20;
            ToggleDewHeaterOnCommand = new RelayCommand(ToggleDewHeaterOn);

            updateTimer = new DeviceUpdateTimer(
                GetCameraValues,
                UpdateCameraValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += async (object sender, EventArgs e) => {
                await RescanDevicesCommand.ExecuteAsync(null);
                RaiseAllPropertiesChanged();  // Reload DefaultGain, and other default camera settings
            };
        }

        public async Task<IList<string>> Rescan() {
            return await Task.Run(async () => {
                await DeviceChooserVM.GetEquipment();
                return DeviceChooserVM.Devices.Select(x => x.Id).ToList();
            });
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get => _status;
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                this.applicationStatusMediator.StatusUpdate(_status);
            }
        }

        private ICameraMediator cameraMediator;

        public IDeviceChooserVM DeviceChooserVM { get; set; }

        private bool _tempChangeRunning;

        public bool TempChangeRunning {
            get => _tempChangeRunning;
            set {
                if (_tempChangeRunning != value) {
                    _tempChangeRunning = value;
                    RaisePropertyChanged();
                }
            }
        }

        private CancellationTokenSource _cancelChangeTemperatureCts;

        public async Task<bool> CoolCamera(double temperature, TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            try {
                TempChangeRunning = true;
                Logger.Info($"Cooling Camera. Target: {temperature} Duration: {duration}");
                var progressRouter = new Progress<double>((p) => {
                    progress.Report(new ApplicationStatus() {
                        Status = Loc.Instance["LblCooling"],
                        Progress = p
                    });
                });

                Cam.CoolerOn = true;
                return await RegulateTemperature(temperature, duration, true, progressRouter, ct);
            } catch (CannotReachTargetTemperatureException) {
                Logger.Error($"Could not reach target temperature. Target Temp: {temperature}, Current Temp: {Cam.Temperature}");
                Notification.ShowError(Loc.Instance["LblCouldNotReachTargetTemperature"]);
                return false;
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
                TempChangeRunning = false;
            }
        }

        public async Task<bool> WarmCamera(TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            try {
                TempChangeRunning = true;
                Logger.Info($"Warming Camera. Duration: {duration}");
                var progressRouter = new Progress<double>((p) => {
                    progress.Report(new ApplicationStatus() {
                        Status = Loc.Instance["LblWarming"],
                        Progress = p
                    });
                });

                if (Cam.Temperature < 20) {
                    using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct)) {
                        try {
                            timeoutCts.CancelAfter(duration + TimeSpan.FromMinutes(15));
                            await RegulateTemperature(20, duration, false, progressRouter, timeoutCts.Token);
                        } catch(OperationCanceledException) { 
                            if(ct.IsCancellationRequested == true) {
                                throw;
                            }
                        } catch (CannotReachTargetTemperatureException) {
                            Logger.Info("Could not reach warming temperature. Most likley due to ambient temperature being lower. Continuing...");
                        }
                    }
                }

                Logger.Debug("Waiting to turn cooler off");
                await CoreUtil.Wait(TimeSpan.FromSeconds(20), true, ct, progress, Loc.Instance["LblWaitingToTurnCoolerOff"]);
                Logger.Info("Turning cooler off");
                Cam.CoolerOn = false;
                return true;
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
                TempChangeRunning = false;
            }
        }

        private async Task<bool> RegulateTemperature(double temperature, TimeSpan duration, bool cooling, IProgress<double> progress, CancellationToken ct) {
            try {
                double currentTemp = Cam.Temperature;
                var totalDeltaTemp = Math.Abs(currentTemp - temperature);

                // Only wait if we're outside of the 1C tolerance at the start
                if (duration > TimeSpan.Zero && totalDeltaTemp > 1.0d) {
                    // Stepped temp change
                    double[] input = { 0, duration.TotalSeconds };
                    double[] output = { currentTemp, temperature };
                    OrdinaryLeastSquares leastSquares = new OrdinaryLeastSquares();
                    var regression = leastSquares.Learn(input, output);

                    Logger.Debug($"Starting stepped temperature change with parameters: Start Temp: {currentTemp}, Target Temp: {temperature}, Duration: {duration.TotalMinutes}m, Slope: {regression.Slope}, Intercept: {regression.Intercept}");

                    var interval = TimeSpan.FromSeconds(15);
                    int checkpoints = (int)(duration.TotalSeconds / interval.TotalSeconds);
                    for (var i = interval.TotalSeconds; i <= checkpoints * interval.TotalSeconds; i += interval.TotalSeconds) {
                        int temperatureStep = (int)Math.Round(regression.Transform(i));
                        Logger.Debug($"Setting Camera Setpoint to new value {temperatureStep}");
                        Cam.TemperatureSetPoint = temperatureStep;

                        var progressTemp = Math.Abs(currentTemp - temperature);
                        progress.Report((totalDeltaTemp - progressTemp) / totalDeltaTemp);

                        await CoreUtil.Wait(interval, ct);
                        currentTemp = Cam.Temperature;
                    }
                } else {
                    Cam.TemperatureSetPoint = temperature;
                }

                // Wait for final step
                var timeout = TimeSpan.FromMinutes(2);
                var idleTime = TimeSpan.Zero;
                var progressThreshold = 0.5d;
                var previousTemperature = currentTemp;
                while ((cooling && currentTemp > (temperature + 1)) || (!cooling && currentTemp < (temperature - 1))) {
                    var progressTemp = Math.Abs(currentTemp - temperature);
                    progress.Report((totalDeltaTemp - progressTemp) / totalDeltaTemp);

                    var t = await CoreUtil.Wait(TimeSpan.FromSeconds(5), ct);
                    currentTemp = Cam.Temperature;
                    var coolerPower = Cam.CoolerPower;
                    var deltaTemp = currentTemp - previousTemperature;
                    previousTemperature = currentTemp;
                    if (coolerPower < 1 && deltaTemp < progressThreshold) {
                        // if cooler power is 0% and temperature is not increasing enough over a 5 second period (0.5C) then bill towards the timeout
                        idleTime += t;
                    } else if (coolerPower > 99 && deltaTemp > -progressThreshold) {
                        // if cooler power is 100% and temperature is not decreasing enough over a 5 second period (0.5C) then bill towards the timeout
                        idleTime += t;
                    } else {
                        idleTime = TimeSpan.Zero;
                    }

                    if (idleTime > timeout) {
                        throw new CannotReachTargetTemperatureException();
                    }
                }
            } catch (OperationCanceledException) {
                if (Cam != null) {
                    Cam.TemperatureSetPoint = Cam.Temperature;
                }
                throw;
            } finally {
            }

            return true;
        }

        public class CannotReachTargetTemperatureException : Exception {
        }

        private void CancelCoolCamera(object o) {
            try { _cancelChangeTemperatureCts?.Cancel(); } catch { }
        }

        private double _targetTemp;

        public double TargetTemp {
            get => _targetTemp;
            set {
                if (_targetTemp != value) {
                    _targetTemp = value;
                    this.profileService.ActiveProfile.CameraSettings.Temperature = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _coolingDuration;

        public double CoolingDuration {
            get => _coolingDuration;
            set {
                if (_coolingDuration != value) {
                    _coolingDuration = value;
                    this.profileService.ActiveProfile.CameraSettings.CoolingDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _warmingDuration;

        public double WarmingDuration {
            get => _warmingDuration;
            set {
                if (_warmingDuration != value) {
                    _warmingDuration = value;
                    this.profileService.ActiveProfile.CameraSettings.WarmingDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ICamera _cam;

        public ICamera Cam {
            get => _cam;
            private set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseCamera() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (DeviceChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.CameraSettings.Id = DeviceChooserVM.SelectedDevice.Id;
                    return false;
                }

                if (DeviceChooserVM.SelectedDevice is OfflineDevice) {
                    await Rescan();
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );

                var cam = new PersistSettingsCameraDecorator(this.profileService, (ICamera)DeviceChooserVM.SelectedDevice);
                _cancelConnectCameraSource?.Dispose();
                _cancelConnectCameraSource = new CancellationTokenSource();
                var token = _cancelConnectCameraSource.Token;
                if (cam != null) {
                    try {
                        var connected = await cam.Connect(_cancelConnectCameraSource.Token);

                        if (connected) {
                            CoolerHistory.Clear();
                            CoolerHistoryMax = 20;
                            CoolerHistoryMin = -20;
                            this.Cam = cam;
                            token.ThrowIfCancellationRequested();

                            if (DefaultGain == -1) {
                                DefaultGain = Cam.Gain;
                            } else if (Cam.Gains.Count > 0 && Cam.Gains.IndexOf(DefaultGain) == -1) {
                                DefaultGain = Cam.Gain;
                            }

                            if (DefaultOffset == -1) {
                                DefaultOffset = Cam.Offset;
                            }

                            CameraInfo = new CameraInfo {
                                BinX = Cam.BinX,
                                BinY = Cam.BinY,
                                BinningModes = Cam.BinningModes,
                                CameraState = Cam.CameraState,
                                CanSubSample = Cam.CanSubSample,
                                ExposureMax = Cam.ExposureMax,
                                ExposureMin = Cam.ExposureMin,
                                SubSampleX = Cam.SubSampleX,
                                SubSampleY = Cam.SubSampleY,
                                SubSampleWidth = Cam.SubSampleWidth == 0 ? Cam.CameraXSize : Cam.SubSampleWidth,
                                SubSampleHeight = Cam.SubSampleHeight == 0 ? Cam.CameraYSize : Cam.SubSampleHeight,
                                Connected = true,
                                CoolerOn = Cam.CoolerOn,
                                CoolerPower = Cam.CoolerPower,
                                HasDewHeater = Cam.HasDewHeater,
                                DewHeaterOn = Cam.DewHeaterOn,
                                CanSetGain = Cam.CanSetGain,
                                Gains = Cam.Gains,
                                GainMin = Cam.GainMin,
                                GainMax = Cam.GainMax,
                                Gain = Cam.Gain,
                                HasShutter = Cam.HasShutter,
                                CanSetTemperature = Cam.CanSetTemperature,
                                IsSubSampleEnabled = Cam.EnableSubSample,
                                CanShowLiveView = Cam.CanShowLiveView,
                                CanGetGain = Cam.CanGetGain,
                                LiveViewEnabled = Cam.LiveViewEnabled,
                                Name = Cam.Name,
                                DisplayName = Cam.DisplayName,
                                DeviceId = Cam.Id,
                                CanSetOffset = Cam.CanSetOffset,
                                OffsetMin = Cam.OffsetMin,
                                OffsetMax = Cam.OffsetMax,
                                Offset = Cam.Offset,
                                PixelSize = Cam.PixelSizeX,
                                Temperature = Cam.Temperature,
                                TemperatureSetPoint = Cam.TemperatureSetPoint,
                                XSize = Cam.CameraXSize,
                                YSize = Cam.CameraYSize,
                                Battery = Cam.BatteryLevel,
                                BitDepth = Cam.BitDepth,
                                ElectronsPerADU = Cam.ElectronsPerADU,
                                ReadoutMode = Cam.ReadoutMode,
                                ReadoutModeForNormalImages = Cam.ReadoutModeForNormalImages,
                                ReadoutModeForSnapImages = Cam.ReadoutModeForSnapImages,
                                ReadoutModes = Cam.ReadoutModes.Cast<string>().ToList(),
                                SensorType = Cam.SensorType,
                                BayerOffsetX = Cam.BayerOffsetX,
                                BayerOffsetY = Cam.BayerOffsetY,
                                DefaultGain = DefaultGain,
                                DefaultOffset = DefaultOffset,
                                USBLimitMin = Cam.USBLimitMin,
                                USBLimitMax = Cam.USBLimitMax,
                                CanSetUSBLimit = Cam.CanSetUSBLimit,
                                USBLimit = Cam.USBLimit,
                                SupportedActions = Cam.SupportedActions,
                            };

                            Notification.ShowSuccess(Loc.Instance["LblCameraConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            _ = updateTimer.Run();

                            profileService.ActiveProfile.CameraSettings.Id = this.Cam.Id;
                            if (Cam.PixelSizeX > 0) {
                                profileService.ActiveProfile.CameraSettings.PixelSize = Cam.PixelSizeX;
                            }

                            BroadcastCameraInfo();

                            if (Cam.CanSetTemperature) {
                                CoolingDuration = this.profileService.ActiveProfile.CameraSettings.CoolingDuration;
                                WarmingDuration = this.profileService.ActiveProfile.CameraSettings.WarmingDuration;
                                TargetTemp = this.profileService.ActiveProfile.CameraSettings.Temperature ?? -10;
                            }

                            await (Connected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                            Logger.Info($"Successfully connected Camera. Id: {Cam.Id} Name: {Cam.Name} DisplayName: {Cam.DisplayName}Driver Version: {Cam.DriverVersion}");

                            return true;
                        } else {
                            Notification.ShowError(string.Format(Loc.Instance["LblUnableToConnectCamera"], cam.Name));
                            this.Cam = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (cam?.Connected == true) { await Disconnect(); }
                        CameraInfo.Connected = false;
                        return false;
                    } catch (Exception ex) {
                        Notification.ShowError(ex.Message);
                        Logger.Error(ex);
                        if (CameraInfo.Connected) { await Disconnect(); }
                        CameraInfo.Connected = false;
                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get {
                if (cameraInfo == null) {
                    cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
                }
                return cameraInfo;
            }
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private void ToggleDewHeaterOn(object o) {
            if (CameraInfo.Connected) {
                Cam.DewHeaterOn = (bool)o;
            }
        }

        private void BroadcastCameraInfo() {
            cameraMediator.Broadcast(CameraInfo);
        }

        private void CancelConnectCamera(object o) {
            try { _cancelConnectCameraSource?.Cancel(); } catch { }
        }

        private void UpdateCameraValues(Dictionary<string, object> cameraValues) {
            object o = null;
            cameraValues.TryGetValue(nameof(CameraInfo.Connected), out o);
            CameraInfo.Connected = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CameraInfo.CoolerOn), out o);
            CameraInfo.CoolerOn = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CameraInfo.Temperature), out o);
            CameraInfo.Temperature = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraInfo.CoolerPower), out o);
            CameraInfo.CoolerPower = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraInfo.DewHeaterOn), out o);
            CameraInfo.DewHeaterOn = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CameraInfo.CameraState), out o);
            CameraInfo.CameraState = (CameraStates)(o ?? CameraStates.NoState);

            cameraValues.TryGetValue(nameof(CameraInfo.Battery), out o);
            CameraInfo.Battery = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.Offset), out o);
            CameraInfo.Offset = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.USBLimit), out o);
            CameraInfo.USBLimit = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.TemperatureSetPoint), out o);
            CameraInfo.TemperatureSetPoint = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraInfo.ElectronsPerADU), out o);
            CameraInfo.ElectronsPerADU = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraInfo.SubSampleX), out o);
            CameraInfo.SubSampleX = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.SubSampleY), out o);
            CameraInfo.SubSampleY = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.SubSampleWidth), out o);
            CameraInfo.SubSampleWidth = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.SubSampleHeight), out o);
            CameraInfo.SubSampleHeight = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.ReadoutMode), out o);
            CameraInfo.ReadoutMode = Convert.ToInt16(o ?? 0);

            cameraValues.TryGetValue(nameof(CameraInfo.Gain), out o);
            CameraInfo.Gain = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.DefaultGain), out o);
            CameraInfo.DefaultGain = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.DefaultOffset), out o);
            CameraInfo.DefaultOffset = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.ExposureMin), out o);
            CameraInfo.ExposureMin = (double)(o ?? 0.0d);

            cameraValues.TryGetValue(nameof(CameraInfo.PixelSize), out o);
            CameraInfo.PixelSize = (double)(o ?? 0.0d);

            CoolerHistory.Add(new CameraCoolingStep(OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Now), CameraInfo.Temperature, CameraInfo.CoolerPower));
            CoolerHistoryMax = Math.Max(CameraInfo.Temperature, CoolerHistoryMax);
            CoolerHistoryMin = Math.Min(CameraInfo.Temperature, CoolerHistoryMin);

            BroadcastCameraInfo();
        }

        private Dictionary<string, object> GetCameraValues() {
            Dictionary<string, object> cameraValues = new Dictionary<string, object>();
            cameraValues.Add(nameof(CameraInfo.Connected), _cam?.Connected ?? false);
            cameraValues.Add(nameof(CameraInfo.CoolerOn), _cam?.CoolerOn ?? false);
            cameraValues.Add(nameof(CameraInfo.Temperature), _cam?.Temperature ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.CoolerPower), _cam?.CoolerPower ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.DewHeaterOn), _cam?.DewHeaterOn ?? false);
            cameraValues.Add(nameof(CameraInfo.CameraState), _cam?.CameraState ?? CameraStates.NoState);
            cameraValues.Add(nameof(CameraInfo.TemperatureSetPoint), _cam?.TemperatureSetPoint ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.ElectronsPerADU), _cam?.ElectronsPerADU ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.SubSampleX), _cam?.SubSampleX ?? -1);
            cameraValues.Add(nameof(CameraInfo.SubSampleY), _cam?.SubSampleY ?? -1);
            cameraValues.Add(nameof(CameraInfo.SubSampleWidth), _cam?.SubSampleWidth ?? -1);
            cameraValues.Add(nameof(CameraInfo.SubSampleHeight), _cam?.SubSampleHeight ?? -1);
            cameraValues.Add(nameof(CameraInfo.ReadoutMode), _cam?.ReadoutMode ?? 0);
            cameraValues.Add(nameof(CameraInfo.ExposureMin), _cam?.ExposureMin ?? 0);
            cameraValues.Add(nameof(CameraInfo.PixelSize), _cam?.PixelSizeX ?? 0);

            if (_cam != null && _cam.CanSetGain) {
                cameraValues.Add(nameof(CameraInfo.Gain), _cam?.Gain ?? -1);
                cameraValues.Add(nameof(CameraInfo.DefaultGain), DefaultGain);
            }

            if (_cam != null && _cam.CanSetOffset) {
                cameraValues.Add(nameof(CameraInfo.Offset), _cam?.Offset ?? -1);
                cameraValues.Add(nameof(CameraInfo.DefaultOffset), DefaultOffset);
            }

            if (_cam != null && _cam.CanSetUSBLimit) {
                cameraValues.Add(nameof(CameraInfo.USBLimit), _cam?.USBLimit ?? -1);
            }

            if (_cam != null && _cam.HasBattery) {
                cameraValues.Add(nameof(CameraInfo.Battery), _cam?.BatteryLevel ?? -1);
            }

            return cameraValues;
        }

        private DeviceUpdateTimer updateTimer;

        private CancellationTokenSource _cancelConnectCameraSource;

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.Show(Loc.Instance["LblDisconnectCamera"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public async Task Disconnect() {
            try {
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }
                try { _cancelChangeTemperatureCts?.Cancel(); } catch { }
                TempChangeRunning = false;
                Cam?.Disconnect();
                Cam = null;
                CameraInfo.Reset();
                BroadcastCameraInfo();
                await (Disconnected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                Logger.Info("Disconnected Camera");
            } catch (Exception ex) {
                Logger.Error(ex);
            }

        }

        public IAsyncEnumerable<IExposureData> LiveView(CaptureSequence sequence, CancellationToken ct) {
            return new AsyncEnumerable<IExposureData>(async yield => {
                if (CameraInfo.Connected) {
                    try {
                        SetGain(sequence.Gain);
                        SetOffset(sequence.Offset);
                        if (sequence.Binning == null) {
                            SetBinning(1, 1);
                        } else {
                            SetBinning(sequence.Binning.X, sequence.Binning.Y);
                        }

                        if (sequence.EnableSubSample) {
                            SetSubSampleArea((int)sequence.SubSambleRectangle.X, (int)sequence.SubSambleRectangle.Y, (int)sequence.SubSambleRectangle.Width, (int)sequence.SubSambleRectangle.Height);
                        } else {
                            Cam.EnableSubSample = false;
                        }

                        _cam.StartLiveView(sequence);

                        while (true) {
                            var iarr = await _cam.DownloadLiveView(ct);

                            await yield.ReturnAsync(iarr);

                            ct.ThrowIfCancellationRequested();
                        }
                    } catch (OperationCanceledException) {
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    } finally {
                        _cam.StopLiveView();
                    }
                }
            });
        }

        public int DefaultGain {
            get => profileService.ActiveProfile.CameraSettings.Gain ?? -1;
            set {
                if (profileService.ActiveProfile.CameraSettings.Gain != value) {
                    profileService.ActiveProfile.CameraSettings.Gain = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int DefaultOffset {
            get => profileService.ActiveProfile.CameraSettings.Offset ?? -1;
            set {
                if (profileService.ActiveProfile.CameraSettings.Offset != value) {
                    profileService.ActiveProfile.CameraSettings.Offset = value;
                    RaisePropertyChanged();
                }
            }
        }

        public async Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress) {
            if (CameraInfo.Connected == true) {
                SetGain(sequence.Gain);
                SetOffset(sequence.Offset);
                if (sequence.Binning == null) {
                    SetBinning(1, 1);
                } else {
                    SetBinning(sequence.Binning.X, sequence.Binning.Y);
                }

                if (sequence.EnableSubSample) {
                    SetSubSampleArea((int)sequence.SubSambleRectangle.X, (int)sequence.SubSambleRectangle.Y, (int)sequence.SubSambleRectangle.Width, (int)sequence.SubSambleRectangle.Height);
                } else {
                    Cam.EnableSubSample = false;
                }

                CameraInfo.IsExposing = true;
                CameraInfo.ExposureEndTime = DateTime.Now.AddSeconds(sequence.ExposureTime);
                BroadcastCameraInfo();

                if (sequence.ExposureTime < ExposureMin) {
                    Logger.Info($"Sequence exposure time {sequence.ExposureTime} is less than the camera minimum. Increasing to {ExposureMin}");
                    sequence.ExposureTime = ExposureMin;
                }
                this.exposureTime = sequence.ExposureTime;
                double exposureTime = sequence.ExposureTime;
                Logger.Info($"Starting Exposure - Exposure Time: {exposureTime}s; Filter: {sequence.FilterType?.Name}; Gain: {CameraInfo.Gain}; Offset {CameraInfo.Offset}; Binning: {CameraInfo.BinX}x{CameraInfo.BinY};");

                Cam.StartExposure(sequence);

                /* Wait for the camera to report image ready and report progress of exposure in remaining seconds */
                using (var exposureReadyCts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                    using (var progressCountCts = CancellationTokenSource.CreateLinkedTokenSource(exposureReadyCts.Token)) {
                        if (exposureTime >= 1) {
                            progress.Report(new ApplicationStatus() {
                                Status = Loc.Instance["LblExposing"],
                                Progress = 0,
                                MaxProgress = (int)exposureTime,
                                ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue
                            });
                            /* Report progress of exposure in parallel to waiting for exposure ready event.*/
                            _ = CoreUtil.Wait(TimeSpan.FromSeconds(exposureTime), progressCountCts.Token, progress, Loc.Instance["LblExposing"]);
                        } else {
                            progress.Report(new ApplicationStatus() {
                                Status = Loc.Instance["LblExposing"]
                            });
                        }

                        exposureReadyCts.CancelAfter(TimeSpan.FromSeconds(exposureTime + profileService.ActiveProfile.CameraSettings.Timeout));
                        try {
                            await Cam.WaitUntilExposureIsReady(exposureReadyCts.Token);
                        } catch (OperationCanceledException) {
                            Console.WriteLine("Parent token cancelled: " + token.IsCancellationRequested);
                            Console.WriteLine("Child token cancelled: " + exposureReadyCts.Token.IsCancellationRequested);
                            if (!token.IsCancellationRequested) {
                                var downloadFailedException = new CameraDownloadFailedException(sequence, $"Camera Timeout - Camera did not set image as ready after exposuretime + {profileService.ActiveProfile.CameraSettings.Timeout} seconds");
                                Logger.Error(downloadFailedException);
                                await (DownloadTimeout?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                                throw downloadFailedException;
                            }
                        } finally {
                            try { progressCountCts?.Cancel(); } catch { }
                            progress.Report(new ApplicationStatus() {
                                Status = Loc.Instance["LblExposureFinished"]
                            });
                        }
                    }
                }

                token.ThrowIfCancellationRequested();
                CameraInfo.IsExposing = false;
                if (Cam != null) {
                    Cam.EnableSubSample = false;
                }
                BroadcastCameraInfo();
            }
        }

        public void SetReadoutMode(short mode) {
            if (CameraInfo.Connected && mode >= 0 && mode < CameraInfo.ReadoutModes.Count()) {
                Cam.ReadoutMode = CameraInfo.ReadoutMode = mode;
                BroadcastCameraInfo();
            }
        }

        public void SetUSBLimit(int usbLimit) {
            if (CameraInfo.Connected && CameraInfo.CanSetUSBLimit && usbLimit >= CameraInfo.USBLimitMin && usbLimit <= CameraInfo.USBLimitMax) {
                Cam.USBLimit = usbLimit;
                CameraInfo.USBLimit = usbLimit;
                BroadcastCameraInfo();
            }
        }

        public void SetReadoutModeForNormalImages(short mode) {
            if (CameraInfo.Connected && mode >= 0 && mode < CameraInfo.ReadoutModes.Count()) {
                Cam.ReadoutModeForNormalImages = CameraInfo.ReadoutModeForNormalImages = mode;
                BroadcastCameraInfo();
            }
        }

        public void SetBinning(short x, short y) {
            Cam.SetBinning(x, y);
            CameraInfo.BinX = Cam.BinX;
            CameraInfo.BinY = Cam.BinY;
            BroadcastCameraInfo();
        }

        public void AbortExposure() {
            if (CameraInfo.Connected == true) {
                Cam?.AbortExposure();
                BroadcastCameraInfo();
            }

            CameraInfo.IsExposing = false;
            CameraInfo.ExposureEndTime = DateTime.Now;
        }

        private void SetGain(int gain) {
            if (CameraInfo.Connected == true) {
                if (gain > -1) {
                    Cam.Gain = gain;
                } else {
                    Cam.Gain = DefaultGain;
                }

                CameraInfo.Gain = Cam.Gain;
                CameraInfo.ElectronsPerADU = Cam.ElectronsPerADU;
                BroadcastCameraInfo();
            }
        }

        private void SetOffset(int offset) {
            if (CameraInfo.Connected == true) {
                if (offset > -1) {
                    Cam.Offset = offset;
                } else {
                    Cam.Offset = DefaultOffset;
                }

                CameraInfo.Offset = Cam.Offset;
                BroadcastCameraInfo();
            }
        }

        public void SetDewHeater(bool onOff) {
            if (CameraInfo.Connected == true && CameraInfo.HasDewHeater == true) {
                Cam.DewHeaterOn = onOff;
                BroadcastCameraInfo();
            }
        }

        public async Task<IExposureData> Download(CancellationToken token) {
            CameraInfo.IsExposing = false;
            CameraInfo.ExposureEndTime = DateTime.Now;
            BroadcastCameraInfo();
            if (CameraInfo.Connected == true) {
                Stopwatch seqDuration = Stopwatch.StartNew();
                var output = await Cam.DownloadExposure(token);
                seqDuration.Stop();
                CameraInfo.LastDownloadTime = seqDuration.Elapsed.TotalSeconds;
                BroadcastCameraInfo();
                if (output != null) {
                    output.MetaData.FromProfile(this.profileService.ActiveProfile);
                    output.MetaData.Image.ExposureTime = this.exposureTime;
                }
                return output;
            } else {
                return null;
            }
        }

        private void SetSubSampleArea(int x, int y, int width, int height) {
            if (CameraInfo.Connected == true && CameraInfo.CanSubSample) {
                if (x < 0) { x = 0; }
                if (y < 0) { y = 0; }
                if (x + width > CameraInfo.XSize) { width = CameraInfo.XSize - x; }
                if (y + height > CameraInfo.YSize) { height = CameraInfo.YSize - y; }

                if (CameraInfo.SensorType != Core.Enum.SensorType.Monochrome) {
                    if (x % 2 > 0) {
                        x += 1;
                    }
                    if (width % 2 > 0) {
                        width -= 1;
                    }
                    if (y % 2 > 0) {
                        y += 1;
                    }
                    if (height % 2 > 0) {
                        height -= 1;
                    }
                }

                if (x >= CameraInfo.XSize || y >= CameraInfo.YSize) {
                    Logger.Warning($"Invalid subsample dimensions for camera {x}x{y} {width}x{height}.");
                    Cam.EnableSubSample = false;
                } else {
                    Cam.EnableSubSample = true;
                    Cam.SubSampleX = x;
                    Cam.SubSampleY = y;
                    Cam.SubSampleWidth = width;
                    Cam.SubSampleHeight = height;
                }
            }
        }

        public bool AtTargetTemp => Math.Abs(cameraInfo.Temperature - TargetTemp) <= 2;

        public double ExposureMin {
            get {
                if (Cam?.Connected != true) {
                    return 0.0;
                }
                // Guard against bad values reported by a driver
                return (double.IsNaN(Cam.ExposureMin) || Cam.ExposureMin > 30) ? 0.0 : Math.Max(0.0, Cam.ExposureMin);
            }
        }

        public Task<bool> Connect() {
            return ChooseCamera();
        }

        public CameraInfo GetDeviceInfo() {
            return CameraInfo;
        }

        public void SetTemperature(double temperature) {
            if (Cam.CanSetTemperature) {
                Cam.TemperatureSetPoint = temperature;
            }
        }

        public void SetCooler(bool onOff) {
            Cam.CoolerOn = onOff;
        }

        public string Action(string actionName, string actionParameters) {
            if (Cam?.Connected == true) {
                return Cam.Action(actionName, actionParameters);
            } else {
                Notification.ShowError(Loc.Instance["LblTelescopeNotConnectedForCommand"] + ": " + actionName);
                return null;
            }
        }

        public string SendCommandString(string command, bool raw = true) {
            if (Cam?.Connected == true) {
                return Cam.SendCommandString(command, raw);
            } else {
                Notification.ShowError(Loc.Instance["LblTelescopeNotConnectedForCommand"] + ": " + command);
                return null;
            }
        }

        public bool SendCommandBool(string command, bool raw = true) {
            if (Cam?.Connected == true) {
                return Cam.SendCommandBool(command, raw);
            } else {
                Notification.ShowError(Loc.Instance["LblTelescopeNotConnectedForCommand"] + ": " + command);
                return false;
            }
        }

        public void SendCommandBlind(string command, bool raw = true) {
            if (Cam?.Connected == true) {
                Cam.SendCommandBlind(command, raw);
            } else {
                Notification.ShowError(Loc.Instance["LblTelescopeNotConnectedForCommand"] + ": " + command);
            }
        }

        public IDevice GetDevice() {
            return Cam;
        }

        public AsyncObservableLimitedSizedStack<CameraCoolingStep> CoolerHistory { get; private set; }

        [ObservableProperty]
        private double coolerHistoryMax;
        [ObservableProperty]
        private double coolerHistoryMin;

        public IAsyncCommand CoolCamCommand { get; private set; }
        public IAsyncCommand WarmCamCommand { get; private set; }
        public ICommand ToggleDewHeaterOnCommand { get; private set; }

        private IApplicationStatusMediator applicationStatusMediator;
        private double exposureTime;

        public event Func<object, EventArgs, Task> Connected;
        public event Func<object, EventArgs, Task> Disconnected;
        public event Func<object, EventArgs, Task> DownloadTimeout;

        public IAsyncCommand ConnectCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public ICommand CancelCoolCamCommand { get; private set; }

        public IAsyncCommand RescanDevicesCommand { get; private set; }
        public ICommand CancelConnectCommand { get; private set; }
    }

    public class CameraCoolingStep { 

        public CameraCoolingStep(double date, double temperature, double power) {
            Date = date;
            Temperature = temperature;
            Power = power;
        }
        public double Date { get; }

        public double Power { get; }

        public double Temperature { get; }
    }
}