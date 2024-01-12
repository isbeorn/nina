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
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Camera {

    [TestFixture]
    internal class WarmCameraTest {
        public Mock<ICameraMediator> cameraMediatorMock;

        [SetUp]
        public void Setup() {
            cameraMediatorMock = new Mock<ICameraMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new WarmCamera(cameraMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (WarmCamera)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Duration.Should().Be(sut.Duration);
        }

        [Test]
        public void Validate_NoIssues() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, CanSetTemperature = true });

            var sut = new WarmCamera(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = false });

            var sut = new WarmCamera(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public void Validate_CanNotSetTemperature_OneIssue() {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, CanSetTemperature = false });

            var sut = new WarmCamera(cameraMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(10)]
        [TestCase(10)]
        [TestCase(0)]
        [TestCase(0)]
        [TestCase(0)]
        [TestCase(10)]
        public async Task Execute_NoIssues_LogicCalled(int duration) {
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true, CanSetTemperature = true });

            var sut = new WarmCamera(cameraMediatorMock.Object);
            sut.Duration = duration;
            await sut.Execute(default, default);

            cameraMediatorMock.Verify(x => x.WarmCamera(It.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(duration)), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(30, 30)]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate(int minutes, int expected) {
            var sut = new WarmCamera(cameraMediatorMock.Object);
            sut.Duration = minutes;

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromMinutes(expected));
        }
    }
}