#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Astrometry;
using NINA.WPF.Base.SkySurvey;
using NUnit.Framework;
using System;

namespace NINA.Test.SkySurvey {

    [TestFixture]
    public class SkyMapObserverSnapshotTest {

        [Test]
        public void UsesLocationTimeAndHorizonUntilRefreshIsDue() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            const double latitude = 50;
            const double longitude = 10;
            double siderealTime = AstroUtil.GetLocalSiderealTime(at, longitude);
            Coordinates zenith = CelestialCoordinates(AstroUtil.HoursToDegrees(siderealTime), latitude);
            Coordinates nadir = CelestialCoordinates(AstroUtil.HoursToDegrees(siderealTime) + 180, -latitude);
            SkyMapObserverSnapshot sut = new SkyMapObserverSnapshot(latitude, longitude, at, _ => 5);

            SkyMapHorizontalCoordinates horizontal = sut.ToHorizontal(zenith);
            Coordinates roundTripSource = CelestialCoordinates(120, 25);
            Coordinates roundTrip = sut.ToCelestial(sut.ToHorizontal(roundTripSource));

            horizontal.Altitude.Should().BeApproximately(90, 0.0001);
            roundTrip.RADegrees.Should().BeApproximately(roundTripSource.RADegrees, 1E-9);
            roundTrip.Dec.Should().BeApproximately(roundTripSource.Dec, 1E-9);
            sut.IsVisible(zenith).Should().BeTrue();
            sut.IsVisible(nadir).Should().BeFalse();
            sut.NeedsRefresh(at.AddSeconds(59)).Should().BeFalse();
            sut.NeedsRefresh(at.AddMinutes(1)).Should().BeTrue();
        }

        [TestCase(0, -45)]
        [TestCase(120, 30)]
        [TestCase(300, 70)]
        public void ToHorizontal_MatchesEstablishedAstrometryFunctions(
            double rightAscension,
            double declination) {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            const double latitude = 50;
            const double longitude = 10;
            double siderealTime = AstroUtil.GetLocalSiderealTime(at, longitude);
            double hourAngle = AstroUtil.HoursToDegrees(AstroUtil.GetHourAngle(
                siderealTime,
                AstroUtil.DegreesToHours(rightAscension)));
            double expectedAltitude = AstroUtil.GetAltitude(hourAngle, latitude, declination);
            double expectedAzimuth = AstroUtil.GetAzimuth(hourAngle, expectedAltitude, latitude, declination);
            SkyMapObserverSnapshot sut = new SkyMapObserverSnapshot(latitude, longitude, at);

            SkyMapHorizontalCoordinates actual = sut.ToHorizontal(CelestialCoordinates(rightAscension, declination));

            actual.Altitude.Should().BeApproximately(expectedAltitude, 1E-10);
            double azimuthDifference = AstroUtil.EuclidianModulus(actual.Azimuth - expectedAzimuth + 180, 360) - 180;
            azimuthDifference.Should().BeApproximately(0, 1E-10);
        }

        [TestCase(90, 120, 25)]
        [TestCase(-90, 300, -25)]
        [TestCase(89.999, 45, 70)]
        public void RoundTrip_RemainsStableAtPolarLatitudes(
            double latitude,
            double rightAscension,
            double declination) {
            SkyMapObserverSnapshot sut = new SkyMapObserverSnapshot(
                latitude,
                new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc),
                16.5);
            Coordinates expected = CelestialCoordinates(rightAscension, declination);

            Coordinates actual = sut.ToCelestial(sut.ToHorizontal(expected));

            double rightAscensionDifference = AstroUtil.EuclidianModulus(
                actual.RADegrees - expected.RADegrees + 180,
                360) - 180;
            rightAscensionDifference.Should().BeApproximately(0, 1E-8);
            actual.Dec.Should().BeApproximately(expected.Dec, 1E-8);
        }

        private static Coordinates CelestialCoordinates(double rightAscension, double declination) {
            return new Coordinates(rightAscension, declination, Epoch.J2000, Coordinates.RAType.Degrees);
        }
    }
}