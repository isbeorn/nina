#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Utility;
using System.Windows.Input;

namespace NINA.WPF.Base.ViewModel.Equipment.Telescope {

    public class TelescopeLatLongSyncVM {

        public TelescopeLatLongSyncVM(
                double nINALatitude,
                double nINALongitude,
                double nINAElevation,
                double telescopeLatitude,
                double telescopeLongitude,
                double telescopeElevation) {
            SyncTelescopeCommand = new RelayCommand(SyncTelescope);
            SyncNINACommand = new RelayCommand(SyncNINA);
            SyncNoneCommand = new RelayCommand(SyncNone);
            this.NINALatitude = nINALatitude;
            this.NINALongitude = nINALongitude;
            this.NINAElevation = nINAElevation;
            this.TelescopeLatitude = telescopeLatitude;
            this.TelescopeLongitude = telescopeLongitude;
            this.TelescopeElevation = telescopeElevation;
        }

        public double NINALatitude { get; private set; }
        public double NINALongitude { get; private set; }
        public double NINAElevation { get; private set; }
        public double TelescopeLatitude { get; private set; }
        public double TelescopeLongitude { get; private set; }
        public double TelescopeElevation { get; private set; }

        public TelescopeLocationSyncDirection Mode { get; set; }

        private void SyncNone(object obj) {
            Mode = TelescopeLocationSyncDirection.NOSYNC;
        }

        private void SyncNINA(object obj) {
            Mode = TelescopeLocationSyncDirection.TOAPPLICATION;
        }

        private void SyncTelescope(object obj) {
            Mode = TelescopeLocationSyncDirection.TOTELESCOPE;
        }

        public ICommand SyncTelescopeCommand { get; set; }
        public ICommand SyncNINACommand { get; set; }
        public ICommand SyncNoneCommand { get; set; }
    }
}