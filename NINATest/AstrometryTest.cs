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
using NUnit.Framework;
using System;

namespace NINATest {

    [TestFixture]
    public class AstrometryTest {
        private const double DEWPOINT_TOLERANCE = 0.5;
        private static double ANGLE_TOLERANCE = 0.0000000000001;
        private static double MODULUS_TOLERANCE = 0.0001;

        [Test]
        public void ToRadians_ValueTest() {
            var degree = 180;
            var expectedRad = Math.PI;

            var rad = Astrometry.ToRadians(degree);

            Assert.AreEqual(expectedRad, rad);
        }

        [Test]
        public void ToDegree_ValueTest() {
            var rad = Math.PI;
            var expectedDeg = 180;

            var deg = Astrometry.ToDegree(rad);

            Assert.AreEqual(expectedDeg, deg);
        }

        [Test]
        public void DegreeToArcmin_ValueTest() {
            var degree = 180;
            var expectedarcmin = 10800;

            var arcmin = Astrometry.DegreeToArcmin(degree);

            Assert.AreEqual(expectedarcmin, arcmin);
        }

        [Test]
        public void DegreeToArcsec_ValueTest() {
            var degree = 180;
            var expectedarcsec = 648000;

            var arcsec = Astrometry.DegreeToArcsec(degree);

            Assert.AreEqual(expectedarcsec, arcsec);
        }

        [Test]
        public void ArcminToArcsec_ValueTest() {
            var arcmin = 20.4;
            var expectedarcsec = 1224;

            var arcsec = Astrometry.ArcminToArcsec(arcmin);

            Assert.AreEqual(expectedarcsec, arcsec);
        }

        [Test]
        public void ArcminToDegree_ValueTest() {
            var arcmin = 150;
            var expecteddeg = 2.5;

            var deg = Astrometry.ArcminToDegree(arcmin);

            Assert.AreEqual(expecteddeg, deg);
        }

        [Test]
        public void ArcsecToArcmin_ValueTest() {
            var arcsec = 150;
            var expectedarcmin = 2.5;

            var arcmin = Astrometry.ArcsecToArcmin(arcsec);

            Assert.AreEqual(expectedarcmin, arcmin);
        }

        [Test]
        public void ArcsecToDegree_ValueTest() {
            var arcsec = 9000;
            var expecteddeg = 2.5;

            var deg = Astrometry.ArcsecToDegree(arcsec);

            Assert.AreEqual(expecteddeg, deg);
        }

        [Test]
        public void HoursToDegree_ValueTest() {
            var hours = 5.2;
            var expecteddeg = 78;

            var deg = Astrometry.HoursToDegrees(hours);

            Assert.AreEqual(expecteddeg, deg);
        }

        [Test]
        public void DegreesToHours_ValueTest() {
            var deg = 78;
            var expectedhours = 5.2;

            var hours = Astrometry.DegreesToHours(deg);

            Assert.AreEqual(expectedhours, hours);
        }

        [Test]
        [TestCase(0, 0, 0, 90)]
        [TestCase(360, 0, 0, 90)]
        [TestCase(180, 0, 0, -90)]
        [TestCase(90, 0, 0, 0)]
        [TestCase(270, 0, 0, 0)]
        public void GetAltitudeTest(double angle, double latitude, double longitude, double expectedAltitude) {
            var alt = Astrometry.GetAltitude(angle, latitude, longitude);

            Assert.AreEqual(expectedAltitude, alt, ANGLE_TOLERANCE);
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
            var az = Astrometry.GetAzimuth(angle, altitude, latitude, declination);

            Assert.AreEqual(expectedAzimuth, az, ANGLE_TOLERANCE);
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
            var value = Astrometry.DegreesToDMS(degree);

            Assert.AreEqual(expected, value);
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
            var value = Astrometry.DegreesToHMS(degree);

            Assert.AreEqual(expected, value);
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
            var value = Astrometry.HoursToHMS(hours);

            Assert.AreEqual(expected, value);
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
            var value = Astrometry.DMSToDegrees(hms);

            Assert.AreEqual(expected, value, ANGLE_TOLERANCE);
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
            var dp = Astrometry.ApproximateDewPoint(temp, humidity);

            Assert.AreEqual(expected, dp, DEWPOINT_TOLERANCE);
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
            var value = Astrometry.HMSToDegrees(hms);

            Assert.AreEqual(expected, value, ANGLE_TOLERANCE);
        }

        [Test]
        [TestCase(0, 0, 0)]
        [TestCase(12, 4, 8.0)]
        [TestCase(24, 24, 0)]
        [TestCase(1.657982, 21.657498, 4.0004840000000002)]
        [TestCase(22.68498, 15.135684, 7.549296)]
        public void GetHourAngleTest(double siderealTime, double rightAscension, double expectedHourAngle) {
            var hourAngle = Astrometry.GetHourAngle(siderealTime, rightAscension);

            Assert.AreEqual(expectedHourAngle, hourAngle, ANGLE_TOLERANCE);
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
            var modulus = Astrometry.EuclidianModulus(x, y);

            Assert.AreEqual(expected, modulus, MODULUS_TOLERANCE);
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
            var modulus = Astrometry.EuclidianModulus(x, y);

            Assert.AreEqual(expected, modulus, MODULUS_TOLERANCE);
        }
    }
}