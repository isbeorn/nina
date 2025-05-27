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
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.Core.Model;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NUnit.Framework.Legacy;

namespace NINA.Test.Dome {

    [TestFixture]
    public class DomeVMTest {
        private Mock<IProfileService> mockProfileService;
        private Mock<IDeviceChooserVM> mockDomeDeviceChooserVM;
        private Mock<IApplicationStatusMediator> mockApplicationStatusMediator;
        private Mock<IDomeMediator> mockDomeMediator;
        private Mock<ITelescopeMediator> mockTelescopeMediator;
        private Mock<ISafetyMonitorMediator> mockSafetyMonitorMediator;
        private Mock<IDomeFollower> mockDomeFollower;
        private Mock<IDeviceUpdateTimerFactory> mockDeviceUpdateTimerFactory;
        private Mock<IDeviceUpdateTimer> mockDeviceUpdateTimer;
        private Mock<IApplicationResourceDictionary> mockResourceDictionary;
        private Mock<IDome> mockDome;

        private string domeId;
        private bool domeConnected;
        private ShutterState domeShutterState;
        private bool domeDriverCanFollow;
        private bool domeCanSetShutter;
        private bool domeCanSetPark;
        private bool domeCanSetAzimuth;
        private bool domeCanSyncAzimuth;
        private bool domeCanPark;
        private bool domeCanFindHome;
        private double domeAzimuth;
        private double domeAltitude;
        private Angle domeTargetAzimuth;
        private Angle domeTargetAltitude;
        private bool domeAtPark;
        private bool domeAtHome;
        private bool domeSlewing;
        private bool domeDriverFollowing;

        [SetUp]
        public void Init() {
            domeId = "ID";
            domeConnected = false;
            domeShutterState = ShutterState.ShutterOpen;
            domeDriverCanFollow = true;
            domeCanSetShutter = true;
            domeCanSetPark = true;
            domeCanSetAzimuth = true;
            domeCanSyncAzimuth = true;
            domeCanPark = true;
            domeCanFindHome = true;
            domeAzimuth = 20.0;
            domeAltitude = 10.0;
            domeTargetAzimuth = Angle.ByDegree(0.0);
            domeTargetAltitude = Angle.ByDegree(0.0);
            domeAtPark = true;
            domeAtHome = false;
            domeSlewing = false;
            domeDriverFollowing = false;

            mockProfileService = new Mock<IProfileService>();
            mockDomeDeviceChooserVM = new Mock<IDeviceChooserVM>();
            mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            mockDomeMediator = new Mock<IDomeMediator>();
            mockTelescopeMediator = new Mock<ITelescopeMediator>();
            mockSafetyMonitorMediator = new Mock<ISafetyMonitorMediator>();
            mockDomeFollower = new Mock<IDomeFollower>();
            mockDomeFollower.Setup(x => x.GetSynchronizedDomeCoordinates(It.IsAny<TelescopeInfo>())).Returns(
                () => new TopocentricCoordinates(azimuth: domeTargetAzimuth, altitude: domeTargetAltitude, latitude: Angle.ByDegree(0), longitude: Angle.ByDegree(0), elevation: 0));
            mockDeviceUpdateTimer = new Mock<IDeviceUpdateTimer>();
            mockDeviceUpdateTimerFactory = new Mock<IDeviceUpdateTimerFactory>();
            mockDeviceUpdateTimerFactory
                .Setup(x => x.Create(It.IsAny<Func<Dictionary<string, object>>>(), It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<double>(), It.IsAny<string>()))
                .Returns(mockDeviceUpdateTimer.Object);
            mockResourceDictionary = new Mock<IApplicationResourceDictionary>();
            mockProfileService.SetupProperty(p => p.ActiveProfile.DomeSettings.Id);
            mockProfileService.SetupGet(p => p.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(1);

            mockApplicationStatusMediator.Setup(x => x.StatusUpdate(It.IsAny<ApplicationStatus>()));
        }

        private async Task<DomeVM> CreateSUT() {
            var domeVM = new DomeVM(mockProfileService.Object, mockDomeMediator.Object, mockApplicationStatusMediator.Object, mockTelescopeMediator.Object,
                mockDomeDeviceChooserVM.Object, mockDomeFollower.Object, mockSafetyMonitorMediator.Object, mockResourceDictionary.Object, mockDeviceUpdateTimerFactory.Object);

            mockDome = new Mock<IDome>();
            mockDome.SetupGet(x => x.Id).Returns(() => domeId);
            mockDome.SetupGet(x => x.Connected).Returns(() => domeConnected);
            mockDome.SetupGet(x => x.ShutterStatus).Returns(() => domeShutterState);
            mockDome.SetupGet(x => x.DriverCanFollow).Returns(() => domeDriverCanFollow);
            mockDome.SetupGet(x => x.CanSetShutter).Returns(() => domeCanSetShutter);
            mockDome.SetupGet(x => x.CanSetPark).Returns(() => domeCanSetPark);
            mockDome.SetupGet(x => x.CanSetAzimuth).Returns(() => domeCanSetAzimuth);
            mockDome.SetupGet(x => x.CanSyncAzimuth).Returns(() => domeCanSyncAzimuth);
            mockDome.SetupGet(x => x.CanPark).Returns(() => domeCanPark);
            mockDome.SetupGet(x => x.CanFindHome).Returns(() => domeCanFindHome);
            mockDome.SetupGet(x => x.Azimuth).Returns(() => domeAzimuth);
            mockDome.SetupGet(x => x.Altitude).Returns(() => domeAltitude);
            mockDome.SetupGet(x => x.AtPark).Returns(() => domeAtPark);
            mockDome.SetupGet(x => x.AtHome).Returns(() => domeAtHome);
            mockDome.SetupGet(x => x.Slewing).Returns(() => domeSlewing);
            mockDome.SetupGet(x => x.DriverFollowing).Returns(() => domeDriverFollowing);
            mockDome.SetupSet(x => x.DriverFollowing = It.IsAny<bool>()).Callback<bool>(v => {
                if (!domeDriverCanFollow) {
                    throw new InvalidOperationException("Dome cannot slave");
                }
                domeDriverFollowing = v;
            });
            mockDome.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Callback<CancellationToken>(ct => {
                domeConnected = true;
            }).ReturnsAsync(true);
            mockDomeDeviceChooserVM.SetupGet(x => x.SelectedDevice).Returns(mockDome.Object);

            var connectionResult = await domeVM.Connect();
            ClassicAssert.IsTrue(connectionResult);
            return domeVM;
        }

        [Test]
        public async Task Test_DomeFollowEnabled_Starts() {
            var sut = await CreateSUT();
            sut.FollowEnabled = true;
            mockDomeFollower.Verify(x => x.Start(), Times.Once);
        }

        [Test]
        public async Task Test_DomeDisconnected_DomeFollowEnabled_NoStart() {
            var sut = await CreateSUT();
            domeConnected = false;
            sut.FollowEnabled = true;
            mockDomeFollower.Verify(x => x.Start(), Times.Never);
        }

        [Test]
        public async Task Test_DomeFollowStops_ToggleSwitchedOff() {
            var sut = await CreateSUT();
            sut.FollowEnabled = true;
            mockDomeFollower.Verify(x => x.Start(), Times.Once);
            mockDomeFollower.SetupGet(f => f.IsFollowing).Returns(false);
            mockDomeFollower.Raise(f => f.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IDomeFollower.IsFollowing)));
            mockDomeFollower.Verify(x => x.Stop(), Times.Once);
        }

        [Test]
        public async Task Test_OpenShutter_IfEnabled() {
            domeCanSetShutter = true;
            domeShutterState = ShutterState.ShutterClosed;
            var sut = await CreateSUT();            
            mockDome.Setup(x => x.OpenShutter(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
            var result = await sut.OpenShutter(CancellationToken.None);
            ClassicAssert.IsTrue(result);
            mockDome.Verify();
        }

        [Test]
        public async Task Test_OpenShutter_NotIfDisabled() {
            domeCanSetShutter = false;
            var sut = await CreateSUT();
            var result = await sut.OpenShutter(CancellationToken.None);
            ClassicAssert.IsFalse(result);
            mockDome.Verify(x => x.OpenShutter(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Test_CloseShutter_IfEnabled() {
            domeCanSetShutter = true;
            var sut = await CreateSUT();
            mockDome.Setup(x => x.CloseShutter(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
            var result = await sut.CloseShutter(CancellationToken.None);
            ClassicAssert.IsTrue(result);
            mockDome.Verify();
        }

        [Test]
        public async Task Test_CloseShutter_NotIfDisabled() {
            domeCanSetShutter = false;
            var sut = await CreateSUT();
            var result = await sut.CloseShutter(CancellationToken.None);
            ClassicAssert.IsFalse(result);
            mockDome.Verify(x => x.CloseShutter(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Test_Park_IfEnabled() {
            domeCanPark = true;
            var sut = await CreateSUT();
            mockDome.Setup(x => x.Park(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
            var result = await sut.Park(CancellationToken.None);
            ClassicAssert.IsTrue(result);
            mockDome.Verify();
        }

        [Test]
        public async Task Test_Park_NotIfDisabled() {
            domeCanPark = false;
            var sut = await CreateSUT();
            var result = await sut.Park(CancellationToken.None);
            ClassicAssert.IsFalse(result);
            mockDome.Verify(x => x.Park(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}