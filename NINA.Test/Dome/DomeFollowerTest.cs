#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Moq;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Astrometry;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NUnit.Framework.Legacy;

namespace NINA.Test.Dome {

    [TestFixture]
    internal class DomeFollowerTest {
        private Mock<IProfileService> mockProfileService;
        private Mock<ITelescopeMediator> mockTelescopeMediator;
        private Mock<IDomeMediator> mockDomeMediator;
        private Mock<IDomeSynchronization> mockDomeSynchronization;

        private Angle siteLatitude;
        private Angle siteLongitude;
        private double siteElevation;
        private Angle domeTargetAzimuth;
        private Angle domeTargetAltitude;
        private double domeAzimuth;
        private bool synchronizeDuringMountSlew;
        private double domeAzimuthToleranceDegrees;
        private bool useSideOfPier;

        [SetUp]
        public void Init() {
            mockProfileService = new Mock<IProfileService>();
            mockTelescopeMediator = new Mock<ITelescopeMediator>();
            mockDomeMediator = new Mock<IDomeMediator>();
            mockDomeSynchronization = new Mock<IDomeSynchronization>();

            synchronizeDuringMountSlew = false;
            domeAzimuthToleranceDegrees = 1.0;
            domeTargetAzimuth = Angle.ByDegree(0.0);
            domeTargetAltitude = Angle.ByDegree(0.0);

            siteLatitude = Angle.ByDegree(41.5);
            siteLongitude = Angle.ByDegree(-23.2);
            siteElevation = 100;
            useSideOfPier = true;

            mockProfileService.SetupGet(p => p.ActiveProfile.DomeSettings.AzimuthTolerance_degrees).Returns(() => domeAzimuthToleranceDegrees);
            mockProfileService.SetupGet(p => p.ActiveProfile.DomeSettings.SynchronizeDuringMountSlew).Returns(() => synchronizeDuringMountSlew);
            mockProfileService.SetupGet(p => p.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(() => useSideOfPier);
            mockDomeSynchronization
                .Setup(x => x.TargetDomeCoordinates(It.IsAny<Coordinates>(), It.IsAny<double>(), It.IsAny<Angle>(), It.IsAny<Angle>(), It.IsAny<double>(), It.IsAny<PierSide>()))
                .Returns(() => new TopocentricCoordinates(azimuth: domeTargetAzimuth, altitude: domeTargetAltitude, latitude: siteLatitude, longitude: siteLongitude, elevation: siteElevation));
        }

        private DomeFollower CreateSUT() {
            return new DomeFollower(mockProfileService.Object, mockTelescopeMediator.Object, mockDomeMediator.Object, mockDomeSynchronization.Object);
        }

        [Test]
        public void Test_DomeSynchronize_ReceivesCorrectParameters() {
            mockProfileService.SetupGet(p => p.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(() => false);
            var sut = CreateSUT();
            var t1 = new TelescopeInfo() {
                Connected = true,
                SiteLatitude = siteLatitude.Degree,
                SiteLongitude = siteLongitude.Degree,
                SiteElevation = siteElevation,
                SiderealTime = 11.2,
                SideOfPier = PierSide.pierEast,
                Coordinates = new Coordinates(1.0, 2.0, Epoch.J2000, Coordinates.RAType.Degrees)
            };
            var d1 = new DomeInfo() {
                Connected = true,
                Azimuth = 10.0
            };
            sut.UpdateDeviceInfo(t1);
            sut.UpdateDeviceInfo(d1);
            sut.TriggerTelescopeSync();
            mockDomeSynchronization.Verify(x => x.TargetDomeCoordinates(t1.Coordinates, t1.SiderealTime, siteLatitude, siteLongitude, siteElevation, t1.SideOfPier), Times.Once);
        }

        [Test]
        public async Task Test_SlewIfExceedsTolerance() {
            mockProfileService.SetupGet(p => p.ActiveProfile.MeridianFlipSettings.UseSideOfPier).Returns(() => false);
            var sut = CreateSUT();
            domeAzimuth = 0.0;
            domeTargetAzimuth = Angle.ByDegree(2);
            var t1 = new TelescopeInfo() {
                Connected = true,
                SiteLatitude = siteLatitude.Degree,
                SiteLongitude = siteLongitude.Degree,
                SiderealTime = 11.2,
                SideOfPier = PierSide.pierEast,
                Coordinates = new Coordinates(1.0, 2.0, Epoch.J2000, Coordinates.RAType.Degrees)
            };
            var d1 = new DomeInfo() { Connected = true, Azimuth = domeAzimuth };
            mockDomeMediator
                .Setup(x => x.SlewToAzimuth(domeTargetAzimuth.Degree, It.IsAny<CancellationToken>()))
                .Callback<double, CancellationToken>((x, y) => {
                    domeAzimuth = x;
                }).Returns(Task.FromResult(true))
                .Verifiable();

            sut.UpdateDeviceInfo(t1);
            sut.UpdateDeviceInfo(d1);
            await sut.TriggerTelescopeSync();
            await sut.WaitForDomeSynchronization(CancellationToken.None);
            mockDomeMediator.Verify();
            ClassicAssert.AreEqual(domeAzimuth, domeTargetAzimuth.Degree);
        }
    }
}