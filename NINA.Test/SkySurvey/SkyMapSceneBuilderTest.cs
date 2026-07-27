#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Astrometry;
using NINA.WPF.Base.SkySurvey;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfImage = System.Windows.Controls.Image;

namespace NINA.Test.SkySurvey {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class SkyMapSceneBuilderTest {

        [Test]
        public void Build_WhenViewportMoves_ReprojectsEveryEnabledLayer() {
            Star firstStar = new Star(1, "First", CelestialCoordinates(82, -2), 2);
            Star secondStar = new Star(2, "Second", CelestialCoordinates(86, 3), 3);
            Constellation constellation = new Constellation("ORI") {
                Stars = [firstStar, secondStar]
            };
            constellation.StarConnections.Add(Tuple.Create(firstStar, secondStar));

            DeepSkyObject dso = new DeepSkyObject("M42", CelestialCoordinates(84, -1), null) {
                DSOType = "BRTNB",
                Size = 3600,
                SizeMin = 2400
            };
            ConstellationBoundary boundary = new ConstellationBoundary {
                Name = "ORI",
                Boundaries = [CelestialCoordinates(80, -5), CelestialCoordinates(90, -5), CelestialCoordinates(90, 5)]
            };

            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([constellation], [dso], [boundary]);
            ViewportFoV firstViewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);
            ViewportFoV movedViewport = new ViewportFoV(CelestialCoordinates(90, 0), 30, 1200, 800, 0);

            SkyMapScene first = sut.Build(firstViewport, SkyMapRenderOptions.All);
            SkyMapScene moved = sut.Build(movedViewport, SkyMapRenderOptions.All);

            first.Stars.Should().ContainSingle(x => x.Id == firstStar.Id);
            first.ConstellationLines.Should().ContainSingle();
            first.DeepSkyObjects.Should().ContainSingle(x => x.Id == dso.Id);
            first.ConstellationBoundaries.Should().ContainSingle();
            first.ConstellationBoundaries[0].Closed.Should().BeTrue();
            first.GridLines.Should().NotBeEmpty();
            first.GridLines.Should().Contain(x => x.StrokeThickness == 3);
            first.Labels.Should().Contain(x => x.Text == firstStar.Name && x.Kind == SkyMapLabelKind.Star);
            first.Labels.Should().Contain(x => x.Text == constellation.Name && x.Kind == SkyMapLabelKind.Constellation);
            first.Labels.Should().Contain(x => x.Kind == SkyMapLabelKind.Grid);
            Point expectedStarPosition = firstStar.Coords.XYProjection(firstViewport);
            first.Stars.Single(x => x.Id == firstStar.Id).Center.X.Should().BeApproximately(expectedStarPosition.X, 1E-9);
            first.Stars.Single(x => x.Id == firstStar.Id).Center.Y.Should().BeApproximately(expectedStarPosition.Y, 1E-9);
            first.DeepSkyObjects.Single().RadiusX.Should().Be(first.DeepSkyObjects.Single().RadiusY);

            moved.Stars.Single(x => x.Id == firstStar.Id).Center.Should().NotBe(first.Stars.Single(x => x.Id == firstStar.Id).Center);
            moved.ConstellationLines[0].Start.Should().NotBe(first.ConstellationLines[0].Start);
            moved.DeepSkyObjects.Single().Center.Should().NotBe(first.DeepSkyObjects.Single().Center);
            moved.ConstellationBoundaries[0].Points[0].Should().NotBe(first.ConstellationBoundaries[0].Points[0]);
            moved.GridLines[0].Points[0].Should().NotBe(first.GridLines[0].Points[0]);
        }

        [Test]
        public void Build_WhenVisibilityChanges_AppliesItToEveryCelestialLayer() {
            Star hiddenStar = new Star(1, "Hidden", CelestialCoordinates(82, -2), 2);
            Star visibleStar = new Star(2, "Visible", CelestialCoordinates(86, 3), 3);
            Constellation constellation = new Constellation("ORI") {
                Stars = [hiddenStar, visibleStar]
            };
            constellation.StarConnections.Add(Tuple.Create(hiddenStar, visibleStar));

            DeepSkyObject hiddenDso = new DeepSkyObject("Hidden DSO", CelestialCoordinates(82, 0), null) { Size = 3600 };
            DeepSkyObject visibleDso = new DeepSkyObject("Visible DSO", CelestialCoordinates(86, 0), null) { Size = 3600 };
            ConstellationBoundary boundary = new ConstellationBoundary {
                Name = "ORI",
                Boundaries = [CelestialCoordinates(82, -5), CelestialCoordinates(86, -5), CelestialCoordinates(86, 5)]
            };
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([constellation], [hiddenDso, visibleDso], [boundary]);
            ViewportFoV viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);

            SkyMapScene scene = sut.Build(viewport, SkyMapRenderOptions.All, new RightAscensionVisibility(84));

            scene.Stars.Should().ContainSingle(x => x.Id == visibleStar.Id);
            scene.ConstellationLines.Should().BeEmpty();
            scene.DeepSkyObjects.Should().ContainSingle(x => x.Id == visibleDso.Id);
            scene.ConstellationBoundaries.Should().ContainSingle();
            scene.ConstellationBoundaries[0].Points.Should().HaveCount(2);
            scene.ConstellationBoundaries[0].Closed.Should().BeFalse();
            scene.GridLines.Should().OnlyContain(x => x.Points.Count > 1);
        }

        [Test]
        public void Build_EquatorialGrid_AnnotatesVisibleRightAscensionAndDeclinationValues() {
            ViewportFoV viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = sut.Build(viewport, SkyMapRenderOptions.EquatorialGrid);

            SkyMapLabel[] visibleLabels = scene.Labels
                .Where(x => x.Kind == SkyMapLabelKind.Grid)
                .Where(x => x.Position.X >= 0 && x.Position.X < viewport.Width)
                .Where(x => x.Position.Y >= 0 && x.Position.Y < viewport.Height)
                .ToArray();
            visibleLabels.Should().Contain(x => x.Text.EndsWith("h", StringComparison.Ordinal));
            visibleLabels.Should().Contain(x => x.Text.EndsWith("°", StringComparison.Ordinal));
        }

        [TestCase(359, 0, 30, 0)]
        [TestCase(85, 45, 20, 37)]
        [TestCase(170, -60, 40, 123)]
        public void Build_EquatorialGrid_AfterPanAndRotation_KeepsCoordinateValuesVisible(
            double rightAscension,
            double declination,
            double verticalFieldOfView,
            double rotation) {
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);
            ViewportFoV firstViewport = new ViewportFoV(
                CelestialCoordinates(rightAscension, declination),
                verticalFieldOfView,
                1200,
                800,
                rotation);
            ViewportFoV movedViewport = new ViewportFoV(
                CelestialCoordinates(rightAscension + 3, declination + 1),
                verticalFieldOfView,
                1200,
                800,
                rotation);

            SkyMapLabel[] firstLabels = VisibleGridLabels(sut.Build(firstViewport, SkyMapRenderOptions.EquatorialGrid), firstViewport);
            SkyMapLabel[] movedLabels = VisibleGridLabels(sut.Build(movedViewport, SkyMapRenderOptions.EquatorialGrid), movedViewport);

            firstLabels.Should().Contain(x => x.Text.EndsWith("h", StringComparison.Ordinal));
            firstLabels.Should().Contain(x => x.Text.EndsWith("°", StringComparison.Ordinal));
            movedLabels.Should().Contain(x => x.Text.EndsWith("h", StringComparison.Ordinal));
            movedLabels.Should().Contain(x => x.Text.EndsWith("°", StringComparison.Ordinal));
            movedLabels.Select(x => x.Position).Should().NotEqual(firstLabels.Select(x => x.Position));
        }

        [Test]
        public void Build_DeepSkyObject_PreservesProjectedAxesAngleAndAliases() {
            DeepSkyObject dso = new DeepSkyObject("NGC1976", CelestialCoordinates(84, -1), null) {
                DSOType = "BRTNB",
                Size = 3600,
                SizeMin = 1800,
                PositionAngle = Angle.ByDegree(25),
                AlsoKnownAs = ["M 42", "NGC 1976"]
            };
            ViewportFoV viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [dso], []);

            SkyMapDeepSkyObject result = sut.Build(viewport, SkyMapRenderOptions.DeepSkyObjects).DeepSkyObjects.Single();

            result.RadiusX.Should().Be(2 * result.RadiusY);
            result.Name.Should().Be($"M 42{Environment.NewLine}NGC 1976");
            result.PositionAngle.Should().NotBe(0);
        }

        [Test]
        public void ObserverSnapshot_UsesLocationTimeAndHorizonUntilRefreshIsDue() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            const double latitude = 50;
            const double longitude = 10;
            double siderealTime = AstroUtil.GetLocalSiderealTime(at, longitude);
            Coordinates zenith = CelestialCoordinates(AstroUtil.HoursToDegrees(siderealTime), latitude);
            Coordinates nadir = CelestialCoordinates(AstroUtil.HoursToDegrees(siderealTime) + 180, -latitude);
            SkyMapObserverSnapshot sut = new SkyMapObserverSnapshot(latitude, longitude, at, _ => 5);

            SkyMapHorizontalCoordinates horizontal = sut.ToHorizontal(zenith);
            Coordinates roundTripSource = CelestialCoordinates(120, 25);
            Coordinates roundTrip = sut.ToCelestial(sut.ToHorizontal(roundTripSource));

            horizontal.Altitude.Should().BeApproximately(90, 0.0001);
            roundTrip.RADegrees.Should().BeApproximately(roundTripSource.RADegrees, 1E-9);
            roundTrip.Dec.Should().BeApproximately(roundTripSource.Dec, 1E-9);
            sut.IsVisible(zenith).Should().BeTrue();
            sut.IsVisible(nadir).Should().BeFalse();
            sut.NeedsRefresh(at.AddSeconds(59)).Should().BeFalse();
            sut.NeedsRefresh(at.AddMinutes(1)).Should().BeTrue();
        }

        [Test]
        public void RasterRenderer_ReusesWritableViewportSurface() {
            SkyMapScene scene = new SkyMapScene(
                [new SkyMapStar(1, "Star", new Point(100, 100), 3)],
                [new SkyMapLine(new Point(100, 100), new Point(200, 200))],
                [new SkyMapDeepSkyObject("M42", "M42", "BRTNB", new Point(300, 300), 20, 15, 30)],
                [new SkyMapPath([new Point(10, 10), new Point(20, 20), new Point(30, 10)])],
                [new SkyMapPath([new Point(0, 400), new Point(1200, 400)])]);
            using SkyMapRasterRenderer sut = new SkyMapRasterRenderer(1200, 800);

            ImageSource first = sut.Render(scene, [], null);
            ImageSource second = sut.Render(scene, [], null);

            first.IsFrozen.Should().BeFalse();
            first.Width.Should().Be(1200);
            first.Height.Should().Be(800);
            second.Should().BeSameAs(first);
        }

        [Test]
        public void RasterRenderer_GridLabelAtViewportEdge_RemainsVisible() {
            SkyMapScene scene = new SkyMapScene(
                [],
                [],
                [],
                [],
                [],
                [new SkyMapLabel("05:40h", new Point(99, 99), SkyMapLabelKind.Grid)]);
            using SkyMapRasterRenderer sut = new SkyMapRasterRenderer(100, 100);

            BitmapSource result = sut.Render(scene, [], null).Should().BeAssignableTo<BitmapSource>().Subject;
            byte[] pixels = new byte[100 * 100 * 4];
            result.CopyPixels(pixels, 100 * 4, 0);

            pixels.Where((_, index) => index % 4 == 3).Should().Contain(x => x > 0);
        }

        [Test]
        public void RasterRenderer_WithCachedImage_ReturnsFreshCompositeForBindingRefresh() {
            BitmapSource cachedImage = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] { 255, 255, 255, 255 },
                4);
            cachedImage.Freeze();
            SkyMapScene scene = new SkyMapScene([], [], [], [], []);
            using SkyMapRasterRenderer sut = new SkyMapRasterRenderer(1200, 800);

            SkyMapImagePlacement placement = new SkyMapImagePlacement(cachedImage, new Point(600, 400), 200, 100, 15);
            ImageSource first = sut.Render(
                scene,
                [placement],
                null);
            ImageSource second = sut.Render(
                scene,
                [placement],
                null);

            first.Should().BeOfType<DrawingImage>();
            first.Width.Should().Be(1200);
            first.Height.Should().Be(800);
            second.Should().NotBeSameAs(first);
        }

        [Test]
        public void RasterRenderer_ConsecutiveCachedFrames_RenderThroughWpfBinding() {
            BitmapSource red = CreatePixel(0, 0, 255);
            BitmapSource blue = CreatePixel(255, 0, 0);
            SkyMapScene scene = new SkyMapScene([], [], [], [], []);
            using SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(25, 50), 20, 20, 0)],
                null);
            byte[] firstFrame = Render(image);
            PixelAt(firstFrame, 100, 25, 50).Should().Equal(0, 0, 255, 255);

            ImageSource firstSource = image.Source;
            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(blue, new Point(75, 50), 20, 20, 0)],
                null);
            byte[] secondFrame = Render(image);

            image.Source.Should().BeSameAs(annotator.SkyMapOverlay);
            image.Source.Should().NotBeSameAs(firstSource);
            PixelAt(secondFrame, 100, 25, 50)[3].Should().Be(0);
            PixelAt(secondFrame, 100, 75, 50).Should().Equal(255, 0, 0, 255);
        }

        private static BitmapSource CreatePixel(byte blue, byte green, byte red) {
            BitmapSource source = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] { blue, green, red, 255 },
                4);
            source.Freeze();
            return source;
        }

        private static byte[] Render(WpfImage image) {
            image.Measure(new Size(100, 100));
            image.Arrange(new Rect(0, 0, 100, 100));
            image.UpdateLayout();
            RenderTargetBitmap bitmap = new RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(image);
            byte[] pixels = new byte[100 * 100 * 4];
            bitmap.CopyPixels(pixels, 100 * 4, 0);
            return pixels;
        }

        private static byte[] PixelAt(byte[] pixels, int width, int x, int y) {
            int offset = (y * width + x) * 4;
            return pixels.Skip(offset).Take(4).ToArray();
        }

        private static SkyMapLabel[] VisibleGridLabels(SkyMapScene scene, ViewportFoV viewport) {
            return scene.Labels
                .Where(x => x.Kind == SkyMapLabelKind.Grid)
                .Where(x => x.Position.X >= 0 && x.Position.X < viewport.Width)
                .Where(x => x.Position.Y >= 0 && x.Position.Y < viewport.Height)
                .ToArray();
        }

        private static Coordinates CelestialCoordinates(double ra, double dec) {
            return new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
        }

        private sealed class RightAscensionVisibility : ISkyMapVisibility {
            private readonly double minimumRightAscension;

            public RightAscensionVisibility(double minimumRightAscension) {
                this.minimumRightAscension = minimumRightAscension;
            }

            public bool IsVisible(Coordinates coordinates) {
                return coordinates.RADegrees >= minimumRightAscension;
            }
        }
    }
}
