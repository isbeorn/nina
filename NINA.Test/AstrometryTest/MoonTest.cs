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
using NINA.Astrometry.Body;
using NINA.Astrometry.RiseAndSet;
using NUnit.Framework;
using System;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class MoonTest {
        /// <summary>
        /// Verifies Moon distance remains within the physically expected geocentric range, catching
        /// AU-to-kilometer conversion mistakes and accidental use of the wrong NOVAS body.
        /// Reference: https://science.nasa.gov/moon/facts/
        /// </summary>
        [Test]
        public void MoonCalculate_RepresentativeDate_ReturnsExpectedDistanceRange() {
            Moon moon = new Moon(new DateTime(2024, 3, 25, 7, 0, 0, DateTimeKind.Utc), 51.4769, 0.0, 46.0);

            moon.Calculate();

            moon.Distance.Should().BeInRange(350_000.0, 410_000.0);
            moon.Radius.Should().Be(1738.0);
        }

        /// <summary>
        /// The topocentric lunar upper limb meets the apparent horizon when the geometric center
        /// is depressed by 34 arcminutes of refraction plus the distance-dependent angular radius.
        /// Parallax is already included by NOVAS's Earth-surface observer.
        /// Reference: https://aa.usno.navy.mil/faq/RST_defs
        /// </summary>
        [TestCase(2024, 4, 8, 52.0)]
        [TestCase(2026, 10, 8, 52.0)]
        [TestCase(2024, 4, 8, -33.0)]
        [TestCase(2026, 10, 8, -33.0)]
        public void MoonRiseAndSet_UpperLimbCrossings_IncludeRefractionAndAngularRadius(int year, int month, int day, double latitude) {
            var date = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc);
            var events = new MoonRiseAndSet(date, latitude, 13.0, 0.0);
            events.Compute().Should().BeTrue();
            events.Rise.Should().NotBeNull();
            events.Set.Should().NotBeNull();

            foreach (var time in new[] {
                events.Rise ?? throw new AssertionException("Expected moonrise."),
                events.Set ?? throw new AssertionException("Expected moonset.")
            }) {
                var moon = new Moon(time, latitude, 13.0, 0.0);
                moon.Calculate();
                var limbAltitude = moon.Altitude + 34.0 / 60.0
                    + AstroUtil.ToDegree(Math.Asin(moon.Radius / moon.Distance));

                limbAltitude.Should().BeApproximately(0.0, 0.03);
            }
        }

        [Test]
        public void MoonCustomRiseAndSet_ExplicitCenterAltitude_PreservesCallerThreshold() {
            var date = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
            var events = new MoonCustomRiseAndSet(date, 52.0, 13.0, 0.0, 5.0);
            events.Compute();

            foreach (var time in new[] {
                events.Rise ?? throw new AssertionException("Expected custom moonrise."),
                events.Set ?? throw new AssertionException("Expected custom moonset.")
            }) {
                var moon = new Moon(time, 52.0, 13.0, 0.0);
                moon.Calculate();
                moon.Altitude.Should().BeApproximately(5.0, 0.03);
            }
        }
    }
}
