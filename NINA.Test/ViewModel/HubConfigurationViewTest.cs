using FluentAssertions;
using Moq;
using NINA.Core.Locale;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.View.Equipment;
using NINA.WPF.Base.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.Test.ViewModel {

    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public partial class HubConfigurationViewTest {
        private ResourceDictionary previousResources = null!;
        private Window? previousWindow;
        private object? previousNotificationManager;
        private object? previousNotificationDispatcher;
        private IProfile? previousProfile;
        private NINA.Profile.ProfileService sharedProfiles = null!;
        private NINA.Profile.Profile profile = null!;

        [SetUp]
        public void SetUp() {
            var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            previousNotificationManager = typeof(Notification).GetField("manager", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
            previousNotificationDispatcher = typeof(Notification).GetField("dispatcher", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
            previousResources = app.Resources;
            previousWindow = app.MainWindow;
            app.Resources = new ResourceDictionary();
            profile = new NINA.Profile.Profile();
            profile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;
            var shared = new SharedResourceDictionary {
                Source = new Uri("/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml", UriKind.Relative)
            };
            sharedProfiles = (NINA.Profile.ProfileService)shared["ProfileService"];
            previousProfile = sharedProfiles.ActiveProfile;
            typeof(NINA.Profile.ProfileService).GetProperty(nameof(IProfileService.ActiveProfile))!.SetValue(sharedProfiles, profile);
            app.Resources.MergedDictionaries.Add(shared);
            foreach (var resource in new[] { "StaticResources/SVGDictionary", "StaticResources/Brushes", "StaticResources/Converters",
                    "Styles/Button", "Styles/TextBlock", "Styles/TextBox", "Styles/ComboBox", "Styles/CheckBox",
                    "Styles/ScrollViewer", "Styles/GroupBox", "Styles/CancellableButton", "Styles/StepperControl",
                    "Styles/ToggleButton", "Styles/RepeatButton", "Styles/Path", "Styles/Tooltip" }) {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary {
                    Source = new Uri($"/NINA.WPF.Base;component/Resources/{resource}.xaml", UriKind.Relative)
                });
            }
            app.Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("/NINA;component/Resources/Styles/Window.xaml", UriKind.Relative)
            });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("/NINA;component/Resources/StaticResources/DataTemplates.xaml", UriKind.Relative)
            });

            // Equipment-only tests can initialize notifications before a WPF application exists.
            typeof(Notification).GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
        }

        [TearDown]
        public void TearDown() {
            Notification.Dispose();
            Drain();
            typeof(Notification).GetField("manager", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, previousNotificationManager);
            typeof(Notification).GetField("dispatcher", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, previousNotificationDispatcher);
            Application.Current.MainWindow = previousWindow;
            typeof(NINA.Profile.ProfileService).GetProperty(nameof(IProfileService.ActiveProfile))!.SetValue(sharedProfiles, previousProfile);
            Application.Current.Resources = previousResources;
            profile.Dispose();
        }

        [Test]
        public async Task WeatherView_RealBindingsPersistSelectionsAndLockConnectedRows() {
            var source = new Mock<IWeatherData>();
            source.SetupGet(device => device.Id).Returns("weather-source");
            source.SetupGet(device => device.Name).Returns("Weather source");
            source.SetupGet(device => device.DisplayName).Returns("Weather source");
            source.SetupGet(device => device.Category).Returns("ASCOM");
            source.Setup(device => device.Connect(It.IsAny<CancellationToken>())).Callback(() =>
                source.SetupGet(device => device.Connected).Returns(true)).ReturnsAsync(true);
            var settings = new WeatherHubSettings();
            using var config = new HubConfigurationVM(Mock.Of<IProfileService>(), new WeatherDataHub("NINA Weather Hub"), settings,
                new List<IDevice> { source.Object });
            var view = new HubConfigurationView { DataContext = config };
            var window = Show(view);
            try {
                var add = Descendants<Button>(view).Single(button => ReferenceEquals(button.Command, config.AddDeviceCommand));
                add.Command.Execute(null);
                Drain();
                var connector = Descendants<Connector>(view).Single();
                connector.SetCurrentValue(Connector.SelectedDeviceProperty, source.Object);
                connector.GetBindingExpression(Connector.SelectedDeviceProperty)!.UpdateSource();
                Drain();
                settings.Devices.Single().Id.Should().Be("weather-source");
                connector.ShowConnectionControls.Should().BeFalse();
                var temperature = Descendants<ComboBox>(view).Single(combo =>
                    combo.DataContext is WeatherHubFieldVM field && field.Field == WeatherDataField.Temperature);
                temperature.SetCurrentValue(ComboBox.SelectedValueProperty, "weather-source");
                temperature.GetBindingExpression(ComboBox.SelectedValueProperty)!.UpdateSource();
                Drain();
                settings.Sources[WeatherDataField.Temperature].Should().Be("weather-source");

                (await config.Hub.Connect(CancellationToken.None)).Should().BeTrue();
                Drain();
                Descendants<ComboBox>(connector).Single().IsEnabled.Should().BeFalse();
                temperature.IsEnabled.Should().BeFalse();
                add.IsEnabled.Should().BeFalse();
                config.Hub.Disconnect();
                Drain();
                temperature.IsEnabled.Should().BeTrue();

                var remove = Descendants<Button>(view).Single(button => ReferenceEquals(button.Command, config.RemoveDeviceCommand));
                remove.Command.Execute(remove.CommandParameter);
                Drain();
                settings.Devices.Should().BeEmpty();
                settings.Sources.Should().NotContainKey(WeatherDataField.Temperature);
            } finally {
                config.Hub.Disconnect();
                window.Close();
            }
        }

        [Test]
        public void WeatherStatusView_SourceAssignmentsUpdateExistingRowsInBothDirections() {
            var settings = new WeatherHubSettings();
            var devices = new List<IDevice>();
            foreach (var id in new[] { "first", "second" }) {
                var device = new Mock<IWeatherData>();
                device.SetupGet(source => source.Id).Returns(id);
                device.SetupGet(source => source.DisplayName).Returns(id);
                device.Setup(source => source.Connect(It.IsAny<CancellationToken>())).Callback(() =>
                    device.SetupGet(source => source.Connected).Returns(true)).ReturnsAsync(true);
                device.Setup(source => source.Disconnect()).Callback(() => device.SetupGet(source => source.Connected).Returns(false));
                settings.Devices.Add(new HubDeviceSettings { Id = id, SourceNumber = devices.Count + 1 });
                devices.Add(device.Object);
            }

            settings.SetSource(WeatherDataField.Temperature, "first");
            using var config = new HubConfigurationVM(Mock.Of<IProfileService>(), new WeatherDataHub("Weather"), settings, devices);
            var view = new HubDeviceStatusView { DataContext = config };
            var window = Show(view);
            try {
                PumpUntilCompleted(Task.Run(() => config.Hub.Connect(CancellationToken.None)));
                Drain();
                VisibleLabels(view).Should().Contain(Loc.Instance["LblEquipmentHubNotUsed"]);
                PumpUntilCompleted(Task.Run(config.Hub.Disconnect));
                Drain();
                var wind = config.Fields.Single(field => field.Field == WeatherDataField.WindSpeed);
                wind.SourceId = "second";
                PumpUntilCompleted(Task.Run(() => config.Hub.Connect(CancellationToken.None)));
                Drain();
                config.Hub.DeviceStatuses.Should().OnlyContain(status => status.Connected);
                VisibleLabels(view).Should().NotContain(Loc.Instance["LblEquipmentHubNotUsed"]);
                Descendants<CheckBox>(view).Count(check => check.IsVisible && check.IsChecked == true).Should().Be(2);
                PumpUntilCompleted(Task.Run(config.Hub.Disconnect));
                Drain();
                wind.SourceId = string.Empty;
                PumpUntilCompleted(Task.Run(() => config.Hub.Connect(CancellationToken.None)));
                Drain();
                VisibleLabels(view).Should().Contain(Loc.Instance["LblEquipmentHubNotUsed"]);
                Descendants<CheckBox>(view).Count(check => check.IsVisible && check.IsChecked == true).Should().Be(1);
            } finally {
                config.Hub.Disconnect();
                window.Close();
            }
        }

        [TestCase("Safety")]
        [TestCase("Switch")]
        [TestCase("Weather")]
        public void EquipmentConnectCommand_WithBoundHubControls_ConnectsAndDisconnectsOnWorkerThread(string category) {
            IEquipmentHub hub = category switch {
                "Safety" => new SafetyMonitorHub("Hub"),
                "Switch" => new CompositeSwitchHub("Hub"),
                _ => new WeatherDataHub("Hub")
            };
            Mock<IDevice> source;
            bool safe = true;
            if (category == "Safety") {
                var safety = new Mock<ISafetyMonitor>();
                safety.SetupGet(device => device.IsSafe).Returns(() => {
                    Application.Current.Dispatcher.CheckAccess().Should().BeFalse("views must use the cached safety reading");
                    return safe;
                });
                source = safety.As<IDevice>();
            } else if (category == "Switch") {
                var gauge = new Mock<ISwitch>();
                gauge.SetupGet(channel => channel.Id).Returns(0);
                gauge.SetupGet(channel => channel.Name).Returns("Voltage / DC");
                gauge.SetupGet(channel => channel.Value).Returns(12);
                gauge.Setup(channel => channel.Poll()).Returns(true);
                var output = new Mock<IWritableSwitch>();
                output.SetupGet(channel => channel.Id).Returns(1);
                output.SetupGet(channel => channel.Name).Returns("Dew heater (main)");
                output.SetupGet(channel => channel.Maximum).Returns(1);
                output.SetupGet(channel => channel.StepSize).Returns(1);
                output.Setup(channel => channel.Poll()).Returns(true);
                var switches = new Mock<ISwitchHub>();
                switches.SetupGet(device => device.Switches).Returns(new List<ISwitch> { gauge.Object, output.Object });
                source = switches.As<IDevice>();
            } else {
                source = new Mock<IWeatherData>().As<IDevice>();
            }

            bool connected = false;
            var connectStarted = new TaskCompletionSource<bool>();
            var finishConnect = new TaskCompletionSource<bool>();
            source.SetupGet(device => device.Id).Returns("source");
            source.SetupGet(device => device.Name).Returns("Equipment source (roof)");
            source.SetupGet(device => device.DisplayName).Returns("Equipment source (roof)");
            source.SetupGet(device => device.Connected).Returns(() => connected);
            source.Setup(device => device.Connect(It.IsAny<CancellationToken>()))
                .Returns(async () => {
                    connectStarted.TrySetResult(true);
                    await finishConnect.Task;
                    connected = true;
                    return true;
                });
            source.Setup(device => device.Disconnect()).Callback(() => connected = false);
            DeviceHubSettings settings = category == "Weather" ? new WeatherHubSettings() : new DeviceHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = "source", SourceNumber = 1 });
            if (settings is WeatherHubSettings weatherSettings) {
                weatherSettings.SetSource(WeatherDataField.Temperature, "source");
            }

            var devices = new List<IDevice> { source.Object };
            if (category == "Weather") {
                foreach (var id in new[] { "unused", "rejected" }) {
                    var weather = new Mock<IWeatherData>();
                    weather.SetupGet(device => device.Id).Returns(id);
                    weather.SetupGet(device => device.DisplayName).Returns(id == "unused" ? "Cloud sensor (unassigned)" : "Wind sensor (offline)");
                    weather.Setup(device => device.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(false);
                    if (id == "rejected") {
                        weather.SetupGet(device => device.Connected).Returns(true);
                        weather.Setup(device => device.Disconnect()).Throws(new InvalidOperationException("Wind sensor cleanup failed"));
                        ((IWeatherHubSettings)settings).SetSource(WeatherDataField.WindSpeed, id);
                    }

                    settings.Devices.Add(new HubDeviceSettings { Id = id, SourceNumber = devices.Count + 1 });
                    devices.Add(weather.Object);
                }
            }

            profile.ApplicationSettings.DevicePollingInterval = 60;
            var profiles = Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
            var chooser = new ViewChooser(profiles, hub, settings, devices);
            var status = Mock.Of<IApplicationStatusMediator>();
            var view = new HubConfigurationView { DataContext = chooser.HubConfiguration };
            FrameworkElement equipmentView = category switch {
                "Safety" => new SafetyMonitorView(),
                "Switch" => new SwitchHubView(),
                _ => new WeatherDataView()
            };
            equipmentView.DataContext = new EquipmentViewState(chooser);
            var content = new StackPanel();
            content.Children.Add(equipmentView);
            content.Children.Add(view);
            var window = Show(content);
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            try {
#pragma warning disable CS0618 // Exercise the production equipment commands, including their worker-thread dispatch.
                Func<Task> connect;
                object equipmentVm;
                string pollMethod;
                Func<Task> disconnect;
                Func<bool> isConnected;
                if (category == "Safety") {
                    var vm = new SafetyMonitorVM(profiles, Mock.Of<ISafetyMonitorMediator>(), status, chooser);
                    equipmentVm = vm;
                    pollMethod = "GetMonitorValues";
                    equipmentView.DataContext = vm;
                    connect = () => vm.ConnectCommand.ExecuteAsync(null);
                    disconnect = vm.Disconnect;
                    isConnected = () => vm.SafetyMonitorInfo.Connected;
                } else if (category == "Switch") {
                    var vm = new SwitchVM(profiles, status, Mock.Of<ISwitchMediator>(), chooser);
                    equipmentVm = vm;
                    pollMethod = "GetSwitchValues";
                    equipmentView.DataContext = vm;
                    connect = () => vm.ConnectCommand.ExecuteAsync(null);
                    disconnect = vm.Disconnect;
                    isConnected = () => vm.SwitchInfo.Connected;
                } else {
                    var vm = new WeatherDataVM(profiles, Mock.Of<IWeatherDataMediator>(), status, chooser);
                    equipmentVm = vm;
                    pollMethod = "GetWeatherDataValues";
                    equipmentView.DataContext = vm;
                    connect = () => vm.ConnectCommand.ExecuteAsync(null);
                    disconnect = vm.Disconnect;
                    isConnected = () => vm.WeatherDataInfo.Connected;
                }
#pragma warning restore CS0618

                var deviceList = Descendants<HubDeviceStatusView>(equipmentView).Single();
                deviceList.IsVisible.Should().BeFalse();
                var connection = connect();
                PumpUntilCompleted(connectStarted.Task);
                Drain();
                deviceList.IsVisible.Should().BeTrue("connecting is an active session");
                finishConnect.SetResult(true);
                PumpUntilCompleted(connection);
                PollEquipment(equipmentVm, pollMethod);
                Drain();
                isConnected().Should().BeTrue();
                VisibleLabels(deviceList).Should().Contain("1: Equipment source (roof)");
                VisibleLabels(deviceList).Should().Contain(Loc.Instance["LblConnected"]);
                if (category == "Safety") {
                    VisibleLabels(deviceList).Should().Contain(Loc.Instance["LblEquipmentHubSafe"]);
                    safe = false;
                    PollEquipment(equipmentVm, pollMethod);
                    Drain();
                    VisibleLabels(deviceList).Should().Contain(Loc.Instance["LblEquipmentHubUnsafe"]);
                    safe = true;
                    PollEquipment(equipmentVm, pollMethod);
                } else if (category == "Weather") {
                    VisibleLabels(deviceList).Should().Contain("2: Cloud sensor (unassigned)");
                    VisibleLabels(deviceList).Should().Contain(Loc.Instance["LblEquipmentHubNotUsed"]);
                    VisibleLabels(deviceList).Should().Contain("3: Wind sensor (offline)");
                    VisibleLabels(deviceList).Should().Contain("Wind sensor cleanup failed");
                    hub.DeviceStatuses.Select(status => status.Connected).Should().Equal(true, false, false);
                }

                SavePreview(equipmentView, $"Hub{category}StatusConnected");
                if (category == "Switch") {
                    var labels = Descendants<TextBlock>(equipmentView).Where(label => label.IsVisible).Select(label => label.Text);
                    labels.Should().Contain("Switch 1 - Voltage / DC");
                    labels.Should().Contain("Switch 1 - Dew heater (main)");
                    ((Border)deviceList.Content).BorderThickness.Top.Should().Be(0);
                    SavePreview(equipmentView, "HubSwitchReadableNames");
                }

                var add = Descendants<Button>(view).Single(button => ReferenceEquals(button.Command, chooser.HubConfiguration.AddDeviceCommand));
                add.IsEnabled.Should().BeFalse();
                var connector = Descendants<Connector>(equipmentView).Single();
                var gear = Descendants<Button>(connector).Single(button => ReferenceEquals(button.Command, chooser.SetupDialogCommand));
                gear.IsEnabled.Should().BeFalse();
                PumpUntilCompleted(chooser.SetupDialogCommand.ExecuteAsync(null));
                Application.Current.Windows.OfType<CustomWindow>().Should().BeEmpty();

                connected = false;
                PollEquipment(equipmentVm, pollMethod);
                Drain();
                hub.Connected.Should().BeFalse();
                deviceList.IsVisible.Should().BeTrue("faulted sessions remain visible until disconnect");
                VisibleLabels(deviceList).Should().Contain(Loc.Instance["LblEquipmentHubDisconnected"]);
                VisibleLabels(deviceList).Should().Contain(Loc.Instance["LblEquipmentHubDeviceDisconnected"]);
                if (category == "Safety") {
                    VisibleLabels(deviceList).Should().Contain(Loc.Instance["LblEquipmentHubUnsafe"]);
                }

                SavePreview(equipmentView, $"Hub{category}StatusFaulted");
                connected = true;
                PollEquipment(equipmentVm, pollMethod);
                Drain();
                hub.DeviceStatuses[0].Connected.Should().BeTrue();
                if (category == "Switch") {
                    hub.Connected.Should().BeFalse("switch faults remain latched even when a child recovers");
                } else {
                    hub.Connected.Should().BeTrue();
                    VisibleLabels(deviceList).Should().NotContain(Loc.Instance["LblEquipmentHubDeviceDisconnected"]);
                }

                PumpUntilCompleted(Task.Run(disconnect));
                Drain();
                isConnected().Should().BeFalse();
                deviceList.IsVisible.Should().BeFalse();
                hub.DeviceStatuses.Should().OnlyContain(status => !status.Connected && !status.IsSafe);
                chooser.HubConfiguration.IsEditable.Should().BeTrue();
                add.IsEnabled.Should().BeTrue();
                gear.IsEnabled.Should().BeTrue();
            } finally {
                SynchronizationContext.SetSynchronizationContext(previousContext);
                window.Close();
                chooser.HubConfiguration.Dispose();
                hub.Disconnect();
            }
        }

        [Test]
        public void SafetyStatusView_MixedStatesShareColumnPositions() {
            var hub = new SafetyMonitorHub("Hub");
            var settings = new DeviceHubSettings();
            var devices = new List<IDevice>();
            var connected = new bool[3];
            var safe = new[] { true, false, true };
            var names = new[] { "Roof safety monitor", "Cloud safety monitor (remote station)", "Shutter safety monitor" };
            foreach (var index in Enumerable.Range(0, names.Length)) {
                var device = new Mock<ISafetyMonitor>();
                device.SetupGet(source => source.Id).Returns($"sensor-{index}");
                device.SetupGet(source => source.DisplayName).Returns(names[index]);
                device.SetupGet(source => source.Connected).Returns(() => connected[index]);
                device.SetupGet(source => source.IsSafe).Returns(() => {
                    Application.Current.Dispatcher.CheckAccess().Should().BeFalse();
                    return safe[index];
                });
                device.Setup(source => source.Connect(It.IsAny<CancellationToken>())).Callback(() => connected[index] = true).ReturnsAsync(true);
                device.Setup(source => source.Disconnect()).Callback(() => connected[index] = false);
                settings.Devices.Add(new HubDeviceSettings { Id = $"sensor-{index}", SourceNumber = index + 1 });
                devices.Add(device.Object);
            }

            profile.ApplicationSettings.DevicePollingInterval = 60;
            var profiles = Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
            var chooser = new ViewChooser(profiles, hub, settings, devices);
            var vm = new SafetyMonitorVM(profiles, Mock.Of<ISafetyMonitorMediator>(), Mock.Of<IApplicationStatusMediator>(), chooser);
            var view = new SafetyMonitorView { DataContext = vm };
            var window = Show(view);
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            try {
#pragma warning disable CS0618 // Exercise the production worker command with the compiled equipment view.
                PumpUntilCompleted(vm.ConnectCommand.ExecuteAsync(null));
#pragma warning restore CS0618
                PollEquipment(vm, "GetMonitorValues");
                var statuses = Descendants<HubDeviceStatusView>(view).Single();
                AssertStatusColumnPositions(statuses);
                SavePreview(view, "HubSafetyAlignedStates");
                connected[2] = false;
                PollEquipment(vm, "GetMonitorValues");
                AssertStatusColumnPositions(statuses);
                vm.SafetyMonitorInfo.IsSafe.Should().BeFalse();
                SavePreview(view, "HubSafetyAlignedFault");
                safe[0] = false;
                safe[1] = true;
                connected[2] = true;
                PollEquipment(vm, "GetMonitorValues");
                AssertStatusColumnPositions(statuses);
                hub.DeviceStatuses.Select(status => status.IsSafe).Should().Equal(false, true, true);
                ((Border)statuses.Content).BorderThickness.Top.Should().Be(1);
            } finally {
                PumpUntilCompleted(Task.Run(vm.Disconnect));
                SynchronizationContext.SetSynchronizationContext(previousContext);
                chooser.HubConfiguration.Dispose();
                window.Close();
            }
        }

        private static void AssertStatusColumnPositions(HubDeviceStatusView view) {
            foreach (var column in new[] { 1, 2 }) {
                var cells = Descendants<StackPanel>(view)
                    .Where(panel => panel.DataContext is HubDeviceRowVM && Grid.GetColumn(panel) == column).ToArray();
                cells.Should().HaveCount(3);
                var positions = cells.Select(cell => cell.TranslatePoint(new Point(0, 0), view).X).ToArray();
                (positions.Max() - positions.Min()).Should().BeLessThan(0.5,
                    "each device must place connection and safety status in the same columns");
            }
        }

        [TestCase("Safety", "empty", "LblEquipmentHubNoDevices")]
        [TestCase("Switch", "empty", "LblEquipmentHubNoDevices")]
        [TestCase("Weather", "empty", "LblEquipmentHubNoDevices")]
        [TestCase("Safety", "unselected", "LblEquipmentHubNoDevices")]
        [TestCase("Switch", "unselected", "LblEquipmentHubNoDevices")]
        [TestCase("Weather", "unselected", "LblEquipmentHubNoDevices")]
        [TestCase("Weather", "unmapped", "LblEquipmentHubNoSources")]
        [TestCase("Safety", "duplicate", "LblEquipmentHubInvalidConfiguration")]
        [TestCase("Switch", "duplicate", "LblEquipmentHubInvalidConfiguration")]
        [TestCase("Weather", "duplicate", "LblEquipmentHubInvalidConfiguration")]
        public async Task EquipmentConnectCommand_WithIncompleteHubConfiguration_ShowsActionableError(
                string category, string configuration, string messageKey) {
            IEquipmentHub hub = category switch {
                "Safety" => new SafetyMonitorHub("NINA Safety Hub"),
                "Switch" => new CompositeSwitchHub("NINA Switch Hub"),
                _ => new WeatherDataHub("NINA Weather Hub")
            };
            var source = category switch {
                "Safety" => new Mock<ISafetyMonitor>().As<IDevice>(),
                "Switch" => new Mock<ISwitchHub>().As<IDevice>(),
                _ => new Mock<IWeatherData>().As<IDevice>()
            };
            source.SetupGet(device => device.Id).Returns("source");
            source.SetupGet(device => device.DisplayName).Returns("Source");
            DeviceHubSettings settings = category == "Weather" ? new WeatherHubSettings() : new DeviceHubSettings();
            if (configuration is "unmapped" or "duplicate") {
                settings.Devices.Add(new HubDeviceSettings { Id = "source", SourceNumber = 1 });
            }
            if (configuration == "duplicate") {
                settings.Devices.Add(new HubDeviceSettings { Id = "source", SourceNumber = 2 });
                if (settings is WeatherHubSettings weatherSettings) {
                    weatherSettings.SetSource(WeatherDataField.Temperature, "source");
                }
            }

            var profiles = Mock.Of<IProfileService>(service => service.ActiveProfile == profile);
            var chooser = new ViewChooser(profiles, hub, settings, new List<IDevice> { source.Object });
            if (configuration == "unselected") {
                chooser.HubConfiguration.AddDeviceCommand.Execute(null);
            }
            var status = Mock.Of<IApplicationStatusMediator>();
            FrameworkElement view;
            Func<Task> connect;
#pragma warning disable CS0618 // Use the equipment command that dispatches connection work off the UI thread.
            if (category == "Safety") {
                var vm = new SafetyMonitorVM(profiles, Mock.Of<ISafetyMonitorMediator>(), status, chooser);
                view = new SafetyMonitorView { DataContext = vm };
                connect = () => vm.ConnectCommand.ExecuteAsync(null);
            } else if (category == "Switch") {
                var vm = new SwitchVM(profiles, status, Mock.Of<ISwitchMediator>(), chooser);
                view = new SwitchHubView { DataContext = vm };
                connect = () => vm.ConnectCommand.ExecuteAsync(null);
            } else {
                var vm = new WeatherDataVM(profiles, Mock.Of<IWeatherDataMediator>(), status, chooser);
                view = new WeatherDataView { DataContext = vm };
                connect = () => vm.ConnectCommand.ExecuteAsync(null);
            }
#pragma warning restore CS0618
            var window = Show(view);
            ClearNotifications();
            try {
                await connect();
                Drain();

                var notification = VisibleNotifications().Should().ContainSingle().Subject;
                notification.Header.Should().Be(Loc.Instance["LblError"]);
                notification.Message.Should().Be($"{hub.Name}: {Loc.Instance[messageKey]}");
                var host = Application.Current.Windows.OfType<NotificationHostWindow>().Single();
                Descendants<TextBlock>(host).Should().Contain(text => text.IsVisible && text.Text == notification.Message);
                hub.DeviceStatuses.Select(device => device.Error).Should().Equal(
                    Enumerable.Repeat(Loc.Instance[messageKey], settings.Devices.Count));
                if (configuration is "empty" or "unmapped") {
                    SavePreview(host, $"Hub{category}-{configuration}-Error");
                }

                hub.Connected.Should().BeFalse();
                hub.IsSessionActive.Should().BeFalse();
                chooser.HubConfiguration.IsEditable.Should().BeTrue();
                var connector = Descendants<Connector>(view).Single();
                Descendants<Button>(connector).Single(button => ReferenceEquals(button.Command, chooser.SetupDialogCommand))
                    .IsEnabled.Should().BeTrue();
                source.Verify(device => device.Connect(It.IsAny<CancellationToken>()), Times.Never);
            } finally {
                ClearNotifications();
                window.Close();
                hub.Disconnect();
                chooser.HubConfiguration.Dispose();
            }
        }

        private static List<CustomNotification> VisibleNotifications() {
            return Application.Current.Windows.OfType<NotificationHostWindow>()
                .SelectMany(host => Descendants<ItemsControl>(host))
                .SelectMany(control => control.Items.OfType<CustomNotification>()).Distinct().ToList();
        }

        private static void ClearNotifications() {
            VisibleNotifications().FirstOrDefault()?.CloseAllCommand.Execute(null);
            Drain();
        }

        [Test]
        public async Task ChildSetup_WithBoundHubControls_DisablesAndRestoresEditing() {
            var source = new Mock<ISafetyMonitor>();
            source.SetupGet(device => device.Id).Returns("source");
            source.SetupGet(device => device.HasSetupDialog).Returns(true);
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            source.Setup(device => device.SetupDialog()).Callback(() => {
                Thread.CurrentThread.GetApartmentState().Should().Be(ApartmentState.STA);
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            });
            var settings = new DeviceHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = "source", SourceNumber = 1 });
            using var config = new HubConfigurationVM(Mock.Of<IProfileService>(), new SafetyMonitorHub("Hub"), settings,
                new List<IDevice> { source.Object });
            var view = new HubConfigurationView { DataContext = config };
            var window = Show(view);
            try {
                var setup = config.Rows[0].Chooser.SetupDialogCommand.ExecuteAsync(null);
                (await Task.Run(() => entered.Wait(TimeSpan.FromSeconds(5)))).Should().BeTrue();
                Drain();
                config.IsSetupDialogOpen.Should().BeTrue();
                config.IsEditable.Should().BeFalse();
                var add = Descendants<Button>(view).Single(button => ReferenceEquals(button.Command, config.AddDeviceCommand));
                add.IsEnabled.Should().BeFalse();

                release.Set();
                await setup;
                Drain();

                config.IsSetupDialogOpen.Should().BeFalse();
                config.IsEditable.Should().BeTrue();
                add.IsEnabled.Should().BeTrue();
                source.Verify(device => device.SetupDialog(), Times.Once);
            } finally {
                release.Set();
                window.Close();
            }
        }

        [TestCase("Safety")]
        [TestCase("Weather")]
        public async Task HubDriverSettings_AreStackedAndTrackSelectionsWithoutDuplicatingHubSetup(string category) {
            Mock<IDevice> Device(string id, string name) {
                var device = category == "Safety"
                    ? new Mock<ISafetyMonitor>().As<IDevice>()
                    : new Mock<IWeatherData>().As<IDevice>();
                if (category == "Safety") {
                    device.As<IWeatherData>();
                } else {
                    device.As<ISafetyMonitor>();
                }
                device.As<IEditableHubSettings>().SetupProperty(settings => settings.Setting, name + " settings");
                device.SetupGet(source => source.Id).Returns(id);
                device.SetupGet(source => source.Name).Returns(name);
                device.SetupGet(source => source.DisplayName).Returns(name);
                bool connected = false;
                device.SetupGet(source => source.Connected).Returns(() => connected);
                device.Setup(source => source.Connect(It.IsAny<CancellationToken>()))
                    .Callback(() => connected = true).ReturnsAsync(true);
                device.Setup(source => source.Disconnect()).Callback(() => connected = false);
                return device;
            }

            var first = Device("first", "First driver");
            var second = Device("second", "Second driver");
            var third = Device("third", "Third driver");
            IEquipmentHub hub = category == "Safety" ? new SafetyMonitorHub("Hub") : new WeatherDataHub("Hub");
            DeviceHubSettings settings = category == "Safety" ? new DeviceHubSettings() : new WeatherHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = "first", SourceNumber = 1 });
            settings.Devices.Add(new HubDeviceSettings { Id = "second", SourceNumber = 2 });
            if (settings is WeatherHubSettings weather) {
                weather.SetSource(WeatherDataField.Temperature, "first");
            }
            var chooser = new ViewChooser(Mock.Of<IProfileService>(), hub, settings,
                new List<IDevice> { first.Object, second.Object, third.Object });
            var postfix = category == "Safety" ? DataTemplatePostfix.SafetyMonitorSettings : DataTemplatePostfix.WeatherDataSettings;
            Application.Current.Resources[first.Object.GetType().FullName + postfix] = (DataTemplate)XamlReader.Parse(
                "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                "<StackPanel><TextBlock Text=\"{Binding Name}\" />" +
                "<TextBox Tag=\"ChildDriverSetting\" Text=\"{Binding Setting, Mode=TwoWay}\" /></StackPanel></DataTemplate>");
            var otherPostfix = category == "Safety" ? DataTemplatePostfix.WeatherDataSettings : DataTemplatePostfix.SafetyMonitorSettings;
            Application.Current.Resources[first.Object.GetType().FullName + otherPostfix] = (DataTemplate)XamlReader.Parse(
                "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"Wrong category settings\" /></DataTemplate>");
            FrameworkElement view = category == "Safety" ? new SafetyMonitorView() : new WeatherDataView();
            view.DataContext = new EquipmentViewState(chooser, first.Object);
            var window = Show(view);
            List<TextBox> Editors(DependencyObject parent) => Descendants<TextBox>(parent)
                .Where(editor => editor.IsVisible && Equals(editor.Tag, "ChildDriverSetting")).ToList();
            try {
                var settingsGroup = Descendants<GroupBox>(view).Single(group =>
                    group.Header is Grid header && Descendants<TextBlock>(header).Any(label => label.Text == Loc.Instance["LblSettings"]));
                settingsGroup.IsVisible.Should().BeTrue();
                Descendants<UniformGrid>(view).First().Columns.Should().Be(2);
                var editors = Editors(view);
                editors.Select(editor => editor.Text).Should().Equal("First driver settings", "Second driver settings");
                Descendants<ContentControl>(settingsGroup).Single(control => control.IsVisible && ReferenceEquals(control.Content, first.Object))
                    .DataContext.Should().BeSameAs(first.Object);
                editors[1].TransformToAncestor(view).Transform(new Point()).Y.Should()
                    .BeGreaterThan(editors[0].TransformToAncestor(view).Transform(new Point()).Y);
                editors[0].SetCurrentValue(TextBox.TextProperty, "Changed first settings");
                editors[0].GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
                first.As<IEditableHubSettings>().Object.Setting.Should().Be("Changed first settings");
                first.Raise(device => device.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IEditableHubSettings.Setting)));
                second.As<IEditableHubSettings>().Object.Setting.Should().Be("Second driver settings");

                chooser.HubConfiguration.AddDeviceCommand.Execute(null);
                chooser.HubConfiguration.Rows.Last().Chooser.SelectedDevice = third.Object;
                Drain();
                Editors(view).Should().HaveCount(3);
                chooser.HubConfiguration.RemoveDeviceCommand.Execute(chooser.HubConfiguration.Rows[1]);
                Drain();
                Editors(view).Select(editor => editor.Text).Should().Equal("Changed first settings", "Third driver settings");
                chooser.HubConfiguration.Rows[1].Chooser.SelectedDevice = second.Object;
                Drain();
                Editors(view).Select(editor => editor.Text).Should().Equal("Changed first settings", "Second driver settings");

                (await Task.Run(() => hub.Connect(CancellationToken.None))).Should().BeTrue();
                Drain();
                Editors(view).Should().OnlyContain(editor => editor.IsEnabled, "driver settings retain their own connection rules");
                hub.Disconnect();
                Drain();
                SavePreview(view, $"Hub{category}StackedSettings");

                var popupView = new HubConfigurationView { DataContext = chooser.HubConfiguration };
                window.Content = popupView;
                Drain();
                Editors(popupView).Should().BeEmpty("the popup configures hub membership and sources, while driver settings stay on the equipment page");
                window.Content = view;
                chooser.SelectedDevice = first.Object;
                Drain();
                Editors(view).Select(editor => editor.Text).Should().Equal("Changed first settings");
                chooser.SelectedDevice = hub;
                Drain();
                Editors(view).Should().HaveCount(2);
            } finally {
                hub.Disconnect();
                window.Close();
                chooser.HubConfiguration.Dispose();
            }
        }

        public interface IEditableHubSettings {
            string Setting { get; set; }
        }

        [TestCase("Weather")]
        [TestCase("Safety")]
        [TestCase("Switch")]
        public void EquipmentGear_OpensHubPopupWithoutInlineConfiguration(string category) {
            IEquipmentHub hub = category switch {
                "Safety" => new SafetyMonitorHub("NINA Safety Hub"),
                "Switch" => new CompositeSwitchHub("NINA Switch Hub"),
                _ => new WeatherDataHub("NINA Weather Hub")
            };
            DeviceHubSettings settings = category == "Weather" ? new WeatherHubSettings() : new DeviceHubSettings();
            var first = Mock.Of<IDevice>(device => device.Id == "source-one" && device.Name == "Test source one"
                && device.DisplayName == "Test source one" && device.Category == "ASCOM" && device.HasSetupDialog);
            var second = Mock.Of<IDevice>(device => device.Id == "source-two" && device.Name == "Test source two"
                && device.DisplayName == "Test source two" && device.Category == "Alpaca");
            Mock.Get(first).Setup(device => device.SetupDialog()).Callback(() =>
                Thread.CurrentThread.GetApartmentState().Should().Be(ApartmentState.STA));
            settings.Devices.Add(new HubDeviceSettings { Id = first.Id, SourceNumber = 1 });
            settings.Devices.Add(new HubDeviceSettings { Id = second.Id, SourceNumber = 2 });
            var chooser = new ViewChooser(Mock.Of<IProfileService>(), hub, settings, new List<IDevice> { first, second });
            FrameworkElement view = category switch {
                "Safety" => new SafetyMonitorView(),
                "Switch" => new SwitchHubView(),
                _ => new WeatherDataView()
            };
            view.DataContext = new EquipmentViewState(chooser);
            var window = Show(view);
            try {
                Descendants<HubConfigurationView>(view).Should().BeEmpty();
                var connector = Descendants<Connector>(view).Single();
                connector.HasSetupDialog.Should().BeTrue();
                var gear = Descendants<Button>(connector).Single(button => ReferenceEquals(button.Command, chooser.SetupDialogCommand));
                gear.IsEnabled.Should().BeTrue();
                if (category != "Switch") {
                    Descendants<UniformGrid>(view).First().Columns.Should().Be(2);
                }

                var inspected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var previousContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
                _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                    CustomWindow? popup = null;
                    try {
                        popup = Application.Current.Windows.OfType<CustomWindow>()
                            .Single(candidate => ReferenceEquals(candidate.Content, chooser.HubConfiguration));
                        popup.Owner.Should().BeSameAs(window);
                        popup.Style.Should().BeSameAs(Application.Current.FindResource("NoResizeWindow"));
                        popup.UpdateLayout();
                        var configView = Descendants<HubConfigurationView>(popup).Single();
                        configView.DataContext.Should().BeSameAs(chooser.HubConfiguration);
                        chooser.SetupDialogOpen.Should().BeTrue();
                        foreach (var combo in Descendants<ComboBox>(configView)) {
                            combo.ActualWidth.Should().BeGreaterThan(30);
                            var toggle = Descendants<ToggleButton>(combo).Single(button => button.Name == "ToggleButton");
                            toggle.Template.Should().BeSameAs(Application.Current.FindResource("ComboBoxToggleButton"));
                        }

                        chooser.HubConfiguration.AddDeviceCommand.Execute(null);
                        settings.Devices.Should().HaveCount(3);
                        chooser.HubConfiguration.RemoveDeviceCommand.Execute(chooser.HubConfiguration.Rows.Last());
                        settings.Devices.Should().HaveCount(2);
                        if (settings is WeatherHubSettings weather) {
                            var temperature = Descendants<ComboBox>(configView).Single(combo =>
                                combo.DataContext is WeatherHubFieldVM field && field.Field == WeatherDataField.Temperature);
                            temperature.SetCurrentValue(ComboBox.SelectedValueProperty, first.Id);
                            temperature.GetBindingExpression(ComboBox.SelectedValueProperty)!.UpdateSource();
                            weather.Sources[WeatherDataField.Temperature].Should().Be(first.Id);
                        }

                        PumpUntilCompleted(chooser.HubConfiguration.Rows[0].Chooser.SetupDialogCommand.ExecuteAsync(null));
                        chooser.SetupDialogOpen.Should().BeTrue("the parent hub popup is still open after child setup closes");
                        Mock.Get(first).Verify(device => device.SetupDialog(), Times.Once);
                        SavePreview(popup, $"Hub{category}Popup");
                        inspected.SetResult();
                    } catch (Exception error) {
                        inspected.SetException(error);
                    } finally {
                        popup?.CloseCommand.Execute(null);
                    }
                }));
                try {
                    PumpUntilCompleted(chooser.SetupDialogCommand.ExecuteAsync(null));
                    PumpUntilCompleted(inspected.Task);
                } finally {
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
                Drain();
                chooser.SetupDialogOpen.Should().BeFalse();
                gear.IsEnabled.Should().BeTrue();
                settings.Devices.Should().HaveCount(2);
                SavePreview(view, $"Hub{category}");

                var reopened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
                _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                    CustomWindow? popup = null;
                    try {
                        popup = Application.Current.Windows.OfType<CustomWindow>()
                            .Single(candidate => ReferenceEquals(candidate.Content, chooser.HubConfiguration));
                        var configView = Descendants<HubConfigurationView>(popup).Single();
                        Descendants<Connector>(configView).Select(child => child.SelectedDevice.Id)
                            .Should().Equal(first.Id, second.Id);
                        if (settings is WeatherHubSettings weather) {
                            weather.Sources[WeatherDataField.Temperature].Should().Be(first.Id);
                        }

                        var combo = Descendants<ComboBox>(configView).First();
                        var escape = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(combo), 0, Key.Escape) {
                            RoutedEvent = Keyboard.KeyDownEvent
                        };
                        combo.RaiseEvent(escape);
                        escape.Handled.Should().BeTrue();
                        popup.IsVisible.Should().BeFalse();
                        reopened.SetResult();
                    } catch (Exception error) {
                        reopened.SetException(error);
                    } finally {
                        popup?.CloseCommand.Execute(null);
                    }
                }));
                try {
                    PumpUntilCompleted(chooser.SetupDialogCommand.ExecuteAsync(null));
                    PumpUntilCompleted(reopened.Task);
                } finally {
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
                Drain();
                chooser.SetupDialogOpen.Should().BeFalse();
                gear.IsEnabled.Should().BeTrue();

                chooser.SelectedDevice = first;
                Drain();
                if (category != "Switch") {
                    Descendants<UniformGrid>(view).First().Columns.Should().Be(2);
                }
            } finally {
                foreach (var popup in Application.Current.Windows.OfType<CustomWindow>().ToArray()) {
                    popup.Close();
                }
                window.Close();
                chooser.HubConfiguration.Dispose();
            }
        }

        [Test]
        public void HubPopup_ClosingDuringChildSetup_KeepsEquipmentLockedUntilChildCloses() {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var source = Mock.Of<IDevice>(device => device.Id == "source" && device.HasSetupDialog);
            Mock.Get(source).Setup(device => device.SetupDialog()).Callback(() => {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
            });
            var settings = new DeviceHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = source.Id, SourceNumber = 1 });
            var chooser = new ViewChooser(Mock.Of<IProfileService>(), new SafetyMonitorHub("Hub"), settings,
                new List<IDevice> { source });
            var view = new SafetyMonitorView { DataContext = new EquipmentViewState(chooser) };
            var window = Show(view);
            var childSetup = Task.CompletedTask;
            var inspected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            try {
                _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                    CustomWindow? popup = null;
                    try {
                        popup = Application.Current.Windows.OfType<CustomWindow>().Single();
                        childSetup = chooser.HubConfiguration.Rows[0].Chooser.SetupDialogCommand.ExecuteAsync(null);
                        entered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                        inspected.SetResult();
                    } catch (Exception error) {
                        inspected.SetException(error);
                    } finally {
                        popup?.CloseCommand.Execute(null);
                    }
                }));
                PumpUntilCompleted(chooser.SetupDialogCommand.ExecuteAsync(null));
                PumpUntilCompleted(inspected.Task);
                Drain();
                chooser.SetupDialogOpen.Should().BeTrue("the native child setup is still open");
                var connector = Descendants<Connector>(view).Single();
                var gear = Descendants<Button>(connector).Single(button => ReferenceEquals(button.Command, chooser.SetupDialogCommand));
                gear.IsEnabled.Should().BeFalse();

                release.Set();
                PumpUntilCompleted(childSetup);
                Drain();
                chooser.SetupDialogOpen.Should().BeFalse();
                gear.IsEnabled.Should().BeTrue();
            } finally {
                release.Set();
                PumpUntilCompleted(childSetup);
                SynchronizationContext.SetSynchronizationContext(previousContext);
                window.Close();
                chooser.HubConfiguration.Dispose();
            }
        }

        private static IEnumerable<string> VisibleLabels(FrameworkElement view) {
            return Descendants<TextBlock>(view).Where(label => label.IsVisible).Select(label => label.Text);
        }

        private static void PollEquipment(object vm, string method) {
            var poll = vm.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var values = Task.Run(() => poll.Invoke(vm, null));
            PumpUntilCompleted(values);
            var update = vm.GetType().GetMethod(method.Replace("Get", "Update"),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            update.Invoke(vm, new[] { values.GetAwaiter().GetResult() });
            Drain();
        }

        private static void PumpUntilCompleted(Task task) {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            timer.Tick += (_, _) => {
                foreach (var popup in Application.Current.Windows.OfType<CustomWindow>().ToArray()) {
                    popup.Close();
                }
                frame.Continue = false;
            };
            _ = task.ContinueWith(_ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)), TaskScheduler.Default);
            timer.Start();
            try {
                if (!task.IsCompleted) {
                    Dispatcher.PushFrame(frame);
                }
                task.IsCompleted.Should().BeTrue("the popup operation must finish without blocking its UI dispatcher");
                task.GetAwaiter().GetResult();
            } finally {
                timer.Stop();
            }
        }

        private static void SavePreview(FrameworkElement view, string name) {
            if (!TestContext.Parameters.Get("SaveTestPreviews", false)) {
                return;
            }

            foreach (int dpi in new[] { 96, 144 }) {
                var scale = dpi / 96.0;
                var bitmap = new RenderTargetBitmap((int)(view.ActualWidth * scale), (int)(view.ActualHeight * scale), dpi, dpi, PixelFormats.Pbgra32);
                var drawing = new DrawingVisual();
                using (var context = drawing.RenderOpen()) {
                    var bounds = new Rect(0, 0, view.ActualWidth, view.ActualHeight);
                    context.DrawRectangle((Brush)Application.Current.FindResource("BackgroundBrush"), null, bounds);
                    context.DrawRectangle(new VisualBrush(view) {
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Viewbox = bounds
                    }, null, bounds);
                }

                bitmap.Render(drawing);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{name}-{dpi}.png");
                using (var stream = File.Create(path)) {
                    encoder.Save(stream);
                }

                TestContext.AddTestAttachment(path);
            }
        }

        [Test]
        public async Task Connector_FaultedSessionOffersDisconnectAndConnectingSessionKeepsCancel() {
            var native = new Mock<ISwitch>();
            native.SetupGet(channel => channel.Id).Returns(1);
            native.Setup(channel => channel.Poll()).Returns(false);
            var source = new Mock<ISwitchHub>();
            source.SetupGet(device => device.Id).Returns("switch-device");
            source.SetupGet(device => device.Connected).Returns(true);
            source.SetupGet(device => device.Switches).Returns(new List<ISwitch> { native.Object });
            var hub = new CompositeSwitchHub("Hub");
            hub.Configure(new[] { new NINA.Equipment.Equipment.Hubs.HubDevice(1, source.Object) });
            var completion = new TaskCompletionSource<bool>();
            source.Setup(device => device.Connect(It.IsAny<CancellationToken>())).Returns(completion.Task);
#pragma warning disable CS0618 // Exercise the connection command used by the existing equipment VMs.
            var connect = new NINA.Core.Utility.AsyncCommand<bool>(() => hub.Connect(CancellationToken.None));
#pragma warning restore CS0618
            var disconnect = new CommunityToolkit.Mvvm.Input.RelayCommand(hub.Disconnect);
            var connector = new Connector { ConnectCommand = connect, DisconnectCommand = disconnect };
            connector.SetBinding(Connector.ConnectedProperty, new Binding(nameof(hub.Connected)) { Source = hub });
            connector.SetBinding(Connector.SessionActiveProperty, new Binding(nameof(hub.IsSessionActive)) { Source = hub });
            var window = Show(connector);
            try {
                var connecting = connect.ExecuteAsync(null);
                Drain();
                connector.IsConnecting.Should().BeTrue();
                connector.ShowDisconnect.Should().BeFalse();
                completion.SetResult(true);
                await connecting;
                hub.Switches.Single().Poll().Should().BeFalse();
                Drain();
                hub.Connected.Should().BeFalse();
                hub.IsSessionActive.Should().BeTrue();
                connector.ShowDisconnect.Should().BeTrue();
                var button = Descendants<Button>(connector).Single(button => ReferenceEquals(button.Command, disconnect));
                button.IsVisible.Should().BeTrue();
                button.IsEnabled.Should().BeTrue();
                button.Command.Execute(null);
                Drain();
                hub.IsSessionActive.Should().BeFalse();
                connector.ShowDisconnect.Should().BeFalse();
            } finally {
                completion.TrySetResult(true);
                hub.Disconnect();
                window.Close();
            }
        }

        private static Window Show(FrameworkElement view) {
            var window = new Window { Width = 1400, Height = 900, Left = -32000, Top = -32000,
                ShowActivated = false, ShowInTaskbar = false, Content = view,
                Background = (Brush)Application.Current.FindResource("BackgroundBrush") };
            Application.Current.MainWindow = window;
            window.Show();
            Drain();
            return window;
        }

        private static void Drain() {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject {
            if (parent is T match) {
                yield return match;
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(parent, i))) {
                    yield return child;
                }
            }
        }

        private sealed class ViewChooser : DeviceChooserVM<IDevice> {

            internal ViewChooser(IProfileService profiles, IEquipmentHub hub, IDeviceHubSettings settings,
                    IList<IDevice> devices) : base(profiles, null) {
                AddHub(devices, hub, new SettingsOwner(settings));
                Devices = devices;
                SelectedDevice = hub;
            }

            public override Task GetEquipment() => Task.CompletedTask;
        }

        private sealed record SettingsOwner(IDeviceHubSettings HubSettings) : IHasDeviceHubSettings;

        private sealed record EquipmentViewState(ViewChooser DeviceChooserVM, IDevice? Device = null) {
            public NINA.Equipment.Equipment.MyWeatherData.WeatherDataInfo WeatherDataInfo { get; } = new();
            public NINA.Equipment.Equipment.MySafetyMonitor.SafetyMonitorInfo SafetyMonitorInfo { get; } = new();
            public NINA.Equipment.Equipment.MySwitch.SwitchInfo SwitchInfo { get; } = new();
            public IWeatherData? WeatherData => Device as IWeatherData;
            public ISafetyMonitor? SafetyMonitor => Device as ISafetyMonitor;
            public ISwitchHub? SwitchHub => Device as ISwitchHub;
            public object? ConnectCommand => null;
            public object? CancelConnectCommand => null;
            public object? DisconnectCommand => null;
            public object? RescanDevicesCommand => null;
        }
    }
}
