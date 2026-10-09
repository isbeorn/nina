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
using System.Windows;

namespace NINA.Test.SkySurvey {

    [TestFixture]
    public class SkyMapViewportProjectionTest {

        [TestCase(0)]
        [TestCase(180)]
        [TestCase(359)]
        public void AltAzProjection_IncreasingAzimuthMovesRight(double centerAzimuth) {
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(
                50,
                10,
                new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc));
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, centerAzimuth));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 0);
            SkyMapViewportProjection sut = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            Coordinates lowerAzimuth = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, centerAzimuth - 2));
            Coordinates higherAzimuth = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, centerAzimuth + 2));

            Point lower = sut.Project(lowerAzimuth);
            Point higher = sut.Project(higherAzimuth);

            lower.X.Should().BeLessThan(viewport.ViewPortCenterPoint.X);
            higher.X.Should().BeGreaterThan(viewport.ViewPortCenterPoint.X);
            lower.Y.Should().BeApproximately(higher.Y, 1);
        }

        [TestCase(0)]
        [TestCase(37)]
        [TestCase(281)]
        public void AltAzProjection_ShiftCenterFollowsRotatedScreenAxes(double rotation) {
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(
                50,
                10,
                new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc));
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, 180));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, rotation);
            SkyMapViewportProjection sut = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            Vector delta = new Vector(23, -17);

            Coordinates shiftedCenter = sut.ShiftCenter(delta);

            Point projected = sut.Project(shiftedCenter);
            projected.X.Should().BeApproximately(viewport.ViewPortCenterPoint.X + delta.X, 1E-9);
            projected.Y.Should().BeApproximately(viewport.ViewPortCenterPoint.Y + delta.Y, 1E-9);
        }

        [Test]
        public void AltAzProjection_SouthMeridianImageTracksCatalogNorthAndKeepsEastLeft() {
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(
                50,
                10,
                new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc));
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(35, 180));
            ViewportFoV viewport = new ViewportFoV(center, 40, 1200, 800, 0);
            SkyMapViewportProjection sut = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);

            (double rotation, bool flipHorizontally) = sut.ImageTransformFromEquatorial(
                center,
                equatorialRotation: 0,
                viewport.ViewPortCenterPoint);

            double normalizedRotation = AstroUtil.EuclidianModulus(rotation + 180, 360) - 180;
            // J2000 north is tilted relative to north of the observation date. Obtain its
            // direction through uncached SOFA transforms instead of assuming zero rotation.
            var (tt1, tt2) = AstroUtil.GetJulianDateTTParts(observer.Timestamp);
            double ri = 0, di = 0, eo = 0;
            SOFA.CelestialToIntermediate(AstroUtil.ToRadians(center.RADegrees), AstroUtil.ToRadians(center.Dec + 0.01),
                0, 0, 0, 0, tt1, tt2, ref ri, ref di, ref eo);
            double azimuth = 0, altitude = 0;
            var lst = AstroUtil.ToRadians(15 * AstroUtil.GetLocalSiderealTime(observer.Timestamp, 10));
            SOFA.Hd2ae(lst - SOFA.Anp(ri - eo), di, AstroUtil.ToRadians(50), ref azimuth, ref altitude);
            double azimuthDifference = azimuth - Math.PI;
            double north = Math.Cos(AstroUtil.ToRadians(35)) * Math.Sin(altitude)
                - Math.Sin(AstroUtil.ToRadians(35)) * Math.Cos(altitude) * Math.Cos(azimuthDifference);
            double east = Math.Sin(azimuthDifference) * Math.Cos(altitude);
            double expectedRotation = AstroUtil.ToDegree(Math.Atan2(east / viewport.ArcSecWidth, north / viewport.ArcSecHeight));
            normalizedRotation.Should().BeApproximately(expectedRotation, 1e-7);
            flipHorizontally.Should().BeFalse();
        }
    }
}