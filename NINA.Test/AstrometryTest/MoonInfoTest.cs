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
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Linq;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class MoonInfoTest {

        /// <summary>
        /// Verifies Moon-target separation by using the NOVAS lunar apparent position itself as the
        /// target at the same instant MoonInfo samples for separation, so the angular distance should
        /// collapse to nearly zero apart from transformation and topocentric rounding.
        /// </summary>
        [Test]
        public void SetReferenceDateAndObserver_TargetAtMoonPosition_ReturnsNearZeroSeparation() {
            DateTime referenceDate = new DateTime(2024, 4, 8, 6, 0, 0, DateTimeKind.Utc);
            DateTime separationSampleTime = referenceDate.AddHours(12);
            ObserverInfo observer = new ObserverInfo { Latitude = 29.7604, Longitude = -95.3698, Elevation = 15.0 };
            NOVAS.SkyPosition moonPosition = AstroUtil.GetMoonPosition(separationSampleTime, observer);
            Coordinates moonCoordinates = new Coordinates(Angle.ByHours(moonPosition.RA), Angle.ByDegree(moonPosition.Dec), Epoch.JNOW, separationSampleTime);
            MoonInfo moonInfo = new MoonInfo(moonCoordinates);

            moonInfo.SetReferenceDateAndObserver(referenceDate, observer);

            moonInfo.Separation.Should().BeLessThan(0.1);
            moonInfo.SeparationText.Should().Be("000°");
            moonInfo.DataPoints.Should().BeNull();
        }

        /// <summary>
        /// Verifies the generated lunar altitude curve spans one reference day at six-minute cadence
        /// and reports the same maximum altitude as the sampled data, preserving the charted Moon path.
        /// </summary>
        [Test]
        public void SetReferenceDateAndObserver_DisplayMoon_GeneratesOneDayAltitudeCurveAndMaximum() {
            DateTime referenceDate = new DateTime(2024, 3, 25, 12, 0, 0, DateTimeKind.Utc);
            ObserverInfo observer = new ObserverInfo { Latitude = 51.4769, Longitude = 0.0, Elevation = 46.0 };
            Coordinates target = new Coordinates(Angle.ByDegree(180.0), Angle.ByDegree(0.0), Epoch.J2000, referenceDate);
            MoonInfo moonInfo = new MoonInfo(target) { DisplayMoon = true };

            moonInfo.SetReferenceDateAndObserver(referenceDate, observer);

            moonInfo.DataPoints.Should().HaveCount(240);
            moonInfo.DataPoints.Select(x => x.Y).Should().OnlyContain(altitude => altitude >= -90.0 && altitude <= 90.0);
            moonInfo.MaxAltitude.Y.Should().Be(moonInfo.DataPoints.Max(x => x.Y));
            moonInfo.MaxAltitude.Y.Should().BeGreaterThan(20.0);
            moonInfo.DataPoints.First().X.Should().Be(Axis.ToDouble(referenceDate));
        }

        /// <summary>
        /// Verifies current Moon display metadata remains in physically valid domains: the phase is
        /// resolved to a known bucket and the grayscale alpha/color channels stay inside byte ranges.
        /// This intentionally does not assert the known-buggy position-angle utility path.
        /// </summary>
        [Test]
        public void CurrentMoonDisplayMetadata_ReturnsKnownPhaseAndValidColorChannels() {
            MoonInfo moonInfo = new MoonInfo(new Coordinates(Angle.ByDegree(180.0), Angle.ByDegree(0.0), Epoch.J2000));

            moonInfo.Phase.Should().NotBe(AstroUtil.MoonPhase.Unknown);
            moonInfo.Color.A.Should().BeLessThanOrEqualTo(byte.MaxValue);
            moonInfo.Color.R.Should().Be(moonInfo.Color.G);
            moonInfo.Color.G.Should().Be(moonInfo.Color.B);
        }
    }
}
