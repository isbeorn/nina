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
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Focuser;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Focuser {

    [TestFixture]
    internal class MoveFocuserRelativeTest {
        public Mock<IFocuserMediator> focuserMediatorMock;

        [SetUp]
        public void Setup() {
            focuserMediatorMock = new Mock<IFocuserMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new MoveFocuserRelative(focuserMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (MoveFocuserRelative)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.RelativePosition.Should().Be(sut.RelativePosition);
        }

        [Test]
        public void Validate_NoIssues() {
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true });

            var sut = new MoveFocuserRelative(focuserMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = false });

            var sut = new MoveFocuserRelative(focuserMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(0)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task Execute_NoIssues_LogicCalled(int relativePosition) {
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo() { Connected = true });

            var sut = new MoveFocuserRelative(focuserMediatorMock.Object);
            sut.RelativePosition = relativePosition;
            await sut.Execute(default, default);

            focuserMediatorMock.Verify(x => x.MoveFocuserRelative(It.Is<int>(p => p == relativePosition), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            var sut = new MoveFocuserRelative(focuserMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}