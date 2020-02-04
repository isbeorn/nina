#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    internal class MeridianFlipVMTest {
        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
        private Mock<IProfile> profileMock = new Mock<IProfile>();
        private Mock<IMeridianFlipSettings> meridianFlipSettingsMock = new Mock<IMeridianFlipSettings>();

        [OneTimeSetUp]
        public void Init() {
            profileServiceMock.SetupGet(m => m.ActiveProfile).Returns(profileMock.Object);
            profileMock.SetupGet(m => m.MeridianFlipSettings).Returns(meridianFlipSettingsMock.Object);
        }

        [Test]
        /* no pause */
        [TestCase(6, 5, 5, 0, false, false)]
        [TestCase(5.9, 5, 5, 0, false, true)]
        /* with pause */
        [TestCase(16, 5, 5, 5, false, false)]
        [TestCase(15.9, 5, 5, 5, false, true)]
        /* sideOfPier */
        [TestCase(16, 5, 5, 5, true, false, PierSide.pierWest)]
        [TestCase(15.9, 5, 5, 5, true, true, PierSide.pierWest)]
        [TestCase(15.9, 5, 5, 5, true, false, PierSide.pierEast)]
        public void ShouldFlipTest(double timeToFlipMinutes, double exposureTimeMinutes, double minutesAfterMeridian, double pauseBeforeMeridian, bool useSideOfPier, bool shouldFlip, PierSide pierSide = PierSide.pierUnknown) {
            meridianFlipSettingsMock.SetupGet(m => m.Enabled).Returns(true);
            meridianFlipSettingsMock.SetupGet(m => m.MinutesAfterMeridian).Returns(minutesAfterMeridian);
            meridianFlipSettingsMock.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(pauseBeforeMeridian);
            meridianFlipSettingsMock.SetupGet(m => m.Recenter).Returns(false);
            meridianFlipSettingsMock.SetupGet(m => m.SettleTime).Returns(0);
            meridianFlipSettingsMock.SetupGet(m => m.UseSideOfPier).Returns(useSideOfPier);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(timeToFlipMinutes).TotalHours,
                SideOfPier = pierSide,
                Connected = true
            };

            var exposureTime = TimeSpan.FromMinutes(exposureTimeMinutes).TotalSeconds;

            var sut = MeridianFlipVM.ShouldFlip(profileServiceMock.Object, exposureTime, telescopeInfo);

            sut.Should().Be(shouldFlip);
        }
    }
}