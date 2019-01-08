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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class AngleTest {

        [Test]
        [TestCase(0)]
        [TestCase(3.23452)]
        [TestCase(12)]
        [TestCase(24)]
        public void CreateByHoursTest(double inputHours) {
            var angle = Angle.CreateByHours(inputHours);

            var expectedDegree = Astrometry.HoursToDegrees(inputHours);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedRadian = Astrometry.ToRadians(expectedDegree);

            Assert.AreEqual(inputHours, angle.Hours);
            Assert.AreEqual(expectedRadian, angle.Radians);
            Assert.AreEqual(expectedDegree, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }

        [Test]
        [TestCase(0)]
        [TestCase(45.234123)]
        [TestCase(90)]
        [TestCase(180)]
        [TestCase(270)]
        [TestCase(360)]
        public void CreateByDegreeTest(double inputDegrees) {
            var angle = Angle.CreateByDegree(inputDegrees);

            var expectedHours = Astrometry.DegreesToHours(inputDegrees);
            var expectedArcmin = Astrometry.DegreeToArcmin(inputDegrees);
            var expectedArcsec = Astrometry.DegreeToArcsec(inputDegrees);
            var expectedRadian = Astrometry.ToRadians(inputDegrees);

            Assert.AreEqual(expectedHours, angle.Hours);
            Assert.AreEqual(expectedRadian, angle.Radians);
            Assert.AreEqual(inputDegrees, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void CreateByRadiansTest(double inputRadians) {
            var angle = Angle.CreateByRadians(inputRadians);

            var expectedDegree = Astrometry.ToDegree(inputRadians);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours);
            Assert.AreEqual(inputRadians, angle.Radians);
            Assert.AreEqual(expectedDegree, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void SinTest(double inputRadians) {
            var angle = Angle.CreateByRadians(inputRadians).Sin();

            var rad = Math.Sin(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours);
            Assert.AreEqual(rad, angle.Radians);
            Assert.AreEqual(expectedDegree, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void CosTest(double inputRadians) {
            var angle = Angle.CreateByRadians(inputRadians).Cos();

            var rad = Math.Cos(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours);
            Assert.AreEqual(rad, angle.Radians);
            Assert.AreEqual(expectedDegree, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void AcosTest(double inputRadians) {
            var angle = Angle.CreateByRadians(inputRadians).Acos();

            var rad = Math.Acos(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours);
            Assert.AreEqual(rad, angle.Radians);
            Assert.AreEqual(expectedDegree, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }

        [Test]
        [TestCase(0)]
        [TestCase(Math.PI)]
        [TestCase(2 * Math.PI)]
        [TestCase(1)]
        public void AtanTest(double inputRadians) {
            var angle = Angle.CreateByRadians(inputRadians).Atan();

            var rad = Math.Atan(inputRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours);
            Assert.AreEqual(rad, angle.Radians);
            Assert.AreEqual(expectedDegree, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(Math.PI, Math.PI)]
        [TestCase(2 * Math.PI, Math.PI)]
        [TestCase(1, Math.PI)]
        public void Atan2Test(double xRadians, double yRadians) {
            var xAngle = Angle.CreateByRadians(xRadians);
            var yAngle = Angle.CreateByRadians(yRadians);
            var angle = xAngle.Atan2(yAngle);

            var rad = Math.Atan2(yRadians, xRadians);
            var expectedDegree = Astrometry.ToDegree(rad);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours);
            Assert.AreEqual(rad, angle.Radians);
            Assert.AreEqual(expectedDegree, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }
    }
}