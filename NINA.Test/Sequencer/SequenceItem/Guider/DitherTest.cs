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
using NINA.Equipment.Equipment.MyGuider;
using NINA.Sequencer;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Profile.Interfaces;

namespace NINA.Test.Sequencer.SequenceItem.Guider {

    [TestFixture]
    internal class DitherTest {
        public Mock<IGuiderMediator> guiderMediatorMock;
        public Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            guiderMediatorMock = new Mock<IGuiderMediator>();
            profileServiceMock = new Mock<IProfileService>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new Dither(guiderMediatorMock.Object, profileServiceMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (Dither)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
        }

        [Test]
        public void Validate_NoIssues() {
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo() { Connected = true });

            var sut = new Dither(guiderMediatorMock.Object, profileServiceMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();

            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo() { Connected = false });

            var sut = new Dither(guiderMediatorMock.Object, profileServiceMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();

            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public async Task Execute_NoIssues_LogicCalled() {
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo() { Connected = true });

            var sut = new Dither(guiderMediatorMock.Object, profileServiceMock.Object);
            await sut.Execute(default, default);

            guiderMediatorMock.Verify(x => x.Dither(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void GetEstimatedDuration_BasedOnParameters_ReturnsCorrectEstimate() {
            profileServiceMock.SetupGet(x => x.ActiveProfile.GuiderSettings.SettleTimeout).Returns(100);
            var sut = new Dither(guiderMediatorMock.Object, profileServiceMock.Object);

            var duration = sut.GetEstimatedDuration();

            duration.Should().Be(TimeSpan.FromSeconds(100));
        }
    }
}