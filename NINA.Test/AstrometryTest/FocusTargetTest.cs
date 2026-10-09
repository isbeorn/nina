using FluentAssertions;
using NINA.Astrometry;
using System;

namespace NINA.Test.AstrometryTest {
    [TestFixture]
    public class FocusTargetTest {
        [Test]
        public void CalculateAltAz_MatchesObservedCoordinateTransformAtCallTime() {
            var coordinates = new Coordinates(5, -20, Epoch.J2000, Coordinates.RAType.Hours);
            var target = new FocusTarget("catalog star") { Coordinates = coordinates };
            var before = DateTime.UtcNow;
            target.CalculateAltAz(35, -105);
            var after = DateTime.UtcNow;
            // An independent observed-place SOFA path, with zero refraction and no shared
            // spherical altitude helper. Bracket the real clock instead of asserting one instant.
            var a = coordinates.Transform(Angle.ByDegree(35), Angle.ByDegree(-105), 0, before);
            var b = coordinates.Transform(Angle.ByDegree(35), Angle.ByDegree(-105), 0, after);
            target.Altitude.Should().BeInRange(Math.Min(a.Altitude.Degree, b.Altitude.Degree) - 0.002,
                Math.Max(a.Altitude.Degree, b.Altitude.Degree) + 0.002);
            var delta = AstroUtil.EuclidianModulus(target.Azimuth - a.Azimuth.Degree + 180, 360) - 180;
            var elapsedAzimuth = AstroUtil.EuclidianModulus(b.Azimuth.Degree - a.Azimuth.Degree + 180, 360) - 180;
            Math.Abs(delta).Should().BeLessThan(Math.Abs(elapsedAzimuth) + 0.002);
        }
    }
}
