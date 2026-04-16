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
using NINA.Astrometry.Body;
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
    }
}
