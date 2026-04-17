using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TelescopeVMTest {
        private Mock<IProfileService> profileService;
        private Mock<IProfile> profile;
        private Mock<IApplicationSettings> applicationSettings;
        private Mock<IAstrometrySettings> astrometrySettings;
        private Mock<ITelescopeSettings> telescopeSettings;
        private Mock<ITelescopeMediator> telescopeMediator;
        private Mock<IApplicationStatusMediator> applicationStatusMediator;
        private Mock<IDomeMediator> domeMediator;
        private Mock<IDeviceChooserVM> deviceChooser;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();

            profileService = new Mock<IProfileService>();
            profile = new Mock<IProfile>();
            applicationSettings = new Mock<IApplicationSettings>();
            astrometrySettings = new Mock<IAstrometrySettings>();
            telescopeSettings = new Mock<ITelescopeSettings>();
            telescopeMediator = new Mock<ITelescopeMediator>();
            applicationStatusMediator = new Mock<IApplicationStatusMediator>();
            domeMediator = new Mock<IDomeMediator>();
            deviceChooser = new Mock<IDeviceChooserVM>();

            applicationSettings.SetupProperty(x => x.DevicePollingInterval, 0);
            astrometrySettings.SetupProperty(x => x.Latitude, 52.5);
            astrometrySettings.SetupProperty(x => x.Longitude, 13.4);
            astrometrySettings.SetupProperty(x => x.Elevation, 45);
            telescopeSettings.SetupProperty(x => x.Id, string.Empty);
            telescopeSettings.SetupProperty(x => x.LastDeviceName, string.Empty);
            telescopeSettings.SetupProperty(x => x.TelescopeLocationSyncDirection, TelescopeLocationSyncDirection.NOSYNC);
            telescopeSettings.SetupProperty(x => x.SnapPortStart, "START");
            telescopeSettings.SetupProperty(x => x.SnapPortStop, "STOP");
            profile.SetupGet(x => x.ApplicationSettings).Returns(applicationSettings.Object);
            profile.SetupGet(x => x.AstrometrySettings).Returns(astrometrySettings.Object);
            profile.SetupGet(x => x.TelescopeSettings).Returns(telescopeSettings.Object);
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            deviceChooser.SetupGet(x => x.Devices).Returns(new List<IDevice>());
            deviceChooser.Setup(x => x.GetEquipment()).Returns(Task.CompletedTask);
            domeMediator.Setup(x => x.GetInfo()).Returns(new DomeInfo());
        }

        /// <summary>
        /// Verifies that a successful telescope connection populates mount state, persists profile selection,
        /// filters custom tracking mode from the UI list, broadcasts connected info, and raises Connected.
        /// This protects mediator consumers that need mount state immediately after connection.
        /// </summary>
        [Test]
        public async Task Connect_WhenTelescopeConnects_PopulatesInfoPersistsProfileAndBroadcastsConnectedInfo() {
            TelescopeVM vm = CreateVm();
            Mock<ITelescope> telescope = CreateTelescope(connects: true);
            bool connectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(telescope.Object);
            vm.Connected += (_, _) => {
                connectedRaised = true;
                return Task.CompletedTask;
            };

            bool connected = await vm.Connect();

            connected.Should().BeTrue();
            connectedRaised.Should().BeTrue();
            vm.Telescope.Should().BeSameAs(telescope.Object);
            vm.TelescopeInfo.Connected.Should().BeTrue();
            vm.TelescopeInfo.DeviceId.Should().Be("telescope");
            vm.TelescopeInfo.RightAscension.Should().Be(12.5);
            vm.TelescopeInfo.Declination.Should().Be(-31.25);
            vm.TelescopeInfo.SiteLatitude.Should().Be(52.5);
            vm.SupportedTrackingModes.Should().Contain(TrackingMode.Sidereal);
            vm.SupportedTrackingModes.Should().NotContain(TrackingMode.Custom);
            telescopeSettings.Object.Id.Should().Be("telescope");
            telescopeSettings.Object.LastDeviceName.Should().Be("Telescope Display");
            telescopeMediator.Verify(x => x.RegisterHandler(vm), Times.Once);
            telescopeMediator.Verify(x => x.Broadcast(It.Is<TelescopeInfo>(info => info.Connected && info.DeviceId == "telescope" && info.RightAscension == 12.5)), Times.AtLeastOnce);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && !string.IsNullOrEmpty(status.Status))), Times.Once);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && status.Status == string.Empty)), Times.Once);
        }

        /// <summary>
        /// Verifies that selecting the dummy "No_Device" telescope choice persists the disabled-device state without connecting.
        /// This protects profiles that intentionally run without a telescope.
        /// </summary>
        [Test]
        public async Task Connect_WhenNoDeviceSelected_PersistsNoDeviceAndDoesNotConnect() {
            TelescopeVM vm = CreateVm();
            Mock<ITelescope> telescope = CreateTelescope(connects: true);
            telescope.SetupGet(x => x.Id).Returns("No_Device");
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(telescope.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            telescopeSettings.Object.Id.Should().Be("No_Device");
            telescopeSettings.Object.LastDeviceName.Should().BeEmpty();
            telescope.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies that a driver-reported telescope connection failure leaves the VM disconnected and clears the mount reference.
        /// This protects retry behavior after a mount driver declines a connection without throwing.
        /// </summary>
        [Test]
        public async Task Connect_WhenTelescopeReturnsFalse_LeavesDisconnectedState() {
            TelescopeVM vm = CreateVm();
            Mock<ITelescope> telescope = CreateTelescope(connects: false);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(telescope.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            vm.Telescope.Should().BeNull();
            vm.TelescopeInfo.Connected.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that disconnect resets mount state, calls the driver disconnect method, raises Disconnected,
        /// and broadcasts a disconnected TelescopeInfo snapshot.
        /// This protects cleanup paths used when profiles change or equipment is manually disconnected.
        /// </summary>
        [Test]
        public async Task Disconnect_AfterConnected_ClearsStateAndBroadcastsDisconnectedInfo() {
            TelescopeVM vm = CreateVm();
            Mock<ITelescope> telescope = CreateTelescope(connects: true);
            bool disconnectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(telescope.Object);
            vm.Disconnected += (_, _) => {
                disconnectedRaised = true;
                return Task.CompletedTask;
            };
            await vm.Connect();

            await vm.Disconnect();

            disconnectedRaised.Should().BeTrue();
            vm.Telescope.Should().BeNull();
            vm.TelescopeInfo.Connected.Should().BeFalse();
            telescope.Verify(x => x.Disconnect(), Times.AtLeastOnce);
            telescopeMediator.Verify(x => x.Broadcast(It.Is<TelescopeInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that common driver command calls are gated by connection state and forwarded only while connected.
        /// This protects plugin and diagnostics callers from sending commands to an absent telescope.
        /// </summary>
        [Test]
        public async Task DriverCommands_AreForwardedOnlyWhenConnected() {
            TelescopeVM vm = CreateVm();
            Mock<ITelescope> telescope = CreateTelescope(connects: true);
            telescope.Setup(x => x.Action("park-check", "now")).Returns("ok");
            telescope.Setup(x => x.SendCommandString(":GV#", true)).Returns("version");
            telescope.Setup(x => x.SendCommandBool(":CHK#", false)).Returns(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(telescope.Object);

            vm.Action("park-check", "now").Should().BeNull();
            vm.SendCommandString(":GV#").Should().BeNull();
            vm.SendCommandBool(":CHK#", false).Should().BeFalse();
            vm.SendCommandBlind(":STOP#", false);
            telescope.Verify(x => x.SendCommandBlind(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            await vm.Connect();

            vm.Action("park-check", "now").Should().Be("ok");
            vm.SendCommandString(":GV#").Should().Be("version");
            vm.SendCommandBool(":CHK#", false).Should().BeTrue();
            vm.SendCommandBlind(":STOP#", false);
            telescope.Verify(x => x.SendCommandBlind(":STOP#", false), Times.Once);
        }

        /// <summary>
        /// Verifies that mount control helpers honor connection and capability state before forwarding to the driver.
        /// This protects sequencer and UI commands for tracking, snap-port, destination pier side, and current coordinates.
        /// </summary>
        [Test]
        public async Task MountHelpers_WhenConnected_ForwardToDriverAndReturnExpectedValues() {
            TelescopeVM vm = CreateVm();
            Mock<ITelescope> telescope = CreateTelescope(connects: true);
            Coordinates destination = new Coordinates(Angle.ByHours(10), Angle.ByDegree(20), Epoch.J2000);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(telescope.Object);

            vm.SetTrackingEnabled(true).Should().BeFalse();
            vm.SendToSnapPort(true).Should().BeFalse();
            vm.DestinationSideOfPier(destination).Should().Be(PierSide.pierUnknown);

            await vm.Connect();

            vm.SetTrackingEnabled(true).Should().BeTrue();
            vm.SendToSnapPort(true).Should().BeTrue();
            vm.GetCurrentPosition().Should().BeSameAs(telescope.Object.Coordinates);
            vm.DestinationSideOfPier(destination).Should().Be(PierSide.pierEast);
            telescope.VerifySet(x => x.TrackingEnabled = true, Times.Once);
            telescope.Verify(x => x.SendCommandString("START", true), Times.Once);
            telescope.Verify(x => x.DestinationSideOfPier(destination), Times.Once);
        }

        private TelescopeVM CreateVm() {
            return new TelescopeVM(profileService.Object, telescopeMediator.Object, applicationStatusMediator.Object, domeMediator.Object, deviceChooser.Object);
        }

        private static Mock<ITelescope> CreateTelescope(bool connects) {
            Mock<ITelescope> telescope = new Mock<ITelescope>();
            bool connected = false;
            Coordinates coordinates = new Coordinates(Angle.ByHours(12.5), Angle.ByDegree(-31.25), Epoch.J2000);
            telescope.SetupGet(x => x.Id).Returns("telescope");
            telescope.SetupGet(x => x.Name).Returns("Telescope");
            telescope.SetupGet(x => x.DisplayName).Returns("Telescope Display");
            telescope.SetupGet(x => x.Description).Returns("Telescope description");
            telescope.SetupGet(x => x.DriverInfo).Returns("Driver info");
            telescope.SetupGet(x => x.DriverVersion).Returns("1.2.3");
            telescope.SetupGet(x => x.SupportedActions).Returns(new List<string> { "park-check" });
            telescope.SetupGet(x => x.Connected).Returns(() => connected);
            telescope.SetupGet(x => x.Coordinates).Returns(coordinates);
            telescope.SetupGet(x => x.RightAscension).Returns(12.5);
            telescope.SetupGet(x => x.RightAscensionString).Returns("12:30:00");
            telescope.SetupGet(x => x.Declination).Returns(-31.25);
            telescope.SetupGet(x => x.DeclinationString).Returns("-31:15:00");
            telescope.SetupGet(x => x.SiderealTime).Returns(13.25);
            telescope.SetupGet(x => x.SiderealTimeString).Returns("13:15:00");
            telescope.SetupGet(x => x.Altitude).Returns(55.5);
            telescope.SetupGet(x => x.AltitudeString).Returns("55:30:00");
            telescope.SetupGet(x => x.Azimuth).Returns(182.25);
            telescope.SetupGet(x => x.AzimuthString).Returns("182:15:00");
            telescope.SetupGet(x => x.HoursToMeridianString).Returns("01:00:00");
            telescope.SetupGet(x => x.TimeToMeridianFlip).Returns(3600);
            telescope.SetupGet(x => x.TimeToMeridianFlipString).Returns("01:00:00");
            telescope.SetupProperty(x => x.TrackingEnabled, true);
            telescope.SetupGet(x => x.TrackingModes).Returns(new List<TrackingMode> { TrackingMode.Sidereal, TrackingMode.Custom, TrackingMode.Lunar });
            telescope.SetupGet(x => x.TrackingRate).Returns(new TrackingRate { TrackingMode = TrackingMode.Sidereal });
            telescope.SetupGet(x => x.SiteLatitude).Returns(52.5);
            telescope.SetupGet(x => x.SiteLongitude).Returns(13.4);
            telescope.SetupGet(x => x.SiteElevation).Returns(45);
            telescope.SetupGet(x => x.EquatorialSystem).Returns(Epoch.J2000);
            telescope.SetupGet(x => x.SideOfPier).Returns(PierSide.pierWest);
            telescope.SetupGet(x => x.CanSetTrackingEnabled).Returns(true);
            telescope.SetupGet(x => x.CanFindHome).Returns(true);
            telescope.SetupGet(x => x.CanPark).Returns(true);
            telescope.SetupGet(x => x.CanUnpark).Returns(true);
            telescope.SetupGet(x => x.CanSetPark).Returns(true);
            telescope.SetupGet(x => x.CanMovePrimaryAxis).Returns(true);
            telescope.SetupGet(x => x.CanMoveSecondaryAxis).Returns(true);
            telescope.SetupGet(x => x.CanSetDeclinationRate).Returns(true);
            telescope.SetupGet(x => x.CanSetRightAscensionRate).Returns(true);
            telescope.SetupGet(x => x.AlignmentMode).Returns(AlignmentMode.GermanPolar);
            telescope.SetupGet(x => x.CanPulseGuide).Returns(true);
            telescope.SetupGet(x => x.CanSetPierSide).Returns(true);
            telescope.SetupGet(x => x.CanSlew).Returns(true);
            telescope.SetupGet(x => x.CanSlewAltAz).Returns(true);
            telescope.SetupGet(x => x.UTCDate).Returns(new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc));
            telescope.Setup(x => x.GetAxisRates(It.IsAny<TelescopeAxes>())).Returns(new List<(double, double)> { (0, 1) });
            telescope.Setup(x => x.DestinationSideOfPier(It.IsAny<Coordinates>())).Returns(PierSide.pierEast);
            telescope.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Returns(() => {
                connected = connects;
                return Task.FromResult(connects);
            });
            telescope.Setup(x => x.Disconnect()).Callback(() => connected = false);
            return telescope;
        }

        private static void EnsureApplicationResources() {
            if (Application.Current == null) {
                _ = new Application();
            }

            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["TelescopeSVG"] = new GeometryGroup();
        }
    }
}
