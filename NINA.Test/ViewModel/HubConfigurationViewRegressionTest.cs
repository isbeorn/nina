using FluentAssertions;
using Moq;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.CustomControlLibrary;
using NINA.Equipment.Equipment.Hubs;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.View.Equipment;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;

namespace NINA.Test.ViewModel {
    public partial class HubConfigurationViewTest {
        [TestCase("Safety", false)]
        [TestCase("Safety", true)]
        [TestCase("Switch", false)]
        [TestCase("Switch", true)]
        [TestCase("Weather", false)]
        [TestCase("Weather", true)]
        public void ProfileSwitchDuringSession_RebindsSettingsOnDisconnectAndReconnect(string category, bool faultSession) {
            using var secondProfile = new NINA.Profile.Profile();
            profile.ApplicationSettings.DevicePollingInterval = 60;
            secondProfile.ApplicationSettings.DevicePollingInterval = 60;
            var first = RegressionDevice(category, "first");
            var second = RegressionDevice(category, "second");
            var firstSettings = RegressionSettings(profile, category);
            var secondSettings = RegressionSettings(secondProfile, category);
            firstSettings.Devices.Add(new HubDeviceSettings { Id = "first", SourceNumber = 1 });
            secondSettings.Devices.Add(new HubDeviceSettings { Id = "second", SourceNumber = 1 });
            if (firstSettings is IWeatherHubSettings firstWeather && secondSettings is IWeatherHubSettings secondWeather) {
                firstWeather.SetSource(WeatherDataField.Temperature, "first");
                secondWeather.SetSource(WeatherDataField.Temperature, "second");
            }
            var hub = RegressionHub(category);
            SetRegressionDeviceId(profile, category, hub.Id);
            SetRegressionDeviceId(secondProfile, category, hub.Id);
            IProfile currentProfile = profile;
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(service => service.ActiveProfile).Returns(() => currentProfile);
            var chooser = new ViewChooser(profiles.Object, hub, firstSettings, new List<IDevice> { first.Object, second.Object });
            var equipment = RegressionEquipment(category, profiles.Object, chooser);
            var window = Show(equipment.View);
            ClearNotifications();
            try {
                PumpUntilCompleted(equipment.Connect());
                hub.IsSessionActive.Should().BeTrue();
                var originalConfiguration = chooser.HubConfiguration;
                currentProfile = secondProfile;
                profiles.Raise(service => service.ProfileChanged += null, new ProfileChangedEventArgs(profile, secondProfile));
                Drain();
                chooser.HubConfiguration.Should().BeSameAs(originalConfiguration);
                chooser.HubConfiguration.Rows.Single().Settings.Should().BeSameAs(firstSettings.Devices.Single());
                second.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Never);
                if (faultSession) {
                    first.SetupGet(device => device.Connected).Returns(false);
                    PollEquipment(equipment.Vm, equipment.PollMethod);
                    hub.Connected.Should().BeFalse();
                    hub.IsSessionActive.Should().BeTrue();
                }

                PumpUntilCompleted(Task.Run(equipment.Disconnect));
                Drain();
                chooser.HubConfiguration.Rows.Single().Settings.Should().BeSameAs(secondSettings.Devices.Single());
                chooser.HubConfiguration.Rows.Single().Chooser.SelectedDevice.Should().BeSameAs(second.Object);
                originalConfiguration.Rows.Single().Chooser.SelectedDevice = second.Object;
                firstSettings.Devices.Single().Id.Should().Be("first", "disposing the old configuration must detach its chooser callbacks");
                chooser.SelectedDevice.Name.Should().Be(Loc.Instance[category switch {
                    "Safety" => "LblSafetyMonitorHub",
                    "Switch" => "LblSwitchHub",
                    _ => "LblWeatherDataHub"
                }]);
                var configurationView = new HubConfigurationView { DataContext = chooser.HubConfiguration };
                window.Content = configurationView;
                Drain();
                var add = Descendants<Button>(configurationView).Single(button => ReferenceEquals(button.Command, chooser.HubConfiguration.AddDeviceCommand));
                add.Command.Execute(null);
                Drain();
                secondSettings.Devices.Should().HaveCount(2);
                firstSettings.Devices.Should().ContainSingle();
                chooser.HubConfiguration.RemoveDeviceCommand.Execute(chooser.HubConfiguration.Rows.Last());
                window.Content = equipment.View;
                PumpUntilCompleted(equipment.Connect());
                second.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Once);
                first.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Once);

                currentProfile = profile;
                profiles.Raise(service => service.ProfileChanged += null, new ProfileChangedEventArgs(secondProfile, profile));
                PumpUntilCompleted(Task.Run(async () => {
                    await equipment.Disconnect();
                    await equipment.Connect();
                }));
                chooser.HubConfiguration.Rows.Single().Settings.Should().BeSameAs(firstSettings.Devices.Single());
                first.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Exactly(2));
            } finally {
                PumpUntilCompleted(Task.Run(equipment.Disconnect));
                ClearNotifications();
                window.Close();
                chooser.HubConfiguration.Dispose();
            }
        }

        [TestCase("Safety")]
        [TestCase("Switch")]
        [TestCase("Weather")]
        public void ProfileSwitchDuringPendingConnection_RebindsAfterDisconnectAndCleansLateConnection(string category) {
            using var secondProfile = new NINA.Profile.Profile();
            profile.ApplicationSettings.DevicePollingInterval = 60;
            secondProfile.ApplicationSettings.DevicePollingInterval = 60;
            var first = RegressionDevice(category, "first");
            var second = RegressionDevice(category, "second");
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var finish = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            bool firstConnected = false;
            first.SetupGet(device => device.Connected).Returns(() => firstConnected);
            first.Setup(device => device.Disconnect()).Callback(() => firstConnected = false);
            first.Setup(device => device.Connect(It.IsAny<CancellationToken>())).Returns(async () => {
                started.TrySetResult(true);
                await finish.Task;
                firstConnected = true;
                return true;
            });
            var firstSettings = RegressionSettings(profile, category);
            var secondSettings = RegressionSettings(secondProfile, category);
            firstSettings.Devices.Add(new HubDeviceSettings { Id = "first", SourceNumber = 1 });
            secondSettings.Devices.Add(new HubDeviceSettings { Id = "second", SourceNumber = 1 });
            if (firstSettings is IWeatherHubSettings firstWeather && secondSettings is IWeatherHubSettings secondWeather) {
                firstWeather.SetSource(WeatherDataField.Temperature, "first");
                secondWeather.SetSource(WeatherDataField.Temperature, "second");
            }
            var hub = RegressionHub(category);
            SetRegressionDeviceId(profile, category, hub.Id);
            SetRegressionDeviceId(secondProfile, category, hub.Id);
            IProfile currentProfile = profile;
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(service => service.ActiveProfile).Returns(() => currentProfile);
            var chooser = new ViewChooser(profiles.Object, hub, firstSettings, new List<IDevice> { first.Object, second.Object });
            var equipment = RegressionEquipment(category, profiles.Object, chooser);
            var window = Show(equipment.View);
            ClearNotifications();
            var pending = equipment.Connect();
            try {
                PumpUntilCompleted(started.Task);
                currentProfile = secondProfile;
                profiles.Raise(service => service.ProfileChanged += null, new ProfileChangedEventArgs(profile, secondProfile));
                chooser.HubConfiguration.Rows.Single().Settings.Should().BeSameAs(firstSettings.Devices.Single());

                PumpUntilCompleted(Task.Run(hub.Disconnect));
                chooser.HubConfiguration.Rows.Single().Settings.Should().BeSameAs(secondSettings.Devices.Single());
                finish.SetResult(true);
                PumpUntilCompleted(pending);
                hub.IsSessionActive.Should().BeFalse();
                hub.DeviceStatuses.Should().OnlyContain(status => !status.Connected && !status.IsSafe && status.Error == string.Empty);
                first.Verify(device => device.Disconnect(), Times.Exactly(2));
                firstConnected.Should().BeFalse();
                VisibleNotifications().Should().BeEmpty();
                PumpUntilCompleted(equipment.Connect());
                second.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Once);
                first.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Once);
            } finally {
                finish.TrySetResult(true);
                PumpUntilCompleted(pending);
                PumpUntilCompleted(Task.Run(equipment.Disconnect));
                ClearNotifications();
                window.Close();
                chooser.HubConfiguration.Dispose();
            }
        }

        [TestCase("Safety")]
        [TestCase("Switch")]
        [TestCase("Weather")]
        public void ProfileSwitchDuringSession_RespectsDisabledEquipmentInNewProfile(string category) {
            using var secondProfile = new NINA.Profile.Profile();
            profile.ApplicationSettings.DevicePollingInterval = 60;
            var source = RegressionDevice(category, "source");
            var settings = RegressionSettings(profile, category);
            settings.Devices.Add(new HubDeviceSettings { Id = "source", SourceNumber = 1 });
            if (settings is IWeatherHubSettings weather) {
                weather.SetSource(WeatherDataField.Temperature, "source");
            }
            var hub = RegressionHub(category);
            SetRegressionDeviceId(profile, category, hub.Id);
            SetRegressionDeviceId(secondProfile, category, "No_Device");
            IProfile currentProfile = profile;
            var profiles = new Mock<IProfileService>();
            profiles.SetupGet(service => service.ActiveProfile).Returns(() => currentProfile);
            var chooser = new ViewChooser(profiles.Object, hub, settings, new List<IDevice> { source.Object });
            var equipment = RegressionEquipment(category, profiles.Object, chooser);
            var window = Show(equipment.View);
            try {
                PumpUntilCompleted(equipment.Connect());
                currentProfile = secondProfile;
                profiles.Raise(service => service.ProfileChanged += null, new ProfileChangedEventArgs(profile, secondProfile));
                PumpUntilCompleted(Task.Run(equipment.Disconnect));
                Drain();

                chooser.SelectedDevice.Id.Should().Be("No_Device");
                chooser.HubConfiguration.Rows.Should().BeEmpty();
                PumpUntilCompleted(equipment.Connect());
                hub.IsSessionActive.Should().BeFalse();
                source.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Once);
            } finally {
                PumpUntilCompleted(Task.Run(equipment.Disconnect));
                ClearNotifications();
                window.Close();
                chooser.HubConfiguration.Dispose();
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void BoundSwitchEditing_UsesCachedSessionStateAndWorkerWritesCheckConnections(bool booleanSwitch, bool disconnectBeforeEditing) {
            profile.ApplicationSettings.DevicePollingInterval = 60;
            var native = new Mock<IWritableSwitch>();
            native.SetupGet(channel => channel.Id).Returns(7);
            native.SetupGet(channel => channel.Name).Returns("Relay");
            native.SetupGet(channel => channel.Maximum).Returns(booleanSwitch ? 1 : 100);
            native.SetupGet(channel => channel.StepSize).Returns(booleanSwitch ? 1 : 0.25);
            native.SetupProperty(channel => channel.TargetValue, 0);
            double value = 0;
            native.SetupGet(channel => channel.Value).Returns(() => value);
            native.Setup(channel => channel.Poll()).Returns(true);
            native.Setup(channel => channel.SetValue()).Callback(() => {
                Application.Current.Dispatcher.CheckAccess().Should().BeFalse();
                value = native.Object.TargetValue;
            });
            var source = RegressionDevice("Switch", "source");
            source.As<ISwitchHub>().SetupGet(device => device.Switches).Returns(new[] { native.Object });
            var settings = new DeviceHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = "source", SourceNumber = 1 });
            var hub = new CompositeSwitchHub("Hub");
            var profiles = Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
            var chooser = new ViewChooser(profiles, hub, settings, new List<IDevice> { source.Object });
            var mediator = new NINA.WPF.Base.Mediator.SwitchMediator();
            var vm = new SwitchVM(profiles, Mock.Of<IApplicationStatusMediator>(), mediator, chooser);
            var view = new SwitchHubView { DataContext = vm };
            var window = Show(view);
            try {
#pragma warning disable CS0618 // Exercise the real equipment command and its worker-thread dispatch.
                PumpUntilCompleted(vm.ConnectCommand.ExecuteAsync(null));
#pragma warning restore CS0618
                Drain();
                var connected = !disconnectBeforeEditing;
                int uiReads = 0;
                int workerReads = 0;
                source.SetupGet(device => device.Connected).Returns(() => {
                    if (Application.Current.Dispatcher.CheckAccess()) {
                        Interlocked.Increment(ref uiReads);
                    } else {
                        Interlocked.Increment(ref workerReads);
                    }
                    return connected;
                });
                double requested = booleanSwitch ? 1 : 0.75;
                if (booleanSwitch) {
                    var toggle = Descendants<ToggleButton>(view).Single(control =>
                        BindingOperations.GetMultiBindingExpression(control, ToggleButton.IsCheckedProperty) != null);
                    toggle.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
                } else {
                    var stepper = Descendants<StepperControl>(view).Single();
                    stepper.SetCurrentValue(StepperControl.ValueProperty, requested);
                    stepper.GetBindingExpression(StepperControl.ValueProperty)!.UpdateSource();
                }
                Drain();
                uiReads.Should().Be(0, "editing a bound value must not query child hardware");
                native.Object.TargetValue.Should().Be(requested);
                native.Verify(channel => channel.SetValue(), Times.Never);
                if (disconnectBeforeEditing) {
                    Func<Task> write = () => Task.Run(() => mediator.SetSwitchValueByIdentity("source", 7, requested, null, CancellationToken.None));
                    var failure = write.Should().ThrowAsync<InvalidOperationException>();
                    PumpUntilCompleted(failure);
                    native.Verify(channel => channel.SetValue(), Times.Never);
                    hub.IsSessionActive.Should().BeTrue();
                } else {
#pragma warning disable CS0618 // Execute the same command used by the bound toggle and Set button.
                    PumpUntilCompleted(vm.SetSwitchValueCommand.ExecuteAsync(vm.WritableSwitches.Single()));
#pragma warning restore CS0618
                    native.Verify(channel => channel.SetValue(), Times.Once);
                    value.Should().Be(requested);
                }
                workerReads.Should().BeGreaterThan(0);
            } finally {
                PumpUntilCompleted(Task.Run(vm.Disconnect));
                ClearNotifications();
                window.Close();
                chooser.HubConfiguration.Dispose();
            }
        }

        [TestCase("Safety", "false")]
        [TestCase("Safety", "throw")]
        [TestCase("Switch", "false")]
        [TestCase("Switch", "throw")]
        [TestCase("Switch", "channels")]
        [TestCase("Weather", "false")]
        [TestCase("Weather", "throw")]
        [TestCase("Safety", "cancel")]
        [TestCase("Switch", "cancel")]
        [TestCase("Weather", "cancel")]
        public void EquipmentConnectCommand_RejectedSessionShowsFailureButCancellationDoesNot(string category, string failure) {
            profile.ApplicationSettings.DevicePollingInterval = 60;
            var first = RegressionDevice(category, "first");
            var second = RegressionDevice(category, "second");
            var expected = failure == "throw" ? "Sensor connection failed" : Loc.Instance["LblEquipmentHubConnectionFailed"];
            if (failure == "throw") {
                second.Setup(device => device.Connect(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException(expected));
            } else if (failure == "cancel") {
                second.Setup(device => device.Connect(It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
            } else if (failure == "false") {
                second.Setup(device => device.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            } else {
                var channel = Mock.Of<ISwitch>(switchChannel => switchChannel.Id == 7);
                second.As<ISwitchHub>().SetupGet(device => device.Switches).Returns(new[] { channel, channel });
                expected = Loc.Instance["LblEquipmentHubInvalidConfiguration"];
            }
            if (category == "Weather" && failure != "cancel") {
                first.Setup(device => device.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            }
            var settings = category == "Weather" ? new WeatherHubSettings() : new DeviceHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = "first", SourceNumber = 1 });
            settings.Devices.Add(new HubDeviceSettings { Id = "second", SourceNumber = 2 });
            if (settings is IWeatherHubSettings weather) {
                weather.SetSource(WeatherDataField.Temperature, "first");
                weather.SetSource(WeatherDataField.WindSpeed, "second");
            }
            var hub = RegressionHub(category);
            var profiles = Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
            var chooser = new ViewChooser(profiles, hub, settings, new List<IDevice> { first.Object, second.Object });
            var equipment = RegressionEquipment(category, profiles, chooser);
            var window = Show(equipment.View);
            ClearNotifications();
            try {
                PumpUntilCompleted(equipment.Connect());
                Drain();
                if (failure == "cancel") {
                    VisibleNotifications().Should().BeEmpty();
                } else {
                    var notification = VisibleNotifications().Should().ContainSingle().Subject;
                    notification.Header.Should().Be(Loc.Instance["LblError"]);
                    notification.Message.Should().Contain(hub.Name).And.Contain("second").And.Contain(expected);
                }
                hub.IsSessionActive.Should().BeFalse();
                hub.DeviceStatuses.Should().OnlyContain(status => !status.Connected && !status.IsSafe && status.Error == string.Empty);
                first.Verify(device => device.Disconnect(), Times.Once);
                second.Verify(device => device.Disconnect(), Times.Once);
            } finally {
                ClearNotifications();
                PumpUntilCompleted(Task.Run(equipment.Disconnect));
                window.Close();
                chooser.HubConfiguration.Dispose();
            }
        }

        private static IEquipmentHub RegressionHub(string category) => category switch {
            "Safety" => new SafetyMonitorHub("Hub"),
            "Switch" => new CompositeSwitchHub("Hub"),
            _ => new WeatherDataHub("Hub")
        };

        private static IDeviceHubSettings RegressionSettings(IProfile profile, string category) => category switch {
            "Safety" => ((IHasDeviceHubSettings)profile.SafetyMonitorSettings).HubSettings,
            "Switch" => ((IHasDeviceHubSettings)profile.SwitchSettings).HubSettings,
            _ => ((IHasDeviceHubSettings)profile.WeatherDataSettings).HubSettings
        };

        private static void SetRegressionDeviceId(IProfile profile, string category, string id) {
            if (category == "Safety") {
                profile.SafetyMonitorSettings.Id = id;
            } else if (category == "Switch") {
                profile.SwitchSettings.Id = id;
            } else {
                profile.WeatherDataSettings.Id = id;
            }
        }

        private static Mock<IDevice> RegressionDevice(string category, string id) {
            var device = category switch {
                "Safety" => new Mock<ISafetyMonitor>().As<IDevice>(),
                "Switch" => new Mock<ISwitchHub>().As<IDevice>(),
                _ => new Mock<IWeatherData>().As<IDevice>()
            };
            if (category == "Switch") {
                device.As<ISwitchHub>().SetupGet(source => source.Switches).Returns(Array.Empty<ISwitch>());
            } else if (category == "Safety") {
                device.As<ISafetyMonitor>().SetupGet(source => source.IsSafe).Returns(true);
            }
            device.SetupGet(source => source.Id).Returns(id);
            device.SetupGet(source => source.Name).Returns(id);
            device.SetupGet(source => source.DisplayName).Returns(id);
            bool connected = false;
            device.SetupGet(source => source.Connected).Returns(() => connected);
            device.Setup(source => source.Connect(It.IsAny<CancellationToken>())).Callback(() => connected = true).ReturnsAsync(true);
            device.Setup(source => source.Disconnect()).Callback(() => connected = false);
            return device;
        }

        private static (object Vm, FrameworkElement View, Func<Task> Connect, Func<Task> Disconnect, string PollMethod) RegressionEquipment(
                string category, IProfileService profiles, ViewChooser chooser) {
            var status = Mock.Of<IApplicationStatusMediator>();
#pragma warning disable CS0618 // Use production equipment commands with their existing worker-thread dispatch.
            if (category == "Safety") {
                var vm = new SafetyMonitorVM(profiles, Mock.Of<ISafetyMonitorMediator>(), status, chooser);
                return (vm, new SafetyMonitorView { DataContext = vm }, () => vm.ConnectCommand.ExecuteAsync(null), vm.Disconnect, "GetMonitorValues");
            }
            if (category == "Switch") {
                var vm = new SwitchVM(profiles, status, Mock.Of<ISwitchMediator>(), chooser);
                return (vm, new SwitchHubView { DataContext = vm }, () => vm.ConnectCommand.ExecuteAsync(null), vm.Disconnect, "GetSwitchValues");
            }
            var weather = new WeatherDataVM(profiles, Mock.Of<IWeatherDataMediator>(), status, chooser);
            return (weather, new WeatherDataView { DataContext = weather }, () => weather.ConnectCommand.ExecuteAsync(null), weather.Disconnect, "GetWeatherDataValues");
#pragma warning restore CS0618
        }
    }
}
