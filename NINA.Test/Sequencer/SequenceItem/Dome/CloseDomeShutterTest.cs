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
using NINA.Equipment.Equipment.MyDome;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Dome;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Dome {

    [TestFixture]
    internal class CloseDomeShutterTest {
        public Mock<IDomeMediator> domeMediatorMock;

        [SetUp]
        public void Setup() {
            domeMediatorMock = new Mock<IDomeMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new CloseDomeShutter(domeMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (CloseDomeShutter)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Validate_NoIssues() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = true });

            var sut = new CloseDomeShutter(domeMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = false });

            var sut = new CloseDomeShutter(domeMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = true, ShutterStatus = NINA.Equipment.Interfaces.ShutterState.ShutterClosed });
            domeMediatorMock.Setup(x => x.CloseShutter(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            var sut = new CloseDomeShutter(domeMediatorMock.Object);
            await sut.Execute(default, default);

            domeMediatorMock.Verify(x => x.CloseShutter(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            var sut = new CloseDomeShutter(domeMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}