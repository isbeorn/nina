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
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Profile.Interfaces;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;

namespace NINA.Test.Focuser {

    [TestFixture]
    internal class FocuserVMTest {
        private Mock<IProfileService> mockProfileService;
        private Mock<IFocuserMediator> mockFocuserMediator;
        private Mock<IApplicationStatusMediator> mockApplicationStatusMediator;
        private Mock<IDeviceChooserVM> mockFocuserChooserVm;
        private Mock<IImageGeometryProvider> mockImageGeometryProvider;
        private Mock<IFocuser> mockFocuser;
        private FocuserVM sut;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            mockProfileService = new Mock<IProfileService>();
            mockFocuserMediator = new Mock<IFocuserMediator>();
            mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            mockFocuserChooserVm = new Mock<IDeviceChooserVM>();
            mockImageGeometryProvider = new Mock<IImageGeometryProvider>();
            mockFocuser = new Mock<IFocuser>();
        }

        [SetUp]
        public void Init() {
            mockProfileService.Reset();
            mockFocuserMediator.Reset();
            mockApplicationStatusMediator.Reset();
            mockFocuserChooserVm.Reset();
            mockImageGeometryProvider.Reset();
            mockFocuser.Reset();

            mockProfileService.Setup(m => m.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(2);

            mockFocuser.Setup(m => m.Move(It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Callback((int position, CancellationToken ct, int waitInMs) => {
                    mockFocuser.Setup(m => m.Position).Returns(position);
                });

            sut = new FocuserVM(mockProfileService.Object, mockFocuserMediator.Object,
                mockApplicationStatusMediator.Object, mockFocuserChooserVm.Object, mockImageGeometryProvider.Object);
        }

        [Test]
        public void TestConstructor() {
            mockProfileService.SetupAdd(m => m.ProfileChanged += It.IsAny<EventHandler>());

            sut.Should().NotBeNull();
            sut.DeviceChooserVM.Should().Be(mockFocuserChooserVm.Object);
            sut.RescanDevicesCommand.Should().NotBeNull();
            sut.ConnectCommand.Should().NotBeNull();
            sut.CancelConnectCommand.Should().NotBeNull();
            sut.DisconnectCommand.Should().NotBeNull();
            sut.MoveFocuserCommand.Should().NotBeNull();
            sut.MoveFocuserInSmallCommand.Should().NotBeNull();
            sut.MoveFocuserInLargeCommand.Should().NotBeNull();
            sut.MoveFocuserOutSmallCommand.Should().NotBeNull();
            sut.MoveFocuserOutLargeCommand.Should().NotBeNull();
            sut.HaltFocuserCommand.Should().NotBeNull();
            sut.ToggleTempCompCommand.Should().NotBeNull();
            mockProfileService.VerifyAdd(m => m.ProfileChanged += It.IsAny<EventHandler>(), Times.Once);
        }

        [Test]
        public async Task TestConnectWithDummyDevice() {
            mockProfileService.SetupProperty(m => m.ActiveProfile.FocuserSettings.Id, "");
            mockFocuserChooserVm.Setup(m => m.SelectedDevice.Id).Returns("No_Device");

            var result = await sut.Connect();

            result.Should().BeFalse();
            sut.Focuser.Should().BeNull();
            mockProfileService.VerifySet(m => m.ActiveProfile.FocuserSettings.Id = "No_Device");
        }

        [Test]
        public async Task ConnectNotConnected() {
            mockProfileService.Setup(m => m.ActiveProfile.FocuserSettings.BacklashCompensationModel)
                .Returns(BacklashCompensationModel.ABSOLUTE);
            mockFocuserChooserVm.Setup(m => m.SelectedDevice).Returns(mockFocuser.Object);
            mockFocuser.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            var result = await sut.Connect();

            result.Should().BeFalse();
            sut.Focuser.Should().BeNull();
        }

        [Test]
        public async Task ConnectCancelled() {
            mockProfileService.Setup(m => m.ActiveProfile.FocuserSettings.BacklashCompensationModel)
                .Returns(BacklashCompensationModel.ABSOLUTE);
            mockFocuserChooserVm.Setup(m => m.SelectedDevice).Returns(mockFocuser.Object);
            mockFocuser.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Throws(new OperationCanceledException());

            var result = await sut.Connect();

            result.Should().BeFalse();
            sut.Focuser.Should().BeNull();
        }

        [Test]
        public async Task ConnectNullFocuser() {
            mockFocuserChooserVm.Setup(m => m.SelectedDevice).Returns((IDevice)null);

            var result = await sut.Connect();

            result.Should().BeFalse();
            sut.Focuser.Should().BeNull();
        }

        [Test]
        public async Task TestConnectAbsolute() {
            mockProfileService.Setup(m => m.ActiveProfile.FocuserSettings.BacklashCompensationModel)
                .Returns(BacklashCompensationModel.ABSOLUTE);
            mockFocuserChooserVm.Setup(m => m.SelectedDevice).Returns(mockFocuser.Object);
            mockFocuser.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            var result = await sut.Connect();

            result.Should().BeTrue();
            sut.Focuser.Should().NotBeNull();
            sut.Focuser.GetType().Should().Be(typeof(AbsoluteBacklashCompensationDecorator));
        }

        [Test]
        public async Task TestConnectOvershoot() {
            mockProfileService.Setup(m => m.ActiveProfile.FocuserSettings.BacklashCompensationModel)
                .Returns(BacklashCompensationModel.OVERSHOOT);
            mockFocuserChooserVm.Setup(m => m.SelectedDevice).Returns(mockFocuser.Object);
            mockFocuser.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            var result = await sut.Connect();

            result.Should().BeTrue();
            sut.Focuser.Should().NotBeNull();
            sut.Focuser.GetType().Should().Be(typeof(OvershootBacklashCompensationDecorator));
        }

        [Test]
        [TestCase(true, 0, 1d, true, true, 0d)]
        [TestCase(false, 0, 1d, true, true, 0d)]
        [TestCase(true, 1000, 1d, true, true, 0d)]
        [TestCase(true, 0, 0.63, true, true, 0d)]
        [TestCase(true, 0, 1d, false, true, 0d)]
        [TestCase(true, 0, 1d, true, false, 0d)]
        [TestCase(true, 0, 1d, true, true, 10d)]
        [TestCase(true, 0, 1d, true, true, -20d)]
        public async Task TestConnectFocuserValues(bool isMoving, int position, double stepSize,
            bool tempCompAvailable, bool tempComp, double temperature) {
            mockProfileService.Setup(m => m.ActiveProfile.FocuserSettings.BacklashCompensationModel)
                .Returns(BacklashCompensationModel.OVERSHOOT);
            mockFocuserChooserVm.Setup(m => m.SelectedDevice).Returns(mockFocuser.Object);
            mockFocuser.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mockFocuser.Setup(m => m.IsMoving).Returns(isMoving);
            mockFocuser.Setup(m => m.Name).Returns("TestFocuserName");
            mockFocuser.Setup(m => m.Position).Returns(position);
            mockFocuser.Setup(m => m.StepSize).Returns(stepSize);
            mockFocuser.Setup(m => m.TempCompAvailable).Returns(tempCompAvailable);
            mockFocuser.Setup(m => m.TempComp).Returns(tempComp);
            mockFocuser.Setup(m => m.Temperature).Returns(temperature);

            var result = await sut.Connect();

            result.Should().BeTrue();
            sut.Focuser.Should().NotBeNull();
            sut.FocuserInfo.IsMoving.Should().Be(isMoving);
            sut.FocuserInfo.Name.Should().Be("TestFocuserName");
            sut.FocuserInfo.Position.Should().Be(position);
            sut.FocuserInfo.StepSize.Should().Be(stepSize);
            sut.FocuserInfo.TempCompAvailable.Should().Be(tempCompAvailable);
            sut.FocuserInfo.TempComp.Should().Be(tempComp);
            sut.FocuserInfo.Temperature.Should().Be(temperature);

            var deviceInfo = sut.GetDeviceInfo();
            deviceInfo.Should().NotBeNull();
            deviceInfo.IsMoving.Should().Be(isMoving);
            deviceInfo.Name.Should().Be("TestFocuserName");
            deviceInfo.Position.Should().Be(position);
            deviceInfo.StepSize.Should().Be(stepSize);
            deviceInfo.TempCompAvailable.Should().Be(tempCompAvailable);
            deviceInfo.TempComp.Should().Be(tempComp);
            deviceInfo.Temperature.Should().Be(temperature);
        }
    }
}