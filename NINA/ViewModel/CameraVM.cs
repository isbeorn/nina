using ASCOM.DriverAccess;
using EDSDKLib;
using Nikon;
using NINA.EquipmentChooser;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.ViewModel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using ZWOptical.ASISDK;

namespace NINA.ViewModel {
    class CameraVM : DockableVM {

        public CameraVM() : base() {
            Title = "LblCamera";
            ContentId = nameof(CameraVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CameraSVG"];

            //ConnectCameraCommand = new RelayCommand(connectCamera);
            ChooseCameraCommand = new RelayCommand(ChooseCamera);
            DisconnectCommand = new RelayCommand(DisconnectDiag);
            CoolCamCommand = new AsyncCommand<bool>(() => CoolCamera(new Progress<double>(p => CoolingProgress = p)));
            CancelCoolCamCommand = new RelayCommand(CancelCoolCamera);
            RefreshCameraListCommand = new RelayCommand(RefreshCameraList);
            
            CoolingRunning = false;
            CoolerPowerHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);
            CCDTemperatureHistory = new AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>>(100);

        }

        private void RefreshCameraList(object obj) {
            CameraChooserVM.GetEquipment();
        }

        private void CoolCamera_Tick(IProgress<double> progress) {

            double currentTemp = Cam.CCDTemperature;
            double deltaTemp = currentTemp - TargetTemp;


            DateTime now = DateTime.Now;
            TimeSpan delta = now.Subtract(_deltaT);

            Duration = Duration - ((double)delta.TotalMilliseconds / (1000 * 60));

            if(Duration < 0) { Duration = 0; }
                        
            double newTemp = GetY(_startPoint, _endPoint, new Vector2(-_startPoint.X, _startPoint.Y), Duration);
            Cam.SetCCDTemperature = newTemp;

            progress.Report(1 - (Duration / _initalDuration));

            _deltaT = DateTime.Now;


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


        private DateTime _deltaT;

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
            Cam.CoolerOn = true;
            if (Duration == 0) {
                Cam.SetCCDTemperature = TargetTemp;
                progress.Report(1);
            } else {
                try {


                    _deltaT = DateTime.Now;
                    double currentTemp = Cam.CCDTemperature;
                    _startPoint = new Vector2(Duration, currentTemp);
                    _endPoint = new Vector2(0, TargetTemp);
                    Cam.SetCCDTemperature = currentTemp;
                    _initalDuration = Duration;
                    
                    CoolingRunning = true;
                    do {
                        CoolCamera_Tick(progress);
                        await Task.Delay(TimeSpan.FromMilliseconds(300), _cancelCoolCameraSource.Token);
                        _cancelCoolCameraSource.Token.ThrowIfCancellationRequested();
                    } while (Duration > 0);


                } catch (OperationCanceledException ex) {
                    Cam.SetCCDTemperature = Cam.CCDTemperature;
                    Logger.Trace(ex.Message);

                } finally {
                    progress.Report(1);
                    Duration = 0;
                    CoolingRunning = false;
                }
            }
            return true;

        }

        private void CancelCoolCamera(object o) {
            _cancelCoolCameraSource?.Cancel();
        }

        private BackgroundWorker _updateCameraWorker;        


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
            set {
                _cam = value;
                Mediator.Instance.Notify(MediatorMessages.CameraChanged, _cam);
            }
        }

        private void ChooseCamera(object obj) {
            Cam = (ICamera)CameraChooserVM.SelectedDevice;
            if (Cam?.Connect() == true) {
                Connected = true;
                RaisePropertyChanged(nameof(Cam));
                _updateCameraWorker?.CancelAsync();
                _updateCameraWorker = new BackgroundWorker();
                _updateCameraWorker.WorkerReportsProgress = true;
                _updateCameraWorker.WorkerSupportsCancellation = true;
                _updateCameraWorker.DoWork += _updateCameraWorker_DoWork;
                _updateCameraWorker.ProgressChanged += _updateCameraWorker_ProgressChanged;
                _updateCameraWorker.RunWorkerAsync();


                Settings.CameraId = Cam.Id;
            } else {
                Cam = null;
            }
        }

        private void _updateCameraWorker_ProgressChanged(object sender,ProgressChangedEventArgs e) {
            var cameraValues = (Dictionary<string,object>)e.UserState;

            object o = null;
            cameraValues.TryGetValue(nameof(Connected),out o);
            Connected = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CoolerOn),out o);
            CoolerOn = (bool)(o ?? false);

            cameraValues.TryGetValue(nameof(CCDTemperature),out o);
            CCDTemperature = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CoolerPower),out o);
            CoolerPower = (double)(o ?? double.NaN);

            cameraValues.TryGetValue(nameof(CameraState),out o);
            CameraState = (string)(o ?? string.Empty);

            DateTime x = DateTime.Now;
            CoolerPowerHistory.Add(new KeyValuePair<DateTime,double>(x,CoolerPower));
            CCDTemperatureHistory.Add(new KeyValuePair<DateTime,double>(x,CCDTemperature));
        }

        private void _updateCameraWorker_DoWork(object sender,DoWorkEventArgs e) {
            Dictionary<string,object> cameraValues = new Dictionary<string,object>();
            do {
                if (_updateCameraWorker.CancellationPending) {
                    break;
                }

                Stopwatch sw = Stopwatch.StartNew();
                cameraValues.Clear();
                cameraValues.Add(nameof(Connected),_cam?.Connected ?? false);
                cameraValues.Add(nameof(CoolerOn),_cam?.CoolerOn ?? false);
                cameraValues.Add(nameof(CCDTemperature),_cam?.CCDTemperature ?? double.NaN);
                cameraValues.Add(nameof(CoolerPower),_cam?.CoolerPower ?? double.NaN);
                cameraValues.Add(nameof(CameraState),_cam?.CameraState ?? string.Empty);
                

                //cameraValues.Add(nameof(FullWellCapacity),_cam?.FullWellCapacity ?? double.NaN);
                //cameraValues.Add(nameof(HeatSinkTemperature),_cam?.HeatSinkTemperature ?? false);
                //cameraValues.Add(nameof(IsPulseGuiding),_cam?.IsPulseGuiding ?? false);

                _updateCameraWorker.ReportProgress(0,cameraValues);

                if (_updateCameraWorker.CancellationPending) {
                    break;
                }

                var elapsed = (int)sw.Elapsed.TotalMilliseconds;

                //Update after one second + the time it takes to read the values
                Thread.Sleep(elapsed + 500);

            } while (Connected == true);

            cameraValues.Clear();
            cameraValues.Add(nameof(Connected),false);
            _updateCameraWorker.ReportProgress(0,cameraValues);
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
                if(prevVal != _connected) {
                    Mediator.Instance.Notify(MediatorMessages.CameraConnectedChanged,_connected);
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
                Mediator.Instance.Notify(MediatorMessages.CameraStateChanged,_connected);
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
                Mediator.Instance.Notify(MediatorMessages.CameraTemperatureChanged,_cCDTemperature);
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
                Mediator.Instance.Notify(MediatorMessages.CameraCoolerPowerChanged,_coolerPower);
            }
        }

        private bool _coolerOn;
        public bool CoolerOn {
            get {
                return _coolerOn;
            }
            set {
                _coolerOn = value;
                if(_cam?.Connected == true) {
                    _cam.CoolerOn = value;
                }               
                
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.CameraCoolerPowerChanged,_coolerOn);
            }
        }


        private void DisconnectDiag(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Camera?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {
            _updateCameraWorker?.CancelAsync();
            _cancelCoolCameraSource?.Cancel();
            CoolingRunning = false;            
            Cam?.Disconnect();
            Cam = null;
        }

        void UpdateCamera_Tick(object sender, EventArgs e) {
            if (Cam.Connected) {
                Cam.UpdateValues();

                DateTime x = DateTime.Now;
                CoolerPowerHistory.Add(new KeyValuePair<DateTime, double>(x, Cam.CoolerPower));
                CCDTemperatureHistory.Add(new KeyValuePair<DateTime, double>(x, Cam.CCDTemperature));

            }

        }


        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CoolerPowerHistory { get; private set; }
        public AsyncObservableLimitedSizedStack<KeyValuePair<DateTime, double>> CCDTemperatureHistory { get; private set; }

        public ICommand CoolCamCommand { get; private set; }

        public ICommand ChooseCameraCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public ICommand CancelCoolCamCommand { get; private set; }

        public ICommand RefreshCameraListCommand { get; private set; }
    }

    class CameraChooserVM : EquipmentChooserVM {
        public CameraChooserVM() : base() {
            
        }

        public override void GetEquipment() {
            Devices.Clear();

            /* ASI */
            Logger.Trace("Adding ASI Cameras");
            for (int i = 0; i < ASICameras.Count; i++) {
                var cam = ASICameras.GetCamera(i);
                if (cam.Name != "") {
                    Logger.Trace("Adding " + cam.Name);
                    Devices.Add(cam);
                }
            }

            /* ASCOM */
            var ascomDevices = new ASCOM.Utilities.Profile();
            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Camera")) {

                try {
                    AscomCamera cam = new AscomCamera(device.Key, device.Value + " (ASCOM)");
                    Logger.Trace("Adding " + cam.Name);
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

                    Logger.Trace("Adding " + info.szDeviceDescription);
                    Devices.Add(new EDCamera(cam, info));
                }
            }

            /* NIKON */
            Devices.Add(new NikonCamera());

            if (Devices.Count > 0) {
                var items = (from device in Devices where device.Id == Settings.CameraId select device);
                if (items.Count() > 0) {
                    SelectedDevice = items.First();

                } else {
                    SelectedDevice = Devices.First();
                }
            }
        }

        
    }
}
