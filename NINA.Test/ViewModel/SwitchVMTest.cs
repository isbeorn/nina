using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class SwitchVMTest {
        private Mock<IProfileService> profileService;
        private Mock<IProfile> profile;
        private Mock<IApplicationSettings> applicationSettings;
        private Mock<ISwitchSettings> switchSettings;
        private Mock<IApplicationStatusMediator> applicationStatusMediator;
        private Mock<ISwitchMediator> switchMediator;
        private Mock<IDeviceChooserVM> deviceChooser;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();

            profileService = new Mock<IProfileService>();
            profile = new Mock<IProfile>();
            applicationSettings = new Mock<IApplicationSettings>();
            switchSettings = new Mock<ISwitchSettings>();
            applicationStatusMediator = new Mock<IApplicationStatusMediator>();
            switchMediator = new Mock<ISwitchMediator>();
            deviceChooser = new Mock<IDeviceChooserVM>();

            applicationSettings.SetupProperty(x => x.DevicePollingInterval, 0);
            switchSettings.SetupProperty(x => x.Id, string.Empty);
            switchSettings.SetupProperty(x => x.LastDeviceName, string.Empty);
            profile.SetupGet(x => x.ApplicationSettings).Returns(applicationSettings.Object);
            profile.SetupGet(x => x.SwitchSettings).Returns(switchSettings.Object);
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            deviceChooser.SetupGet(x => x.Devices).Returns(new List<IDevice>());
            deviceChooser.Setup(x => x.GetEquipment()).Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Verifies that a successful switch-hub connection partitions writable and read-only switches, persists the selected device,
        /// raises the connected event, and broadcasts a populated SwitchInfo snapshot.
        /// This protects the equipment panel and sequencer paths that consume switch metadata immediately after connection.
        /// </summary>
        [Test]
        public async Task Connect_WhenSwitchHubConnects_PopulatesSwitchInfoAndPersistsProfileSelection() {
            SwitchVM vm = CreateVm();
            Mock<ISwitchHub> switchHub = CreateSwitchHub(connects: true);
            Mock<IWritableSwitch> writableSwitch = CreateWritableSwitch(id: 0, name: "Flat panel", value: 0);
            Mock<ISwitch> readonlySwitch = CreateReadonlySwitch(id: 1, name: "Voltage", value: 12.3);
            bool connectedRaised = false;
            switchHub.SetupGet(x => x.Switches).Returns(new ISwitch[] { writableSwitch.Object, readonlySwitch.Object });
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(switchHub.Object);
            bool connectedBroadcastSawPersistedProfile = false;
            switchMediator
                .Setup(x => x.Broadcast(It.Is<SwitchInfo>(info => info.Connected && info.DeviceId == "switch-hub")))
                .Callback(() => connectedBroadcastSawPersistedProfile =
                    switchSettings.Object.Id == "switch-hub" &&
                    switchSettings.Object.LastDeviceName == "Switch Hub Display");
            vm.Connected += (_, _) => {
                connectedRaised = true;
                return Task.CompletedTask;
            };

            bool connected = await vm.Connect();

            connected.Should().BeTrue();
            connectedRaised.Should().BeTrue();
            vm.SwitchHub.Should().BeSameAs(switchHub.Object);
            vm.WritableSwitches.Should().ContainSingle().Which.Should().BeSameAs(writableSwitch.Object);
            vm.ReadonlySwitches.Should().ContainSingle().Which.Should().BeSameAs(readonlySwitch.Object);
            vm.SelectedWritableSwitch.Should().BeSameAs(writableSwitch.Object);
            vm.SwitchInfo.Connected.Should().BeTrue();
            vm.SwitchInfo.Name.Should().Be("Switch Hub");
            vm.SwitchInfo.DisplayName.Should().Be("Switch Hub Display");
            vm.SwitchInfo.WritableSwitches.Should().ContainSingle().Which.Should().BeSameAs(writableSwitch.Object);
            vm.SwitchInfo.ReadonlySwitches.Should().ContainSingle().Which.Should().BeSameAs(readonlySwitch.Object);
            switchSettings.Object.Id.Should().Be("switch-hub");
            switchSettings.Object.LastDeviceName.Should().Be("Switch Hub Display");
            connectedBroadcastSawPersistedProfile.Should().BeTrue();
            switchMediator.Verify(x => x.RegisterHandler(vm), Times.Once);
            switchMediator.Verify(x => x.Broadcast(It.Is<SwitchInfo>(info => info.Connected && info.DeviceId == "switch-hub")), Times.AtLeastOnce);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && !string.IsNullOrEmpty(status.Status))), Times.Once);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && status.Status == string.Empty)), Times.Once);
        }

        /// <summary>
        /// Verifies that selecting the dummy "No_Device" switch choice updates profile settings and does not attempt a hardware connection.
        /// This protects profile compatibility for users who intentionally disable switch-hub integration.
        /// </summary>
        [Test]
        public async Task Connect_WhenNoDeviceSelected_PersistsNoDeviceAndDoesNotConnect() {
            SwitchVM vm = CreateVm();
            Mock<ISwitchHub> switchHub = CreateSwitchHub(connects: true);
            switchHub.SetupGet(x => x.Id).Returns("No_Device");
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(switchHub.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            switchSettings.Object.Id.Should().Be("No_Device");
            switchSettings.Object.LastDeviceName.Should().BeEmpty();
            switchHub.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies that failed switch-hub connection attempts leave the VM disconnected and do not retain a hub reference.
        /// This protects retry behavior after a driver reports a clean connection failure.
        /// </summary>
        [Test]
        public async Task Connect_WhenSwitchHubReturnsFalse_LeavesDisconnectedState() {
            SwitchVM vm = CreateVm();
            Mock<ISwitchHub> switchHub = CreateSwitchHub(connects: false);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(switchHub.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            vm.SwitchHub.Should().BeNull();
            vm.SwitchInfo.Connected.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that disconnect clears the switch collections, resets SwitchInfo, calls the device disconnect method, and raises Disconnected.
        /// This protects cleanup paths used when profiles change or equipment is disconnected manually.
        /// </summary>
        [Test]
        public async Task Disconnect_AfterConnected_ClearsStateAndBroadcastsDisconnectedInfo() {
            SwitchVM vm = CreateVm();
            Mock<ISwitchHub> switchHub = CreateSwitchHub(connects: true);
            switchHub.SetupGet(x => x.Switches).Returns(new ISwitch[] { CreateWritableSwitch(0, "Light", 1).Object });
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(switchHub.Object);
            bool disconnectedRaised = false;
            vm.Disconnected += (_, _) => {
                disconnectedRaised = true;
                return Task.CompletedTask;
            };
            await vm.Connect();

            await vm.Disconnect();

            disconnectedRaised.Should().BeTrue();
            vm.SwitchHub.Should().BeNull();
            vm.WritableSwitches.Should().BeEmpty();
            vm.ReadonlySwitches.Should().BeEmpty();
            vm.SwitchInfo.Connected.Should().BeFalse();
            switchHub.Verify(x => x.Disconnect(), Times.AtLeastOnce);
            switchMediator.Verify(x => x.Broadcast(It.Is<SwitchInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that action and command-string calls are gated by connection state and forwarded only while connected.
        /// This protects plugin and advanced-control callers from sending commands to an absent switch hub.
        /// </summary>
        [Test]
        public async Task DriverCommands_AreForwardedOnlyWhenConnected() {
            SwitchVM vm = CreateVm();
            Mock<ISwitchHub> switchHub = CreateSwitchHub(connects: true);
            switchHub.SetupGet(x => x.Switches).Returns(Array.Empty<ISwitch>());
            switchHub.Setup(x => x.Action("pulse", "A")).Returns("action-result");
            switchHub.Setup(x => x.SendCommandString(":GV#", true)).Returns("version");
            switchHub.Setup(x => x.SendCommandBool(":CHK#", false)).Returns(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(switchHub.Object);

            vm.Action("pulse", "A").Should().BeNull();
            vm.SendCommandString(":GV#").Should().BeNull();
            vm.SendCommandBool(":CHK#", false).Should().BeFalse();
            vm.SendCommandBlind(":STOP#", false);
            switchHub.Verify(x => x.SendCommandBlind(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            await vm.Connect();

            vm.Action("pulse", "A").Should().Be("action-result");
            vm.SendCommandString(":GV#").Should().Be("version");
            vm.SendCommandBool(":CHK#", false).Should().BeTrue();
            vm.SendCommandBlind(":STOP#", false);
            switchHub.Verify(x => x.SendCommandBlind(":STOP#", false), Times.Once);
        }

        /// <summary>
        /// Verifies that setting a switch by index updates TargetValue and invokes the writable switch SetValue path.
        /// This protects sequencer switch instructions that address switches by their hub index.
        /// </summary>
        [Test]
        public async Task SetSwitchValue_WithWritableSwitchIndex_SetsTargetValueAndInvokesDevice() {
            SwitchVM vm = CreateVm();
            Mock<IWritableSwitch> writableSwitch = CreateWritableSwitch(id: 0, name: "Relay", value: 0);
            double currentValue = 0;
            writableSwitch.SetupGet(x => x.Value).Returns(() => currentValue);
            writableSwitch.Setup(x => x.SetValue()).Callback(() => currentValue = writableSwitch.Object.TargetValue);
            vm.WritableSwitches = new List<IWritableSwitch> { writableSwitch.Object };

            await vm.SetSwitchValue(0, 0.75, progress: null, ct: CancellationToken.None);

            writableSwitch.Object.TargetValue.Should().Be(0.75);
            writableSwitch.Verify(x => x.SetValue(), Times.Once);
            writableSwitch.Verify(x => x.Poll(), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that an out-of-range switch index completes without touching existing switches.
        /// This protects sequencer compatibility when a saved switch index no longer exists on the connected hub.
        /// </summary>
        [Test]
        public async Task SetSwitchValue_WithMissingIndex_CompletesWithoutTouchingExistingSwitches() {
            SwitchVM vm = CreateVm();
            Mock<IWritableSwitch> writableSwitch = CreateWritableSwitch(id: 0, name: "Relay", value: 0);
            vm.WritableSwitches = new List<IWritableSwitch> { writableSwitch.Object };

            Func<Task> setMissingSwitch = () => vm.SetSwitchValue(2, 1, progress: null, ct: CancellationToken.None);

            await setMissingSwitch.Should().NotThrowAsync();
            writableSwitch.Verify(x => x.SetValue(), Times.Never);
        }

        private SwitchVM CreateVm() {
            return new SwitchVM(profileService.Object, applicationStatusMediator.Object, switchMediator.Object, deviceChooser.Object);
        }

        private static Mock<ISwitchHub> CreateSwitchHub(bool connects) {
            Mock<ISwitchHub> switchHub = new Mock<ISwitchHub>();
            switchHub.SetupGet(x => x.Id).Returns("switch-hub");
            switchHub.SetupGet(x => x.Name).Returns("Switch Hub");
            switchHub.SetupGet(x => x.DisplayName).Returns("Switch Hub Display");
            switchHub.SetupGet(x => x.Description).Returns("Switch description");
            switchHub.SetupGet(x => x.DriverInfo).Returns("Driver info");
            switchHub.SetupGet(x => x.DriverVersion).Returns("1.2.3");
            switchHub.SetupGet(x => x.SupportedActions).Returns(new List<string> { "pulse" });
            switchHub.SetupGet(x => x.Switches).Returns(Array.Empty<ISwitch>());
            switchHub.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(connects);
            return switchHub;
        }

        private static Mock<IWritableSwitch> CreateWritableSwitch(short id, string name, double value) {
            Mock<IWritableSwitch> writableSwitch = new Mock<IWritableSwitch>();
            writableSwitch.SetupGet(x => x.Id).Returns(id);
            writableSwitch.SetupGet(x => x.Name).Returns(name);
            writableSwitch.SetupGet(x => x.Value).Returns(value);
            writableSwitch.SetupProperty(x => x.TargetValue, value);
            return writableSwitch;
        }

        private static Mock<ISwitch> CreateReadonlySwitch(short id, string name, double value) {
            Mock<ISwitch> readonlySwitch = new Mock<ISwitch>();
            readonlySwitch.SetupGet(x => x.Id).Returns(id);
            readonlySwitch.SetupGet(x => x.Name).Returns(name);
            readonlySwitch.SetupGet(x => x.Value).Returns(value);
            return readonlySwitch;
        }

        private static void EnsureApplicationResources() {
            if (Application.Current == null) {
                _ = new Application();
            }

            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["SwitchesSVG"] = new GeometryGroup();
        }
    }
}
