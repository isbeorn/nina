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
using NINA.Core.Enum;
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
        public void Build_AltAzGrid_UsesObserverLocationAndTime() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot firstObserver = new SkyMapObserverSnapshot(50, 10, at);
            SkyMapObserverSnapshot laterObserver = new SkyMapObserverSnapshot(50, 10, at.AddHours(1));
            Coordinates center = firstObserver.ToCelestial(new SkyMapHorizontalCoordinates(45, 180));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapViewportProjection firstProjection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, firstObserver);
            SkyMapViewportProjection laterProjection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, laterObserver);
            SkyMapScene first = sut.Build(firstProjection, SkyMapRenderOptions.HorizontalGrid);
            SkyMapScene later = sut.Build(laterProjection, SkyMapRenderOptions.HorizontalGrid);

            SkyMapLabel[] labels = VisibleGridLabels(first, viewport);
            first.GridLines.Should().NotBeEmpty();
            labels.Should().NotBeEmpty();
            labels.Should().OnlyContain(x => x.Text.EndsWith("°", StringComparison.Ordinal));
            later.GridLines.SelectMany(x => x.Points).Should().NotEqual(first.GridLines.SelectMany(x => x.Points));
        }

        [TestCase(85, 0)]
        [TestCase(359, 45)]
        [TestCase(170, -60)]
        public void Build_AltAzGrid_ForArbitraryCelestialViewport_RemainsVisible(double rightAscension, double declination) {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            ViewportFoV viewport = new ViewportFoV(CelestialCoordinates(rightAscension, declination), 30, 1200, 800, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapScene scene = sut.Build(projection, SkyMapRenderOptions.HorizontalGrid);

            scene.GridLines.Should().NotBeEmpty();
            VisibleGridLabels(scene, viewport).Should().NotBeEmpty();
        }

        [Test]
        public void AltAzProjection_HorizontalDragMovesAlongAzimuthGrid() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, 180));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 0);
            SkyMapViewportProjection sut = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);

            Coordinates shiftedCenter = sut.ShiftCenter(new Vector(20, 0));
            SkyMapHorizontalCoordinates shifted = observer.ToHorizontal(shiftedCenter);
            Point sameAltitude = sut.Project(observer.ToCelestial(new SkyMapHorizontalCoordinates(35, 182)));

            shifted.Altitude.Should().BeApproximately(35, 0.01);
            shifted.Azimuth.Should().BeGreaterThan(180);
            sameAltitude.Y.Should().BeApproximately(viewport.ViewPortCenterPoint.Y, 1);
        }

        [Test]
        public void EquatorialProjection_MatchesEstablishedViewportProjectionAndShift() {
            ViewportFoV viewport = new ViewportFoV(CelestialCoordinates(85, 20), 30, 1200, 800, 17);
            Coordinates target = CelestialCoordinates(91, 24);
            Vector delta = new Vector(25, -40);
            SkyMapViewportProjection sut = new SkyMapViewportProjection(viewport);
            ViewportFoV expectedShift = new ViewportFoV(
                viewport.CenterCoordinates,
                viewport.VFoV,
                viewport.Width,
                viewport.Height,
                viewport.Rotation);

            Point projected = sut.Project(target);
            expectedShift.Shift(delta);
            Coordinates shifted = sut.ShiftCenter(delta);

            Point expectedProjection = target.XYProjection(viewport);
            projected.X.Should().BeApproximately(expectedProjection.X, 1E-9);
            projected.Y.Should().BeApproximately(expectedProjection.Y, 1E-9);
            shifted.RADegrees.Should().BeApproximately(expectedShift.CenterCoordinates.RADegrees, 1E-10);
            shifted.Dec.Should().BeApproximately(expectedShift.CenterCoordinates.Dec, 1E-10);
        }

        [TestCase(SkyMapProjectionMode.Equatorial)]
        [TestCase(SkyMapProjectionMode.AltAz)]
        public void ViewportProjection_ProjectAndUnproject_RoundTripsAtRotation(SkyMapProjectionMode mode) {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, at, 16.5);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(30, 180));
            Coordinates expected = observer.ToCelestial(new SkyMapHorizontalCoordinates(15, 145));
            ViewportFoV viewport = new ViewportFoV(center, 100, 1200, 800, 37);
            SkyMapViewportProjection sut = new SkyMapViewportProjection(viewport, mode, observer);

            Coordinates actual = sut.Unproject(sut.Project(expected));

            double rightAscensionDifference = AstroUtil.EuclidianModulus(
                actual.RADegrees - expected.RADegrees + 180,
                360) - 180;
            rightAscensionDifference.Should().BeApproximately(0, 1E-9);
            actual.Dec.Should().BeApproximately(expected.Dec, 1E-9);
        }

        [TestCase(50, 45, 180, 38, 205, 11)]
        [TestCase(50, 45, 180, 38, 155, 11)]
        [TestCase(50, 70, 350, 60, 10, 0)]
        [TestCase(-33, 45, 0, 30, 25, 27)]
        [TestCase(-33, 45, 0, 30, 335, 27)]
        public void CameraRectanglePlacement_AltAz_AlignsCenterAndCameraAxis(
            double latitude,
            double centerAltitude,
            double centerAzimuth,
            double rectangleAltitude,
            double rectangleAzimuth,
            double viewportRotation) {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(latitude, 10, at);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(centerAltitude, centerAzimuth));
            Coordinates rectangleCoordinates = observer.ToCelestial(new SkyMapHorizontalCoordinates(rectangleAltitude, rectangleAzimuth));
            ViewportFoV viewport = new ViewportFoV(center, 40, 800, 600, viewportRotation);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            FramingRectangle rectangle = new FramingRectangle(0, 0, 0, 320, 180) {
                Coordinates = rectangleCoordinates,
                Id = 3
            };
            const double positionAngle = 343;
            SkyMapCameraRectanglePlacement sut = new SkyMapCameraRectanglePlacement(rectangle);

            sut.Update(projection, positionAngle);

            Point expectedCenter = projection.Project(rectangleCoordinates);
            double expectedRotation = AstroUtil.EuclidianModulus(
                projection.RotationForPositionAngle(rectangleCoordinates, positionAngle, expectedCenter) + 90,
                360);
            sut.X.Should().BeApproximately(expectedCenter.X - rectangle.Width / 2, 1E-9);
            sut.Y.Should().BeApproximately(expectedCenter.Y - rectangle.Height / 2, 1E-9);
            sut.Rotation.Should().BeApproximately(expectedRotation, 0.05);
            sut.Width.Should().Be(rectangle.Width);
            sut.Height.Should().Be(rectangle.Height);
            sut.Id.Should().Be(rectangle.Id);
        }

        [Test]
        public void CameraRectanglePlacement_Equatorial_PreservesEstablishedOverlayRotation() {
            Coordinates center = CelestialCoordinates(85, 20);
            ViewportFoV viewport = new ViewportFoV(center, 30, 1200, 800, 13);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport);
            FramingRectangle rectangle = new FramingRectangle(13, 0, 0, 320, 180) {
                Coordinates = center,
                Rotation = 34
            };
            SkyMapCameraRectanglePlacement sut = new SkyMapCameraRectanglePlacement(rectangle);

            sut.Update(projection, AstroUtil.EuclidianModulus(360 - rectangle.TotalRotation, 360));

            sut.X.Should().BeApproximately(viewport.ViewPortCenterPoint.X - rectangle.Width / 2, 1E-9);
            sut.Y.Should().BeApproximately(viewport.ViewPortCenterPoint.Y - rectangle.Height / 2, 1E-9);
            sut.Rotation.Should().BeApproximately(rectangle.Rotation, 0.05);
        }

        [Test]
        public void CameraRectanglePlacement_ObserverRefresh_ReprojectsExistingPlacement() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            Coordinates rectangleCoordinates = CelestialCoordinates(10.68470833, 41.26875);
            SkyMapObserverSnapshot firstObserver = new SkyMapObserverSnapshot(50, 10, at);
            SkyMapObserverSnapshot laterObserver = new SkyMapObserverSnapshot(50, 10, at.AddMinutes(1));
            Coordinates center = firstObserver.ToCelestial(new SkyMapHorizontalCoordinates(45, 180));
            ViewportFoV viewport = new ViewportFoV(center, 40, 800, 600, 0);
            SkyMapViewportProjection firstProjection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, firstObserver);
            SkyMapViewportProjection laterProjection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, laterObserver);
            FramingRectangle rectangle = new FramingRectangle(0, 0, 0, 320, 180) {
                Coordinates = rectangleCoordinates
            };
            SkyMapCameraRectanglePlacement sut = new SkyMapCameraRectanglePlacement(rectangle);

            sut.Update(firstProjection, 35);
            double firstX = sut.X;
            double firstRotation = sut.Rotation;
            double firstInverseRotation = sut.InverseRotation;
            sut.Update(laterProjection, 35);

            Point expectedCenter = laterProjection.Project(rectangleCoordinates);
            sut.X.Should().BeApproximately(expectedCenter.X - rectangle.Width / 2, 1E-9);
            sut.Y.Should().BeApproximately(expectedCenter.Y - rectangle.Height / 2, 1E-9);
            sut.X.Should().NotBeApproximately(firstX, 0.01);
            sut.Rotation.Should().NotBeApproximately(firstRotation, 0.01);
            sut.InverseRotation.Should().NotBeApproximately(firstInverseRotation, 0.01);
            (sut.Rotation + sut.InverseRotation).Should().BeApproximately(0, 1E-10);
        }

        [Test]
        public void CameraRectanglePlacement_ScrollRecalculation_ReusesPresentationObject() {
            FramingRectangle first = new FramingRectangle(0, 10, 20, 320, 180) { Id = 1 };
            FramingRectangle recalculated = new FramingRectangle(0, 30, 40, 640, 360) { Id = 2 };
            SkyMapCameraRectanglePlacement sut = new SkyMapCameraRectanglePlacement(first);

            sut.SetRectangle(recalculated);
            sut.Update(recalculated.X, recalculated.Y, recalculated.Rotation);

            sut.Rectangle.Should().BeSameAs(recalculated);
            sut.X.Should().Be(recalculated.X);
            sut.Y.Should().Be(recalculated.Y);
            sut.Width.Should().Be(recalculated.Width);
            sut.Height.Should().Be(recalculated.Height);
            sut.Id.Should().Be(recalculated.Id);
        }

        [Test]
        public void CameraRectanglePlacement_ProjectionBeforeCoordinates_DoesNotThrowOrMove() {
            FramingRectangle rectangle = new FramingRectangle(0, 10, 20, 320, 180);
            SkyMapCameraRectanglePlacement sut = new SkyMapCameraRectanglePlacement(rectangle);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(
                new ViewportFoV(CelestialCoordinates(0, 0), 6, 1200, 800, 0));

            Action act = () => sut.Update(projection, 0);

            act.Should().NotThrow();
            sut.X.Should().Be(rectangle.X);
            sut.Y.Should().Be(rectangle.Y);
            sut.Rotation.Should().Be(rectangle.Rotation);
        }

        [Test]
        public void CameraRectanglePlacement_InverseRotation_CancelsAltAzDisplayRotation() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(45, 180));
            Coordinates rectangleCoordinates = observer.ToCelestial(new SkyMapHorizontalCoordinates(38, 205));
            const double viewportRotation = 11;
            ViewportFoV viewport = new ViewportFoV(center, 40, 800, 600, viewportRotation);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            FramingRectangle rectangle = new FramingRectangle(viewportRotation, 0, 0, 320, 180) {
                Coordinates = rectangleCoordinates,
                Rotation = 34
            };
            SkyMapCameraRectanglePlacement sut = new SkyMapCameraRectanglePlacement(rectangle);

            sut.Update(projection, AstroUtil.EuclidianModulus(360 - rectangle.TotalRotation, 360));

            double projectedResidual = AstroUtil.EuclidianModulus(sut.Rotation + sut.InverseRotation + 180, 360) - 180;
            projectedResidual.Should().BeApproximately(0, 1E-10);
        }

        [Test]
        public void Build_AltAzMode_ProjectsSkyLayersAgainstHorizontalGrid() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            Coordinates left = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, 178));
            Coordinates right = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, 182));
            Constellation constellation = new Constellation("Horizontal") {
                Stars = [new Star(1, "Left", left, 2), new Star(2, "Right", right, 2)]
            };
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, 180));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([constellation], [], []);

            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapScene scene = sut.Build(
                projection,
                SkyMapRenderOptions.Stars | SkyMapRenderOptions.HorizontalGrid);

            scene.Stars.Should().HaveCount(2);
            scene.Stars.Single(x => x.Name == "Left").Center.X.Should().BeLessThan(viewport.ViewPortCenterPoint.X);
            scene.Stars.Single(x => x.Name == "Right").Center.X.Should().BeGreaterThan(viewport.ViewPortCenterPoint.X);
            scene.Stars[0].Center.Y.Should().BeApproximately(scene.Stars[1].Center.Y, 1);
        }

        [Test]
        public void Build_HorizonEnabled_DrawsHorizonAndHidesEveryLayerBelowIt() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at, _ => 5);
            Coordinates above1 = observer.ToCelestial(new SkyMapHorizontalCoordinates(10, 175));
            Coordinates above2 = observer.ToCelestial(new SkyMapHorizontalCoordinates(10, 185));
            Coordinates below = observer.ToCelestial(new SkyMapHorizontalCoordinates(0, 180));
            Star visibleStar = new Star(1, "Visible", above1, 2);
            Star hiddenStar = new Star(2, "Hidden", below, 2);
            Constellation constellation = new Constellation("Test") { Stars = [visibleStar, hiddenStar] };
            constellation.StarConnections.Add(Tuple.Create(visibleStar, hiddenStar));
            DeepSkyObject visibleDso = new DeepSkyObject("Visible DSO", above2, null) { Size = 3600 };
            DeepSkyObject hiddenDso = new DeepSkyObject("Hidden DSO", below, null) { Size = 3600 };
            ConstellationBoundary boundary = new ConstellationBoundary {
                Name = "Test",
                Boundaries = [above1, above2, below]
            };
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(5, 180));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([constellation], [visibleDso, hiddenDso], [boundary]);

            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.Equatorial, observer);
            SkyMapScene withoutHorizon = sut.Build(viewport, SkyMapRenderOptions.All);
            SkyMapScene scene = sut.Build(projection, SkyMapRenderOptions.All | SkyMapRenderOptions.Horizon);

            withoutHorizon.HorizonLines.Should().BeEmpty();
            withoutHorizon.HorizonMaskAreas.Should().BeEmpty();
            withoutHorizon.Stars.Should().HaveCount(2);
            withoutHorizon.DeepSkyObjects.Should().HaveCount(2);
            scene.HorizonLines.Should().NotBeEmpty();
            scene.HorizonMaskAreas.Should().NotBeEmpty();
            scene.Stars.Should().ContainSingle(x => x.Id == visibleStar.Id);
            scene.ConstellationLines.Should().BeEmpty();
            scene.DeepSkyObjects.Should().ContainSingle(x => x.Id == visibleDso.Id);
            scene.ConstellationBoundaries.Should().ContainSingle();
            scene.ConstellationBoundaries.Single().Points.Should().HaveCount(2);
        }

        [Test]
        public void Build_HorizonBelowEntireViewport_MasksCompleteViewport() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at, _ => 5);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(-20, 180));
            ViewportFoV viewport = new ViewportFoV(center, 10, 100, 100, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapScene scene = sut.Build(projection, SkyMapRenderOptions.Horizon);

            scene.HorizonLines.Should().BeEmpty();
            scene.HorizonMaskAreas.Should().ContainSingle();
            scene.HorizonMaskAreas.Single().Points.Should().Equal(
                new Point(0, 0),
                new Point(100, 0),
                new Point(100, 100),
                new Point(0, 100));
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

        [TestCase(4, 37, 0)]
        [TestCase(17, 37, 23)]
        [TestCase(10, 48, 71)]
        public void Build_AltAzMode_KeepsM110OutsideM31EllipseWhenViewportIsPanned(
            double viewportRightAscension,
            double viewportDeclination,
            double viewportRotation) {
            DeepSkyObject m31 = new DeepSkyObject("NGC224", CelestialCoordinates(10.68470833, 41.26875), null) {
                DSOType = "GALXY",
                Size = 11340,
                SizeMin = 3660,
                PositionAngle = Angle.ByDegree(35)
            };
            DeepSkyObject m110 = new DeepSkyObject("NGC205", CelestialCoordinates(10.09189356, 41.68541564), null) {
                DSOType = "GALXY",
                Size = 1170,
                SizeMin = 690,
                PositionAngle = Angle.ByDegree(170)
            };
            ViewportFoV viewport = new ViewportFoV(
                CelestialCoordinates(viewportRightAscension, viewportDeclination),
                20,
                1200,
                800,
                viewportRotation);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(
                50,
                new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc),
                16.5);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [m31, m110], []);

            SkyMapScene equatorial = sut.Build(viewport, SkyMapRenderOptions.DeepSkyObjects);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapScene altAz = sut.Build(projection, SkyMapRenderOptions.DeepSkyObjects);
            SkyMapDeepSkyObject equatorialM31 = equatorial.DeepSkyObjects.Single(x => x.Id == m31.Id);
            SkyMapDeepSkyObject equatorialM110 = equatorial.DeepSkyObjects.Single(x => x.Id == m110.Id);
            SkyMapDeepSkyObject altAzM31 = altAz.DeepSkyObjects.Single(x => x.Id == m31.Id);
            SkyMapDeepSkyObject altAzM110 = altAz.DeepSkyObjects.Single(x => x.Id == m110.Id);
            double equatorialDistance = NormalizedEllipseDistance(equatorialM31, equatorialM110.Center);
            double altAzDistance = NormalizedEllipseDistance(altAzM31, altAzM110.Center);

            equatorialDistance.Should().BeGreaterThan(1);
            altAzDistance.Should().BeApproximately(equatorialDistance, 0.01);
            altAzDistance.Should().BeGreaterThan(1);
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
        public void RasterRenderer_WithHorizontallyFlippedCachedImage_MirrorsImagePixels() {
            BitmapSource asymmetric = BitmapSource.Create(
                2,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] {
                    0, 0, 255, 255,
                    255, 0, 0, 255
                },
                8);
            asymmetric.Freeze();
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
                [new SkyMapImagePlacement(asymmetric, new Point(50, 50), 40, 20, 0, FlipHorizontally: true)],
                null);
            byte[] frame = Render(image);

            byte[] left = PixelAt(frame, 100, 40, 50);
            byte[] right = PixelAt(frame, 100, 60, 50);
            left[0].Should().BeGreaterThan(left[2]);
            right[2].Should().BeGreaterThan(right[0]);
            left[3].Should().Be(255);
            right[3].Should().Be(255);
        }

        [Test]
        public void RasterRenderer_HorizonMask_HidesCachedImagesBelowHorizon() {
            BitmapSource red = CreatePixel(0, 0, 255);
            SkyMapPath hiddenHalf = new SkyMapPath(
                [new Point(0, 50), new Point(100, 50), new Point(100, 100), new Point(0, 100)],
                closed: true);
            SkyMapScene scene = new SkyMapScene(
                [],
                [],
                [new SkyMapDeepSkyObject("D", "D", "BRTNB", new Point(50, 45), 20, 20, 0)],
                [],
                [],
                [],
                [],
                [hiddenHalf]);
            using SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(50, 50), 100, 100, 0)],
                null);
            byte[] frame = Render(image);

            PixelAt(frame, 100, 10, 25).Should().Equal(0, 0, 255, 255);
            PixelAt(frame, 100, 10, 75).Should().Equal(0, 0, 0, 255);
            PixelAt(frame, 100, 50, 55).Should().Equal(0, 0, 0, 255);
            PixelAt(frame, 100, 50, 75).Should().Equal(0, 0, 0, 255);
            PixelAt(frame, 100, 90, 75).Should().Equal(0, 0, 0, 255);
        }

        [Test]
        public void HorizonPipeline_HidesCachedSkyBelowLocalHorizon() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(0, 180));
            ViewportFoV viewport = new ViewportFoV(center, 20, 100, 100, 0);
            SkyMapSceneBuilder builder = new SkyMapSceneBuilder([], [], []);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapScene scene = builder.Build(projection, SkyMapRenderOptions.Horizon);
            BitmapSource red = CreatePixel(0, 0, 255);
            using SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(50, 50), 100, 100, 0)],
                null);
            byte[] frame = Render(image);

            PixelAt(frame, 100, 50, 25).Should().Equal(0, 0, 255, 255);
            PixelAt(frame, 100, 10, 75).Should().Equal(0, 0, 0, 255);
            PixelAt(frame, 100, 50, 75).Should().Equal(0, 0, 0, 255);
            PixelAt(frame, 100, 90, 75).Should().Equal(0, 0, 0, 255);
        }

        [TestCase(30, 0, 0)]
        [TestCase(-30, 0, 0)]
        [TestCase(30, 37, 0)]
        [TestCase(-30, 37, 0)]
        [TestCase(30, 37, 15)]
        public void HorizonPipeline_WhenZoomedOut_MasksOnlyCoordinatesBelowHorizon(
            double centerAltitude,
            double rotation,
            double customHorizonAmplitude) {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            Func<double, double> customHorizon = customHorizonAmplitude == 0
                ? null
                : azimuth => customHorizonAmplitude * Math.Sin(AstroUtil.ToRadians(azimuth * 2));
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, at, 16.5, customHorizon);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(centerAltitude, 180));
            ViewportFoV viewport = new ViewportFoV(center, 140, 100, 100, rotation);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapSceneBuilder builder = new SkyMapSceneBuilder([], [], []);
            SkyMapScene scene = builder.Build(projection, SkyMapRenderOptions.Horizon);
            BitmapSource red = CreatePixel(0, 0, 255);
            using SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(50, 50), 100, 100, 0)],
                null);
            byte[] frame = Render(image);
            int visibleSamples = 0;
            int hiddenSamples = 0;

            foreach (double altitude in new[] { -60d, -30d, -10d, 10d, 30d, 60d }) {
                for (double azimuth = 0; azimuth < 360; azimuth += 15) {
                    Coordinates coordinates = observer.ToCelestial(new SkyMapHorizontalCoordinates(altitude, azimuth));
                    if (!projection.Contains(coordinates)) {
                        continue;
                    }
                    Point projected = projection.Project(coordinates);
                    int x = (int)Math.Round(projected.X);
                    int y = (int)Math.Round(projected.Y);
                    if (x < 2 || x >= 98 || y < 2 || y >= 98) {
                        continue;
                    }

                    double clearance = observer.HorizonClearance(coordinates);
                    if (Math.Abs(clearance) < 8) {
                        continue;
                    }
                    if (clearance >= 0) {
                        PixelAt(frame, 100, x, y).Should().Equal(
                            new byte[] { 0, 0, 255, 255 },
                            $"altitude {altitude}°, azimuth {azimuth}° is above the configured horizon");
                        visibleSamples++;
                    } else {
                        PixelAt(frame, 100, x, y).Should().Equal(
                            new byte[] { 0, 0, 0, 255 },
                            $"altitude {altitude}°, azimuth {azimuth}° is below the configured horizon");
                        hiddenSamples++;
                    }
                }
            }

            visibleSamples.Should().BeGreaterThan(0);
            hiddenSamples.Should().BeGreaterThan(0);
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

        private static double NormalizedEllipseDistance(SkyMapDeepSkyObject ellipse, Point point) {
            double angle = AstroUtil.ToRadians(ellipse.PositionAngle);
            double deltaX = point.X - ellipse.Center.X;
            double deltaY = point.Y - ellipse.Center.Y;
            double localX = deltaX * Math.Cos(angle) + deltaY * Math.Sin(angle);
            double localY = -deltaX * Math.Sin(angle) + deltaY * Math.Cos(angle);
            return localX * localX / (ellipse.RadiusX * ellipse.RadiusX)
                + localY * localY / (ellipse.RadiusY * ellipse.RadiusY);
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