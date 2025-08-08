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
using NINA.Core.Model;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem.SafetyMonitor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.SafetyMonitor {

    [TestFixture]
    public class WaitUntilSafeTest {
        public Mock<ISafetyMonitorMediator> safetyMonitorMock;

        [SetUp]
        public void Setup() {
            safetyMonitorMock = new Mock<ISafetyMonitorMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new WaitUntilSafe(safetyMonitorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (WaitUntilSafe)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Validate_NoIssues() {
            safetyMonitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = true });

            var sut = new WaitUntilSafe(safetyMonitorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            safetyMonitorMock.Setup(x => x.GetInfo()).Returns(new SafetyMonitorInfo() { Connected = false });

            var sut = new WaitUntilSafe(safetyMonitorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public async Task Execute_NoIssues_AlreadySafe_LogicCalled() {
            safetyMonitorMock.Setup(x => x.GetInfo())
                .Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = true });

            var sut = new WaitUntilSafe(safetyMonitorMock.Object);
            sut.WaitInterval = TimeSpan.Zero;
            await sut.Execute(default, default);

            safetyMonitorMock.Verify(x => x.GetInfo(), Times.Exactly(1));
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled() {
            safetyMonitorMock.SetupSequence(x => x.GetInfo())
                .Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = false })
                .Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = false })
                .Returns(new SafetyMonitorInfo() { Connected = true, IsSafe = true });

            var sut = new WaitUntilSafe(safetyMonitorMock.Object);
            sut.WaitInterval = TimeSpan.Zero;
            await sut.Execute(default, default);

            safetyMonitorMock.Verify(x => x.GetInfo(), Times.Exactly(3));
        }

        [Test]
        public void ToString_Correct() {
            var category = "SomeCategory";
            var sut = new WaitUntilSafe(safetyMonitorMock.Object);
            sut.Category = category;

            sut.ToString().Should().Be("Category: SomeCategory, Item: WaitUntilSafe");
        }
    }
}