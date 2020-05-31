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
    public class AbsoluteBacklashCompensationDecoratorTest {
        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
        private Mock<IFocuser> focuserMock = new Mock<IFocuser>();

        [SetUp]
        public void Setup() {
            profileServiceMock.Reset();
            focuserMock.Reset();
            // Initial position = 1000
            focuserMock.SetupGet(x => x.Position).Returns(1000);
            focuserMock.SetupGet(x => x.MaxStep).Returns(50000);

            // Move commands set position to input value
            focuserMock.Setup(x => x.Move(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .Callback((int position, CancellationToken ct) => {
                     focuserMock.SetupGet(x => x.Position).Returns(position);
                 });
        }

        [Test]
        public async Task Move_SameDirection_NoBacklashComp_Outwards() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 100);

            var sut = new AbsoluteBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(1200, default);
            await sut.Move(1400, default);

            sut.Position.Should().Be(1400);
            focuserMock.Object.Position.Should().Be(1400);
        }

        [Test]
        public async Task Move_SameDirection_NoBacklashComp_Inwards() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 100);

            var sut = new AbsoluteBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(600, default);
            await sut.Move(400, default);

            sut.Position.Should().Be(400);
            focuserMock.Object.Position.Should().Be(400);
        }

        [Test]
        public async Task Move_SameDirection_BacklashComp_DirectionChangeToOutwards() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 100);

            var sut = new AbsoluteBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(600, default);
            await sut.Move(1000, default);

            sut.Position.Should().Be(1000);
            focuserMock.Object.Position.Should().Be(1100);
        }

        [Test]
        public async Task Move_SameDirection_BacklashComp_DirectionChangeToInwards() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 100);

            var sut = new AbsoluteBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(1500, default);
            await sut.Move(1000, default);

            sut.Position.Should().Be(1000);
            focuserMock.Object.Position.Should().Be(500);
        }

        [Test]
        public async Task Move_SameDirection_BacklashComp_FirstInThenOutThenInThenOut() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 100);

            var sut = new AbsoluteBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(500, default); // no comp
            await sut.Move(1500, default); // +100
            await sut.Move(1300, default); // -500
            await sut.Move(1600, default); // +100

            sut.Position.Should().Be(1600);
            focuserMock.Object.Position.Should().Be(1300);
        }

        [Test]
        public async Task Move_SameDirection_BacklashComp_FirstOutThenInThenOutThenIn() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 100);

            var sut = new AbsoluteBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(1500, default); // no comp
            await sut.Move(1000, default); // -500
            await sut.Move(1400, default); // +100
            await sut.Move(1200, default); // -500

            sut.Position.Should().Be(1200);
            focuserMock.Object.Position.Should().Be(300);
        }

        [Test]
        public async Task Move_BelowZeroDueToBacklashComp_MoveTo0() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 500);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 100);

            var sut = new AbsoluteBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(1500, default); // no comp
            await sut.Move(1000, default); // -500
            await sut.Move(1400, default); // +100
            await sut.Move(1200, default); // -500
            await sut.Move(0, default);

            sut.Position.Should().Be(0);
            focuserMock.Object.Position.Should().Be(0);
        }

        [Test]
        public async Task Move_AboveMaxStepDueToBacklashComp_MoveToMaxStep() {
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, 100);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, 500);

            var sut = new AbsoluteBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(1500, default); // no comp
            await sut.Move(1000, default); // -100
            await sut.Move(1400, default); // +500
            await sut.Move(1000, default); // -100
            await sut.Move(50000, default);

            sut.Position.Should().Be(50000);
            focuserMock.Object.Position.Should().Be(50000);
        }
    }
}