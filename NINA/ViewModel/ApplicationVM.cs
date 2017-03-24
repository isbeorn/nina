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
            
            Name = "Menu";
        }     


        public string Version {
            get {               
                return "v. 0.3.1";
            }
        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
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
        
        
        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        
        private CameraVM _cameraVM;
        public CameraVM CameraVM {
            get {
                if(_cameraVM == null) {
                    _cameraVM = new CameraVM(this);

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
                    _imagingVM = new ImagingVM(this);
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
                    _polarAlignVM = new PolarAlignmentVM(this);
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
                    _platesolveVM = new PlatesolveVM(this);
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
                    _telescopeVM = new TelescopeVM(this);
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
                    _phd2VM = new PHD2VM(this);
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
                    _frameFocusVM = new FrameFocusVM(this);
                }
                return _frameFocusVM;
            }
            set {
                _frameFocusVM = value;
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

        
        
        
    }
}
