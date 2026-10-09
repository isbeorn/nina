using FluentAssertions;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.AstrometryTest {
    [TestFixture]
    public class WorldCoordinateSystemTest {
        [Test]
        [TestCase(184.4986182148048, 47.2023067837935, 2328.65927451, 1760.64837362, -0.0002218599577337, 0.0001551969897898, 0.0001550825162187, 0.0002219379280554, 325.046, 0.974, 0.974)]
        [TestCase(85.41227853247808, -2.256044729836495, 1677.51036549, 1265.00061302, -1.19409568357e-05, 0.0004180497740614, 0.0004180867435723, 1.203801387e-05, 271.636, 1.506, 1.506)]
        [TestCase(194.177945901117, 21.66907682939931, 2320.57411033, 1753.4508792, -0.0001167769034989, -0.0002470716905644, -0.0002469362868456, 0.0001168178576494, 64.69, 0.983, 0.983)]
        public void Test1(double crval1, double crval2, double crpix1, double crpix2, double cd1_1, double cd1_2, double cd2_1, double cd2_2, double expectedRotation, double expectedPixelScaleX, double expectedPixelScaleY) {

            var wcs = new WorldCoordinateSystem(crval1, crval2, crpix1, crpix2, cd1_1, cd1_2, cd2_1, cd2_2);

            wcs.Rotation.Should().BeApproximately(expectedRotation, 0.001);
            wcs.PositionAngle.Should().BeApproximately(AstroUtil.EuclidianModulus(360 - expectedRotation, 360), 0.001);
            wcs.PixelScaleX.Should().BeApproximately(expectedPixelScaleX, 0.001);
            wcs.PixelScaleY.Should().BeApproximately(expectedPixelScaleY, 0.001);
        }

        private const double AngleTolerance = 1e-10;

        // FITS WCS Paper I CD transform and Paper II TAN projection.
        // At an equatorial reference point, tan(RA) = xi and tan(Dec) = eta*cos(RA).
        [TestCase(-0.001, 0, 0, 0.001, 100, 100)]
        [TestCase(0.001, 0, 0, 0.001, 100, 100)]
        [TestCase(-0.001, 0, 0, -0.001, 100, 100)]
        [TestCase(0.001, 0, 0, -0.001, 100, 100)]
        [TestCase(-0.001, 0.0005, 0, 0.001, 0, 100)]
        [TestCase(0, -0.002, 0.001, 0, 100, 50)]
        public void GetCoordinates_SignedAndSkewedMatrices_PreservesTangentPlane(double cd11, double cd12, double cd21, double cd22, double x, double y) {
            var wcs = new WorldCoordinateSystem(0, 0, 10, 20, cd11, cd12, cd21, cd22);
            var result = wcs.GetCoordinates(x + 10, y + 20);
            double xi = AstroUtil.ToRadians(cd11 * x + cd12 * y);
            double eta = AstroUtil.ToRadians(cd21 * x + cd22 * y);
            double ra = AstroUtil.ToRadians(result.RADegrees);
            double dec = AstroUtil.ToRadians(result.Dec);
            Math.Tan(ra).Should().BeApproximately(xi, 1e-12);
            (Math.Tan(dec) / Math.Cos(ra)).Should().BeApproximately(eta, 1e-12);
            result.Epoch.Should().Be(Epoch.J2000);
        }

        [TestCase(-0.001, 0.002, 30)]
        [TestCase(0.001, 0.002, 30)]
        [TestCase(-0.001, -0.002, 120)]
        [TestCase(0.001, -0.002, 270)]
        public void GetCoordinates_CdeltaRotation_AgreesWithEquivalentCdMatrix(double scaleX, double scaleY, double rotation) {
            double sine = Math.Sin(AstroUtil.ToRadians(rotation));
            double cosine = Math.Cos(AstroUtil.ToRadians(rotation));
            var matrix = new WorldCoordinateSystem(359.9, 80, 10, 20,
                scaleX * cosine, -scaleY * sine, scaleX * sine, scaleY * cosine);
            var legacy = new WorldCoordinateSystem(359.9, 80, 10, 20, scaleX, scaleY, rotation);
            foreach (var pixel in new[] { (10d, 20d), (110d, -180d), (-190d, 120d) }) {
                var expected = matrix.GetCoordinates(pixel.Item1, pixel.Item2);
                var actual = legacy.GetCoordinates(pixel.Item1, pixel.Item2);
                actual.RADegrees.Should().BeApproximately(expected.RADegrees, 1e-10);
                actual.Dec.Should().BeApproximately(expected.Dec, 1e-10);
            }
        }

        [TestCase(89.9)]
        [TestCase(-89.9)]
        [TestCase(90)]
        [TestCase(-90)]
        public void GetCoordinates_PolarReferenceAndRaWrap_ProjectsBackToTangentPlane(double referenceDec) {
            var wcs = new WorldCoordinateSystem(359.9, referenceDec, 0, 0, -0.001, 0.0005, 0.0002, 0.001);
            foreach (double x in new[] { -100d, 100d }) {
                const double y = 200;
                var result = wcs.GetCoordinates(x, y);
                double dec = AstroUtil.ToRadians(result.Dec);
                double dec0 = AstroUtil.ToRadians(referenceDec);
                double deltaRa = AstroUtil.ToRadians(result.RADegrees - 359.9);
                double denominator = Math.Sin(dec) * Math.Sin(dec0) + Math.Cos(dec) * Math.Cos(dec0) * Math.Cos(deltaRa);
                double xi = Math.Cos(dec) * Math.Sin(deltaRa) / denominator;
                double eta = (Math.Sin(dec) * Math.Cos(dec0) - Math.Cos(dec) * Math.Sin(dec0) * Math.Cos(deltaRa)) / denominator;
                xi.Should().BeApproximately(AstroUtil.ToRadians(-0.001 * x + 0.0005 * y), 1e-12);
                eta.Should().BeApproximately(AstroUtil.ToRadians(0.0002 * x + 0.001 * y), 1e-12);
                result.RADegrees.Should().BeInRange(0, 360);
            }
        }

        /// <summary>
        /// Verifies WCS coordinate lookup at the reference pixel and one pixel from center, covering
        /// the production GetCoordinates path in addition to direct projection helpers.
        /// </summary>
        [Test]
        public void WorldCoordinateSystemGetCoordinates_ReferenceAndOffsetPixels_ReturnsExpectedSkyCoordinates() {
            WorldCoordinateSystem wcs = new WorldCoordinateSystem(
                180.0,
                45.0,
                1000.0,
                1000.0,
                -1.0 / 3600.0,
                1.0 / 3600.0,
                0.0);

            Coordinates center = wcs.GetCoordinates(1000.0, 1000.0);
            Coordinates offset = wcs.GetCoordinates(1001.0, 1000.0);

            center.RADegrees.Should().BeApproximately(180.0, 1e-12);
            center.Dec.Should().BeApproximately(45.0, 1e-12);
            offset.RADegrees.Should().BeLessThan(center.RADegrees);
        }

        /// <summary>
        /// Verifies WCS flipped-axis handling for both CD-matrix and CDELT/CROTA inputs, because
        /// FITS solvers can encode handedness in either form.
        /// </summary>
        [Test]
        public void WorldCoordinateSystem_FlippedAxisInputs_SetFlippedAndAdjustedRotation() {
            WorldCoordinateSystem matrix = new WorldCoordinateSystem(
                180.0,
                45.0,
                1000.0,
                1000.0,
                1.0 / 3600.0,
                0.0,
                0.0,
                1.0 / 3600.0);
            WorldCoordinateSystem cdelt = new WorldCoordinateSystem(
                180.0,
                45.0,
                1000.0,
                1000.0,
                1.0 / 3600.0,
                1.0 / 3600.0,
                30.0);

            matrix.Flipped.Should().BeTrue();
            cdelt.Flipped.Should().BeTrue();
            cdelt.Rotation.Should().BeApproximately(330.0, AngleTolerance);
        }
    }
}
