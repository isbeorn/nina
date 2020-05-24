#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Model.MyFocuser;
using NINA.Profile;
using NINA.ViewModel.Equipment.Focuser;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Focuser {

    [TestFixture]
    public class OvershootBacklashCompensationDecoratorTest {
        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
        private Mock<IFocuser> focuserMock = new Mock<IFocuser>();

        private class TestableOvershootBacklashCompensationDecorator : OvershootBacklashCompensationDecorator {

            public TestableOvershootBacklashCompensationDecorator(IProfileService profileService, IFocuser focuser) : base(profileService, focuser) {
            }

            public Direction LastDirection { get => base.lastDirection; }
        }

        [SetUp]
        public void Setup() {
            profileServiceMock.Reset();
            focuserMock.Reset();
            // Initial position = 1000
            focuserMock.SetupGet(x => x.Position).Returns(1000);

            // Move commands set position to input value
            focuserMock.Setup(x => x.Move(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .Callback((int position, CancellationToken ct) => {
                     focuserMock.SetupGet(x => x.Position).Returns(position);
                 });
        }

        [Test]
        public async Task Move_SameDirection_NoBacklashComp_Outwards() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 0);

            var sut = new TestableOvershootBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(1200, default);
            await sut.Move(1400, default);

            sut.Position.Should().Be(1400);
            focuserMock.Object.Position.Should().Be(1400);
            sut.LastDirection.Should().Be(FocuserDecorator.Direction.OUT);
        }

        [Test]
        public async Task Move_SameDirection_NoBacklashComp_Inwards() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 0);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 500);

            var sut = new TestableOvershootBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(800, default);
            await sut.Move(400, default);

            sut.Position.Should().Be(400);
            focuserMock.Object.Position.Should().Be(400);
            sut.LastDirection.Should().Be(FocuserDecorator.Direction.IN);
        }

        [Test]
        public async Task Move_SameDirection_BacklashComp_Inwards() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 0);

            var sut = new TestableOvershootBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(1500, default);
            await sut.Move(400, default);

            sut.Position.Should().Be(400);
            focuserMock.Object.Position.Should().Be(400);
            sut.LastDirection.Should().Be(FocuserDecorator.Direction.OUT);
        }

        [Test]
        public async Task Move_SameDirection_BacklashComp_Outwards() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 0);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 500);

            var sut = new TestableOvershootBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(500, default);
            await sut.Move(1500, default);

            sut.Position.Should().Be(1500);
            focuserMock.Object.Position.Should().Be(1500);
            sut.LastDirection.Should().Be(FocuserDecorator.Direction.IN);
        }
    }
}