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

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class TwilightCalculatorTest {
        /// <summary>
        /// Verifies the twilight calculator returns a positive, bounded duration for a mid-latitude
        /// equinox case where all civil sunrise and astronomical twilight events exist.
        /// Reference: https://gml.noaa.gov/grad/solcalc/calcdetails.html
        /// </summary>
        [Test]
        public void TwilightCalculator_GreenwichEquinox_ReturnsPhysicalDuration() {
            TwilightCalculator calculator = new TwilightCalculator();

            TimeSpan duration = calculator.GetTwilightDuration(new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc), 51.4769, 0.0, 46.0);

            duration.Should().BeGreaterThan(TimeSpan.FromMinutes(60));
            duration.Should().BeLessThan(TimeSpan.FromMinutes(180));
        }

        /// <summary>
        /// Verifies the legacy twilight-duration overload delegates to the elevation-aware overload,
        /// preserving historical call sites that do not supply site elevation.
        /// </summary>
        [Test]
        public void TwilightCalculator_LegacyOverload_MatchesZeroElevationDuration() {
            TwilightCalculator calculator = new TwilightCalculator();
            DateTime date = new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);

#pragma warning disable CS0618
            TimeSpan legacy = calculator.GetTwilightDuration(date, 51.4769, 0.0);
#pragma warning restore CS0618
            TimeSpan current = calculator.GetTwilightDuration(date, 51.4769, 0.0, 0.0);

            legacy.Should().Be(current);
        }

        /// <summary>
        /// Verifies polar-day behavior where neither astronomical-night rise nor normal sunrise/set
        /// events exist; the calculator should return zero duration instead of manufacturing a
        /// twilight interval from missing horizon crossings.
        /// Reference: https://www.sunrise-and-sunset.com/en/sun/norway/tromso/2024
        /// </summary>
        [Test]
        public void TwilightCalculator_TromsoMidnightSun_ReturnsZeroDurationForMissingCrossings() {
            TwilightCalculator calculator = new TwilightCalculator();

            TimeSpan duration = calculator.GetTwilightDuration(new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc), 69.6492, 18.9553, 10.0);

            duration.Should().Be(TimeSpan.Zero);
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void TwilightCalculator_NearDawn_PairsDawnWithTheUpcomingSunrise(int minutesAfterDawn) {
            var date = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);
            var dawn = AstroUtil.GetNightTimes(date, 52.0, 13.0, 0.0).Rise ?? throw new AssertionException("Expected dawn.");
            var sunrise = AstroUtil.GetSunRiseAndSet(date, 52.0, 13.0, 0.0).Rise ?? throw new AssertionException("Expected sunrise.");

            var duration = new TwilightCalculator().GetTwilightDuration(dawn.AddMinutes(minutesAfterDawn), 52.0, 13.0, 0.0);

            duration.Should().BeCloseTo(sunrise - dawn, TimeSpan.FromSeconds(30));
            duration.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Test]
        public void TwilightCalculator_NoAstronomicalDawn_UsesThePrecedingSunset() {
            var date = new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc);
            var sunrise = AstroUtil.GetSunRiseAndSet(date, 52.0, 13.0, 0.0).Rise ?? throw new AssertionException("Expected sunrise.");
            var sunset = AstroUtil.GetSunRiseAndSet(sunrise.AddDays(-1), 52.0, 13.0, 0.0).Set ?? throw new AssertionException("Expected preceding sunset.");
            AstroUtil.GetNightTimes(sunrise.AddDays(-1), 52.0, 13.0, 0.0).Rise.Should().BeNull();

            var duration = new TwilightCalculator().GetTwilightDuration(date, 52.0, 13.0, 0.0);

            duration.Should().BeCloseTo(sunrise - sunset, TimeSpan.FromSeconds(30));
            duration.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Test]
        public void TwilightCalculator_LocalAndUtcRepresentations_ReturnTheSameDuration() {
            var date = new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc);
            var calculator = new TwilightCalculator();

            var utc = calculator.GetTwilightDuration(date, 52.0, 13.0, 0.0);
            var local = calculator.GetTwilightDuration(date.ToLocalTime(), 52.0, 13.0, 0.0);

            local.Should().Be(utc);
        }
    }
}
