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