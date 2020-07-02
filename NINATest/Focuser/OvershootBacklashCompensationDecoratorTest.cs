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

            // Move commands set position to input value
            focuserMock.Setup(x => x.Move(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .Callback((int position, CancellationToken ct) => {
                     focuserMock.SetupGet(x => x.Position).Returns(position);
                 });
        }

        [Test]
        [TestCase(1000, 50000, 500, 0, 1200, 1400, 1400, FocuserDecorator.Direction.OUT)]
        [TestCase(1000, 50000, 0, 500, 800, 400, 400, FocuserDecorator.Direction.IN)]
        [TestCase(1000, 50000, 200, 0, 1500, 400, 400, FocuserDecorator.Direction.OUT)]
        [TestCase(1000, 50000, 0, 500, 500, 1500, 1500, FocuserDecorator.Direction.IN)]
        [TestCase(1000, 50000, 500, 0, 1500, 0, 0, FocuserDecorator.Direction.IN)]
        [TestCase(1000, 50000, 0, 500, 1500, 50000, 50000, FocuserDecorator.Direction.OUT)]
        public async Task Move_TwoTimes(int initialPosition, int maxStep, int backlashIn, int backlashOut, int firstMove, int secondMove, int expectedPosition, FocuserDecorator.Direction expectedLastDirection) {
            focuserMock.SetupGet(x => x.Position).Returns(initialPosition);
            focuserMock.SetupGet(x => x.MaxStep).Returns(maxStep);

            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashIn, backlashIn);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.BacklashOut, backlashOut);

            var sut = new TestableOvershootBacklashCompensationDecorator(profileServiceMock.Object, focuserMock.Object);

            await sut.Move(firstMove, default);
            await sut.Move(secondMove, default);

            sut.Position.Should().Be(expectedPosition);
            focuserMock.Object.Position.Should().Be(expectedPosition);
            sut.LastDirection.Should().Be(expectedLastDirection);
        }
    }
}