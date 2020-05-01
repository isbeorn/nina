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
        private FlatDeviceVM Sut { get; set; }
        private Mock<IProfileService> MockProfileService { get; set; }
        private Mock<IFlatDeviceMediator> MockFlatDeviceMediator { get; set; }
        private Mock<IApplicationStatusMediator> MockApplicationStatusMediator { get; set; }
        private Mock<IFlatDevice> MockFlatDevice { get; set; }
        private Mock<IFlatDeviceChooserVM> MockFlatDeviceChooserVM { get; set; }
        private Mock<IFilterWheelMediator> MockFilterWheelMediator { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            MockProfileService = new Mock<IProfileService>();
            MockFlatDeviceMediator = new Mock<IFlatDeviceMediator>();
            MockFilterWheelMediator = new Mock<IFilterWheelMediator>();
            MockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            MockFlatDevice = new Mock<IFlatDevice>();
            MockFlatDeviceChooserVM = new Mock<IFlatDeviceChooserVM>();
        }

        [SetUp]
        public void Init() {
            MockProfileService.Reset();
            MockFlatDeviceMediator.Reset();
            MockFilterWheelMediator.Reset();
            MockApplicationStatusMediator.Reset();
            MockFlatDevice.Reset();
            MockFlatDeviceChooserVM.Reset();

            MockProfileService.Setup(m => m.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(200);
            MockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.Id).Returns("mockDevice");
            Sut = new FlatDeviceVM(MockProfileService.Object, MockFlatDeviceMediator.Object,
                MockApplicationStatusMediator.Object, MockFilterWheelMediator.Object);
        }

        [Test]
        public void TestFilterWheelMediatorRegistered() {
            MockFilterWheelMediator.Verify(m => m.RegisterConsumer(It.IsAny<FlatDeviceVM>()), Times.Once);
        }

        [Test]
        public async Task TestOpenCoverNullFlatDevice() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.OpenCover(), Is.False);
        }

        [Test]
        public async Task TestOpenCoverNotConnectedFlatDevice() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Connected).Returns(false);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.OpenCover(), Is.False);
        }

        [Test]
        public async Task TestOpenCoverOpenCloseNotSupported() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Connected).Returns(true);
            MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.OpenCover(), Is.False);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestOpenCoverSuccess(bool expected) {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("Something");
            MockFlatDevice.Setup(m => m.Connected).Returns(true);
            MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            MockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.OpenCover(), Is.EqualTo(expected));
        }

        [Test]
        public async Task TestOpenCoverCancelled() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("Something");
            MockFlatDevice.Setup(m => m.Connected).Returns(true);
            MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            MockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken ct) => throw new OperationCanceledException());
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.OpenCover(), Is.False);
        }

        [Test]
        public async Task TestCloseCoverNullFlatDevice() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.CloseCover(), Is.False);
        }

        [Test]
        public async Task TestCloseCoverNotConnectedFlatDevice() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Connected).Returns(false);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.CloseCover(), Is.False);
        }

        [Test]
        public async Task TestCloseCoverOpenCloseNotSupported() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Connected).Returns(true);
            MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.CloseCover(), Is.False);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestCloseCoverSuccess(bool expected) {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("Something");
            MockFlatDevice.Setup(m => m.Connected).Returns(true);
            MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            MockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.CloseCover(), Is.EqualTo(expected));
        }

        [Test]
        public async Task TestCloseCoverCancelled() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("Something");
            MockFlatDevice.Setup(m => m.Connected).Returns(true);
            MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            MockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken ct) => throw new OperationCanceledException());
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.CloseCover(), Is.False);
        }

        [Test]
        public async Task TestConnectNullDevice() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.Connect(), Is.False);
        }

        [Test]
        public async Task TestConnectDummyDevice() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("No_Device");
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.Connect(), Is.False);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestConnectSuccess(bool expected) {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("Something");
            MockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.Connect(), Is.EqualTo(expected));
        }

        [Test]
        public async Task TestConnectCancelled() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("Something");
            MockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken ct) => throw new OperationCanceledException());
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Assert.That(await Sut.Connect(), Is.False);
        }

        [Test]
        public void TestWizardTrainedValuesWithoutFilters() {
            MockProfileService.Raise(m => m.ActiveProfile.FlatDeviceSettings.PropertyChanged += null,
                new PropertyChangedEventArgs("FilterSettings"));
            var result = Sut.WizardTrainedValues;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Columns.Count, Is.EqualTo(1));
            Assert.That(result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));
        }

        [Test]
        public void TestWizardTrainedValuesWithFilters() {
            var returnValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
            int gainValue = 30;
            const string filterName = "Blue";

            MockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                    It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(returnValue);
            MockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                .Returns(new List<BinningMode> { new BinningMode(1, 1) });
            MockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                .Returns(new List<int> { gainValue });
            MockFilterWheelMediator.Setup(m => m.GetAllFilters())
                .Returns(new List<FilterInfo>() { new FilterInfo() { Name = filterName } });
            MockProfileService.Raise(m => m.ActiveProfile.FlatDeviceSettings.PropertyChanged += null,
                new PropertyChangedEventArgs("FilterSettings"));
            var result = Sut.WizardTrainedValues;
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
            Sut.UpdateDeviceInfo(info);
            var result1 = Sut.WizardTrainedValues;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result1.Columns.Count, Is.EqualTo(1));
            Assert.That(result1.Rows.Count, Is.EqualTo(1));
            Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));

            info.SelectedFilter = new FilterInfo { Name = "Red" };
            Sut.UpdateDeviceInfo(info);
            var result2 = Sut.WizardTrainedValues;
            Assert.That(result1, Is.EqualTo(result2));
        }

        [Test]
        public void TestWizardTrainedValuesMustChangeWithNewFilterWheel() {
            var info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
            Sut.UpdateDeviceInfo(info);
            var result1 = Sut.WizardTrainedValues;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result1.Columns.Count, Is.EqualTo(1));
            Assert.That(result1.Rows.Count, Is.EqualTo(1));
            Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));

            info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
            Sut.UpdateDeviceInfo(info);
            var result2 = Sut.WizardTrainedValues;
            Assert.That(result1, Is.Not.EqualTo(result2));
        }

        [Test]
        public void TestWizardTrainedValuesForCamerasWithoutBinning() {
            MockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                .Returns((IEnumerable<BinningMode>)new List<BinningMode> { null });
            var info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
            Sut.UpdateDeviceInfo(info);
            var result1 = Sut.WizardTrainedValues;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result1.Columns.Count, Is.EqualTo(1));
            Assert.That(result1.Rows.Count, Is.EqualTo(1));
            Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));
        }

        [Test]
        public void TestSetBrightnessNullFlatDevice() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Sut.SetBrightness(1.0);
            Assert.That(Sut.Brightness, Is.EqualTo(0d));
            MockFlatDevice.Verify(m => m.Brightness, Times.Never);
        }

        [Test]
        public async Task TestSetBrightnessConnectedFlatDeviceAsync() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("Something");
            MockFlatDevice.Setup(m => m.Connected).Returns(true);
            MockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            await Sut.Connect();
            Sut.SetBrightness(1.0);
            Assert.That(Sut.Brightness, Is.EqualTo(0d));
            MockFlatDevice.VerifySet(m => m.Brightness = 1d, Times.Once);
        }

        [Test]
        public void TestToggleLightNullFlatDevice() {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            Sut.ToggleLight(true);
            Assert.That(Sut.LightOn, Is.EqualTo(false));
            MockFlatDevice.Verify(m => m.LightOn, Times.Never);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestToggleLightConnected(bool expected) {
            MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, MockFlatDevice.Object);
            MockFlatDevice.Setup(m => m.Id).Returns("Something");
            MockFlatDevice.Setup(m => m.Connected).Returns(true);
            MockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            Sut.FlatDeviceChooserVM = MockFlatDeviceChooserVM.Object;
            await Sut.Connect();
            Sut.ToggleLight(expected);
            MockFlatDevice.VerifySet(m => m.LightOn = expected, Times.Once);
        }

        [Test]
        public void TestClearWizardTrainedValues() {
            Sut.ClearValuesCommand.Execute(new object());
            MockProfileService.Verify(m => m.ActiveProfile.FlatDeviceSettings.ClearBrightnessInfo(), Times.Once);
        }

        [Test]
        public void TestDispose() {
            Sut.Dispose();
            MockFilterWheelMediator.Verify(m => m.RemoveConsumer(It.IsAny<FlatDeviceVM>()), Times.Once);
        }
    }
}