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
using System.Windows;

namespace NINATest {

    [TestFixture]
    public class CoordinatesTest {
        private static double ANGLE_TOLERANCE = 0.000000000001;

        [Test]
        [TestCase(10, 10)]
        [TestCase(22.5987, -80.125)]
        [TestCase(23.9, 89)]
        [TestCase(0.01, -89)]
        [TestCase(5.567, -2.234)]
        public void Create_RADegreesTest(double ra, double dec) {
            var epoch = Epoch.J2000;
            var coordinates = new Coordinates(ra, dec, epoch, Coordinates.RAType.Degrees);

            Assert.AreEqual(Astrometry.DegreesToHours(ra), coordinates.RA, 0.0001);
            Assert.AreEqual(ra, coordinates.RADegrees, 0.0001);
            Assert.AreEqual(dec, coordinates.Dec, 0.0001);
            Assert.AreEqual(epoch, coordinates.Epoch);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(22.5987, -80.125)]
        [TestCase(23.9, 89)]
        [TestCase(0.01, -89)]
        [TestCase(5.567, -2.234)]
        public void Create_RAHoursTest(double ra, double dec) {
            var epoch = Epoch.JNOW;
            var coordinates = new Coordinates(ra, dec, epoch, Coordinates.RAType.Hours);

            Assert.AreEqual(ra, coordinates.RA, 0.0001);
            Assert.AreEqual(Astrometry.HoursToDegrees(ra), coordinates.RADegrees, 0.0001);
            Assert.AreEqual(dec, coordinates.Dec, 0.0001);
            Assert.AreEqual(epoch, coordinates.Epoch);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(22.5987, -80.125)]
        [TestCase(23.9, 89)]
        [TestCase(0.01, -89)]
        [TestCase(5.567, -2.234)]
        public void Transform_CelestialToApparentTest(double ra, double dec) {
            //Arrange
            var coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);

            //Act
            coordinates = coordinates.Transform(Epoch.JNOW);

            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetJ2000(ra, dec);

            //Check with ascom transformation that the transformation logic matches
            Assert.AreEqual(transform.RAApparent, coordinates.RA, 0.0001);
            Assert.AreEqual(transform.DECApparent, coordinates.Dec, 0.0001);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(22.5987, -80.125)]
        [TestCase(23.9, 89)]
        [TestCase(0.01, -89)]
        [TestCase(5.567, -2.234)]
        public void Transform_ApparentToCelestialTest(double ra, double dec) {
            //Arrange
            var coordinates = new Coordinates(ra, dec, Epoch.JNOW, Coordinates.RAType.Hours);

            //Act
            coordinates = coordinates.Transform(Epoch.J2000);

            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetApparent(ra, dec);

            //Assert
            //Check with ascom transformation that the transformation logic matches
            Assert.AreEqual(transform.RAJ2000, coordinates.RA, 0.0001);
            Assert.AreEqual(transform.DecJ2000, coordinates.Dec, 0.0001);
        }

        [Test]
        [TestCase(0, 0, 0, 0, 0, 0, 0)]
        [TestCase(0, 0, 0, 0, 90, 0, 0)]
        [TestCase(0, 0, 0, 0, 180, 0, 0)]
        [TestCase(0, 0, 0, 0, 270, 0, 0)]
        [TestCase(0, 0, 0, 0, 360, 0, 0)]
        /*Slight Shift - Rotation 0*/
        [TestCase(0, 0, 10, 0, 0, 350.09972275101, 0)]
        [TestCase(0, 0, 0, 10, 0, 0, -9.90027724898984)]
        [TestCase(0, 0, 10, 10, 0, 350.09972275101, -9.75570096944661)]
        [TestCase(0, 0, -10, 0, 0, 9.90027724898984, 0)]
        [TestCase(0, 0, 0, -10, 0, 0, 9.90027724898984)]
        [TestCase(0, 0, -10, -10, 0, 9.90027724898984, 9.75570096944661)]
        /*Slight Shift - Rotation 90*/
        [TestCase(0, 0, 10, 0, 90, 0, -9.90027724898984)]
        [TestCase(0, 0, 0, 10, 90, 9.90027724898984, -6.031850605973E-16)]
        [TestCase(0, 0, 10, 10, 90, 9.90027724898984, -9.75570096944661)]
        [TestCase(0, 0, -10, 0, 90, 6.12303176911189E-16, 9.90027724898984)]
        [TestCase(0, 0, 0, -10, 90, 350.09972275101, 6.031850605973E-16)]
        [TestCase(0, 0, -10, -10, 90, 350.09972275101, 9.75570096944661)]
        /*Slight Shift - Rotation 180*/
        [TestCase(0, 0, 10, 0, 180, 9.90027724898984, -1.2063701211946E-15)]
        [TestCase(0, 0, 0, 10, 180, 1.22460635382238E-15, 9.90027724898984)]
        [TestCase(0, 0, 10, 10, 180, 9.90027724898984, 9.75570096944661)]
        [TestCase(0, 0, -10, 0, 180, 350.09972275101, 1.2063701211946E-15)]
        [TestCase(0, 0, 0, -10, 180, 0, -9.90027724898984)]
        [TestCase(0, 0, -10, -10, 180, 350.09972275101, -9.75570096944661)]
        /*Slight Shift - Rotation 270*/
        [TestCase(0, 0, 10, 0, 270, 1.83690953073357E-15, 9.90027724898984)]
        [TestCase(0, 0, 0, 10, 270, 350.09972275101, 1.8095551817919E-15)]
        [TestCase(0, 0, 10, 10, 270, 350.09972275101, 9.75570096944661)]
        [TestCase(0, 0, -10, 0, 270, 0, -9.90027724898984)]
        [TestCase(0, 0, 0, -10, 270, 9.90027724898984, -1.8095551817919E-15)]
        [TestCase(0, 0, -10, -10, 270, 9.90027724898984, -9.75570096944661)]
        /*Slight Shift - Rotation 360*/
        [TestCase(0, 0, 10, 0, 360, 350.09972275101, 2.4127402423892E-15)]
        [TestCase(0, 0, 0, 10, 360, 0, -9.90027724898984)]
        [TestCase(0, 0, 10, 10, 360, 350.09972275101, -9.75570096944661)]
        [TestCase(0, 0, -10, 0, 360, 9.90027724898984, -2.4127402423892E-15)]
        [TestCase(0, 0, 0, -10, 360, 2.44921270764475E-15, 9.90027724898984)]
        [TestCase(0, 0, -10, -10, 360, 9.90027724898984, 9.75570096944661)]
        /*High Dec - Slight Shift - Rotation 0*/
        [TestCase(4, 80, 10, 0, 0, 14.8544085534108, 75.9637523968214)]
        [TestCase(8, 80, 0, 10, 0, 120, 70.0997227510102)]
        [TestCase(12, 80, 10, 10, 0, 153.200873607711, 67.9244644754593)]
        [TestCase(16, 80, -10, 0, 0, 285.145591446589, 75.9637523968214)]
        [TestCase(20, 80, 0, -10, 0, 300, 89.9002772489898)]
        [TestCase(24, 80, -10, -10, 0, 89.4200136589819, 80.2437942958025)]
        /*High Dec - Slight Shift - Rotation 90*/
        [TestCase(4, 80, 10, 0, 90, 60, 70.0997227510102)]
        [TestCase(8, 80, 0, 10, 90, 165.145591446589, 75.9637523968214)]
        [TestCase(12, 80, 10, 10, 90, 206.799126392289, 67.9244644754593)]
        [TestCase(16, 80, -10, 0, 90, 240, 89.9002772489898)]
        [TestCase(20, 80, 0, -10, 90, 254.854408553411, 75.9637523968214)]
        [TestCase(24, 80, -10, -10, 90, 270.579986341018, 80.2437942958025)]
        /*High Dec - Slight Shift - Rotation 180*/
        [TestCase(4, 80, 10, 0, 180, 105.145591446589, 75.9637523968214)]
        [TestCase(8, 80, 0, 10, 180, 120.000000000001, 89.9002772489898)]
        [TestCase(12, 80, 10, 10, 180, 269.420013658982, 80.2437942958023)]
        [TestCase(16, 80, -10, 0, 180, 194.854408553411, 75.9637523968214)]
        [TestCase(20, 80, 0, -10, 180, 300, 70.0997227510102)]
        [TestCase(24, 80, -10, -10, 180, 333.200873607711, 67.9244644754593)]
        /*High Dec - Slight Shift - Rotation 270*/
        [TestCase(4, 80, 10, 0, 270, 60.000000000001, 89.9002772489898)]
        [TestCase(8, 80, 0, 10, 270, 74.8544085534108, 75.9637523968214)]
        [TestCase(12, 80, 10, 10, 270, 90.5799863410181, 80.2437942958028)]
        [TestCase(16, 80, -10, 0, 270, 240, 70.0997227510102)]
        [TestCase(20, 80, 0, -10, 270, 345.145591446589, 75.9637523968214)]
        [TestCase(24, 80, -10, -10, 270, 26.7991263922891, 67.9244644754593)]
        /*High Dec - Slight Shift - Rotation 360*/
        [TestCase(4, 80, 10, 0, 360, 14.8544085534108, 75.9637523968214)]
        [TestCase(8, 80, 0, 10, 360, 120, 70.0997227510102)]
        [TestCase(12, 80, 10, 10, 360, 153.200873607711, 67.9244644754593)]
        [TestCase(16, 80, -10, 0, 360, 285.145591446589, 75.9637523968214)]
        [TestCase(20, 80, 0, -10, 360, 300.000000000001, 89.9002772489898)]
        [TestCase(24, 80, -10, -10, 360, 89.4200136589819, 80.2437942958023)]
        public void Shift_CoordinatesTest(double ra, double dec, double deltaX, double deltaY, double rotation, double expectedRA, double expectedDec) {
            var coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);

            var shifted = coordinates.Shift(deltaX, deltaY, rotation);

            Assert.AreEqual(expectedRA, shifted.RADegrees, ANGLE_TOLERANCE);
            Assert.AreEqual(expectedDec, shifted.Dec, ANGLE_TOLERANCE);
        }

        [Test]
        [TestCase(0, 0, 0, 0, 100, 100, 1, 0, 100, 100)]
        [TestCase(0, 10, 0, 0, 100, 100, 1, 0, 100, -36270.050511967027)]
        [TestCase(10, 0, 0, 0, 100, 100, 1, 0, -36270.050511967027, 100)]
        [TestCase(10, 10, 0, 0, 100, 100, 1, 0, -36270.05051196702, -36831.117165480086)]
        [TestCase(-10, 0, 0, 0, 100, 100, 1, 0, 36470.050511967027, 100)]
        [TestCase(0, -10, 0, 0, 100, 100, 1, 0, 100, 36470.050511967027)]
        [TestCase(-10, -10, 0, 0, 100, 100, 1, 0, 36470.05051196702, 37031.117165480086)]
        /* Rotation 90 */
        [TestCase(0, 0, 0, 0, 100, 100, 1, 90, 100, 100)]
        [TestCase(0, 10, 0, 0, 100, 100, 1, 90, -36270.050511967027, 99.999999999997769)]
        [TestCase(10, 0, 0, 0, 100, 100, 1, 90, 99.999999999997769, 36470.050511967027)]
        [TestCase(10, 10, 0, 0, 100, 100, 1, 90, -36831.117165480086, 36470.05051196702)]
        [TestCase(-10, 0, 0, 0, 100, 100, 1, 90, 100.00000000000223, -36270.050511967027)]
        [TestCase(0, -10, 0, 0, 100, 100, 1, 90, 36470.050511967027, 100.00000000000223)]
        [TestCase(-10, -10, 0, 0, 100, 100, 1, 90, 37031.117165480086, -36270.05051196702)]
        /* Center high dec */
        [TestCase(80, 80, 80, 80, 100, 100, 1, 0, 100, 100)]
        [TestCase(0, 0, 80, 80, 100, 100, 1, 0, 6736628.1997940624, 1169885.8456961261)]
        [TestCase(0, 10, 80, 80, 100, 100, 1, 0, 996809.1228493352, 142187.83605427248)]
        [TestCase(10, 0, 80, 80, 100, 100, 1, 0, 3263640.7132176529, 1169885.8456961263)]
        [TestCase(10, 10, 80, 80, 100, 100, 1, 0, 831828.69500885683, 271124.45064052724)]
        [TestCase(-10, 0, 80, 80, 100, 100, 1, 0, 1.9399404130470604E+22, 1169885.8456961261)]
        [TestCase(0, -10, 80, 80, 100, 100, 1, 0, -1415502.6695668532, -289729.61543120455)]
        [TestCase(-10, -10, 80, 80, 100, 100, 1, 0, -1187731.6779271192, -36270.050511967114)]
        /* Center high dec Rotation 90*/
        [TestCase(80, 80, 80, 80, 100, 100, 1, 90, 100, 100)]
        [TestCase(0, 0, 80, 80, 100, 100, 1, 90, 1169885.8456961266, -6736428.1997940624)]
        [TestCase(0, 10, 80, 80, 100, 100, 1, 90, 142187.83605427254, -996609.1228493352)]
        [TestCase(10, 0, 80, 80, 100, 100, 1, 90, 1169885.8456961266, -3263440.7132176529)]
        [TestCase(10, 10, 80, 80, 100, 100, 1, 90, 271124.4506405273, -831628.69500885683)]
        [TestCase(-10, 0, 80, 80, 100, 100, 1, 90, 2357717.5236232448, -1.9399404130470604E+22)]
        [TestCase(0, -10, 80, 80, 100, 100, 1, 90, -289729.61543120461, 1415702.6695668532)]
        [TestCase(-10, -10, 80, 80, 100, 100, 1, 90, -36270.050511967194, 1187931.6779271192)]
        public void GnomonicTanProjectionTest(double ra, double dec, double centerRa, double centerDec, double centerX, double centerY, double arcSecPerPixel, double rotation, double expectedX, double expectedY) {
            var coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            var centerPoint = new Point(centerX, centerY);
            var center = new Coordinates(centerRa, centerDec, Epoch.J2000, Coordinates.RAType.Degrees);

            var p = coordinates.XYProjection(center, centerPoint, arcSecPerPixel, arcSecPerPixel, rotation, Coordinates.ProjectionType.Gnomonic);

            var expectedPoint = new Point(expectedX, expectedY);

            Assert.AreEqual(expectedPoint.X, p.X, ANGLE_TOLERANCE);
            Assert.AreEqual(expectedPoint.Y, p.Y, ANGLE_TOLERANCE);
        }
    }
}