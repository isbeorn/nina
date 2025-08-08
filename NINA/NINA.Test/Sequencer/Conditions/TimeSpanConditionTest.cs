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
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility.DateTimeProvider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class TimeSpanConditionTest {

        [Test]
        public void TimeSpanCondition_Clone_GoodClone() {
            var sut = new TimeSpanCondition();
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (TimeSpanCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Hours.Should().Be(sut.Hours);
            item2.Minutes.Should().Be(sut.Minutes);
            item2.Seconds.Should().Be(sut.Seconds);
        }

        [Test]
        public void TimeSpanCondition_Constructor_NoCrash() {
            var sut = new TimeSpanCondition();

            sut.Hours.Should().Be(0);
            sut.Minutes.Should().Be(1);
            sut.Seconds.Should().Be(0);
        }

        [Test]
        public void TimeSpanCondition_ToString_IncludesTimeCorrectly() {
            var sut = new TimeSpanCondition();
            sut.Hours = 10;
            sut.Minutes = 20;
            sut.Seconds = 30;

            sut.ToString().Should().Be("Condition: TimeSpanCondition, Time: 10:20:30h");
        }

        [Test]
        public void TimeSpanCondition_ResetProgress_RemainingTimeSame() {
            var dateMock = new Mock<ICustomDateTime>();
            dateMock
                .SetupSequence(x => x.UtcNow)
                .Returns(new DateTime(2000, 1, 1, 0, 0, 10))
                .Returns(new DateTime(2000, 1, 1, 0, 0, 30))
                .Returns(new DateTime(2000, 1, 1, 0, 0, 30))
                .Returns(new DateTime(2000, 1, 1, 0, 0, 30));

            var sut = new TimeSpanCondition();
            sut.DateTime = dateMock.Object;
            sut.Hours = 0;
            sut.Minutes = 0;
            sut.Seconds = 30;

            sut.SequenceBlockInitialize();
            sut.RemainingTime.Should().Be(TimeSpan.FromSeconds(10));
            sut.ResetProgress();
            sut.RemainingTime.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Test]
        public void TimeSpanCondition_Initialized_And_ResetProgress_RemainingTimeCleared() {
            var dateMock = new Mock<ICustomDateTime>();
            dateMock
                .SetupSequence(x => x.UtcNow)
                .Returns(new DateTime(2000, 1, 1, 0, 0, 10))
                .Returns(new DateTime(2000, 1, 1, 0, 0, 30));

            var sut = new TimeSpanCondition();
            sut.DateTime = dateMock.Object;
            sut.Hours = 0;
            sut.Minutes = 0;
            sut.Seconds = 30;

            sut.SequenceBlockInitialize();
            sut.RemainingTime.Should().Be(TimeSpan.FromSeconds(10));
            sut.ResetProgress();
            sut.RemainingTime.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Test]
        public void TimeSpanCondition_Initialized_WatchdogStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var dateMock = new Mock<ICustomDateTime>();
            dateMock
                .SetupSequence(x => x.UtcNow)
                .Returns(new DateTime(2000, 1, 1, 0, 0, 10))
                .Returns(new DateTime(2000, 1, 1, 0, 0, 30));

            var sut = new TimeSpanCondition();
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.DateTime = dateMock.Object;
            sut.Hours = 0;
            sut.Minutes = 0;
            sut.Seconds = 30;

            sut.SequenceBlockInitialize();

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
            sut.RemainingTime.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Test]
        public void TimeSpanCondition_Teardown_WatchdogStopped() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var dateMock = new Mock<ICustomDateTime>();
            dateMock
                .SetupSequence(x => x.UtcNow)
                .Returns(new DateTime(2000, 1, 1, 0, 0, 10));

            var sut = new TimeSpanCondition();
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.DateTime = dateMock.Object;
            sut.Hours = 0;
            sut.Minutes = 0;
            sut.Seconds = 30;

            sut.SequenceBlockTeardown();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void TimeSpanCondition_Initialized_AfterTeardown_RemainingTimeContinues() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var dateMock = new Mock<ICustomDateTime>();
            dateMock
                .SetupSequence(x => x.UtcNow)
                .Returns(new DateTime(2000, 1, 1, 0, 0, 10))
                .Returns(new DateTime(2000, 1, 1, 0, 0, 30))
                .Returns(new DateTime(2000, 1, 1, 0, 0, 32));

            var sut = new TimeSpanCondition();
            sut.ConditionWatchdog = watchdogMock.Object;
            sut.DateTime = dateMock.Object;
            sut.Hours = 0;
            sut.Minutes = 0;
            sut.Seconds = 30;

            sut.SequenceBlockInitialize();
            sut.SequenceBlockTeardown();
            sut.SequenceBlockInitialize();
            sut.RemainingTime.Should().Be(TimeSpan.FromSeconds(8));
        }

        [Test]
        [TestCase(0, 0, false)]
        [TestCase(10, 0, true)]
        [TestCase(10, 10, false)]
        [TestCase(10, 9, true)]
        public void TimeSpanCondition_Check_ReturnsCorrectly(int remainingSeconds, int nextItemSeconds, bool expected) {
            var dateMock = new Mock<ICustomDateTime>();
            dateMock
                .SetupGet(x => x.UtcNow)
                .Returns(new DateTime(2000, 1, 1, 0, 0, 10));

            var itemMock = new Mock<ISequenceItem>();
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromSeconds(nextItemSeconds));

            var sut = new TimeSpanCondition();
            sut.DateTime = dateMock.Object;
            sut.Hours = 0;
            sut.Minutes = 0;
            sut.Seconds = remainingSeconds;

            sut.Check(null, itemMock.Object).Should().Be(expected);
        }
    }
}