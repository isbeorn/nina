using FluentAssertions;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.ColorSchema;
using NINA.Profile;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Media;
using ProfileModel = NINA.Profile.Profile;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class SettingsPropertyNotificationContractTest {

        /// <summary>
        /// Verifies that persisted settings properties emit their own PropertyChanged signal when changed, preserving Profile autosave semantics.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(PersistedSettingsPropertyScenarios))]
        public void PersistedSettingsProperty_WhenChanged_RaisesOwnPropertyChangedSignal(SettingPropertyScenario scenario) {
            object settings = scenario.CreateSettings();
            object newValue = scenario.CreateValue(settings);
            List<string> propertyNames = CapturePropertyChanges((INotifyPropertyChanged)settings);

            scenario.Property.SetValue(settings, newValue);

            propertyNames.Should().Contain(scenario.Property.Name);
            object actualValue = scenario.Property.GetValue(settings);
            AssertReadbackValue(scenario.Property, newValue, actualValue);
        }

        /// <summary>
        /// Verifies the explicit compatibility decision that ASCOM 32-bit data output remains disabled regardless of persisted input.
        /// </summary>
        [Test]
        public void CameraSettings_AscomCreate32BitData_RemainsDisabledForCompatibility() {
            CameraSettings settings = new CameraSettings();
            List<string> propertyNames = CapturePropertyChanges(settings);

            settings.ASCOMCreate32BitData = true;

            settings.ASCOMCreate32BitData.Should().BeFalse();
            propertyNames.Should().NotContain(nameof(CameraSettings.ASCOMCreate32BitData));
        }

        private static IEnumerable<TestCaseData> PersistedSettingsPropertyScenarios() {
            foreach (SettingsFactory factory in SettingsFactories()) {
                object settings = factory.Create();
                Type settingsType = settings.GetType();
                foreach (PropertyInfo property in settingsType.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                    if (!ShouldCoverProperty(property)) {
                        continue;
                    }

                    if (!CanCreateSampleValue(settings, property)) {
                        continue;
                    }

                    SettingPropertyScenario scenario = new SettingPropertyScenario(factory.Name, factory.Create, property);
                    yield return new TestCaseData(scenario).SetName($"{factory.Name}_{property.Name}_RaisesPropertyChanged");
                }
            }
        }

        private static IEnumerable<SettingsFactory> SettingsFactories() {
            yield return new SettingsFactory(nameof(ProfileModel.ApplicationSettings), () => new ProfileModel().ApplicationSettings);
            yield return new SettingsFactory(nameof(ProfileModel.AstrometrySettings), () => new ProfileModel().AstrometrySettings);
            yield return new SettingsFactory(nameof(ProfileModel.CameraSettings), () => new ProfileModel().CameraSettings);
            yield return new SettingsFactory(nameof(ProfileModel.ColorSchemaSettings), () => new ProfileModel().ColorSchemaSettings);
            yield return new SettingsFactory(nameof(ProfileModel.DockPanelSettings), () => new ProfileModel().DockPanelSettings);
            yield return new SettingsFactory(nameof(ProfileModel.DomeSettings), () => new ProfileModel().DomeSettings);
            yield return new SettingsFactory(nameof(ProfileModel.FilterWheelSettings), () => new ProfileModel().FilterWheelSettings);
            yield return new SettingsFactory(nameof(ProfileModel.FlatDeviceSettings), () => new ProfileModel().FlatDeviceSettings);
            yield return new SettingsFactory(nameof(ProfileModel.FlatWizardSettings), () => new ProfileModel().FlatWizardSettings);
            yield return new SettingsFactory(nameof(ProfileModel.FocuserSettings), () => new ProfileModel().FocuserSettings);
            yield return new SettingsFactory(nameof(ProfileModel.FramingAssistantSettings), () => new ProfileModel().FramingAssistantSettings);
            yield return new SettingsFactory(nameof(ProfileModel.GnssSettings), () => new ProfileModel().GnssSettings);
            yield return new SettingsFactory(nameof(ProfileModel.GuiderSettings), () => new ProfileModel().GuiderSettings);
            yield return new SettingsFactory(nameof(ProfileModel.ImageFileSettings), () => new ProfileModel().ImageFileSettings);
            yield return new SettingsFactory(nameof(ProfileModel.ImageHistorySettings), () => new ProfileModel().ImageHistorySettings);
            yield return new SettingsFactory(nameof(ProfileModel.ImageSettings), () => new ProfileModel().ImageSettings);
            yield return new SettingsFactory(nameof(ProfileModel.MeridianFlipSettings), () => new ProfileModel().MeridianFlipSettings);
            yield return new SettingsFactory(nameof(ProfileModel.PlanetariumSettings), () => new ProfileModel().PlanetariumSettings);
            yield return new SettingsFactory(nameof(ProfileModel.PlateSolveSettings), () => new ProfileModel().PlateSolveSettings);
            yield return new SettingsFactory(nameof(ProfileModel.RotatorSettings), () => new ProfileModel().RotatorSettings);
            yield return new SettingsFactory(nameof(ProfileModel.SafetyMonitorSettings), () => new ProfileModel().SafetyMonitorSettings);
            yield return new SettingsFactory(nameof(ProfileModel.SequenceSettings), () => new ProfileModel().SequenceSettings);
            yield return new SettingsFactory(nameof(ProfileModel.SnapShotControlSettings), () => new ProfileModel().SnapShotControlSettings);
            yield return new SettingsFactory(nameof(ProfileModel.SwitchSettings), () => new ProfileModel().SwitchSettings);
            yield return new SettingsFactory(nameof(ProfileModel.TelescopeSettings), () => new ProfileModel().TelescopeSettings);
            yield return new SettingsFactory(nameof(ProfileModel.WeatherDataSettings), () => new ProfileModel().WeatherDataSettings);
            yield return new SettingsFactory(nameof(ProfileModel.AlpacaSettings), () => new ProfileModel().AlpacaSettings);
        }

        private static bool ShouldCoverProperty(PropertyInfo property) {
            if (!property.CanWrite || property.GetSetMethod() == null) {
                return false;
            }

            if (property.GetCustomAttribute<DataMemberAttribute>() == null) {
                return false;
            }

            if (property.DeclaringType == typeof(CameraSettings) && property.Name == nameof(CameraSettings.ASCOMCreate32BitData)) {
                return false;
            }

            return true;
        }

        private static bool CanCreateSampleValue(object settings, PropertyInfo property) {
            try {
                _ = CreateSampleValue(settings, property);
                return true;
            } catch (NotSupportedException) {
                return false;
            }
        }

        private static object CreateSampleValue(object settings, PropertyInfo property) {
            Type propertyType = property.PropertyType;

            if (property.DeclaringType == typeof(ApplicationSettings) && property.Name == nameof(ApplicationSettings.Culture)) {
                return "de-DE";
            }

            if (property.DeclaringType == typeof(PlateSolveSettings) && property.Name == nameof(PlateSolveSettings.PinPointAllSkyApiHost)) {
                return "api.astrometry.local";
            }

            if (property.DeclaringType == typeof(PlateSolveSettings) &&
                (property.Name == nameof(PlateSolveSettings.AstrometryURL) ||
                 property.Name == nameof(PlateSolveSettings.AstrometryAPIKey) ||
                 property.Name == nameof(PlateSolveSettings.PinPointAllSkyApiKey))) {
                return "profile-test-value";
            }

            if (property.DeclaringType == typeof(FocuserSettings) && property.Name == nameof(FocuserSettings.AutoFocusInitialOffsetSteps)) {
                return 5;
            }

            if (property.DeclaringType == typeof(FocuserSettings) && property.Name == nameof(FocuserSettings.AutoFocusTotalNumberOfAttempts)) {
                return 3;
            }

            if (property.DeclaringType == typeof(FocuserSettings) && property.Name == nameof(FocuserSettings.AutoFocusNumberOfFramesPerPoint)) {
                return 2;
            }

            if (property.DeclaringType == typeof(FocuserSettings) && property.Name == nameof(FocuserSettings.RSquaredThreshold)) {
                return 0.8d;
            }

            if (property.DeclaringType == typeof(SequenceSettings) && property.Name == nameof(SequenceSettings.TimeSpanInTicks)) {
                throw new NotSupportedException("TimeSpanInTicks is a serialization proxy; EstimatedDownloadTime is the notifying runtime property.");
            }

            Type nullableType = Nullable.GetUnderlyingType(propertyType);
            if (nullableType != null) {
                return CreateNonNullableSampleValue(settings, property, nullableType);
            }

            return CreateNonNullableSampleValue(settings, property, propertyType);
        }

        private static object CreateNonNullableSampleValue(object settings, PropertyInfo property, Type valueType) {
            if (valueType == typeof(string)) {
                return $"profile-test-{property.Name}";
            }
            if (valueType == typeof(bool)) {
                bool current = property.GetValue(settings) is bool currentValue && currentValue;
                return !current;
            }
            if (valueType == typeof(byte)) {
                return (byte)42;
            }
            if (valueType == typeof(sbyte)) {
                return (sbyte)-42;
            }
            if (valueType == typeof(short)) {
                return (short)2;
            }
            if (valueType == typeof(ushort)) {
                return (ushort)4242;
            }
            if (valueType == typeof(int)) {
                return 42;
            }
            if (valueType == typeof(uint)) {
                return 42u;
            }
            if (valueType == typeof(long)) {
                return 42000000000L;
            }
            if (valueType == typeof(ulong)) {
                return 42000000000UL;
            }
            if (valueType == typeof(float)) {
                return 12.5f;
            }
            if (valueType == typeof(double)) {
                return 12.5d;
            }
            if (valueType == typeof(decimal)) {
                return 12.5m;
            }
            if (valueType == typeof(char)) {
                return 'Z';
            }
            if (valueType == typeof(DateTime)) {
                return new DateTime(2026, 4, 17, 23, 30, 0, DateTimeKind.Utc);
            }
            if (valueType == typeof(Guid)) {
                return Guid.Parse("dcfb87e7-22b9-4437-8d4a-1034e608f89e");
            }
            if (valueType == typeof(CultureInfo)) {
                return new CultureInfo("de-DE");
            }
            if (valueType == typeof(Color)) {
                return Color.FromArgb(200, 10, 20, 30);
            }
            if (valueType == typeof(BinningMode)) {
                return new BinningMode(2, 2);
            }
            if (valueType == typeof(FilterInfo)) {
                return new FilterInfo("ProfileTest", 4, 2);
            }
            if (valueType == typeof(ColorSchema)) {
                ColorSchemaSettings colorSettings = (ColorSchemaSettings)settings;
                ColorSchema current = (ColorSchema)property.GetValue(settings);
                return colorSettings.ColorSchemas.Items.First(schema => current == null || schema.Name != current.Name);
            }
            if (valueType == typeof(List<string>)) {
                return new List<string> { "Messier", "NGC" };
            }
            if (valueType.IsEnum) {
                Array values = Enum.GetValues(valueType);
                object current = property.GetValue(settings);
                return values.Cast<object>().First(value => !Equals(value, current));
            }
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(AsyncObservableCollection<>)) {
                return Activator.CreateInstance(valueType);
            }
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ObserveAllCollection<>)) {
                return Activator.CreateInstance(valueType);
            }

            throw new NotSupportedException($"No sample value is defined for {valueType.FullName}");
        }

        private static void AssertReadbackValue(PropertyInfo property, object expectedValue, object actualValue) {
            if (expectedValue is IEnumerable expectedEnumerable && expectedValue is not string && actualValue is IEnumerable actualEnumerable) {
                actualEnumerable.Cast<object>().Should().Equal(expectedEnumerable.Cast<object>());
                return;
            }

            if (property.PropertyType == typeof(ColorSchema) || property.PropertyType == typeof(FilterInfo) || property.PropertyType == typeof(BinningMode)) {
                actualValue.Should().BeSameAs(expectedValue);
                return;
            }

            actualValue.Should().Be(expectedValue);
        }

        private static List<string> CapturePropertyChanges(INotifyPropertyChanged source) {
            List<string> propertyNames = new List<string>();
            source.PropertyChanged += (object sender, PropertyChangedEventArgs args) => propertyNames.Add(args.PropertyName);
            return propertyNames;
        }

        public sealed class SettingPropertyScenario {
            public SettingPropertyScenario(string settingsName, Func<object> createSettings, PropertyInfo property) {
                SettingsName = settingsName;
                CreateSettings = createSettings;
                Property = property;
            }

            public string SettingsName { get; }
            public Func<object> CreateSettings { get; }
            public PropertyInfo Property { get; }

            public object CreateValue(object settings) {
                return CreateSampleValue(settings, Property);
            }

            public override string ToString() {
                return $"{SettingsName}.{Property.Name}";
            }
        }

        private sealed class SettingsFactory {
            public SettingsFactory(string name, Func<object> create) {
                Name = name;
                Create = create;
            }

            public string Name { get; }
            public Func<object> Create { get; }
        }
    }
}
