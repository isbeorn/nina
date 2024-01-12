#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyDome {

    public class DomeInfo : DeviceInfo {
        private ShutterState shutterStatus = ShutterState.ShutterNone;

        public ShutterState ShutterStatus {
            get => shutterStatus;
            set { if (shutterStatus != value) { shutterStatus = value; RaisePropertyChanged(); } }
        }

        private bool driverCanFollow = false;

        public bool DriverCanFollow {
            get => driverCanFollow;
            set { if (driverCanFollow != value) { driverCanFollow = value; RaisePropertyChanged(); } }
        }

        private bool canSetShutter = false;

        public bool CanSetShutter {
            get => canSetShutter;
            set { if (canSetShutter != value) { canSetShutter = value; RaisePropertyChanged(); } }
        }

        private bool canSetPark = false;

        public bool CanSetPark {
            get => canSetPark;
            set { if (canSetPark != value) { canSetPark = value; RaisePropertyChanged(); } }
        }

        private bool canSetAzimuth = false;

        public bool CanSetAzimuth {
            get => canSetAzimuth;
            set { if (canSetAzimuth != value) { canSetAzimuth = value; RaisePropertyChanged(); } }
        }

        private bool canSyncAzimuth = false;

        public bool CanSyncAzimuth {
            get => canSyncAzimuth;
            set { if (canSyncAzimuth != value) { canSyncAzimuth = value; RaisePropertyChanged(); } }
        }

        private bool canPark = false;

        public bool CanPark {
            get => canPark;
            set { if (canPark != value) { canPark = value; RaisePropertyChanged(); } }
        }

        private bool canFindHome = false;

        public bool CanFindHome {
            get => canFindHome;
            set { if (canFindHome != value) { canFindHome = value; RaisePropertyChanged(); } }
        }

        private bool atPark = false;

        public bool AtPark {
            get => atPark;
            set { if (atPark != value) { atPark = value; RaisePropertyChanged(); } }
        }

        private bool atHome = false;

        public bool AtHome {
            get => atHome;
            set { if (atHome != value) { atHome = value; RaisePropertyChanged(); } }
        }

        private bool driverFollowing = false;

        public bool DriverFollowing {
            get => driverFollowing;
            set { if (driverFollowing != value) { driverFollowing = value; RaisePropertyChanged(); } }
        }

        private bool slewing = false;

        public bool Slewing {
            get => slewing;
            set { if (slewing != value) { slewing = value; RaisePropertyChanged(); } }
        }

        private double azimuth = double.NaN;

        public double Azimuth {
            get => azimuth;
            set { if (azimuth != value) { azimuth = value; RaisePropertyChanged(); } }
        }

        private IList<string> supportedActions = [];

        public IList<string> SupportedActions {
            get => supportedActions;
            set {
                supportedActions = value;
                RaisePropertyChanged();
            }
        }
    }
}