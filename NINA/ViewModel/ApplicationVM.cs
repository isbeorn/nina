#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Equipment.FilterWheel;
using NINA.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
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
            Logger.SetLogLevel(profileService.ActiveProfile.ApplicationSettings.LogLevel);
            cameraMediator = new CameraMediator();
            telescopeMediator = new TelescopeMediator();
            focuserMediator = new FocuserMediator();
            filterWheelMediator = new FilterWheelMediator();
            rotatorMediator = new RotatorMediator();
            guiderMediator = new GuiderMediator();
            imagingMediator = new ImagingMediator();
            applicationStatusMediator = new ApplicationStatusMediator();

            ExitCommand = new RelayCommand(ExitApplication);
            ClosingCommand = new RelayCommand(ClosingApplication);
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
                        var rotator = rotatorMediator.Connect();
                        var guider = guiderMediator.Connect();
                        await Task.WhenAll(cam, fw, telescope, focuser, rotator, guider);
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

            InitAvalonDockLayout();

            OptionsVM.PropertyChanged += OptionsVM_PropertyChanged;

            profileService.ProfileChanged += ProfileService_ProfileChanged;
        }

        public IProfile ActiveProfile {
            get {
                return profileService.ActiveProfile;
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(ActiveProfile));
        }

        private async void OptionsVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(OptionsVM.AutoUpdateSource)) {
                await CheckUpdate();
            }
        }

        private ICameraMediator cameraMediator;
        private ITelescopeMediator telescopeMediator;
        private IFocuserMediator focuserMediator;
        private IFilterWheelMediator filterWheelMediator;
        private RotatorMediator rotatorMediator;
        private IGuiderMediator guiderMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        private void LoadProfile(object obj) {
            if (profileService.Profiles.Count > 1) {
                new ProfileSelectVM(profileService).SelectProfile();
            }
        }

        private Task<bool> CheckUpdate() {
            return VersionCheckVM.CheckUpdate();
        }

        private void OpenManual(object o) {
            System.Diagnostics.Process.Start("https://nighttime-imaging.eu/docs/documentation/");
        }

        public void InitAvalonDockLayout() {
            DockManagerVM.Anchorables.Add(ImagingVM.ImageControl);
            DockManagerVM.Anchorables.Add(CameraVM);
            DockManagerVM.Anchorables.Add(FilterWheelVM);
            DockManagerVM.Anchorables.Add(FocuserVM);
            DockManagerVM.Anchorables.Add(RotatorVM);
            DockManagerVM.Anchorables.Add(TelescopeVM);
            DockManagerVM.Anchorables.Add(GuiderVM);

            DockManagerVM.Anchorables.Add(ImagingVM);
            DockManagerVM.Anchorables.Add(SeqVM);
            DockManagerVM.Anchorables.Add(ImagingVM.ImageControl.ImgStatisticsVM);
            DockManagerVM.Anchorables.Add(ImagingVM.ImageControl.ImgHistoryVM);

            DockManagerVM.Anchorables.Add(ThumbnailVM);
            DockManagerVM.Anchorables.Add(WeatherDataVM);
            DockManagerVM.Anchorables.Add(PlatesolveVM);
            DockManagerVM.Anchorables.Add(PolarAlignVM);
            DockManagerVM.Anchorables.Add(AutoFocusVM);
            DockManagerVM.Anchorables.Add(FocusTargetsVM);

            DockManagerVM.AnchorableInfoPanels.Add(ImagingVM.ImageControl);
            DockManagerVM.AnchorableInfoPanels.Add(CameraVM);
            DockManagerVM.AnchorableInfoPanels.Add(FilterWheelVM);
            DockManagerVM.AnchorableInfoPanels.Add(FocuserVM);
            DockManagerVM.AnchorableInfoPanels.Add(RotatorVM);
            DockManagerVM.AnchorableInfoPanels.Add(TelescopeVM);
            DockManagerVM.AnchorableInfoPanels.Add(GuiderVM);
            DockManagerVM.AnchorableInfoPanels.Add(SeqVM);
            DockManagerVM.AnchorableInfoPanels.Add(ImagingVM.ImageControl.ImgStatisticsVM);
            DockManagerVM.AnchorableInfoPanels.Add(ImagingVM.ImageControl.ImgHistoryVM);

            DockManagerVM.AnchorableTools.Add(ImagingVM);
            DockManagerVM.AnchorableTools.Add(ThumbnailVM);
            DockManagerVM.AnchorableTools.Add(WeatherDataVM);
            DockManagerVM.AnchorableTools.Add(PlatesolveVM);
            DockManagerVM.AnchorableTools.Add(PolarAlignVM);
            DockManagerVM.AnchorableTools.Add(AutoFocusVM);
            DockManagerVM.AnchorableTools.Add(FocusTargetsVM);
        }

        public void ChangeTab(ApplicationTab tab) {
            TabIndex = (int)tab;
        }

        public string Version {
            get {
                return new ProjectVersion(Utility.Utility.Version).ToString();
            }
        }

        public string Title {
            get {
                return Utility.Utility.Title;
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

        public VersionCheckVM VersionCheckVM { get; private set; } = new VersionCheckVM();

        private ApplicationStatusVM _applicationStatusVM;

        public ApplicationStatusVM ApplicationStatusVM {
            get {
                if (_applicationStatusVM == null) {
                    _applicationStatusVM = new ApplicationStatusVM(profileService, applicationStatusMediator);
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
                if (diag != MessageBoxResult.OK) {
                    return;
                }
            }
            Application.Current.Shutdown();
        }

        private void ClosingApplication(object o) {
            Notification.Dispose();
            DisconnectEquipment();
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
                rotatorMediator.Disconnect();
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
                    _thumbnailVM = new ThumbnailVM(profileService, imagingMediator);
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
                    _cameraVM = new CameraVM(profileService, cameraMediator, telescopeMediator, applicationStatusMediator);
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
                    _filterWheelVM = new FilterWheelVM(profileService, filterWheelMediator, focuserMediator, applicationStatusMediator);
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
                    _focuserVM = new FocuserVM(profileService, focuserMediator, applicationStatusMediator);
                }
                return _focuserVM;
            }
            set {
                _focuserVM = value;
                RaisePropertyChanged();
            }
        }

        private RotatorVM rotatorVM;

        public RotatorVM RotatorVM {
            get {
                if (rotatorVM == null) {
                    rotatorVM = new RotatorVM(profileService, rotatorMediator, applicationStatusMediator);
                }
                return rotatorVM;
            }
            set {
                rotatorVM = value;
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

        private FlatWizardVM _flatWizardVM;

        public FlatWizardVM FlatWizardVM {
            get {
                if (_flatWizardVM == null) {
                    _flatWizardVM = new FlatWizardVM(profileService,
                        new ImagingVM(profileService, new ImagingMediator(), cameraMediator, telescopeMediator, filterWheelMediator, focuserMediator, rotatorMediator, guiderMediator, applicationStatusMediator),
                        cameraMediator,
                        filterWheelMediator,
                        telescopeMediator,
                        new ApplicationResourceDictionary(),
                        applicationStatusMediator);
                }
                return _flatWizardVM;
            }
            set {
                _flatWizardVM = value;
                RaisePropertyChanged();
            }
        }

        private SequenceVM _seqVM;

        public SequenceVM SeqVM {
            get {
                if (_seqVM == null) {
                    _seqVM = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, rotatorMediator, imagingMediator, applicationStatusMediator);
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
                    _imagingVM = new ImagingVM(profileService, imagingMediator, cameraMediator, telescopeMediator, filterWheelMediator, focuserMediator, rotatorMediator, guiderMediator, applicationStatusMediator);
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
                    _polarAlignVM = new PolarAlignmentVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
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
                    _platesolveVM = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
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
                    _telescopeVM = new TelescopeVM(profileService, telescopeMediator, applicationStatusMediator);
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
                    _guiderVM = new
                        GuiderVM(profileService, guiderMediator, cameraMediator, applicationStatusMediator, telescopeMediator);
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

        private FocusTargetsVM focusTargetsVM;

        public FocusTargetsVM FocusTargetsVM {
            get => focusTargetsVM ?? (focusTargetsVM = new FocusTargetsVM(profileService, telescopeMediator, new ApplicationResourceDictionary()));
            set {
                focusTargetsVM = value;
                RaisePropertyChanged();
            }
        }

        private AutoFocusVM _autoFocusVM;

        public AutoFocusVM AutoFocusVM {
            get {
                if (_autoFocusVM == null) {
                    _autoFocusVM = new AutoFocusVM(profileService, cameraMediator, filterWheelMediator, focuserMediator, guiderMediator, imagingMediator, applicationStatusMediator);
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
                    _framingAssistantVM = new FramingAssistantVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
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
        public ICommand ClosingCommand { get; private set; }
        public ICommand ConnectAllDevicesCommand { get; private set; }
        public ICommand DisconnectAllDevicesCommand { get; private set; }
    }

    public enum ApplicationTab {
        EQUIPMENT,
        SKYATLAS,
        FRAMINGASSISTANT,
        FLATWIZARD,
        SEQUENCE,
        IMAGING,
        OPTIONS
    }
}