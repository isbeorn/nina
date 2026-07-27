#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using System;

namespace NINA.WPF.Base.SkySurvey {

    public readonly record struct SkyMapHorizontalCoordinates(double Altitude, double Azimuth);

    public sealed class SkyMapObserverSnapshot : ISkyMapVisibility {
        public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
        private readonly Func<double, double> horizonAltitude;
        private readonly double latitude;
        private readonly double siderealTime;

        public SkyMapObserverSnapshot(
            double latitude,
            double longitude,
            DateTime timestamp,
            Func<double, double> horizonAltitude = null) {
            this.latitude = latitude;
            this.horizonAltitude = horizonAltitude ?? (_ => 0);
            Timestamp = timestamp;
            siderealTime = AstroUtil.GetLocalSiderealTime(timestamp, longitude);
        }

        public DateTime Timestamp { get; }

        public bool IsVisible(Coordinates coordinates) {
            SkyMapHorizontalCoordinates horizontal = ToHorizontal(coordinates);
            return horizontal.Altitude >= horizonAltitude(horizontal.Azimuth);
        }

        public bool NeedsRefresh(DateTime timestamp) {
            return timestamp >= Timestamp + RefreshInterval;
        }

        public SkyMapHorizontalCoordinates ToHorizontal(Coordinates coordinates) {
            double hourAngle = AstroUtil.HoursToDegrees(AstroUtil.GetHourAngle(siderealTime, coordinates.RA));
            double altitude = AstroUtil.GetAltitude(hourAngle, latitude, coordinates.Dec);
            double azimuth = AstroUtil.GetAzimuth(hourAngle, altitude, latitude, coordinates.Dec);
            return new SkyMapHorizontalCoordinates(altitude, azimuth);
        }

        public Coordinates ToCelestial(SkyMapHorizontalCoordinates horizontal) {
            double altitude = AstroUtil.ToRadians(horizontal.Altitude);
            double azimuth = AstroUtil.ToRadians(horizontal.Azimuth);
            double latitudeRadians = AstroUtil.ToRadians(latitude);
            double declination = Math.Asin(
                Math.Sin(altitude) * Math.Sin(latitudeRadians)
                + Math.Cos(altitude) * Math.Cos(latitudeRadians) * Math.Cos(azimuth));
            double hourAngleCosine = (Math.Sin(altitude) - Math.Sin(latitudeRadians) * Math.Sin(declination))
                / (Math.Cos(latitudeRadians) * Math.Cos(declination));
            double hourAngle = Math.Acos(Math.Clamp(hourAngleCosine, -1, 1));
            if (horizontal.Azimuth < 180) {
                hourAngle = -hourAngle;
            }
            double rightAscension = AstroUtil.EuclidianModulus(
                AstroUtil.HoursToDegrees(siderealTime) - AstroUtil.ToDegree(hourAngle),
                360);
            return new Coordinates(rightAscension, AstroUtil.ToDegree(declination), Epoch.J2000, Coordinates.RAType.Degrees);
        }
    }
}
