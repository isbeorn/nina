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
using NINA.Utility;
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
        private Mock<IFlatDeviceSettings> _mockFlatDeviceSettings;
        private Mock<IFilterWheelSettings> _mockFilterWheelSettings;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _mockProfileService = new Mock<IProfileService>();
            _mockFlatDeviceMediator = new Mock<IFlatDeviceMediator>();
            _mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            _mockFlatDevice = new Mock<IFlatDevice>();
            _mockFlatDeviceChooserVM = new Mock<IFlatDeviceChooserVM>();
            _mockFlatDeviceSettings = new Mock<IFlatDeviceSettings>();
            _mockFilterWheelSettings = new Mock<IFilterWheelSettings>();
        }

        [SetUp]
        public void Init() {
            _mockProfileService.Reset();
            _mockFlatDeviceMediator.Reset();
            _mockApplicationStatusMediator.Reset();
            _mockFlatDevice.Reset();
            _mockFlatDeviceChooserVM.Reset();
            _mockFlatDeviceSettings.Reset();
            _mockFilterWheelSettings.Reset();

            _mockFlatDeviceSettings.SetupProperty(m => m.Id, "mockDevice");

            _mockProfileService.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings, _mockFlatDeviceSettings.Object);
            _mockFilterWheelSettings.Setup(m => m.FilterWheelFilters).Returns(new ObserveAllCollection<FilterInfo>());
            _mockProfileService.SetupProperty(m => m.ActiveProfile.FilterWheelSettings, _mockFilterWheelSettings.Object);
            _mockProfileService.Setup(m => m.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(200);
            //            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.Id).Returns("mockDevice");
            _sut = new FlatDeviceVM(_mockProfileService.Object, _mockFlatDeviceMediator.Object,
                _mockApplicationStatusMediator.Object);
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
        public void TestWizardGridWithoutData() {
            var result = _sut.WizardGrid;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Blocks.Count, Is.EqualTo(0));
        }

        [Test]
        public void TestWizardGridWithoutFilterWheel() {
            const int gain = 30;
            var binningMode = new BinningMode(1, 1);
            var settingsValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
            var settingsKey = new FlatDeviceFilterSettingsKey(null, binningMode, gain);
            _mockProfileService
                        .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                        .Returns(new List<BinningMode> { binningMode });
            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                    .Returns(new List<int> { gain });
            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                        It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(settingsValue);
            _mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));
            var result = _sut.WizardGrid;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Blocks.Count, Is.EqualTo(1));
            Assert.That(result.Blocks[0].Columns.Count, Is.EqualTo(2));
            Assert.That(result.Blocks[0].Binning, Is.EqualTo(binningMode));
            Assert.That(result.Blocks[0].Columns[0].Header, Is.EqualTo(Loc.Instance["LblFilter"]));
            Assert.That(result.Blocks[0].Columns[0].Settings[0].ShowFilterNameOnly, Is.True);
            Assert.That(result.Blocks[0].Columns[0].Settings[0].Key.Position, Is.Null);
            Assert.That(result.Blocks[0].Columns[1].Header, Is.Null);
            Assert.That(result.Blocks[0].Columns[1].Settings[0].ShowFilterNameOnly, Is.False);
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Key, Is.EqualTo(settingsKey));
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Brightness, Is.EqualTo(settingsValue.Brightness));
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Time, Is.EqualTo(settingsValue.Time));
        }

        [Test]
        public void TestWizardGridWithFilters() {
            const int gain = 30;
            const short position = 2;
            var binningMode = new BinningMode(1, 1);
            var settingsValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
            var settingsKey = new FlatDeviceFilterSettingsKey(position, binningMode, gain);

            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                    It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(settingsValue);
            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                .Returns(new List<BinningMode> { binningMode });
            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                                .Returns(new List<int> { gain });
            _mockFilterWheelSettings.Setup(m => m.FilterWheelFilters)
                    .Returns(new ObserveAllCollection<FilterInfo> { new FilterInfo { Position = position } });
            _mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));
            var result = _sut.WizardGrid;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Blocks.Count, Is.EqualTo(1));
            Assert.That(result.Blocks[0].Columns.Count, Is.EqualTo(2));
            Assert.That(result.Blocks[0].Binning, Is.EqualTo(binningMode));
            Assert.That(result.Blocks[0].Columns[0].Header, Is.EqualTo(Loc.Instance["LblFilter"]));
            Assert.That(result.Blocks[0].Columns[0].Settings[0].ShowFilterNameOnly, Is.True);
            Assert.That(result.Blocks[0].Columns[0].Settings[0].Key.Position, Is.EqualTo(position));
            Assert.That(result.Blocks[0].Columns[1].Header, Is.Null);
            Assert.That(result.Blocks[0].Columns[1].Settings[0].ShowFilterNameOnly, Is.False);
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Key, Is.EqualTo(settingsKey));
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Brightness, Is.EqualTo(settingsValue.Brightness));
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Time, Is.EqualTo(settingsValue.Time));
        }

        [Test]
        public void TestWizardGridMustChangeWithNewProfile() {
            const int gain = 30;
            const short position = 2;
            var binningMode = new BinningMode(1, 1);
            var settingsValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);

            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                        It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(settingsValue);
            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                    .Returns(new List<BinningMode> { binningMode });
            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                    .Returns(new List<int> { gain });
            _mockFilterWheelSettings.Setup(m => m.FilterWheelFilters)
                    .Returns(new ObserveAllCollection<FilterInfo> { new FilterInfo { Position = position } });
            _mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));

            var result1 = _sut.WizardGrid;
            Assert.That(result1, Is.Not.Null);
            Assert.That(result1.Blocks.Count, Is.EqualTo(1));
            Assert.That(result1.Blocks[0].Columns[0].Settings[0].Key.Position, Is.EqualTo(position));

            _mockFilterWheelSettings.Setup(m => m.FilterWheelFilters)
            .Returns(new ObserveAllCollection<FilterInfo> { null });
            _mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));

            var result2 = _sut.WizardGrid;
            Assert.That(result2, Is.Not.Null);
            Assert.That(result2.Blocks.Count, Is.EqualTo(1));
            Assert.That(result2.Blocks[0].Columns[0].Settings[0].Key.Position, Is.Null);
        }

        [Test]
        public void TestWizardGridForCamerasWithoutBinning() {
            const int gain = 30;
            const short position = 2;
            var settingsValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
            var settingsKey = new FlatDeviceFilterSettingsKey(position, null, gain);

            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                        It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(settingsValue);
            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                    .Returns(new List<BinningMode> { null });
            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                    .Returns(new List<int> { gain });
            _mockFilterWheelSettings.Setup(m => m.FilterWheelFilters)
                    .Returns(new ObserveAllCollection<FilterInfo> { new FilterInfo { Position = position } });
            _mockFilterWheelSettings.Raise(m => m.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterWheelFilters"));

            var result = _sut.WizardGrid;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Blocks.Count, Is.EqualTo(1));
            Assert.That(result.Blocks[0].Columns.Count, Is.EqualTo(2));
            Assert.That(result.Blocks[0].Binning, Is.Null);
            Assert.That(result.Blocks[0].Columns[0].Header, Is.EqualTo(Loc.Instance["LblFilter"]));
            Assert.That(result.Blocks[0].Columns[0].Settings[0].ShowFilterNameOnly, Is.True);
            Assert.That(result.Blocks[0].Columns[0].Settings[0].Key.Position, Is.EqualTo(position));
            Assert.That(result.Blocks[0].Columns[1].Header, Is.Null);
            Assert.That(result.Blocks[0].Columns[1].Settings[0].ShowFilterNameOnly, Is.False);
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Key, Is.EqualTo(settingsKey));
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Brightness, Is.EqualTo(settingsValue.Brightness));
            Assert.That(result.Blocks[0].Columns[1].Settings[0].Time, Is.EqualTo(settingsValue.Time));
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
        public void TestBinningModesReturnsCorrectName() {
            var binningMode = new BinningMode(1, 1);

            _mockProfileService
            .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
            .Returns(new List<BinningMode> { binningMode });
            var result = _sut.BinningModes;

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result, Is.EquivalentTo(new List<string> { binningMode.Name }));
        }

        [Test]
        public void TestBinningModesReturnsCorrectNameForNullBinningMode() {
            _mockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                    .Returns(new List<BinningMode> { null });
            var result = _sut.BinningModes;

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result, Is.EquivalentTo(new List<string> { Loc.Instance["LblNone"] }));
        }

        [Test]
        public void TestAddGain() {
            var result = string.Empty;
            _mockFlatDeviceSettings.Setup(m =>
                            m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()))
                    .Callback((FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) => { result = key.Gain.ToString(); });
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(_mockFlatDeviceSettings.Object);
            _sut.AddGainCommand.Execute("25");

            _mockFlatDeviceSettings.Verify(m =>
                    m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            Assert.That(result, Is.EqualTo("25"));
        }

        [Test]
        public void TestAddBinning() {
            BinningMode result = null;
            var binning = new BinningMode(1, 1);
            _mockFlatDeviceSettings.Setup(m =>
                            m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()))
                    .Callback((FlatDeviceFilterSettingsKey key, FlatDeviceFilterSettingsValue value) => { result = key.Binning; });
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings).Returns(_mockFlatDeviceSettings.Object);
            _sut.AddBinningCommand.Execute(binning);

            _mockFlatDeviceSettings.Verify(m =>
                    m.AddBrightnessInfo(It.IsAny<FlatDeviceFilterSettingsKey>(), It.IsAny<FlatDeviceFilterSettingsValue>()), Times.Once);
            Assert.That(result, Is.EqualTo(binning));
        }
    }
}