#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class AngleTest {
        private static double TOLERANCE = 0.0000000000001;

        [Test]
        [TestCase(0)]
        [TestCase(3.23452)]
        [TestCase(12)]
        [TestCase(24)]
        public void CreateByHoursTest(double inputHours) {
            var angle = Angle.ByHours(inputHours);

            var expectedDegree = Astrometry.HoursToDegrees(inputHours);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedRadian = Astrometry.ToRadians(expectedDegree);

            Assert.AreEqual(inputHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(45.234123)]
        [TestCase(90)]
        [TestCase(180)]
        [TestCase(270)]
        [TestCase(360)]
        public void CreateByDegreeTest(double inputDegrees) {
            var angle = Angle.ByDegree(inputDegrees);

            var expectedHours = Astrometry.DegreesToHours(inputDegrees);
            var expectedArcmin = Astrometry.DegreeToArcmin(inputDegrees);
            var expectedArcsec = Astrometry.DegreeToArcsec(inputDegrees);
            var expectedRadian = Astrometry.ToRadians(inputDegrees);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
            Assert.AreEqual(inputDegrees, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void CreateByRadiansTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians);

            var expectedDegree = Astrometry.ToDegree(inputRadians);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(inputRadians, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void SinTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Sin();

            var rad = Math.Sin(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(rad, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        [TestCase(-Math.PI)]
        [TestCase(-2 * Math.PI)]
        [TestCase(-1)]
        public void AbsTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Abs();

            var rad = Math.Abs(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(rad, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void AsinTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Asin();

            var rad = Math.Asin(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(rad, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void CosTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Cos();

            var rad = Math.Cos(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(rad, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void AcosTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Acos();

            var rad = Math.Acos(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(rad, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void AtanTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Atan();

            var rad = Math.Atan(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(rad, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(Math.PI, Math.PI)]
        [TestCase(2 * Math.PI, Math.PI)]
        [TestCase(1, Math.PI)]
        public void Atan2Test(double xRadians, double yRadians) {
            var xAngle = Angle.ByRadians(xRadians);
            var yAngle = Angle.ByRadians(yRadians);
            var angle = xAngle.Atan2(yAngle);

            var rad = Math.Atan2(yRadians, xRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(rad, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(Math.PI, Math.PI)]
        [TestCase(2 * Math.PI, Math.PI)]
        [TestCase(1, Math.PI)]
        public void StaticAtan2Test(double xRadians, double yRadians) {
            var xAngle = Angle.ByRadians(xRadians);
            var yAngle = Angle.ByRadians(yRadians);
            var angle = Angle.Atan2(yAngle, xAngle);

            var rad = Math.Atan2(yRadians, xRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(rad, angle.Radians, TOLERANCE);
            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorPlusTest(double firstDegree, double secondDegree) {
            var firstAngle = Angle.ByDegree(firstDegree);
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = firstAngle + secondAngle;

            var expectedRadian = Astrometry.ToRadians(firstDegree) + Astrometry.ToRadians(secondDegree);
            var expectedDegree = Astrometry.ToDegree(expectedRadian);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorPlusDoubleTest(double firstDegree, double secondDegree) {
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = Astrometry.ToRadians(firstDegree) + secondAngle;

            var expectedRadian = Astrometry.ToRadians(firstDegree) + Astrometry.ToRadians(secondDegree);
            var expectedDegree = Astrometry.ToDegree(expectedRadian);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorMinusTest(double firstDegree, double secondDegree) {
            var firstAngle = Angle.ByDegree(firstDegree);
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = firstAngle - secondAngle;

            var expectedRadian = Astrometry.ToRadians(firstDegree) - Astrometry.ToRadians(secondDegree);
            var expectedDegree = Astrometry.ToDegree(expectedRadian);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorMinusDoubleTest(double firstDegree, double secondDegree) {
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = Astrometry.ToRadians(firstDegree) - secondAngle;

            var expectedRadian = Astrometry.ToRadians(firstDegree) - Astrometry.ToRadians(secondDegree);
            var expectedDegree = Astrometry.ToDegree(expectedRadian);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorMultiplyTest(double firstDegree, double secondDegree) {
            var firstAngle = Angle.ByDegree(firstDegree);
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = firstAngle * secondAngle;

            var expectedRadian = Astrometry.ToRadians(firstDegree) * Astrometry.ToRadians(secondDegree);
            var expectedDegree = Astrometry.ToDegree(expectedRadian);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorMultiplyDoubleTest(double firstDegree, double secondDegree) {
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = Astrometry.ToRadians(firstDegree) * secondAngle;

            var expectedRadian = Astrometry.ToRadians(firstDegree) * Astrometry.ToRadians(secondDegree);
            var expectedDegree = Astrometry.ToDegree(expectedRadian);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorDivideTest(double firstDegree, double secondDegree) {
            var firstAngle = Angle.ByDegree(firstDegree);
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = firstAngle / secondAngle;

            var expectedRadian = Astrometry.ToRadians(firstDegree) / Astrometry.ToRadians(secondDegree);
            var expectedDegree = Astrometry.ToDegree(expectedRadian);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            Assert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            Assert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, "00° 00' 00\"")]
        [TestCase(360, "360° 00' 00\"")]
        [TestCase(42.23423, "42° 14' 03\"")]
        public void OperatorDivideTest(double inputDegree, string expectedDMS) {
            var angle = Angle.ByDegree(inputDegree);

            var dms = angle.ToString();

            Assert.AreEqual(expectedDMS, dms);
        }
    }
}