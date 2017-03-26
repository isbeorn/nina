using ASCOM.DriverAccess;
using NINA.Utility;
using NINA.ViewModel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class CameraVM : ChildVM {

        public CameraVM(ApplicationVM root) : base(root){
            Name = "Camera";
            ImageGeometry  = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["CameraSVG"];

            //ConnectCameraCommand = new RelayCommand(connectCamera);
            ChooseCameraCommand = new RelayCommand(ChooseCamera);
            DisconnectCommand = new RelayCommand(DisconnectCamera);
            CoolCamCommand = new AsyncCommand<bool>(() => CoolCamera(new Progress<double>(p => CoolingProgress = p)));
            CancelCoolCamCommand = new RelayCommand(CancelCoolCamera);
            
            _updateCamera = new DispatcherTimer();
            _updateCamera.Interval = TimeSpan.FromMilliseconds(1000);
            _updateCamera.Tick += UpdateCamera_Tick;
            
            CoolingRunning = false;
            CoolerPowerHistory = new AsyncObservableCollection<KeyValuePair<DateTime, double>>();
            CCDTemperatureHistory = new AsyncObservableCollection<KeyValuePair<DateTime, double>>();
        }

        
        private void CoolCamera_Tick(IProgress<double> progress) {           

            double currentTemp = Cam.CCDTemperature;
            double deltaTemp = currentTemp - TargetTemp;

            
            DateTime now = DateTime.Now;
            TimeSpan delta = now.Subtract(_deltaT);

            Duration = Duration - ((double)delta.TotalMilliseconds / (1000 * 60));

            //double newTemp = GetY(_startPoint, _endPoint, Duration);
            double newTemp = GetY(_startPoint, _endPoint, new Vector2(-_startPoint.X, _startPoint.Y), Duration);
            Cam.SetCCDTemperature = newTemp;
           
            progress.Report(1 - (Duration / _initalDuration));

            _deltaT = DateTime.Now;

            
        }

        private class Vector2 {
            public double X;
            public double Y;

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

        private FilterWheelVM _filterWheelVM;
        public FilterWheelVM FilterWheelVM {
            get {
                if (_filterWheelVM == null) {
                    _filterWheelVM = new FilterWheelVM(RootVM);                    
                }
                return _filterWheelVM;
            }
            set {
                _filterWheelVM = value;
                RaisePropertyChanged();
            }
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
            } set {
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

                    //CoolCameraTimer.Start();

                    CoolingRunning = true;
                    do {
                        CoolCamera_Tick(progress);
                        await Task.Delay(TimeSpan.FromMilliseconds(300));
                        _cancelCoolCameraSource.Token.ThrowIfCancellationRequested();
                    } while (Duration >= 0);
                                                
                    
                } catch(OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                    
                } finally {
                    progress.Report(1);
                    Duration = 0;
                    Cam.SetCCDTemperature = Cam.CCDTemperature;
                    CoolingRunning = false;
                }
            }
            return true;

        }

        private void CancelCoolCamera(object o) {
            if(_cancelCoolCameraSource != null) {
                _cancelCoolCameraSource.Cancel();
            }
        }
            

        DispatcherTimer _updateCamera;


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
            }
        }

        /*private void connectCamera(object obj) {
            Cam.AscomCamera = new Camera(Cam.ProgId);
        }*/

        private void ChooseCamera(object obj) {

            /*var i = ZWOptical.ASISDK.ASICameras.Count;
            Cam = ZWOptical.ASISDK.ASICameras.GetCamera(i - 1);
            if(Cam.Connect()) {
                _updateCamera.Start();
                RaisePropertyChanged("Cam");
            }*/

            string cameraid = Settings.CameraId;
            var id = ASCOM.DriverAccess.Camera.Choose(cameraid);
            if (id != "") {
                Cam = new Model.MyCamera.AscomCamera(id);
                if (Cam.Connect()) {
                    Settings.CameraId = id;
                    _updateCamera.Start();
                }

                RaisePropertyChanged("Cam");
            }
        }

        private void DisconnectCamera(object obj) {
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show("Disconnect Camera?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.Cancel);
            if(result == System.Windows.MessageBoxResult.OK) {
                _updateCamera.Stop();
                if(_cancelCoolCameraSource != null) {
                    _cancelCoolCameraSource.Cancel();
                }                
                CoolingRunning = false;
                Cam.Disconnect();
            }
        }

        void UpdateCamera_Tick(object sender, EventArgs e) {
           if(Cam.Connected) {
                Cam.UpdateValues();
                
                DateTime x = DateTime.Now;
                if (CoolerPowerHistory.Count > 100) {
                    CoolerPowerHistory.RemoveAt(0);
                }
                CoolerPowerHistory.Add(new KeyValuePair<DateTime, double>(x, Cam.CoolerPower));

                if (CCDTemperatureHistory.Count > 100) {
                    CCDTemperatureHistory.RemoveAt(0);
                }
                CCDTemperatureHistory.Add(new KeyValuePair<DateTime, double>(x, Cam.CCDTemperature));
                
            }
            
        }


        public AsyncObservableCollection<KeyValuePair<DateTime, double>> CoolerPowerHistory { get; private set; }
        public AsyncObservableCollection<KeyValuePair<DateTime, double>> CCDTemperatureHistory { get; private set; }




       


        private ICommand _coolCamCommand;
        public ICommand CoolCamCommand {
            get {
                return _coolCamCommand;
            }
            set {
                _coolCamCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _chooseCameraCommand;
        public ICommand ChooseCameraCommand {
            get {
                return _chooseCameraCommand; 
            } set {
                _chooseCameraCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _disconnectCommand;
        public ICommand DisconnectCommand {
            get {
                return _disconnectCommand;
            }
            set {
                _disconnectCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _cancelCoolCommand;
        public ICommand CancelCoolCamCommand {
            get { return _cancelCoolCommand; }
            private set { _cancelCoolCommand = value; RaisePropertyChanged(); } }

        
    }
}
