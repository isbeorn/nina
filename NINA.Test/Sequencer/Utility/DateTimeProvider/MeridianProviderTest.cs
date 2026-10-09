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
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Core.Utility;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Utility.DateTimeProvider {

    [TestFixture]
    public class MeridianProviderTest {

        [Test]
        [TestCase(19, 10)]
        [TestCase(18, 10)]
        [TestCase(13, 10)]
        [TestCase(11, 10)]
        [TestCase(9, 10)]
        [TestCase(7, 10)]
        [TestCase(6, 10)]
        public void GetDateTime_EntityHasParentWithCoordinates_CalculatesTimeToMeridian(double ra, double dec) {
            ra = AstroUtil.EuclidianModulus(ra, 24);
            dec = AstroUtil.EuclidianModulus(dec, 360);

            var profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(10);

            var referenceDate = new DateTime(2020, 1, 1, 0, 0, 0);
            referenceDate = DateTime.SpecifyKind(referenceDate, DateTimeKind.Utc);

            var customDateTimeMock = new Mock<ICustomDateTime>();
            customDateTimeMock.SetupGet(x => x.Now).Returns(referenceDate);

            var entityMock = new Mock<ISequenceEntity>();
            var containerMock = new Mock<IDeepSkyObjectContainer>();
            var coordinates = new Coordinates(Angle.ByHours(ra), Angle.ByDegree(dec), Epoch.J2000, customDateTimeMock.Object);
            var target = new InputTarget(Angle.ByDegree(10), Angle.ByDegree(10), null);
            target.InputCoordinates = new InputCoordinates(coordinates);
            containerMock.SetupGet(x => x.Target).Returns(target);
            entityMock.SetupGet(x => x.Parent).Returns(containerMock.Object);

            var sut = new MeridianProvider(profileServiceMock.Object);
            sut.DateTime = customDateTimeMock.Object;

            var date = sut.GetDateTime(entityMock.Object);

            (date - referenceDate).Should().BeGreaterThanOrEqualTo(TimeSpan.Zero)
                .And.BeLessThan(TimeSpan.FromHours(12 / SiderealShiftTrackingRate.SIDEREAL_SEC_PER_SI_SEC));
            void AssertOnMeridianLine(DateTime instant) {
                var apparent = coordinates.Transform(Epoch.JNOW, instant);
                double hourAngle = AstroUtil.GetLocalSiderealTime(instant, 0) - apparent.RA;
                double lineError = AstroUtil.EuclidianModulus(hourAngle + 6, 12) - 6;
                Math.Abs(lineError * 15).Should().BeLessThan(0.001);
            }
            AssertOnMeridianLine(date);
            var rollover = DateOnly.FromDateTime(date).ToDateTime(sut.GetRolloverTime(entityMock.Object));
            if (rollover < date) rollover = rollover.AddDays(1);
            // TimeOnly has no zone information, so restore the UTC kind of the fixture clock.
            rollover = DateTime.SpecifyKind(rollover, DateTimeKind.Utc);
            AssertOnMeridianLine(rollover);
        }

        [Test]
        public void GetDateTime_ConextIsNull_ReturnNow() {
            var profileServiceMock = new Mock<IProfileService>();

            var referenceDate = new DateTime(2020, 1, 1, 0, 0, 0);

            var customDateTimeMock = new Mock<ICustomDateTime>();
            customDateTimeMock.SetupGet(x => x.Now).Returns(referenceDate);

            var sut = new MeridianProvider(profileServiceMock.Object);
            sut.DateTime = customDateTimeMock.Object;

            var date = sut.GetDateTime(null);
            date.Should().Be(referenceDate);
        }

        [Test]
        public void GetDateTime_ConextHasParentWithoutCoordinates_ReturnNow() {
            var profileServiceMock = new Mock<IProfileService>();

            var referenceDate = new DateTime(2020, 1, 1, 0, 0, 0);

            var customDateTimeMock = new Mock<ICustomDateTime>();
            customDateTimeMock.SetupGet(x => x.Now).Returns(referenceDate);

            var sut = new MeridianProvider(profileServiceMock.Object);
            sut.DateTime = customDateTimeMock.Object;

            var entityMock = new Mock<ISequenceEntity>();

            var date = sut.GetDateTime(entityMock.Object);
            date.Should().Be(referenceDate);
        }
    }
}
