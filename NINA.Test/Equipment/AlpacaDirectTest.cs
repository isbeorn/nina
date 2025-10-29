using ASCOM.Common.Alpaca;
using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;
using NINA.Image.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Test.Equipment {
    [TestFixture]
    public class AlpacaDirectTest {
        private static Mock<IProfileService> CreateProfileService() {
            var service = new Mock<IProfileService>();
            service.SetupGet(x => x.ActiveProfile.PluginSettings).Returns(new PluginSettings());
            service.SetupGet(x => x.ActiveProfile.AlpacaSettings).Returns(new AlpacaSettings());
            return service;
        }

        private static IDevice[] CreateDevices(IProfileService service) {
            return typeof(AlpacaInteraction).Assembly.GetTypes()
                .Where(t => t.Name.StartsWith("AlpacaDirect") && typeof(IDevice).IsAssignableFrom(t))
                .OrderBy(t => t.FullName)
                .Select(t => (IDevice)Activator.CreateInstance(t, t.Name == "AlpacaDirectCamera"
                    ? new object[] { service, Mock.Of<IExposureDataFactory>() }
                    : new object[] { service })!)
                .ToArray();
        }

        private static AlpacaDirectSettings Settings(IProfileService service, IDevice device) {
            return new AlpacaDirectSettings(new PluginOptionsAccessor(service, Guid.Parse(device.Id)));
        }

        [Test]
        public void AllTenDevices_HaveUniqueIdsAndSafeDisconnectedInformation() {
            var devices = CreateDevices(CreateProfileService().Object);
            devices.Should().HaveCount(10);
            devices.Select(d => d.Id).Should().OnlyHaveUniqueItems();
            foreach (var device in devices) {
                device.Connected.Should().BeFalse();
                device.Name.Should().NotBeNullOrWhiteSpace();
                device.DisplayName.Should().Contain("127.0.0.1");
                device.Description.Should().BeEmpty();
                device.DriverInfo.Should().BeEmpty();
                device.DriverVersion.Should().BeEmpty();
                device.SupportedActions.Should().BeEmpty();
                device.Disconnect();
                device.Disconnect();
                device.Connected.Should().BeFalse();
            }
        }

        [Test]
        public void Settings_AreIndependentForEachDeviceAndProfile() {
            var service = CreateProfileService();
            var firstProfile = new PluginSettings();
            var secondProfile = new PluginSettings();
            var currentProfile = firstProfile;
            service.SetupGet(x => x.ActiveProfile.PluginSettings).Returns(() => currentProfile);
            var devices = CreateDevices(service.Object);
            for (int i = 0; i < devices.Length; i++) {
                var settings = Settings(service.Object, devices[i]);
                settings.Port = 5001 + i;
                settings.DeviceNumber = i;
                settings.IpAddress = " 192.0.2.10 ";
                settings.ServiceType = ServiceType.Https;
            }
            currentProfile = secondProfile;
            foreach (var device in devices) {
                var settings = Settings(service.Object, device);
                settings.Port.Should().Be(5000);
                settings.DeviceNumber.Should().Be(0);
                settings.ServiceType.Should().Be(ServiceType.Http);
                settings.IpAddress.Should().Be("127.0.0.1");
            }
            currentProfile = firstProfile;
            for (int i = 0; i < devices.Length; i++) {
                var settings = Settings(service.Object, devices[i]);
                settings.Port.Should().Be(5001 + i);
                settings.DeviceNumber.Should().Be(i);
                settings.ServiceType.Should().Be(ServiceType.Https);
                settings.IpAddress.Should().Be("192.0.2.10");
            }
        }

        [TestCase("bad address", 5000, 0)]
        [TestCase("", 5000, 0)]
        [TestCase("127.0.0.1", 0, 0)]
        [TestCase("127.0.0.1", -1, 0)]
        [TestCase("127.0.0.1", 65536, 0)]
        [TestCase("127.0.0.1", 5000, -1)]
        public async Task Connect_InvalidEndpointIsRejectedForAllDevices(string address, int port, int number) {
            var service = CreateProfileService().Object;
            foreach (var device in CreateDevices(service)) {
                var settings = Settings(service, device);
                settings.IpAddress = address;
                settings.Port = port;
                settings.DeviceNumber = number;
                Func<Task> connect = () => device.Connect(CancellationToken.None);
                await connect.Should().ThrowAsync<ArgumentException>();
                device.Connected.Should().BeFalse();
            }
        }

        [Test]
        public async Task Connect_AlreadyCanceledDoesNotStartConnectionForAnyDevice() {
            foreach (var device in CreateDevices(CreateProfileService().Object)) {
                Func<Task> connect = () => device.Connect(new CancellationToken(true));
                await connect.Should().ThrowAsync<OperationCanceledException>();
                device.Connected.Should().BeFalse();
            }
        }

        [Test]
        public async Task DiscoveryFailure_StillListsAllTenDirectConnections() {
            var service = CreateProfileService();
            var discoverySettings = new Mock<IAlpacaSettings>();
            discoverySettings.SetupGet(x => x.NumberOfPolls).Throws(new InvalidOperationException("Simulated discovery failure"));
            service.SetupGet(x => x.ActiveProfile.AlpacaSettings).Returns(discoverySettings.Object);
            var discovery = new AlpacaInteraction(service.Object);
            var lists = new List<IEnumerable<IDevice>> {
                await discovery.GetCameras(Mock.Of<IExposureDataFactory>(), CancellationToken.None),
                await discovery.GetTelescopes(CancellationToken.None),
                await discovery.GetDomes(CancellationToken.None),
                await discovery.GetFilterWheels(CancellationToken.None),
                await discovery.GetCoverCalibrators(CancellationToken.None),
                await discovery.GetFocusers(CancellationToken.None),
                await discovery.GetRotators(CancellationToken.None),
                await discovery.GetSafetyMonitors(CancellationToken.None),
                await discovery.GetSwitches(CancellationToken.None),
                await discovery.GetWeatherDataSources(CancellationToken.None)
            };
            lists.Should().HaveCount(10);
            foreach (var list in lists) {
                list.Should().ContainSingle().Which.GetType().Name.Should().StartWith("AlpacaDirect");
            }
        }

        [Test]
        public async Task SafetyMonitor_ConnectReadDisconnectAndReconnectUseConfiguredHttpEndpoint() {
            await using var server = new SafetyMonitorServer();
            var service = CreateProfileService().Object;
            var device = CreateDevices(service).Single(d => d is ISafetyMonitor);
            var settings = Settings(service, device);
            settings.Port = server.Port;
            settings.DeviceNumber = 7;
            try {
                (await device.Connect(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();
                ((ISafetyMonitor)device).IsSafe.Should().BeTrue();
                device.Disconnect();
                device.Connected.Should().BeFalse();
                (await device.Connect(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();
                ((ISafetyMonitor)device).IsSafe.Should().BeTrue();
            } finally {
                device.Disconnect();
            }
            server.Requests.Should().Contain("GET /api/v1/safetymonitor/7/issafe");
            server.Requests.Count(r => r == "PUT /api/v1/safetymonitor/7/connected").Should().Be(4);
            server.Connected.Should().BeFalse();
        }

        [Test]
        [Explicit("Creates a WPF Application dispatcher. Run this view-construction test alone in its own test process.")]
        [Apartment(ApartmentState.STA)]
        public void SetupTemplate_ConstructsAndBindsEndpointFields() {
            var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            foreach (var uri in new[] {
                "/NINA.WPF.Base;component/Resources/StaticResources/ProfileService.xaml",
                "/NINA.WPF.Base;component/Resources/StaticResources/Brushes.xaml",
                "/NINA;component/Resources/StaticResources/DataTemplates.xaml"
            }) {
                if (!app.Resources.MergedDictionaries.Any(d => d.Source?.OriginalString == uri)) {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) });
                }
            }
            var service = CreateProfileService().Object;
            var settings = Settings(service, CreateDevices(service)[0]);
            var template = (DataTemplate)app.FindResource(new DataTemplateKey(typeof(AlpacaDirectSettings)));
            var view = (FrameworkElement)template.LoadContent();
            view.DataContext = settings;
            var host = new Window { Content = view, ShowInTaskbar = false, ShowActivated = false };
            try {
                host.Show();
                host.UpdateLayout();
                var inputs = Descendants<TextBox>(view).ToArray();
                inputs.Select(t => t.Text).Should().BeEquivalentTo("127.0.0.1", "5000", "0");
                var port = inputs.Single(t => t.Text == "5000");
                port.Text = "6000";
                port.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
                settings.Port.Should().Be(6000);
            } finally {
                host.Close();
            }
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T match) yield return match;
                foreach (var descendant in Descendants<T>(child)) yield return descendant;
            }
        }

        private sealed class SafetyMonitorServer : IAsyncDisposable {
            private readonly TcpListener listener = new(IPAddress.Loopback, 0);
            private readonly CancellationTokenSource cancellation = new();
            private readonly Task worker;
            public ConcurrentBag<string> Requests { get; } = new();
            public bool Connected { get; private set; }
            public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

            public SafetyMonitorServer() {
                listener.Start();
                worker = Task.Run(Run);
            }

            private async Task Run() {
                try {
                    while (!cancellation.IsCancellationRequested) {
                        using var client = await listener.AcceptTcpClientAsync(cancellation.Token);
                        await using var stream = client.GetStream();
                        using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                        var request = (await reader.ReadLineAsync(cancellation.Token))!.Split(' ');
                        string path = request[1].Split('?')[0].ToLowerInvariant();
                        Requests.Add(request[0] + " " + path);
                        int length = 0;
                        string? header;
                        while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync(cancellation.Token))) {
                            if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) {
                                length = int.Parse(header.Split(':')[1]);
                            }
                        }
                        char[] body = new char[length];
                        int read = 0;
                        while (read < body.Length) {
                            int count = await reader.ReadAsync(body.AsMemory(read), cancellation.Token);
                            if (count == 0) throw new EndOfStreamException();
                            read += count;
                        }
                        if (request[0] == "PUT" && path.EndsWith("/connected")) {
                            Connected = new string(body).Contains("Connected=true", StringComparison.OrdinalIgnoreCase);
                        }
                        object value = path.Split('/').Last() switch {
                            "connected" => Connected,
                            "issafe" => true,
                            "name" => "Test Alpaca Safety Monitor",
                            "interfaceversion" => 3,
                            "supportedactions" => Array.Empty<string>(),
                            "devicestate" => Array.Empty<object>(),
                            _ => "Test driver"
                        };
                        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { Value = value, ClientTransactionID = 0, ServerTransactionID = 1, ErrorNumber = 0, ErrorMessage = "" });
                        await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n"), cancellation.Token);
                        await stream.WriteAsync(bytes, cancellation.Token);
                    }
                } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            }

            public async ValueTask DisposeAsync() {
                cancellation.Cancel();
                listener.Stop();
                await worker;
                cancellation.Dispose();
            }
        }
    }
}
