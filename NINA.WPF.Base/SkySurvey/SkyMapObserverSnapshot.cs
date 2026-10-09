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
using System.Collections.Generic;

namespace NINA.WPF.Base.SkySurvey {

    public readonly record struct SkyMapHorizontalCoordinates(double Altitude, double Azimuth);

    public sealed class SkyMapObserverSnapshot {
        public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
        private readonly Func<double, double> horizonAltitude;
        private readonly double latitudeCosine;
        private readonly double latitudeSine;
        private readonly double siderealTimeDegrees;
        private readonly Dictionary<(double RA, double Dec), SkyMapHorizontalCoordinates> catalogPositions = new();
        private readonly object catalogPositionLock = new();

        public SkyMapObserverSnapshot(
            double latitude,
            double longitude,
            DateTime timestamp,
            Func<double, double> horizonAltitude = null)
            : this(latitude, timestamp, AstroUtil.GetLocalSiderealTime(timestamp, longitude), horizonAltitude) {
        }

        public SkyMapObserverSnapshot(
            double latitude,
            DateTime timestamp,
            double localSiderealTime,
            Func<double, double> horizonAltitude = null) {
            double latitudeRadians = AstroUtil.ToRadians(latitude);
            latitudeCosine = Math.Cos(latitudeRadians);
            latitudeSine = Math.Sin(latitudeRadians);
            HasFlatHorizon = horizonAltitude is null;
            this.horizonAltitude = horizonAltitude ?? (_ => 0);
            Timestamp = timestamp;
            siderealTimeDegrees = AstroUtil.HoursToDegrees(localSiderealTime);
        }

        public DateTime Timestamp { get; }
        public bool HasFlatHorizon { get; }

        public bool IsVisible(Coordinates coordinates) {
            return HorizonClearance(coordinates) >= 0;
        }

        public double HorizonClearance(Coordinates coordinates) {
            return HorizonClearance(ToHorizontal(coordinates));
        }

        public double HorizonClearance(SkyMapHorizontalCoordinates horizontal) {
            return horizontal.Altitude - horizonAltitude(horizontal.Azimuth);
        }

        public double HorizonAltitude(double azimuth) {
            return horizonAltitude(azimuth);
        }

        public bool NeedsRefresh(DateTime timestamp) {
            return timestamp >= Timestamp + RefreshInterval;
        }

        public SkyMapHorizontalCoordinates ToHorizontal(Coordinates coordinates) {
            var key = (coordinates.RADegrees, coordinates.Dec);
            var catalog = coordinates.Epoch == Epoch.J2000;
            if (catalog) {
                lock (catalogPositionLock) {
                    if (catalogPositions.TryGetValue(key, out var cached)) return cached;
                }
            }
            var apparent = coordinates.Transform(Epoch.JNOW, Timestamp);
            var horizontal = ApparentToHorizontal(apparent.RADegrees, apparent.Dec);
            if (catalog) {
                lock (catalogPositionLock) {
                    // The observer and instant are immutable. Numeric keys observe mutable catalog
                    // coordinate edits without retaining the objects or growing without a limit.
                    // Keep hot entries when saturated instead of clearing them on every frame.
                    if (catalogPositions.Count < 65536) catalogPositions.TryAdd(key, horizontal);
                }
            }
            return horizontal;
        }

        public SkyMapHorizontalCoordinates ToHorizontal(double rightAscension, double declination) {
            lock (catalogPositionLock) {
                if (catalogPositions.TryGetValue((rightAscension, declination), out var horizontal)) return horizontal;
            }
            return ToHorizontal(new Coordinates(rightAscension, declination, Epoch.J2000, Coordinates.RAType.Degrees));
        }

        private SkyMapHorizontalCoordinates ApparentToHorizontal(double rightAscension, double declination) {
            double hourAngle = AstroUtil.ToRadians(AstroUtil.EuclidianModulus(siderealTimeDegrees - rightAscension, 360));
            declination = AstroUtil.ToRadians(declination);
            double declinationSine = Math.Sin(declination);
            double declinationCosine = Math.Cos(declination);
            double altitudeSine = declinationSine * latitudeSine
                + declinationCosine * latitudeCosine * Math.Cos(hourAngle);
            double altitude = Math.Asin(Math.Clamp(altitudeSine, -1, 1));
            double azimuth = Math.Atan2(
                -Math.Sin(hourAngle) * declinationCosine,
                declinationSine * latitudeCosine - declinationCosine * latitudeSine * Math.Cos(hourAngle));
            return new SkyMapHorizontalCoordinates(
                AstroUtil.ToDegree(altitude),
                AstroUtil.EuclidianModulus(AstroUtil.ToDegree(azimuth), 360));
        }

        public Coordinates ToCelestial(SkyMapHorizontalCoordinates horizontal) {
            double altitude = AstroUtil.ToRadians(horizontal.Altitude);
            double azimuth = AstroUtil.ToRadians(horizontal.Azimuth);
            double declination = Math.Asin(Math.Clamp(
                Math.Sin(altitude) * latitudeSine
                + Math.Cos(altitude) * latitudeCosine * Math.Cos(azimuth), -1, 1));
            double hourAngle = Math.Atan2(
                -Math.Sin(azimuth) * Math.Cos(altitude),
                Math.Sin(altitude) * latitudeCosine
                    - Math.Cos(altitude) * latitudeSine * Math.Cos(azimuth));
            double rightAscension = AstroUtil.EuclidianModulus(
                siderealTimeDegrees - AstroUtil.ToDegree(hourAngle),
                360);
            return new Coordinates(Angle.ByDegree(rightAscension), Angle.ByRadians(declination), Epoch.JNOW, Timestamp).Transform(Epoch.J2000);
        }
    }
}
