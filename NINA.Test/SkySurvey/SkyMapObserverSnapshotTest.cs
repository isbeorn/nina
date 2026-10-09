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
        public void RepeatedCatalogProjection_DoesNotAllocateAndObservesCoordinateChanges() {
            var at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            var snapshot = new SkyMapObserverSnapshot(52, 13, at);
            var coordinates = CelestialCoordinates(120, 30);
            var first = snapshot.ToHorizontal(coordinates);
            for (int i = 0; i < 100; i++) snapshot.ToHorizontal(coordinates);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) {
                snapshot.ToHorizontal(coordinates);
                snapshot.ToHorizontal(coordinates.RADegrees, coordinates.Dec);
            }
            (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(1024);
            coordinates.RA += 1;
            snapshot.ToHorizontal(coordinates).Should().NotBe(first);
            coordinates.RA -= 1;
            snapshot.ToHorizontal(coordinates).Should().Be(first);
            coordinates.Dec += 1;
            snapshot.ToHorizontal(coordinates).Should().NotBe(first);
            coordinates.Dec -= 1;
            snapshot.ToHorizontal(coordinates).Should().Be(first);
            coordinates.Epoch = Epoch.JNOW;
            snapshot.ToHorizontal(coordinates).Should().NotBe(first);
            coordinates.Epoch = Epoch.J2000;
            snapshot.ToHorizontal(coordinates).Should().Be(first);
        }

        [Test]
        public void CatalogProjection_ConcurrentCacheSaturationPreservesPositionsAndHotEntries() {
            var at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            var snapshot = new SkyMapObserverSnapshot(52, 13, at);
            var first = snapshot.ToHorizontal(120, 30);
            System.Threading.Tasks.Parallel.For(0, 70000, i => {
                var ra = i * 360d / 70000;
                var actual = snapshot.ToHorizontal(ra, 30);
                if (i % 257 == 0) {
                    var independent = new SkyMapObserverSnapshot(52, 13, at).ToHorizontal(ra, 30);
                    actual.Should().Be(independent);
                }
            });
            long before = GC.GetAllocatedBytesForCurrentThread();
            var retained = snapshot.ToHorizontal(120, 30);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            retained.Should().Be(first);
            bytes.Should().Be(0, "saturating a snapshot must not discard the hot positions used by every frame");
        }

        [Test]
        public void CatalogAndApparentCoordinatesAtSnapshot_DescribeSameDirection() {
            var at = new DateTime(2050, 3, 20, 22, 0, 0, DateTimeKind.Utc);
            var apparent = new Coordinates(Angle.ByHours(5), Angle.ByDegree(30), Epoch.JNOW, at);
            var catalog = apparent.Transform(Epoch.J2000);
            var snapshot = new SkyMapObserverSnapshot(52, 13, at);
            var actual = snapshot.ToHorizontal(catalog);
            var expected = snapshot.ToHorizontal(apparent);
            actual.Altitude.Should().BeApproximately(expected.Altitude, 1e-6);
            actual.Azimuth.Should().BeApproximately(expected.Azimuth, 1e-6);
        }

        [Test]
        public void UsesLocationTimeAndHorizonUntilRefreshIsDue() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            const double latitude = 50;
            const double longitude = 10;
            double siderealTime = AstroUtil.GetLocalSiderealTime(at, longitude);
            Coordinates zenith = new Coordinates(Angle.ByHours(siderealTime), Angle.ByDegree(latitude), Epoch.JNOW, at);
            Coordinates nadir = new Coordinates(Angle.ByHours(siderealTime + 12), Angle.ByDegree(-latitude), Epoch.JNOW, at);
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
            var catalogDeclination = declination;
            var (tt1, tt2) = AstroUtil.GetJulianDateTTParts(at);
            double ri = 0, di = 0, eo = 0;
            SOFA.CelestialToIntermediate(AstroUtil.ToRadians(rightAscension), AstroUtil.ToRadians(declination),
                0, 0, 0, 0, tt1, tt2, ref ri, ref di, ref eo);
            double hourAngle = AstroUtil.HoursToDegrees(siderealTime) - AstroUtil.ToDegree(SOFA.Anp(ri - eo));
            declination = AstroUtil.ToDegree(di);
            double expectedAltitude = AstroUtil.GetAltitude(hourAngle, latitude, declination);
            double expectedAzimuth = AstroUtil.GetAzimuth(hourAngle, expectedAltitude, latitude, declination);
            SkyMapObserverSnapshot sut = new SkyMapObserverSnapshot(latitude, longitude, at);

            SkyMapHorizontalCoordinates actual = sut.ToHorizontal(rightAscension, catalogDeclination);

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
