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
using NINA.Equipment.Equipment.MyTelescope;
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
    internal class EnableDomeSynchronizationTest {
        public Mock<IDomeMediator> domeMediatorMock;
        public Mock<ITelescopeMediator> telescopeMediatorMock;

        [SetUp]
        public void Setup() {
            domeMediatorMock = new Mock<IDomeMediator>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new EnableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (EnableDomeSynchronization)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Validate_NoIssues() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = true });
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });

            var sut = new EnableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_DomeNotConnected_OneIssue() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = false });
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });

            var sut = new EnableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public void Validate_TelescopeNotConnected_OneIssue() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = true });
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = false });

            var sut = new EnableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public void Validate_TelescopeAndDomeNotConnected_TwoIssues() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = false });
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = false });

            var sut = new EnableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(2);
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = true });
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });

            var sut = new EnableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);
            await sut.Execute(default, default);

            domeMediatorMock.Verify(x => x.EnableFollowing(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public Task Execute_HasIssues_LogicNotCalled() {
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(new DomeInfo() { Connected = false });
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() { Connected = true });

            var sut = new EnableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);
            Func<Task> act = () => { return sut.Execute(default, default); };

            domeMediatorMock.Verify(x => x.EnableFollowing(It.IsAny<CancellationToken>()), Times.Never);
            return act.Should().ThrowAsync<SequenceItemSkippedException>(string.Join(",", sut.Issues));
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            var sut = new EnableDomeSynchronization(domeMediatorMock.Object, telescopeMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}