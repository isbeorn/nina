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
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    [NonParallelizable]
    public class NOVASConcurrencyTest {
        private const int Iterations = 10000;
        private const double AltitudeTolerance = 1e-10;
        private static readonly DateTime ObservationTime = new DateTime(2026, 8, 23, 22, 0, 0, DateTimeKind.Utc);

        public enum CompetitorCall {
            PlanetApparentCoordinates,
            BodyPositionAndVelocity,
            GeoPositionAndVelocity,
            OrbitalsPattern
        }

        [Test]
        public void Place_NonzeroResult_InvalidatesPositionBeforeItCanBecomeAltitude() {
            NOVAS.CelestialObject earth = new NOVAS.CelestialObject {
                Type = (short)NOVAS.ObjectType.MajorPlanetSunOrMoon,
                Number = (short)NOVAS.Body.Earth,
                Name = "Earth",
                Star = new NOVAS.CatalogueEntry()
            };
            NOVAS.Observer observer = new NOVAS.Observer {
                Where = (short)NOVAS.ObserverLocation.EarthSurface,
                OnSurf = new NOVAS.OnSurface { Latitude = 47.0, Longitude = 11.0, Height = 650.0 }
            };
            NOVAS.SkyPosition position = new NOVAS.SkyPosition {
                RHat = new[] { 1.0, 2.0, 3.0 }, RA = 4.0, Dec = 5.0, Dis = 6.0, RV = 7.0
            };

            short result = NOVAS.Place(
                AstroUtil.GetJulianDateTT(ObservationTime), earth, observer, AstroUtil.DeltaT(ObservationTime),
                NOVAS.CoordinateSystem.EquinoxOfDate, NOVAS.Accuracy.Full, ref position);
            double altitude = AstroUtil.GetAltitude(
                AstroUtil.HoursToDegrees(AstroUtil.GetHourAngle(0.0, position.RA)),
                observer.OnSurf.Latitude,
                position.Dec);

            result.Should().NotBe(0);
            new[] { position.RA, position.Dec, position.Dis, position.RV, altitude }
                .Should().OnlyContain(value => double.IsNaN(value));
            position.RHat.Should().OnlyContain(value => double.IsNaN(value));
        }

        [Test]
        [CancelAfter(60000)]
        [TestCase(CompetitorCall.PlanetApparentCoordinates)]
        [TestCase(CompetitorCall.BodyPositionAndVelocity)]
        [TestCase(CompetitorCall.GeoPositionAndVelocity)]
        [TestCase(CompetitorCall.OrbitalsPattern)]
        public void SunAndMoonAltitude_ConcurrentNOVASCalls_RemainStable(CompetitorCall competitorCall) {
            var observer = new ObserverInfo { Latitude = 47.0, Longitude = 11.0, Elevation = 650.0 };
            double sunBaseline = AstroUtil.GetSunAltitude(ObservationTime, observer);
            double moonBaseline = AstroUtil.GetMoonAltitude(ObservationTime, observer);
            double[] julianDates = Enumerable.Range(0, 240)
                .Select(i => AstroUtil.GetJulianDateTT(ObservationTime.Date.AddMinutes(i * 6)))
                .ToArray();
            var sunAltitudes = new ConcurrentBag<double>();
            var moonAltitudes = new ConcurrentBag<double>();
            using var start = new Barrier(3);

            Task sunTask = Task.Run(() => SampleAltitudes(
                () => AstroUtil.GetSunAltitude(ObservationTime, observer), sunAltitudes, start));
            Task moonTask = Task.Run(() => SampleAltitudes(
                () => AstroUtil.GetMoonAltitude(ObservationTime, observer), moonAltitudes, start));
            Task competitorTask = Task.Run(() => {
                start.SignalAndWait();
                for (int i = 0; i < Iterations; i++) {
                    RunCompetitor(competitorCall, julianDates[i % julianDates.Length]);
                }
            });

            Task.WaitAll(sunTask, moonTask, competitorTask);

            sunBaseline.Should().BeApproximately(-29.22203484260581, 1e-3);
            AssertStable(sunAltitudes, sunBaseline);
            AssertStable(moonAltitudes, moonBaseline);
            sunAltitudes.Should().OnlyContain(altitude => altitude <= -28.5);
        }

        private static void SampleAltitudes(Func<double> calculate, ConcurrentBag<double> samples, Barrier start) {
            start.SignalAndWait();
            for (int i = 0; i < Iterations; i++) {
                samples.Add(calculate());
            }
        }

        private static void RunCompetitor(CompetitorCall competitorCall, double varyingJulianDate) {
            if (competitorCall == CompetitorCall.PlanetApparentCoordinates) {
                NOVAS.PlanetApparentCoordinates(varyingJulianDate, NOVAS.Body.Mars);
            } else if (competitorCall == CompetitorCall.BodyPositionAndVelocity) {
                NOVAS.BodyPositionAndVelocity(varyingJulianDate, NOVAS.Body.Mars, NOVAS.SolarSystemOrigin.Barycenter);
            } else if (competitorCall == CompetitorCall.GeoPositionAndVelocity) {
                var observer = new NOVAS.Observer {
                    Where = (short)NOVAS.ObserverLocation.EarthSurface,
                    OnSurf = new NOVAS.OnSurface { Latitude = 47.0, Longitude = 11.0, Height = 650.0 }
                };
                short result = NOVAS.NOVAS_geo_posvel(
                    varyingJulianDate, AstroUtil.DeltaT(ObservationTime), NOVAS.Accuracy.Full, observer,
                    new double[3], new double[3]);
                if (result != 0) {
                    throw new InvalidOperationException($"NOVAS geo_posvel failed with result {result}");
                }
            } else {
                double jd = AstroUtil.GetJulianDate(ObservationTime);
                NOVAS.BodyPositionAndVelocity(jd, NOVAS.Body.Mars, NOVAS.SolarSystemOrigin.SolarCenterOfMass);
                NOVAS.BodyPositionAndVelocity(jd, NOVAS.Body.Earth, NOVAS.SolarSystemOrigin.SolarCenterOfMass);
                NOVAS.PlanetApparentCoordinates(jd, NOVAS.Body.Mars);
                NOVAS.PlanetApparentCoordinates(jd + 1.0 / 86400.0, NOVAS.Body.Mars);
            }
        }

        private static void AssertStable(ConcurrentBag<double> samples, double baseline) {
            samples.Should().HaveCount(Iterations);
            samples.Should().OnlyContain(altitude =>
                double.IsFinite(altitude) && Math.Abs(altitude - baseline) <= AltitudeTolerance);
        }
    }
}
