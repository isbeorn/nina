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
    [Apartment(ApartmentState.STA)]
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

        /// <summary>
        /// Verifies that a successful camera connection populates camera metadata, persists profile settings,
        /// broadcasts a connected snapshot, raises Connected, and exposes the unwrapped physical camera via GetDevice.
        /// This protects mediator consumers that need camera capabilities immediately after connection.
        /// </summary>
        [Test]
        public async Task Connect_WhenCameraConnects_PopulatesInfoPersistsProfileAndBroadcastsConnectedInfo() {
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            bool connectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);
            vm.Connected += (_, _) => {
                connectedRaised = true;
                return Task.CompletedTask;
            };

            bool connected = await vm.Connect();

            connected.Should().BeTrue();
            connectedRaised.Should().BeTrue();
            vm.Cam.Should().NotBeNull();
            vm.GetDevice().Should().BeSameAs(camera.Object);
            vm.CameraInfo.Connected.Should().BeTrue();
            vm.CameraInfo.DeviceId.Should().Be("camera");
            vm.CameraInfo.DisplayName.Should().Be("Camera Display");
            vm.CameraInfo.PixelSize.Should().Be(3.76);
            vm.CameraInfo.XSize.Should().Be(6248);
            vm.CameraInfo.YSize.Should().Be(4176);
            vm.CameraInfo.DefaultGain.Should().Be(120);
            vm.CameraInfo.DefaultOffset.Should().Be(8);
            cameraSettings.Object.Id.Should().Be("camera");
            cameraSettings.Object.LastDeviceName.Should().Be("Camera Display");
            cameraSettings.Object.PixelSize.Should().Be(3.76);
            cameraMediator.Verify(x => x.RegisterHandler(vm), Times.Once);
            cameraMediator.Verify(x => x.Broadcast(It.Is<CameraInfo>(info => info.Connected && info.DeviceId == "camera" && info.XSize == 6248)), Times.AtLeastOnce);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && !string.IsNullOrEmpty(status.Status))), Times.Once);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && status.Status == string.Empty)), Times.Once);
        }

        /// <summary>
        /// Verifies that selecting the dummy "No_Device" camera choice persists the disabled-device state without connecting.
        /// This protects profiles that intentionally run without a camera attached.
        /// </summary>
        [Test]
        public async Task Connect_WhenNoDeviceSelected_PersistsNoDeviceAndDoesNotConnect() {
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            camera.SetupGet(x => x.Id).Returns("No_Device");
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            cameraSettings.Object.Id.Should().Be("No_Device");
            cameraSettings.Object.LastDeviceName.Should().BeEmpty();
            camera.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies that a driver-reported camera connection failure leaves the VM disconnected and does not retain the camera reference.
        /// This protects retry behavior after a camera driver declines a connection without throwing.
        /// </summary>
        [Test]
        public async Task Connect_WhenCameraReturnsFalse_LeavesDisconnectedState() {
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: false);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            vm.Cam.Should().BeNull();
            vm.CameraInfo.Connected.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that disconnect resets the camera state, calls the driver disconnect method, raises Disconnected,
        /// and broadcasts a disconnected CameraInfo snapshot.
        /// This protects profile-change and shutdown cleanup paths.
        /// </summary>
        [Test]
        public async Task Disconnect_AfterConnected_ClearsStateAndBroadcastsDisconnectedInfo() {
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            bool disconnectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);
            vm.Disconnected += (_, _) => {
                disconnectedRaised = true;
                return Task.CompletedTask;
            };
            await vm.Connect();

            await vm.Disconnect();

            disconnectedRaised.Should().BeTrue();
            vm.Cam.Should().BeNull();
            vm.CameraInfo.Connected.Should().BeFalse();
            camera.Verify(x => x.Disconnect(), Times.AtLeastOnce);
            cameraMediator.Verify(x => x.Broadcast(It.Is<CameraInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that common driver command calls are gated by connection state and forwarded only while connected.
        /// This protects plugin and diagnostics callers from sending commands to an absent camera.
        /// </summary>
        [Test]
        public async Task DriverCommands_AreForwardedOnlyWhenConnected() {
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            camera.Setup(x => x.Action("cool", "now")).Returns("cooling");
            camera.Setup(x => x.SendCommandString(":GV#", true)).Returns("version");
            camera.Setup(x => x.SendCommandBool(":CHK#", false)).Returns(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);

            vm.Action("cool", "now").Should().BeNull();
            vm.SendCommandString(":GV#").Should().BeNull();
            vm.SendCommandBool(":CHK#", false).Should().BeFalse();
            vm.SendCommandBlind(":STOP#", false);
            camera.Verify(x => x.SendCommandBlind(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            await vm.Connect();

            vm.Action("cool", "now").Should().Be("cooling");
            vm.SendCommandString(":GV#").Should().Be("version");
            vm.SendCommandBool(":CHK#", false).Should().BeTrue();
            vm.SendCommandBlind(":STOP#", false);
            camera.Verify(x => x.SendCommandBlind(":STOP#", false), Times.Once);
        }

        /// <summary>
        /// Verifies that camera setting helpers update both driver state and CameraInfo while broadcasting the updated snapshot.
        /// This protects UI controls and mediators that change readout mode, USB limit, and binning after connection.
        /// </summary>
        [Test]
        public async Task CameraSetters_WhenConnected_UpdateDriverInfoAndBroadcast() {
            CameraVM vm = CreateVm();
            Mock<ICamera> camera = CreateCamera(connects: true);
            short binX = 1;
            short binY = 1;
            int usbLimit = 40;
            short readoutMode = 0;
            camera.SetupGet(x => x.BinX).Returns(() => binX);
            camera.SetupGet(x => x.BinY).Returns(() => binY);
            camera.SetupSet(x => x.USBLimit = It.IsAny<int>()).Callback<int>(value => usbLimit = value);
            camera.SetupGet(x => x.USBLimit).Returns(() => usbLimit);
            camera.SetupSet(x => x.ReadoutMode = It.IsAny<short>()).Callback<short>(value => readoutMode = value);
            camera.SetupGet(x => x.ReadoutMode).Returns(() => readoutMode);
            camera.Setup(x => x.SetBinning(It.IsAny<short>(), It.IsAny<short>())).Callback<short, short>((x, y) => {
                binX = x;
                binY = y;
            });
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(camera.Object);
            await vm.Connect();

            vm.SetReadoutMode(1);
            vm.SetUSBLimit(80);
            vm.SetBinning(2, 2);

            vm.CameraInfo.ReadoutMode.Should().Be(1);
            vm.CameraInfo.USBLimit.Should().Be(80);
            vm.CameraInfo.BinX.Should().Be(2);
            vm.CameraInfo.BinY.Should().Be(2);
            camera.VerifySet(x => x.ReadoutMode = 1, Times.Once);
            camera.VerifySet(x => x.USBLimit = 80, Times.Once);
            camera.Verify(x => x.SetBinning(2, 2), Times.Once);
            cameraMediator.Verify(x => x.Broadcast(It.Is<CameraInfo>(info => info.Connected && info.BinX == 2 && info.BinY == 2)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that SetBinning is a safe no-op before a camera is connected.
        /// This protects mediator callers and UI controls from null-reference failures when binning commands arrive while disconnected.
        /// </summary>
        [Test]
        public void SetBinning_WhenDisconnected_DoesNotThrowOrBroadcast() {
            CameraVM vm = CreateVm();

            Action setBinning = () => vm.SetBinning(2, 2);

            setBinning.Should().NotThrow();
            vm.CameraInfo.BinX.Should().Be(0);
            vm.CameraInfo.BinY.Should().Be(0);
            cameraMediator.Verify(x => x.Broadcast(It.Is<CameraInfo>(info => info.BinX == 2 && info.BinY == 2)), Times.Never);
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
                _ = new Application();
            }

            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["CameraSVG"] = new GeometryGroup();
        }
    }
}
