#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyDome {

    public partial class DomeInfo : DeviceInfo {
        [ObservableProperty]
        private ShutterState shutterStatus = ShutterState.ShutterNone;

        [ObservableProperty]
        private bool driverCanFollow = false;

        [ObservableProperty]
        private bool canSetShutter = false;

        [ObservableProperty]
        private bool canSetPark = false;

        [ObservableProperty]
        private bool canSetAzimuth = false;

        [ObservableProperty]
        private bool canSyncAzimuth = false;

        [ObservableProperty]
        private bool canPark = false;

        [ObservableProperty]
        private bool canFindHome = false;

        [ObservableProperty]
        private bool atPark = false;

        [ObservableProperty]
        private bool atHome = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FollowingType))]
        private bool driverFollowing = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FollowingType))]
        private bool applicationFollowing = false;

        public string FollowingType {
            get {
                if (DriverFollowing) {
                    return Loc.Instance["LblDomeFollowingViaDriver"];
                } if (ApplicationFollowing) {
                    return Loc.Instance["LblDomeFollowingViaNINA"];
                }
                return Loc.Instance["LblOff"];
            }
        }

        [ObservableProperty]
        private bool slewing = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AzimuthDMS))]
        private double azimuth = double.NaN;
        public string AzimuthDMS => double.IsNaN(Azimuth) ? "" : AstroUtil.DegreesToDMS(Azimuth);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AltitudeDMS))]
        private double altitude = double.NaN;

        public string AltitudeDMS => double.IsNaN(Altitude) ? "" : AstroUtil.DegreesToDMS(Altitude);

        [ObservableProperty]
        private IList<string> supportedActions = [];

    }
}