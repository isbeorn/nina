#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Astrometry;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test {

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

            var expectedDegree = AstroUtil.HoursToDegrees(inputHours);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedRadian = AstroUtil.ToRadians(expectedDegree);

            ClassicAssert.AreEqual(inputHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
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

            var expectedHours = AstroUtil.DegreesToHours(inputDegrees);
            var expectedArcmin = AstroUtil.DegreeToArcmin(inputDegrees);
            var expectedArcsec = AstroUtil.DegreeToArcsec(inputDegrees);
            var expectedRadian = AstroUtil.ToRadians(inputDegrees);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(inputDegrees, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void CreateByRadiansTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians);

            var expectedDegree = AstroUtil.ToDegree(inputRadians);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(inputRadians, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void SinTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Sin();

            var rad = Math.Sin(inputRadians);
            var expectedDegree = AstroUtil.ToDegree(rad);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(rad, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
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
            var expectedDegree = AstroUtil.ToDegree(rad);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(rad, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void AsinTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Asin();

            var rad = Math.Asin(inputRadians);
            var expectedDegree = AstroUtil.ToDegree(rad);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(rad, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void CosTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Cos();

            var rad = Math.Cos(inputRadians);
            var expectedDegree = AstroUtil.ToDegree(rad);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(rad, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void AcosTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Acos();

            var rad = Math.Acos(inputRadians);
            var expectedDegree = AstroUtil.ToDegree(rad);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(rad, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void AtanTest(double inputRadians) {
            var angle = Angle.ByRadians(inputRadians).Atan();

            var rad = Math.Atan(inputRadians);
            var expectedDegree = AstroUtil.ToDegree(rad);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(rad, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
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
            var expectedDegree = AstroUtil.ToDegree(rad);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(rad, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
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
            var expectedDegree = AstroUtil.ToDegree(rad);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(rad, angle.Radians, TOLERANCE);
            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
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

            var expectedRadian = AstroUtil.ToRadians(firstDegree) + AstroUtil.ToRadians(secondDegree);
            var expectedDegree = AstroUtil.ToDegree(expectedRadian);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorPlusDoubleTest(double firstDegree, double secondDegree) {
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = AstroUtil.ToRadians(firstDegree) + secondAngle;

            var expectedRadian = AstroUtil.ToRadians(firstDegree) + AstroUtil.ToRadians(secondDegree);
            var expectedDegree = AstroUtil.ToDegree(expectedRadian);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
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

            var expectedRadian = AstroUtil.ToRadians(firstDegree) - AstroUtil.ToRadians(secondDegree);
            var expectedDegree = AstroUtil.ToDegree(expectedRadian);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorMinusDoubleTest(double firstDegree, double secondDegree) {
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = AstroUtil.ToRadians(firstDegree) - secondAngle;

            var expectedRadian = AstroUtil.ToRadians(firstDegree) - AstroUtil.ToRadians(secondDegree);
            var expectedDegree = AstroUtil.ToDegree(expectedRadian);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
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

            var expectedRadian = AstroUtil.ToRadians(firstDegree) * AstroUtil.ToRadians(secondDegree);
            var expectedDegree = AstroUtil.ToDegree(expectedRadian);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(360, 360)]
        [TestCase(360, 0)]
        [TestCase(0, 360)]
        [TestCase(Math.PI, Math.PI)]
        public void OperatorMultiplyDoubleTest(double firstDegree, double secondDegree) {
            var secondAngle = Angle.ByDegree(secondDegree);

            var angle = AstroUtil.ToRadians(firstDegree) * secondAngle;

            var expectedRadian = AstroUtil.ToRadians(firstDegree) * AstroUtil.ToRadians(secondDegree);
            var expectedDegree = AstroUtil.ToDegree(expectedRadian);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
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

            var expectedRadian = AstroUtil.ToRadians(firstDegree) / AstroUtil.ToRadians(secondDegree);
            var expectedDegree = AstroUtil.ToDegree(expectedRadian);
            var expectedArcmin = AstroUtil.DegreeToArcmin(expectedDegree);
            var expectedArcsec = AstroUtil.DegreeToArcsec(expectedDegree);
            var expectedHours = AstroUtil.DegreesToHours(expectedDegree);

            ClassicAssert.AreEqual(expectedDegree, angle.Degree, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcmin, angle.ArcMinutes, TOLERANCE);
            ClassicAssert.AreEqual(expectedArcsec, angle.ArcSeconds, TOLERANCE);
            ClassicAssert.AreEqual(expectedHours, angle.Hours, TOLERANCE);
            ClassicAssert.AreEqual(expectedRadian, angle.Radians, TOLERANCE);
        }

        [Test]
        [TestCase(0, "00° 00' 00\"")]
        [TestCase(360, "360° 00' 00\"")]
        [TestCase(42.23423, "42° 14' 03\"")]
        public void OperatorDivideTest(double inputDegree, string expectedDMS) {
            var angle = Angle.ByDegree(inputDegree);

            var dms = angle.ToString();

            ClassicAssert.AreEqual(expectedDMS, dms);
        }

        [Test]
        [TestCase(1.0, 1.0, 0)]
        [TestCase(1.0, 2.0, 1.0)]
        [TestCase(1.0, 2.0, 4.0)]
        [TestCase(359.9, 0.1, 0.2)]
        public void EqualWithToleranceTest(double lhs, double rhs, double tolerance) {
            var lhsAngle = Angle.ByDegree(lhs);
            var rhsAngle = Angle.ByDegree(rhs);
            var toleranceAngle = Angle.ByDegree(tolerance);
            ClassicAssert.IsTrue(lhsAngle.Equals(rhsAngle, toleranceAngle));
        }

        [Test]
        [TestCase(2.0, 0.0, 1.0)]
        [TestCase(1.0, 1.2, 0.1)]
        [TestCase(1.0, 2.0, 0.9)]
        [TestCase(1.0, 2.0, 0.1)]
        [TestCase(359.9, 0.1, 0.1)]
        public void NotEqualWithToleranceTest(double lhs, double rhs, double tolerance) {
            var lhsAngle = Angle.ByDegree(lhs);
            var rhsAngle = Angle.ByDegree(rhs);
            var toleranceAngle = Angle.ByDegree(tolerance);
            ClassicAssert.IsFalse(lhsAngle.Equals(rhsAngle, toleranceAngle));
        }


        [Test]
        [TestCase(1.0, 1.0, 0)]
        [TestCase(1.0, 2.0, 1.0)]
        [TestCase(1.0, 2.0, 4.0)]
        [TestCase(359.9, 0.1, 0.2)]
        [TestCase(1.0, 181.0, 0)]
        [TestCase(1.0, 182.0, 1.0)]
        [TestCase(1.0, 182.0, 4.0)]
        [TestCase(359.9, 180.1, 0.2)]
        [TestCase(90, 270, 0.1)]
        [TestCase(45, 225, 0.1)]
        [TestCase(135, 315, 0.1)]
        public void EqualWithTolerance180Test(double lhs, double rhs, double tolerance) {
            var lhsAngle = Angle.ByDegree(lhs);
            var rhsAngle = Angle.ByDegree(rhs);
            var toleranceAngle = Angle.ByDegree(tolerance);
            ClassicAssert.IsTrue(lhsAngle.Equals(rhsAngle, toleranceAngle, true));
        }

        [Test]
        [TestCase(2.0, 0.0, 1.0)]
        [TestCase(1.0, 1.2, 0.1)]
        [TestCase(1.0, 2.0, 0.9)]
        [TestCase(1.0, 2.0, 0.1)]
        [TestCase(359.9, 0.1, 0.1)]
        [TestCase(2.0, 180.0, 1.0)]
        [TestCase(1.0, 181.2, 0.1)]
        [TestCase(1.0, 182.0, 0.9)]
        [TestCase(1.0, 182.0, 0.1)]
        [TestCase(359.9, 180.1, 0.1)]
        [TestCase(45, 315, 0.1)]
        [TestCase(135, 225, 0.1)]
        public void NotEqualWithTolerance180Test(double lhs, double rhs, double tolerance) {
            var lhsAngle = Angle.ByDegree(lhs);
            var rhsAngle = Angle.ByDegree(rhs);
            var toleranceAngle = Angle.ByDegree(tolerance);
            ClassicAssert.IsFalse(lhsAngle.Equals(rhsAngle, toleranceAngle, true));
        }
    }
}