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
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.FlatDevice {

    [TestFixture]
    internal class ToggleLightTest {
        public Mock<IFlatDeviceMediator> fdMediatorMock;

        [SetUp]
        public void Setup() {
            fdMediatorMock = new Mock<IFlatDeviceMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new ToggleLight(fdMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.OnOff = true;

            var item2 = (ToggleLight)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.OnOff.Should().Be(sut.OnOff);
        }

        [Test]
        public void Validate_NoIssues() {
            fdMediatorMock.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo() { Connected = true, SupportsOpenClose = true, SupportsOnOff = true });

            var sut = new ToggleLight(fdMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        [TestCase(false, false, true, 1)]
        [TestCase(false, true, true, 1)]
        [TestCase(true, true, false, 1)]
        public void Validate_NotConnected_OneIssue(bool isConnected, bool canClose, bool canOnOff, int count) {
            fdMediatorMock.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo() { Connected = isConnected, SupportsOpenClose = canClose, SupportsOnOff = canOnOff });

            var sut = new ToggleLight(fdMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(count);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task Execute_NoIssues_LogicCalled(bool onoff) {
            fdMediatorMock.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo() { Connected = true, SupportsOpenClose = true, SupportsOnOff = true, LightOn = onoff });

            var sut = new ToggleLight(fdMediatorMock.Object);
            sut.OnOff = onoff;
            await sut.Execute(default, default);

            fdMediatorMock.Verify(x => x.ToggleLight(It.Is<bool>(b => b == sut.OnOff), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public async Task Execute_HasIssues_LogicNotCalled(bool isConnected, bool canOnOff) {
            fdMediatorMock.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo() { Connected = isConnected, SupportsOnOff = canOnOff });

            var sut = new ToggleLight(fdMediatorMock.Object);
            await sut.Run(default, default);

            fdMediatorMock.Verify(x => x.ToggleLight(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Never);
            sut.Status.Should().Be(SequenceEntityStatus.FAILED);
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            var sut = new ToggleLight(fdMediatorMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.Zero);
        }
    }
}