using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class WeatherDataVMTest {
        private Mock<IProfileService> profileService;
        private Mock<IProfile> profile;
        private Mock<IApplicationSettings> applicationSettings;
        private Mock<IWeatherDataSettings> weatherDataSettings;
        private Mock<IWeatherDataMediator> weatherDataMediator;
        private Mock<IApplicationStatusMediator> applicationStatusMediator;
        private Mock<IDeviceChooserVM> deviceChooser;

        [SetUp]
        public void SetUp() {
            EnsureApplicationResources();

            profileService = new Mock<IProfileService>();
            profile = new Mock<IProfile>();
            applicationSettings = new Mock<IApplicationSettings>();
            weatherDataSettings = new Mock<IWeatherDataSettings>();
            weatherDataMediator = new Mock<IWeatherDataMediator>();
            applicationStatusMediator = new Mock<IApplicationStatusMediator>();
            deviceChooser = new Mock<IDeviceChooserVM>();

            applicationSettings.SetupProperty(x => x.DevicePollingInterval, 0);
            weatherDataSettings.SetupProperty(x => x.Id, string.Empty);
            weatherDataSettings.SetupProperty(x => x.LastDeviceName, string.Empty);
            profile.SetupGet(x => x.ApplicationSettings).Returns(applicationSettings.Object);
            profile.SetupGet(x => x.WeatherDataSettings).Returns(weatherDataSettings.Object);
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            deviceChooser.SetupGet(x => x.Devices).Returns(new List<IDevice>());
            deviceChooser.Setup(x => x.GetEquipment()).Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Verifies that a successful weather-device connection copies all environmental sensor values,
        /// persists the selected device, raises Connected, and exposes the populated WeatherDataInfo snapshot.
        /// This protects the imaging and safety UI paths that depend on deterministic telemetry immediately after connection.
        /// </summary>
        [Test]
        public async Task Connect_WhenWeatherDeviceConnects_PopulatesTelemetryAndPersistsProfileSelection() {
            WeatherDataVM vm = CreateVm();
            Mock<IWeatherData> weatherData = CreateWeatherData(connects: true);
            bool connectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(weatherData.Object);
            vm.Connected += (_, _) => {
                connectedRaised = true;
                return Task.CompletedTask;
            };

            bool connected = await vm.Connect();

            connected.Should().BeTrue();
            connectedRaised.Should().BeTrue();
            vm.WeatherData.Should().BeSameAs(weatherData.Object);
            vm.WeatherDataInfo.Connected.Should().BeTrue();
            vm.WeatherDataInfo.DeviceId.Should().Be("weather-data");
            vm.WeatherDataInfo.DisplayName.Should().Be("Weather Data Display");
            vm.WeatherDataInfo.AveragePeriod.Should().Be(0.5);
            vm.WeatherDataInfo.CloudCover.Should().Be(21.5);
            vm.WeatherDataInfo.DewPoint.Should().Be(4.2);
            vm.WeatherDataInfo.Humidity.Should().Be(67.1);
            vm.WeatherDataInfo.Pressure.Should().Be(1012.3);
            vm.WeatherDataInfo.RainRate.Should().Be(0.0);
            vm.WeatherDataInfo.SkyBrightness.Should().Be(12.4);
            vm.WeatherDataInfo.SkyQuality.Should().Be(20.8);
            vm.WeatherDataInfo.SkyTemperature.Should().Be(-18.2);
            vm.WeatherDataInfo.StarFWHM.Should().Be(2.3);
            vm.WeatherDataInfo.Temperature.Should().Be(8.7);
            vm.WeatherDataInfo.WindDirection.Should().Be(182.0);
            vm.WeatherDataInfo.WindGust.Should().Be(5.4);
            vm.WeatherDataInfo.WindSpeed.Should().Be(2.1);
            weatherDataSettings.Object.Id.Should().Be("weather-data");
            weatherDataSettings.Object.LastDeviceName.Should().Be("Weather Data Display");
            weatherDataMediator.Verify(x => x.RegisterHandler(vm), Times.Once);
            weatherDataMediator.Verify(x => x.Broadcast(It.Is<WeatherDataInfo>(info => info.Connected && info.DeviceId == "weather-data" && info.Temperature == 8.7)), Times.AtLeastOnce);
            vm.GetDeviceInfo().Should().BeSameAs(vm.WeatherDataInfo);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && !string.IsNullOrEmpty(status.Status))), Times.Once);
            applicationStatusMediator.Verify(x => x.StatusUpdate(It.Is<ApplicationStatus>(status => status.Source == vm.Title && status.Status == string.Empty)), Times.Once);
        }

        /// <summary>
        /// Verifies that selecting the dummy "No_Device" weather device persists the disabled state and skips driver connection.
        /// This protects profiles that intentionally run without weather telemetry.
        /// </summary>
        [Test]
        public async Task Connect_WhenNoDeviceSelected_PersistsNoDeviceAndDoesNotConnect() {
            WeatherDataVM vm = CreateVm();
            Mock<IWeatherData> weatherData = CreateWeatherData(connects: true);
            weatherData.SetupGet(x => x.Id).Returns("No_Device");
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(weatherData.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            weatherDataSettings.Object.Id.Should().Be("No_Device");
            weatherDataSettings.Object.LastDeviceName.Should().BeEmpty();
            weatherData.Verify(x => x.Connect(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Verifies that a driver-reported weather-device connection failure leaves the VM disconnected and clears the device reference.
        /// This protects retry behavior after a weather provider fails without throwing.
        /// </summary>
        [Test]
        public async Task Connect_WhenWeatherDeviceReturnsFalse_LeavesDisconnectedState() {
            WeatherDataVM vm = CreateVm();
            Mock<IWeatherData> weatherData = CreateWeatherData(connects: false);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(weatherData.Object);

            bool connected = await vm.Connect();

            connected.Should().BeFalse();
            vm.WeatherData.Should().BeNull();
            vm.WeatherDataInfo.Connected.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that disconnect resets telemetry state, calls the driver disconnect method, raises Disconnected,
        /// and broadcasts a disconnected WeatherDataInfo snapshot.
        /// This protects profile-change and shutdown cleanup paths.
        /// </summary>
        [Test]
        public async Task Disconnect_AfterConnected_ClearsStateAndBroadcastsDisconnectedInfo() {
            WeatherDataVM vm = CreateVm();
            Mock<IWeatherData> weatherData = CreateWeatherData(connects: true);
            bool disconnectedRaised = false;
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(weatherData.Object);
            vm.Disconnected += (_, _) => {
                disconnectedRaised = true;
                return Task.CompletedTask;
            };
            await vm.Connect();

            await vm.Disconnect();

            disconnectedRaised.Should().BeTrue();
            vm.WeatherData.Should().BeNull();
            vm.WeatherDataInfo.Connected.Should().BeFalse();
            double.IsNaN(vm.WeatherDataInfo.Temperature).Should().BeTrue();
            weatherData.Verify(x => x.Disconnect(), Times.AtLeastOnce);
            weatherDataMediator.Verify(x => x.Broadcast(It.Is<WeatherDataInfo>(info => !info.Connected)), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that driver action and command methods are gated by connection state and forwarded only while connected.
        /// This protects plugin and diagnostics callers from sending commands to an absent weather device.
        /// </summary>
        [Test]
        public async Task DriverCommands_AreForwardedOnlyWhenConnected() {
            WeatherDataVM vm = CreateVm();
            Mock<IWeatherData> weatherData = CreateWeatherData(connects: true);
            weatherData.Setup(x => x.Action("refresh", "now")).Returns("refreshed");
            weatherData.Setup(x => x.SendCommandString(":GV#", true)).Returns("version");
            weatherData.Setup(x => x.SendCommandBool(":CHK#", false)).Returns(true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(weatherData.Object);

            vm.Action("refresh", "now").Should().BeNull();
            vm.SendCommandString(":GV#").Should().BeNull();
            vm.SendCommandBool(":CHK#", false).Should().BeFalse();
            vm.SendCommandBlind(":STOP#", false);
            weatherData.Verify(x => x.SendCommandBlind(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            await vm.Connect();

            vm.Action("refresh", "now").Should().Be("refreshed");
            vm.SendCommandString(":GV#").Should().Be("version");
            vm.SendCommandBool(":CHK#", false).Should().BeTrue();
            vm.SendCommandBlind(":STOP#", false);
            weatherData.Verify(x => x.SendCommandBlind(":STOP#", false), Times.Once);
        }

        /// <summary>
        /// Verifies that GetDevice tracks the connected weather device lifetime and returns null after disconnect.
        /// This protects direct-device access through the common equipment VM contract.
        /// </summary>
        [Test]
        public async Task GetDevice_TracksConnectedWeatherDeviceLifetime() {
            WeatherDataVM vm = CreateVm();
            Mock<IWeatherData> weatherData = CreateWeatherData(connects: true);
            deviceChooser.SetupGet(x => x.SelectedDevice).Returns(weatherData.Object);

            await vm.Connect();
            vm.GetDevice().Should().BeSameAs(weatherData.Object);

            await vm.Disconnect();
            vm.GetDevice().Should().BeNull();
        }

        private WeatherDataVM CreateVm() {
            return new WeatherDataVM(profileService.Object, weatherDataMediator.Object, applicationStatusMediator.Object, deviceChooser.Object);
        }

        private static Mock<IWeatherData> CreateWeatherData(bool connects) {
            Mock<IWeatherData> weatherData = new Mock<IWeatherData>();
            weatherData.SetupGet(x => x.Id).Returns("weather-data");
            weatherData.SetupGet(x => x.Name).Returns("Weather Data");
            weatherData.SetupGet(x => x.DisplayName).Returns("Weather Data Display");
            weatherData.SetupGet(x => x.Description).Returns("Weather description");
            weatherData.SetupGet(x => x.DriverInfo).Returns("Driver info");
            weatherData.SetupGet(x => x.DriverVersion).Returns("1.2.3");
            weatherData.SetupGet(x => x.SupportedActions).Returns(new List<string> { "refresh" });
            weatherData.SetupGet(x => x.Connected).Returns(connects);
            weatherData.SetupGet(x => x.AveragePeriod).Returns(0.5);
            weatherData.SetupGet(x => x.CloudCover).Returns(21.5);
            weatherData.SetupGet(x => x.DewPoint).Returns(4.2);
            weatherData.SetupGet(x => x.Humidity).Returns(67.1);
            weatherData.SetupGet(x => x.Pressure).Returns(1012.3);
            weatherData.SetupGet(x => x.RainRate).Returns(0.0);
            weatherData.SetupGet(x => x.SkyBrightness).Returns(12.4);
            weatherData.SetupGet(x => x.SkyQuality).Returns(20.8);
            weatherData.SetupGet(x => x.SkyTemperature).Returns(-18.2);
            weatherData.SetupGet(x => x.StarFWHM).Returns(2.3);
            weatherData.SetupGet(x => x.Temperature).Returns(8.7);
            weatherData.SetupGet(x => x.WindDirection).Returns(182.0);
            weatherData.SetupGet(x => x.WindGust).Returns(5.4);
            weatherData.SetupGet(x => x.WindSpeed).Returns(2.1);
            weatherData.Setup(x => x.Connect(It.IsAny<CancellationToken>())).ReturnsAsync(connects);
            return weatherData;
        }

        private static void EnsureApplicationResources() {
            if (Application.Current == null) {
                _ = new Application();
            }

            Application.Current.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            Application.Current.Resources["CloudSVG"] = new GeometryGroup();
        }
    }
}
