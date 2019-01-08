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
        [TestCase(12)]
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
        [TestCase(12)]
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
        [TestCase(12)]
        public void CreateByRadiansTest(double inputRadians) {
            var angle = Angle.CreateByRadians(inputRadians);

            var expectedDegree = Astrometry.ToRadians(inputRadians);
            var expectedArcmin = Astrometry.DegreeToArcmin(expectedDegree);
            var expectedArcsec = Astrometry.DegreeToArcsec(expectedDegree);
            var expectedHours = Astrometry.DegreesToHours(expectedDegree);

            Assert.AreEqual(expectedHours, angle.Hours);
            Assert.AreEqual(inputRadians, angle.Radians);
            Assert.AreEqual(expectedDegree, angle.Degree);
            Assert.AreEqual(expectedArcmin, angle.ArcMinutes);
            Assert.AreEqual(expectedArcsec, angle.ArcSeconds);
        }
    }
}