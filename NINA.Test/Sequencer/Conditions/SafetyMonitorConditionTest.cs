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
using NINA.Sequencer.Conditions;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Container;

namespace NINA.Test.Sequencer.Conditions {

    [TestFixture]
    public class SafetyMonitorConditionTest {
        private Mock<ISafetyMonitorMediator> monitorMock;

        [SetUp]
        public void Setup() {
            monitorMock = new Mock<ISafetyMonitorMediator>();
        }

        [Test]
        public void SafetyMonitorConditionTest_Clone_GoodClone() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (SafetyMonitorCondition)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Check_True_WhenConnectedAndSafe() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = true });

            sut.Check(null, null).Should().BeTrue();
            sut.IsSafe.Should().BeTrue();
        }

        [Test]
        public void Check_False_WhenNotConnected() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = false });

            sut.Check(null, null).Should().BeFalse();
            sut.IsSafe.Should().BeFalse();
        }

        [Test]
        public void Check_False_WhenConnectedAndNotSafe() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = false });

            sut.Check(null, null).Should().BeFalse();
            sut.IsSafe.Should().BeFalse();
        }

        [Test]
        public void ToString_Test() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            sut.ToString().Should().Be("Condition: SafetyMonitorCondition");
        }

        [Test]
        public void SequenceBlockInitialize_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new SafetyMonitorCondition(monitorMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.SequenceBlockInitialize();

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }

        [Test]
        public void SequenceBlockTeardown_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new SafetyMonitorCondition(monitorMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.SequenceBlockTeardown();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void Validate_MonitorNotConnected_OneIssue() {
            var sut = new SafetyMonitorCondition(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = false, IsSafe = true });

            var valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Count.Should().Be(1);
        }

        [Test]
        public void Validate_MonitorConnected_IsSafeAssigned() {
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = true });

            var sut = new SafetyMonitorCondition(monitorMock.Object);

            var valid = sut.Validate();

            valid.Should().BeTrue();
            sut.Issues.Count.Should().Be(0);
            sut.IsSafe.Should().BeTrue();
        }

        [Test]
        public async Task MonitorIsUnsafe_InterruptParent() {
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = true });
            var parentMock = new Mock<ISequenceRootContainer>();
            parentMock.Setup(x => x.Interrupt()).Returns(Task.CompletedTask);
            parentMock.SetupGet(x => x.Parent).Returns(new Mock<ISequenceRootContainer>().Object);
            parentMock.SetupGet(x => x.Status).Returns(NINA.Core.Enum.SequenceEntityStatus.RUNNING);

            var sut = new SafetyMonitorCondition(monitorMock.Object);
            sut.Parent = parentMock.Object;
            sut.ConditionWatchdog.Delay = TimeSpan.FromMilliseconds(10);

            sut.SequenceBlockInitialize();

            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = false });

            await Task.Delay(50);

            parentMock.Verify(x => x.Interrupt(), Times.AtLeastOnce);
        }
    }

    [TestFixture]
    public class LoopWhileUnsafeTest {
        private Mock<ISafetyMonitorMediator> monitorMock;

        [SetUp]
        public void Setup() {
            monitorMock = new Mock<ISafetyMonitorMediator>();
        }

        [Test]
        public void LoopWhileUnsafeTest_Clone_GoodClone() {
            var sut = new LoopWhileUnsafe(monitorMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (LoopWhileUnsafe)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Check_True_WhenConnectedAndSafe() {
            var sut = new LoopWhileUnsafe(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = true });

            sut.Check(null, null).Should().BeFalse();
            sut.IsSafe.Should().BeTrue();
        }

        [Test]
        public void Check_False_WhenNotConnected() {
            var sut = new LoopWhileUnsafe(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = false });

            sut.Check(null, null).Should().BeTrue();
            sut.IsSafe.Should().BeFalse();
        }

        [Test]
        public void Check_False_WhenConnectedAndNotSafe() {
            var sut = new LoopWhileUnsafe(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = false });

            sut.Check(null, null).Should().BeTrue();
            sut.IsSafe.Should().BeFalse();
        }

        [Test]
        public void ToString_Test() {
            var sut = new LoopWhileUnsafe(monitorMock.Object);
            sut.ToString().Should().Be("Condition: LoopWhileUnsafe");
        }

        [Test]
        public void SequenceBlockInitialize_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new LoopWhileUnsafe(monitorMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.SequenceBlockInitialize();

            watchdogMock.Verify(x => x.Start(), Times.Once);
            watchdogMock.Verify(x => x.Cancel(), Times.Never);
        }

        [Test]
        public void SequenceBlockTeardown_NoParent_WatchdogNotStarted() {
            var watchdogMock = new Mock<IConditionWatchdog>();

            var sut = new LoopWhileUnsafe(monitorMock.Object);
            sut.ConditionWatchdog = watchdogMock.Object;

            sut.SequenceBlockTeardown();

            watchdogMock.Verify(x => x.Start(), Times.Never);
            watchdogMock.Verify(x => x.Cancel(), Times.Once);
        }

        [Test]
        public void Validate_MonitorNotConnected_OneIssue() {
            var sut = new LoopWhileUnsafe(monitorMock.Object);
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = false, IsSafe = true });

            var valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Count.Should().Be(1);
        }

        [Test]
        public void Validate_MonitorConnected_IsSafeAssigned() {
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = true });

            var sut = new LoopWhileUnsafe(monitorMock.Object);

            var valid = sut.Validate();

            valid.Should().BeTrue();
            sut.Issues.Count.Should().Be(0);
            sut.IsSafe.Should().BeTrue();
        }

        [Test]
        public async Task MonitorIsUnsafe_InterruptParent() {
            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = false });
            var parentMock = new Mock<ISequenceContainer>();
            parentMock.Setup(x => x.Interrupt()).Returns(Task.CompletedTask);
            parentMock.SetupGet(x => x.Parent).Returns(new Mock<ISequenceRootContainer>().Object);
            parentMock.SetupGet(x => x.Status).Returns(NINA.Core.Enum.SequenceEntityStatus.RUNNING);

            var sut = new LoopWhileUnsafe(monitorMock.Object);
            sut.AttachNewParent(parentMock.Object);
            sut.ConditionWatchdog.Delay = TimeSpan.FromMilliseconds(10);

            sut.SequenceBlockInitialize();

            monitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = true });

            await Task.Delay(50);

            parentMock.Verify(x => x.Interrupt(), Times.AtLeastOnce);
        }
    }
}