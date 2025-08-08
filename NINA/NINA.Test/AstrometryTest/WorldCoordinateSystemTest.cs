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
    }
}
