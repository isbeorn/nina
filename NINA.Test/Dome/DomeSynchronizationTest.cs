#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NUnit.Framework;
using System;
using NINA.Core.Utility;
using System.Windows.Media.Media3D;
using NINA.Astrometry;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Core.Database;
using System.IO;
using Moq;
using NINA.Profile.Interfaces;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces;
using NUnit.Framework.Legacy;

namespace NINA.Test.Dome {

    [TestFixture]
    public class DomeSynchronizationTest {
        private double siteLatitude;
        private double siteLongitude;
        private double localSiderealTime;
        private DatabaseInteraction db;
        private static Angle DEGREES_EPSILON = Angle.ByDegree(0.1);

        [SetUp]
        public void Init() {
            var ninaDbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"NINA.sqlite");
            db = new DatabaseInteraction(string.Format(@"Data Source={0};", ninaDbPath));
        }

        private DomeSynchronization Initialize(
            MountTypeEnum mountType = MountTypeEnum.EQUATORIAL,
            double domeRadius = 1000.0,
            double gemAxisLength = 0.0,
            double decOffsetHorizontal = 0.0,
            double lateralAxisLength = 0.0,
            double mountOffsetX = 0.0,
            double mountOffsetY = 0.0,
            double mountOffsetZ = 0.0,
            double siteLatitude = 41.3,
            double siteLongitude = -74.4) {
            var mockProfileService = new Mock<IProfileService>();
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.MountType).Returns(mountType);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.DomeRadius_mm).Returns(domeRadius);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.GemAxis_mm).Returns(gemAxisLength);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.DecOffsetHorizontal_mm).Returns(decOffsetHorizontal);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.LateralAxis_mm).Returns(lateralAxisLength);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionNorthSouth_mm).Returns(mountOffsetX);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionEastWest_mm).Returns(mountOffsetY);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionUpDown_mm).Returns(mountOffsetZ);
            this.siteLatitude = siteLatitude;
            this.siteLongitude = siteLongitude;
            this.localSiderealTime = AstroUtil.GetLocalSiderealTime(DateTime.Now, this.siteLongitude, this.db);
            var mountOffset = new Vector3D(mountOffsetX, mountOffsetY, mountOffsetZ);
            return new DomeSynchronization(mockProfileService.Object);
        }

        private TopocentricCoordinates CalculateCoordinates(IDomeSynchronization domeSynchronization, Coordinates coordinates, PierSide sideOfPier) {
            return domeSynchronization.TargetDomeCoordinates(coordinates, localSiderealTime, Angle.ByDegree(siteLatitude), Angle.ByDegree(siteLongitude), sideOfPier);
        }

        private Coordinates GetCoordinatesFromAltAz(double altitude, double azimuth) {
            return new TopocentricCoordinates(
                azimuth: Angle.ByDegree(azimuth),
                altitude: Angle.ByDegree(altitude),
                latitude: Angle.ByDegree(siteLatitude),
                longitude: Angle.ByDegree(siteLongitude)).Transform(Epoch.JNOW, db);
        }

        [Test]
        public void Meridian_AltAz_Test() {
            var sut = Initialize(gemAxisLength: 0);
            // An AltAz (0-length GEM axis) pointed at the meridian should result in a dome perfectly centered at 0, regardless of the side of pier
            var coordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude) + 10.0, 0);
            var eastResult = CalculateCoordinates(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateCoordinates(sut, coordinates, PierSide.pierWest);
            ClassicAssert.IsTrue(eastResult.Azimuth.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
            ClassicAssert.IsTrue(westResult.Azimuth.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));

            // The dome azimuth should be approximately the same as the scope Altitude since it is pointed almost completely straight
            ClassicAssert.IsTrue(eastResult.Altitude.Equals(Angle.ByDegree(Math.Abs(siteLatitude) + 10.0), DEGREES_EPSILON));
            ClassicAssert.IsTrue(westResult.Altitude.Equals(Angle.ByDegree(Math.Abs(siteLatitude) + 10.0), DEGREES_EPSILON));
        }

        [Test]
        public void Meridian_AltAz_SouthernHemisphere_Test() {
            var sut = Initialize(gemAxisLength: 0, siteLatitude: -41.3);
            // An AltAz (0-length GEM axis) pointed at the meridian should result in a dome perfectly centered at 0, regardless of the side of pier
            var coordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude) + 10.0, 180.0);
            var eastResult = CalculateCoordinates(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateCoordinates(sut, coordinates, PierSide.pierWest);
            ClassicAssert.IsTrue(eastResult.Azimuth.Equals(Angle.ByDegree(180.0), DEGREES_EPSILON));
            ClassicAssert.IsTrue(westResult.Azimuth.Equals(Angle.ByDegree(180.0), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(600)]
        public void Meridian_EQ_Test(double length) {
            var sut = Initialize(gemAxisLength: length);
            // When pointed at the meridian, a meridian flip when the EQ mount is perfectly centered should have the same absolute distance from 0
            var coordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude) + 10.0, 0);
            var eastResult = CalculateCoordinates(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateCoordinates(sut, coordinates, PierSide.pierWest);
            ClassicAssert.IsTrue(eastResult.Azimuth.Equals(-1.0 * westResult.Azimuth, DEGREES_EPSILON));
            ClassicAssert.IsTrue(eastResult.Azimuth.Degree >= 0 && eastResult.Azimuth.Degree <= 90);
        }

        [Test]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(600)]
        public void Meridian_EQ_SouthernHemisphere_Test(double length) {
            var sut = Initialize(gemAxisLength: length);
            // When pointed at the meridian, a meridian flip when the EQ mount is perfectly centered should have the same absolute distance from 0
            var coordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude) + 10.0, 180);
            var eastResult = CalculateCoordinates(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateCoordinates(sut, coordinates, PierSide.pierWest);
            ClassicAssert.IsTrue(eastResult.Azimuth.Equals(-1.0 * westResult.Azimuth, DEGREES_EPSILON));
            ClassicAssert.IsTrue(eastResult.Azimuth.Degree >= 90 && eastResult.Azimuth.Degree <= 180);
        }

        [Test]
        [TestCase(15)]
        [TestCase(-15)]
        [TestCase(35)]
        [TestCase(-40)]
        [TestCase(80)]
        [TestCase(90)]
        [TestCase(-90)]
        public void CelestialEquator_AltAz_Test(double azimuth) {
            // On the celestial equator, the dome azimuth should be the same as the Alt-Az mount azimuth
            var sut = Initialize(gemAxisLength: 0);
            var coordinates = GetCoordinatesFromAltAz(0, azimuth);
            var eastResult = CalculateCoordinates(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateCoordinates(sut, coordinates, PierSide.pierWest);
            ClassicAssert.IsTrue(eastResult.Azimuth.Equals(Angle.ByDegree(azimuth), DEGREES_EPSILON));
            ClassicAssert.IsTrue(westResult.Azimuth.Equals(Angle.ByDegree(azimuth), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(15)]
        [TestCase(-15)]
        [TestCase(35)]
        [TestCase(-40)]
        [TestCase(80)]
        [TestCase(90)]
        [TestCase(-90)]
        public void CelestialEquator_AltAz_SouthernHemisphere_Test(double azimuth) {
            // On the celestial equator, the dome aziumth should be the same as the Alt-Az mount azimuth
            var sut = Initialize(gemAxisLength: 0, siteLatitude: -41.3);
            var coordinates = GetCoordinatesFromAltAz(0, azimuth);
            var eastResult = CalculateCoordinates(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateCoordinates(sut, coordinates, PierSide.pierWest);
            ClassicAssert.IsTrue(eastResult.Azimuth.Equals(Angle.ByDegree(azimuth), DEGREES_EPSILON));
            ClassicAssert.IsTrue(westResult.Azimuth.Equals(Angle.ByDegree(azimuth), DEGREES_EPSILON));
        }

        [Test]
        public void NorthOffset_AltAz_Test() {
            var sut = Initialize(gemAxisLength: 0, mountOffsetX: 500, domeRadius: 1000);

            // When pointed to the east or west along the celestial equator, we expect the dome azimuth to be +/- 60 degrees, since the mount offset is half of the dome radius
            var eastCoordinates = GetCoordinatesFromAltAz(0, 90);
            ClassicAssert.IsTrue(CalculateCoordinates(sut, eastCoordinates, PierSide.pierEast).Azimuth.Equals(Angle.ByDegree(60.0), DEGREES_EPSILON));
            ClassicAssert.IsTrue(CalculateCoordinates(sut, eastCoordinates, PierSide.pierWest).Azimuth.Equals(Angle.ByDegree(60.0), DEGREES_EPSILON));

            var westCoordinates = GetCoordinatesFromAltAz(0, -90);
            ClassicAssert.IsTrue(CalculateCoordinates(sut, westCoordinates, PierSide.pierEast).Azimuth.Equals(Angle.ByDegree(-60.0), DEGREES_EPSILON));
            ClassicAssert.IsTrue(CalculateCoordinates(sut, westCoordinates, PierSide.pierWest).Azimuth.Equals(Angle.ByDegree(-60.0), DEGREES_EPSILON));
        }

        [Test]
        public void LateralOffset_CelestialPole_Test() {
            var domeRadius = 1000;
            var lateralAxisLength = 500;
            var sut = Initialize(lateralAxisLength: lateralAxisLength, domeRadius: domeRadius);

            var poleCoordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude), 0);

            var distanceFromScopeOrigin = Math.Sqrt(domeRadius * domeRadius - lateralAxisLength * lateralAxisLength);
            var northProjectionDistanceToDomeIntersection = distanceFromScopeOrigin * Math.Cos(Angle.ByDegree(this.siteLatitude).Radians);
            var expectedAzimuth = Math.Atan2(lateralAxisLength, northProjectionDistanceToDomeIntersection);

            var eastResult = CalculateCoordinates(sut, poleCoordinates, PierSide.pierEast);
            var westResult = CalculateCoordinates(sut, poleCoordinates, PierSide.pierWest);

            ClassicAssert.IsTrue(eastResult.Azimuth.Equals(Angle.ByRadians(expectedAzimuth), DEGREES_EPSILON));
            ClassicAssert.IsTrue(westResult.Azimuth.Equals(Angle.ByRadians(-expectedAzimuth), DEGREES_EPSILON));

            ClassicAssert.IsTrue(eastResult.Altitude.Equals(Angle.ByDegree(this.siteLatitude), DEGREES_EPSILON));
            ClassicAssert.IsTrue(westResult.Altitude.Equals(Angle.ByDegree(this.siteLatitude), DEGREES_EPSILON));
        }

        [Test]
        public void LateralOffset_CelestialPole_SouthernHemisphere_Test() {
            var domeRadius = 1000;
            var lateralAxisLength = 500;
            var sut = Initialize(lateralAxisLength: lateralAxisLength, domeRadius: domeRadius, siteLatitude: -41.3);

            // When pointed where the horizon and meridian intersect, we expect the dome azimuth to be +/- 60 degrees, since the lateral offset is half of the dome radius
            var poleCoordinates = GetCoordinatesFromAltAz(Math.Abs(this.siteLatitude), 180);

            var distanceFromScopeOrigin = Math.Sqrt(domeRadius * domeRadius - lateralAxisLength * lateralAxisLength);
            var southProjectionDistanceToDomeIntersection = distanceFromScopeOrigin * Math.Cos(Angle.ByDegree(this.siteLatitude).Radians);
            var expectedAzimuth = Math.Atan(lateralAxisLength / southProjectionDistanceToDomeIntersection);

            ClassicAssert.IsTrue(CalculateCoordinates(sut, poleCoordinates, PierSide.pierEast).Azimuth.Equals(Angle.ByRadians(Math.PI + expectedAzimuth), DEGREES_EPSILON));
            ClassicAssert.IsTrue(CalculateCoordinates(sut, poleCoordinates, PierSide.pierWest).Azimuth.Equals(Angle.ByRadians(Math.PI - expectedAzimuth), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(0)]
        [TestCase(200)]
        [TestCase(400)]
        public void NorthOffset_AltAz_Test(int length) {
            var sut = Initialize(gemAxisLength: length, mountOffsetX: 500, domeRadius: 1000);

            // When pointed at the celestial pole, an AltAz should still have an azimuth of 0 as long as the E/W mount offset is 0, regardless of gem length
            var poleCoordinates = GetCoordinatesFromAltAz(Math.Abs(this.siteLatitude), 0);
            ClassicAssert.IsTrue(CalculateCoordinates(sut, poleCoordinates, PierSide.pierEast).Azimuth.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
            ClassicAssert.IsTrue(CalculateCoordinates(sut, poleCoordinates, PierSide.pierWest).Azimuth.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
        }

        [Test]
        public void ForkOnWedge_Straight_Test() {
            var sut = Initialize(mountType: MountTypeEnum.FORK_ON_WEDGE, domeRadius: 1000);

            var poleCoordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude), 0);
            ClassicAssert.IsTrue(CalculateCoordinates(sut, poleCoordinates, PierSide.pierEast).Azimuth.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
            ClassicAssert.IsTrue(CalculateCoordinates(sut, poleCoordinates, PierSide.pierWest).Azimuth.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(-500)]
        [TestCase(-250)]
        [TestCase(250)]
        [TestCase(500)]
        public void ForkOnWedge_Straight_HorizontalOffset_Test(double horizontalOffset) {
            var domeRadius = 1000;
            var sut = Initialize(mountType: MountTypeEnum.FORK_ON_WEDGE, domeRadius: domeRadius, decOffsetHorizontal: horizontalOffset);
            // Set RA and Dec explicitly to ensure the fork mount isn't rotated along the RA when pointed at the pole
            var poleCoordinates = new Coordinates(
                ra: Angle.ByHours(localSiderealTime),
                dec: Angle.ByDegree(90),
                epoch: Epoch.JNOW);

            var otaToDomeDistance = Math.Sqrt(domeRadius * domeRadius - horizontalOffset * horizontalOffset);
            var northSouthDistanceProjected = Math.Cos(Angle.ByDegree(siteLatitude).Radians) * otaToDomeDistance;
            var expectedAzimuth = Angle.ByRadians(Math.Atan(horizontalOffset / northSouthDistanceProjected));
            ClassicAssert.IsTrue(CalculateCoordinates(sut, poleCoordinates, PierSide.pierEast).Azimuth.Equals(expectedAzimuth, DEGREES_EPSILON));
            ClassicAssert.IsTrue(CalculateCoordinates(sut, poleCoordinates, PierSide.pierWest).Azimuth.Equals(expectedAzimuth, DEGREES_EPSILON));
        }

        [Test]
        public void DomeTooSmallTest() {
            var sut = Initialize(gemAxisLength: 370, mountOffsetX: -30, mountOffsetY: 35, mountOffsetZ: 40, domeRadius: 1000);

            // When pointed at the celestial pole, an AltAz should still have an azimuth of 0 as long as the E/W mount offset is 0, regardless of gem length
            var coordinates = GetCoordinatesFromAltAz(64, 208);
            var result = CalculateCoordinates(sut, coordinates, PierSide.pierWest);

            Console.WriteLine();
        }
    }
}