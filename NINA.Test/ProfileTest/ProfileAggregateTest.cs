using FluentAssertions;
using NINA.Core.Model.Equipment;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using ProfileModel = NINA.Profile.Profile;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class ProfileAggregateTest {

        /// <summary>
        /// Verifies that a newly created profile is a complete aggregate with every persisted settings section initialized.
        /// </summary>
        [Test]
        public void Constructor_InitializesEverySettingsSection() {
            ProfileModel profile = new ProfileModel("Observatory");

            profile.Id.Should().NotBeEmpty();
            profile.Name.Should().Be("Observatory");
            profile.Description.Should().BeEmpty();
            profile.ApplicationSettings.Should().NotBeNull();
            profile.AstrometrySettings.Should().NotBeNull();
            profile.CameraSettings.Should().NotBeNull();
            profile.ColorSchemaSettings.Should().NotBeNull();
            profile.DomeSettings.Should().NotBeNull();
            profile.FilterWheelSettings.Should().NotBeNull();
            profile.FlatWizardSettings.Should().NotBeNull();
            profile.FocuserSettings.Should().NotBeNull();
            profile.FramingAssistantSettings.Should().NotBeNull();
            profile.GuiderSettings.Should().NotBeNull();
            profile.ImageFileSettings.Should().NotBeNull();
            profile.ImageSettings.Should().NotBeNull();
            profile.MeridianFlipSettings.Should().NotBeNull();
            profile.PlanetariumSettings.Should().NotBeNull();
            profile.PlateSolveSettings.Should().NotBeNull();
            profile.RotatorSettings.Should().NotBeNull();
            profile.FlatDeviceSettings.Should().NotBeNull();
            profile.SequenceSettings.Should().NotBeNull();
            profile.SwitchSettings.Should().NotBeNull();
            profile.TelescopeSettings.Should().NotBeNull();
            profile.WeatherDataSettings.Should().NotBeNull();
            profile.SnapShotControlSettings.Should().NotBeNull();
            profile.SafetyMonitorSettings.Should().NotBeNull();
            profile.PluginSettings.Should().NotBeNull();
            profile.GnssSettings.Should().NotBeNull();
            profile.DockPanelSettings.Should().NotBeNull();
            profile.AlpacaSettings.Should().NotBeNull();
            profile.ImageHistorySettings.Should().NotBeNull();
        }

        /// <summary>
        /// Verifies that profile identity fields notify both their own property and the aggregate Settings marker used by autosave.
        /// </summary>
        [Test]
        public void NameAndDescription_RaiseOwnPropertyAndSettingsOnlyWhenValueChanges() {
            ProfileModel profile = new ProfileModel("Original");
            List<string> propertyNames = CapturePropertyChanges(profile);

            profile.Name = "Science Rig";
            profile.Description = "Backyard narrowband profile";
            profile.Name = "Science Rig";
            profile.Description = "Backyard narrowband profile";

            propertyNames.Should().Equal(
                nameof(ProfileModel.Name),
                "Settings",
                nameof(ProfileModel.Description),
                "Settings");
        }

        /// <summary>
        /// Verifies that child settings changes bubble up as the profile-level Settings marker so ProfileService can schedule a save.
        /// </summary>
        [Test]
        public void ChildSettingChange_RaisesProfileSettingsNotification() {
            ProfileModel profile = new ProfileModel("Observatory");
            List<string> propertyNames = CapturePropertyChanges(profile);

            profile.CameraSettings.PixelSize = 4.63d;
            profile.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("Ha", -20, 3));
            profile.PluginSettings.SetValue(Guid.Parse("0f129a06-9adb-41fc-ac13-894c2b10ae8a"), "gain", 120);

            propertyNames.Should().Contain("Settings");
            propertyNames.Should().OnlyContain(propertyName => propertyName == "Settings");
            propertyNames.Should().HaveCount(3);
        }

        /// <summary>
        /// Verifies that cloning produces a deep copy with a new identity while preserving scientifically relevant imaging settings.
        /// </summary>
        [Test]
        public void Clone_CreatesIndependentCopyWithNewIdentityAndPreservedSettings() {
            Guid pluginId = Guid.Parse("34ed110c-1621-470b-8b92-98af9970c7f5");
            ProfileModel original = new ProfileModel("Deep Sky") {
                Description = "Dark-site equipment profile"
            };
            original.CameraSettings.PixelSize = 3.76d;
            original.CameraSettings.MinFlatExposureTime = 0.4d;
            original.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("L", 0, 0));
            original.FilterWheelSettings.FilterWheelFilters.Add(new FilterInfo("OIII", 15, 2));
            original.PluginSettings.SetValue(pluginId, "calibrationFrames", 25);

            IProfile clone = ProfileModel.Clone(original);

            clone.Id.Should().NotBe(original.Id);
            clone.Name.Should().Be("Deep Sky Copy");
            clone.Description.Should().Be(original.Description);
            clone.CameraSettings.Should().NotBeSameAs(original.CameraSettings);
            clone.CameraSettings.PixelSize.Should().Be(3.76d);
            clone.CameraSettings.MinFlatExposureTime.Should().Be(0.4d);
            clone.FilterWheelSettings.FilterWheelFilters.Should().NotBeSameAs(original.FilterWheelSettings.FilterWheelFilters);
            clone.FilterWheelSettings.FilterWheelFilters.Should().HaveCount(2);
            clone.PluginSettings.TryGetValue(pluginId, "calibrationFrames", out int calibrationFrames).Should().BeTrue();
            calibrationFrames.Should().Be(25);

            clone.CameraSettings.PixelSize = 2.4d;
            clone.FilterWheelSettings.FilterWheelFilters[0].Name = "Red";
            clone.PluginSettings.SetValue(pluginId, "calibrationFrames", 50);

            original.CameraSettings.PixelSize.Should().Be(3.76d);
            original.FilterWheelSettings.FilterWheelFilters[0].Name.Should().Be("L");
            original.PluginSettings.TryGetValue(pluginId, "calibrationFrames", out int originalCalibrationFrames).Should().BeTrue();
            originalCalibrationFrames.Should().Be(25);
        }

        private static List<string> CapturePropertyChanges(INotifyPropertyChanged source) {
            List<string> propertyNames = new List<string>();
            source.PropertyChanged += (object sender, PropertyChangedEventArgs args) => propertyNames.Add(args.PropertyName);
            return propertyNames;
        }
    }
}
