using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Profile;
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
                    Mediator.Instance.Request(new SetProfileByIdMessage() {
                        Id = value.Id
                    });
                    RaiseAllPropertiesChanged();
                }
            }
        }

        public string Camera {
            get {
                return Mediator.Instance.Request(new GetEquipmentNameByIdMessage() {
                    Id = ActiveProfile.CameraSettings.Id
                }, typeof(CameraChooserVM));
            }
        }

        public string FilterWheel {
            get {
                return Mediator.Instance.Request(new GetEquipmentNameByIdMessage() {
                    Id = ActiveProfile.FilterWheelSettings.Id
                }, typeof(FilterWheelChooserVM));
            }
        }

        public string FocalLength {
            get {
                return ActiveProfile.TelescopeSettings.FocalLength.ToString(CultureInfo.InvariantCulture);
            }
        }

        public string Focuser {
            get {
                return Mediator.Instance.Request(new GetEquipmentNameByIdMessage() {
                    Id = ActiveProfile.FocuserSettings.Id
                }, typeof(FocuserChooserVM));
            }
        }

        public ObserveAllCollection<IProfile> Profiles { set; get; }

        public string Telescope {
            get {
                return Mediator.Instance.Request(new GetEquipmentNameByIdMessage() {
                    Id = ActiveProfile.TelescopeSettings.Id
                }, typeof(TelescopeChooserVM));
            }
        }

        public bool UseSavedProfile { get; set; } = Properties.Settings.Default.UseSavedProfileSelection;

        public void SelectProfile() {
            _cancelTokenSource = new CancellationTokenSource();
            try {
                if (!UseSavedProfile) {
                    var ws = new WindowService();
                    ws.OnDialogResultChanged += (s, e) => {
                        var dialogResult = (WindowService.DialogResultEventArgs)e;
                        if (dialogResult.DialogResult != true) {
                            _cancelTokenSource.Cancel();
                            Mediator.Instance.Request(new SetProfileByIdMessage() {
                                Id = _defaultProfile.Id
                            });
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