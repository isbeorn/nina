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
using NINA.Equipment.Equipment.MyCamera;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NINA.Core.Model;

namespace NINA.Test.Sequencer.SequenceItem.Camera {

    [TestFixture]
    internal class SetReadoutModeTest {
        public Mock<ICameraMediator> cameraMediatorMock;

        [SetUp]
        public void Setup() {
            cameraMediatorMock = new Mock<ICameraMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new SetReadoutMode(cameraMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Mode = 3;
            var item2 = (SetReadoutMode)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Mode.Should().Be(sut.Mode);
        }

        [Test]
        public void Validate_NoIssues() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, ReadoutModes = new List<string>() { "0", "1" } });

            var sut = new SetReadoutMode(cameraMediatorMock.Object);
            sut.Mode = 1;
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });

            var sut = new SetReadoutMode(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public void Validate_NegativeReadoutMode_OneIssue() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, ReadoutModes = new List<string>() { "0", "1" } });

            var sut = new SetReadoutMode(cameraMediatorMock.Object);
            sut.Mode = -1;
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public void Validate_TooBigReadoutMode_OneIssue() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, ReadoutModes = new List<string>() { "0", "1" } });

            var sut = new SetReadoutMode(cameraMediatorMock.Object);
            sut.Mode = 2;
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, ReadoutModes = new List<string>() { "0", "1" } });

            var sut = new SetReadoutMode(cameraMediatorMock.Object);
            sut.Mode = 1;
            await sut.Execute(default, default);

            cameraMediatorMock.Verify(x => x.SetReadoutMode(It.IsAny<short>()), Times.Never);
            cameraMediatorMock.Verify(x => x.SetReadoutModeForNormalImages(1), Times.Once);
        }
    }
}