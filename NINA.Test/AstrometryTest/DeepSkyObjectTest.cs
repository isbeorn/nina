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
using NINA.Core.Model;
using OxyPlot.Axes;
using System;
using System.IO;
using System.Linq;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class DeepSkyObjectTest {
        private static readonly DateTime ReferenceDate = new DateTime(2024, 3, 25, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Refresh_WithAndWithoutCustomHorizon_PreservesAltitudeCurveAndTransit() {
            using var horizonReader = new StringReader("0 0\n360 0");
            var flatHorizon = CustomHorizon.FromReader_Standard(horizonReader);
            var coordinates = new Coordinates(Angle.ByHours(5), Angle.ByDegree(30), Epoch.J2000);
            var withoutHorizon = new DeepSkyObject("without", coordinates, null);
            var withHorizon = new DeepSkyObject("with", coordinates, flatHorizon);
            withoutHorizon.SetDateAndPosition(ReferenceDate, 51.5, 13);
            withHorizon.SetDateAndPosition(ReferenceDate, 51.5, 13);

            withoutHorizon.Refresh();
            withHorizon.Refresh();

            withoutHorizon.Altitudes.Should().Equal(withHorizon.Altitudes);
            withoutHorizon.MaxAltitude.Should().Be(withHorizon.MaxAltitude);
            withoutHorizon.DoesTransitSouth.Should().Be(withHorizon.DoesTransitSouth);
            withoutHorizon.Horizon.Should().BeEmpty();
            withHorizon.Horizon.Should().HaveCount(withHorizon.Altitudes.Count)
                .And.OnlyContain(point => point.Y == 0);
        }

        [Test]
        public void SetCustomHorizon_AndTargetUpdates_PreserveAltitudeCurveAndTransit() {
            using var horizonReader = new StringReader("0 4\n90 12\n180 28\n270 8\n360 4");
            var horizon = CustomHorizon.FromReader_Standard(horizonReader);
            var firstCoordinates = new Coordinates(Angle.ByHours(5), Angle.ByDegree(30), Epoch.J2000);
            var secondCoordinates = new Coordinates(Angle.ByHours(17), Angle.ByDegree(-25), Epoch.J2000);
            var sut = new DeepSkyObject("target", firstCoordinates, null);

            var states = new[] {
                (Coordinates: firstCoordinates, Date: ReferenceDate, Latitude: 51.5, Longitude: 13.0, South: true),
                (Coordinates: secondCoordinates, Date: ReferenceDate.AddDays(30), Latitude: -33.0, Longitude: 151.0, South: false),
                (Coordinates: firstCoordinates, Date: ReferenceDate, Latitude: 51.5, Longitude: 13.0, South: true)
            };

            foreach (var state in states) {
                sut.SetDateAndPosition(state.Date, state.Latitude, state.Longitude);
                sut.Coordinates = state.Coordinates;
                var altitudes = sut.Altitudes.ToArray();
                var maximum = sut.MaxAltitude;
                altitudes.Length.Should().BeInRange(240, 241);
                altitudes[0].X.Should().Be(DateTimeAxis.ToDouble(state.Date));
                var hourAngle = AstroUtil.GetHourAngle(AstroUtil.GetLocalSiderealTime(state.Date, state.Longitude), state.Coordinates.RA);
                altitudes[0].Y.Should().Be(AstroUtil.GetAltitude(AstroUtil.HoursToDegrees(hourAngle), state.Latitude, state.Coordinates.Dec));
                maximum.Y.Should().Be(altitudes.Max(point => point.Y));
                sut.DoesTransitSouth.Should().Be(state.South);
                sut.Horizon.Should().BeEmpty();

                sut.SetCustomHorizon(horizon);

                sut.Altitudes.Should().Equal(altitudes);
                sut.MaxAltitude.Should().Be(maximum);
                sut.DoesTransitSouth.Should().Be(state.South);
                sut.Horizon.Should().HaveCount(altitudes.Length);
                var angle = hourAngle;
                for (int i = 0; i < altitudes.Length; i++, angle += 0.1) {
                    var azimuth = AstroUtil.GetAzimuth(AstroUtil.HoursToDegrees(angle), altitudes[i].Y,
                        state.Latitude, state.Coordinates.Dec);
                    sut.Horizon[i].X.Should().Be(altitudes[i].X);
                    sut.Horizon[i].Y.Should().Be(horizon.GetAltitude(azimuth));
                }

                sut.SetCustomHorizon(null);

                sut.Horizon.Should().BeEmpty();
                sut.Altitudes.Should().Equal(altitudes);
                sut.MaxAltitude.Should().Be(maximum);
                sut.DoesTransitSouth.Should().Be(state.South);
            }
        }

    }
}