using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.Hubs;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel.Equipment;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class HubConfigurationVMTest {

        [Test]
        public async Task ChildSelections_AreIndependentAndConnectTheDiscoveredInstances() {
            var first = SafetyDevice("first");
            var second = SafetyDevice("second");
            var settings = new DeviceHubSettings();
            var hub = new SafetyMonitorHub("Hub");
            using var vm = new HubConfigurationVM(Mock.Of<IProfileService>(), hub, settings,
                new List<IDevice> { first.Object, second.Object });

            vm.AddDeviceCommand.Execute(null);
            vm.AddDeviceCommand.Execute(null);
            vm.Rows[0].Chooser.SelectedDevice = first.Object;
            vm.Rows[1].Chooser.SelectedDevice = second.Object;

            settings.Devices.Select(device => device.Id).Should().Equal("first", "second");
            vm.Rows[0].Chooser.SelectedDevice.Should().BeSameAs(first.Object);
            vm.Rows[1].Chooser.SelectedDevice.Should().BeSameAs(second.Object);
            (await hub.Connect(CancellationToken.None)).Should().BeTrue();
            vm.IsEditable.Should().BeFalse();
            vm.AddDeviceCommand.CanExecute(null).Should().BeFalse();
            first.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Once);
            second.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Once);

            hub.Disconnect();
            vm.IsEditable.Should().BeTrue();
        }

        [Test]
        public void ChildChooser_PreservesOfflineSelectionAndExcludesNinaHubs() {
            var settings = new DeviceHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = "missing", LastDeviceName = "Saved", SourceNumber = 1 });
            using var vm = new HubConfigurationVM(Mock.Of<IProfileService>(), new SafetyMonitorHub("Hub"), settings,
                new List<IDevice> { SafetyDevice("present").Object, new SafetyMonitorHub("Nested") });

            vm.Rows.Single().Chooser.SelectedDevice.Should().BeOfType<OfflineDevice>();
            vm.Rows.Single().Chooser.SelectedDevice.Id.Should().Be("missing");
            vm.Rows.Single().Chooser.Devices.Should().NotContain(device => device is IEquipmentHub);
            settings.Devices.Single().Id.Should().Be("missing");
        }

        [Test]
        public void WeatherMappings_SurviveUnrelatedSelectionChangesAndClearOnlyRemovedSources() {
            var first = WeatherDevice("first");
            var second = WeatherDevice("second");
            var third = WeatherDevice("third");
            var concreteSettings = new WeatherHubSettings();
            var settingsContract = new Mock<IWeatherHubSettings>();
            settingsContract.SetupProperty(settings => settings.Devices, concreteSettings.Devices);
            settingsContract.SetupGet(settings => settings.Sources).Returns(() => concreteSettings.Sources);
            settingsContract.Setup(settings => settings.SetSource(It.IsAny<WeatherDataField>(), It.IsAny<string>()))
                .Callback<WeatherDataField, string>(concreteSettings.SetSource);
            IWeatherHubSettings settings = settingsContract.Object;
            settings.Devices.Add(new HubDeviceSettings { Id = "first", SourceNumber = 1 });
            settings.Devices.Add(new HubDeviceSettings { Id = "second", SourceNumber = 2 });
            settings.SetSource(WeatherDataField.Temperature, "first");
            using var vm = new HubConfigurationVM(Mock.Of<IProfileService>(), new WeatherDataHub("Hub"), settings,
                new List<IDevice> { first.Object, second.Object, third.Object });
            var temperature = vm.Fields.Single(field => field.Field == WeatherDataField.Temperature);

            vm.Rows[1].Chooser.SelectedDevice = third.Object;

            temperature.SourceId.Should().Be("first");
            temperature.SourceId = null!;
            temperature.SourceId.Should().Be("first", "temporary ComboBox null selections must preserve mappings");
            vm.Fields.Should().HaveCount(14);
            vm.RemoveDeviceCommand.Execute(vm.Rows[0]);
            temperature.SourceId.Should().BeEmpty();
            settings.Sources.Should().NotContainKey(WeatherDataField.Temperature);
            vm.Rows.Single().Settings.SourceNumber.Should().Be(2);
        }

        [Test]
        public async Task InvalidEditedConfiguration_DoesNotConnectPreviousValidSelection() {
            var first = SafetyDevice("first");
            var settings = new DeviceHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = "first", SourceNumber = 1 });
            using var vm = new HubConfigurationVM(Mock.Of<IProfileService>(), new SafetyMonitorHub("Hub"), settings,
                new List<IDevice> { first.Object });

            vm.AddDeviceCommand.Execute(null);

            (await vm.Hub.Connect(CancellationToken.None)).Should().BeFalse();
            first.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestCase(typeof(HubDeviceStatus), nameof(HubDeviceStatus.Connected), false)]
        [TestCase(typeof(HubDeviceStatus), nameof(HubDeviceStatus.IsSafe), false)]
        [TestCase(typeof(HubDeviceRowVM), nameof(HubDeviceRowVM.IsUsed), false)]
        [TestCase(typeof(EquipmentHub), nameof(EquipmentHub.IsSessionActive), true)]
        [TestCase(typeof(DeviceChooserVM<IDevice>), nameof(DeviceChooserVM<IDevice>.HubConfiguration), true)]
        public void RuntimeStateProperties_CannotBeWrittenByExternalConsumers(Type type, string property, bool privateSetter) {
            var setter = type.GetProperty(property)!.GetSetMethod(nonPublic: true)!;

            setter.IsPublic.Should().BeFalse();
            if (privateSetter) {
                setter.IsPrivate.Should().BeTrue();
            } else {
                setter.IsAssembly.Should().BeTrue();
            }
        }

        private static Mock<ISafetyMonitor> SafetyDevice(string id) {
            var device = new Mock<ISafetyMonitor>();
            device.SetupGet(value => value.Id).Returns(id);
            device.SetupGet(value => value.DisplayName).Returns(id);
            device.SetupGet(value => value.Connected).Returns(false);
            device.Setup(value => value.Connect(It.IsAny<CancellationToken>())).Callback(() =>
                device.SetupGet(value => value.Connected).Returns(true)).ReturnsAsync(true);
            device.Setup(value => value.Disconnect()).Callback(() => device.SetupGet(value => value.Connected).Returns(false));
            return device;
        }

        private static Mock<IWeatherData> WeatherDevice(string id) {
            var device = new Mock<IWeatherData>();
            device.SetupGet(value => value.Id).Returns(id);
            device.SetupGet(value => value.DisplayName).Returns(id);
            return device;
        }
    }
}
