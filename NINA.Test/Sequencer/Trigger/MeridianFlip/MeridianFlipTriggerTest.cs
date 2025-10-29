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
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel.ImageHistory;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Equipment.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.WPF.Base.Interfaces;

namespace NINA.Test.Sequencer.Trigger.MeridianFlip {

    [TestFixture]
    public class MeridianFlipTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IMeridianFlipVMFactory> meridianFlipVMFactoryMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            meridianFlipVMFactoryMock = new Mock<IMeridianFlipVMFactory>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
        }

        private MeridianFlipTrigger CreateSUT() {
            return new MeridianFlipTrigger(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, focuserMediatorMock.Object, applicationStatusMediatorMock.Object, meridianFlipVMFactoryMock.Object, safetyMonitorMediatorMock.Object);
        }

        [Test]
        public void CloneTest() {
            var initial = CreateSUT();
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (MeridianFlipTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        public void ShouldTrigger_TimeToMeridianZero_True() {
            var sut = CreateSUT();

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() {
                Connected = true,
                TimeToMeridianFlip = 0,
                TrackingEnabled = true
            });

            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(false);

            var itemMock = new Mock<ISequenceItem>();
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.Zero);

            var should = sut.ShouldTrigger(null, itemMock.Object);

            should.Should().BeTrue();
        }

        [Test]
        public void ShouldTrigger_TimeToMeridianLarge_ButSequenceItemDurationLarger_True() {
            var sut = CreateSUT();

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(new TelescopeInfo() {
                Connected = true,
                TimeToMeridianFlip = 5,
                TrackingEnabled = true
            });

            profileServiceMock.SetupGet(x => x.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(false);

            var itemMock = new Mock<ISequenceItem>();
            itemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromHours(10));

            var should = sut.ShouldTrigger(null, itemMock.Object);

            should.Should().BeTrue();
        }

        [Test]
        public void ShouldFlip_NoTelescopeConnected_UnableToFlip() {
            var sut = CreateSUT();
            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(new Mock<IMeridianFlipSettings>().Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = 10,
                Connected = false
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(6000));

            sut.ShouldTrigger(null, nextItemMock.Object).Should().BeFalse();
        }

        [Test]
        public void ShouldFlip_TelescopeConnectedButNaNTime_UnableToFlip() {
            var sut = CreateSUT();
            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(new Mock<IMeridianFlipSettings>().Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = double.NaN,
                Connected = true,
                TrackingEnabled = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(6000));

            sut.ShouldTrigger(null, nextItemMock.Object).Should().BeFalse();
        }

        [Test]
        public void ShouldFlip_LastFlipHappenedAlready_NoFlip() {
            //todo
        }

        [Test]
        [TestCase(5, 5, -1, true)]
        [TestCase(5, 5, 0, true)]
        [TestCase(5, 5, 2, true)]
        [TestCase(5, 5, 4, true)]
        [TestCase(5, 5, 5, true)]
        [TestCase(5, 10, 8, false)]
        [TestCase(5, 10, 10, false)]
        [TestCase(5, 10, 11, false)]
        public void ShouldFlip_BetweenMinimumAndMaximumTime_NoPause_NoPierSide_FlipWhenExpected(double minTimeToFlip, double maxTimeToFlip, double remainingTimeToFlip, bool expectToFlip) {
            var sut = CreateSUT();

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(false);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                Connected = true,
                TrackingEnabled = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(minTimeToFlip));

            sut.ShouldTrigger(null, nextItemMock.Object).Should().Be(expectToFlip);
        }

        [Test]
        [TestCase(5, 5, -1, PierSide.pierWest, PierSide.pierEast, true)]
        [TestCase(5, 5, 0, PierSide.pierWest, PierSide.pierEast, true)]
        [TestCase(5, 5, 2, PierSide.pierWest, PierSide.pierEast, true)]
        [TestCase(5, 5, 4, PierSide.pierWest, PierSide.pierEast, true)]
        [TestCase(5, 5, 5, PierSide.pierWest, PierSide.pierEast, true)]
        [TestCase(5, 10, 8, PierSide.pierWest, PierSide.pierEast, false)]
        [TestCase(5, 10, 10, PierSide.pierWest, PierSide.pierEast, false)]
        [TestCase(5, 10, 11, PierSide.pierWest, PierSide.pierEast, false)]
        /* Same tests as above, except target and current pier sides are inverted, so the results should be the same */
        [TestCase(5, 5, -1, PierSide.pierEast, PierSide.pierWest, true)]
        [TestCase(5, 5, 0, PierSide.pierEast, PierSide.pierWest, true)]
        [TestCase(5, 5, 2, PierSide.pierEast, PierSide.pierWest, true)]
        [TestCase(5, 5, 4, PierSide.pierEast, PierSide.pierWest, true)]
        [TestCase(5, 5, 5, PierSide.pierEast, PierSide.pierWest, true)]
        [TestCase(5, 10, 8, PierSide.pierEast, PierSide.pierWest, false)]
        [TestCase(5, 10, 10, PierSide.pierEast, PierSide.pierWest, false)]
        [TestCase(5, 10, 11, PierSide.pierEast, PierSide.pierWest, false)]
        /* Same tests as before, but with pier side inverted, therefore no flip is expected in each case */
        [TestCase(5, 5, -1, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(5, 5, 0, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(5, 5, 2, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(5, 5, 4, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(5, 5, 5, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(5, 10, 8, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(5, 10, 10, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(5, 10, 11, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(5, 5, -1, PierSide.pierWest, PierSide.pierWest, false)]
        [TestCase(5, 5, 0, PierSide.pierWest, PierSide.pierWest, false)]
        [TestCase(5, 5, 2, PierSide.pierWest, PierSide.pierWest, false)]
        [TestCase(5, 5, 4, PierSide.pierWest, PierSide.pierWest, false)]
        [TestCase(5, 5, 5, PierSide.pierWest, PierSide.pierWest, false)]
        [TestCase(5, 10, 8, PierSide.pierWest, PierSide.pierWest, false)]
        [TestCase(5, 10, 10, PierSide.pierWest, PierSide.pierWest, false)]
        [TestCase(5, 10, 11, PierSide.pierWest, PierSide.pierWest, false)]
        public void ShouldFlip_BetweenMinimumAndMaximumTime_NoPause_UsePierSide_FlipWhenExpected(
            double minTimeToFlip,
            double maxTimeToFlip,
            double remainingTimeToFlip,
            PierSide pierSide,
            PierSide targetPierSide,
            bool expectToFlip) {
            var sut = CreateSUT();

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(true);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);
            var rightAscension = 12.9;
            var localSiderealTime = 13.0;
            if (targetPierSide == PierSide.pierWest) {
                rightAscension -= 12.0;
            }
            var coordinates = new Coordinates(Angle.ByHours(rightAscension), Angle.ByDegree(20.0), Epoch.JNOW);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                SideOfPier = pierSide,
                Coordinates = coordinates,
                SiderealTime = localSiderealTime,
                Connected = true,
                TrackingEnabled = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(minTimeToFlip));

            sut.ShouldTrigger(null, nextItemMock.Object).Should().Be(expectToFlip);
        }

        [Test]
        /* Exposure time is 7 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure still fits in, no flip yet
         */
        [TestCase(7, 5, 10, 8, PierSide.pierWest, PierSide.pierEast, false)]
        [TestCase(7, 5, 10, 8, PierSide.pierEast, PierSide.pierWest, false)]
        /* Exposure time is 9 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure does not fit, flip needs to start with a wait time
         */
        [TestCase(9, 5, 10, 8, PierSide.pierWest, PierSide.pierEast, true)]
        [TestCase(9, 5, 10, 8, PierSide.pierEast, PierSide.pierWest, true)]
        /* Same Test as before, but pier side is already correct and no flip required */
        [TestCase(9, 5, 10, 8, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(9, 5, 10, 8, PierSide.pierWest, PierSide.pierWest, false)]
        public void ShouldFlip_BeforeMinimumTime_NoPause_PierSideIsUsed_EvaluateIfFlipIsNecessary(
            double nextItemExpectedTime,
            double minTimeToFlip,
            double maxTimeToFlip,
            double remainingTimeToFlip,
            PierSide pierSide,
            PierSide targetPierSide,
            bool expectToFlip) {
            var sut = CreateSUT();

            var rightAscension = 12.9;
            var localSiderealTime = 13.0;
            if (targetPierSide == PierSide.pierWest) {
                rightAscension -= 12.0;
            }
            var coordinates = new Coordinates(Angle.ByHours(rightAscension), Angle.ByDegree(20.0), Epoch.JNOW);

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(true);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                SideOfPier = pierSide,
                Coordinates = coordinates,
                SiderealTime = localSiderealTime,
                Connected = true,
                TrackingEnabled = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(nextItemExpectedTime));

            sut.ShouldTrigger(null, nextItemMock.Object).Should().Be(expectToFlip);
        }

        [Test]
        /* Exposure time is 6 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure still fits in, no flip yet
         */
        [TestCase(5, 5, 10, 8, PierSide.pierWest, false)]
        /* Exposure time is 6 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure still fits in, but the two minute grace period hits
         */
        [TestCase(7, 5, 10, 8, PierSide.pierWest, true)]
        /* Exposure time is 9 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure does not fit, flip needs to start with a wait time
         */
        [TestCase(9, 5, 10, 8, PierSide.pierWest, true)]
        /* Same Test as before, but pier side is already correct, however the pier side should not be considered and a flip is required*/
        [TestCase(9, 5, 10, 8, PierSide.pierEast, true)]
        public void ShouldFlip_BeforeMinimumTime_NoPause_PierSideIsNOTUsed_EvaluateIfFlipIsNecessary(double nextItemExpectedTime, double minTimeToFlip, double maxTimeToFlip, double remainingTimeToFlip, PierSide pierSide, bool expectToFlip) {
            var sut = CreateSUT();

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(false);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                SideOfPier = pierSide,
                Connected = true,
                TrackingEnabled = true
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(nextItemExpectedTime));

            sut.ShouldTrigger(null, nextItemMock.Object).Should().Be(expectToFlip);
        }

        [Test]
        /* Exposure time is 7 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure still fits in, no flip yet
         */
        [TestCase(7, 5, 10, 8, PierSide.pierWest, PierSide.pierEast, false)]
        [TestCase(7, 5, 10, 8, PierSide.pierEast, PierSide.pierWest, false)]
        /* Exposure time is 9 minutes
         * Remaining time to exceed minimum time is 3 minutes
         * Remaining time to exceed maximum time is 8 minutes
         * => The exposure does not fit, flip needs to start with a wait time
         */
        [TestCase(9, 5, 10, 8, PierSide.pierWest, PierSide.pierEast, false)]
        [TestCase(9, 5, 10, 8, PierSide.pierEast, PierSide.pierWest, false)]
        /* Same Test as before, but pier side is already correct and no flip required */
        [TestCase(9, 5, 10, 8, PierSide.pierEast, PierSide.pierEast, false)]
        [TestCase(9, 5, 10, 8, PierSide.pierWest, PierSide.pierWest, false)]
        public void ShouldFlip_NotTracking_NoFlip(
           double nextItemExpectedTime,
           double minTimeToFlip,
           double maxTimeToFlip,
           double remainingTimeToFlip,
           PierSide pierSide,
           PierSide targetPierSide,
           bool expectToFlip) {
            var sut = CreateSUT();

            var rightAscension = 12.9;
            var localSiderealTime = 13.0;
            if (targetPierSide == PierSide.pierWest) {
                rightAscension -= 12.0;
            }
            var coordinates = new Coordinates(Angle.ByHours(rightAscension), Angle.ByDegree(20.0), Epoch.JNOW);

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(minTimeToFlip);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(maxTimeToFlip);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(true);

            profileServiceMock.SetupGet(m => m.ActiveProfile.MeridianFlipSettings).Returns(settings.Object);

            var telescopeInfo = new TelescopeInfo() {
                TimeToMeridianFlip = TimeSpan.FromMinutes(remainingTimeToFlip).TotalHours,
                SideOfPier = pierSide,
                Coordinates = coordinates,
                SiderealTime = localSiderealTime,
                Connected = true,
                TrackingEnabled = false
            };
            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(telescopeInfo);

            var nextItemMock = new Mock<ISequenceItem>();
            nextItemMock.Setup(x => x.GetEstimatedDuration()).Returns(TimeSpan.FromMinutes(nextItemExpectedTime));

            sut.ShouldTrigger(null, nextItemMock.Object).Should().Be(expectToFlip);
        }

        [Test]
        public void LongMaxMinutesAfterMeridian_CorrectSide_NoFlip() {
            // When an object is more than 1 hour (62 minutes) past the meridian cross above the pole, MaxMinutesAfterMeridian is even longer than that (65 minutes),
            // and we're already on the target side of the pier, then the time to flip should be more than 12 hours from no (no flip should be scheduled)
            var lst = 14.0;
            var rightAscension = (TimeSpan.FromHours(lst) - TimeSpan.FromMinutes(62)).TotalHours;
            var coordinates = new Coordinates(Angle.ByHours(rightAscension), Angle.ByDegree(20.0), Epoch.JNOW);
            var expectedSideOfPier = NINA.Astrometry.MeridianFlip.ExpectedPierSide(coordinates, Angle.ByHours(lst));
            var timeToMeridian = NINA.Astrometry.MeridianFlip.TimeToMeridian(coordinates, Angle.ByHours(lst));

            var settings = new Mock<IMeridianFlipSettings>();
            settings.SetupGet(m => m.MinutesAfterMeridian).Returns(55);
            settings.SetupGet(m => m.MaxMinutesAfterMeridian).Returns(65);
            settings.SetupGet(m => m.PauseTimeBeforeMeridian).Returns(0);
            settings.SetupGet(m => m.UseSideOfPier).Returns(true);
            var timeToFlip = NINA.Astrometry.MeridianFlip.TimeToMeridianFlip(settings.Object, coordinates, Angle.ByHours(lst), PierSide.pierEast);

            Assert.That(timeToFlip > TimeSpan.FromHours(12));
        }
    }
}