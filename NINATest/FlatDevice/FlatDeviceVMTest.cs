using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.FlatDevice;
using NUnit.Framework;

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

        [SetUp]
        public void Init() {
            _mockProfileService = new Mock<IProfileService>();
            _mockProfileService.Setup(m => m.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(200);
            _mockProfileService.Setup(m => m.ActiveProfile.FlatDeviceSettings.Id).Returns("mockDevice");
            _mockFlatDeviceMediator = new Mock<IFlatDeviceMediator>();
            _mockFilterWheelMediator = new Mock<IFilterWheelMediator>();
            _mockApplicationStatusMediator = new Mock<IApplicationStatusMediator>();
            _mockFlatDevice = new Mock<IFlatDevice>();
            _mockFlatDeviceChooserVM = new Mock<IFlatDeviceChooserVM>();
            _sut = new FlatDeviceVM(_mockProfileService.Object, _mockFlatDeviceMediator.Object,
                _mockApplicationStatusMediator.Object, _mockFilterWheelMediator.Object);
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
            _mockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>())).Returns(Task.Run(() => expected));
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.OpenCover(), Is.EqualTo(expected));
        }

        [Test]
        public async Task TestOpenCoverCancelled() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            _mockFlatDevice.Setup(m => m.Open(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken ct) => throw new OperationCanceledException());
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
            _mockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>())).Returns(Task.Run(() => expected));
            _sut.FlatDeviceChooserVM = _mockFlatDeviceChooserVM.Object;
            Assert.That(await _sut.CloseCover(), Is.EqualTo(expected));
        }

        [Test]
        public async Task TestCloseCoverCancelled() {
            _mockFlatDeviceChooserVM.SetupProperty(m => m.SelectedDevice, _mockFlatDevice.Object);
            _mockFlatDevice.Setup(m => m.Id).Returns("Something");
            _mockFlatDevice.Setup(m => m.Connected).Returns(true);
            _mockFlatDevice.Setup(m => m.SupportsOpenClose).Returns(true);
            _mockFlatDevice.Setup(m => m.Close(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken ct) => throw new OperationCanceledException());
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
            _mockFlatDevice.Setup(m => m.Connect(It.IsAny<CancellationToken>())).Returns(Task.Run(() => expected));
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
            var result = _sut.WizardTrainedValues;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Columns.Count, Is.EqualTo(1));
            Assert.That(result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Rows[0][0], Is.EqualTo(Loc.Instance["LblNoFilterwheel"]));
        }

        [Test]
        public void TestWizardTrainedValuesWithFilters() {
            (double time, double brightness) returnValue = (0.5, 0.7);
            short gainValue = 30;
            const string filterName = "Blue";

            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfo(
                    It.IsAny<(string name, BinningMode binning, short gain)>())).Returns(returnValue);
            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoBinnings())
                .Returns(new List<BinningMode> { new BinningMode(1, 1) });
            _mockProfileService
                .Setup(m => m.ActiveProfile.FlatDeviceSettings.GetBrightnessInfoGains())
                .Returns(new List<short> { gainValue });
            _mockFilterWheelMediator.Setup(m => m.GetAllFilters())
                .Returns(new List<FilterInfo>() { new FilterInfo() { Name = filterName } });

            var result = _sut.WizardTrainedValues;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Columns.Count, Is.EqualTo(2));
            Assert.That(result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Rows[0][0], Is.EqualTo(filterName));
            Assert.That(result.Rows[0][1], Is.EqualTo($"{returnValue.time,3:0.0}s @ {returnValue.brightness,3:P0}"));
        }
    }
}