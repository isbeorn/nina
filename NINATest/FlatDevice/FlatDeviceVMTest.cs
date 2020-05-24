#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.FlatDevice;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class FlatDeviceVMTest {
        private FlatDeviceVM _sut;
        private Mock<IProfileService> _mockProfileService;
        private Mock<IFlatDeviceMediator> _mockFlatDeviceMediator;
        private Mock<IApplicationStatusMediator> _mockApplicationStatusMediator;
        private Mock<IFlatDevice> _mockFlatDevice;
        private Mock<IFlatDeviceChooserVM> _mockFlatDeviceChooserVM;
        private Mock<IFilterWheelMediator> _mockFilterWheelMediator;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _mockProfileService = new Mock<IProfileService>();
            _mockFlatDeviceMediator = new Mock<IFlatDeviceMediator>();
            _mockFilterWheelMediator = new Mock<IFilterWheelMediator>();
            _mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            _mockFlatDevice = new Mock<IFlatDevice>();
            _mockFlatDeviceChooserVM = new Mock<IFlatDeviceChooserVM>();
        }

        [SetUp]
        public void Init() {
            _mockProfileService.Reset();
            _mockFlatDeviceMediator.Reset();
            _mockFilterWheelMediator.Reset();
            _mockApplicationStatusMediator.Reset();
            _mockFlatDevice.Reset();
            _mockFlatDeviceChooserVM.Reset();

            _mockProfileService.Setup(m => m.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(200);
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.Id).Returns("mockDevice");
            _sut = new FlatDeviceVM(_mockProfileService.Object, _mockFlatDeviceMediator.Object,
                _mockApplicationStatusMediator.Object, _mockFilterWheelMediator.Object);
        }

        [Test]
        public void TestFilterWheelMediatorRegistered() {
            _mockFilterWheelMediator.Verify(m => m.RegisterConsumer(It.IsAny<FlatDeviceVM>()), Times.Once);
        }

        [Test]
        public async Task TestOpenCoverNullFlatDevice() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.OpenCover(), Is.False);
        }

        [Test]
        public async Task TestOpenCoverNotConnectedFlatDevice() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Connected).Returns(false);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.OpenCover(), Is.False);
        }

        [Test]
        public async Task TestOpenCoverOpenCloseNotSupported() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.OpenCover(), Is.False);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestOpenCoverSuccess(bool expected) {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            _mockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>(), It.IsAny<int>())).Returns(Task.FromResult(expected));
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.OpenCover(), Is.EqualTo(expected));
        }

        [Test]
        public async Task TestOpenCoverCancelled() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            _mockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Callback((CancellationToken ct, int delay) => throw new OperationCanceledException());
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.OpenCover(), Is.False);
        }

        [Test]
        public async Task TestCloseCoverNullFlatDevice() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.CloseCover(), Is.False);
        }

        [Test]
        public async Task TestCloseCoverNotConnectedFlatDevice() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Connected).Returns(false);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.CloseCover(), Is.False);
        }

        [Test]
        public async Task TestCloseCoverOpenCloseNotSupported() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.CloseCover(), Is.False);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestCloseCoverSuccess(bool expected) {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            _mockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>(), It.IsAny<int>())).Returns(Task.FromResult(expected));
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.CloseCover(), Is.EqualTo(expected));
        }

        [Test]
        public async Task TestCloseCoverCancelled() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            _mockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Callback((CancellationToken ct, int delay) => throw new OperationCanceledException());
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.CloseCover(), Is.False);
        }

        [Test]
        public async Task TestConnectNullDevice() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.Connect(), Is.False);
        }

        [Test]
        public async Task TestConnectDummyDevice() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("No_Device");
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.Connect(), Is.False);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestConnectSuccess(bool expected) {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.Connect(), Is.EqualTo(expected));
        }

        [Test]
        public async Task TestConnectCancelled() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken ct) => throw new OperationCanceledException());
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.Connect(), Is.False);
        }

        [Test]
        public void TestWizardTrainedValuesWithoutFilters() {
            _mockProfileService.Raise(m => m.ActiveProfile.FlatDeviceSettings.PropertyChanged += null,
                new PropertyChangedEventArgs("FilterSettings"));
            var result = _sut.WizardTrainedValues;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Columns.Count, Is.EqualTo(1));
            Assert.That(result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));
        }

        [Test]
        public void TestWizardTrainedValuesWithFilters() {
            var returnValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
            const int gainValue = 30;
            const string filterName = "Blue";

            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                    It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(returnValue);
            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                .Returns(new List<BinningMode> { new BinningMode(1, 1) });
            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                .Returns(new List<int> { gainValue });
            _mockFilterWheelMediator.Setup(m => m.GetAllFilters())
                .Returns(new List<FilterInfo>() { new FilterInfo() { Name = filterName } });
            _mockProfileService.Raise(m => m.ActiveProfile.FlatDeviceSettings.PropertyChanged += null,
                new PropertyChangedEventArgs("FilterSettings"));
            var result = _sut.WizardTrainedValues;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Columns.Count, Is.EqualTo(2));
            Assert.That(result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Rows[0][0], Is.EqualTo(filterName));
            Assert.That(result.Rows[0][1],
                Is.EqualTo($"{returnValue.Time,3:0.0}s @ {returnValue.Brightness,3:P0}"));
        }

        [Test]
        public void TestWizardTrainedValuesMustNotChangeWithNewSelectedFilter() {
            var info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
            _sut.UpdateDeviceInfo(info);
            var result1 = _sut.WizardTrainedValues;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result1.Columns.Count, Is.EqualTo(1));
            Assert.That(result1.Rows.Count, Is.EqualTo(1));
            Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));

            info.SelectedFilter = new FilterInfo { Name = "Red" };
            _sut.UpdateDeviceInfo(info);
            var result2 = _sut.WizardTrainedValues;
            Assert.That(result1, Is.EqualTo(result2));
        }

        [Test]
        public void TestWizardTrainedValuesMustChangeWithNewFilterWheel() {
            var info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
            _sut.UpdateDeviceInfo(info);
            var result1 = _sut.WizardTrainedValues;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result1.Columns.Count, Is.EqualTo(1));
            Assert.That(result1.Rows.Count, Is.EqualTo(1));
            Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));

            info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
            _sut.UpdateDeviceInfo(info);
            var result2 = _sut.WizardTrainedValues;
            Assert.That(result1, Is.Not.EqualTo(result2));
        }

        [Test]
        public void TestWizardTrainedValuesForCamerasWithoutBinning() {
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                .Returns((IEnumerable<BinningMode>)new List<BinningMode> { null });
            var info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
            _sut.UpdateDeviceInfo(info);
            var result1 = _sut.WizardTrainedValues;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result1.Columns.Count, Is.EqualTo(1));
            Assert.That(result1.Rows.Count, Is.EqualTo(1));
            Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));
        }

        [Test]
        public void TestSetBrightnessNullFlatDevice() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            _sut.SetBrightness(1.0);
            Assert.That(_sut.Brightness, Is.EqualTo(0d));
            _mockFlatDevice.Verify(m => m.Brightness, Times.Never);
        }

        [Test]
        public async Task TestSetBrightnessConnectedFlatDeviceAsync() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            await _sut.Connect();
            _sut.SetBrightness(1.0);
            Assert.That(_sut.Brightness, Is.EqualTo(0d));
            _mockFlatDevice.VerifySet(m => m.Brightness = 1d, Times.Once);
        }

        [Test]
        public void TestToggleLightNullFlatDevice() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            _sut.ToggleLight(true);
            Assert.That(_sut.LightOn, Is.EqualTo(false));
            _mockFlatDevice.Verify(m => m.LightOn, Times.Never);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestToggleLightConnected(bool expected) {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            await _sut.Connect();
            _sut.ToggleLight(expected);
            _mockFlatDevice.VerifySet(m => m.LightOn = expected, Times.Once);
        }

        [Test]
        public void TestClearWizardTrainedValues() {
            _sut.ClearValuesCommand.Execute(new object());
            _mockProfileService.Verify(m => m.ActiveProfile.FlatDeviceSettings.ClearBrightnessInfo(), Times.Once);
        }

        [Test]
        public void TestDispose() {
            _sut.Dispose();
            _mockFilterWheelMediator.Verify(m => m.RemoveConsumer(It.IsAny<FlatDeviceVM>()), Times.Once);
        }
    }
}