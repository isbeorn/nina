#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;

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
    }
}