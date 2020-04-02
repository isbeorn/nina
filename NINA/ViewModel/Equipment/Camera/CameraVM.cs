#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.Camera {

    internal class CameraVM : DockableVM, ICameraVM {

        public CameraVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblCamera";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CameraSVG"];

            _cameraChooserVM = new CameraChooserVM(profileService, telescopeMediator);
            _cameraChooserVM.GetEquipment();

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;

            ChooseCameraCommand = new AsyncCommand<bool>(ChooseCamera);
            CancelConnectCameraCommand = new RelayCommand(CancelConnectCamera);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectDiag());
            CoolCamCommand = new AsyncCommand<bool>(() => StartCoolCamera(new Progress<double>(p => CoolingProgress = p)));
            CancelCoolCamCommand = new RelayCommand(CancelCoolCamera);
            RefreshCameraListCommand = new RelayCommand(RefreshCameraList, o => !(Cam?.Connected == true));

            CoolingRunning = false;
            WarmingRunning = false;
            CoolerPowerHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);
            CCDTemperatureHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);
            ToggleCoolerOnCommand = new RelayCommand(ToggleCoolerOn);
            ToggleDewHeaterOnCommand = new RelayCommand(ToggleDewHeaterOn);

            updateTimer = new DeviceUpdateTimer(
                GetCameraValues,
                UpdateCameraValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshCameraList(null);
            };
        }

        private void RefreshCameraList(object obj) {
            CameraChooserVM.GetEquipment();
        }

        private async Task WaitForTargetTemperatureStep(TimeSpan maximumStepDuration, double targetTemperatureStep, double percentage, CancellationToken token) {
            var interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
            var threshold = 1;
            TimeSpan timeWaited = TimeSpan.Zero;
            double temperature = 0.0;
            while ((Math.Abs((temperature = cameraInfo.Temperature) - targetTemperatureStep) > threshold) && timeWaited < maximumStepDuration) {
                if (CoolingRunning) {
                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = Locale.Loc.Instance["LblCooling"],
                            Progress = percentage,
                            Status2 = Locale.Loc.Instance["LblWaitForTemperatureStep"]
                        }
                    );
                } else {
                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = Locale.Loc.Instance["LblWarming"],
                            Progress = percentage,
                            Status2 = Locale.Loc.Instance["LblWaitForTemperatureStep"]
                        }
                    );
                    if (CameraInfo.CoolerPower == 0) {
                        _remainingDuration = TimeSpan.Zero;
                        break;
                    }
                }
                timeWaited = timeWaited + await Utility.Utility.Wait(TimeSpan.FromSeconds(interval), token);
            }
        }

        private async Task<double> GetNextTemperatureStep(CancellationToken token) {
            var interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
            var delta = await Utility.Utility.Delay(TimeSpan.FromSeconds(interval), token);

            _remainingDuration = _remainingDuration - delta;
            if (_remainingDuration < TimeSpan.Zero) { _remainingDuration = TimeSpan.Zero; }

            return GetY(_startPoint, _endPoint, _remainingDuration.TotalMilliseconds);
        }

        private async Task SetNextTemperatureStep(TimeSpan maximumDurationPerDegree, IProgress<double> progress, CancellationToken token) {
            var targetTemperatureStep = await GetNextTemperatureStep(token);

            TimeSpan maximumStepDuration = TimeSpan.FromMilliseconds(Math.Round(maximumDurationPerDegree.TotalMilliseconds * Math.Abs(cameraInfo.Temperature - targetTemperatureStep)));

            Cam.TemperatureSetPoint = targetTemperatureStep;

            var percentage = 1 - ((double)_remainingDuration.TotalMilliseconds / _initalDuration.TotalMilliseconds);

            //Use the camera set point here (some cameras like the ASI cameras can only set integer values and lose precision)
            await WaitForTargetTemperatureStep(maximumStepDuration, Cam.TemperatureSetPoint, percentage, token);

            progress.Report(percentage);

            if (CoolingRunning) {
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblCooling"],
                        Progress = percentage
                    }
                );
            } else {
                applicationStatusMediator.StatusUpdate(
                new ApplicationStatus() {
                    Source = Title,
                    Status = Locale.Loc.Instance["LblWarming"],
                    Progress = percentage
                }
            );
            }
        }

        private ICameraMediator cameraMediator;

        private CameraChooserVM _cameraChooserVM;

        public CameraChooserVM CameraChooserVM {
            get {
                return _cameraChooserVM;
            }
            set {
                _cameraChooserVM = value;
            }
        }

        private class Vector2 {
            public double X { get; private set; }
            public double Y { get; private set; }

            public Vector2(double x, double y) {
                X = x;
                Y = y;
            }
        }

        private double GetY(Vector2 point1, Vector2 point2, double x) {
            var m = (point2.Y - point1.Y) / (point2.X - point1.X);
            var b = point1.Y - (m * point1.X);

            return m * x + b;
        }

        private double GetY(Vector2 point1, Vector2 point2, Vector2 point3, double x) {
            double denom = (point1.X - point2.X) * (point1.X - point3.X) * (point2.X - point3.X);
            double A = (point3.X * (point2.Y - point1.Y) + point2.X * (point1.Y - point3.Y) + point1.X * (point3.Y - point2.Y)) / denom;
            double B = (point3.X * point3.X * (point1.Y - point2.Y) + point2.X * point2.X * (point3.Y - point1.Y) + point1.X * point1.X * (point2.Y - point3.Y)) / denom;
            double C = (point2.X * point3.X * (point2.X - point3.X) * point1.Y + point3.X * point1.X * (point3.X - point1.X) * point2.Y + point1.X * point2.X * (point1.X - point2.X) * point3.Y) / denom;

            return (A * Math.Pow(x, 2) + B * x + C);
        }

        private Vector2 _startPoint;
        private Vector2 _endPoint;

        private TimeSpan _initalDuration;
        private TimeSpan _remainingDuration;
        private double _coolingProgress;

        public double CoolingProgress {
            get {
                return _coolingProgress;
            }

            set {
                _coolingProgress = value;
                RaisePropertyChanged();
            }
        }

        private bool _coolingRunning;

        public bool CoolingRunning {
            get {
                return _coolingRunning;
            }
            set {
                _coolingRunning = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource _cancelCoolCameraSource;

        private async Task<bool> StartCoolCamera(IProgress<double> progress) {
            _cancelCoolCameraSource?.Dispose();
            _cancelCoolCameraSource = new CancellationTokenSource();
            return await StartChangeCameraTemp(progress, TargetTemp, TimeSpan.FromMinutes(Duration), false, _cancelCoolCameraSource.Token);
        }

        public async Task<bool> StartChangeCameraTemp(IProgress<double> progress, double temperature, TimeSpan duration, bool turnOffCooler, CancellationToken cancelChangeCameraTempToken) {
            _remainingDuration = duration;
            TimeSpan maximumDurationPerDegree = TimeSpan.FromMilliseconds(Math.Round(1.2 * (double)duration.TotalMilliseconds / Math.Abs(cameraInfo.Temperature - temperature)));
            return await Task<bool>.Run(async () => {
                if (_remainingDuration == TimeSpan.Zero) {
                    Cam.TemperatureSetPoint = temperature;
                    Cam.CoolerOn = true;
                    progress.Report(1);
                } else {
                    try {
                        double currentTemp = Cam.Temperature;
                        _startPoint = new Vector2(_remainingDuration.TotalMilliseconds, currentTemp);
                        _endPoint = new Vector2(0, temperature);
                        Cam.TemperatureSetPoint = currentTemp;
                        _initalDuration = _remainingDuration;

                        Cam.CoolerOn = true;
                        if (temperature >= currentTemp) {
                            WarmingRunning = true;
                            CoolingRunning = false;
                        } else {
                            CoolingRunning = true;
                            WarmingRunning = false;
                        }
                        do {
                            await SetNextTemperatureStep(maximumDurationPerDegree, progress, cancelChangeCameraTempToken);
                            cancelChangeCameraTempToken.ThrowIfCancellationRequested();
                        } while (_remainingDuration > TimeSpan.Zero);
                        if (turnOffCooler) {
                            //adding a delay - Some cams seem to not like to immediately have their cooler set to off
                            await Utility.Utility.Delay(TimeSpan.FromSeconds(10), cancelChangeCameraTempToken);
                            Cam.CoolerOn = false;
                        }
                    } catch (OperationCanceledException ex) {
                        Cam.TemperatureSetPoint = Cam.Temperature;
                        Logger.Trace(ex.Message);
                    } finally {
                        progress.Report(1);
                        _remainingDuration = TimeSpan.Zero;
                        WarmingRunning = false;
                        CoolingRunning = false;
                        applicationStatusMediator.StatusUpdate(
                            new ApplicationStatus() {
                                Source = Title,
                                Status = string.Empty
                            }
                        );
                    }
                }
                return true;
            });
        }

        private bool _warmingRunning;

        public bool WarmingRunning {
            get {
                return _warmingRunning;
            }
            set {
                _warmingRunning = value;
                RaisePropertyChanged();
            }
        }

        private void CancelCoolCamera(object o) {
            _cancelCoolCameraSource?.Cancel();
        }

        private double _targetTemp;

        public double TargetTemp {
            get {
                return _targetTemp;
            }
            set {
                _targetTemp = value;
                RaisePropertyChanged();
            }
        }

        private double _duration;

        public double Duration {
            get {
                return _duration;
            }
            set {
                _duration = value;
                RaisePropertyChanged();
            }
        }

        private Model.MyCamera.ICamera _cam;

        public Model.MyCamera.ICamera Cam {
            get {
                return _cam;
            }
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

                if (CameraChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.CameraSettings.Id = CameraChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );

                var cam = (ICamera)CameraChooserVM.SelectedDevice;
                _cancelConnectCameraSource?.Dispose();
                _cancelConnectCameraSource = new CancellationTokenSource();
                if (cam != null) {
                    try {
                        var connected = await cam.Connect(_cancelConnectCameraSource.Token);
                        _cancelConnectCameraSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.Cam = cam;

                            CameraInfo = new CameraInfo {
                                BinX = Cam.BinX,
                                BinY = Cam.BinY,
                                CameraState = Cam.CameraState,
                                CanSubSample = Cam.CanSubSample,
                                SubSampleX = Cam.SubSampleX,
                                SubSampleY = Cam.SubSampleY,
                                SubSampleWidth = Cam.SubSampleWidth,
                                SubSampleHeight = Cam.SubSampleHeight,
                                Connected = true,
                                CoolerOn = Cam.CoolerOn,
                                CoolerPower = Cam.CoolerPower,
                                DewHeaterOn = Cam.DewHeaterOn,
                                Gain = Cam.Gain,
                                HasShutter = Cam.HasShutter,
                                CanSetTemperature = Cam.CanSetTemperature,
                                IsSubSampleEnabled = Cam.EnableSubSample,
                                Name = Cam.Name,
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
                                BayerOffsetY = Cam.BayerOffsetY
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblCameraConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            profileService.ActiveProfile.CameraSettings.Id = this.Cam.Id;
                            if (Cam.PixelSizeX > 0) {
                                profileService.ActiveProfile.CameraSettings.PixelSize = Cam.PixelSizeX;
                            }

                            BroadcastCameraInfo();

                            if (Cam.CanSetTemperature) {
                                TargetTemp = Cam.TemperatureSetPoint;
                            }

                            Logger.Info($"Successfully connected Camera. Id: {Cam.Id} Name: {Cam.Name} Driver Version: {Cam.DriverVersion}");

                            return true;
                        } else {
                            this.Cam = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (CameraInfo.Connected) { await Disconnect(); }
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

        private void ToggleCoolerOn(object o) {
            if (CameraInfo.Connected) {
                Cam.CoolerOn = (bool)o;
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
            _cancelConnectCameraSource?.Cancel();
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
            CameraInfo.CameraState = (string)(o ?? string.Empty);

            cameraValues.TryGetValue(nameof(CameraInfo.Battery), out o);
            CameraInfo.Battery = (int)(o ?? -1);

            cameraValues.TryGetValue(nameof(CameraInfo.Offset), out o);
            CameraInfo.Offset = (int)(o ?? -1);

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

            DateTime x = DateTime.Now;
            CoolerPowerHistory.Add(new KeyValuePair<DateTime, double>(x, CameraInfo.CoolerPower));
            CCDTemperatureHistory.Add(new KeyValuePair<DateTime, double>(x, CameraInfo.Temperature));

            BroadcastCameraInfo();
        }

        private Dictionary<string, object> GetCameraValues() {
            Dictionary<string, object> cameraValues = new Dictionary<string, object>();
            cameraValues.Add(nameof(CameraInfo.Connected), _cam?.Connected ?? false);
            cameraValues.Add(nameof(CameraInfo.CoolerOn), _cam?.CoolerOn ?? false);
            cameraValues.Add(nameof(CameraInfo.Temperature), _cam?.Temperature ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.CoolerPower), _cam?.CoolerPower ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.DewHeaterOn), _cam?.DewHeaterOn ?? false);
            cameraValues.Add(nameof(CameraInfo.CameraState), _cam?.CameraState ?? string.Empty);
            cameraValues.Add(nameof(CameraInfo.TemperatureSetPoint), _cam?.TemperatureSetPoint ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.ElectronsPerADU), _cam?.ElectronsPerADU ?? double.NaN);
            cameraValues.Add(nameof(CameraInfo.SubSampleX), _cam?.SubSampleX ?? -1);
            cameraValues.Add(nameof(CameraInfo.SubSampleY), _cam?.SubSampleY ?? -1);
            cameraValues.Add(nameof(CameraInfo.SubSampleWidth), _cam?.SubSampleWidth ?? -1);
            cameraValues.Add(nameof(CameraInfo.SubSampleHeight), _cam?.SubSampleHeight ?? -1);
            cameraValues.Add(nameof(CameraInfo.ReadoutMode), _cam?.ReadoutMode ?? 0);

            if (_cam != null && _cam.CanSetOffset) {
                cameraValues.Add(nameof(CameraInfo.Offset), _cam?.Offset ?? -1);
            }

            if (_cam != null && _cam.HasBattery) {
                cameraValues.Add(nameof(CameraInfo.Battery), _cam?.BatteryLevel ?? -1);
            }

            return cameraValues;
        }

        private DeviceUpdateTimer updateTimer;

        private CancellationTokenSource _cancelConnectCameraSource;

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Camera?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public async Task Disconnect() {
            if (Cam != null) { Logger.Info("Disconnected Camera"); }
            if (updateTimer != null) {
                await updateTimer.Stop();
            }
            _cancelCoolCameraSource?.Cancel();
            CoolingRunning = false;
            WarmingRunning = false;
            Cam?.Disconnect();
            Cam = null;
            CameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
            BroadcastCameraInfo();
        }

        public IAsyncEnumerable<int> GetValues() {
            return new AsyncEnumerable<int>(async yield => {
                await _cam.DownloadLiveView(new CancellationToken()).ConfigureAwait(false);

                // Yes, it's even needed for 'yield.ReturnAsync'
                await yield.ReturnAsync(123).ConfigureAwait(false);
            });
        }

        public IAsyncEnumerable<IExposureData> LiveView(CancellationToken ct) {
            return new AsyncEnumerable<IExposureData>(async yield => {
                if (CameraInfo.Connected && _cam.CanShowLiveView) {
                    try {
                        _cam.StartLiveView();

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

        public async Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress) {
            this.exposureTime = sequence.ExposureTime;
            double exposureTime = sequence.ExposureTime;
            if (CameraInfo.Connected == true) {
                if (sequence.Gain > -1) {
                    SetGain(sequence.Gain);
                }

                if (sequence.Offset > -1) {
                    SetOffset(sequence.Offset);
                }

                if (sequence.Binning == null) {
                    SetBinning(1, 1);
                } else {
                    SetBinning(sequence.Binning.X, sequence.Binning.Y);
                }

                SetSubSample(sequence.EnableSubSample);

                CameraInfo.IsExposing = true;
                CameraInfo.ExposureEndTime = DateTime.Now.AddSeconds(sequence.ExposureTime);
                CameraInfo.NextExposureLength = sequence.NextSequence?.ExposureTime ?? -1;
                BroadcastCameraInfo();

                Logger.Debug($"Starting Exposure - Exposure Time: {exposureTime}s; Gain: {CameraInfo.Gain}; Offset {CameraInfo.Offset}; Binning: {CameraInfo.BinX};");

                Cam.StartExposure(sequence);

                /* Wait for the camera to report image ready and report progress of exposure in remaining seconds */
                using (var exposureReadyCts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                    using (var progressCountCts = CancellationTokenSource.CreateLinkedTokenSource(exposureReadyCts.Token)) {
                        if (exposureTime >= 1) {
                            progress.Report(new ApplicationStatus() {
                                Status = Locale.Loc.Instance["LblExposing"],
                                Progress = 0,
                                MaxProgress = (int)exposureTime,
                                ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue
                            });
                            /* Report progress of exposure in parallel to waiting for exposure ready event.*/
                            _ = Utility.Utility.Wait(TimeSpan.FromSeconds(exposureTime), progressCountCts.Token, progress, Locale.Loc.Instance["LblExposing"]);
                        } else {
                            progress.Report(new ApplicationStatus() {
                                Status = Locale.Loc.Instance["LblExposing"]
                            });
                        }

                        exposureReadyCts.CancelAfter(TimeSpan.FromSeconds(exposureTime + 15));
                        try {
                            await Cam.WaitUntilExposureIsReady(exposureReadyCts.Token);
                        } catch (OperationCanceledException) {
                            Console.WriteLine("Parent token cancelled: " + token.IsCancellationRequested);
                            Console.WriteLine("Child token cancelled: " + exposureReadyCts.Token.IsCancellationRequested);
                            if (!token.IsCancellationRequested) {
                                Logger.Error("Camera Timeout - Camera did not set image as ready after exposuretime + 15 seconds");
                                Notification.ShowError(Locale.Loc.Instance["LblCameraTimeout"]);
                            }
                        } finally {
                            progressCountCts.Cancel();
                            progress.Report(new ApplicationStatus() {
                                Status = Locale.Loc.Instance["LblExposureFinished"]
                            });
                        }
                    }
                }

                token.ThrowIfCancellationRequested();
                CameraInfo.IsExposing = false;
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

        public void SetGain(int gain) {
            if (CameraInfo.Connected == true) {
                Cam.Gain = gain;
                CameraInfo.Gain = Cam.Gain;
                CameraInfo.ElectronsPerADU = Cam.ElectronsPerADU;
                BroadcastCameraInfo();
            }
        }

        public void SetOffset(int offset) {
            if (CameraInfo.Connected == true) {
                Cam.Offset = offset;
                CameraInfo.Offset = Cam.Offset;
                BroadcastCameraInfo();
            }
        }

        public void SetSubSample(bool subSample) {
            if (CameraInfo.Connected == true) {
                Cam.EnableSubSample = subSample;
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
                    output.MetaData.FromCameraInfo(this.CameraInfo);
                    output.MetaData.Image.ExposureTime = this.exposureTime;
                }
                return output;
            } else {
                return null;
            }
        }

        public void SetSubSampleArea(int x, int y, int width, int height) {
            if (CameraInfo.Connected == true && CameraInfo.CanSubSample) {
                Cam.SubSampleX = x;
                Cam.SubSampleY = y;
                Cam.SubSampleWidth = width;
                Cam.SubSampleHeight = height;
            }
        }

        public bool AtTargetTemp {
            get {
                return Math.Abs(cameraInfo.Temperature - TargetTemp) <= 2;
            }
        }

        public Task<bool> Connect() {
            return ChooseCamera();
        }

        public CameraInfo GetDeviceInfo() {
            return CameraInfo;
        }

        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CoolerPowerHistory { get; private set; }
        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CCDTemperatureHistory { get; private set; }
        public ICommand ToggleCoolerOnCommand { get; private set; }
        public ICommand CoolCamCommand { get; private set; }
        public ICommand ToggleDewHeaterOnCommand { get; private set; }

        private IApplicationStatusMediator applicationStatusMediator;
        private double exposureTime;

        public IAsyncCommand ChooseCameraCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public ICommand CancelCoolCamCommand { get; private set; }

        public ICommand RefreshCameraListCommand { get; private set; }
        public ICommand CancelConnectCameraCommand { get; private set; }
    }
}