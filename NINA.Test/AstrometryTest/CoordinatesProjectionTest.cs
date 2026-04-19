using FluentAssertions;
using NINA.Astrometry;
using System;
using System.Windows;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class CoordinatesProjectionTest {

        [Test]
        public void XYProjection_ViewportOverload_UsesRequestedProjectionType() {
            Coordinates center = new Coordinates(Angle.ByDegree(40.0), Angle.ByDegree(70.0), Epoch.J2000);
            Coordinates target = new Coordinates(Angle.ByDegree(43.0), Angle.ByDegree(72.0), Epoch.J2000);
            ViewportFoV viewPort = new ViewportFoV(center, 10.0, 2000.0, 1500.0, 17.0);

            Point actual = target.XYProjection(viewPort, Coordinates.ProjectionType.Gnomonic);
            Point expectedGnomonic = target.XYProjection(
                viewPort.CenterCoordinates,
                viewPort.ViewPortCenterPoint,
                viewPort.ArcSecWidth,
                viewPort.ArcSecHeight,
                viewPort.Rotation,
                Coordinates.ProjectionType.Gnomonic);
            Point expectedStereographic = target.XYProjection(
                viewPort.CenterCoordinates,
                viewPort.ViewPortCenterPoint,
                viewPort.ArcSecWidth,
                viewPort.ArcSecHeight,
                viewPort.Rotation,
                Coordinates.ProjectionType.Stereographic);

            actual.X.Should().BeApproximately(expectedGnomonic.X, 1e-9);
            actual.Y.Should().BeApproximately(expectedGnomonic.Y, 1e-9);
            Math.Abs(expectedGnomonic.X - expectedStereographic.X).Should().BeGreaterThan(1e-6);
        }
    }
}
