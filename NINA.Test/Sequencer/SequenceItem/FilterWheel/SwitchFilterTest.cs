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
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model.Equipment;

namespace NINA.Test.Sequencer.SequenceItem.FilterWheel {

    [TestFixture]
    internal class SwitchFilterTest {
        public Mock<IFilterWheelMediator> fwMediatorMock;
        public Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            fwMediatorMock = new Mock<IFilterWheelMediator>();
            profileServiceMock = new Mock<IProfileService>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (SwitchFilter)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Filter.Should().BeSameAs(sut.Filter);
        }

        [Test]
        public void Validate_NoIssues() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_NoFilterSelected_NoIssue() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = false });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().HaveCount(0);
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = false });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            sut.Filter = new FilterInfo();
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled() {
            var filter = new FilterInfo();
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = true });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            sut.Filter = filter;
            await sut.Execute(default, default);

            fwMediatorMock.Verify(x => x.ChangeFilter(It.Is<FilterInfo>(f => f == filter), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Once);
        }

        [Test]
        public Task Execute_NoFilterSelected_Skipped() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = false });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            Func<Task> act = () => { return sut.Execute(default, default); };

            fwMediatorMock.Verify(x => x.ChangeFilter(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Never);
            return act.Should().ThrowAsync<SequenceItemSkippedException>();
        }

        [Test]
        public Task Execute_HasIssues_LogicNotCalled() {
            fwMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { Connected = false });

            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);
            Func<Task> act = () => { return sut.Execute(default, default); };

            fwMediatorMock.Verify(x => x.ChangeFilter(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), Times.Never);
            return act.Should().ThrowAsync<SequenceItemSkippedException>(string.Join(",", sut.Issues));
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(30, 30)]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate(int minutes, int expected) {
            var sut = new SwitchFilter(profileServiceMock.Object, fwMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}