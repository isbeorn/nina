using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Explicit("Creates a WPF Application dispatcher; run CameraVMTest in an isolated process.")]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class CameraVMTest {
        private Mock<IProfileService> profileService;
        private Mock<IProfile> profile;
        private Mock<IApplicationSettings> applicationSettings;
        private Mock<ICameraSettings> cameraSettings;
        private Mock<ICameraMediator> cameraMediator;
        private Mock<IFilterWheelMediator> filterWheelMediator;
        private Mock<IApplicationStatusMediator> applicationStatusMediator;
        private Mock<IDeviceChooserVM> deviceChooser;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();

            profileService = new Mock<IProfileService>();
            profile = new Mock<IProfile>();
            applicationSettings = new Mock<IApplicationSettings>();
            cameraSettings = new Mock<ICameraSettings>();
            cameraMediator = new Mock<ICameraMediator>();
            filterWheelMediator = new Mock<IFilterWheelMediator>();
            applicationStatusMediator = new Mock<IApplicationStatusMediator>();
            deviceChooser = new Mock<IDeviceChooserVM>();

            applicationSettings.SetupProperty(x => x.DevicePollingInterval, 0);
            cameraSettings.SetupProperty(x => x.Id, string.Empty);
            cameraSettings.SetupProperty(x => x.LastDeviceName, string.Empty);
            cameraSettings.SetupProperty(x => x.PixelSize, 0);
            cameraSettings.SetupProperty(x => x.Gain, null);
            cameraSettings.SetupProperty(x => x.Offset, null);
            cameraSettings.SetupProperty(x => x.USBLimit, null);
            cameraSettings.SetupProperty(x => x.CoolingDuration, 2);
            cameraSettings.SetupProperty(x => x.WarmingDuration, 3);
            cameraSettings.SetupProperty(x => x.Temperature, -15);
            profile.SetupGet(x => x.ApplicationSettings).Returns(applicationSettings.Object);
            profile.SetupGet(x => x.CameraSettings).Returns(cameraSettings.Object);
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            deviceChooser.SetupGet(x => x.Devices).Returns(new List<IDevice>());
            deviceChooser.Setup(x => x.GetEquipment()).Returns(Task.CompletedTask);
            filterWheelMediator.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo());
        }

        [Test]
        [TestCase(0, -10)]
        [TestCase(-20, -10)]
        public async Task CoolCamera_WhenTemperatureStartsOutsideTolerance_WaitsInEitherDirection(double currentTemperature, double targetTemperature) {
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            camera.SetupGet(x => x.Temperature).Returns(currentTemperature);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);
            await vm.Connect();
            using CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            Func<Task> coolCamera = () => vm.CoolCamera(
                targetTemperature,
                TimeSpan.Zero,
                new Progress<ApplicationStatus>(),
                cancellationTokenSource.Token);

            await coolCamera.Should().ThrowAsync<OperationCanceledException>();
            camera.VerifySet(x => x.TemperatureSetPoint = currentTemperature, Times.Once);
        }

        [Test]
        [TestCase(-11, -10)]
        [TestCase(-9, -10)]
        public async Task CoolCamera_WhenTemperatureIsAtToleranceBoundary_CompletesImmediately(double currentTemperature, double targetTemperature) {
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            camera.SetupGet(x => x.Temperature).Returns(currentTemperature);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);
            await vm.Connect();
            using CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            bool success = await vm.CoolCamera(
                targetTemperature,
                TimeSpan.Zero,
                new Progress<ApplicationStatus>(),
                cancellationTokenSource.Token);

            success.Should().BeTrue();
            camera.VerifySet(x => x.TemperatureSetPoint = targetTemperature, Times.Once);
        }

        [Test]
        public async Task CoolCamera_WhenTemperatureIsPastOppositeToleranceBoundary_ContinuesWaiting() {
            const double currentTemperature = -12;
            const double targetTemperature = -10;
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            camera.SetupGet(x => x.Temperature).Returns(currentTemperature);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);
            await vm.Connect();
            using CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            Func<Task> coolCamera = () => vm.CoolCamera(
                targetTemperature,
                TimeSpan.Zero,
                new Progress<ApplicationStatus>(),
                cancellationTokenSource.Token);

            await coolCamera.Should().ThrowAsync<OperationCanceledException>();
            camera.VerifySet(x => x.TemperatureSetPoint = currentTemperature, Times.Once);
        }

        [Test]
        public async Task CoolCamera_WhenRampIsShorterThanCheckpoint_AppliesExactTargetBeforeWaiting() {
            const double currentTemperature = 0;
            const double targetTemperature = -10;
            List<double> setPoints = new();
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            camera.SetupGet(x => x.Temperature).Returns(currentTemperature);
            camera.SetupSet(x => x.TemperatureSetPoint = It.IsAny<double>()).Callback<double>(setPoint => setPoints.Add(setPoint));
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);
            await vm.Connect();
            using CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            Func<Task> coolCamera = () => vm.CoolCamera(
                targetTemperature,
                TimeSpan.FromSeconds(10),
                new Progress<ApplicationStatus>(),
                cancellationTokenSource.Token);

            await coolCamera.Should().ThrowAsync<OperationCanceledException>();
            setPoints.Should().Equal(targetTemperature, currentTemperature);
        }

        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, true, true)]
        [TestCase(true, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, true, true)]
        public async Task CaptureAndLiveView_ApplyReadoutBeforeGainAndOffsetAndPreserveFilterChoice(bool liveView, bool wheelConnected, bool explicitFilter) {
            var vm = CreateVm();
            var camera = CreateCamera(true);
            var calls = new List<string>();
            camera.SetupSet(x => x.ReadoutMode = It.IsAny<short>()).Callback<short>(_ => calls.Add("readout"));
            camera.SetupSet(x => x.Gain = It.IsAny<int>()).Callback<int>(_ => calls.Add("gain"));
            camera.SetupSet(x => x.Offset = It.IsAny<int>()).Callback<int>(_ => calls.Add("offset"));
            camera.Setup(x => x.WaitUntilExposureIsReady(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            camera.Setup(x => x.DownloadLiveView(It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);
            await vm.Connect();
            calls.Clear();
            var wheelFilter = new FilterInfo { Name = "L", Position = 1 };
            var chosenFilter = new FilterInfo { Name = "Ha", Position = 2 };
            filterWheelMediator.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo { Connected = wheelConnected, SelectedFilter = wheelFilter });
            var sequence = new CaptureSequence { ExposureTime = 0.01, Gain = 20, Offset = 5,
                ImageType = liveView ? CaptureSequence.ImageTypes.SNAPSHOT : CaptureSequence.ImageTypes.LIGHT,
                FilterType = explicitFilter ? chosenFilter : null };
            if (liveView) {
                await foreach (var image in vm.LiveView(sequence, CancellationToken.None)) { }
            } else {
                await vm.Capture(sequence, CancellationToken.None, new Progress<ApplicationStatus>());
            }
            calls.Take(3).Should().Equal("readout", "gain", "offset");
            sequence.FilterType.Should().BeSameAs(explicitFilter ? chosenFilter : wheelConnected ? wheelFilter : null);
            camera.VerifySet(x => x.ReadoutMode = (short)(liveView ? 1 : 0), Times.AtLeastOnce);
            if (liveView) camera.Verify(x => x.StopLiveView(), Times.Once);
            await vm.Disconnect();
        }

        private CameraVM CreateVm() {
            return new CameraVM(profileService.Object, cameraMediator.Object, filterWheelMediator.Object, applicationStatusMediator.Object, deviceChooser.Object);
        }

        private static Mock<ICamera> CreateCamera(bool connects) {
            Mock<ICamera> camera = new Mock<ICamera>();
            bool connected = false;
            camera.SetupGet(x => x.Id).Returns("camera");
            camera.SetupGet(x => x.Name).Returns("Camera");
            camera.SetupGet(x => x.DisplayName).Returns("Camera Display");
            camera.SetupGet(x => x.Description).Returns("Camera description");
            camera.SetupGet(x => x.DriverInfo).Returns("Driver info");
            camera.SetupGet(x => x.DriverVersion).Returns("1.2.3");
            camera.SetupGet(x => x.SupportedActions).Returns(new List<string> { "cool" });
            camera.SetupGet(x => x.Connected).Returns(() => connected);
            camera.SetupGet(x => x.BinX).Returns((short)1);
            camera.SetupGet(x => x.BinY).Returns((short)1);
            camera.SetupGet(x => x.BinningModes).Returns(new AsyncObservableCollection<BinningMode> { new BinningMode(1, 1), new BinningMode(2, 2) });
            camera.SetupGet(x => x.CameraState).Returns(CameraStates.Idle);
            camera.SetupGet(x => x.CanSubSample).Returns(true);
            camera.SetupGet(x => x.ExposureMin).Returns(0.001);
            camera.SetupGet(x => x.ExposureMax).Returns(3600);
            camera.SetupGet(x => x.CameraXSize).Returns(6248);
            camera.SetupGet(x => x.CameraYSize).Returns(4176);
            camera.SetupGet(x => x.CoolerOn).Returns(true);
            camera.SetupGet(x => x.CoolerPower).Returns(45.5);
            camera.SetupGet(x => x.HasDewHeater).Returns(true);
            camera.SetupGet(x => x.DewHeaterOn).Returns(false);
            camera.SetupGet(x => x.CanSetGain).Returns(true);
            camera.SetupGet(x => x.CanGetGain).Returns(true);
            camera.SetupGet(x => x.Gains).Returns(new List<int> { 0, 120, 240 });
            camera.SetupGet(x => x.GainMin).Returns(0);
            camera.SetupGet(x => x.GainMax).Returns(240);
            camera.SetupGet(x => x.Gain).Returns(120);
            camera.SetupGet(x => x.HasShutter).Returns(false);
            camera.SetupGet(x => x.CanSetTemperature).Returns(true);
            camera.SetupGet(x => x.EnableSubSample).Returns(false);
            camera.SetupGet(x => x.CanShowLiveView).Returns(true);
            camera.SetupGet(x => x.LiveViewEnabled).Returns(false);
            camera.SetupGet(x => x.CanSetOffset).Returns(true);
            camera.SetupGet(x => x.OffsetMin).Returns(0);
            camera.SetupGet(x => x.OffsetMax).Returns(50);
            camera.SetupGet(x => x.Offset).Returns(8);
            camera.SetupGet(x => x.PixelSizeX).Returns(3.76);
            camera.SetupGet(x => x.PixelSizeY).Returns(3.76);
            camera.SetupGet(x => x.Temperature).Returns(-10.2);
            camera.SetupGet(x => x.TemperatureSetPoint).Returns(-15);
            camera.SetupGet(x => x.HasBattery).Returns(true);
            camera.SetupGet(x => x.BatteryLevel).Returns(91);
            camera.SetupGet(x => x.BitDepth).Returns(16);
            camera.SetupGet(x => x.ElectronsPerADU).Returns(0.42);
            camera.SetupGet(x => x.ReadoutMode).Returns((short)0);
            camera.SetupGet(x => x.ReadoutModeForNormalImages).Returns((short)0);
            camera.SetupGet(x => x.ReadoutModeForSnapImages).Returns((short)1);
            camera.SetupGet(x => x.ReadoutModes).Returns(new List<string> { "Normal", "Fast" });
            camera.SetupGet(x => x.SensorType).Returns(SensorType.Monochrome);
            camera.SetupGet(x => x.BayerOffsetX).Returns((short)0);
            camera.SetupGet(x => x.BayerOffsetY).Returns((short)0);
            camera.SetupGet(x => x.USBLimitMin).Returns(40);
            camera.SetupGet(x => x.USBLimitMax).Returns(100);
            camera.SetupGet(x => x.USBLimit).Returns(40);
            camera.SetupGet(x => x.CanSetUSBLimit).Returns(true);
            camera.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Returns(() => {
                connected = connects;
                return Task.FromResult(connects);
            });
            camera.Setup(x => x.Disconnect()).Callback(() => connected = false);
            return camera;
        }

        private static void EnsureApplicationResources() {
            if (Application.Current == null) {
                _ = new Application {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }

            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["CameraSVG"] = new GeometryGroup();
        }
    }
}
