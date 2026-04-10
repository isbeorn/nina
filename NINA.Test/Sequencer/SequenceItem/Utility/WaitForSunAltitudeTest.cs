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
    public class WaitForSunAltitudeTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void WaitForSunAltitude_Clone_GoodClone() {
            var sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (WaitForSunAltitude)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Data.TargetAltitude.Should().Be(sut.Data.TargetAltitude);
            item2.Data.Comparator.Should().Be(sut.Data.Comparator);
        }

        [Test]
        public void WaitForSunAltitude_MustWait_GreaterThan_WaitsWhenBelowTarget() {
            // 91° is unreachable by the sun, so the condition is never met and we must always wait
            var sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;
            sut.Data.Offset = 91;

            sut.CalculateExpectedTime();

            sut.Data.ExpectedDateTime.Should().BeAfter(DateTime.Now);
        }

        [Test]
        public void WaitForSunAltitude_MustWait_GreaterThan_DoesNotWaitWhenAboveTarget() {
            // -91° is always below the sun, so the condition is already met and we must not wait
            var sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;
            sut.Data.Offset = -91;

            sut.CalculateExpectedTime();

            sut.Data.ExpectedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Test]
        public void WaitForSunAltitude_MustWait_LessThan_WaitsWhenAboveTarget() {
            // Sun is always above -91°, so the condition is never met and we must always wait
            var sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.LESS_THAN;
            sut.Data.Offset = -91;

            sut.CalculateExpectedTime();

            sut.Data.ExpectedDateTime.Should().BeAfter(DateTime.Now);
        }

        [Test]
        public void WaitForSunAltitude_MustWait_LessThan_DoesNotWaitWhenBelowTarget() {
            // 91° is always above the sun, so the condition is already met and we must not wait
            var sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.LESS_THAN;
            sut.Data.Offset = 91;

            sut.CalculateExpectedTime();

            sut.Data.ExpectedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }
    }
}
