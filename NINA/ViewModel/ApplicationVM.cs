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
    class ApplicationVM : DockManagerVM {

        public ApplicationVM() {
            ExitCommand = new RelayCommand(ExitApplication);
            MinimizeWindowCommand = new RelayCommand(MinimizeWindow);
            MaximizeWindowCommand = new RelayCommand(MaximizeWindow);
            LoadAvalonDockLayoutCommand = new RelayCommand(LoadAvalonDockLayout);
            RegisterMediatorMessages();

            InitAvalonDockLayout();
            Notification.Initialize();

        }  
        
        public void InitAvalonDockLayout() {            
            this.Documents.Add(ImagingVM.ImageControl);
            this.Anchorables.Add(CameraVM);
            this.Anchorables.Add(TelescopeVM);
            this.Anchorables.Add(PlatesolveVM);            
            this.Anchorables.Add(PolarAlignVM);
            this.Anchorables.Add(WeatherDataVM);
            this.Anchorables.Add(PHD2VM);
            this.Anchorables.Add(SeqVM);
            this.Anchorables.Add(FilterWheelVM);
            this.Anchorables.Add(FocuserVM);
            this.Anchorables.Add(ImagingVM);
            this.Anchorables.Add(ImagingVM.ImageControl.ImgHistoryVM);
            this.Anchorables.Add(ImagingVM.ImageControl.ImgStatisticsVM);            
        }

        private Xceed.Wpf.AvalonDock.DockingManager _dockmanager;
        private bool _dockloaded = false;
        public void LoadAvalonDockLayout(object o) {
            if(!_dockloaded) { 
                _dockmanager = (Xceed.Wpf.AvalonDock.DockingManager)o;
                var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                serializer.LayoutSerializationCallback += (s, args) => {
                    
                    args.Content = args.Content;
                };

                if (System.IO.File.Exists(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH)) {
                    serializer.Deserialize(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH);
                }
                _dockloaded = true;
            }
        }

        public void SaveAvalonDockLayout() {
            var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
            serializer.Serialize(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH);
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                Status = (string)o;
            }, MediatorMessages.StatusUpdate);
        }
        


        public string Version {
            get {               
                return "v. 1.0.2";
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
            SaveAvalonDockLayout();
            if (CameraVM?.Cam?.Connected == true) {
                var diag = MyMessageBox.MyMessageBox.Show("Camera still connected. Exit anyway?", "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);                
                if(diag == MessageBoxResult.OK) {
                    Application.Current.Shutdown();
                }
            } else {
                Application.Current.Shutdown();
            }
            
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
                    _cameraVM = new CameraVM();

                }
                return _cameraVM;
            }
            set {
                _cameraVM = value;
                RaisePropertyChanged();
            }
        }

        private FilterWheelVM _filterWheelVM;
        public FilterWheelVM FilterWheelVM {
            get {
                if (_filterWheelVM == null) {
                    _filterWheelVM = new FilterWheelVM();
                }
                return _filterWheelVM;
            }
            set {
                _filterWheelVM = value;
                RaisePropertyChanged();
            }
        }

        private FocuserVM _focuserVM;
        public FocuserVM FocuserVM {
            get {
                if (_focuserVM == null) {
                    _focuserVM = new FocuserVM();
                }
                return _focuserVM;
            }
            set {
                _focuserVM = value;
                RaisePropertyChanged();
            }
        }

        private WeatherDataVM _weatherDataVM;
        public WeatherDataVM WeatherDataVM {
            get {
                if (_weatherDataVM == null) {
                    _weatherDataVM = new WeatherDataVM();

                }
                return _weatherDataVM;
            }
            set {
                _weatherDataVM = value;
                RaisePropertyChanged();
            }
        }

        private SequenceVM _seqVM;
        public SequenceVM SeqVM {
            get {
                if(_seqVM == null) {
                    _seqVM = new SequenceVM();
                }
                return _seqVM;
            }
            set {
                _seqVM = value;
                RaisePropertyChanged();
            }
        }

        private ImagingVM _imagingVM;
        public ImagingVM ImagingVM {
            get {
                if (_imagingVM == null) {
                    _imagingVM = new ImagingVM();
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
                    _frameFocusVM = new FrameFocusVM();
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

        private ICommand _loadAvalonDockLayoutCommand;
        public ICommand LoadAvalonDockLayoutCommand {
            get {
                return _loadAvalonDockLayoutCommand;
            }

            set {
                _loadAvalonDockLayoutCommand = value;
                RaisePropertyChanged();
            }
        }

        
    }
}
