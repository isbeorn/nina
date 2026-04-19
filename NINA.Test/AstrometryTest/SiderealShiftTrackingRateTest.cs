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
    public class SiderealShiftTrackingRateTest {
        private const double AngleTolerance = 1e-10;

        /// <summary>
        /// Verifies sidereal shift tracking converts an ephemeris delta into per-hour and
        /// per-second rates without losing the sidereal-second conversion needed by mount tracking.
        /// </summary>
        [Test]
        public void SiderealShiftTrackingRate_CreateFromCoordinates_ReturnsExpectedRates() {
            Coordinates start = new Coordinates(10.0, -5.0, Epoch.J2000, Coordinates.RAType.Degrees);
            Coordinates end = new Coordinates(10.5, -4.75, Epoch.J2000, Coordinates.RAType.Degrees);

            SiderealShiftTrackingRate rate = SiderealShiftTrackingRate.Create(start, end, TimeSpan.FromHours(2));

            rate.Enabled.Should().BeTrue();
            rate.RADegreesPerHour.Should().BeApproximately(0.25, AngleTolerance);
            rate.DecDegreesPerHour.Should().BeApproximately(0.125, AngleTolerance);
            rate.RAArcsecsPerHour.Should().BeApproximately(900.0, AngleTolerance);
            rate.RASecondsPerSiderealSecond.Should().BeApproximately(0.25 / SiderealShiftTrackingRate.SIDEREAL_RATE, AngleTolerance);
        }

        /// <summary>
        /// Verifies disabled and direct sidereal-rate factories for non-sidereal tracking, including
        /// declination arcsecond rates used by mount control.
        /// </summary>
        [Test]
        public void SiderealShiftTrackingRate_DisabledAndDirectFactories_ReturnExpectedRates() {
            SiderealShiftTrackingRate disabled = SiderealShiftTrackingRate.Disabled;
            SiderealShiftTrackingRate direct = SiderealShiftTrackingRate.Create(0.5, -0.25);

            disabled.Enabled.Should().BeFalse();
            disabled.RAArcsecsPerHour.Should().Be(0.0);
            direct.Enabled.Should().BeTrue();
            direct.RAArcsecsPerSec.Should().Be(0.5);
            direct.DecArcsecsPerSec.Should().Be(-0.25);
            direct.DecArcsecsPerHour.Should().Be(-900.0);
        }
    }
}
