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
using System.Threading;
using System.Windows;

namespace NINA.Test.SkySurvey {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class SkyMapCameraRectanglePlacementTest {
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


        private static Coordinates CelestialCoordinates(double rightAscension, double declination) {
            return new Coordinates(rightAscension, declination, Epoch.J2000, Coordinates.RAType.Degrees);
        }
    }
}



