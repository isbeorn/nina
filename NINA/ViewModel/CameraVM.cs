using EDSDKLib;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.AtikSDK;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ZWOptical.ASISDK;

namespace NINA.ViewModel {

    internal class CameraVM : DockableVM {

        public CameraVM() : base() {
            Title = "LblCamera";
            ContentId = nameof(CameraVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CameraSVG"];

            //ConnectCameraCommand = new RelayCommand(connectCamera);
            ChooseCameraCommand = new AsyncCommand<bool>(ChooseCamera);
            CancelConnectCameraCommand = new RelayCommand(CancelConnectCamera);
            DisconnectCommand = new RelayCommand(DisconnectDiag);
            CoolCamCommand = new AsyncCommand<bool>(() => CoolCamera(new Progress<double>(p => CoolingProgress = p)));
            CancelCoolCamCommand = new RelayCommand(CancelCoolCamera);
            RefreshCameraListCommand = new RelayCommand(RefreshCameraList);

            CoolingRunning = false;
            CoolerPowerHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);
            CCDTemperatureHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);

            Mediator.Instance.RegisterAsyncRequest(
                new ConnectCameraMessageHandle(async (ConnectCameraMessage msg) => {
                    await ChooseCameraCommand.ExecuteAsync(null);
                    return true;
                })
            );

            Mediator.Instance.Register((o) => { RefreshCameraList(o); }, MediatorMessages.ProfileChanged);
        }

        private void RefreshCameraList(object obj) {
            CameraChooserVM.GetEquipment();
        }

        private async Task CoolCamera_Tick(IProgress<double> progress, CancellationToken token) {
            double currentTemp = Cam.CCDTemperature;
            double deltaTemp = currentTemp - TargetTemp;

            var delta = await Utility.Utility.Delay(300, token);

            Duration = Duration - ((double)delta.TotalMilliseconds / (1000 * 60));

            if (Duration < 0) { Duration = 0; }

            double newTemp = GetY(_startPoint, _endPoint, new Vector2(-_startPoint.X, _startPoint.Y), Duration);
            Cam.SetCCDTemperature = newTemp;

            var percentage = 1 - (Duration / _initalDuration);
            progress.Report(percentage);

            Mediator.Instance.Request(new StatusUpdateMessage() {
                Status = new ApplicationStatus() {
                    Source = Title,
                    Status = Locale.Loc.Instance["LblCooling"],
                    Progress = percentage
                }
            });
        }

        private CameraChooserVM _cameraChooserVM;

        public CameraChooserVM CameraChooserVM {
            get {
                if (_cameraChooserVM == null) {
                    _cameraChooserVM = new CameraChooserVM();
                }
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

        private double _initalDuration;
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

        private async Task<bool> CoolCamera(IProgress<double> progress) {
            _cancelCoolCameraSource = new CancellationTokenSource();
            return await Task<bool>.Run(async () => {
                Cam.CoolerOn = true;
                if (Duration == 0) {
                    Cam.SetCCDTemperature = TargetTemp;
                    progress.Report(1);
                } else {
                    try {
                        double currentTemp = Cam.CCDTemperature;
                        _startPoint = new Vector2(Duration, currentTemp);
                        _endPoint = new Vector2(0, TargetTemp);
                        Cam.SetCCDTemperature = currentTemp;
                        _initalDuration = Duration;

                        CoolingRunning = true;
                        do {
                            await CoolCamera_Tick(progress, _cancelCoolCameraSource.Token);

                            _cancelCoolCameraSource.Token.ThrowIfCancellationRequested();
                        } while (Duration > 0);
                    } catch (OperationCanceledException ex) {
                        Cam.SetCCDTemperature = Cam.CCDTemperature;
                        Logger.Trace(ex.Message);
                    } finally {
                        progress.Report(1);
                        Duration = 0;
                        CoolingRunning = false;
                        Mediator.Instance.Request(new StatusUpdateMessage() {
                            Status = new ApplicationStatus() {
                                Source = Title,
                                Status = string.Empty
                            }
                        });
                    }
                }
                return true;
            });
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
                Disconnect();
                _cancelUpdateCameraValues?.Cancel();

                if (CameraChooserVM.SelectedDevice.Id == "No_Device") {
                    ProfileManager.Instance.ActiveProfile.CameraSettings.Id = CameraChooserVM.SelectedDevice.Id;
                    return false;
                }

                Mediator.Instance.Request(new StatusUpdateMessage() {
                    Status = new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                });

                var cam = (ICamera)CameraChooserVM.SelectedDevice;
                _cancelConnectCameraSource = new CancellationTokenSource();
                if (cam != null) {
                    try {
                        var connected = await cam.Connect(_cancelConnectCameraSource.Token);
                        _cancelConnectCameraSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.Cam = cam;
                            Connected = true;
                            if (Cam.CanSetCCDTemperature) {
                                TargetTemp = Cam.SetCCDTemperature;
                            }

                            Notification.ShowSuccess(Locale.Loc.Instance["LblCameraConnected"]);

                            _updateCameraValuesProgress = new Progress<Dictionary<string, object>>(UpdateCameraValues);
                            _cancelUpdateCameraValues = new CancellationTokenSource();
                            _updateCameraValuesTask = Task.Run(() => GetCameraValues(_updateCameraValuesProgress, _cancelUpdateCameraValues.Token));

                            ProfileManager.Instance.ActiveProfile.CameraSettings.Id = this.Cam.Id;
                            if (Cam.PixelSizeX > 0) {
                                ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize = Cam.PixelSizeX;
                                Mediator.Instance.Notify(MediatorMessages.CameraPixelSizeChanged, Cam.PixelSizeX);
                            }

                            Mediator.Instance.Notify(MediatorMessages.CameraChanged, Cam);
                            return true;
                        } else {
                            this.Cam = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (Connected) { Disconnect(); }
                        Connected = false;
                        return false;
                    }
                } else {
                    return false;
                }
            } finally {
                ss.Release();
                Mediator.Instance.Request(new StatusUpdateMessage() {
                    Status = new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                });
            }
        }

        private void CancelConnectCamera(object o) {
            _cancelConnectCameraSource?.Cancel();
        }

        private void UpdateCameraValues(Dictionary<string, object> cameraValues) {
            object o = null;
            cameraValues.TryGetValue(nameof(Connected), out o);
            Connected = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CoolerOn), out o);
            CoolerOn = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CCDTemperature), out o);
            CCDTemperature = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CoolerPower), out o);
            CoolerPower = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraState), out o);
            CameraState = (string)(o ?? string.Empty);

            DateTime x = DateTime.Now;
            CoolerPowerHistory.Add(new KeyValuePair<DateTime, double>(x, CoolerPower));
            CCDTemperatureHistory.Add(new KeyValuePair<DateTime, double>(x, CCDTemperature));
        }

        private void GetCameraValues(IProgress<Dictionary<string, object>> progress, CancellationToken token) {
            Dictionary<string, object> cameraValues = new Dictionary<string, object>();
            try {
                do {
                    token.ThrowIfCancellationRequested();

                    cameraValues.Clear();
                    cameraValues.Add(nameof(Connected), _cam?.Connected ?? false);
                    cameraValues.Add(nameof(CoolerOn), _cam?.CoolerOn ?? false);
                    cameraValues.Add(nameof(CCDTemperature), _cam?.CCDTemperature ?? double.NaN);
                    cameraValues.Add(nameof(CoolerPower), _cam?.CoolerPower ?? double.NaN);
                    cameraValues.Add(nameof(CameraState), _cam?.CameraState ?? string.Empty);

                    //cameraValues.Add(nameof(FullWellCapacity),_cam?.FullWellCapacity ?? double.NaN);
                    //cameraValues.Add(nameof(HeatSinkTemperature),_cam?.HeatSinkTemperature ?? false);
                    //cameraValues.Add(nameof(IsPulseGuiding),_cam?.IsPulseGuiding ?? false);

                    progress.Report(cameraValues);

                    token.ThrowIfCancellationRequested();

                    //Update after one second + the time it takes to read the values
                    Thread.Sleep((int)(ProfileManager.Instance.ActiveProfile.ApplicationSettings.DevicePollingInterval * 1000));
                } while (Connected == true);
            } catch (OperationCanceledException) {
            } finally {
                cameraValues.Clear();
                cameraValues.Add(nameof(Connected), false);
                progress.Report(cameraValues);
            }
        }

        private bool _connected;

        public bool Connected {
            get {
                return _connected;
            }
            private set {
                var prevVal = _connected;
                _connected = value;
                RaisePropertyChanged();
                if (prevVal != _connected) {
                    Mediator.Instance.Notify(MediatorMessages.CameraConnectedChanged, _connected);
                }
            }
        }

        private string _cameraState;

        public string CameraState {
            get {
                return _cameraState;
            }
            private set {
                _cameraState = value;
                RaisePropertyChanged();
            }
        }

        private double _cCDTemperature;

        public double CCDTemperature {
            get {
                return _cCDTemperature;
            }
            private set {
                _cCDTemperature = value;
                RaisePropertyChanged();
            }
        }

        private double _coolerPower;

        public double CoolerPower {
            get {
                return _coolerPower;
            }
            private set {
                _coolerPower = value;
                RaisePropertyChanged();
            }
        }

        private bool _coolerOn;
        private IProgress<Dictionary<string, object>> _updateCameraValuesProgress;
        private CancellationTokenSource _cancelUpdateCameraValues;
        private Task _updateCameraValuesTask;
        private CancellationTokenSource _cancelConnectCameraSource;

        public bool CoolerOn {
            get {
                return _coolerOn;
            }
            set {
                _coolerOn = value;
                if (Connected == true) {
                    Cam.CoolerOn = value;
                }

                RaisePropertyChanged();
            }
        }

        private void DisconnectDiag(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Camera?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {
            _cancelUpdateCameraValues?.Cancel();
            _cancelCoolCameraSource?.Cancel();
            do {
                Task.Delay(100);
            } while (!_updateCameraValuesTask?.IsCompleted == true);
            CoolingRunning = false;
            Cam?.Disconnect();
            Cam = null;

            Mediator.Instance.Notify(MediatorMessages.CameraChanged, null);
        }

        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CoolerPowerHistory { get; private set; }
        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CCDTemperatureHistory { get; private set; }

        public ICommand CoolCamCommand { get; private set; }

        public IAsyncCommand ChooseCameraCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public ICommand CancelCoolCamCommand { get; private set; }

        public ICommand RefreshCameraListCommand { get; private set; }
        public ICommand CancelConnectCameraCommand { get; private set; }
    }

    internal class CameraChooserVM : EquipmentChooserVM {

        public CameraChooserVM() : base(typeof(CameraChooserVM)) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new Model.DummyDevice(Locale.Loc.Instance["LblNoCamera"]));

            /* ASI */
            Logger.Trace("Adding ASI Cameras");
            for (int i = 0; i < ASICameras.Count; i++) {
                var cam = ASICameras.GetCamera(i);
                if (!string.IsNullOrEmpty(cam.Name)) {
                    Logger.Trace(string.Format("Adding {0}", cam.Name));
                    Devices.Add(cam);
                }
            }

            /* ASCOM */
            var ascomDevices = new ASCOM.Utilities.Profile();
            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Camera")) {
                try {
                    AscomCamera cam = new AscomCamera(device.Key, device.Value + " (ASCOM)");
                    Logger.Trace(string.Format("Adding {0}", cam.Name));
                    Devices.Add(cam);
                } catch (Exception) {
                    //only add cameras which are supported. e.g. x86 drivers will not work in x64
                }
            }

            /* CANON */
            IntPtr cameraList;
            uint err = EDSDK.EdsGetCameraList(out cameraList);
            if (err == (uint)EDSDK.EDS_ERR.OK) {
                int count;
                err = EDSDK.EdsGetChildCount(cameraList, out count);

                for (int i = 0; i < count; i++) {
                    IntPtr cam;
                    err = EDSDK.EdsGetChildAtIndex(cameraList, i, out cam);

                    EDSDK.EdsDeviceInfo info;
                    err = EDSDK.EdsGetDeviceInfo(cam, out info);

                    Logger.Trace(string.Format("Adding {0}", info.szDeviceDescription));
                    Devices.Add(new EDCamera(cam, info));
                }
            }

            /* NIKON */
            Devices.Add(new NikonCamera());

            /* Atik */
            Logger.Trace("Adding Atik Cameras");
            var atikDevices = AtikCameraDll.RefreshDevicesCount();
            for (int i = 0; i < atikDevices; i++) {
                if (AtikCameraDll.ArtemisDeviceIsCamera(i)) {
                    var cam = new AtikCamera(i);
                    Devices.Add(cam);
                }
            }

            DetermineSelectedDevice(ProfileManager.Instance.ActiveProfile.CameraSettings.Id);
        }
    }
}