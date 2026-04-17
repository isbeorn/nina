using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GuiderVMTest {
        private Mock<IProfileService> profileService;
        private Mock<IProfile> profile;
        private Mock<IAstrometrySettings> astrometrySettings;
        private Mock<ICameraSettings> cameraSettings;
        private Mock<ITelescopeSettings> telescopeSettings;
        private Mock<IGuiderSettings> guiderSettings;
        private Mock<IGuiderMediator> guiderMediator;
        private Mock<IApplicationStatusMediator> applicationStatusMediator;
        private Mock<IDeviceChooserVM> deviceChooser;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();

            profileService = new Mock<IProfileService>();
            profile = new Mock<IProfile>();
            astrometrySettings = new Mock<IAstrometrySettings>();
            cameraSettings = new Mock<ICameraSettings>();
            telescopeSettings = new Mock<ITelescopeSettings>();
            guiderSettings = new Mock<IGuiderSettings>();
            guiderMediator = new Mock<IGuiderMediator>();
            applicationStatusMediator = new Mock<IApplicationStatusMediator>();
            deviceChooser = new Mock<IDeviceChooserVM>();

            cameraSettings.SetupProperty(x => x.PixelSize, 3.76);
            telescopeSettings.SetupProperty(x => x.FocalLength, 600);
            guiderSettings.SetupProperty(x => x.GuiderName, string.Empty);
            guiderSettings.SetupProperty(x => x.PHD2HistorySize, 100);
            guiderSettings.SetupProperty(x => x.PHD2GuiderScale, GuiderScaleEnum.ARCSECONDS);
            guiderSettings.SetupProperty(x => x.MaxY, 4);
            guiderSettings.SetupProperty(x => x.DitherPixels, 5);
            profile.SetupGet(x => x.AstrometrySettings).Returns(astrometrySettings.Object);
            profile.SetupGet(x => x.CameraSettings).Returns(cameraSettings.Object);
            profile.SetupGet(x => x.TelescopeSettings).Returns(telescopeSettings.Object);
            profile.SetupGet(x => x.GuiderSettings).Returns(guiderSettings.Object);
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            deviceChooser.SetupGet(x => x.Devices).Returns(new List<IDevice>());
            deviceChooser.Setup(x => x.GetEquipment()).Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Verifies that a successful guider connection stores capabilities, persists the profile selection,
        /// broadcasts a connected GuiderInfo snapshot, subscribes to guider updates, and raises Connected.
        /// This protects mediator consumers and dithering calculations that need guider metadata immediately after connection.
        /// </summary>
        [Test]
        public async Task Connect_WhenGuiderConnects_PopulatesInfoPersistsProfileAndBroadcastsConnectedInfo() {
            GuiderVM vm = CreateVm();
            Mock<IGuider> guider = CreateGuider(connects: true);
            bool connectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(guider.Object);
            vm.Connected += (_, _) => {
                connectedRaised = true;
                return Task.CompletedTask;
            };

            bool connected = await vm.Connect();

            connected.Should().BeTrue();
            connectedRaised.Should().BeTrue();
            vm.Guider.Should().BeSameAs(guider.Object);
            vm.GuiderInfo.Connected.Should().BeTrue();
            vm.GuiderInfo.DeviceId.Should().Be("guider");
            vm.GuiderInfo.CanClearCalibration.Should().BeTrue();
            vm.GuiderInfo.CanSetShiftRate.Should().BeTrue();
            vm.GuiderInfo.PixelScale.Should().Be(1.5);
            guiderSettings.Object.GuiderName.Should().Be("guider");
            guiderMediator.Verify(x => x.RegisterHandler(vm), Times.Once);
            guiderMediator.Verify(x => x.Broadcast(It.Is<GuiderInfo>(info => info.Connected && info.DeviceId == "guider" && info.PixelScale == 1.5)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that a failed guider connection clears state and broadcasts disconnected info.
        /// This protects retry behavior when a guider driver reports that it could not connect.
        /// </summary>
        [Test]
        public async Task Connect_WhenGuiderReturnsFalse_LeavesDisconnectedState() {
            GuiderVM vm = CreateVm();
            Mock<IGuider> guider = CreateGuider(connects: false);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(guider.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            vm.Guider.Should().BeNull();
            vm.GuiderInfo.Connected.Should().BeFalse();
            guiderMediator.Verify(x => x.Broadcast(It.Is<GuiderInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that disconnect detaches the guider, resets info, calls the driver disconnect method, raises Disconnected,
        /// and broadcasts a disconnected GuiderInfo snapshot.
        /// This protects cleanup paths used when profiles change or guiding hardware is manually disconnected.
        /// </summary>
        [Test]
        public async Task Disconnect_AfterConnected_ClearsStateAndBroadcastsDisconnectedInfo() {
            GuiderVM vm = CreateVm();
            Mock<IGuider> guider = CreateGuider(connects: true);
            bool disconnectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(guider.Object);
            vm.Disconnected += (_, _) => {
                disconnectedRaised = true;
                return Task.CompletedTask;
            };
            await vm.Connect();

            await vm.Disconnect();

            disconnectedRaised.Should().BeTrue();
            vm.Guider.Should().BeNull();
            vm.GuiderInfo.Connected.Should().BeFalse();
            guider.Verify(x => x.Disconnect(), Times.AtLeastOnce);
            guiderMediator.Verify(x => x.Broadcast(It.Is<GuiderInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that guiding start/stop calls are forwarded only while connected and raise the corresponding lifecycle events.
        /// This protects sequencing paths that depend on reliable guiding started/stopped notifications.
        /// </summary>
        [Test]
        public async Task GuidingCommands_AreForwardedOnlyWhenConnectedAndRaiseLifecycleEvents() {
            GuiderVM vm = CreateVm();
            Mock<IGuider> guider = CreateGuider(connects: true);
            bool guidingStarted = false;
            bool guidingStopped = false;
            guider.Setup(x => x.StartGuiding(true, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            guider.Setup(x => x.StopGuiding(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(guider.Object);
            vm.GuidingStarted += (_, _) => {
                guidingStarted = true;
                return Task.CompletedTask;
            };
            vm.GuidingStopped += (_, _) => {
                guidingStopped = true;
                return Task.CompletedTask;
            };

            (await vm.StartGuiding(true, null, CancellationToken.None)).Should().BeFalse();
            (await vm.StopGuiding(CancellationToken.None)).Should().BeFalse();

            await vm.Connect();

            (await vm.StartGuiding(true, null, CancellationToken.None)).Should().BeTrue();
            (await vm.StopGuiding(CancellationToken.None)).Should().BeTrue();
            guidingStarted.Should().BeTrue();
            guidingStopped.Should().BeTrue();
            guider.Verify(x => x.StartGuiding(true, It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
            guider.Verify(x => x.StopGuiding(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies that common driver command calls are gated by connection state and forwarded only while connected.
        /// This protects plugin and diagnostics callers from sending commands to an absent guider.
        /// </summary>
        [Test]
        public async Task DriverCommands_AreForwardedOnlyWhenConnected() {
            GuiderVM vm = CreateVm();
            Mock<IGuider> guider = CreateGuider(connects: true);
            guider.Setup(x => x.Action("calibrate", "fast")).Returns("calibrated");
            guider.Setup(x => x.SendCommandString(":GV#", true)).Returns("version");
            guider.Setup(x => x.SendCommandBool(":CHK#", false)).Returns(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(guider.Object);

            vm.Action("calibrate", "fast").Should().BeNull();
            vm.SendCommandString(":GV#").Should().BeNull();
            vm.SendCommandBool(":CHK#", false).Should().BeFalse();
            vm.SendCommandBlind(":STOP#", false);
            guider.Verify(x => x.SendCommandBlind(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            await vm.Connect();

            vm.Action("calibrate", "fast").Should().Be("calibrated");
            vm.SendCommandString(":GV#").Should().Be("version");
            vm.SendCommandBool(":CHK#", false).Should().BeTrue();
            vm.SendCommandBlind(":STOP#", false);
            guider.Verify(x => x.SendCommandBlind(":STOP#", false), Times.Once);
        }

        /// <summary>
        /// Verifies that guider property changes update derived dither values and broadcast connection changes.
        /// This protects live guider state updates that arrive after the initial connection snapshot.
        /// </summary>
        [Test]
        public async Task GuiderPropertyChanges_UpdateDerivedStateAndBroadcastConnectionChanges() {
            GuiderVM vm = CreateVm();
            Mock<IGuider> guider = CreateGuider(connects: true);
            double pixelScale = 1.5;
            bool connected = true;
            guider.SetupGet(x => x.PixelScale).Returns(() => pixelScale);
            guider.SetupGet(x => x.Connected).Returns(() => connected);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(guider.Object);
            await vm.Connect();
            double originalMainCameraDitherPixels = vm.MainCameraDitherPixels;

            pixelScale = 3.0;
            guider.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IGuider.PixelScale)));

            vm.GuideStepsHistory.PixelScale.Should().Be(3.0);
            vm.MainCameraDitherPixels.Should().NotBe(originalMainCameraDitherPixels);

            connected = false;
            guider.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IGuider.Connected)));

            vm.GuiderInfo.Connected.Should().BeFalse();
            guiderMediator.Verify(x => x.Broadcast(It.Is<GuiderInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that shift-rate, stop-shifting, and lock-position calls are safe no-ops when no guider is connected.
        /// This protects mediator callers from null-reference failures during disconnected or profile-transition states.
        /// </summary>
        [Test]
        public async Task DisconnectedGuiderControlCalls_ReturnFalseOrNullWithoutThrowing() {
            GuiderVM vm = CreateVm();

            Func<Task> setShiftRate = async () => {
                bool result = await vm.SetShiftRate(SiderealShiftTrackingRate.Disabled, CancellationToken.None);
                result.Should().BeFalse();
            };
            Func<Task> stopShifting = async () => {
                bool result = await vm.StopShifting(CancellationToken.None);
                result.Should().BeFalse();
            };
            Action getLockPosition = () => vm.GetLockPosition().Should().BeNull();

            await setShiftRate.Should().NotThrowAsync();
            await stopShifting.Should().NotThrowAsync();
            getLockPosition.Should().NotThrow();
        }

        private GuiderVM CreateVm() {
            return new GuiderVM(profileService.Object, guiderMediator.Object, applicationStatusMediator.Object, deviceChooser.Object);
        }

        private static Mock<IGuider> CreateGuider(bool connects) {
            Mock<IGuider> guider = new Mock<IGuider>();
            bool connected = false;
            guider.SetupGet(x => x.Id).Returns("guider");
            guider.SetupGet(x => x.Name).Returns("Guider");
            guider.SetupGet(x => x.DisplayName).Returns("Guider Display");
            guider.SetupGet(x => x.Description).Returns("Guider description");
            guider.SetupGet(x => x.DriverInfo).Returns("Driver info");
            guider.SetupGet(x => x.DriverVersion).Returns("1.2.3");
            guider.SetupGet(x => x.SupportedActions).Returns(new List<string> { "calibrate" });
            guider.SetupGet(x => x.Connected).Returns(() => connected);
            guider.SetupGet(x => x.PixelScale).Returns(1.5);
            guider.SetupGet(x => x.CanClearCalibration).Returns(true);
            guider.SetupGet(x => x.CanSetShiftRate).Returns(true);
            guider.SetupGet(x => x.CanGetLockPosition).Returns(true);
            guider.SetupGet(x => x.ShiftEnabled).Returns(true);
            guider.SetupGet(x => x.ShiftRate).Returns(SiderealShiftTrackingRate.Disabled);
            guider.Setup(x => x.Connect(It.IsAny<CancellationToken>())).Returns(() => {
                connected = connects;
                return Task.FromResult(connects);
            });
            guider.Setup(x => x.Disconnect()).Callback(() => connected = false);
            return guider;
        }

        private static void EnsureApplicationResources() {
            if (Application.Current == null) {
                _ = new Application();
            }

            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["GuiderSVG"] = new GeometryGroup();
        }
    }
}
