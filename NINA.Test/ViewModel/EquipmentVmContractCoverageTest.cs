using FluentAssertions;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class EquipmentVmContractCoverageTest {

        /// <summary>
        /// Verifies the complete concrete IDockableVM surface in NINA.WPF.Base so the test suite inventory fails when a new dockable VM is added without deliberate coverage.
        /// This protects the coverage goal by making the expected dockable view-model set explicit.
        /// </summary>
        [Test]
        public void ConcreteDockableVmTypes_MatchExpectedCoverageInventory() {
            Type[] expectedTypes = {
                typeof(DockableVM),
                typeof(CameraVM),
                typeof(DomeVM),
                typeof(FilterWheelVM),
                typeof(FlatDeviceVM),
                typeof(FocuserVM),
                typeof(GuiderVM),
                typeof(RotatorVM),
                typeof(SafetyMonitorVM),
                typeof(SwitchVM),
                typeof(TelescopeVM),
                typeof(WeatherDataVM)
            };

            Type[] actualTypes = typeof(DockableVM).Assembly.GetTypes()
                .Where(type => typeof(IDockableVM).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false })
                .OrderBy(type => type.FullName)
                .ToArray();

            actualTypes.Should().BeEquivalentTo(expectedTypes);
        }

        /// <summary>
        /// Verifies the complete concrete IDeviceVM surface in NINA.WPF.Base so each equipment mediator handler remains covered by focused tests.
        /// This protects the common connect, disconnect, broadcast, and command-forwarding contract shared by equipment VMs.
        /// </summary>
        [Test]
        public void ConcreteDeviceVmTypes_MatchExpectedCoverageInventory() {
            Type[] expectedTypes = {
                typeof(CameraVM),
                typeof(DomeVM),
                typeof(FilterWheelVM),
                typeof(FlatDeviceVM),
                typeof(FocuserVM),
                typeof(GuiderVM),
                typeof(RotatorVM),
                typeof(SafetyMonitorVM),
                typeof(SwitchVM),
                typeof(TelescopeVM),
                typeof(WeatherDataVM)
            };

            Type[] actualTypes = typeof(DockableVM).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && ImplementsDeviceVm(type))
                .OrderBy(type => type.FullName)
                .ToArray();

            actualTypes.Should().BeEquivalentTo(expectedTypes);
        }

        private static bool ImplementsDeviceVm(Type type) {
            return type.GetInterfaces().Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(IDeviceVM<>));
        }
    }
}
