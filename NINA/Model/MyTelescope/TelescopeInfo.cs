#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using NINA.Utility.Enum;

namespace NINA.Model.MyTelescope {

    public class TelescopeInfo : DeviceInfo {
        private double siderealTime;

        public double SiderealTime {
            get {
                return siderealTime;
            }
            set {
                siderealTime = value;
                RaisePropertyChanged();
            }
        }

        private double rightAscension;

        public double RightAscension {
            get {
                return rightAscension;
            }
            set {
                rightAscension = value;
                RaisePropertyChanged();
            }
        }

        private double declination;

        public double Declination {
            get {
                return declination;
            }
            set {
                declination = value;
                RaisePropertyChanged();
            }
        }

        private double siteLatitude;

        public double SiteLatitude {
            get {
                return siteLatitude;
            }
            set {
                siteLatitude = value;
                RaisePropertyChanged();
            }
        }

        private double siteLongitude;

        public double SiteLongitude {
            get {
                return siteLongitude;
            }
            set {
                siteLongitude = value;
                RaisePropertyChanged();
            }
        }

        private double siteElevation;

        public double SiteElevation {
            get {
                return siteElevation;
            }
            set {
                siteElevation = value;
                RaisePropertyChanged();
            }
        }

        private string rightAscensionString;

        public string RightAscensionString {
            get {
                return rightAscensionString;
            }
            set {
                rightAscensionString = value;
                RaisePropertyChanged();
            }
        }

        private string declinationString;

        public string DeclinationString {
            get {
                return declinationString;
            }
            set {
                declinationString = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get {
                return coordinates;
            }
            set {
                coordinates = value;
                RaisePropertyChanged();
            }
        }

        private double timeToMeridianFlip;

        public double TimeToMeridianFlip {
            get {
                return timeToMeridianFlip;
            }
            set {
                timeToMeridianFlip = value;
                RaisePropertyChanged();
            }
        }

        private PierSide sideOfPier;

        public PierSide SideOfPier {
            get {
                return sideOfPier;
            }
            set {
                sideOfPier = value;
                RaisePropertyChanged();
            }
        }

        private string altitudeString;

        public string AltitudeString {
            get { return altitudeString; }
            set { altitudeString = value; RaisePropertyChanged(); }
        }

        private string azimuthString;

        public string AzimuthString {
            get { return azimuthString; }
            set { azimuthString = value; RaisePropertyChanged(); }
        }

        private string siderealTimeString;

        public string SiderealTimeString {
            get { return siderealTimeString; }
            set { siderealTimeString = value; RaisePropertyChanged(); }
        }

        private string hoursToMeridianString;

        public string HoursToMeridianString {
            get { return hoursToMeridianString; }
            set { hoursToMeridianString = value; RaisePropertyChanged(); }
        }

        private bool atPark;

        public bool AtPark {
            get { return atPark; }
            set { atPark = value; RaisePropertyChanged(); }
        }

        private bool tracking;

        public bool Tracking {
            get { return tracking; }
            set { tracking = value; RaisePropertyChanged(); }
        }

        private bool canPark;

        public bool CanPark {
            get { return canPark; }
            set { canPark = value; RaisePropertyChanged(); }
        }

        private bool canSetPark;

        public bool CanSetPark {
            get { return canSetPark; }
            set { canSetPark = value; RaisePropertyChanged(); }
        }

        private bool canSetTracking;

        public bool CanSetTracking {
            get { return canSetTracking; }
            set { canSetTracking = value; RaisePropertyChanged(); }
        }

        private Epoch equatorialSystem;

        public Epoch EquatorialSystem {
            get { return equatorialSystem; }
            set { equatorialSystem = value; RaisePropertyChanged(); }
        }

        private bool hasUnknownEpoch;

        public bool HasUnknownEpoch {
            get { return hasUnknownEpoch; }
            set { hasUnknownEpoch = value; RaisePropertyChanged(); }
        }

        private string timeToMeridianFlipString;

        public string TimeToMeridianFlipString {
            get { return timeToMeridianFlipString; }
            set { timeToMeridianFlipString = value; RaisePropertyChanged(); }
        }
    }
}