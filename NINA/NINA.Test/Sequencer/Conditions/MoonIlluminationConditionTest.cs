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
using NINA.Sequencer.Conditions;
using NINA.Core.Enum;
using NUnit.Framework;
using System.Collections.Generic;
using Moq;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Container;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class MoonIlluminationConditionTest {

        [Test]
        public void MoonIlluminationCondition_Clone_GoodClone() {
            var sut = new MoonIlluminationCondition();
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (MoonIlluminationCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.UserMoonIllumination.Should().Be(sut.UserMoonIllumination);
            item2.Comparator.Should().Be(sut.Comparator);
        }

        [Test]
        public void MoonIlluminationCondition_NoProviderInConstructor_NoCrash() {
            var sut = new MoonIlluminationCondition();

            sut.UserMoonIllumination.Should().Be(0);
            sut.Comparator.Should().Be(ComparisonOperatorEnum.GREATER_THAN);
        }

        [Test]
        public void ComparisonOperators_FilteredAccordingly() {
            var sut = new MoonIlluminationCondition();

            var expectedOperators = new List<ComparisonOperatorEnum>() {
                ComparisonOperatorEnum.LESS_THAN,
                ComparisonOperatorEnum.LESS_THAN_OR_EQUAL,
                ComparisonOperatorEnum.GREATER_THAN,
                ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL
            };

            sut.ComparisonOperators.Should().BeEquivalentTo(expectedOperators);
        }

        [Test]
        [TestCase(10, 20, ComparisonOperatorEnum.LESS_THAN, false)]
        [TestCase(20, 10, ComparisonOperatorEnum.LESS_THAN, true)]
        [TestCase(10, 10, ComparisonOperatorEnum.LESS_THAN, true)]
        [TestCase(10, 10.01, ComparisonOperatorEnum.LESS_THAN, false)]
        [TestCase(10, 9.99, ComparisonOperatorEnum.LESS_THAN, true)]
        [TestCase(10, 20, ComparisonOperatorEnum.GREATER_THAN, true)]
        [TestCase(20, 10, ComparisonOperatorEnum.GREATER_THAN, false)]
        [TestCase(10, 10, ComparisonOperatorEnum.GREATER_THAN, true)]
        [TestCase(10, 10.01, ComparisonOperatorEnum.GREATER_THAN, true)]
        [TestCase(10, 9.99, ComparisonOperatorEnum.GREATER_THAN, false)]
        [TestCase(10, 20, ComparisonOperatorEnum.LESS_THAN_OR_EQUAL, false)]
        [TestCase(20, 10, ComparisonOperatorEnum.LESS_THAN_OR_EQUAL, true)]
        [TestCase(10, 10, ComparisonOperatorEnum.LESS_THAN_OR_EQUAL, false)]
        [TestCase(10, 10.01, ComparisonOperatorEnum.LESS_THAN_OR_EQUAL, false)]
        [TestCase(10, 9.99, ComparisonOperatorEnum.LESS_THAN_OR_EQUAL, true)]
        [TestCase(10, 20, ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL, true)]
        [TestCase(20, 10, ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL, false)]
        [TestCase(10, 10, ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL, false)]
        [TestCase(10, 10.01, ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL, true)]
        [TestCase(10, 9.99, ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL, false)]
        public void Check_LESS_THAN(double currentAlt, double userAlt, ComparisonOperatorEnum Comparator, bool expected) {
            var sut = new MoonIlluminationCondition();
            sut.Comparator = Comparator;
            sut.UserMoonIllumination = userAlt;
            sut.CurrentMoonIllumination = currentAlt;

            sut.Check(default, default).Should().Be(expected);
        }

        [Test]
        public void ToString_Test() {
            var sut = new MoonIlluminationCondition();
            sut.Comparator = ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL;
            sut.UserMoonIllumination = 10;
            sut.CurrentMoonIllumination = 20;

            sut.ToString().Should().Be("Condition: MoonIlluminationCondition, CurrentMoonIllumination: 20%, Comparator: GREATER_THAN_OR_EQUAL, UserMoonIllumination: 10%");
        }

        [Test]
        public void AfterParentChanged_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new MoonIlluminationCondition();
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void AfterParentChanged_NotInRootContainer_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new MoonIlluminationCondition();
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

            var sut = new MoonIlluminationCondition();
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.AfterParentChanged();

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }

        [Test]
        public void OnDeserialized_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new MoonIlluminationCondition();
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void OnDeserialized_NotInRootContainer_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();
            var parentMock = new Mock<ISequenceContainer>();

            var sut = new MoonIlluminationCondition();
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

            var sut = new MoonIlluminationCondition();
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.Parent = parentMock.Object;

            sut.OnDeserialized(default);

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }
    }
}