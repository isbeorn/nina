using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class SafetyMonitorVMTest {
        private Mock<IProfileService> profileService;
        private Mock<IProfile> profile;
        private Mock<IApplicationSettings> applicationSettings;
        private Mock<ISafetyMonitorSettings> safetyMonitorSettings;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediator;
        private Mock<IApplicationStatusMediator> applicationStatusMediator;
        private Mock<IDeviceChooserVM> deviceChooser;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();

            profileService = new Mock<IProfileService>();
            profile = new Mock<IProfile>();
            applicationSettings = new Mock<IApplicationSettings>();
            safetyMonitorSettings = new Mock<ISafetyMonitorSettings>();
            safetyMonitorMediator = new Mock<ISafetyMonitorMediator>();
            applicationStatusMediator = new Mock<IApplicationStatusMediator>();
            deviceChooser = new Mock<IDeviceChooserVM>();

            applicationSettings.SetupProperty(x => x.DevicePollingInterval, 0);
            safetyMonitorSettings.SetupProperty(x => x.Id, string.Empty);
            safetyMonitorSettings.SetupProperty(x => x.LastDeviceName, string.Empty);
            profile.SetupGet(x => x.ApplicationSettings).Returns(applicationSettings.Object);
            profile.SetupGet(x => x.SafetyMonitorSettings).Returns(safetyMonitorSettings.Object);
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            deviceChooser.SetupGet(x => x.Devices).Returns(new List<IDevice>());
            deviceChooser.Setup(x => x.GetEquipment()).Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Verifies that a successful safety-monitor connection stores device metadata, persists the profile selection,
        /// raises Connected, and broadcasts the connected SafetyMonitorInfo snapshot.
        /// This protects shutdown/safety consumers that rely on the mediator state immediately after connection.
        /// </summary>
        [Test]
        public async Task Connect_WhenSafetyMonitorConnects_PopulatesInfoAndPersistsProfileSelection() {
            SafetyMonitorVM vm = CreateVm();
            Mock<ISafetyMonitor> safetyMonitor = CreateSafetyMonitor(connects: true, isSafe: true);
            bool connectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(safetyMonitor.Object);
            vm.Connected += (_, _) => {
                connectedRaised = true;
                return Task.CompletedTask;
            };

            bool connected = await vm.Connect();

            connected.Should().BeTrue();
            connectedRaised.Should().BeTrue();
            vm.SafetyMonitor.Should().BeSameAs(safetyMonitor.Object);
            vm.SafetyMonitorInfo.Connected.Should().BeTrue();
            vm.SafetyMonitorInfo.IsSafe.Should().BeTrue();
            vm.SafetyMonitorInfo.Name.Should().Be("Safety Monitor");
            vm.SafetyMonitorInfo.DisplayName.Should().Be("Safety Monitor Display");
            vm.SafetyMonitorInfo.DeviceId.Should().Be("safety-monitor");
            safetyMonitorSettings.Object.Id.Should().Be("safety-monitor");
            safetyMonitorSettings.Object.LastDeviceName.Should().Be("Safety Monitor Display");
            safetyMonitorMediator.Verify(x => x.RegisterHandler(vm), Times.Once);
            safetyMonitorMediator.Verify(x => x.Broadcast(It.Is<SafetyMonitorInfo>(info => info.Connected && info.IsSafe && info.DeviceId == "safety-monitor")), Times.AtLeastOnce);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && !string.IsNullOrEmpty(status.Status))), Times.Once);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && status.Status == string.Empty)), Times.Once);
        }

        /// <summary>
        /// Verifies that selecting the dummy "No_Device" safety monitor choice persists the disabled-device state without connecting.
        /// This protects users who intentionally run without a safety monitor.
        /// </summary>
        [Test]
        public async Task Connect_WhenNoDeviceSelected_PersistsNoDeviceAndDoesNotConnect() {
            SafetyMonitorVM vm = CreateVm();
            Mock<ISafetyMonitor> safetyMonitor = CreateSafetyMonitor(connects: true, isSafe: true);
            safetyMonitor.SetupGet(x => x.Id).Returns("No_Device");
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(safetyMonitor.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            safetyMonitorSettings.Object.Id.Should().Be("No_Device");
            safetyMonitorSettings.Object.LastDeviceName.Should().BeEmpty();
            safetyMonitor.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies that failed safety-monitor connection attempts leave the VM disconnected and clear the device reference.
        /// This protects retry behavior after a driver reports that it could not connect.
        /// </summary>
        [Test]
        public async Task Connect_WhenSafetyMonitorReturnsFalse_LeavesDisconnectedState() {
            SafetyMonitorVM vm = CreateVm();
            Mock<ISafetyMonitor> safetyMonitor = CreateSafetyMonitor(connects: false, isSafe: false);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(safetyMonitor.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            vm.SafetyMonitor.Should().BeNull();
            vm.SafetyMonitorInfo.Connected.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that disconnect resets device state, calls the monitor disconnect method, raises Disconnected, and broadcasts disconnected info.
        /// This protects profile-change and shutdown cleanup paths.
        /// </summary>
        [Test]
        public async Task Disconnect_AfterConnected_ClearsStateAndBroadcastsDisconnectedInfo() {
            SafetyMonitorVM vm = CreateVm();
            Mock<ISafetyMonitor> safetyMonitor = CreateSafetyMonitor(connects: true, isSafe: true);
            bool disconnectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(safetyMonitor.Object);
            vm.Disconnected += (_, _) => {
                disconnectedRaised = true;
                return Task.CompletedTask;
            };
            await vm.Connect();

            await vm.Disconnect();

            disconnectedRaised.Should().BeTrue();
            vm.SafetyMonitor.Should().BeNull();
            vm.SafetyMonitorInfo.Connected.Should().BeFalse();
            safetyMonitor.Verify(x => x.Disconnect(), Times.AtLeastOnce);
            safetyMonitorMediator.Verify(x => x.Broadcast(It.Is<SafetyMonitorInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that driver action and command methods are gated by connection state and forwarded only while connected.
        /// This protects plugin and diagnostic callers from sending commands to an absent safety monitor.
        /// </summary>
        [Test]
        public async Task DriverCommands_AreForwardedOnlyWhenConnected() {
            SafetyMonitorVM vm = CreateVm();
            Mock<ISafetyMonitor> safetyMonitor = CreateSafetyMonitor(connects: true, isSafe: true);
            safetyMonitor.Setup(x => x.Action("arm", "now")).Returns("armed");
            safetyMonitor.Setup(x => x.SendCommandString(":GV#", true)).Returns("version");
            safetyMonitor.Setup(x => x.SendCommandBool(":CHK#", false)).Returns(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(safetyMonitor.Object);

            vm.Action("arm", "now").Should().BeNull();
            vm.SendCommandString(":GV#").Should().BeNull();
            vm.SendCommandBool(":CHK#", false).Should().BeFalse();
            vm.SendCommandBlind(":STOP#", false);
            safetyMonitor.Verify(x => x.SendCommandBlind(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            await vm.Connect();

            vm.Action("arm", "now").Should().Be("armed");
            vm.SendCommandString(":GV#").Should().Be("version");
            vm.SendCommandBool(":CHK#", false).Should().BeTrue();
            vm.SendCommandBlind(":STOP#", false);
            safetyMonitor.Verify(x => x.SendCommandBlind(":STOP#", false), Times.Once);
        }

        /// <summary>
        /// Verifies that GetDevice returns the active safety monitor while connected and null after disconnect.
        /// This protects direct-device access through the common equipment VM contract.
        /// </summary>
        [Test]
        public async Task GetDevice_TracksConnectedSafetyMonitorLifetime() {
            SafetyMonitorVM vm = CreateVm();
            Mock<ISafetyMonitor> safetyMonitor = CreateSafetyMonitor(connects: true, isSafe: true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(safetyMonitor.Object);

            await vm.Connect();
            vm.GetDevice().Should().BeSameAs(safetyMonitor.Object);

            await vm.Disconnect();
            vm.GetDevice().Should().BeNull();
        }

        private SafetyMonitorVM CreateVm() {
            return new SafetyMonitorVM(profileService.Object, safetyMonitorMediator.Object, applicationStatusMediator.Object, deviceChooser.Object);
        }

        private static Mock<ISafetyMonitor> CreateSafetyMonitor(bool connects, bool isSafe) {
            Mock<ISafetyMonitor> safetyMonitor = new Mock<ISafetyMonitor>();
            safetyMonitor.SetupGet(x => x.Id).Returns("safety-monitor");
            safetyMonitor.SetupGet(x => x.Name).Returns("Safety Monitor");
            safetyMonitor.SetupGet(x => x.DisplayName).Returns("Safety Monitor Display");
            safetyMonitor.SetupGet(x => x.Description).Returns("Safety description");
            safetyMonitor.SetupGet(x => x.DriverInfo).Returns("Driver info");
            safetyMonitor.SetupGet(x => x.DriverVersion).Returns("1.2.3");
            safetyMonitor.SetupGet(x => x.SupportedActions).Returns(new List<string> { "arm" });
            safetyMonitor.SetupGet(x => x.IsSafe).Returns(isSafe);
            safetyMonitor.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(connects);
            return safetyMonitor;
        }

        private static void EnsureApplicationResources() {
            if (Application.Current == null) {
                _ = new Application();
            }

            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["ShieldSVG"] = new GeometryGroup();
        }
    }
}
