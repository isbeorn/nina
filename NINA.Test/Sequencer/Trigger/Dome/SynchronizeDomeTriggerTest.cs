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
using NINA.Astrometry;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger.Dome;
using NINA.Sequencer.Trigger.Platesolving;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Trigger.Dome {

    [TestFixture]
    public class SynchronizeDomeTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IDomeFollower> domeFollowerMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private TelescopeInfo telescopeInfo;
        private DomeInfo domeInfo;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            domeFollowerMock = new Mock<IDomeFollower>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();
            telescopeInfo = TelescopeInfo.CreateDefaultInstance<TelescopeInfo>();
            telescopeInfo.Connected = true;
            telescopeInfo.AtPark = false;
            domeInfo = DomeInfo.CreateDefaultInstance<DomeInfo>();
            domeInfo.Connected = true;

            telescopeMediatorMock.Setup(x => x.GetInfo()).Returns(() => telescopeInfo);
            domeMediatorMock.Setup(x => x.GetInfo()).Returns(() => domeInfo);
            domeFollowerMock.SetupGet(x => x.IsFollowing).Returns(false);
        }

        private SynchronizeDomeTrigger CreateSUT() {
            return new SynchronizeDomeTrigger(profileServiceMock.Object, telescopeMediatorMock.Object, domeMediatorMock.Object, domeFollowerMock.Object, applicationStatusMediatorMock.Object);
        }

        [Test]
        public void CloneTest() {
            var initial = CreateSUT();
            initial.TargetAltitude = 1.0;
            initial.TargetAzimuth = 2.0;
            initial.CurrentAzimuth = 3.0;

            var sut = (SynchronizeDomeTrigger)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.TargetAzimuth.Should().Be(initial.TargetAzimuth);
            sut.TargetAltitude.Should().Be(initial.TargetAltitude);
            sut.CurrentAzimuth.Should().Be(initial.CurrentAzimuth);
        }

        [Test]
        public void ShouldTrigger_ParkedTelescope() {
            telescopeInfo.AtPark = true;

            var sut = CreateSUT();
            var trigger = sut.ShouldTrigger(null, new Mock<ISequenceItem>().Object);
            trigger.Should().Be(false);
        }

        [Test]
        public void ShouldTrigger_DomeFollowerEnabled() {
            domeFollowerMock.SetupGet(x => x.IsFollowing).Returns(true);

            var sut = CreateSUT();
            var trigger = sut.ShouldTrigger(null, new Mock<ISequenceItem>().Object);
            trigger.Should().Be(false);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ShouldTrigger_WithinTolerance(bool withinTolerance) {
            var currentAzimuth = 10.0d;
            domeInfo.Azimuth = currentAzimuth;
            var targetAzimuth = 10.1d;
            var targetAltitude = 30.0d;
            var targetCoordinates = new TopocentricCoordinates(Angle.ByDegree(targetAzimuth), Angle.ByDegree(targetAltitude), Angle.ByDegree(0), Angle.ByDegree(0));
            domeFollowerMock.Setup(x => x.GetSynchronizedDomeCoordinates(It.IsAny<TelescopeInfo>())).Returns(targetCoordinates);
            domeFollowerMock.Setup(x => x.IsDomeWithinTolerance(It.IsAny<Angle>(), It.IsAny<TopocentricCoordinates>())).Returns(withinTolerance);

            var sut = CreateSUT();

            var trigger = sut.ShouldTrigger(null, new Mock<ISequenceItem>().Object);
            trigger.Should().Be(!withinTolerance);

            domeFollowerMock.Verify(x => x.GetSynchronizedDomeCoordinates(It.Is<TelescopeInfo>(t => Object.ReferenceEquals(t, telescopeInfo))), Times.Once);
            domeFollowerMock.Verify(x => x.IsDomeWithinTolerance(
                It.Is<Angle>(a => a.Equals(Angle.ByDegree(currentAzimuth), Angle.ByDegree(0.01d))),
                It.Is<TopocentricCoordinates>(tc => Object.ReferenceEquals(tc, targetCoordinates))), Times.Once);
        }

        [Test]
        public async Task Execute() {
            var sut = CreateSUT();

            var targetAzimuth = 20.0d;
            sut.TargetAzimuth = targetAzimuth;
            var currentAzimuth = 10.0d;
            sut.CurrentAzimuth = currentAzimuth;
            domeInfo.Azimuth = targetAzimuth;

            domeMediatorMock.Setup(x => x.SlewToAzimuth(targetAzimuth, It.IsAny<CancellationToken>())).Returns(Task<bool>.FromResult(true));

            await sut.Execute(null, null, CancellationToken.None);
            domeMediatorMock.VerifyAll();

            sut.CurrentAzimuth.Should().Be(targetAzimuth);
        }
    }
}