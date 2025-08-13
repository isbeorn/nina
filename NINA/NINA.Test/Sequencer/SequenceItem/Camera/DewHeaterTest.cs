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
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Sequencer;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Camera {

    [TestFixture]
    internal class DewHeaterTest {
        public Mock<ICameraMediator> cameraMediatorMock;

        [SetUp]
        public void Setup() {
            cameraMediatorMock = new Mock<ICameraMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new DewHeater(cameraMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.OnOff = true;

            var item2 = (DewHeater)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.OnOff.Should().Be(sut.OnOff);
        }

        [Test]
        public void Validate_NoIssues() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, HasDewHeater = true });

            var sut = new DewHeater(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        [TestCase(false, false, 1)]
        [TestCase(false, true, 1)]
        public void Validate_NotConnected_OneIssue(bool isConnected, bool hasDH, int count) {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = isConnected, HasDewHeater = hasDH });

            var sut = new DewHeater(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(count);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task Execute_NoIssues_LogicCalled(bool onOff) {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, HasDewHeater = true });

            var sut = new DewHeater(cameraMediatorMock.Object);
            sut.OnOff = onOff;
            await sut.Execute(default, default);

            cameraMediatorMock.Verify(x => x.SetDewHeater(It.Is<bool>(b => b == onOff)), Times.Once);
        }

        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public async Task Execute_HasIssues_LogicNotCalled(bool isConnected, bool hasDH) {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = isConnected, HasDewHeater = hasDH });

            var sut = new DewHeater(cameraMediatorMock.Object);
            await sut.Run(default, default);

            cameraMediatorMock.Verify(x => x.SetDewHeater(It.IsAny<bool>()), Times.Never);
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }
    }
}