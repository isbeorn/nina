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
using NINA.Core.Model;
using NINA.WPF.Base.SkySurvey;
using NUnit.Framework;
using System;
using System.IO;
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
        private static readonly string[] CardinalDirections = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        private const string SharpHorizon = """
            348, 15
            33, 12
            32, 36
            37, 39
            88, 28
            90, 7
            103, 7
            118, 12
            152, 14
            176, 33
            188, 17
            219, 17
            221, 27
            249, 23
            280, 32
            297, 11
            310, 8
            321, 14
            356, 15
            """;

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

            SkyMapScene first = sut.Build(new SkyMapViewportProjection(firstViewport), SkyMapRenderOptions.All);
            SkyMapScene moved = sut.Build(new SkyMapViewportProjection(movedViewport), SkyMapRenderOptions.All);

            first.Stars.Should().HaveCount(2);
            first.ConstellationLines.Should().ContainSingle();
            first.DeepSkyObjects.Should().ContainSingle();
            first.ConstellationBoundaries.Should().ContainSingle();
            first.ConstellationBoundaries[0].Closed.Should().BeTrue();
            first.GridLines.Should().NotBeEmpty();
            first.GridLines.Should().Contain(x => x.StrokeThickness == 3);
            first.Labels.Should().Contain(x => x.Text == firstStar.Name && x.Kind == SkyMapLabelKind.Star);
            first.Labels.Should().Contain(x => x.Text == constellation.Name && x.Kind == SkyMapLabelKind.Constellation);
            first.Labels.Should().Contain(x => x.Kind == SkyMapLabelKind.Grid);
            Point expectedStarPosition = firstStar.Coords.XYProjection(firstViewport);
            first.Stars[0].Center.X.Should().BeApproximately(expectedStarPosition.X, 1E-9);
            first.Stars[0].Center.Y.Should().BeApproximately(expectedStarPosition.Y, 1E-9);
            first.DeepSkyObjects.Single().RadiusX.Should().Be(first.DeepSkyObjects.Single().RadiusY);

            moved.Stars[0].Center.Should().NotBe(first.Stars[0].Center);
            moved.ConstellationLines[0].Start.Should().NotBe(first.ConstellationLines[0].Start);
            moved.DeepSkyObjects.Single().Center.Should().NotBe(first.DeepSkyObjects.Single().Center);
            moved.ConstellationBoundaries[0].Points[0].Should().NotBe(first.ConstellationBoundaries[0].Points[0]);
            moved.GridLines[0].Points[0].Should().NotBe(first.GridLines[0].Points[0]);
        }

        [Test]
        public void Build_SharedConstellationStar_IsRenderedOnce() {
            Star sharedStar = new Star(1, "Shared", CelestialCoordinates(85, 0), 2);
            Constellation first = new Constellation("First") { Stars = [sharedStar] };
            Constellation second = new Constellation("Second") { Stars = [sharedStar] };
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([first, second], [], []);
            ViewportFoV viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);

            SkyMapScene scene = sut.Build(new SkyMapViewportProjection(viewport), SkyMapRenderOptions.Stars);

            scene.Stars.Should().ContainSingle();
        }

        [Test]
        public void Build_EquatorialGrid_AnnotatesVisibleRightAscensionAndDeclinationValues() {
            ViewportFoV viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = sut.Build(new SkyMapViewportProjection(viewport), SkyMapRenderOptions.EquatorialGrid);

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

            SkyMapLabel[] firstLabels = VisibleGridLabels(sut.Build(new SkyMapViewportProjection(firstViewport), SkyMapRenderOptions.EquatorialGrid), firstViewport);
            SkyMapLabel[] movedLabels = VisibleGridLabels(sut.Build(new SkyMapViewportProjection(movedViewport), SkyMapRenderOptions.EquatorialGrid), movedViewport);

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

        [TestCase(0, "N")]
        [TestCase(45, "NE")]
        [TestCase(90, "E")]
        [TestCase(135, "SE")]
        [TestCase(180, "S")]
        [TestCase(225, "SW")]
        [TestCase(270, "W")]
        [TestCase(315, "NW")]
        public void Build_AltAzGrid_AnnotatesVisibleCardinalDirection(double azimuth, string direction) {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(10, azimuth));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 37);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = sut.Build(projection, SkyMapRenderOptions.HorizontalGrid);
            SkyMapLabel[] cardinalDirections = scene.Labels
                .Where(x => x.Kind == SkyMapLabelKind.CardinalDirection)
                .ToArray();

            cardinalDirections.Should().ContainSingle();
            cardinalDirections.Should().ContainSingle(x => x.Text == direction);
            SkyMapLabel label = cardinalDirections.Single(x => x.Text == direction);
            label.Position.X.Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(viewport.Width);
            label.Position.Y.Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(viewport.Height);
            SkyMapHorizontalCoordinates horizontal = projection.UnprojectHorizontal(label.Position);
            horizontal.Altitude.Should().BeApproximately(0, 0.01);
            double azimuthError = Math.Abs(AstroUtil.EuclidianModulus(horizontal.Azimuth - azimuth + 180, 360) - 180);
            azimuthError.Should().BeLessThan(0.01);
        }

        [Test]
        public void Build_AltAzGrid_WhenZeroAltitudeIsOutsideViewport_DoesNotAnnotateCardinalDirection() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, 90));
            ViewportFoV viewport = new ViewportFoV(center, 20, 1200, 800, 0);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = sut.Build(projection, SkyMapRenderOptions.HorizontalGrid);

            scene.Labels.Should().NotContain(x => x.Kind == SkyMapLabelKind.CardinalDirection);
        }

        [Test]
        public void Build_AltAzGrid_WithCustomHorizon_KeepsZeroAltitudeCardinalDirection() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, at, 10, _ => 30);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(10, 90));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 37);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = sut.Build(
                projection,
                SkyMapRenderOptions.HorizontalGrid | SkyMapRenderOptions.Horizon);
            SkyMapLabel direction = scene.Labels.Single(x => x.Text == "E");
            SkyMapHorizontalCoordinates horizontal = projection.UnprojectHorizontal(direction.Position);

            horizontal.Altitude.Should().BeApproximately(0, 0.01);
            observer.HorizonClearance(horizontal).Should().BeLessThan(0);
        }

        [Test]
        public void Build_AltAzGrid_WithFlatHorizon_AnnotatesCardinalDirectionOnHorizon() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(10, 90));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 37);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = sut.Build(
                projection,
                SkyMapRenderOptions.HorizontalGrid | SkyMapRenderOptions.Horizon);
            SkyMapLabel direction = scene.Labels.Single(x => x.Text == "E");
            SkyMapHorizontalCoordinates horizontal = projection.UnprojectHorizontal(direction.Position);

            horizontal.Altitude.Should().BeApproximately(0, 0.01);
            observer.HorizonClearance(horizontal).Should().BeApproximately(0, 0.01);
        }

        [Test]
        public void Build_EquatorialGrid_DoesNotAnnotateCardinalDirections() {
            ViewportFoV viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = sut.Build(new SkyMapViewportProjection(viewport), SkyMapRenderOptions.EquatorialGrid);

            scene.Labels.Should().NotContain(x => CardinalDirections.Contains(x.Text));
            scene.Labels.Should().NotContain(x => x.Kind == SkyMapLabelKind.CardinalDirection);
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
            scene.Stars[0].Center.X.Should().BeLessThan(viewport.ViewPortCenterPoint.X);
            scene.Stars[1].Center.X.Should().BeGreaterThan(viewport.ViewPortCenterPoint.X);
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
            SkyMapScene withoutHorizon = sut.Build(new SkyMapViewportProjection(viewport), SkyMapRenderOptions.All);
            SkyMapScene scene = sut.Build(projection, SkyMapRenderOptions.All | SkyMapRenderOptions.Horizon);

            withoutHorizon.HorizonLines.Should().BeEmpty();
            withoutHorizon.HorizonMaskAreas.Should().BeEmpty();
            withoutHorizon.Stars.Should().HaveCount(2);
            withoutHorizon.DeepSkyObjects.Should().HaveCount(2);
            scene.HorizonLines.Should().NotBeEmpty();
            scene.HorizonMaskAreas.Should().NotBeEmpty();
            scene.Stars.Should().ContainSingle();
            scene.ConstellationLines.Should().BeEmpty();
            scene.DeepSkyObjects.Should().ContainSingle();
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
        public void Build_HorizonBelowViewport_LeavesEntireViewportVisible() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at, _ => 0);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(20, 180));
            ViewportFoV viewport = new ViewportFoV(center, 10, 100, 100, 0);
            SkyMapSceneBuilder sut = new SkyMapSceneBuilder([], [], []);

            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapScene scene = sut.Build(projection, SkyMapRenderOptions.Horizon);

            scene.HorizonLines.Should().BeEmpty();
            scene.HorizonMaskAreas.Should().BeEmpty();
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

            SkyMapDeepSkyObject result = sut.Build(new SkyMapViewportProjection(viewport), SkyMapRenderOptions.DeepSkyObjects).DeepSkyObjects.Single();

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

            SkyMapScene equatorial = sut.Build(new SkyMapViewportProjection(viewport), SkyMapRenderOptions.DeepSkyObjects);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapScene altAz = sut.Build(projection, SkyMapRenderOptions.DeepSkyObjects);
            SkyMapDeepSkyObject equatorialM31 = equatorial.DeepSkyObjects.Single(x => x.Name.Contains("NGC224", StringComparison.Ordinal));
            SkyMapDeepSkyObject equatorialM110 = equatorial.DeepSkyObjects.Single(x => x.Name.Contains("NGC205", StringComparison.Ordinal));
            SkyMapDeepSkyObject altAzM31 = altAz.DeepSkyObjects.Single(x => x.Name.Contains("NGC224", StringComparison.Ordinal));
            SkyMapDeepSkyObject altAzM110 = altAz.DeepSkyObjects.Single(x => x.Name.Contains("NGC205", StringComparison.Ordinal));
            double equatorialDistance = NormalizedEllipseDistance(equatorialM31, equatorialM110.Center);
            double altAzDistance = NormalizedEllipseDistance(altAzM31, altAzM110.Center);

            equatorialDistance.Should().BeGreaterThan(1);
            altAzDistance.Should().BeApproximately(equatorialDistance, 0.01);
            altAzDistance.Should().BeGreaterThan(1);
        }

        [TestCase(SkyMapProjectionMode.Equatorial, 0)]
        [TestCase(SkyMapProjectionMode.Equatorial, 15)]
        [TestCase(SkyMapProjectionMode.AltAz, 0)]
        [TestCase(SkyMapProjectionMode.AltAz, 15)]
        public void Build_WhenZoomedOut_HorizonLineFollowsImageMaskBoundary(
            SkyMapProjectionMode projectionMode,
            double customHorizonAmplitude) {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            Func<double, double> customHorizon = customHorizonAmplitude == 0
                ? null
                : azimuth => customHorizonAmplitude * Math.Sin(AstroUtil.ToRadians(azimuth * 2));
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(
                50,
                at,
                16.5,
                customHorizon);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(30, 180));
            ViewportFoV viewport = new ViewportFoV(center, 140, 1200, 800, 37);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, projectionMode, observer);
            SkyMapSceneBuilder builder = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = builder.Build(projection, SkyMapRenderOptions.Horizon);
            Point[] lineMidpoints = scene.HorizonLines
                .Select(line => new Point(
                    (line.Start.X + line.End.X) / 2,
                    (line.Start.Y + line.End.Y) / 2))
                .Where(point => point.X >= 0 && point.X <= viewport.Width)
                .Where(point => point.Y >= 0 && point.Y <= viewport.Height)
                .ToArray();

            lineMidpoints.Should().NotBeEmpty();
            scene.HorizonMaskAreas.Should().NotBeEmpty();
            lineMidpoints.Max(point => DistanceToMaskBoundary(point, scene.HorizonMaskAreas)).Should().BeLessThan(0.01);
        }

        [TestCase(SkyMapProjectionMode.Equatorial, 31.75, 0)]
        [TestCase(SkyMapProjectionMode.Equatorial, 32.25, 17)]
        [TestCase(SkyMapProjectionMode.AltAz, 32.5, 0)]
        [TestCase(SkyMapProjectionMode.AltAz, 32.75, 37)]
        public void Build_SharpCustomHorizon_RemainsAccurateWhilePanning(
            SkyMapProjectionMode projectionMode,
            double centerAzimuth,
            double rotation) {
            using StringReader reader = new StringReader(SharpHorizon);
            CustomHorizon customHorizon = CustomHorizon.FromReader_Standard(reader);
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, at, 16.5, customHorizon.GetAltitude);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(25, centerAzimuth));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, rotation);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, projectionMode, observer);
            SkyMapSceneBuilder builder = new SkyMapSceneBuilder([], [], []);

            SkyMapScene scene = builder.Build(projection, SkyMapRenderOptions.Horizon);
            Point[] linePoints = scene.HorizonLines
                .SelectMany(line => new[] { line.Start, line.End })
                .ToArray();

            linePoints.Should().NotBeEmpty();
            linePoints.Max(point => Math.Abs(observer.HorizonClearance(projection.UnprojectHorizontal(point))))
                .Should().BeLessThan(0.05);
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

        private static double DistanceToMaskBoundary(Point point, IReadOnlyList<SkyMapPath> maskAreas) {
            return maskAreas.Min(path => Enumerable.Range(0, path.Points.Count)
                .Min(index => DistanceToSegment(point, path.Points[index], path.Points[(index + 1) % path.Points.Count])));
        }

        private static double DistanceToSegment(Point point, Point start, Point end) {
            Vector segment = end - start;
            if (segment.LengthSquared == 0) {
                return (point - start).Length;
            }

            double amount = Math.Clamp(Vector.Multiply(point - start, segment) / segment.LengthSquared, 0, 1);
            Point closest = start + segment * amount;
            return (point - closest).Length;
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
    }
}
