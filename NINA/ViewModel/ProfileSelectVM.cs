using NINA.Utility;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel
{
    class ProfileSelectVM : BaseINPC
    {
        private CancellationTokenSource _cancelTokenSource;
        private bool _useSavedProfile = Properties.Settings.Default.UseSavedProfileSelection;
        private ObserveAllCollection<Profile> _profileList;
        private Profile _tempProfile;
        private Profile _defaultProfile;
        private ApplicationVM _applicationVM;

        public ProfileSelectVM(ApplicationVM applicationVM)
        {
            _applicationVM = applicationVM;
            Profiles = ProfileManager.Instance.Profiles.ProfileList;
            ActiveProfile = ProfileManager.Instance.ActiveProfile;
            _defaultProfile = ActiveProfile;
        }

        public ObserveAllCollection<Profile> Profiles
        {
            set
            {
                _profileList = value;
            }
            get
            {
                return _profileList;
            }
        }

        public Profile ActiveProfile
        {
            get
            {
                return _tempProfile;
            }
            set
            {
                _tempProfile = value;
                RaiseAllPropertiesChanged();
            }
        }

        public bool UseSavedProfile
        {
            get
            {
                return _useSavedProfile;
            }
            set
            {
                _useSavedProfile = value;
            }
        }

        public string Camera
        {
            get
            {
                return _applicationVM.CameraVM.CameraChooserVM.Devices.Single(cam => cam.Id == ActiveProfile.CameraSettings.Id).Name;
            }
        }

        public string FilterWheel
        {
            get
            {
                return _applicationVM.FilterWheelVM.FilterWheelChooserVM.Devices.Single(cam => cam.Id == ActiveProfile.FilterWheelSettings.Id).Name;
            }
        }

        public string Focuser
        {
            get
            {
                return _applicationVM.FocuserVM.FocuserChooserVM.Devices.Single(cam => cam.Id == ActiveProfile.FocuserSettings.Id).Name;
            }
        }

        public string Telescope
        {
            get
            {
                return _applicationVM.TelescopeVM.TelescopeChooserVM.Devices.Single(cam => cam.Id == ActiveProfile.TelescopeSettings.Id).Name;
            }
        }

        public string FocalLength
        {
            get
            {
                return ActiveProfile.TelescopeSettings.FocalLength.ToString(CultureInfo.InvariantCulture);
            }
        }

        public void SelectProfile()
        {
            _cancelTokenSource = new CancellationTokenSource();
            try
            {
                if (!UseSavedProfile)
                {
                    var ws = new WindowService();
                    ws.OnDialogResultChanged += (s, e) =>
                    {
                        var dialogResult = (WindowService.DialogResultEventArgs)e;
                        if (dialogResult.DialogResult != true)
                        {
                            _cancelTokenSource.Cancel();
                        }
                        else
                        {
                            _applicationVM.OptionsVM.SelectedProfile = _tempProfile;
                            _applicationVM.OptionsVM.SelectProfileCommand.Execute(null);
                            if (UseSavedProfile == true)
                            {
                                Properties.Settings.Default.UseSavedProfileSelection = true;
                                Properties.Settings.Default.Save();
                            }
                        }
                    };
                    ws.ShowDialog(this, Locale.Loc.Instance["LblChooseProfile"], System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.SingleBorderWindow);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
