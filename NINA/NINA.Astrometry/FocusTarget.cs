#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Locale;
using NINA.Astrometry;
using System;

namespace NINA.Astrometry {

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

        public string AzimuthString => AstroUtil.DegreesToDMS(Azimuth);

        public string SkyDirection => (Azimuth <= 90 || Azimuth >= 270 ? Loc.Instance["LblNorthern"] : Loc.Instance["LblSouthern"]) + " " + (Azimuth >= 0 && Azimuth < 180 ? Loc.Instance["LblEast"] : Loc.Instance["LblWest"]);

        public void CalculateAltAz(double latitude, double longitude) {
            var start = DateTime.UtcNow;
            var siderealTime = AstroUtil.GetLocalSiderealTime(start, longitude);
            var hourAngle = AstroUtil.GetHourAngle(siderealTime, Coordinates.RA);

            var degAngle = AstroUtil.HoursToDegrees(hourAngle);
            Altitude = AstroUtil.GetAltitude(degAngle, latitude, Coordinates.Dec);
            Azimuth = AstroUtil.GetAzimuth(degAngle, Altitude, latitude, Coordinates.Dec);
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

        public string AltitudeString => AstroUtil.DegreesToDMS(Altitude);

        public string Information => $"{Name} ({SkyDirection}, Alt: {Altitude:0.00}°, Az: {Azimuth:0.00}°)";

        public override string ToString() {
            return Information;
        }
    }
}