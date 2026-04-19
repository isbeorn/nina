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
using NINA.Astrometry.Body;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class AstroUtilTest {
        private const double AngleTolerance = 1e-10;
        
        private const double DEWPOINT_TOLERANCE = 0.5;
        private static double ANGLE_TOLERANCE = 0.0000000000001;
        private static double MODULUS_TOLERANCE = 0.0001;

        [Test]
        public void ToRadians_ValueTest() {
            var degree = 180;
            var expectedRad = Math.PI;

            var rad = AstroUtil.ToRadians(degree);

            Assert.That(rad, Is.EqualTo(expectedRad));
        }

        [Test]
        public void ToDegree_ValueTest() {
            var rad = Math.PI;
            var expectedDeg = 180;

            var deg = AstroUtil.ToDegree(rad);

            Assert.That(deg, Is.EqualTo(expectedDeg));
        }

        [Test]
        public void DegreeToArcmin_ValueTest() {
            var degree = 180;
            var expectedarcmin = 10800;

            var arcmin = AstroUtil.DegreeToArcmin(degree);

            Assert.That(arcmin, Is.EqualTo(expectedarcmin));
        }

        [Test]
        public void DegreeToArcsec_ValueTest() {
            var degree = 180;
            var expectedarcsec = 648000;

            var arcsec = AstroUtil.DegreeToArcsec(degree);

            Assert.That(arcsec, Is.EqualTo(expectedarcsec));
        }

        [Test]
        public void ArcminToArcsec_ValueTest() {
            var arcmin = 20.4;
            var expectedarcsec = 1224;

            var arcsec = AstroUtil.ArcminToArcsec(arcmin);

            Assert.That(arcsec, Is.EqualTo(expectedarcsec));
        }

        [Test]
        public void ArcminToDegree_ValueTest() {
            var arcmin = 150;
            var expecteddeg = 2.5;

            var deg = AstroUtil.ArcminToDegree(arcmin);

            Assert.That(deg, Is.EqualTo(expecteddeg));
        }

        [Test]
        public void ArcsecToArcmin_ValueTest() {
            var arcsec = 150;
            var expectedarcmin = 2.5;

            var arcmin = AstroUtil.ArcsecToArcmin(arcsec);

            Assert.That(arcmin, Is.EqualTo(expectedarcmin));
        }

        [Test]
        public void ArcsecToDegree_ValueTest() {
            var arcsec = 9000;
            var expecteddeg = 2.5;

            var deg = AstroUtil.ArcsecToDegree(arcsec);

            Assert.That(deg, Is.EqualTo(expecteddeg));
        }

        [Test]
        public void HoursToDegree_ValueTest() {
            var hours = 5.2;
            var expecteddeg = 78;

            var deg = AstroUtil.HoursToDegrees(hours);

            Assert.That(deg, Is.EqualTo(expecteddeg));
        }

        [Test]
        public void DegreesToHours_ValueTest() {
            var deg = 78;
            var expectedhours = 5.2;

            var hours = AstroUtil.DegreesToHours(deg);

            Assert.That(hours, Is.EqualTo(expectedhours));
        }

        [Test]
        [TestCase(0, 0, 0, 90)]
        [TestCase(360, 0, 0, 90)]
        [TestCase(180, 0, 0, -90)]
        [TestCase(90, 0, 0, 0)]
        [TestCase(270, 0, 0, 0)]
        public void GetAltitudeTest(double angle, double latitude, double longitude, double expectedAltitude) {
            var alt = AstroUtil.GetAltitude(angle, latitude, longitude);

            Assert.That(alt, Is.EqualTo(expectedAltitude).Within(ANGLE_TOLERANCE));
        }

        [Test]
        [TestCase(0, 10, 0, 0, 270)]
        [TestCase(360, 20, 0, 10, 79.350963258685638)]
        [TestCase(180, 30, 0, 80, 360)]
        [TestCase(90, 40, 0, -80, 180)]
        [TestCase(270, 50, 0, -10, 105.6731100510834d)]
        [TestCase(0, 10, 20, 0, 266.32035559963668)]
        [TestCase(360, 20, 20, 10, 86.32035559963667)]
        [TestCase(180, 30, 20, 80, 359.99999914622634)]
        [TestCase(90, 40, 20, -80, 180)]
        [TestCase(270, 50, 20, -10, 136.15769484583683)]
        public void GetAzimuthTest(double angle, double altitude, double latitude, double declination, double expectedAzimuth) {
            var az = AstroUtil.GetAzimuth(angle, altitude, latitude, declination);

            Assert.That(az, Is.EqualTo(expectedAzimuth).Within(ANGLE_TOLERANCE));
        }

        [Test]
        [TestCase(0, "00° 00' 00\"")]
        [TestCase(90, "90° 00' 00\"")]
        [TestCase(-90, "-90° 00' 00\"")]
        [TestCase(91, "91° 00' 00\"")]
        [TestCase(-91, "-91° 00' 00\"")]
        [TestCase(72.016666666666666666, "72° 01' 00\"")] //Arcsec rounded = 60
        [TestCase(-72.016666666666666666, "-72° 01' 00\"")]//Arcsec rounded = 60
        [TestCase(33.9999999, "34° 00' 00\"")] //Arcsec rounded = 60 and arcmin will be 60
        [TestCase(-33.9999999, "-34° 00' 00\"")] //Arcsec rounded = 60 and arcmin will be 60
        public void DegreesToDMS(double degree, string expected) {
            var value = AstroUtil.DegreesToDMS(degree);

            Assert.That(value, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(0, "00:00:00")]
        [TestCase(90, "06:00:00")]
        [TestCase(-90, "-06:00:00")]
        [TestCase(91, "06:04:00")]
        [TestCase(-91, "-06:04:00")]
        [TestCase(72.016666666666666666, "04:48:04")]
        [TestCase(-72.016666666666666666, "-04:48:04")]
        [TestCase(33.9999999, "02:16:00")]
        [TestCase(-33.9999999, "-02:16:00")]
        [TestCase(75, "05:00:00")]
        [TestCase(-75, "-05:00:00")]
        [TestCase(0.248, "00:01:00")]
        [TestCase(-0.248, "-00:01:00")]
        [TestCase(14.999, "01:00:00")]
        [TestCase(-14.999, "-01:00:00")]
        public void DegreesToHMS(double degree, string expected) {
            var value = AstroUtil.DegreesToHMS(degree);

            Assert.That(value, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(0, "00:00:00")]
        [TestCase(90, "90:00:00")]
        [TestCase(-90, "-90:00:00")]
        [TestCase(91, "91:00:00")]
        [TestCase(-91, "-91:00:00")]
        [TestCase(72.016666666666666666, "72:01:00")]
        [TestCase(-72.016666666666666666, "-72:01:00")]
        [TestCase(33.9999999, "34:00:00")]
        [TestCase(-33.9999999, "-34:00:00")]
        public void HoursToHMS(double hours, string expected) {
            var value = AstroUtil.HoursToHMS(hours);

            Assert.That(value, Is.EqualTo(expected));
        }

        [Test]
        [TestCase("00°00'00\"", 0)]
        [TestCase("90°00'00\"", 90)]
        [TestCase("-90°00'00\"", -90)]
        [TestCase("91°00'00\"", 91)]
        [TestCase("-91°00'00\"", -91)]
        [TestCase("72°01'00\"", 72.016666666666666666)]
        [TestCase("-72°01'00\"", -72.016666666666666666)]
        [TestCase("34°00'00\"", 34)]
        [TestCase("-34°00'00\"", -34)]
        public void DMSToDegrees(string hms, double expected) {
            var value = AstroUtil.DMSToDegrees(hms);

            Assert.That(value, Is.EqualTo(expected).Within(ANGLE_TOLERANCE));
        }

        [Test]
        [TestCase("00°00'00\"", true)]
        [TestCase("90°00'00\"", true)]
        [TestCase("-90°00'00\"", true)]
        [TestCase("91°00'00\"", true)]
        [TestCase("-91°00'00\"", true)]
        [TestCase("72°01'00\"", true)]
        [TestCase("-72°01'00.6664\"", true)]
        [TestCase("34°00'00\"", true)]
        [TestCase("-34°00'00\"", true)]
        [TestCase("44 00 00.24", true)]
        [TestCase("-153 30 05.95", true)]
        [TestCase("+46d 46m 04s", false)]
        public void IsDmsTest(string coordinate, bool expected) {
            var value = AstroUtil.IsDMS(coordinate);

            Assert.That(value, Is.EqualTo(expected));
        }

        [Test]
        [TestCase("00 00 00", true)]
        [TestCase("13:00:00", true)]
        [TestCase("4:02:35.3452", true)]
        [TestCase("-02 00 00", false)]
        [TestCase("34°00'00\"", false)]
        public void IsHmsTest(string coordinate, bool expected) {
            var value = AstroUtil.IsHMS(coordinate);

            Assert.That(value, Is.EqualTo(expected));
        }

        [Test]
        /* Expected values taken from http://www.table-references.info/meteo-table-dew-point.php#celsius */
        [TestCase(-20, 100, -20)]
        [TestCase(-18, 100, -18)]
        [TestCase(-16, 100, -16)]
        [TestCase(-14, 100, -14)]
        [TestCase(-12, 100, -12)]
        [TestCase(-10, 100, -10)]
        [TestCase(-8, 100, -8)]
        [TestCase(-6, 100, -6)]
        [TestCase(-4, 100, -4)]
        [TestCase(-2, 100, -2)]
        [TestCase(0, 100, 0)]
        [TestCase(2, 100, 2)]
        [TestCase(4, 100, 4)]
        [TestCase(6, 100, 6)]
        [TestCase(8, 100, 8)]
        [TestCase(10, 100, 10)]
        [TestCase(12, 100, 12)]
        [TestCase(14, 100, 14)]
        [TestCase(16, 100, 16)]
        [TestCase(18, 100, 18)]
        [TestCase(20, 100, 20)]
        [TestCase(22, 100, 22)]
        [TestCase(24, 100, 24)]
        [TestCase(26, 100, 26)]
        [TestCase(28, 100, 28)]
        [TestCase(30, 100, 30)]
        [TestCase(32, 100, 32)]
        [TestCase(34, 100, 34)]
        [TestCase(36, 100, 36)]
        [TestCase(38, 100, 38)]
        [TestCase(40, 100, 40)]
        [TestCase(42, 100, 42)]
        [TestCase(44, 100, 44)]
        [TestCase(46, 100, 46)]
        [TestCase(48, 100, 48)]
        [TestCase(50, 100, 50)]
        [TestCase(-20, 90, -21.2)]
        [TestCase(-18, 90, -19.2)]
        [TestCase(-16, 90, -17.3)]
        [TestCase(-14, 90, -15.3)]
        [TestCase(-12, 90, -13.3)]
        [TestCase(-10, 90, -11.3)]
        [TestCase(-8, 90, -9.3)]
        [TestCase(-6, 90, -7.4)]
        [TestCase(-4, 90, -5.4)]
        [TestCase(-2, 90, -3.4)]
        [TestCase(0, 90, -1.4)]
        [TestCase(2, 90, 0.5)]
        [TestCase(4, 90, 2.5)]
        [TestCase(6, 90, 4.5)]
        [TestCase(8, 90, 6.5)]
        [TestCase(10, 90, 8.4)]
        [TestCase(12, 90, 10.4)]
        [TestCase(14, 90, 12.4)]
        [TestCase(16, 90, 14.4)]
        [TestCase(18, 90, 16.3)]
        [TestCase(20, 90, 18.3)]
        [TestCase(22, 90, 20.3)]
        [TestCase(24, 90, 22.3)]
        [TestCase(26, 90, 24.2)]
        [TestCase(28, 90, 26.2)]
        [TestCase(30, 90, 28.2)]
        [TestCase(32, 90, 30.1)]
        [TestCase(34, 90, 32.1)]
        [TestCase(36, 90, 34.1)]
        [TestCase(38, 90, 36.1)]
        [TestCase(40, 90, 38)]
        [TestCase(42, 90, 40)]
        [TestCase(44, 90, 42)]
        [TestCase(46, 90, 43.9)]
        [TestCase(48, 90, 45.9)]
        [TestCase(50, 90, 47.9)]
        [TestCase(-20, 80, -22.5)]
        [TestCase(-18, 80, -20.6)]
        [TestCase(-16, 80, -18.6)]
        [TestCase(-14, 80, -16.7)]
        [TestCase(-12, 80, -14.7)]
        [TestCase(-10, 80, -12.8)]
        [TestCase(-8, 80, -10.8)]
        [TestCase(-6, 80, -8.9)]
        [TestCase(-4, 80, -6.9)]
        [TestCase(-2, 80, -5)]
        [TestCase(0, 80, -3)]
        [TestCase(2, 80, -1.1)]
        [TestCase(4, 80, 0.9)]
        [TestCase(6, 80, 2.8)]
        [TestCase(8, 80, 4.8)]
        [TestCase(10, 80, 6.7)]
        [TestCase(12, 80, 8.7)]
        [TestCase(14, 80, 10.6)]
        [TestCase(16, 80, 12.5)]
        [TestCase(18, 80, 14.5)]
        [TestCase(20, 80, 16.4)]
        [TestCase(22, 80, 18.4)]
        [TestCase(24, 80, 20.3)]
        [TestCase(26, 80, 22.3)]
        [TestCase(28, 80, 24.2)]
        [TestCase(30, 80, 26.2)]
        [TestCase(32, 80, 28.1)]
        [TestCase(34, 80, 30)]
        [TestCase(36, 80, 32)]
        [TestCase(38, 80, 33.9)]
        [TestCase(40, 80, 35.9)]
        [TestCase(42, 80, 37.8)]
        [TestCase(44, 80, 39.8)]
        [TestCase(46, 80, 41.7)]
        [TestCase(48, 80, 43.6)]
        [TestCase(50, 80, 45.6)]
        [TestCase(-20, 70, -24)]
        [TestCase(-18, 70, -22.1)]
        [TestCase(-16, 70, -20.2)]
        [TestCase(-14, 70, -18.3)]
        [TestCase(-12, 70, -16.3)]
        [TestCase(-10, 70, -14.4)]
        [TestCase(-8, 70, -12.5)]
        [TestCase(-6, 70, -10.6)]
        [TestCase(-4, 70, -8.7)]
        [TestCase(-2, 70, -6.7)]
        [TestCase(0, 70, -4.8)]
        [TestCase(2, 70, -2.9)]
        [TestCase(4, 70, -1)]
        [TestCase(6, 70, 0.9)]
        [TestCase(8, 70, 2.9)]
        [TestCase(10, 70, 4.8)]
        [TestCase(12, 70, 6.7)]
        [TestCase(14, 70, 8.6)]
        [TestCase(16, 70, 10.5)]
        [TestCase(18, 70, 12.4)]
        [TestCase(20, 70, 14.4)]
        [TestCase(22, 70, 16.3)]
        [TestCase(24, 70, 18.2)]
        [TestCase(26, 70, 20.1)]
        [TestCase(28, 70, 22)]
        [TestCase(30, 70, 23.9)]
        [TestCase(32, 70, 25.8)]
        [TestCase(34, 70, 27.7)]
        [TestCase(36, 70, 29.6)]
        [TestCase(38, 70, 31.6)]
        [TestCase(40, 70, 33.5)]
        [TestCase(42, 70, 35.4)]
        [TestCase(44, 70, 37.3)]
        [TestCase(46, 70, 39.2)]
        [TestCase(48, 70, 41.1)]
        [TestCase(50, 70, 43)]
        [TestCase(-20, 60, -25.7)]
        [TestCase(-18, 60, -23.8)]
        [TestCase(-16, 60, -22)]
        [TestCase(-14, 60, -20.1)]
        [TestCase(-12, 60, -18.2)]
        [TestCase(-10, 60, -16.3)]
        [TestCase(-8, 60, -14.4)]
        [TestCase(-6, 60, -12.5)]
        [TestCase(-4, 60, -10.6)]
        [TestCase(-2, 60, -8.7)]
        [TestCase(0, 60, -6.8)]
        [TestCase(2, 60, -4.9)]
        [TestCase(4, 60, -3.1)]
        [TestCase(6, 60, -1.2)]
        [TestCase(8, 60, 0.7)]
        [TestCase(10, 60, 2.6)]
        [TestCase(12, 60, 4.5)]
        [TestCase(14, 60, 6.4)]
        [TestCase(16, 60, 8.2)]
        [TestCase(18, 60, 10.1)]
        [TestCase(20, 60, 12)]
        [TestCase(22, 60, 13.9)]
        [TestCase(24, 60, 15.7)]
        [TestCase(26, 60, 17.6)]
        [TestCase(28, 60, 19.5)]
        [TestCase(30, 60, 21.4)]
        [TestCase(32, 60, 23.2)]
        [TestCase(34, 60, 25.1)]
        [TestCase(36, 60, 27)]
        [TestCase(38, 60, 28.9)]
        [TestCase(40, 60, 30.7)]
        [TestCase(42, 60, 32.6)]
        [TestCase(44, 60, 34.5)]
        [TestCase(46, 60, 36.3)]
        [TestCase(48, 60, 38.2)]
        [TestCase(50, 60, 40.1)]
        [TestCase(-20, 50, -27.7)]
        [TestCase(-18, 50, -25.9)]
        [TestCase(-16, 50, -24)]
        [TestCase(-14, 50, -22.1)]
        [TestCase(-12, 50, -20.3)]
        [TestCase(-10, 50, -18.4)]
        [TestCase(-8, 50, -16.6)]
        [TestCase(-6, 50, -14.7)]
        [TestCase(-4, 50, -12.9)]
        [TestCase(-2, 50, -11)]
        [TestCase(0, 50, -9.2)]
        [TestCase(2, 50, -7.3)]
        [TestCase(4, 50, -5.5)]
        [TestCase(6, 50, -3.6)]
        [TestCase(8, 50, -1.8)]
        [TestCase(10, 50, 0.1)]
        [TestCase(12, 50, 1.9)]
        [TestCase(14, 50, 3.7)]
        [TestCase(16, 50, 5.6)]
        [TestCase(18, 50, 7.4)]
        [TestCase(20, 50, 9.3)]
        [TestCase(22, 50, 11.1)]
        [TestCase(24, 50, 12.9)]
        [TestCase(26, 50, 14.8)]
        [TestCase(28, 50, 16.6)]
        [TestCase(30, 50, 18.4)]
        [TestCase(32, 50, 20.3)]
        [TestCase(34, 50, 22.1)]
        [TestCase(36, 50, 23.9)]
        [TestCase(38, 50, 25.7)]
        [TestCase(40, 50, 27.6)]
        [TestCase(42, 50, 29.4)]
        [TestCase(44, 50, 31.2)]
        [TestCase(46, 50, 33)]
        [TestCase(48, 50, 34.9)]
        [TestCase(50, 50, 36.7)]
        [TestCase(-20, 40, -30.1)]
        [TestCase(-18, 40, -28.3)]
        [TestCase(-16, 40, -26.5)]
        [TestCase(-14, 40, -24.6)]
        [TestCase(-12, 40, -22.8)]
        [TestCase(-10, 40, -21)]
        [TestCase(-8, 40, -19.2)]
        [TestCase(-6, 40, -17.4)]
        [TestCase(-4, 40, -15.6)]
        [TestCase(-2, 40, -13.8)]
        [TestCase(0, 40, -12)]
        [TestCase(2, 40, -10.2)]
        [TestCase(4, 40, -8.4)]
        [TestCase(6, 40, -6.6)]
        [TestCase(8, 40, -4.8)]
        [TestCase(10, 40, -3)]
        [TestCase(12, 40, -1.2)]
        [TestCase(14, 40, 0.6)]
        [TestCase(16, 40, 2.4)]
        [TestCase(18, 40, 4.2)]
        [TestCase(20, 40, 6)]
        [TestCase(22, 40, 7.8)]
        [TestCase(24, 40, 9.6)]
        [TestCase(26, 40, 11.3)]
        [TestCase(28, 40, 13.1)]
        [TestCase(30, 40, 14.9)]
        [TestCase(32, 40, 16.7)]
        [TestCase(34, 40, 18.5)]
        [TestCase(36, 40, 20.2)]
        [TestCase(38, 40, 22)]
        [TestCase(40, 40, 23.8)]
        [TestCase(42, 40, 25.6)]
        [TestCase(44, 40, 27.3)]
        [TestCase(46, 40, 29.1)]
        [TestCase(48, 40, 30.9)]
        [TestCase(50, 40, 32.6)]
        [TestCase(-20, 30, -33.1)]
        [TestCase(-18, 30, -31.3)]
        [TestCase(-16, 30, -29.5)]
        [TestCase(-14, 30, -27.8)]
        [TestCase(-12, 30, -26)]
        [TestCase(-10, 30, -24.3)]
        [TestCase(-8, 30, -22.5)]
        [TestCase(-6, 30, -20.7)]
        [TestCase(-4, 30, -19)]
        [TestCase(-2, 30, -17.2)]
        [TestCase(0, 30, -15.5)]
        [TestCase(2, 30, -13.7)]
        [TestCase(4, 30, -12)]
        [TestCase(6, 30, -10.3)]
        [TestCase(8, 30, -8.5)]
        [TestCase(10, 30, -6.8)]
        [TestCase(12, 30, -5)]
        [TestCase(14, 30, -3.3)]
        [TestCase(16, 30, -1.6)]
        [TestCase(18, 30, 0.2)]
        [TestCase(20, 30, 1.9)]
        [TestCase(22, 30, 3.6)]
        [TestCase(24, 30, 5.3)]
        [TestCase(26, 30, 7.1)]
        [TestCase(28, 30, 8.8)]
        [TestCase(30, 30, 10.5)]
        [TestCase(32, 30, 12.2)]
        [TestCase(34, 30, 13.9)]
        [TestCase(36, 30, 15.7)]
        [TestCase(38, 30, 17.4)]
        [TestCase(40, 30, 19.1)]
        [TestCase(42, 30, 20.8)]
        [TestCase(44, 30, 22.5)]
        [TestCase(46, 30, 24.2)]
        [TestCase(48, 30, 25.9)]
        [TestCase(50, 30, 27.6)]
        [TestCase(-20, 20, -37.1)]
        [TestCase(-18, 20, -35.4)]
        [TestCase(-16, 20, -33.7)]
        [TestCase(-14, 20, -32)]
        [TestCase(-12, 20, -30.3)]
        [TestCase(-10, 20, -28.7)]
        [TestCase(-8, 20, -27)]
        [TestCase(-6, 20, -25.3)]
        [TestCase(-4, 20, -23.6)]
        [TestCase(-2, 20, -21.9)]
        [TestCase(0, 20, -20.3)]
        [TestCase(2, 20, -18.6)]
        [TestCase(4, 20, -16.9)]
        [TestCase(6, 20, -15.3)]
        [TestCase(8, 20, -13.6)]
        [TestCase(10, 20, -11.9)]
        [TestCase(12, 20, -10.3)]
        [TestCase(14, 20, -8.6)]
        [TestCase(16, 20, -7)]
        [TestCase(18, 20, -5.3)]
        [TestCase(20, 20, -3.6)]
        [TestCase(22, 20, -2)]
        [TestCase(24, 20, -0.4)]
        [TestCase(26, 20, 1.3)]
        [TestCase(28, 20, 2.9)]
        [TestCase(30, 20, 4.6)]
        [TestCase(32, 20, 6.2)]
        [TestCase(34, 20, 7.8)]
        [TestCase(36, 20, 9.5)]
        [TestCase(38, 20, 11.1)]
        [TestCase(40, 20, 12.7)]
        [TestCase(42, 20, 14.4)]
        [TestCase(44, 20, 16)]
        [TestCase(46, 20, 17.6)]
        [TestCase(48, 20, 19.2)]
        [TestCase(50, 20, 20.8)]
        public void ApproximateDewPointTest(double temp, double humidity, double expected) {
            var dp = AstroUtil.ApproximateDewPoint(temp, humidity);

            Assert.That(dp, Is.EqualTo(expected).Within(DEWPOINT_TOLERANCE));
        }

        [Test]
        [TestCase("00:00:00", 0)]
        [TestCase("1:00:00", 15)]
        [TestCase("-1:00:00", -15)]
        [TestCase("23:59:59", 359.99583333333339)]
        [TestCase("-23:59:59", -359.99583333333339)]
        [TestCase("5:30:0", 82.5)]
        [TestCase("-5:30:0", -82.5)]
        public void HMSToDegrees(string hms, double expected) {
            var value = AstroUtil.HMSToDegrees(hms);

            Assert.That(value, Is.EqualTo(expected).Within(ANGLE_TOLERANCE));
        }

        [Test]
        [TestCase(0, 0, 0)]
        [TestCase(12, 4, 8.0)]
        [TestCase(24, 24, 0)]
        [TestCase(1.657982, 21.657498, 4.0004840000000002)]
        [TestCase(22.68498, 15.135684, 7.549296)]
        public void GetHourAngleTest(double siderealTime, double rightAscension, double expectedHourAngle) {
            var hourAngle = AstroUtil.GetHourAngle(siderealTime, rightAscension);

            Assert.That(hourAngle, Is.EqualTo(expectedHourAngle).Within(ANGLE_TOLERANCE));
        }

        [Test]
        [TestCase(182, 360, 182)]
        [TestCase(365, 360, 5)]
        [TestCase(-20, 360, 340)]
        [TestCase(832, 360, 112)]
        [TestCase(832, 360.5f, 111)]
        [TestCase(-380, 360, 340)]
        [TestCase(-10, -360, -10)]
        [TestCase(3, 7, 3)]
        [TestCase(3, -7, -4)]
        [TestCase(-3, 7, 4)]
        [TestCase(-3, -7, -3)]
        [TestCase(7, 3, 1)]
        [TestCase(7, -3, -2)]
        [TestCase(-7, 3, 2)]
        [TestCase(-7, -3, -1)]
        [TestCase(10.2f, 10, 0.2f)]
        [TestCase(10.2f, 10.5f, 10.2f)]
        [TestCase(float.MaxValue, float.MaxValue, 0)]
        [TestCase(150, float.MaxValue, 150)]
        [TestCase(float.MaxValue, 10, 0)]
        [TestCase(12.55f, 10.32f, 2.23f)]
        [TestCase(122.55f, 10.32f, 9.03f)]
        public void GetEuclidianModulus(float x, float y, float expected) {
            var modulus = AstroUtil.EuclidianModulus(x, y);

            Assert.That(modulus, Is.EqualTo(expected).Within(MODULUS_TOLERANCE));
        }

        [Test]
        [TestCase(182, 360, 182)]
        [TestCase(365, 360, 5)]
        [TestCase(-20, 360, 340)]
        [TestCase(832, 360, 112)]
        [TestCase(832, 360.5f, 111)]
        [TestCase(-380, 360, 340)]
        [TestCase(-10, -360, -10)]
        [TestCase(3, 7, 3)]
        [TestCase(3, -7, -4)]
        [TestCase(-3, 7, 4)]
        [TestCase(-3, -7, -3)]
        [TestCase(7, 3, 1)]
        [TestCase(7, -3, -2)]
        [TestCase(-7, 3, 2)]
        [TestCase(-7, -3, -1)]
        [TestCase(10.2f, 10, 0.2f)]
        [TestCase(10.2f, 10.5f, 10.2f)]
        [TestCase(double.MaxValue, double.MaxValue, 0)]
        [TestCase(150, double.MaxValue, 150)]
        [TestCase(double.MaxValue, 10, 8)]
        [TestCase(12.55f, 10.32f, 2.23f)]
        [TestCase(122.55f, 10.32f, 9.03f)]
        public void GetEuclidianModulus(double x, double y, double expected) {
            var modulus = AstroUtil.EuclidianModulus(x, y);

            Assert.That(modulus, Is.EqualTo(expected).Within(MODULUS_TOLERANCE));
        }

        [TestCase(35d, 2.5, 20.5, ExpectedResult = 9.56)]
        public double DeterminePolarAlignmentError(double startDeclination, double driftRate, double declinationError) {
            return Math.Round(AstroUtil.DegreeToArcmin(AstroUtil.DetermineDriftAlignError(startDeclination, driftRate, AstroUtil.ArcsecToDegree(declinationError))), 2);
        }

        [TestCase(1, 100, 2.06264806)]
        [TestCase(3.8, 700, 1.1197232)]
        public void ArcSecPerPixel_CorrectlyTransformed(double pixelSize, double focalLength, double expected) {
            var px = AstroUtil.ArcsecPerPixel(pixelSize, focalLength);

            px.Should().BeApproximately(expected, 0.00001);
        }
        
        [Test]
        [TestCase(1, 1005d, 7d, 80d, 0.574d, double.NaN, 1)]
        [TestCase(3, 1005d, 7d, 80d, 0.574d, 672, 1)]
        [TestCase(4, 1005d, 7d, 80d, 0.574d, 631, 1)]
        [TestCase(5, 1005d, 7d, 80d, 0.574d, 557, 1)]
        // Below Test Cases are taken from SOFA Documented values for method iauRefco
        [TestCase(10, 1005d, 7d, 80d, 0.574d, 318.55, 4)]
        [TestCase(12, 1005d, 7d, 80d, 0.574d, 267.29, 3)]
        [TestCase(14, 1005d, 7d, 80d, 0.574d, 229.43, 2)]
        [TestCase(16, 1005d, 7d, 80d, 0.574d, 200.38, 2)]
        [TestCase(18, 1005d, 7d, 80d, 0.574d, 177.37, 2)]
        [TestCase(20, 1005d, 7d, 80d, 0.574d, 158.68, 2)]
        [TestCase(25, 1005d, 7d, 80d, 0.574d, 124.26, 2)]
        [TestCase(30, 1005d, 7d, 80d, 0.574d, 100.54, 2)]
        [TestCase(35, 1005d, 7d, 80d, 0.574d, 82.99 , 1)]
        [TestCase(40, 1005d, 7d, 80d, 0.574d, 69.30 , 1)]
        [TestCase(45, 1005d, 7d, 80d, 0.574d, 58.18 , 1)]
        [TestCase(50, 1005d, 7d, 80d, 0.574d, 48.83 , 1)]
        [TestCase(60, 1005d, 7d, 80d, 0.574d, 33.61 , 1)]
        [TestCase(70, 1005d, 7d, 80d, 0.574d, 21.20 , 1)]
        [TestCase(80, 1005d, 7d, 80d, 0.574d, 10.27 , 1)]
        public void CalculateRefractedAltitudeTest(double altitude, double pressure, double temperature, double humidity, double wavelength, double expectedArcsecDistance, double tolerance) {
            var result = AstroUtil.CalculateRefractedAltitude(altitude, pressure, temperature, humidity, wavelength);

            var arcsec = Math.Abs(AstroUtil.DegreeToArcsec(altitude - result));

            //Should be within <tolerance> arcseconds precision
            if(double.IsNaN(expectedArcsecDistance)) {
                arcsec.Should().Be(double.NaN);
            } else {
                arcsec.Should().BeApproximately(expectedArcsecDistance, tolerance);
            }            
        }

        [Test]
        [TestCase("Right ascension\t00h 42m 44.3s[1]", 10.684583333333)] // Wikipedia
        [TestCase("RA center: 00h42m29s.54", 10.62083333333)] // Astrobin
        [TestCase("20hr 34' 54\"", 308.725)] // Telescopius
        [TestCase("20hr 34′ 54″", 308.725)] // Telescopius
        public void ExtractHMS_ValidInput_SuccessfullyMatches(string sut, double expectedDegree) {
            var pattern = AstroUtil.HMSPattern;
            var match = Regex.Match(sut, pattern);
            match.Success.Should().BeTrue();

            AstroUtil.HMSToDegrees(match.Value).Should().BeApproximately(expectedDegree, MODULUS_TOLERANCE);
        }

        [Test]
        [TestCase("Declination\t+41° 16′ 9″[1]")] // Wikipedia
        [TestCase("DEC center: +41°11′12″.2")] // Astrobin
        [TestCase("60º 09' 00\"")] // Telescopius
        public void ExtractHMS_InvalidInput_FailsMatches(string sut) {
            var pattern = AstroUtil.HMSPattern;
            var match = Regex.Match(sut, pattern);
            match.Success.Should().BeFalse();
        }

        [Test]
        [TestCase("Declination\t+41° 16′ 9″[1]", 41.2691666666)] // Wikipedia
        [TestCase("Declination\t+41° 16' 9\"[1]", 41.2691666666)] // Wikipedia
        [TestCase("DEC center: +41°11'12\".2", 41.186666666)] // Astrobin
        [TestCase("DEC center: +41°11′12″.2", 41.186666666)] // Astrobin
        [TestCase("60º 09' 00\"", 60.15)] // Telescopius
        [TestCase("07 50 34.9", 7.843028)] // NASA Horizon
        [TestCase("+41 49 29", 41.824722)] // MPC
        public void ExtractDMS_ValidInput_SuccessfullyMatches(string sut, double expectedDegree) {
            var pattern = AstroUtil.DMSPattern;
            var match = Regex.Match(sut, pattern);
            match.Success.Should().BeTrue();

            AstroUtil.DMSToDegrees(match.Value).Should().BeApproximately(expectedDegree, MODULUS_TOLERANCE);
        }

        [Test]
        [TestCase("Right ascension\t00h 42m 44.3s[1]")] // Wikipedia
        [TestCase("RA center: 00h42m29s.54")] // Astrobin
        [TestCase("20hr 34' 54\"")] // Telescopius
        public void ExtractDMS_InvalidInput_FailsMatches(string sut) {
            var pattern = AstroUtil.DMSPattern;
            var match = Regex.Match(sut, pattern);
            match.Success.Should().BeFalse();
        }

        /// <summary>
        /// Verifies UTC Julian Date conversion against standard epoch examples from Jean Meeus,
        /// Astronomical Algorithms, so calendar-to-Julian conversion stays anchored to published values.
        /// Reference: https://www.obliquity.com/astro/meeus.html
        /// </summary>
        [Test]
        [TestCase(2000, 1, 1, 12, 0, 0, 2451545.0)]
        [TestCase(1987, 1, 27, 0, 0, 0, 2446822.5)]
        [TestCase(1987, 6, 19, 12, 0, 0, 2446966.0)]
        [TestCase(1988, 1, 27, 0, 0, 0, 2447187.5)]
        public void GetJulianDate_KnownAstronomicalEpochs_ReturnsPublishedJulianDate(int year, int month, int day, int hour, int minute, int second, double expectedJulianDate) {
            DateTime utc = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

            double julianDate = AstroUtil.GetJulianDate(utc);

            julianDate.Should().BeApproximately(expectedJulianDate, 1e-9);
            utc.ToJD().Should().BeApproximately(expectedJulianDate, 1e-9);
            utc.ToMJD().Should().BeApproximately(expectedJulianDate - 2400000.5, 1e-9);
            utc.ToMJD2000().Should().BeApproximately(expectedJulianDate - 2451545.0, 1e-9);
        }

        /// <summary>
        /// Verifies TT conversion at the J2000 UTC instant, where TT is offset by TAI-UTC plus
        /// 32.184 seconds; this catches leap-second and time-scale regressions in the SOFA path.
        /// References: https://www.iers.org/IERS/EN/Service/FAQs/TheNewIAUResolutions/timeArgumentForUsingIERSProducts_104_157
        /// and https://www.cnmoc.usff.navy.mil/Our-Commands/United-States-Naval-Observatory/Precise-Time-Department/Global-Positioning-System/USNO-GPS-Time-Transfer/Leap-Seconds/
        /// </summary>
        [Test]
        public void GetJulianDateTT_J2000Utc_ContainsTerrestrialTimeOffset() {
            DateTime utc = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            double expectedTt = 2451545.0 + (32.0 + 32.184) / 86400.0;

            double julianDateTt = AstroUtil.GetJulianDateTT(utc);

            julianDateTt.Should().BeApproximately(expectedTt, 1e-9);
        }

        /// <summary>
        /// Verifies the Earth-rotation database lookup used by Delta-T selects the nearest UT1-UTC
        /// sample for the requested UTC date, matching the IERS finals data model of daily samples.
        /// Reference: https://www.iers.org/IERS/EN/DataProducts/EarthOrientationData/eop.html
        /// </summary>
        [Test]
        public void DeltaUT_EarthRotationTable_UsesNearestUt1MinusUtcSample() {
            DateTime target = new DateTime(2030, 1, 3, 18, 0, 0, DateTimeKind.Utc);
            using TempEarthRotationDatabase database = CreateEarthRotationDatabase(
                (new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), -0.25),
                (new DateTime(2030, 1, 4, 0, 0, 0, DateTimeKind.Utc), 0.125));
            ClearDeltaUTCaches();

            double deltaUT = AstroUtil.DeltaUT(target, database.Interaction);

            deltaUT.Should().BeApproximately(0.125, 1e-12);
        }

        /// <summary>
        /// Verifies the Delta-T formula: Delta-T equals 32.184 seconds plus TAI-UTC minus UT1-UTC.
        /// The controlled UT1-UTC row guards the sign of the Earth-rotation correction.
        /// References: https://www.cnmoc.usff.navy.mil/Our-Commands/United-States-Naval-Observatory/Precise-Time-Department/Global-Positioning-System/USNO-GPS-Time-Transfer/Leap-Seconds/
        /// and https://www.iers.org/IERS/EN/DataProducts/EarthOrientationData/eop.html
        /// </summary>
        [Test]
        public void DeltaT_ControlledUt1MinusUtc_SubtractsEarthRotationCorrection() {
            DateTime date = new DateTime(2024, 4, 8, 18, 0, 0, DateTimeKind.Utc);
            using TempEarthRotationDatabase database = CreateEarthRotationDatabase((date, 0.123456));
            ClearDeltaUTCaches();

            double deltaT = AstroUtil.DeltaT(date, database.Interaction);

            deltaT.Should().BeApproximately(32.184 + 37.0 - 0.123456, 1e-6);
        }

        /// <summary>
        /// Verifies that fractional seconds are preserved when SOFA receives UTC parts, because
        /// sub-second exposure timing is relevant for precise astrometry and satellite work.
        /// </summary>
        [Test]
        public void GetSecondOfMinuteWithFraction_MillisecondTime_PreservesFractionalSecond() {
            DateTime timestamp = new DateTime(2024, 4, 8, 18, 17, 42, 375, DateTimeKind.Utc).AddTicks(1200);

            double second = AstroUtil.GetSecondOfMinuteWithFraction(timestamp);

            second.Should().BeApproximately(42.37512, 1e-12);
        }

        /// <summary>
        /// Verifies that local sidereal time shifts by exactly one sidereal hour per 15 degrees
        /// longitude, preserving the astronomical sign convention used by mount pointing.
        /// </summary>
        [Test]
        public void GetLocalSiderealTime_LongitudeOffset_ChangesByOneHourPerFifteenDegrees() {
            DateTime utc = new DateTime(2024, 3, 20, 0, 0, 0, DateTimeKind.Utc);

            double greenwichSiderealTime = AstroUtil.GetLocalSiderealTime(utc, 0.0);
            double eastSiderealTime = AstroUtil.GetLocalSiderealTime(utc, 15.0);
            double westSiderealTime = AstroUtil.GetLocalSiderealTime(utc, -30.0);

            eastSiderealTime.Should().BeApproximately(greenwichSiderealTime + 1.0, 1e-10);
            westSiderealTime.Should().BeApproximately(greenwichSiderealTime - 2.0, 1e-10);
        }

        /// <summary>
        /// Verifies Greenwich sidereal time at the J2000 epoch against the standard 18.697374558 hour
        /// reference for Greenwich mean sidereal time, allowing a small tolerance because this code asks
        /// NOVAS for apparent sidereal time and therefore includes nutation/equation-of-equinoxes terms.
        /// Reference: https://aa.usno.navy.mil/faq/GAST
        /// </summary>
        [Test]
        public void GetLocalSiderealTime_J2000Greenwich_ReturnsPublishedSiderealEpochValue() {
            DateTime j2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            double siderealTime = AstroUtil.GetLocalSiderealTime(j2000, 0.0);

            siderealTime.Should().BeApproximately(18.697374558, 0.01);
        }

        /// <summary>
        /// Verifies local sidereal time consumes UT1-UTC from the Earth-rotation table: one second
        /// of UT1 changes sidereal time by one sidereal second, about 1.0027379 solar seconds.
        /// Reference: https://aa.usno.navy.mil/faq/GAST
        /// </summary>
        [Test]
        public void GetLocalSiderealTime_DifferentUt1MinusUtcRows_ShiftsBySiderealSecond() {
            DateTime date = new DateTime(2024, 4, 8, 18, 0, 0, DateTimeKind.Utc);
            using TempEarthRotationDatabase zeroUt1Database = CreateEarthRotationDatabase((date, 0.0));
            using TempEarthRotationDatabase oneSecondUt1Database = CreateEarthRotationDatabase((date, 1.0));

            ClearDeltaUTCaches();
            double zeroUt1SiderealTime = AstroUtil.GetLocalSiderealTime(date, 0.0, zeroUt1Database.Interaction);
            ClearDeltaUTCaches();
            double oneSecondUt1SiderealTime = AstroUtil.GetLocalSiderealTime(date, 0.0, oneSecondUt1Database.Interaction);

            (oneSecondUt1SiderealTime - zeroUt1SiderealTime).Should().BeApproximately(1.0027379 / 3600.0, 1e-7);
        }

        /// <summary>
        /// Verifies small conversion helpers used by sidereal and formatting calculations,
        /// including the zero-divisor modulus branch that represents an undefined wrap interval.
        /// </summary>
        [Test]
        public void AngleAndModulusHelpers_CoreBranches_ReturnExpectedValues() {
            AstroUtil.RadianToHour(Math.PI).Should().BeApproximately(12.0, AngleTolerance);
            AstroUtil.MathMod(-10.0, 360.0).Should().BeApproximately(350.0, AngleTolerance);
            AstroUtil.MathMod(10.0, 0.0).Should().Be(double.NaN);
            AstroUtil.GetLocalSiderealTimeNow(0.0).Should().NotBe(double.NaN);
        }

        /// <summary>
        /// Verifies inverse hour-angle conversion without normalization, matching the formula
        /// RA = local sidereal time minus hour angle used in mount-side coordinate math.
        /// </summary>
        [Test]
        public void GetRightAscensionFromHourAngle_SubtractsHourAngleFromSiderealTime() {
            Angle rightAscension = AstroUtil.GetRightAscensionFromHourAngle(Angle.ByHours(7.0), Angle.ByHours(5.0));

            rightAscension.Hours.Should().BeApproximately(-2.0, AngleTolerance);
        }

        /// <summary>
        /// Verifies the closed-form equatorial altitude relation at upper and lower culmination,
        /// a core spherical-astronomy identity for converting hour angle and declination to altitude.
        /// </summary>
        [Test]
        [TestCase(51.5, 23.44, 0.0, 61.94)]
        [TestCase(51.5, 23.44, 180.0, -15.06)]
        [TestCase(-30.0, -60.0, 0.0, 60.0)]
        [TestCase(-30.0, -60.0, 180.0, 0.0)]
        public void GetAltitude_CulminationGeometry_ReturnsMeridianAltitudes(double latitude, double declination, double hourAngle, double expectedAltitude) {
            double altitude = AstroUtil.GetAltitude(hourAngle, latitude, declination);

            altitude.Should().BeApproximately(expectedAltitude, 1e-10);
        }

        /// <summary>
        /// Verifies azimuth at meridian transit for targets north and south of the zenith, which
        /// guards the branch logic that chooses north-versus-south culmination.
        /// </summary>
        [Test]
        [TestCase(51.5, 23.44, 180.0)]
        [TestCase(20.0, 70.0, 0.0)]
        [TestCase(-30.0, -60.0, 180.0)]
        public void GetAzimuth_MeridianTransit_IdentifiesNorthOrSouthTransit(double latitude, double declination, double expectedAzimuth) {
            double altitude = AstroUtil.GetAltitude(0.0, latitude, declination);

            double azimuth = AstroUtil.GetAzimuth(0.0, altitude, latitude, declination);

            AngularDifference(azimuth, expectedAzimuth).Should().BeLessThan(1e-5);
        }

        /// <summary>
        /// Verifies Greenwich equinox sunrise and sunset against public almanac expectations to
        /// within a practical tolerance for the quadratic interpolation used by RiseAndSetEvent.
        /// Reference: https://www.suntoday.org/sunrise-sunset/2024/march.html
        /// </summary>
        [Test]
        public void GetSunRiseAndSet_GreenwichNearMarchEquinox_MatchesAlmanacTimesWithinMinutes() {
            DateTime referenceDate = new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);

            RiseAndSetEvent sun = AstroUtil.GetSunRiseAndSet(referenceDate, 51.4769, 0.0, 46.0);

            sun.Rise.Should().NotBeNull();
            sun.Set.Should().NotBeNull();
            AssertCloseToTime(sun.Rise, new DateTime(2024, 3, 21, 6, 1, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(20));
            AssertCloseToTime(sun.Set, new DateTime(2024, 3, 20, 18, 13, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(20));
        }

        /// <summary>
        /// Verifies that civil, nautical, and astronomical twilight are ordered by the physical
        /// solar depression thresholds of -6, -12, and -18 degrees.
        /// Reference: https://gml.noaa.gov/grad/solcalc/calcdetails.html
        /// </summary>
        [Test]
        public void TwilightRiseAndSet_GreenwichNearMarchEquinox_OrdersBySolarDepression() {
            DateTime referenceDate = new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);

            RiseAndSetEvent civil = AstroUtil.GetCivilNightTimes(referenceDate, 51.4769, 0.0, 46.0);
            RiseAndSetEvent nautical = AstroUtil.GetNauticalNightTimes(referenceDate, 51.4769, 0.0, 46.0);
            RiseAndSetEvent astronomical = AstroUtil.GetNightTimes(referenceDate, 51.4769, 0.0, 46.0);

            civil.Set.Should().BeBefore(nautical.Set.Value);
            nautical.Set.Should().BeBefore(astronomical.Set.Value);
            astronomical.Rise.Should().BeBefore(nautical.Rise.Value);
            nautical.Rise.Should().BeBefore(civil.Rise.Value);
        }

        /// <summary>
        /// Verifies polar day behavior at Tromso near June solstice, where the Sun should not cross
        /// the apparent horizon and the rise/set solver must report no event.
        /// Reference: https://www.sunrise-and-sunset.com/en/sun/norway/tromso/2024
        /// </summary>
        [Test]
        public void GetSunRiseAndSet_TromsoJuneSolstice_DetectsMidnightSunNoCrossing() {
            DateTime referenceDate = new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc);

            RiseAndSetEvent sun = AstroUtil.GetSunRiseAndSet(referenceDate, 69.6492, 18.9553, 10.0);

            sun.Rise.Should().BeNull();
            sun.Set.Should().BeNull();
        }

        /// <summary>
        /// Verifies polar night behavior at Tromso near December solstice, where the Sun remains
        /// below the apparent horizon and no sunrise or sunset crossing should be produced.
        /// Reference: https://www.sunrise-and-sunset.com/en/sun/norway/tromso/2024
        /// </summary>
        [Test]
        public void GetSunRiseAndSet_TromsoDecemberSolstice_DetectsPolarNightNoCrossing() {
            DateTime referenceDate = new DateTime(2024, 12, 21, 12, 0, 0, DateTimeKind.Utc);

            RiseAndSetEvent sun = AstroUtil.GetSunRiseAndSet(referenceDate, 69.6492, 18.9553, 10.0);

            sun.Rise.Should().BeNull();
            sun.Set.Should().BeNull();
        }

        /// <summary>
        /// Verifies lunar illumination near the 2024 total solar eclipse new moon, an externally
        /// recognizable syzygy where the illuminated fraction should be very small.
        /// Reference: https://science.nasa.gov/eclipses/future-eclipses/eclipse-2024/
        /// </summary>
        [Test]
        public void GetMoonIllumination_NewMoonAtSolarEclipse_ReturnsDarkMoon() {
            DateTime eclipseNewMoon = new DateTime(2024, 4, 8, 18, 21, 0, DateTimeKind.Utc);
            ObserverInfo observer = new ObserverInfo { Latitude = 29.7604, Longitude = -95.3698, Elevation = 15.0 };

            double illumination = AstroUtil.GetMoonIllumination(eclipseNewMoon, observer);
            AstroUtil.MoonPhase phase = AstroUtil.GetMoonPhase(eclipseNewMoon, observer);

            phase.Should().BeOneOf(AstroUtil.MoonPhase.WaningCrescent, AstroUtil.MoonPhase.NewMoon, AstroUtil.MoonPhase.WaxingCrescent);
            illumination.Should().BeLessThan(0.02);
        }

        /// <summary>
        /// Verifies lunar illumination near the 2024 March full moon, where Sun-Moon
        /// elongation is near opposition and the illuminated fraction should be near unity.
        /// Reference: https://moon.nasa.gov/moon-in-motion/moon-phases/
        /// </summary>
        [Test]
        public void GetMoonIllumination_FullMoon_ReturnsBrightMoon() {
            DateTime fullMoon = new DateTime(2024, 3, 25, 7, 0, 0, DateTimeKind.Utc);
            ObserverInfo observer = new ObserverInfo { Latitude = 51.4769, Longitude = 0.0, Elevation = 46.0 };

            double illumination = AstroUtil.GetMoonIllumination(fullMoon, observer);
            AstroUtil.MoonPhase phase = AstroUtil.GetMoonPhase(fullMoon, observer);

            phase.Should().BeOneOf(AstroUtil.MoonPhase.WaxingGibbous, AstroUtil.MoonPhase.FullMoon, AstroUtil.MoonPhase.WaningGibbous);
            illumination.Should().BeGreaterThan(0.98);
        }

        /// <summary>
        /// Verifies that the 2024 March full moon has both moonrise and moonset around Greenwich,
        /// covering the lunar rise/set path with the Moon-specific apparent-limb horizon threshold.
        /// Reference: https://moon.nasa.gov/moon-in-motion/moon-phases/
        /// </summary>
        [Test]
        public void GetMoonRiseAndSet_GreenwichFullMoon_FindsBothEvents() {
            DateTime referenceDate = new DateTime(2024, 3, 25, 12, 0, 0, DateTimeKind.Utc);

            RiseAndSetEvent moon = AstroUtil.GetMoonRiseAndSet(referenceDate, 51.4769, 0.0, 46.0);

            moon.Rise.Should().NotBeNull();
            moon.Set.Should().NotBeNull();
            moon.Rise.Value.Should().BeAfter(referenceDate);
            moon.Set.Value.Should().BeAfter(referenceDate);
        }

        /// <summary>
        /// Verifies legacy no-elevation rise/set overloads still delegate to the elevation-aware
        /// implementations, preserving old call sites while keeping the same astronomical events.
        /// </summary>
        [Test]
        public void RiseAndSetCompatibilityOverloads_NoElevation_DelegateToZeroElevationImplementations() {
            DateTime referenceDate = new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);
#pragma warning disable CS0618
            RiseAndSetEvent astronomicalLegacy = AstroUtil.GetNightTimes(referenceDate, 51.4769, 0.0);
            RiseAndSetEvent nauticalLegacy = AstroUtil.GetNauticalNightTimes(referenceDate, 51.4769, 0.0);
            RiseAndSetEvent civilLegacy = AstroUtil.GetCivilNightTimes(referenceDate, 51.4769, 0.0);
            RiseAndSetEvent sunLegacy = AstroUtil.GetSunRiseAndSet(referenceDate, 51.4769, 0.0);
            RiseAndSetEvent moonLegacy = AstroUtil.GetMoonRiseAndSet(referenceDate, 51.4769, 0.0);
#pragma warning restore CS0618

            astronomicalLegacy.Rise.Should().Be(AstroUtil.GetNightTimes(referenceDate, 51.4769, 0.0, 0.0).Rise);
            nauticalLegacy.Rise.Should().Be(AstroUtil.GetNauticalNightTimes(referenceDate, 51.4769, 0.0, 0.0).Rise);
            civilLegacy.Rise.Should().Be(AstroUtil.GetCivilNightTimes(referenceDate, 51.4769, 0.0, 0.0).Rise);
            sunLegacy.Rise.Should().Be(AstroUtil.GetSunRiseAndSet(referenceDate, 51.4769, 0.0, 0.0).Rise);
            moonLegacy.Rise.Should().Be(AstroUtil.GetMoonRiseAndSet(referenceDate, 51.4769, 0.0, 0.0).Rise);
        }

        /// <summary>
        /// Verifies solar altitude at the equator near equinox noon and midnight, exercising the
        /// NOVAS solar position path against the expected day-night symmetry.
        /// </summary>
        [Test]
        public void GetSunAltitude_EquatorAtEquinox_HighAtNoonAndLowAtMidnight() {
            ObserverInfo observer = new ObserverInfo { Latitude = 0.0, Longitude = 0.0, Elevation = 0.0 };

            double noonAltitude = AstroUtil.GetSunAltitude(new DateTime(2024, 3, 20, 12, 7, 0, DateTimeKind.Utc), observer);
            double midnightAltitude = AstroUtil.GetSunAltitude(new DateTime(2024, 3, 20, 0, 7, 0, DateTimeKind.Utc), observer);

            noonAltitude.Should().BeGreaterThan(88.0);
            midnightAltitude.Should().BeLessThan(-88.0);
        }

        /// <summary>
        /// Verifies the apparent solar declination near the equinoxes and solstices, where the Sun
        /// should be near 0 degrees at equinox and near the obliquity of the ecliptic at solstice.
        /// Reference: https://gml.noaa.gov/grad/solcalc/solareqns.PDF
        /// </summary>
        [Test]
        [TestCase(2024, 3, 20, 3, 6, 0.0)]
        [TestCase(2024, 6, 20, 20, 51, 23.44)]
        [TestCase(2024, 9, 22, 12, 44, 0.0)]
        [TestCase(2024, 12, 21, 9, 20, -23.44)]
        public void GetSunPosition_EquinoxesAndSolstices_ReturnsExpectedSolarDeclination(int year, int month, int day, int hour, int minute, double expectedDeclination) {
            DateTime date = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
            ObserverInfo observer = new ObserverInfo { Latitude = 0.0, Longitude = 0.0, Elevation = 0.0 };

            NOVAS.SkyPosition sun = AstroUtil.GetSunPosition(date, observer);

            sun.Dec.Should().BeApproximately(expectedDeclination, 0.35);
        }

        /// <summary>
        /// Verifies lunar altitude at Greenwich around the March 2024 full moon, exercising the
        /// NOVAS lunar position path independently of the rise/set interpolation.
        /// </summary>
        [Test]
        public void GetMoonAltitude_GreenwichFullMoonNight_ReturnsAboveHorizonAltitude() {
            DateTime date = new DateTime(2024, 3, 25, 0, 30, 0, DateTimeKind.Utc);
            ObserverInfo observer = new ObserverInfo { Latitude = 51.4769, Longitude = 0.0, Elevation = 46.0 };

            double altitude = AstroUtil.GetMoonAltitude(date, observer);

            altitude.Should().BeGreaterThan(20.0);
        }

        /// <summary>
        /// Verifies legacy latitude/longitude solar and lunar altitude overloads delegate to the
        /// current ObserverInfo overloads for deterministic historical callers.
        /// </summary>
        [Test]
        public void AltitudeCompatibilityOverloads_LatitudeLongitude_MatchObserverInfoOverloads() {
            DateTime date = new DateTime(2024, 3, 25, 0, 30, 0, DateTimeKind.Utc);
            ObserverInfo observer = new ObserverInfo { Latitude = 51.4769, Longitude = 0.0 };

#pragma warning disable CS0618
            double moonLegacy = AstroUtil.GetMoonAltitude(date, observer.Latitude, observer.Longitude);
            double sunLegacy = AstroUtil.GetSunAltitude(date, observer.Latitude, observer.Longitude);
#pragma warning restore CS0618

            moonLegacy.Should().BeApproximately(AstroUtil.GetMoonAltitude(date, observer), 1e-10);
            sunLegacy.Should().BeApproximately(AstroUtil.GetSunAltitude(date, observer), 1e-10);
        }

        /// <summary>
        /// Verifies NOVAS standard refraction increases apparent altitude most near the horizon,
        /// which protects the zenith-distance sign and unit conversion.
        /// </summary>
        [Test]
        public void CalculateAltitudeForStandardRefraction_LowAltitude_IncreasesApparentAltitude() {
            double lowAltitude = AstroUtil.CalculateAltitudeForStandardRefraction(5.0, 51.4769, 0.0, 46.0);
            double highAltitude = AstroUtil.CalculateAltitudeForStandardRefraction(80.0, 51.4769, 0.0, 46.0);

            lowAltitude.Should().BeGreaterThan(5.0);
            highAltitude.Should().BeGreaterThan(80.0);
            (lowAltitude - 5.0).Should().BeGreaterThan(highAltitude - 80.0);
        }

        /// <summary>
        /// Verifies obsolete solar and lunar position overloads still return the same NOVAS body
        /// positions as the current ObserverInfo overloads.
        /// </summary>
        [Test]
        public void BodyPositionCompatibilityOverloads_IgnoreJulianDateArgument_DelegateToCurrentOverloads() {
            DateTime date = new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);
            ObserverInfo observer = new ObserverInfo { Latitude = 51.4769, Longitude = 0.0, Elevation = 46.0 };
            double julianDate = AstroUtil.GetJulianDate(date);

#pragma warning disable CS0618
            NOVAS.SkyPosition sunLegacy = AstroUtil.GetSunPosition(date, julianDate, observer);
            NOVAS.SkyPosition moonLegacy = AstroUtil.GetMoonPosition(date, julianDate, observer);
            Tuple<NOVAS.SkyPosition, NOVAS.SkyPosition> tupleLegacy = AstroUtil.GetMoonAndSunPosition(date, julianDate, observer);
#pragma warning restore CS0618

            NOVAS.SkyPosition sun = AstroUtil.GetSunPosition(date, observer);
            NOVAS.SkyPosition moon = AstroUtil.GetMoonPosition(date, observer);

            sunLegacy.RA.Should().BeApproximately(sun.RA, AngleTolerance);
            moonLegacy.RA.Should().BeApproximately(moon.RA, AngleTolerance);
            tupleLegacy.Item1.RA.Should().BeApproximately(moon.RA, AngleTolerance);
            tupleLegacy.Item2.RA.Should().BeApproximately(sun.RA, AngleTolerance);
        }

        /// <summary>
        /// Verifies legacy Moon phase, illumination, and position-angle overloads continue to
        /// produce finite values for default geocentric-style observer assumptions.
        /// </summary>
        [Test]
        public void MoonCompatibilityOverloads_DefaultObserver_ReturnFinitePhaseAndIllumination() {
            DateTime date = new DateTime(2024, 4, 8, 18, 21, 0, DateTimeKind.Utc);

#pragma warning disable CS0618
            double illumination = AstroUtil.GetMoonIllumination(date);
            double positionAngle = AstroUtil.GetMoonPositionAngle(date);
            AstroUtil.MoonPhase phase = AstroUtil.GetMoonPhase(date);
            double calculatedIllumination = AstroUtil.CalculateMoonIllumination(date);
#pragma warning restore CS0618

            illumination.Should().BeInRange(0.0, 1.0);
            calculatedIllumination.Should().BeApproximately(illumination, AngleTolerance);
            positionAngle.Should().BeInRange(-180.0, 180.0);
            phase.Should().NotBe(AstroUtil.MoonPhase.Unknown);
        }

        /// <summary>
        /// Verifies the Gueymard 1993 airmass model at representative altitudes and rejects
        /// physically invalid altitude inputs with NaN.
        /// Reference: https://doi.org/10.1016/0038-092X(93)90074-X
        /// </summary>
        [Test]
        [TestCase(90.0, 1.0)]
        [TestCase(60.0, 1.15425329789205)]
        [TestCase(45.0, 1.41282515211066)]
        [TestCase(30.0, 1.99426095351295)]
        [TestCase(10.0, 5.58083120021974)]
        [TestCase(0.0, 37.8082182299908)]
        public void Airmass_ValidAltitudes_ReturnsGueymardReferenceValues(double altitude, double expectedAirmass) {
            double airmass = AstroUtil.Airmass(altitude);

            airmass.Should().BeApproximately(expectedAirmass, 1e-12);
        }

        /// <summary>
        /// Verifies invalid airmass inputs, because negative altitude and non-finite values are
        /// outside the physical domain of the Gueymard approximation.
        /// </summary>
        [Test]
        [TestCase(-0.1)]
        [TestCase(90.1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Airmass_InvalidAltitude_ReturnsNaN(double altitude) {
            double airmass = AstroUtil.Airmass(altitude);

            airmass.Should().Be(double.NaN);
        }

        /// <summary>
        /// Verifies ISO 2533 standard-atmosphere pressure conversion from sea-level pressure to
        /// local observing-site pressure at common elevations.
        /// Reference: https://www.engineeringtoolbox.com/standard-atmosphere-d_604.html
        /// </summary>
        [Test]
        [TestCase(1013.25, 0.0, 1013.25)]
        [TestCase(1013.25, 1000.0, 898.745604273543)]
        [TestCase(1013.25, 1609.344, 834.275729916201)]
        [TestCase(1013.25, 2000.0, 794.951974352912)]
        public void MslToLocalPressure_StandardAtmosphere_ReturnsExpectedPressure(double seaLevelPressure, double elevation, double expectedPressure) {
            double pressure = AstroUtil.MslToLocalPressure(seaLevelPressure, elevation);

            pressure.Should().BeApproximately(expectedPressure, 1e-9);
        }

        /// <summary>
        /// Verifies SOFA refraction behavior by checking that denser air refracts a target more
        /// than thin high-altitude air and that the apparent altitude increases.
        /// </summary>
        [Test]
        public void CalculateRefractedAltitude_DifferentPressure_IncreasesAltitudeMoreAtSeaLevel() {
            double vacuumAltitude = 20.0;

            double seaLevel = AstroUtil.CalculateRefractedAltitude(vacuumAltitude, 1013.25, 10.0, 50.0, 0.574);
            double highAltitude = AstroUtil.CalculateRefractedAltitude(vacuumAltitude, 600.0, 10.0, 50.0, 0.574);

            seaLevel.Should().BeGreaterThan(vacuumAltitude);
            highAltitude.Should().BeGreaterThan(vacuumAltitude);
            seaLevel.Should().BeGreaterThan(highAltitude);
        }

        /// <summary>
        /// Verifies that refraction rejects a negative geometric altitude, because the implemented
        /// SOFA-based iteration is documented only for targets at or above the horizon.
        /// </summary>
        [Test]
        public void CalculateRefractedAltitude_NegativeAltitude_ThrowsArgumentException() {
            Action act = () => AstroUtil.CalculateRefractedAltitude(-0.01, 1013.25, 10.0, 50.0, 0.574);

            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Verifies that astronomical unit conversion uses the IAU exact astronomical unit in
        /// kilometers, which is then reused by Sun and Moon distance calculations.
        /// Reference: https://iau-a3.gitlab.io/res.html
        /// </summary>
        [Test]
        public void AUToKilometer_OneAstronomicalUnit_ReturnsIauKilometers() {
            AstroUtil.AUToKilometer(1.0).Should().Be(149597870.7);
            Earth.Radius.Should().Be(6371.0);
        }

        /// <summary>
        /// Verifies FITS-compatible sexagesimal formatting for RA and Dec, including signed
        /// declination output used in image headers.
        /// </summary>
        [Test]
        public void FitsSexagesimalFormatting_PositiveAndNegativeCoordinates_ReturnsFitsHeaderStrings() {
            AstroUtil.DegreesToFitsDMS(12.5).Should().Be("+12 30 00");
            AstroUtil.DegreesToFitsDMS(-12.5).Should().Be("-12 30 00");
            AstroUtil.HoursToFitsHMS(5.25).Should().Be("05 15 00");
        }

        /// <summary>
        /// Verifies comma decimal DMS parsing for European-formatted source data, because imported
        /// catalog and planetarium text can use comma decimal separators.
        /// </summary>
        [Test]
        public void DMSToDegrees_CommaDecimalSeconds_ParsesUsingCommaPattern() {
            double value = AstroUtil.DMSToDegrees("12°30'30,5\"");

            value.Should().BeApproximately(12.508472222222222, 1e-12);
        }

        /// <summary>
        /// Verifies image-scale field-of-view helpers with rectangular sensors, ensuring the
        /// maximum field uses the longer side and per-axis field uses the requested axis only.
        /// </summary>
        [Test]
        public void FieldOfView_RectangularSensor_ReturnsArcminuteExtent() {
            const double arcsecPerPixel = 1.5;

            AstroUtil.FieldOfView(arcsecPerPixel, 3000).Should().BeApproximately(75.0, AngleTolerance);
            AstroUtil.MaxFieldOfView(arcsecPerPixel, 3000, 2000).Should().BeApproximately(75.0, AngleTolerance);
        }

        /// <summary>
        /// Verifies polar-to-Cartesian conversion at axis-aligned spherical coordinates so 3D sky
        /// preview geometry preserves the expected handedness.
        /// </summary>
        [Test]
        public void Polar3DToCartesian_AxisAlignedAngles_ReturnsExpectedVectorComponents() {
            var xAxis = AstroUtil.Polar3DToCartesian(2.0, 0.0, 0.0);
            var zAxis = AstroUtil.Polar3DToCartesian(2.0, Math.PI / 2.0, 0.0);
            var negativeYAxis = AstroUtil.Polar3DToCartesian(2.0, Math.PI / 2.0, Math.PI / 2.0);

            xAxis.X.Should().BeApproximately(2.0, AngleTolerance);
            xAxis.Y.Should().BeApproximately(0.0, AngleTolerance);
            xAxis.Z.Should().BeApproximately(0.0, AngleTolerance);
            zAxis.Z.Should().BeApproximately(2.0, AngleTolerance);
            negativeYAxis.Y.Should().BeApproximately(-2.0, AngleTolerance);
        }

        private static double AngularDifference(double actualDegrees, double expectedDegrees) {
            double difference = Math.Abs(AstroUtil.EuclidianModulus(actualDegrees - expectedDegrees + 180.0, 360.0) - 180.0);
            return difference;
        }

        private static void AssertCloseToTime(DateTime? actual, DateTime expected, TimeSpan tolerance) {
            actual.Should().NotBeNull();
            TimeSpan difference = (actual.Value - expected).Duration();
            difference.Should().BeLessThanOrEqualTo(tolerance);
        }

        private static TempEarthRotationDatabase CreateEarthRotationDatabase(params (DateTime Date, double Ut1MinusUtc)[] rows) {
            string databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"earth-rotation-{Guid.NewGuid():N}.sqlite");
            string connectionString = $"Data Source={databasePath};";
            DatabaseInteraction databaseInteraction = new DatabaseInteraction(connectionString);

            using (var context = databaseInteraction.GetContext()) {
                foreach ((DateTime date, double ut1MinusUtc) in rows) {
                    long unixTimestamp = CoreUtil.DateTimeToUnixTimeStamp(date);
                    double modifiedJulianDate = AstroUtil.GetJulianDate(date) - 2400000.5;
                    context.Database.ExecuteSqlCommand(
                        "INSERT OR REPLACE INTO `earthrotationparameters` (date, modifiedjuliandate, x, y, ut1_utc, lod, dx, dy) VALUES (@p0, @p1, 0, 0, @p2, 1, 0, 0)",
                        unixTimestamp,
                        modifiedJulianDate,
                        ut1MinusUtc);
                }
            }

            return new TempEarthRotationDatabase(databasePath, databaseInteraction);
        }

        private static void ClearDeltaUTCaches() {
            typeof(AstroUtil).GetField("DeltaUTToday", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            typeof(AstroUtil).GetField("DeltaUTYesterday", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            typeof(AstroUtil).GetField("DeltaUTTomorrow", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            typeof(AstroUtil).GetField("DeltaUTReference", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, default(DateTime));

            FieldInfo cacheField = typeof(AstroUtil).GetField("DeltaUTCache", BindingFlags.NonPublic | BindingFlags.Static);
            cacheField?.SetValue(null, new ConcurrentDictionary<DateTime, double>());
        }

        private sealed class TempEarthRotationDatabase : IDisposable {
            public TempEarthRotationDatabase(string path, DatabaseInteraction interaction) {
                Path = path;
                Interaction = interaction;
            }

            public string Path { get; }
            public DatabaseInteraction Interaction { get; }

            public void Dispose() {
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (File.Exists(Path)) {
                    File.Delete(Path);
                }
            }
        }
    }
}
