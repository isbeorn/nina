using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {
    [TestFixture]
    [Explicit("Constructs a WPF Application; run TelescopeSyncTest alone.")]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class TelescopeSyncTest {
        private Mock<IProfileService> profile;
        private Mock<ITelescope> telescope;
        private TelescopeVM vm;
        private DeviceUpdateTimer timer;

        [SetUp]
        public void SetUp() {
            if (Application.Current == null) {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["TelescopeSVG"] = new GeometryGroup();
            profile = new Mock<IProfileService> { DefaultValue = DefaultValue.Mock };
            profile.SetupGet(x => x.ActiveProfile.TelescopeSettings.SettleTime).Returns(1);
            profile.SetupGet(x => x.ActiveProfile.ApplicationSettings.DevicePollingInterval).Returns(0.05);
            var chooser = new Mock<IDeviceChooserVM>();
            chooser.SetupGet(x => x.Devices).Returns(new List<IDevice>());
            chooser.Setup(x => x.GetEquipment()).Returns(Task.CompletedTask);
            vm = new TelescopeVM(profile.Object, Mock.Of<ITelescopeMediator>(),
                Mock.Of<IApplicationStatusMediator>(), Mock.Of<IDomeMediator>(), chooser.Object);
            telescope = new Mock<ITelescope>();
            telescope.SetupGet(x => x.Connected).Returns(true);
            telescope.SetupGet(x => x.Coordinates).Returns(CoordinatesAt(0));
            typeof(TelescopeVM).GetField("_telescope", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(vm, telescope.Object);
            vm.TelescopeInfo.Connected = true;
            vm.TelescopeInfo.EquatorialSystem = Epoch.J2000;
            timer = new DeviceUpdateTimer(() => new Dictionary<string, object>(), _ => { }, 0.01);
            typeof(TelescopeVM).GetField("updateTimer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(vm, timer);
        }

        [TearDown]
        public async Task TearDown() {
            await timer.Stop();
        }

        [Test]
        public async Task Sync_WhenDriverRejects_ReturnsImmediately() {
            telescope.Setup(x => x.Sync(It.IsAny<Coordinates>())).Returns(false);
            var watch = Stopwatch.StartNew();
            (await vm.Sync(CoordinatesAt(1)).WaitAsync(TimeSpan.FromSeconds(4))).Should().BeFalse();
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(0.8));
        }

        [TestCase(0)]
        [TestCase(359.9999)]
        public async Task Sync_WhenPositionConverges_DoesNotWaitForFixedSettleDelay(double targetRa) {
            var target = CoordinatesAt(targetRa);
            telescope.Setup(x => x.Sync(It.IsAny<Coordinates>())).Callback(() =>
                telescope.SetupGet(x => x.Coordinates).Returns(target)).Returns(true);
            _ = Task.Run(timer.Run);
            var watch = Stopwatch.StartNew();
            (await vm.Sync(target).WaitAsync(TimeSpan.FromSeconds(4))).Should().BeTrue();
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(0.8));
        }

        [Test]
        public async Task Sync_WhenPollingStalls_ReturnsWithinSettleDeadline() {
            telescope.Setup(x => x.Sync(It.IsAny<Coordinates>())).Returns(true);
            timer.Interval = 30;
            _ = Task.Run(timer.Run);
            var watch = Stopwatch.StartNew();
            (await vm.Sync(CoordinatesAt(90)).WaitAsync(TimeSpan.FromSeconds(3))).Should().BeTrue();
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public async Task Sync_WhenDisconnectedOrDisabled_DoesNotCallDriver(bool connected, bool noSync) {
            vm.TelescopeInfo.Connected = connected;
            profile.SetupGet(x => x.ActiveProfile.TelescopeSettings.NoSync).Returns(noSync);
            (await vm.Sync(CoordinatesAt(90))).Should().BeFalse();
            telescope.Verify(x => x.Sync(It.IsAny<Coordinates>()), Times.Never);
        }

        private static Coordinates CoordinatesAt(double ra) => new Coordinates(ra, 0, Epoch.J2000, Coordinates.RAType.Degrees);
    }
}
