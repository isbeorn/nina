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
using NINA.Core.Locale;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Profile;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;

namespace NINA.Test.FlatDevice {

    [TestFixture]
    public class FlatDeviceVMTest {
        private FlatDeviceVM sut;
        private Mock<IProfileService> mockProfileService;
        private Mock<IFlatDeviceMediator> mockFlatDeviceMediator;
        private Mock<IApplicationStatusMediator> mockApplicationStatusMediator;
        private Mock<IFlatDevice> mockFlatDevice;
        private Mock<IDeviceChooserVM> mockFlatDeviceChooserVM;
        private Mock<IFlatDeviceSettings> mockFlatDeviceSettings;
        private Mock<IFilterWheelSettings> mockFilterWheelSettings;
        private Mock<ICameraMediator> mockCameraMediator;
        private Mock<IImageGeometryProvider> mockImageGeometryProvider;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            mockProfileService = new Mock<IProfileService>();
            mockFlatDeviceMediator = new Mock<IFlatDeviceMediator>();
            mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            mockFlatDevice = new Mock<IFlatDevice>();
            mockFlatDeviceChooserVM = new Mock<IDeviceChooserVM>();
            mockFlatDeviceSettings = new Mock<IFlatDeviceSettings>();
            mockFilterWheelSettings = new Mock<IFilterWheelSettings>();
            mockCameraMediator = new Mock<ICameraMediator>();
            mockImageGeometryProvider = new Mock<IImageGeometryProvider>();
        }

        [SetUp]
        public void Init() {
            mockProfileService.Reset();
            mockFlatDeviceMediator.Reset();
            mockApplicationStatusMediator.Reset();
            mockFlatDevice.Reset();
            mockFlatDeviceChooserVM.Reset();
            mockFlatDeviceSettings.Reset();
            mockFilterWheelSettings.Reset();
            mockCameraMediator.Reset();
            mockImageGeometryProvider.Reset();

            mockFlatDeviceSettings.SetupProperty(m => m.Id, "mockDevice");

            mockProfileService.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings, mockFlatDeviceSettings.Object);
            mockFilterWheelSettings.Setup(m => m.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>());
            mockProfileService.SetupProperty(m => m.ActiveProfile.FilterWheelSettings, mockFilterWheelSettings.Object);
            mockProfileService.Setup(m => m.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(0);
            sut = new FlatDeviceVM(mockProfileService.Object, mockFlatDeviceMediator.Object,
                mockApplicationStatusMediator.Object, mockCameraMediator.Object, mockFlatDeviceChooserVM.Object, mockImageGeometryProvider.Object);
        }

        [Test]
        public async Task TestOpenCoverNullFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            (await sut.OpenCover(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Should().BeFalse();
        }

        [Test]
        public async Task TestOpenCoverNotConnectedFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Connected).Returns(false);
            mockFlatDevice.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            await sut.Connect();
            (await sut.OpenCover(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Should().BeFalse();
        }

        [Test]
        public async Task TestOpenCoverOpenCloseNotSupported() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
            mockFlatDevice.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            await sut.Connect();
            (await sut.OpenCover(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).Should().BeFalse();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestOpenCoverSuccess(bool expected) {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            mockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>(), It.IsAny<int>())).Returns(Task.FromResult(expected));
            mockFlatDevice.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            await sut.Connect();
            (await sut.OpenCover(null, CancellationToken.None)).Should().Be(expected);
        }

        [Test]
        public async Task TestOpenCoverCancelled() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            mockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Callback((CancellationToken ct, int delay) => throw new OperationCanceledException());
            mockFlatDevice.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            await sut.Connect();
            (await sut.OpenCover(null, CancellationToken.None)).Should().BeFalse();
        }

        [Test]
        public async Task TestCloseCoverNullFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            (await sut.CloseCover(null, CancellationToken.None)).Should().BeFalse();
        }

        [Test]
        public async Task TestCloseCoverNotConnectedFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Connected).Returns(false);
            mockFlatDevice.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            await sut.Connect();
            (await sut.CloseCover(null, CancellationToken.None)).Should().BeFalse();
        }

        [Test]
        public async Task TestCloseCoverOpenCloseNotSupported() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
            mockFlatDevice.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            await sut.Connect();
            (await sut.CloseCover(null, CancellationToken.None)).Should().BeFalse();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestCloseCoverSuccess(bool expected) {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            mockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>(), It.IsAny<int>())).Returns(Task.FromResult(expected));
            mockFlatDevice.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            await sut.Connect();
            (await sut.CloseCover(null, CancellationToken.None)).Should().Be(expected);
        }

        [Test]
        public async Task TestCloseCoverCancelled() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            mockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Callback((CancellationToken ct, int delay) => throw new OperationCanceledException());
            mockFlatDevice.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            await sut.Connect();
            (await sut.CloseCover(null, CancellationToken.None)).Should().BeFalse();
        }

        [Test]
        public async Task TestConnectNullDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            (await sut.Connect()).Should().BeFalse();
        }

        [Test]
        public async Task TestConnectDummyDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("No_Device");
            (await sut.Connect()).Should().BeFalse();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestConnectSuccess(bool expected) {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
            (await sut.Connect()).Should().Be(expected);
        }

        [Test]
        public async Task TestConnectCancelled() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken ct) => throw new OperationCanceledException());
            (await sut.Connect()).Should().BeFalse();
        }

        [Test]
        public async Task TestSetBrightnessNullFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            (await sut.SetBrightness(1, null, CancellationToken.None)).Should().Be(false);
            sut.Brightness.Should().Be(0);
            mockFlatDevice.Verify(m => m.Brightness, Times.Never);
        }

        [Test]
        public async Task TestSetBrightnessConnectedFlatDeviceAsync() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.MaxBrightness).Returns(100);
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            await sut.Connect();
            (await sut.SetBrightness(1, null, CancellationToken.None)).Should().Be(true);
            sut.Brightness.Should().Be(0);
            mockFlatDevice.VerifySet(m => m.Brightness = 1, Times.Once);
        }

        [Test]
        public async Task TestSetBrightness_OverMaxValue_Adjusted() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.MaxBrightness).Returns(100);
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            await sut.Connect();
            (await sut.SetBrightness(1000, null, CancellationToken.None)).Should().Be(true);
            sut.Brightness.Should().Be(0);
            mockFlatDevice.VerifySet(m => m.Brightness = 100, Times.Once);
        }

        [Test]
        public async Task TestSetBrightness_UnderMinValue_Adjusted() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.MinBrightness).Returns(100);
            mockFlatDevice.Setup(m => m.MaxBrightness).Returns(1000);
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            await sut.Connect();
            (await sut.SetBrightness(20, null, CancellationToken.None)).Should().Be(true);
            sut.Brightness.Should().Be(0);
            mockFlatDevice.VerifySet(m => m.Brightness = 100, Times.Once);
        }

        [Test]
        public async Task TestToggleLightNullFlatDevice() {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            (await sut.ToggleLight(true, null, CancellationToken.None)).Should().Be(false);
            sut.LightOn.Should().BeFalse();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestToggleLightConnected(bool expected) {
            mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, mockFlatDevice.Object);
            mockFlatDevice.SetupGet(x => x.LightOn).Returns(!expected);
            mockFlatDevice.Setup(m => m.Id).Returns("Something");
            mockFlatDevice.Setup(m => m.Connected).Returns(true);
            mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            await sut.Connect();
            (await sut.ToggleLight(expected, null, CancellationToken.None)).Should().Be(true);
            mockFlatDevice.VerifySet(m => m.LightOn = expected, Times.Once);
        }        
    }
}