#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Core.Enum;
using NUnit.Framework;
using System.Collections.Generic;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class SunAltitudeConditionTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void SunAltitudeCondition_Clone_GoodClone() {
            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (SunAltitudeCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Data.TargetAltitude.Should().Be(sut.Data.TargetAltitude);
            item2.Data.Comparator.Should().Be(sut.Data.Comparator);
        }

        [Test]
        public void SunAltitudeCondition_NoProviderInConstructor_NoCrash() {
            var sut = new SunAltitudeCondition(profileServiceMock.Object);

            sut.Data.TargetAltitude.Should().Be(0);
            sut.Data.Comparator.Should().Be(ComparisonOperatorEnum.GREATER_THAN);
        }

        [Test]
        public void ComparisonOperators_FilteredAccordingly() {
            var sut = new SunAltitudeCondition(profileServiceMock.Object);

            var expectedOperators = new List<ComparisonOperatorEnum>() {
                ComparisonOperatorEnum.LESS_THAN,
                ComparisonOperatorEnum.GREATER_THAN,
            };

            sut.Data.ComparisonOperators.Should().BeEquivalentTo(expectedOperators);
        }

        [Test]
        [TestCase(10, 20, ComparisonOperatorEnum.LESS_THAN, false)]
        [TestCase(20, 10, ComparisonOperatorEnum.LESS_THAN, true)]
        [TestCase(10, 10.01, ComparisonOperatorEnum.LESS_THAN, false)]
        [TestCase(10, 9.99, ComparisonOperatorEnum.LESS_THAN, true)]
        [TestCase(10, 20, ComparisonOperatorEnum.GREATER_THAN, true)]
        [TestCase(20, 10, ComparisonOperatorEnum.GREATER_THAN, false)]
        [TestCase(10, 10.01, ComparisonOperatorEnum.GREATER_THAN, true)]
        [TestCase(10, 9.99, ComparisonOperatorEnum.GREATER_THAN, false)]
        public void Check_LESS_THAN(double currentAlt, double userAlt, ComparisonOperatorEnum Comparator, bool expected) {
            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.Data.Comparator = Comparator;
            sut.Data.Offset = userAlt;
            sut.Data.CurrentAltitude = currentAlt;

            sut.Check(default, default, true).Should().Be(expected);
        }

        [Test]
        public void ToString_Test() {
            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;
            sut.Data.TargetAltitude = 10;
            sut.Data.CurrentAltitude = 20;

            sut.ToString().Should().Be("Condition: SunAltitudeCondition, CurrentAltitude: 20, Comparator: GREATER_THAN, TargetAltitude: 10");
        }

        [Test]
        public void AfterParentChanged_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AfterParentChanged_NotInRootContainer_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AfterParentChanged_InRootContainer_WatchdogStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceRootContainer>();

            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }

        [Test]
        public void OnDeserialized_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void OnDeserialized_NotInRootContainer_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void OnDeserialized_InRootContainer_WatchdogStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceRootContainer>();

            var sut = new SunAltitudeCondition(profileServiceMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }
    }
}