using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using System;
using System.Globalization;
using System.Threading;

namespace NINA.ViewModel {

    internal class ProfileSelectVM : BaseINPC {
        private CancellationTokenSource _cancelTokenSource;
        private IProfile _defaultProfile;
        private IProfile _tempProfile;

        public ProfileSelectVM(IProfileService profileService) {
            this.profileService = profileService;
            Profiles = profileService.Profiles.ProfileList;
            ActiveProfile = profileService.ActiveProfile;
            _defaultProfile = ActiveProfile;
        }

        private IProfileService profileService;

        public IProfile ActiveProfile {
            get {
                return _tempProfile;
            }
            set {
                if (_tempProfile?.Id != value.Id || _tempProfile == null) {
                    _tempProfile = value;
                    profileService.SelectProfile(value.Id);
                    RaiseAllPropertiesChanged();
                }
            }
        }

        public string Camera {
            get {
                return ActiveProfile.CameraSettings.Id;
            }
        }

        public string FilterWheel {
            get {
                return ActiveProfile.FilterWheelSettings.Id;
            }
        }

        public string FocalLength {
            get {
                return ActiveProfile.TelescopeSettings.FocalLength.ToString(CultureInfo.InvariantCulture);
            }
        }

        public string Focuser {
            get {
                return ActiveProfile.FocuserSettings.Id;
            }
        }

        public ObserveAllCollection<IProfile> Profiles { set; get; }

        public string Telescope {
            get {
                return ActiveProfile.TelescopeSettings.Id;
            }
        }

        private IWindowServiceFactory windowServiceFactory;

        public IWindowServiceFactory WindowServiceFactory {
            get {
                if (windowServiceFactory == null) {
                    windowServiceFactory = new WindowServiceFactory();
                }
                return windowServiceFactory;
            }
            set {
                windowServiceFactory = value;
            }
        }

        public bool UseSavedProfile { get; set; } = Properties.Settings.Default.UseSavedProfileSelection;

        public void SelectProfile() {
            _cancelTokenSource = new CancellationTokenSource();
            try {
                if (!UseSavedProfile) {
                    var ws = WindowServiceFactory.Create();
                    ws.OnDialogResultChanged += (s, e) => {
                        var dialogResult = (DialogResultEventArgs)e;
                        if (dialogResult.DialogResult != true) {
                            _cancelTokenSource.Cancel();
                            profileService.SelectProfile(_defaultProfile.Id);
                        } else {
                            if (UseSavedProfile == true) {
                                Properties.Settings.Default.UseSavedProfileSelection = true;
                                Properties.Settings.Default.Save();
                            }
                        }
                    };
                    ws.ShowDialog(this, Locale.Loc.Instance["LblChooseProfile"], System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.SingleBorderWindow);
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }
    }
}