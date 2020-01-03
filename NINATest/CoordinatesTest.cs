#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using FluentAssertions;
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
        public void ShiftGnomonic_CoordinatesTest(double ra, double dec, double deltaX, double deltaY, double rotation, double expectedRA, double expectedDec) {
            var coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);

            var shifted = coordinates.Shift(deltaX, deltaY, rotation, Coordinates.ProjectionType.Gnomonic);

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

        [Test]
        [TestCase(0, 0, 0, 0, 0, 0, 0)]
        [TestCase(0, 0, 0, 0, 90, 0, 0)]
        [TestCase(0, 0, 0, 0, 180, 0, 0)]
        [TestCase(0, 0, 0, 0, 270, 0, 0)]
        [TestCase(0, 0, 0, 0, 360, 0, 0)]
        [TestCase(0, 0, 10, 0, 0, 350.02526942249, 0)]
        [TestCase(0, 0, 0, 10, 0, 0, -9.9747305775100159)]
        [TestCase(0, 0, 10, 10, 0, 349.94969937586677, -9.89915183179233)]
        [TestCase(0, 0, -10, 0, 0, 9.9747305775100159, 0)]
        [TestCase(0, 0, 0, -10, 0, 0, 9.9747305775100159)]
        [TestCase(0, 0, -10, -10, 0, 10.050300624133238, 9.89915183179233)]
        [TestCase(0, 0, 10, 0, 90, 0, -9.9747305775100159)]
        [TestCase(0, 0, 0, 10, 90, 9.9747305775100159, -6.0767546361632621E-16)]
        [TestCase(0, 0, 10, 10, 90, 10.050300624133238, -9.89915183179233)]
        [TestCase(0, 0, -10, 0, 90, 6.170019151964536E-16, 9.9747305775100159)]
        [TestCase(0, 0, 0, -10, 90, 350.02526942249, 6.0767546361632621E-16)]
        [TestCase(0, 0, -10, -10, 90, 349.94969937586677, 9.89915183179233)]
        [TestCase(0, 0, 10, 0, 180, 9.9747305775100159, -1.2153509272326524E-15)]
        [TestCase(0, 0, 0, 10, 180, 1.2340038303929072E-15, 9.9747305775100159)]
        [TestCase(0, 0, 10, 10, 180, 10.050300624133239, 9.89915183179233)]
        [TestCase(0, 0, -10, 0, 180, 350.02526942249, 1.2153509272326524E-15)]
        [TestCase(0, 0, 0, -10, 180, 0, -9.9747305775100159)]
        [TestCase(0, 0, -10, -10, 180, 349.94969937586677, -9.89915183179233)]
        [TestCase(0, 0, 10, 0, 270, 1.8510057455893608E-15, 9.9747305775100159)]
        [TestCase(0, 0, 0, 10, 270, 350.02526942249, 1.8230263908489786E-15)]
        [TestCase(0, 0, 10, 10, 270, 349.94969937586677, 9.8991518317923326)]
        [TestCase(0, 0, -10, 0, 270, 0, -9.9747305775100159)]
        [TestCase(0, 0, 0, -10, 270, 9.9747305775100159, -1.8230263908489786E-15)]
        [TestCase(0, 0, -10, -10, 270, 10.050300624133236, -9.8991518317923326)]
        [TestCase(0, 0, 10, 0, 360, 350.02526942249, 2.4307018544653048E-15)]
        [TestCase(0, 0, 0, 10, 360, 0, -9.9747305775100159)]
        [TestCase(0, 0, 10, 10, 360, 349.94969937586677, -9.899151831792329)]
        [TestCase(0, 0, -10, 0, 360, 9.9747305775100159, -2.4307018544653048E-15)]
        [TestCase(0, 0, 0, -10, 360, 2.4680076607858144E-15, 9.9747305775100159)]
        [TestCase(0, 0, -10, -10, 360, 10.050300624133241, 9.899151831792329)]
        [TestCase(4, 80, 10, 0, 0, 14.635416655035876, 75.911675361071474)]
        [TestCase(8, 80, 0, 10, 0, 119.99999999999999, 70.025269422489984)]
        [TestCase(12, 80, 10, 10, 0, 153.02329671845297, 67.729687331465755)]
        [TestCase(16, 80, -10, 0, 0, 285.3645833449641, 75.911675361071474)]
        [TestCase(20, 80, 0, -10, 0, 300, 89.974730577496516)]
        [TestCase(24, 80, -10, -10, 0, 90.288232304955216, 80.100721646704116)]
        [TestCase(4, 80, 10, 0, 90, 59.999999999999993, 70.025269422489984)]
        [TestCase(8, 80, 0, 10, 90, 165.3645833449641, 75.911675361071474)]
        [TestCase(12, 80, 10, 10, 90, 206.97670328154703, 67.729687331465755)]
        [TestCase(16, 80, -10, 0, 90, 240.00000000000136, 89.974730577496516)]
        [TestCase(20, 80, 0, -10, 90, 254.63541665503587, 75.911675361071474)]
        [TestCase(24, 80, -10, -10, 90, 269.71176769504484, 80.100721646704116)]
        [TestCase(4, 80, 10, 0, 180, 105.36458334496412, 75.911675361071474)]
        [TestCase(8, 80, 0, 10, 180, 120.00000000000274, 89.974730577496516)]
        [TestCase(12, 80, 10, 10, 180, 270.28823230495391, 80.100721646704116)]
        [TestCase(16, 80, -10, 0, 180, 194.63541665503587, 75.911675361071474)]
        [TestCase(20, 80, 0, -10, 180, 300, 70.025269422489984)]
        [TestCase(24, 80, -10, -10, 180, 333.02329671845297, 67.729687331465755)]
        [TestCase(4, 80, 10, 0, 270, 60.000000000004128, 89.974730577496516)]
        [TestCase(8, 80, 0, 10, 270, 74.635416655035883, 75.911675361071474)]
        [TestCase(12, 80, 10, 10, 270, 89.711767695042226, 80.100721646704116)]
        [TestCase(16, 80, -10, 0, 270, 239.99999999999997, 70.025269422489984)]
        [TestCase(20, 80, 0, -10, 270, 345.36458334496416, 75.911675361071474)]
        [TestCase(24, 80, -10, -10, 270, 26.976703281547032, 67.729687331465755)]
        [TestCase(4, 80, 10, 0, 360, 14.635416655035876, 75.911675361071474)]
        [TestCase(8, 80, 0, 10, 360, 119.99999999999999, 70.025269422489984)]
        [TestCase(12, 80, 10, 10, 360, 153.02329671845294, 67.729687331465755)]
        [TestCase(16, 80, -10, 0, 360, 285.3645833449641, 75.911675361071474)]
        [TestCase(20, 80, 0, -10, 360, 300.00000000000551, 89.974730577496516)]
        [TestCase(24, 80, -10, -10, 360, 90.288232304951464, 80.100721646704116)]
        public void ShiftStereographic_CoordinatesTest(double ra, double dec, double deltaX, double deltaY, double rotation, double expectedRA, double expectedDec) {
            var coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours);

            var shifted = coordinates.Shift(deltaX, deltaY, rotation, Coordinates.ProjectionType.Stereographic);

            Assert.AreEqual(expectedRA, shifted.RADegrees, ANGLE_TOLERANCE);
            Assert.AreEqual(expectedDec, shifted.Dec, ANGLE_TOLERANCE);
        }

        [Test]
        [TestCase(0, 0, 0, 0, 100, 100, 1, 0, 100, 100)]
        [TestCase(0, 10, 0, 0, 100, 100, 1, 0, 100, -35991.664461984234)]
        [TestCase(10, 0, 0, 0, 100, 100, 1, 0, -35991.664461984234, 100)]
        [TestCase(10, 10, 0, 0, 100, 100, 1, 0, -35713.311030138284, -36265.7890797437)]
        [TestCase(-10, 0, 0, 0, 100, 100, 1, 0, 36191.664461984234, 100)]
        [TestCase(0, -10, 0, 0, 100, 100, 1, 0, 100, 36191.664461984234)]
        [TestCase(-10, -10, 0, 0, 100, 100, 1, 0, 35913.311030138284, 36465.7890797437)]
        [TestCase(0, 0, 0, 0, 100, 100, 1, 90, 100, 100)]
        [TestCase(0, 10, 0, 0, 100, 100, 1, 90, -35991.664461984234, 99.999999999997783)]
        [TestCase(10, 0, 0, 0, 100, 100, 1, 90, 99.999999999997783, 36191.664461984234)]
        [TestCase(10, 10, 0, 0, 100, 100, 1, 90, -36265.7890797437, 35913.311030138284)]
        [TestCase(-10, 0, 0, 0, 100, 100, 1, 90, 100.00000000000222, -35991.664461984234)]
        [TestCase(0, -10, 0, 0, 100, 100, 1, 90, 36191.664461984234, 100.00000000000222)]
        [TestCase(-10, -10, 0, 0, 100, 100, 1, 90, 36465.7890797437, -35713.311030138284)]
        [TestCase(80, 80, 80, 80, 100, 100, 1, 0, 100, 100)]
        [TestCase(0, 0, 80, 80, 100, 100, 1, 0, 394470.631130169, 68581.741421111044)]
        [TestCase(0, 10, 80, 80, 100, 100, 1, 0, 333312.65675437177, 47601.787892514942)]
        [TestCase(10, 0, 80, 80, 100, 100, 1, 0, 366018.69179743726, 131260.15516725625)]
        [TestCase(10, 10, 80, 80, 100, 100, 1, 0, 310601.88298505027, 101279.14984041326)]
        [TestCase(-10, 0, 80, 80, 100, 100, 1, 0, 412629.61249419267, 100.00000000002487)]
        [TestCase(0, -10, 80, 80, 100, 100, 1, 0, 466033.47077156231, 95494.930762293821)]
        [TestCase(-10, -10, 80, 80, 100, 100, 1, 0, 490169.11525005684, 15105.357078120629)]
        [TestCase(80, 80, 80, 80, 100, 100, 1, 90, 100, 100)]
        [TestCase(0, 0, 80, 80, 100, 100, 1, 90, 68581.741421111074, -394270.631130169)]
        [TestCase(0, 10, 80, 80, 100, 100, 1, 90, 47601.787892514963, -333112.65675437177)]
        [TestCase(10, 0, 80, 80, 100, 100, 1, 90, 131260.15516725628, -365818.69179743726)]
        [TestCase(10, 10, 80, 80, 100, 100, 1, 90, 101279.14984041327, -310401.88298505027)]
        [TestCase(-10, 0, 80, 80, 100, 100, 1, 90, 100.00000000005014, -412429.61249419267)]
        [TestCase(0, -10, 80, 80, 100, 100, 1, 90, 95494.93076229385, -465833.47077156231)]
        [TestCase(-10, -10, 80, 80, 100, 100, 1, 90, 15105.357078120656, -489969.11525005684)]
        public void StereographicTanProjectionTest(double ra, double dec, double centerRa, double centerDec, double centerX, double centerY, double arcSecPerPixel, double rotation, double expectedX, double expectedY) {
            var coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            var centerPoint = new Point(centerX, centerY);
            var center = new Coordinates(centerRa, centerDec, Epoch.J2000, Coordinates.RAType.Degrees);

            var p = coordinates.XYProjection(center, centerPoint, arcSecPerPixel, arcSecPerPixel, rotation, Coordinates.ProjectionType.Stereographic);

            var expectedPoint = new Point(expectedX, expectedY);

            Assert.AreEqual(expectedPoint.X, p.X, ANGLE_TOLERANCE);
            Assert.AreEqual(expectedPoint.Y, p.Y, ANGLE_TOLERANCE);
        }

        [Test]
        /* Zero distance tests */
        [TestCase(10, 100, Epoch.J2000, 10, 100, Epoch.J2000, 0)]
        [TestCase(10, 100, Epoch.JNOW, 10, 100, Epoch.JNOW, 0)]
        [TestCase(10, -100, Epoch.J2000, 10, -100, Epoch.J2000, 0)]
        [TestCase(10, -100, Epoch.JNOW, 10, -100, Epoch.JNOW, 0)]
        /* Test that different epoch is considered properly */
        // Test case is disabled because JNOW and J2000 will continue getting further apart forever
        // [TestCase(10, 100, Epoch.JNOW, 10, 100, Epoch.J2000, 334.69)]
        /* Actual distance tests */
        [TestCase(0, 0, Epoch.J2000, 0, 10, Epoch.J2000, 36000)]
        [TestCase(0, 0, Epoch.J2000, 0, -10, Epoch.J2000, 36000)]
        [TestCase(0, 10, Epoch.J2000, 0, 0, Epoch.J2000, 36000)]
        [TestCase(0, -10, Epoch.J2000, 0, 0, Epoch.J2000, 36000)]
        [TestCase(10, 0, Epoch.J2000, 0, 0, Epoch.J2000, 540000)]
        [TestCase(10, 0, Epoch.J2000, 0, 0, Epoch.J2000, 540000)]
        [TestCase(0, 0, Epoch.J2000, 10, 0, Epoch.J2000, 540000)]
        [TestCase(0, 0, Epoch.J2000, 10, 0, Epoch.J2000, 540000)]
        [TestCase(10, 10, Epoch.J2000, 10, -10, Epoch.J2000, 72000)]
        [TestCase(10, -10, Epoch.J2000, 10, 10, Epoch.J2000, 72000)]
        [TestCase(10, 10, Epoch.J2000, 20, 20, Epoch.J2000, 496460.69)]
        [TestCase(20, 20, Epoch.J2000, 10, 10, Epoch.J2000, 496460.69)]
        public void CoordinateSubstractionTest(double ra1, double dec1, Epoch epoch1, double ra2, double dec2, Epoch epoch2, double expectedDistance) {
            var c1 = new Coordinates(Angle.ByHours(ra1), Angle.ByDegree(dec1), epoch1);
            var c2 = new Coordinates(Angle.ByHours(ra2), Angle.ByDegree(dec2), epoch2);

            var sut = c1 - c2;

            sut.Distance.ArcSeconds.Should().BeApproximately(expectedDistance, 0.01);
        }
    }
}