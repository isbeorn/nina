using FluentAssertions;
using Moq;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel.Equipment;
using System.Threading;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class DeviceChooserVMTest {

        /// <summary>
        /// Verifies that a persisted device id selects the matching live device without inserting an offline placeholder.
        /// This protects equipment chooser startup behavior when the previously selected driver is still installed.
        /// </summary>
        [Test]
        public void DetermineSelectedDevice_WhenPersistedIdExists_SelectsMatchingDevice() {
            TestDeviceChooserVM chooser = CreateChooser();
            Mock<IDevice> matchingDevice = CreateDevice("camera-1", "Camera One");
            IList<IDevice> devices = new List<IDevice> {
                CreateDevice("camera-0", "Camera Zero").Object,
                matchingDevice.Object
            };

            chooser.Determine(devices, "camera-1", "Saved Camera");

            chooser.SelectedDevice.Should().BeSameAs(matchingDevice.Object);
            chooser.Devices.Should().Equal(devices);
            chooser.Devices.Should().HaveCount(2);
        }

        /// <summary>
        /// Verifies that a missing persisted device id is preserved as an offline placeholder at the top of the chooser.
        /// This protects compatibility with profiles that reference hardware not currently connected or installed.
        /// </summary>
        [Test]
        public void DetermineSelectedDevice_WhenPersistedIdIsMissing_InsertsOfflineDeviceAtTop() {
            TestDeviceChooserVM chooser = CreateChooser();
            IList<IDevice> devices = new List<IDevice> {
                CreateDevice("camera-0", "Camera Zero").Object
            };

            chooser.Determine(devices, "missing-camera", "Saved Camera");

            chooser.SelectedDevice.Id.Should().Be("missing-camera");
            chooser.SelectedDevice.Name.Should().Be("Saved Camera (OFFLINE)");
            chooser.Devices[0].Should().BeSameAs(chooser.SelectedDevice);
            chooser.Devices.Should().HaveCount(2);
        }

        /// <summary>
        /// Verifies that an empty provider result does not fabricate an offline device.
        /// This documents the chooser invariant that an offline placeholder is only added when there is a populated list to display.
        /// </summary>
        [Test]
        public void DetermineSelectedDevice_WhenDeviceListIsEmpty_LeavesSelectionUnset() {
            TestDeviceChooserVM chooser = CreateChooser();

            chooser.Determine(new List<IDevice>(), "missing-camera", "Saved Camera");

            chooser.SelectedDevice.Should().BeNull();
            chooser.Devices.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the setup dialog command opens the selected device setup on an STA thread and resets the busy flag afterward.
        /// This protects WPF/COM setup dialogs that require STA execution and ensures the UI command can be used again after completion.
        /// </summary>
        [Test]
        public async Task SetupDialogCommand_WhenDeviceHasSetupDialog_RunsDialogOnStaThreadAndResetsState() {
            TestDeviceChooserVM chooser = CreateChooser();
            ManualResetEventSlim setupEntered = new ManualResetEventSlim(false);
            ManualResetEventSlim releaseSetup = new ManualResetEventSlim(false);
            ApartmentState setupApartmentState = ApartmentState.Unknown;
            Mock<IDevice> setupDevice = CreateDevice("camera-1", "Camera One");
            setupDevice.SetupGet(x => x.HasSetupDialog).Returns(true);
            setupDevice.Setup(x => x.SetupDialog()).Callback(() => {
                setupApartmentState = Thread.CurrentThread.GetApartmentState();
                setupEntered.Set();
                releaseSetup.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            });

            chooser.SelectedDevice = setupDevice.Object;

            Task setupTask = chooser.SetupDialogCommand.ExecuteAsync(null);
            setupEntered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            chooser.SetupDialogOpen.Should().BeTrue();

            releaseSetup.Set();
            await setupTask;

            setupApartmentState.Should().Be(ApartmentState.STA);
            chooser.SetupDialogOpen.Should().BeFalse();
            chooser.SetupDialogNotOpen.Should().BeTrue();
            setupDevice.Verify(x => x.SetupDialog(), Times.Once);
        }

        /// <summary>
        /// Verifies that the setup dialog command is a no-op when the selected device does not expose a setup dialog.
        /// This prevents chooser state from entering a busy mode for drivers that cannot display configuration UI.
        /// </summary>
        [Test]
        public async Task SetupDialogCommand_WhenDeviceHasNoSetupDialog_DoesNotOpenDialog() {
            TestDeviceChooserVM chooser = CreateChooser();
            Mock<IDevice> setupDevice = CreateDevice("camera-1", "Camera One");
            setupDevice.SetupGet(x => x.HasSetupDialog).Returns(false);
            chooser.SelectedDevice = setupDevice.Object;

            await chooser.SetupDialogCommand.ExecuteAsync(null);

            chooser.SetupDialogOpen.Should().BeFalse();
            setupDevice.Verify(x => x.SetupDialog(), Times.Never);
        }

        private static TestDeviceChooserVM CreateChooser() {
            return new TestDeviceChooserVM(Mock.Of<IProfileService>(), Mock.Of<IEquipmentProviders<IDevice>>());
        }

        private static Mock<IDevice> CreateDevice(string id, string name) {
            Mock<IDevice> device = new Mock<IDevice>();
            device.SetupGet(x => x.Id).Returns(id);
            device.SetupGet(x => x.Name).Returns(name);
            device.SetupGet(x => x.DisplayName).Returns(name);
            return device;
        }

        private sealed class TestDeviceChooserVM : DeviceChooserVM<IDevice> {

            public TestDeviceChooserVM(IProfileService profileService, IEquipmentProviders<IDevice> equipmentProviders) : base(profileService, equipmentProviders) {
            }

            public override Task GetEquipment() {
                return Task.CompletedTask;
            }

            public void Determine(IList<IDevice> devices, string id, string name) {
                DetermineSelectedDevice(devices, id, name);
            }
        }
    }
}
