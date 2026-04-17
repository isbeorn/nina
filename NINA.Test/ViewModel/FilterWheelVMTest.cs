using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class FilterWheelVMTest {
        private Mock<IProfileService> profileService;
        private Mock<IProfile> profile;
        private Mock<IApplicationSettings> applicationSettings;
        private Mock<IFilterWheelSettings> filterWheelSettings;
        private Mock<IFocuserSettings> focuserSettings;
        private Mock<IFilterWheelMediator> filterWheelMediator;
        private Mock<IFocuserMediator> focuserMediator;
        private Mock<IGuiderMediator> guiderMediator;
        private Mock<IApplicationStatusMediator> applicationStatusMediator;
        private Mock<IDeviceChooserVM> deviceChooser;
        private ObserveAllCollection<FilterInfo> profileFilters;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();

            profileService = new Mock<IProfileService>();
            profile = new Mock<IProfile>();
            applicationSettings = new Mock<IApplicationSettings>();
            filterWheelSettings = new Mock<IFilterWheelSettings>();
            focuserSettings = new Mock<IFocuserSettings>();
            filterWheelMediator = new Mock<IFilterWheelMediator>();
            focuserMediator = new Mock<IFocuserMediator>();
            guiderMediator = new Mock<IGuiderMediator>();
            applicationStatusMediator = new Mock<IApplicationStatusMediator>();
            deviceChooser = new Mock<IDeviceChooserVM>();
            profileFilters = new ObserveAllCollection<FilterInfo>();

            applicationSettings.SetupProperty(x => x.DevicePollingInterval, 0);
            filterWheelSettings.SetupProperty(x => x.Id, string.Empty);
            filterWheelSettings.SetupProperty(x => x.LastDeviceName, string.Empty);
            filterWheelSettings.SetupProperty(x => x.DisableGuidingOnFilterChange, false);
            filterWheelSettings.SetupGet(x => x.FilterWheelFilters).Returns(profileFilters);
            focuserSettings.SetupProperty(x => x.UseFilterWheelOffsets, false);
            profile.SetupGet(x => x.ApplicationSettings).Returns(applicationSettings.Object);
            profile.SetupGet(x => x.FilterWheelSettings).Returns(filterWheelSettings.Object);
            profile.SetupGet(x => x.FocuserSettings).Returns(focuserSettings.Object);
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            deviceChooser.SetupGet(x => x.Devices).Returns(new List<IDevice>());
            deviceChooser.Setup(x => x.GetEquipment()).Returns(Task.CompletedTask);
            focuserMediator.Setup(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
            guiderMediator.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            guiderMediator.Setup(x => x.StartGuiding(It.IsAny<bool>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        /// <summary>
        /// Verifies that a successful filter-wheel connection stores device metadata, persists the profile selection,
        /// selects the wheel's current filter, auto-imports driver filters into an empty profile, and broadcasts the connected snapshot.
        /// This protects first-run setup and sequence consumers that depend on filter metadata immediately after connection.
        /// </summary>
        [Test]
        public async Task Connect_WhenFilterWheelConnects_AutoImportsFiltersAndPersistsProfileSelection() {
            FilterWheelVM vm = CreateVm();
            FilterInfo luminance = new FilterInfo("L", 0, 0);
            FilterInfo red = new FilterInfo("R", 10, 1);
            Mock<IFilterWheel> filterWheel = CreateFilterWheel(connects: true, currentPosition: 1, luminance, red);
            bool connectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(filterWheel.Object);
            vm.Connected += (_, _) => {
                connectedRaised = true;
                return Task.CompletedTask;
            };

            bool connected = await vm.Connect();

            connected.Should().BeTrue();
            connectedRaised.Should().BeTrue();
            vm.FW.Should().BeSameAs(filterWheel.Object);
            vm.FilterWheelInfo.Connected.Should().BeTrue();
            vm.FilterWheelInfo.SelectedFilter.Should().BeSameAs(red);
            vm.TargetFilter.Should().BeSameAs(red);
            vm.GetAllFilters().Should().BeEquivalentTo(new[] { luminance, red });
            profileFilters.Should().Equal(luminance, red);
            filterWheelSettings.Object.Id.Should().Be("filter-wheel");
            filterWheelSettings.Object.LastDeviceName.Should().Be("Filter Wheel Display");
            filterWheelMediator.Verify(x => x.RegisterHandler(vm), Times.Once);
            filterWheelMediator.Verify(x => x.Broadcast(It.Is<FilterWheelInfo>(info => info.Connected && info.DeviceId == "filter-wheel")), Times.AtLeastOnce);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && !string.IsNullOrEmpty(status.Status))), Times.Once);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && status.Status == string.Empty)), Times.Once);
        }

        /// <summary>
        /// Verifies that selecting the dummy "No_Device" filter wheel persists the disabled-device setting and avoids driver connection.
        /// This protects profiles that intentionally run with manual or absent filter-wheel integration.
        /// </summary>
        [Test]
        public async Task Connect_WhenNoDeviceSelected_PersistsNoDeviceAndDoesNotConnect() {
            FilterWheelVM vm = CreateVm();
            Mock<IFilterWheel> filterWheel = CreateFilterWheel(connects: true, currentPosition: 0, new FilterInfo("L", 0, 0));
            filterWheel.SetupGet(x => x.Id).Returns("No_Device");
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(filterWheel.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            filterWheelSettings.Object.Id.Should().Be("No_Device");
            filterWheelSettings.Object.LastDeviceName.Should().BeEmpty();
            filterWheel.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies that a driver-reported connection failure leaves the VM disconnected and does not retain the wheel reference.
        /// This protects retry behavior when a filter-wheel driver declines a connection without throwing.
        /// </summary>
        [Test]
        public async Task Connect_WhenFilterWheelReturnsFalse_LeavesDisconnectedState() {
            FilterWheelVM vm = CreateVm();
            Mock<IFilterWheel> filterWheel = CreateFilterWheel(connects: false, currentPosition: 0, new FilterInfo("L", 0, 0));
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(filterWheel.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            vm.FW.Should().BeNull();
            vm.FilterWheelInfo.Connected.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that changing filters applies the driver position, reports the from/to filter transition,
        /// moves the focuser by the profile offset delta, and resumes guiding when this VM stopped it.
        /// This protects the scientific imaging path where filter changes must preserve focus and guiding state deterministically.
        /// </summary>
        [Test]
        public async Task ChangeFilter_WithFilterOffsetsAndGuidingEnabled_MovesFocuserAndRaisesFilterChanged() {
            FilterWheelVM vm = CreateVm();
            FilterInfo luminance = new FilterInfo("L", 100, 0);
            FilterInfo red = new FilterInfo("R", 125, 1);
            Mock<IFilterWheel> filterWheel = CreateFilterWheel(connects: true, currentPosition: 0, luminance, red);
            filterWheelSettings.Object.DisableGuidingOnFilterChange = true;
            focuserSettings.Object.UseFilterWheelOffsets = true;
            guiderMediator.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(filterWheel.Object);
            FilterChangedEventArgs raisedArgs = null;
            vm.FilterChanged += (_, args) => {
                raisedArgs = args;
                return Task.CompletedTask;
            };
            await vm.Connect();

            FilterInfo selected = await vm.ChangeFilter(red, CancellationToken.None);

            selected.Should().BeSameAs(red);
            filterWheel.Object.Position.Should().Be(1);
            vm.FilterWheelInfo.SelectedFilter.Should().BeSameAs(red);
            raisedArgs.Should().NotBeNull();
            raisedArgs.From.Should().BeSameAs(luminance);
            raisedArgs.To.Should().BeSameAs(red);
            focuserMediator.Verify(x => x.MoveFocuserRelative(25, It.IsAny<CancellationToken>()), Times.Once);
            guiderMediator.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
            guiderMediator.Verify(x => x.StartGuiding(false, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            filterWheelMediator.Verify(x => x.Broadcast(It.Is<FilterWheelInfo>(info => info.SelectedFilter == red && !info.IsMoving)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that requesting a null target filter is a no-op that returns the current profile filter without moving hardware.
        /// This protects callers that use ChangeFilter defensively when a target filter is optional or unresolved.
        /// </summary>
        [Test]
        public async Task ChangeFilter_WithNullTarget_ReturnsCurrentFilterWithoutMovingWheel() {
            FilterWheelVM vm = CreateVm();
            FilterInfo luminance = new FilterInfo("L", 100, 0);
            Mock<IFilterWheel> filterWheel = CreateFilterWheel(connects: true, currentPosition: 0, luminance);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(filterWheel.Object);
            await vm.Connect();

            FilterInfo selected = await vm.ChangeFilter(null, CancellationToken.None);

            selected.Should().BeSameAs(luminance);
            filterWheel.Object.Position.Should().Be(0);
            focuserMediator.Verify(x => x.MoveFocuserRelative(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies that requesting a filter position absent from the connected wheel returns null and does not move the driver.
        /// This protects compatibility when a saved profile filter no longer exists on the currently connected wheel.
        /// </summary>
        [Test]
        public async Task ChangeFilter_WithMissingDriverFilter_ReturnsNullWithoutMovingWheel() {
            FilterWheelVM vm = CreateVm();
            FilterInfo luminance = new FilterInfo("L", 100, 0);
            FilterInfo missing = new FilterInfo("Ha", 150, 5);
            Mock<IFilterWheel> filterWheel = CreateFilterWheel(connects: true, currentPosition: 0, luminance);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(filterWheel.Object);
            await vm.Connect();

            FilterInfo selected = await vm.ChangeFilter(missing, CancellationToken.None);

            selected.Should().BeNull();
            filterWheel.Object.Position.Should().Be(0);
            vm.FilterWheelInfo.SelectedFilter.Should().BeSameAs(luminance);
        }

        /// <summary>
        /// Verifies that disconnect resets state, calls the driver disconnect method, raises Disconnected, and broadcasts disconnected info.
        /// This protects cleanup paths used by profile changes, manual disconnects, and failed connection retries.
        /// </summary>
        [Test]
        public async Task Disconnect_AfterConnected_ClearsStateAndBroadcastsDisconnectedInfo() {
            FilterWheelVM vm = CreateVm();
            Mock<IFilterWheel> filterWheel = CreateFilterWheel(connects: true, currentPosition: 0, new FilterInfo("L", 0, 0));
            bool disconnectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(filterWheel.Object);
            vm.Disconnected += (_, _) => {
                disconnectedRaised = true;
                return Task.CompletedTask;
            };
            await vm.Connect();

            await vm.Disconnect();

            disconnectedRaised.Should().BeTrue();
            vm.FW.Should().BeNull();
            vm.FilterWheelInfo.Connected.Should().BeFalse();
            vm.GetAllFilters().Should().BeNull();
            filterWheel.Verify(x => x.Disconnect(), Times.AtLeastOnce);
            filterWheelMediator.Verify(x => x.Broadcast(It.Is<FilterWheelInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that driver action and command methods are gated by connection state and forwarded only while connected.
        /// This protects plugin and diagnostics callers from sending commands to an absent filter wheel.
        /// </summary>
        [Test]
        public async Task DriverCommands_AreForwardedOnlyWhenConnected() {
            FilterWheelVM vm = CreateVm();
            Mock<IFilterWheel> filterWheel = CreateFilterWheel(connects: true, currentPosition: 0, new FilterInfo("L", 0, 0));
            filterWheel.Setup(x => x.Action("calibrate", "fast")).Returns("calibrated");
            filterWheel.Setup(x => x.SendCommandString(":GV#", true)).Returns("version");
            filterWheel.Setup(x => x.SendCommandBool(":CHK#", false)).Returns(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(filterWheel.Object);

            vm.Action("calibrate", "fast").Should().BeNull();
            vm.SendCommandString(":GV#").Should().BeNull();
            vm.SendCommandBool(":CHK#", false).Should().BeFalse();
            vm.SendCommandBlind(":STOP#", false);
            filterWheel.Verify(x => x.SendCommandBlind(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            await vm.Connect();

            vm.Action("calibrate", "fast").Should().Be("calibrated");
            vm.SendCommandString(":GV#").Should().Be("version");
            vm.SendCommandBool(":CHK#", false).Should().BeTrue();
            vm.SendCommandBlind(":STOP#", false);
            filterWheel.Verify(x => x.SendCommandBlind(":STOP#", false), Times.Once);
        }

        private FilterWheelVM CreateVm() {
            return new FilterWheelVM(profileService.Object, filterWheelMediator.Object, focuserMediator.Object, guiderMediator.Object, deviceChooser.Object, applicationStatusMediator.Object);
        }

        private static Mock<IFilterWheel> CreateFilterWheel(bool connects, short currentPosition, params FilterInfo[] filters) {
            Mock<IFilterWheel> filterWheel = new Mock<IFilterWheel>();
            AsyncObservableCollection<FilterInfo> filterCollection = new AsyncObservableCollection<FilterInfo>();
            bool connected = false;
            foreach (FilterInfo filter in filters.OrderBy(x => x.Position)) {
                filterCollection.Add(filter);
            }

            filterWheel.SetupGet(x => x.Id).Returns("filter-wheel");
            filterWheel.SetupGet(x => x.Name).Returns("Filter Wheel");
            filterWheel.SetupGet(x => x.DisplayName).Returns("Filter Wheel Display");
            filterWheel.SetupGet(x => x.Description).Returns("Filter wheel description");
            filterWheel.SetupGet(x => x.DriverInfo).Returns("Driver info");
            filterWheel.SetupGet(x => x.DriverVersion).Returns("1.2.3");
            filterWheel.SetupGet(x => x.SupportedActions).Returns(new List<string> { "calibrate" });
            filterWheel.SetupGet(x => x.Filters).Returns(filterCollection);
            filterWheel.SetupGet(x => x.FocusOffsets).Returns(filters.Select(x => x.FocusOffset).ToArray());
            filterWheel.SetupGet(x => x.Names).Returns(filters.Select(x => x.Name).ToArray());
            filterWheel.SetupGet(x => x.Connected).Returns(() => connected);
            filterWheel.SetupProperty(x => x.Position, currentPosition);
            filterWheel.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Returns(() => {
                connected = connects;
                return Task.FromResult(connects);
            });
            filterWheel.Setup(x => x.Disconnect()).Callback(() => connected = false);
            return filterWheel;
        }

        private static void EnsureApplicationResources() {
            if (Application.Current == null) {
                _ = new Application();
            }

            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["FWSVG"] = new GeometryGroup();
        }
    }
}
