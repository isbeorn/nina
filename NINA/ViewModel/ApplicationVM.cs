using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class ApplicationVM : BaseVM {

        public ApplicationVM() : this(new ProfileService()) {
        }

        public ApplicationVM(IProfileService profileService) : base(profileService) {
            cameraMediator = new CameraMediator();
            telescopeMediator = new TelescopeMediator();
            focuserMediator = new FocuserMediator();
            filterWheelMediator = new FilterWheelMediator();
            guiderMediator = new GuiderMediator();

            ExitCommand = new RelayCommand(ExitApplication);
            MinimizeWindowCommand = new RelayCommand(MinimizeWindow);
            MaximizeWindowCommand = new RelayCommand(MaximizeWindow);
            CheckProfileCommand = new RelayCommand(LoadProfile);
            CheckUpdateCommand = new AsyncCommand<bool>(() => CheckUpdate());
            OpenManualCommand = new RelayCommand(OpenManual);
            ConnectAllDevicesCommand = new AsyncCommand<bool>(async () => {
                var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblReconnectAll"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    return await Task<bool>.Run(async () => {
                        var cam = cameraMediator.Connect();
                        var fw = filterWheelMediator.Connect();
                        var telescope = telescopeMediator.Connect();
                        var focuser = focuserMediator.Connect();
                        var guider = guiderMediator.Connect();
                        await Task.WhenAll(cam, fw, telescope, focuser, guider);
                        return true;
                    });
                } else {
                    return false;
                }
            });
            DisconnectAllDevicesCommand = new RelayCommand((object o) => {
                var diag = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblDisconnectAll"], "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    DisconnectEquipment();
                }
            });

            RegisterMediatorMessages();

            InitAvalonDockLayout();
        }

        private CameraMediator cameraMediator;
        private TelescopeMediator telescopeMediator;
        private FocuserMediator focuserMediator;
        private FilterWheelMediator filterWheelMediator;
        private GuiderMediator guiderMediator;

        private void LoadProfile(object obj) {
            if (profileService.Profiles.ProfileList.Count > 1) {
                new ProfileSelectVM(profileService).SelectProfile();
            }
        }

        private async Task<bool> CheckUpdate() {
            return await new VersionCheckVM().CheckUpdate();
        }

        private static string NINAMANUAL = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Documentation", "NINA.html");

        private void OpenManual(object o) {
            if (File.Exists(NINAMANUAL)) {
                System.Diagnostics.Process.Start(NINAMANUAL);
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblManualNotFound"]);
            }
        }

        public void InitAvalonDockLayout() {
            DockManagerVM.Documents.Add(ImagingVM.ImageControl);
            DockManagerVM.Anchorables.Add(ThumbnailVM);
            DockManagerVM.Anchorables.Add(CameraVM);
            DockManagerVM.Anchorables.Add(TelescopeVM);
            DockManagerVM.Anchorables.Add(PlatesolveVM);
            DockManagerVM.Anchorables.Add(PolarAlignVM);
            DockManagerVM.Anchorables.Add(WeatherDataVM);
            DockManagerVM.Anchorables.Add(GuiderVM);
            DockManagerVM.Anchorables.Add(SeqVM);
            DockManagerVM.Anchorables.Add(FilterWheelVM);
            DockManagerVM.Anchorables.Add(FocuserVM);
            DockManagerVM.Anchorables.Add(ImagingVM);
            DockManagerVM.Anchorables.Add(ImagingVM.ImageControl.ImgHistoryVM);
            DockManagerVM.Anchorables.Add(ImagingVM.ImageControl.ImgStatisticsVM);
            DockManagerVM.Anchorables.Add(AutoFocusVM);
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterRequest(new ChangeApplicationTabMessageHandle((ChangeApplicationTabMessage m) => {
                TabIndex = (int)m.Tab;
                return true;
            }));
        }

        public string Version {
            get {
                return Utility.Utility.Version;
            }
        }

        private int _tabIndex;

        public int TabIndex {
            get {
                return _tabIndex;
            }
            set {
                _tabIndex = value;
                RaisePropertyChanged();
            }
        }

        private ApplicationStatusVM _applicationStatusVM;

        public ApplicationStatusVM ApplicationStatusVM {
            get {
                if (_applicationStatusVM == null) {
                    _applicationStatusVM = new ApplicationStatusVM(profileService);
                }
                return _applicationStatusVM;
            }
            set {
                _applicationStatusVM = value;
                RaisePropertyChanged();
            }
        }

        private static void MaximizeWindow(object obj) {
            if (Application.Current.MainWindow.WindowState == WindowState.Maximized) {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            } else {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeWindow(object obj) {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void ExitApplication(object obj) {
            DockManagerVM.SaveAvalonDockLayout();
            if (CameraVM?.Cam?.Connected == true) {
                var diag = MyMessageBox.MyMessageBox.Show("Camera still connected. Exit anyway?", "", MessageBoxButton.OKCancel, MessageBoxResult.Cancel);
                if (diag == MessageBoxResult.OK) {
                    DisconnectEquipment();
                    Application.Current.Shutdown();
                }
            } else {
                DisconnectEquipment();
                Application.Current.Shutdown();
            }
        }

        public void DisconnectEquipment() {
            try {
                cameraMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                telescopeMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                filterWheelMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                focuserMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            try {
                guiderMediator.Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private DockManagerVM _dockManagerVM;

        public DockManagerVM DockManagerVM {
            get {
                if (_dockManagerVM == null) {
                    _dockManagerVM = new DockManagerVM(profileService);
                }
                return _dockManagerVM;
            }
            set {
                _dockManagerVM = value;
                RaisePropertyChanged();
            }
        }

        private ThumbnailVM _thumbnailVM;

        public ThumbnailVM ThumbnailVM {
            get {
                if (_thumbnailVM == null) {
                    _thumbnailVM = new ThumbnailVM(profileService);
                }
                return _thumbnailVM;
            }
            set {
                _thumbnailVM = value;
                RaisePropertyChanged();
            }
        }

        private CameraVM _cameraVM;

        public CameraVM CameraVM {
            get {
                if (_cameraVM == null) {
                    _cameraVM = new CameraVM(profileService, cameraMediator, telescopeMediator);
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
                    _filterWheelVM = new FilterWheelVM(profileService, filterWheelMediator, focuserMediator);
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
                    _focuserVM = new FocuserVM(profileService, focuserMediator);
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
                    _weatherDataVM = new WeatherDataVM(profileService);
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
                if (_seqVM == null) {
                    _seqVM = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator);
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
                    _imagingVM = new ImagingVM(profileService, cameraMediator, telescopeMediator, filterWheelMediator, guiderMediator);
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
                    _polarAlignVM = new PolarAlignmentVM(profileService, cameraMediator, telescopeMediator);
                }
                return _polarAlignVM;
            }
            set {
                _polarAlignVM = value;
                RaisePropertyChanged();
            }
        }

        private PlatesolveVM _platesolveVM;

        public PlatesolveVM PlatesolveVM {
            get {
                if (_platesolveVM == null) {
                    _platesolveVM = new PlatesolveVM(profileService, cameraMediator, telescopeMediator);
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
                    _telescopeVM = new TelescopeVM(profileService, telescopeMediator);
                }
                return _telescopeVM;
            }
            set {
                _telescopeVM = value;
                RaisePropertyChanged();
            }
        }

        private GuiderVM _guiderVM;

        public GuiderVM GuiderVM {
            get {
                if (_guiderVM == null) {
                    _guiderVM = new GuiderVM(profileService, guiderMediator);
                }
                return _guiderVM;
            }
            set {
                _guiderVM = value;
                RaisePropertyChanged();
            }
        }

        private OptionsVM _optionsVM;

        public OptionsVM OptionsVM {
            get {
                if (_optionsVM == null) {
                    _optionsVM = new OptionsVM(profileService, filterWheelMediator);
                }
                return _optionsVM;
            }
            set {
                _optionsVM = value;
                RaisePropertyChanged();
            }
        }

        private AutoFocusVM _autoFocusVM;

        public AutoFocusVM AutoFocusVM {
            get {
                if (_autoFocusVM == null) {
                    _autoFocusVM = new AutoFocusVM(profileService, cameraMediator, focuserMediator, guiderMediator);
                }
                return _autoFocusVM;
            }
            set {
                _autoFocusVM = value;
                RaisePropertyChanged();
            }
        }

        private FramingAssistantVM _framingAssistantVM;

        public FramingAssistantVM FramingAssistantVM {
            get {
                if (_framingAssistantVM == null) {
                    _framingAssistantVM = new FramingAssistantVM(profileService, cameraMediator, telescopeMediator);
                }
                return _framingAssistantVM;
            }
            set {
                _framingAssistantVM = value;
                RaisePropertyChanged();
            }
        }

        private SkyAtlasVM _skyAtlasVM;

        public SkyAtlasVM SkyAtlasVM {
            get {
                if (_skyAtlasVM == null) {
                    _skyAtlasVM = new SkyAtlasVM(profileService, telescopeMediator);
                }
                return _skyAtlasVM;
            }
            set {
                _skyAtlasVM = value;
                RaisePropertyChanged();
            }
        }

        public ICommand MinimizeWindowCommand { get; private set; }

        public ICommand MaximizeWindowCommand { get; private set; }
        public ICommand CheckProfileCommand { get; }
        public ICommand CheckUpdateCommand { get; private set; }
        public ICommand OpenManualCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        public ICommand ConnectAllDevicesCommand { get; private set; }
        public ICommand DisconnectAllDevicesCommand { get; private set; }
    }

    public enum ApplicationTab {
        CAMERA,
        FWANDFOCUSER,
        TELESCOPE,
        GUIDER,
        SKYATLAS,
        FRAMINGASSISTANT,
        SEQUENCE,
        IMAGING,
        OPTIONS
    }
}