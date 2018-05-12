using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;

namespace NINATest {

    [TestFixture]
    public class AstrometryTest {
        private const int DOUBLE_TOLERANCE = 12;

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
        public void GetAltitude_0Angle_Northern_ValueTest() {
            var angle = 0;
            var latitude = 0;
            var longitude = 0;

            var alt = Astrometry.GetAltitude(angle, latitude, longitude);

            Assert.AreEqual(90, alt);
        }

        [Test]
        public void GetAltitude_360Angle_Northern_ValueTest() {
            var angle = 360;
            var latitude = 0;
            var longitude = 0;

            var alt = Astrometry.GetAltitude(angle, latitude, longitude);

            Assert.AreEqual(90, alt);
        }

        [Test]
        public void GetAltitude_180Angle_Northern_ValueTest() {
            var angle = 180;
            var latitude = 0;
            var longitude = 0;

            var alt = Astrometry.GetAltitude(angle, latitude, longitude);

            Assert.AreEqual(-90, alt);
        }

        [Test]
        public void GetAltitude_90Angle_Northern_ValueTest() {
            var angle = 90;
            var latitude = 0;
            var longitude = 0;

            var alt = Astrometry.GetAltitude(angle, latitude, longitude);
            alt = Math.Round(alt, DOUBLE_TOLERANCE);

            Assert.AreEqual(0, alt);
        }

        [Test]
        public void GetAltitude_270Angle_Northern_ValueTest() {
            var angle = 270;
            var latitude = 0;
            var longitude = 0;

            var alt = Astrometry.GetAltitude(angle, latitude, longitude);
            alt = Math.Round(alt, DOUBLE_TOLERANCE);

            Assert.AreEqual(0, alt);
        }
    }
}