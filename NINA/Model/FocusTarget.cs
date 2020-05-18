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
using NINA.Utility.Astrometry;
using System;

namespace NINA.Model {

    public class FocusTarget : BaseINPC {

        public FocusTarget(string name) {
            Name = name;
        }

        private string name;

        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                coordinates = value;
                RaisePropertyChanged();
            }
        }

        private double magnitude;

        public double Magnitude {
            get => magnitude;
            set {
                magnitude = value;
                RaisePropertyChanged();
            }
        }

        private double azimuth;

        public double Azimuth {
            get => azimuth;
            set {
                azimuth = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(AzimuthString));
            }
        }

        public string AzimuthString {
            get {
                return Astrometry.DegreesToDMS(Azimuth);
            }
        }

        public string SkyDirection => (Azimuth <= 90 || Azimuth >= 270 ? Locale.Loc.Instance["LblNorthern"] : Locale.Loc.Instance["LblSouthern"]) + " " + (Azimuth >= 0 && Azimuth < 180 ? Locale.Loc.Instance["LblEast"] : Locale.Loc.Instance["LblWest"]);

        public void CalculateAltAz(double latitude, double longitude) {
            var start = DateTime.UtcNow;
            var siderealTime = Astrometry.GetLocalSiderealTime(start, longitude);
            var hourAngle = Astrometry.GetHourAngle(siderealTime, Coordinates.RA);

            var degAngle = Astrometry.HoursToDegrees(hourAngle);
            Altitude = Astrometry.GetAltitude(degAngle, latitude, Coordinates.Dec);
            Azimuth = Astrometry.GetAzimuth(degAngle, Altitude, latitude, Coordinates.Dec);
            RaisePropertyChanged(nameof(Information));
        }

        private double altitude;

        public double Altitude {
            get => altitude;
            set {
                altitude = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(AltitudeString));
            }
        }

        public string AltitudeString {
            get {
                return Astrometry.DegreesToDMS(Altitude);
            }
        }

        public string Information {
            get => $"{Name} ({SkyDirection}, Alt: {Altitude:0.00}°, Az: {Azimuth:0.00}°)";
        }

        public override string ToString() {
            return Information;
        }
    }
}
