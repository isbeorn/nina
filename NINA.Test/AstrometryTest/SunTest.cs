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
    }
}
