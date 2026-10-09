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
using Moq;
using NINA.Profile.Interfaces;
using NINA.Astrometry;
using NINA.Astrometry.Body;
using NINA.Astrometry.RiseAndSet;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class SunTest {
        /// <summary>
        /// Verifies Sun distance near perihelion and aphelion ranges, using known annual extremes
        /// to catch unit conversion or body-selection mistakes in the NOVAS body wrapper.
        /// Reference: https://www.timeanddate.com/astronomy/perihelion-aphelion-solstice.html
        /// </summary>
        [Test]
        public void SunCalculate_PerihelionAndAphelion_ReturnsExpectedDistanceRanges() {
            Sun perihelion = new Sun(new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc), 0.0, 0.0, 0.0);
            Sun aphelion = new Sun(new DateTime(2024, 7, 5, 0, 0, 0, DateTimeKind.Utc), 0.0, 0.0, 0.0);

            perihelion.Calculate();
            aphelion.Calculate();

            perihelion.Distance.Should().BeInRange(146_000_000.0, 148_500_000.0);
            aphelion.Distance.Should().BeInRange(151_000_000.0, 153_000_000.0);
            aphelion.Distance.Should().BeGreaterThan(perihelion.Distance);
            perihelion.Radius.Should().Be(696342.0);
        }

        [TestCase(90.0, 6)]
        [TestCase(90.0, 12)]
        [TestCase(-90.0, 6)]
        [TestCase(-90.0, 12)]
        public void SunRiseAndSet_PolarSolstice_TerminatesWithoutEvents(double latitude, int month) {
            var events = new GuardedSunRiseAndSet(new DateTime(2026, month, 21, 0, 0, 0, DateTimeKind.Utc), latitude);

            events.Compute().Should().BeFalse();

            events.Rise.Should().BeNull();
            events.Set.Should().BeNull();
        }

        /// <summary>
        /// Event precision is measured against independent evaluation and bisection of the real
        /// solar altitude, including the audit's 23.8-second error at latitude 70 and altitude -18.
        /// This bounds the numerical solver error, not atmospheric or ephemeris uncertainty.
        /// Threshold definitions: https://aa.usno.navy.mil/faq/RST_defs
        /// </summary>
        [TestCaseSource(nameof(SolarEventCases))]
        public void SunRiseAndSet_RealAltitudeCrossings_AreRefinedWithinOneTenthSecond(double latitude, int month, double threshold) {
            var date = new DateTime(2026, month, 21, 12, 0, 0, DateTimeKind.Utc);
            var observer = new ObserverInfo { Latitude = latitude, Longitude = 13.0 };
            var events = new SunCustomRiseAndSet(date, latitude, 13.0, 0.0, threshold);
            events.Compute();

            DateTime? expectedRise = null;
            DateTime? expectedSet = null;
            var left = date;
            var leftAltitude = AstroUtil.GetSunAltitude(left, observer) - threshold;

            // Search the entire historical 26-hour window independently of the reported events.
            // One-minute samples are deliberately finer than the production two-hour windows.
            for (var minute = 1; minute <= 26 * 60; minute++) {
                var right = date.AddMinutes(minute);
                var rightAltitude = AstroUtil.GetSunAltitude(right, observer) - threshold;
                if (leftAltitude * rightAltitude < 0.0) {
                    var crossing = RefineSolarCrossing(left, right, leftAltitude, observer, threshold);
                    if (leftAltitude < 0.0) {
                        expectedRise ??= crossing;
                    } else {
                        expectedSet ??= crossing;
                    }
                }

                // A sample exactly on the threshold must cross on both sides, not merely touch it.
                if (leftAltitude == 0.0 || rightAltitude == 0.0) {
                    foreach (var sample in new[] { left, right }) {
                        if ((sample == left ? leftAltitude : rightAltitude) != 0.0) {
                            continue;
                        }
                        var before = AstroUtil.GetSunAltitude(sample.AddSeconds(-0.1), observer) - threshold;
                        var after = AstroUtil.GetSunAltitude(sample.AddSeconds(0.1), observer) - threshold;
                        if (before < 0.0 && after > 0.0) {
                            expectedRise ??= sample;
                        } else if (before > 0.0 && after < 0.0) {
                            expectedSet ??= sample;
                        }
                    }
                }
                left = right;
                leftAltitude = rightAltitude;
            }

            AssertSolarEvent(events.Rise, expectedRise, "rise");
            AssertSolarEvent(events.Set, expectedSet, "set");

            // Exercise shared samples and extrema through the real nighttime consumer against
            // the same independent scan, rather than only comparing two production solvers.
            var profile = Mock.Of<IProfileService>(x =>
                x.ActiveProfile.AstrometrySettings.Latitude == latitude &&
                x.ActiveProfile.AstrometrySettings.Longitude == 13.0 &&
                x.ActiveProfile.AstrometrySettings.Elevation == 0.0);
            var night = new NighttimeCalculator(profile).Calculate(date);
            night.Ticker.Stop();
            var shared = threshold switch {
                -18.0 => night.TwilightRiseAndSet,
                -12.0 => night.NauticalTwilightRiseAndSet,
                -6.0 => night.CivilTwilightRiseAndSet,
                _ => night.SunRiseAndSet
            };
            AssertSolarEvent(shared.Rise, expectedRise, "shared rise");
            AssertSolarEvent(shared.Set, expectedSet, "shared set");
        }

        [TestCase(1, -0.833)]
        [TestCase(1, -18.0)]
        [TestCase(-1, -0.833)]
        [TestCase(-1, -18.0)]
        public void Nighttime_SharedSolarExtrema_PreserveSubminuteGrazingCrossings(int hemisphere, double threshold) {
            var date = new DateTime(2026, hemisphere > 0 ? 6 : 12, 21, 12, 0, 0, DateTimeKind.Utc);
            var observer = new ObserverInfo { Longitude = 13 };
            var lowerLatitude = 0.0;
            var upperLatitude = 89.0;

            // Construct a real, shallow solar excursion using independent ternary minimization
            // and latitude bisection. No event returned by production supplies the reference bracket.
            for (var iteration = 0; iteration < 35; iteration++) {
                observer.Latitude = hemisphere * (lowerLatitude + upperLatitude) / 2;
                var minimum = FindSolarMinimum(date.AddHours(8), date.AddHours(16), observer);
                if (AstroUtil.GetSunAltitude(minimum, observer) > threshold - 0.00001) {
                    upperLatitude = Math.Abs(observer.Latitude);
                } else {
                    lowerLatitude = Math.Abs(observer.Latitude);
                }
            }
            var minimumTime = FindSolarMinimum(date.AddHours(8), date.AddHours(16), observer);
            var before = minimumTime.AddMinutes(-1);
            var after = minimumTime.AddMinutes(1);
            var beforeAltitude = AstroUtil.GetSunAltitude(before, observer) - threshold;
            var minimumAltitude = AstroUtil.GetSunAltitude(minimumTime, observer) - threshold;
            beforeAltitude.Should().BePositive();
            minimumAltitude.Should().BeNegative();
            (AstroUtil.GetSunAltitude(after, observer) - threshold).Should().BePositive();
            var expectedSet = RefineSolarCrossing(before, minimumTime, beforeAltitude, observer, threshold);
            var expectedRise = RefineSolarCrossing(minimumTime, after, minimumAltitude, observer, threshold);
            (expectedRise - expectedSet).TotalSeconds.Should().BeInRange(0.2, 60);

            var latitude = observer.Latitude;
            var profile = Mock.Of<IProfileService>(x =>
                x.ActiveProfile.AstrometrySettings.Latitude == latitude &&
                x.ActiveProfile.AstrometrySettings.Longitude == 13.0 &&
                x.ActiveProfile.AstrometrySettings.Elevation == 0.0);
            var night = new NighttimeCalculator(profile).Calculate(date);
            night.Ticker.Stop();
            var events = threshold == -18 ? night.TwilightRiseAndSet : night.SunRiseAndSet;
            AssertSolarEvent(events.Set, expectedSet, "grazing set");
            AssertSolarEvent(events.Rise, expectedRise, "grazing rise");
        }

        private static DateTime FindSolarMinimum(DateTime left, DateTime right, ObserverInfo observer) {
            for (var iteration = 0; iteration < 40; iteration++) {
                var third = (right - left).Ticks / 3;
                var first = left.AddTicks(third);
                var second = right.AddTicks(-third);
                if (AstroUtil.GetSunAltitude(first, observer) < AstroUtil.GetSunAltitude(second, observer)) {
                    right = second;
                } else {
                    left = first;
                }
            }
            return left.AddTicks((right - left).Ticks / 2);
        }

        private static DateTime RefineSolarCrossing(DateTime left, DateTime right, double leftAltitude, ObserverInfo observer, double threshold) {
            // Tighten the independent reference considerably beyond the production tolerance.
            for (var iteration = 0; iteration < 30; iteration++) {
                var middle = left.AddTicks((right - left).Ticks / 2);
                var middleAltitude = AstroUtil.GetSunAltitude(middle, observer) - threshold;
                if ((middleAltitude > 0.0) == (leftAltitude > 0.0)) {
                    left = middle;
                    leftAltitude = middleAltitude;
                } else {
                    right = middle;
                }
            }
            return left.AddTicks((right - left).Ticks / 2);
        }

        private static void AssertSolarEvent(DateTime? actual, DateTime? expected, string name) {
            actual.HasValue.Should().Be(expected.HasValue, $"the independent scan must agree whether a {name} occurs");
            if (expected.HasValue) {
                var actualTime = actual ?? throw new AssertionException($"Expected a solar {name}.");
                Math.Abs((actualTime - expected.Value).TotalSeconds).Should().BeLessThanOrEqualTo(0.1,
                    $"the first {name} must match the independently bracketed crossing");
            }
        }

        [TestCase(3, 28)]
        [TestCase(10, 24)]
        public void SunRiseAndSet_AcrossDstTransition_UsesElapsedUtcAndPreservesOutputKind(int month, int day) {
            var start = new DateTime(2026, month, day, 12, 0, 0, DateTimeKind.Local);
            var local = new SunRiseAndSet(start, 52.0, 13.0, 0.0);
            var utc = new SunRiseAndSet(start.ToUniversalTime(), 52.0, 13.0, 0.0);
            local.Compute();
            utc.Compute();

            var localRise = local.Rise ?? throw new AssertionException("Expected a local sunrise.");
            var localSet = local.Set ?? throw new AssertionException("Expected a local sunset.");
            var utcRise = utc.Rise ?? throw new AssertionException("Expected a UTC sunrise.");
            var utcSet = utc.Set ?? throw new AssertionException("Expected a UTC sunset.");
            localRise.Kind.Should().Be(DateTimeKind.Local);
            localSet.Kind.Should().Be(DateTimeKind.Local);
            localRise.ToUniversalTime().Should().BeCloseTo(utcRise, TimeSpan.FromSeconds(0.1));
            localSet.ToUniversalTime().Should().BeCloseTo(utcSet, TimeSpan.FromSeconds(0.1));
        }

        [Test]
        public void SunRiseAndSet_OrdinaryDay_BoundsBodyEvaluations() {
            var events = new GuardedSunRiseAndSet(new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), 52.0);

            events.Compute().Should().BeTrue();

            events.Rise.Should().NotBeNull();
            events.Set.Should().NotBeNull();
            events.SampleCount.Should().BeLessThanOrEqualTo(35,
                "ordinary crossings should converge through verified interpolation brackets");
        }

        private static IEnumerable<object[]> SolarEventCases() {
            foreach (var latitude in new[] { 0.0, 30.0, 52.0, 65.0, 70.0, 80.0 }) {
                foreach (var month in new[] { 3, 6, 9, 12 }) {
                    foreach (var threshold in new[] { -0.833, -6.0, -12.0, -18.0 }) {
                        yield return new object[] { latitude, month, threshold };
                    }
                }
            }
        }

        private sealed class GuardedSunRiseAndSet(DateTime date, double latitude) : SunRiseAndSet(date, latitude, 0.0, 0.0) {
            private int samples;
            public int SampleCount => samples;

            protected override double AdjustAltitude(BasicBody body) {
                if (++samples > 2000) {
                    throw new InvalidOperationException("The polar rise/set calculation did not terminate.");
                }
                return base.AdjustAltitude(body);
            }
        }

    }
}
