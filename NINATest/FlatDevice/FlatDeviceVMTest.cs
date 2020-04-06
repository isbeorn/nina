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
    [Parallelizable(ParallelScope.All)]
    public class FlatDeviceVMTest {

        private sealed class TestScope : IDisposable {
            public FlatDeviceVM Sut { get; }
            public Mock<IProfileService> MockProfileService { get; }
            public Mock<IFlatDeviceMediator> MockFlatDeviceMediator { get; }
            public Mock<IApplicationStatusMediator> MockApplicationStatusMediator { get; }
            public Mock<IFlatDevice> MockFlatDevice { get; }
            public Mock<IFlatDeviceChooserVM> MockFlatDeviceChooserVM { get; }
            public Mock<IFilterWheelMediator> MockFilterWheelMediator { get; }

            public TestScope() {
                MockProfileService = new Mock<IProfileService>();
                MockProfileService.Setup(m => m.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(200);
                MockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.Id).Returns("mockDevice");
                MockFlatDeviceMediator = new Mock<IFlatDeviceMediator>();
                MockFilterWheelMediator = new Mock<IFilterWheelMediator>();
                MockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
                MockFlatDevice = new Mock<IFlatDevice>();
                MockFlatDeviceChooserVM = new Mock<IFlatDeviceChooserVM>();
                Sut = new FlatDeviceVM(MockProfileService.Object, MockFlatDeviceMediator.Object,
                    MockApplicationStatusMediator.Object, MockFilterWheelMediator.Object);
            }

            public async void Dispose() {
                await Sut.Disconnect();
            }
        }

        [SetUp]
        public void Init() {
        }

        [Test]
        public void TestFilterWheelMediatorRegistered() {
            using (var scope = new TestScope()) {
                scope.MockFilterWheelMediator.Verify(m => m.RegisterConsumer(It.IsAny<FlatDeviceVM>()), Times.Once);
            }
        }

        [Test]
        public async Task TestOpenCoverNullFlatDevice() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.OpenCover(), Is.False);
            }
        }

        [Test]
        public async Task TestOpenCoverNotConnectedFlatDevice() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(false);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.OpenCover(), Is.False);
            }
        }

        [Test]
        public async Task TestOpenCoverOpenCloseNotSupported() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(true);
                scope.MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.OpenCover(), Is.False);
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestOpenCoverSuccess(bool expected) {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("Something");
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(true);
                scope.MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
                scope.MockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.OpenCover(), Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task TestOpenCoverCancelled() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("Something");
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(true);
                scope.MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
                scope.MockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>()))
                    .Callback((CancellationToken ct) => throw new OperationCanceledException());
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.OpenCover(), Is.False);
            }
        }

        [Test]
        public async Task TestCloseCoverNullFlatDevice() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.CloseCover(), Is.False);
            }
        }

        [Test]
        public async Task TestCloseCoverNotConnectedFlatDevice() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(false);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.CloseCover(), Is.False);
            }
        }

        [Test]
        public async Task TestCloseCoverOpenCloseNotSupported() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(true);
                scope.MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(false);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.CloseCover(), Is.False);
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestCloseCoverSuccess(bool expected) {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("Something");
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(true);
                scope.MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
                scope.MockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.CloseCover(), Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task TestCloseCoverCancelled() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("Something");
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(true);
                scope.MockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
                scope.MockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>()))
                    .Callback((CancellationToken ct) => throw new OperationCanceledException());
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.CloseCover(), Is.False);
            }
        }

        [Test]
        public async Task TestConnectNullDevice() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.Connect(), Is.False);
            }
        }

        [Test]
        public async Task TestConnectDummyDevice() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("No_Device");
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.Connect(), Is.False);
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestConnectSuccess(bool expected) {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("Something");
                scope.MockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(expected));
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.Connect(), Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task TestConnectCancelled() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("Something");
                scope.MockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>()))
                    .Callback((CancellationToken ct) => throw new OperationCanceledException());
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                Assert.That(await scope.Sut.Connect(), Is.False);
            }
        }

        [Test]
        public void TestWizardTrainedValuesWithoutFilters() {
            using (var scope = new TestScope()) {
                scope.MockProfileService.Raise(m => m.ActiveProfile.FlatDeviceSettings.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterSettings"));
                var result = scope.Sut.WizardTrainedValues;
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Columns.Count, Is.EqualTo(1));
                Assert.That(result.Rows.Count, Is.EqualTo(1));
                Assert.That(result.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));
            }
        }

        [Test]
        public void TestWizardTrainedValuesWithFilters() {
            using (var scope = new TestScope()) {
                var returnValue = new FlatDeviceFilterSettingsValue(0.7, 0.5);
                int gainValue = 30;
                const string filterName = "Blue";

                scope.MockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                        It.IsAny<FlatDeviceFilterSettingsKey>())).Returns(returnValue);
                scope.MockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                    .Returns(new List<BinningMode> { new BinningMode(1, 1) });
                scope.MockProfileService
                    .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                    .Returns(new List<int> { gainValue });
                scope.MockFilterWheelMediator.Setup(m => m.GetAllFilters())
                    .Returns(new List<FilterInfo>() { new FilterInfo() { Name = filterName } });
                scope.MockProfileService.Raise(m => m.ActiveProfile.FlatDeviceSettings.PropertyChanged += null,
                    new PropertyChangedEventArgs("FilterSettings"));
                var result = scope.Sut.WizardTrainedValues;
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Columns.Count, Is.EqualTo(2));
                Assert.That(result.Rows.Count, Is.EqualTo(1));
                Assert.That(result.Rows[0][0], Is.EqualTo(filterName));
                Assert.That(result.Rows[0][1],
                    Is.EqualTo($"{returnValue.Time,3:0.0}s @ {returnValue.Brightness,3:P0}"));
            }
        }

        [Test]
        public void TestWizardTrainedValuesMustNotChangeWithNewSelectedFilter() {
            using (var scope = new TestScope()) {
                var info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
                scope.Sut.UpdateDeviceInfo(info);
                var result1 = scope.Sut.WizardTrainedValues;

                Assert.That(result1, Is.Not.Null);
                Assert.That(result1.Columns.Count, Is.EqualTo(1));
                Assert.That(result1.Rows.Count, Is.EqualTo(1));
                Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));

                info.SelectedFilter = new FilterInfo { Name = "Red" };
                scope.Sut.UpdateDeviceInfo(info);
                var result2 = scope.Sut.WizardTrainedValues;
                Assert.That(result1, Is.EqualTo(result2));
            }
        }

        [Test]
        public void TestWizardTrainedValuesMustChangeWithNewFilterWheel() {
            using (var scope = new TestScope()) {
                var info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
                scope.Sut.UpdateDeviceInfo(info);
                var result1 = scope.Sut.WizardTrainedValues;

                Assert.That(result1, Is.Not.Null);
                Assert.That(result1.Columns.Count, Is.EqualTo(1));
                Assert.That(result1.Rows.Count, Is.EqualTo(1));
                Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));

                info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
                scope.Sut.UpdateDeviceInfo(info);
                var result2 = scope.Sut.WizardTrainedValues;
                Assert.That(result1, Is.Not.EqualTo(result2));
            }
        }

        [Test]
        public void TestWizardTrainedValuesForCamerasWithoutBinning() {
            using (var scope = new TestScope()) {
                scope.MockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                    .Returns((IEnumerable<BinningMode>)new List<BinningMode> { null });
                var info = new FilterWheelInfo { SelectedFilter = new FilterInfo { Name = "Clear" } };
                scope.Sut.UpdateDeviceInfo(info);
                var result1 = scope.Sut.WizardTrainedValues;

                Assert.That(result1, Is.Not.Null);
                Assert.That(result1.Columns.Count, Is.EqualTo(1));
                Assert.That(result1.Rows.Count, Is.EqualTo(1));
                Assert.That(result1.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));
            }
        }

        [Test]
        public void TestSetBrightnessNullFlatDevice() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                scope.Sut.SetBrightness(1.0);
                Assert.That(scope.Sut.Brightness, Is.EqualTo(0d));
                scope.MockFlatDevice.Verify(m => m.Brightness, Times.Never);
            }
        }

        [Test]
        public async Task TestSetBrightnessConnectedFlatDeviceAsync() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("Something");
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(true);
                scope.MockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                await scope.Sut.Connect();
                scope.Sut.SetBrightness(1.0);
                Assert.That(scope.Sut.Brightness, Is.EqualTo(0d));
                scope.MockFlatDevice.VerifySet(m => m.Brightness = 1d, Times.Once);
            }
        }

        [Test]
        public void TestToggleLightNullFlatDevice() {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, null);
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                scope.Sut.ToggleLight(true);
                Assert.That(scope.Sut.LightOn, Is.EqualTo(false));
                scope.MockFlatDevice.Verify(m => m.LightOn, Times.Never);
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestToggleLightConnected(bool expected) {
            using (var scope = new TestScope()) {
                scope.MockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, scope.MockFlatDevice.Object);
                scope.MockFlatDevice.Setup(m => m.Id).Returns("Something");
                scope.MockFlatDevice.Setup(m => m.Connected).Returns(true);
                scope.MockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
                scope.Sut.FlatDeviceChooserVM = scope.MockFlatDeviceChooserVM.Object;
                await scope.Sut.Connect();
                scope.Sut.ToggleLight(expected);
                scope.MockFlatDevice.VerifySet(m => m.LightOn = expected, Times.Once);
            }
        }

        [Test]
        public void TestClearWizardTrainedValues() {
            using (var scope = new TestScope()) {
                scope.Sut.ClearValuesCommand.Execute(new object());
                scope.MockProfileService.Verify(m => m.ActiveProfile.FlatDeviceSettings.ClearBrightnessInfo(), Times.Once);
            }
        }

        [Test]
        public void TestDispose() {
            using (var scope = new TestScope()) {
                scope.Sut.Dispose();
                scope.MockFilterWheelMediator.Verify(m => m.RemoveConsumer(It.IsAny<FlatDeviceVM>()), Times.Once);
            }
        }
    }
}