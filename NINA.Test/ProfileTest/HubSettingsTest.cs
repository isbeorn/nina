using FluentAssertions;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace NINA.Test.ProfileTest {
    [TestFixture]
    public class HubSettingsTest {
        [Test]
        public void NestedChangesAndRemoval_BubbleWithoutRetainingRemovedChildren() {
            var settings = new SwitchSettings();
            var changes = new List<string>();
            settings.PropertyChanged += (_, e) => changes.Add(e.PropertyName ?? string.Empty);
            var child = new HubDeviceSettings { Id = "a", SourceNumber = 1 };
            settings.HubSettings.Devices.Add(child);
            child.LastDeviceName = "Renamed";
            changes.Should().Equal("HubSettings", "HubSettings");
            settings.HubSettings.Devices.Clear();
            changes.Clear();
            child.LastDeviceName = "Detached";
            changes.Should().BeEmpty();
        }

        [Test]
        public void WeatherMapping_RoundTripsAndStillBubblesChanges() {
            var serializer = new DataContractSerializer(typeof(WeatherDataSettings));
            var settings = new WeatherDataSettings();
            var hub = (IWeatherHubSettings)settings.HubSettings;
            hub.Devices.Add(new HubDeviceSettings { Id = "weather", SourceNumber = 3 });
            hub.SetSource(WeatherDataField.AveragePeriod, "weather");
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, settings);
            stream.Position = 0;
            var restored = (WeatherDataSettings)serializer.ReadObject(stream)!;
            var restoredHub = (IWeatherHubSettings)restored.HubSettings;
            restoredHub.Sources[WeatherDataField.AveragePeriod].Should().Be("weather");
            restoredHub.Devices.Single().SourceNumber.Should().Be(3);
            using var monitor = restored.Monitor();
            restoredHub.SetSource(WeatherDataField.WindSpeed, "weather");
            restoredHub.Devices[0].LastDeviceName = "Updated";
            monitor.Should().RaisePropertyChangeFor(x => x.HubSettings);
        }

        [TestCaseSource(nameof(SettingsTypes))]
        public void InterfaceCollectionReplacement_BubblesAndDetachesOldEntries(System.Type type) {
            var owner = (IHasDeviceHubSettings)System.Activator.CreateInstance(type, true)!;
            IDeviceHubSettings hub = owner.HubSettings;
            var previous = new HubDeviceSettings { Id = "previous" };
            hub.Devices.Add(previous);
            var replacement = new HubDeviceSettings { Id = "replacement" };
            hub.Devices = new System.Collections.ObjectModel.ObservableCollection<HubDeviceSettings> { replacement };
            var events = new List<string>();
            ((ISettings)owner).PropertyChanged += (_, e) => events.Add(e.PropertyName!);
            IHubDeviceSettings entry = replacement;
            entry.LastDeviceName = "Updated";
            events.Should().Equal("HubSettings");
            events.Clear();
            previous.LastDeviceName = "Detached";
            events.Should().BeEmpty();
            hub.Devices.Clear();
            events.Clear();
            entry.SourceNumber = 7;
            events.Should().BeEmpty();
        }

        [Test]
        public void MissingNestedMembers_RestoreDefaultsWithOriginalSerializedTypes() {
            var serializer = new DataContractSerializer(typeof(DeviceHubSettings));
            IWeatherHubSettings settings = new WeatherHubSettings();
            settings.Devices.Add(new HubDeviceSettings { Id = "weather", LastDeviceName = "Weather sensor", SourceNumber = 3 });
            settings.SetSource(WeatherDataField.Temperature, "weather");
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, settings);
            stream.Position = 0;
            var document = XDocument.Load(stream);
            document.Root!.Name.LocalName.Should().Be("DeviceHubSettings");
            document.Root.Attribute(XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance"))!.Value.Should().Be("WeatherHubSettings");
            var entry = document.Descendants().Single(element => element.Name.LocalName == "HubDeviceSettings");
            entry.Elements().Select(element => element.Name.LocalName).Should().Equal("Id", "LastDeviceName", "SourceNumber");
            entry.RemoveNodes();
            document.Descendants().Single(element => element.Name.LocalName == "Sources").Remove();
            stream.SetLength(0);
            document.Save(stream);
            stream.Position = 0;
            var restored = (IWeatherHubSettings)serializer.ReadObject(stream)!;
            restored.Sources.Should().BeEmpty();
            restored.Devices.Single().Id.Should().BeEmpty();
            restored.Devices.Single().LastDeviceName.Should().BeEmpty();
            restored.Devices.Single().SourceNumber.Should().Be(0);
        }

        private static System.Type[] SettingsTypes => new[] {
            typeof(SwitchSettings), typeof(WeatherDataSettings),
            typeof(SwitchSettings).Assembly.GetType("NINA.Profile.SafetyMonitorSettings")!
        };

        [TestCaseSource(nameof(SettingsTypes))]
        public void LegacySettingsWithoutHub_InitializeEmptySettings(System.Type type) {
            var serializer = new DataContractSerializer(type);
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, System.Activator.CreateInstance(type, true));
            stream.Position = 0;
            var document = XDocument.Load(stream);
            document.Descendants().Single(x => x.Name.LocalName == "HubSettings").Remove();
            stream.SetLength(0);
            document.Save(stream);
            stream.Position = 0;
            var restored = (IHasDeviceHubSettings)serializer.ReadObject(stream)!;
            restored.HubSettings.Devices.Should().BeEmpty();
            if (type == typeof(WeatherDataSettings)) {
                restored.HubSettings.Should().BeOfType<WeatherHubSettings>();
            }
        }
    }
}
