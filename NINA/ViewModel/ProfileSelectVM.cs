#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using NINA.Core.Utility.WindowService;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile;

namespace NINA.ViewModel {

    public class ProfileSelectVM : BaseINPC {

        public ProfileSelectVM(IProfileService profileService) {
            this.profileService = profileService;
            Profiles = profileService.Profiles.OrderBy(x => x.Name).ToList();
            selectedProfileMeta = profileService.Profiles.Where(x => x.Id == profileService.ActiveProfile.Id).First();
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
                    Notification.ShowWarning(Loc.Instance["LblSelectProfileInUseWarning"]);
                }
                RaisePropertyChanged();
            }
        }

        public IProfile ActiveProfile => profileService.ActiveProfile;

        public string Camera => ActiveProfile.CameraSettings.Id;

        public string FilterWheel => ActiveProfile.FilterWheelSettings.Id;

        public double FocalLength => ActiveProfile.TelescopeSettings.FocalLength;

        public string Focuser => ActiveProfile.FocuserSettings.Id;

        public string Telescope => ActiveProfile.TelescopeSettings.Id;
        public bool UseSavedProfile {
            get => Properties.Settings.Default.UseSavedProfileSelection;
            set {
                Properties.Settings.Default.UseSavedProfileSelection = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
            }
        }
    }
}