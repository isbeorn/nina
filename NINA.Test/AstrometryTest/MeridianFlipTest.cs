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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using System;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class MeridianFlipTest {

        [TestCase(359.9, 0.1, 359.8)]
        [TestCase(0.1, 359.9, 0.2)]
        [TestCase(90.0, 0.0, 90.0)]
        public void TimeToMeridian_GeometricInterval_IsElapsedCivilTime(double targetDegrees, double siderealDegrees, double deltaDegrees) {
            var coordinates = new Coordinates(Angle.ByDegree(targetDegrees), Angle.ByDegree(0), Epoch.JNOW);
            var expectedHours = (deltaDegrees / 15.0) % 12.0;
            var actual = MeridianFlip.TimeToMeridian(coordinates, Angle.ByDegree(siderealDegrees));
            actual.TotalSeconds.Should().BeApproximately(expectedHours * 3600 / 1.002737909350795, 0.01);
        }

        /// <summary>
        /// Verifies the hour-angle relation used for meridian timing: a target east of the meridian
        /// reaches transit soon, a target west of the meridian wraps to the next upper culmination,
        /// and a target exactly twelve sidereal hours away maps to the same meridian line.
        /// </summary>
        [Test]
        [TestCase(6.0, 5.0, 1.0)]
        [TestCase(4.0, 5.0, 11.0)]
        [TestCase(17.0, 5.0, 0.0)]
        public void TimeToMeridian_HourAngleGeometry_WrapsAtTwelveSiderealHours(double rightAscensionHours, double localSiderealTimeHours, double expectedHours) {
            Coordinates coordinates = new Coordinates(Angle.ByHours(rightAscensionHours), Angle.ByDegree(0.0), Epoch.JNOW);

            TimeSpan time = MeridianFlip.TimeToMeridian(coordinates, Angle.ByHours(localSiderealTimeHours));

            time.TotalSeconds.Should().BeApproximately(expectedHours * 3600 / SiderealShiftTrackingRate.SIDEREAL_SEC_PER_SI_SEC, 0.001);
        }

        /// <summary>
        /// Verifies the expected pier side from hour-angle quadrants: objects still approaching
        /// the meridian use the west counterweight-down side, while objects past transit use east.
        /// </summary>
        [Test]
        [TestCase(11.0, 10.0, PierSide.pierWest)]
        [TestCase(9.0, 10.0, PierSide.pierEast)]
        [TestCase(22.0, 10.0, PierSide.pierEast)]
        public void ExpectedPierSide_HourAngleQuadrants_ReturnsCounterweightDownSide(double rightAscensionHours, double localSiderealTimeHours, PierSide expectedPierSide) {
            Coordinates coordinates = new Coordinates(Angle.ByHours(rightAscensionHours), Angle.ByDegree(0.0), Epoch.JNOW);

            PierSide pierSide = MeridianFlip.ExpectedPierSide(coordinates, Angle.ByHours(localSiderealTimeHours));

            pierSide.Should().Be(expectedPierSide);
        }

        /// <summary>
        /// Verifies side-of-pier protection when the mount is already flipped shortly before the
        /// meridian: the next required flip is delayed by a half sidereal day instead of triggering
        /// at the imminent meridian crossing.
        /// </summary>
        [Test]
        public void TimeToMeridianFlip_AlreadyFlippedBeforeMeridian_DelaysToNextMeridianCycle() {
            Mock<IMeridianFlipSettings> settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(x => x.MaxMinutesAfterMeridian).Returns(30.0);
            settings.SetupGet(x => x.UseSideOfPier).Returns(true);
            Coordinates coordinates = new Coordinates(Angle.ByHours(10.25), Angle.ByDegree(0.0), Epoch.JNOW);

            TimeSpan time = MeridianFlip.TimeToMeridianFlip(settings.Object, coordinates, Angle.ByHours(10.0), PierSide.pierEast);

            time.TotalSeconds.Should().BeApproximately(12.25 * 3600 / SiderealShiftTrackingRate.SIDEREAL_SEC_PER_SI_SEC + 1800, 0.001);
        }

        /// <summary>
        /// Verifies that an unknown pier side does not alter the pure hour-angle flip time, because
        /// side-of-pier compensation is only physically meaningful when the mount reports a side.
        /// </summary>
        [Test]
        public void TimeToMeridianFlip_UnknownPierSide_UsesProjectedHourAngleOnly() {
            Mock<IMeridianFlipSettings> settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(x => x.MaxMinutesAfterMeridian).Returns(30.0);
            settings.SetupGet(x => x.UseSideOfPier).Returns(true);
            Coordinates coordinates = new Coordinates(Angle.ByHours(10.25), Angle.ByDegree(0.0), Epoch.JNOW);

            TimeSpan time = MeridianFlip.TimeToMeridianFlip(settings.Object, coordinates, Angle.ByHours(10.0), PierSide.pierUnknown);

            time.TotalSeconds.Should().BeApproximately(0.25 * 3600 / SiderealShiftTrackingRate.SIDEREAL_SEC_PER_SI_SEC + 1800, 0.001);
        }

        /// <summary>
        /// Verifies the post-meridian side-of-pier guard: if the target has crossed the meridian but
        /// the mount still reports the expected pre-flip side, the imminent flip window is deferred
        /// to the next meridian cycle instead of being treated as immediately actionable.
        /// </summary>
        [Test]
        public void TimeToMeridianFlip_NotYetFlippedAfterMeridian_DelaysToNextMeridianCycle() {
            Mock<IMeridianFlipSettings> settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(x => x.MaxMinutesAfterMeridian).Returns(30.0);
            settings.SetupGet(x => x.UseSideOfPier).Returns(true);
            Coordinates coordinates = new Coordinates(Angle.ByHours(10.0), Angle.ByDegree(0.0), Epoch.JNOW);

            TimeSpan time = MeridianFlip.TimeToMeridianFlip(settings.Object, coordinates, Angle.ByHours(10.25), PierSide.pierEast);

            time.TotalSeconds.Should().BeApproximately(11.75 * 3600 / SiderealShiftTrackingRate.SIDEREAL_SEC_PER_SI_SEC + 1800, 0.001);
        }
    }
}
