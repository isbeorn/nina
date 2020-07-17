#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Profile;
using NINA.Utility.WindowService;
using System;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using NINA.Utility.Notification;
using System.Linq;

namespace NINA.ViewModel {

    internal class ProfileSelectVM : BaseINPC {
        private CancellationTokenSource _cancelTokenSource;
        private IProfile _defaultProfile;
        private IProfile _tempProfile;

        public ProfileSelectVM(IProfileService profileService) {
            this.profileService = profileService;
            Profiles = profileService.Profiles;
            selectedProfileMeta = profileService.Profiles.Where(x => x.Id == profileService.ActiveProfile.Id).First();
            _tempProfile = profileService.ActiveProfile;
            _defaultProfile = ActiveProfile;
        }

        private IProfileService profileService;

        public ICollection<ProfileMeta> Profiles { set; get; }

        private ProfileMeta selectedProfileMeta;

        public ProfileMeta SelectedProfileMeta {
            get => selectedProfileMeta;
            set {
                if (profileService.SelectProfile(value)) {
                    selectedProfileMeta = value;
                    RaisePropertyChanged(nameof(ActiveProfile));
                    RaisePropertyChanged(nameof(Camera));
                    RaisePropertyChanged(nameof(FilterWheel));
                    RaisePropertyChanged(nameof(Telescope));
                    RaisePropertyChanged(nameof(FocalLength));
                    RaisePropertyChanged(nameof(Focuser));
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblSelectProfileInUseWarning"]);
                }
                RaisePropertyChanged();
            }
        }

        public IProfile ActiveProfile {
            get {
                return profileService.ActiveProfile;
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
            _cancelTokenSource?.Dispose();
            _cancelTokenSource = new CancellationTokenSource();
            try {
                if (!UseSavedProfile) {
                    var ws = WindowServiceFactory.Create();
                    ws.OnDialogResultChanged += (s, e) => {
                        var dialogResult = (DialogResultEventArgs)e;
                        if (dialogResult.DialogResult != true) {
                            _cancelTokenSource.Cancel();
                            profileService.SelectProfile(new ProfileMeta() { Id = _defaultProfile.Id, Name = _defaultProfile.Name, Location = _defaultProfile.Location });
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