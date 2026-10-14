using FluentAssertions;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace NINA.Test.ProfileTest {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class HubProfilePersistenceTest {
        [Test]
        public void NestedHubChanges_AutosaveAndRoundTripThroughRealProfileService() {
            string originalTempPath = CoreUtil.APPLICATIONTEMPPATH;
            string originalProfileFolder = ProfileService.PROFILEFOLDER;
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "HubProfiles", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(directory, "Profiles"));
            CoreUtil.APPLICATIONTEMPPATH = directory;
            ProfileService.PROFILEFOLDER = Path.Combine(directory, "Profiles");
            if (Application.Current == null) {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }

            var service = new ProfileService();
            try {
                service.TryLoad(null).Should().BeTrue();
                string location = service.ActiveProfile.Location;
                var safety = ((IHasDeviceHubSettings)service.ActiveProfile.SafetyMonitorSettings).HubSettings;
                var switches = ((IHasDeviceHubSettings)service.ActiveProfile.SwitchSettings).HubSettings;
                var weather = (IWeatherHubSettings)((IHasDeviceHubSettings)service.ActiveProfile.WeatherDataSettings).HubSettings;
                safety.Devices.Add(new HubDeviceSettings { Id = "safety", SourceNumber = 1 });
                switches.Devices.Add(new HubDeviceSettings { Id = "switch", SourceNumber = 2 });
                weather.Devices.Add(new HubDeviceSettings { Id = "weather", SourceNumber = 3 });
                weather.SetSource(WeatherDataField.AveragePeriod, "weather");
                WaitForPersistedText(location, "weather");

                weather.Devices = new System.Collections.ObjectModel.ObservableCollection<HubDeviceSettings> {
                    new HubDeviceSettings { Id = "weather", SourceNumber = 3, LastDeviceName = "collection-replacement" }
                };
                WaitForPersistedText(location, "collection-replacement");
                weather.Devices.Single().LastDeviceName = "autosaved-weather-rename";
                WaitForPersistedText(location, "autosaved-weather-rename");
                weather.SetSource(WeatherDataField.WindSpeed, "switch-source-marker");
                WaitForPersistedText(location, "switch-source-marker");
                var originalProfile = service.Profiles.Single(info => info.Location == location);
                service.Add();
                var secondProfile = service.Profiles.Single(info => info.Id != originalProfile.Id);
                service.SelectProfile(secondProfile).Should().BeTrue();
                var secondWeather = (IWeatherHubSettings)((IHasDeviceHubSettings)service.ActiveProfile.WeatherDataSettings).HubSettings;
                secondWeather.Should().NotBeSameAs(weather);
                secondWeather.Devices.Should().BeEmpty();
                secondWeather.Sources.Should().BeEmpty();
                secondWeather.SetSource(WeatherDataField.Temperature, "second-profile-source");
                WaitForPersistedText(secondProfile.Location, "second-profile-source");
                service.SelectProfile(originalProfile).Should().BeTrue();
                var switchedBack = (IWeatherHubSettings)((IHasDeviceHubSettings)service.ActiveProfile.WeatherDataSettings).HubSettings;
                switchedBack.Devices.Single().LastDeviceName.Should().Be("autosaved-weather-rename");
                switchedBack.Sources.Should().NotContainValue("second-profile-source");
                service.Release();
                var document = System.Xml.Linq.XDocument.Load(location);
                document.Descendants().Where(element => element.Name.LocalName == "HubSettings")
                    .SelectMany(element => element.Descendants()).Should().NotContain(element =>
                        element.Name.LocalName == "IsSafe" || element.Name.LocalName == "Connected" || element.Name.LocalName == "DeviceStatuses");

                using var restored = NINA.Profile.Profile.Load(location);
                ((IHasDeviceHubSettings)restored.SafetyMonitorSettings).HubSettings.Devices.Single().Id.Should().Be("safety");
                ((IHasDeviceHubSettings)restored.SwitchSettings).HubSettings.Devices.Single().SourceNumber.Should().Be(2);
                var restoredWeather = (IWeatherHubSettings)((IHasDeviceHubSettings)restored.WeatherDataSettings).HubSettings;
                restoredWeather.Devices.Single().LastDeviceName.Should().Be("autosaved-weather-rename");
                restoredWeather.Sources[WeatherDataField.AveragePeriod].Should().Be("weather");
                restoredWeather.Sources[WeatherDataField.WindSpeed].Should().Be("switch-source-marker");
                var events = new List<string>();
                restored.PropertyChanged += (_, e) => events.Add(e.PropertyName ?? string.Empty);
                restoredWeather.Devices.Clear();
                restoredWeather.SetSource(WeatherDataField.WindSpeed, string.Empty);
                events.Should().Equal("Settings", "Settings");
            } finally {
                service.Release();
                CoreUtil.APPLICATIONTEMPPATH = originalTempPath;
                ProfileService.PROFILEFOLDER = originalProfileFolder;
                Directory.Delete(directory, true);
            }
        }

        private static void WaitForPersistedText(string path, string expected) {
            bool saved = SpinWait.SpinUntil(() => {
                try {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd().Contains(expected, StringComparison.Ordinal);
                } catch (IOException) {
                    return false;
                }
            }, TimeSpan.FromSeconds(10));
            saved.Should().BeTrue("nested hub edits must trigger the existing profile autosave timer");
        }
    }
}
