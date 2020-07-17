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
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class TelescopeLatLongSyncVM {

        public TelescopeLatLongSyncVM(
                bool canTelescopeSync,
                double nINALatitude,
                double nINALongitude,
                double telescopeLatitude,
                double telescopeLongitude) {
            SyncTelescopeCommand = new RelayCommand(SyncTelescope, (object o) => canTelescopeSync);
            SyncNINACommand = new RelayCommand(SyncNINA);
            SyncNoneCommand = new RelayCommand(SyncNone);
            this.NINALatitude = nINALatitude;
            this.NINALongitude = nINALongitude;
            this.TelescopeLatitude = telescopeLatitude;
            this.TelescopeLongitude = telescopeLongitude;
        }

        public double NINALatitude { get; private set; }
        public double NINALongitude { get; private set; }
        public double TelescopeLatitude { get; private set; }
        public double TelescopeLongitude { get; private set; }

        public enum LatLongSyncMode {
            NONE,
            TELESCOPE,
            NINA
        }

        public LatLongSyncMode Mode { get; set; }

        private void SyncNone(object obj) {
            Mode = LatLongSyncMode.NONE;
        }

        private void SyncNINA(object obj) {
            Mode = LatLongSyncMode.NINA;
        }

        private void SyncTelescope(object obj) {
            Mode = LatLongSyncMode.TELESCOPE;
        }

        public ICommand SyncTelescopeCommand { get; set; }
        public ICommand SyncNINACommand { get; set; }
        public ICommand SyncNoneCommand { get; set; }
    }
}