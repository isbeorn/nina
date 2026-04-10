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
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitForMoonAltitudeTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void WaitForMoonAltitude_Clone_GoodClone() {
            var sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (WaitForMoonAltitude)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Data.TargetAltitude.Should().Be(sut.Data.TargetAltitude);
            item2.Data.Comparator.Should().Be(sut.Data.Comparator);
        }

        [Test]
        public void WaitForMoonAltitude_MustWait_GreaterThan_WaitsWhileAboveTarget() {
            // Moon is always above -91°, so it is always waiting while the condition (altitude > -91°) holds
            var sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;
            sut.Data.Offset = -91;

            sut.CalculateExpectedTime();

            sut.Data.ExpectedDateTime.Should().BeAfter(DateTime.Now);
        }

        [Test]
        public void WaitForMoonAltitude_MustWait_GreaterThan_DoesNotWaitWhenBelowTarget() {
            // Moon can never reach 91°, so the condition (altitude > 91°) is never true and there is nothing to wait for
            var sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;
            sut.Data.Offset = 91;

            sut.CalculateExpectedTime();

            sut.Data.ExpectedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Test]
        public void WaitForMoonAltitude_MustWait_LessThan_WaitsWhileBelowTarget() {
            // Moon is always below 91°, so it is always waiting while the condition (altitude <= 91°) holds
            var sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.LESS_THAN;
            sut.Data.Offset = 91;

            sut.CalculateExpectedTime();

            sut.Data.ExpectedDateTime.Should().BeAfter(DateTime.Now);
        }

        [Test]
        public void WaitForMoonAltitude_MustWait_LessThan_DoesNotWaitWhenAboveTarget() {
            // Moon is always above -91°, so the condition (altitude <= -91°) is never true and there is nothing to wait for
            var sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.LESS_THAN;
            sut.Data.Offset = -91;

            sut.CalculateExpectedTime();

            sut.Data.ExpectedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }
    }
}
