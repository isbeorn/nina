using NINA.Model;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;

namespace NINA.ViewModel {
    class ApplicationVM : BaseVM {

        public ApplicationVM() {



            ExitCommand = new RelayCommand(ExitApplication);
            MinimizeWindowCommand = new RelayCommand(MinimizeWindow);
            MaximizeWindowCommand = new RelayCommand(MaximizeWindow);
            Visibility = true;
            Views = new ObservableCollection<BaseVM>();
            //Views.Add(this);
            Name = "Menu";
            _activeView = this.CameraVM;
            

            var cam = this.CameraVM;
            
            this.ImagingVM.CameraVM = cam;            
            this.FrameFocusVM.ImagingVM = this.ImagingVM;
            var ps = this.PlatesolveVM;
            ps.ImagingVM = this.ImagingVM;            
            var tele = this.TelescopeVM;
            ps.TelescopeVM = tele;
            var phd2 = this.PHD2VM;

            var polarVM = this.PolarAlignVM;
            polarVM.TelescopeVM = tele;
            polarVM.PlatesolveVM = ps;
            polarVM.ImagingVM = ImagingVM;
            //this.FrameFocusVM.Cam = cam.Cam;
            //this.FrameFocusVM.FW = cam.FilterWheelVM.FW;
            // var a = this.TelescopeVM;
            //addListeners();


        }     


        public string Version {
            get {               
                return "v. 0.3.0";
            }
        }

        public new bool Visibility {
            get {
                return _visibility;
            } set {
                _visibility = value;

                if(_visibility == true) {
                    this.OverViewVisibility = false;
                    if (_activeView != null)
                        _activeView.Visibility = false;
                } else {
                    if (_activeView != null)
                        _activeView.Visibility = true;
                }

                RaisePropertyChanged();                
            }
        }
        



        private static void MaximizeWindow(object obj) {
            if (Application.Current.MainWindow.WindowState == WindowState.Maximized) {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
            else {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeWindow(object obj) {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void ExitApplication(object obj) {
            if(CameraVM.Cam != null && CameraVM.Cam.Connected) {
                System.Windows.MessageBoxResult diag = System.Windows.MessageBox.Show("Camera still connected. Exit anyway?", "", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                if(diag == MessageBoxResult.OK) {
                    Application.Current.Shutdown();
                }
            } else {
                Application.Current.Shutdown();
            }
            
        }

        private void AddListeners() {
            
        }
        



        // protected void syncModel(object sender, PropertyChangedEventArgs e) {
        //this.ImagingVM.Cam = this.CameraVM.Cam;            
        //  }

        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        private bool _overViewVisibility;
        public bool OverViewVisibility {
            get {
                return _overViewVisibility;
            }
            set {
                _overViewVisibility = value;
                if (_overViewVisibility == true) {
                    this.Visibility = false;
                    if(_activeView != null) 
                        _activeView.Visibility = false;
                } else {
                    if (_activeView != null)
                        _activeView.Visibility = true;
                }                
                RaisePropertyChanged();
            }
        }        

        private ObservableCollection<BaseVM> _views;
        private BaseVM _activeView;

        private CameraVM _cameraVM;
        public CameraVM CameraVM {
            get {
                if(_cameraVM == null) {
                    _cameraVM = new CameraVM();
                    Views.Add(_cameraVM);

                }
                return _cameraVM;
            }
            set {
                _cameraVM = value;
                RaisePropertyChanged();
            }
        }

        private ImagingVM _imagingVM;
        public ImagingVM ImagingVM {
            get {
                if (_imagingVM == null) {
                    _imagingVM = new ImagingVM();
                    Views.Add(_imagingVM);
                }
                return _imagingVM;
            }
            set {
                _imagingVM = value;
                RaisePropertyChanged();
            }
        }

        private PolarAlignmentVM _polarAlignVM;
        public PolarAlignmentVM PolarAlignVM {
            get {
                if (_polarAlignVM == null) {
                    _polarAlignVM = new PolarAlignmentVM();
                    Views.Add(_polarAlignVM);
                }
                return _polarAlignVM;
            } set {
                _polarAlignVM = value;
                RaisePropertyChanged();
            }
        }

        private PlatesolveVM _platesolveVM;
        public PlatesolveVM PlatesolveVM {
            get {
                if (_platesolveVM == null) {
                    _platesolveVM = new PlatesolveVM();
                    Views.Add(_platesolveVM);
                }
                return _platesolveVM;
            }
            set {
                _platesolveVM = value;
                RaisePropertyChanged();
            }
        }

        private TelescopeVM _telescopeVM;
        public TelescopeVM TelescopeVM {
            get {
                if (_telescopeVM == null) {
                    _telescopeVM = new TelescopeVM();
                    Views.Add(_telescopeVM);
                }
                return _telescopeVM;
            }
            set {
                _telescopeVM = value;
                RaisePropertyChanged();
            }
        }

        private PHD2VM _phd2VM;
        public PHD2VM PHD2VM {
            get {
                if (_phd2VM == null) {
                    _phd2VM = new PHD2VM();
                    Views.Add(_phd2VM);
                }
                return _phd2VM;
            }
            set {                
                _phd2VM = value;
                RaisePropertyChanged();
            }
        }

        private OptionsVM _optionsVM;
        public OptionsVM OptionsVM {
            get {
                if (_optionsVM == null) {
                    _optionsVM = new OptionsVM();
                    Views.Add(_optionsVM);
                }
                return _optionsVM;
            }
            set {
                _optionsVM = value;
                RaisePropertyChanged();
            }
        }

       

        private FrameFocusVM _frameFocusVM;
        public FrameFocusVM FrameFocusVM {
            get {
                if (_frameFocusVM == null) {
                    _frameFocusVM = new FrameFocusVM();
                    Views.Add(_frameFocusVM);
                }
                return _frameFocusVM;
            }
            set {
                _frameFocusVM = value;
                RaisePropertyChanged();
            }
        }

        private void ToggleView(object o) {
            if (o != null) {
                this.Visibility = false;
                this.OverViewVisibility = false;
                BaseVM a = _activeView;
                BaseVM b = null;
                
                this.OverViewVisibility = false;
                b = GetViewByName(o.ToString());
                                
                if (a != null && b != null ) {
                    a.Visibility = false;
                    b.Visibility = true;
                    _activeView = b;
                }
            }

        }

        private void ToggleMenu(object o) {
            this.OverViewVisibility = false;
            if (this.Visibility) {
                _activeView.Visibility = true;
                this.Visibility = false;
            }
            else {
                _activeView.Visibility = false;
                this.Visibility = true;
            }
        }

        private void ToggleOverview(object o) {
            this.Visibility = false;
            if(this.OverViewVisibility) {
                _activeView.Visibility = true;
                this.OverViewVisibility = false;
            } else {
                _activeView.Visibility = false;
                this.OverViewVisibility = true;
            }
            
        }

        private void GetNextView(object o) {
            this.Visibility = false;
            this.OverViewVisibility = false;
            BaseVM a = _activeView;
            a.Visibility = false;
            int idx = Views.IndexOf(a);
            idx = (idx + 1) % Views.Count;
            Views[idx].Visibility = true;
            _activeView = Views[idx];         
        }

        private void GetPrevView(object o) {
            this.Visibility = false;         
            this.OverViewVisibility = false;
            BaseVM a = _activeView;
            a.Visibility = false;
            int idx = Views.IndexOf(a);
            idx = (idx - 1) % Views.Count;
            if (idx < 0) idx = Views.Count - 1;
            Views[idx].Visibility = true;
            _activeView = Views[idx];
        }

       

        private BaseVM GetViewByName(String name) {
            var a = (from b in Views where b.Name == name select b);
            if (a.Count() > 0) {
                return a.First();
            }
            else {
                return null;
            }
        }

                private ICommand _nextViewCommand;
        public ICommand NextViewCommand {
            get {
                return _nextViewCommand;
            }
            set {
                _nextViewCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _pevViewCommand;
        public ICommand PrevViewCommand {
            get {
                return _pevViewCommand;
            }
            set {
                _pevViewCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _toggleViewCommand;
        public ICommand ToggleViewCommand {
            get {
                return _toggleViewCommand;
            }

            set {
                _toggleViewCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _toggleMenuCommand;
        public ICommand ToggleMenuCommand {
            get {
                return _toggleMenuCommand;
            }

            set {
                _toggleMenuCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _toggleOverviewCommand;
        public ICommand ToggleOverviewCommand {
            get {
                return _toggleOverviewCommand;
            }

            set {
                _toggleOverviewCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _minimizeWindowCommand;
        private ICommand _maximizeWindowCommand;
        private ICommand _exitCommand;
        public ICommand MinimizeWindowCommand {
            get {
                return _minimizeWindowCommand;
            }

            set {
                _minimizeWindowCommand = value;
                RaisePropertyChanged();
            }
        }

        public ICommand MaximizeWindowCommand {
            get {
                return _maximizeWindowCommand;
            }

            set {
                _maximizeWindowCommand = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ExitCommand {
            get {
                return _exitCommand;
            }

            set {
                _exitCommand = value;
                RaisePropertyChanged();
            }
        }

        

        public ObservableCollection<BaseVM> Views {
            get {
                return _views;
            }

            set {
                _views = value;
                RaisePropertyChanged();
            }
        }

        
    }
}
