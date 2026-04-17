using FluentAssertions;
using NINA.Profile;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NINA.Test.ProfileTest {

    [TestFixture]
    public class PluginSettingsGeneratedTypesTest {

        /// <summary>
        /// Verifies that the generated plugin settings storage preserves every supported primitive value type without cross-type coercion.
        /// </summary>
        [Test]
        public void SetValueAndTryGetValue_RoundTripEveryGeneratedPrimitiveType() {
            Guid pluginId = Guid.Parse("5a49d13f-d3c0-4249-bef0-08e6f2039600");
            PluginSettings settings = new PluginSettings();
            DateTime timestamp = new DateTime(2026, 4, 17, 21, 15, 0, DateTimeKind.Utc);
            Guid storedGuid = Guid.Parse("c768e81c-8164-4eec-a09c-d59208923ea7");

            settings.SetValue(pluginId, "bool", true);
            settings.SetValue(pluginId, "byte", (byte)251);
            settings.SetValue(pluginId, "sbyte", (sbyte)-12);
            settings.SetValue(pluginId, "char", 'N');
            settings.SetValue(pluginId, "decimal", 123.456m);
            settings.SetValue(pluginId, "double", 98.765d);
            settings.SetValue(pluginId, "single", 54.25f);
            settings.SetValue(pluginId, "int", -12345);
            settings.SetValue(pluginId, "uint", 12345u);
            settings.SetValue(pluginId, "long", -9876543210L);
            settings.SetValue(pluginId, "ulong", 9876543210UL);
            settings.SetValue(pluginId, "short", (short)-1234);
            settings.SetValue(pluginId, "ushort", (ushort)1234);
            settings.SetValue(pluginId, "string", "plugin value");
            settings.SetValue(pluginId, "datetime", timestamp);
            settings.SetValue(pluginId, "guid", storedGuid);

            settings.TryGetValue(pluginId, "bool", out bool boolValue).Should().BeTrue();
            boolValue.Should().BeTrue();
            settings.TryGetValue(pluginId, "byte", out byte byteValue).Should().BeTrue();
            byteValue.Should().Be(251);
            settings.TryGetValue(pluginId, "sbyte", out sbyte sbyteValue).Should().BeTrue();
            sbyteValue.Should().Be(-12);
            settings.TryGetValue(pluginId, "char", out char charValue).Should().BeTrue();
            charValue.Should().Be('N');
            settings.TryGetValue(pluginId, "decimal", out decimal decimalValue).Should().BeTrue();
            decimalValue.Should().Be(123.456m);
            settings.TryGetValue(pluginId, "double", out double doubleValue).Should().BeTrue();
            doubleValue.Should().Be(98.765d);
            settings.TryGetValue(pluginId, "single", out float singleValue).Should().BeTrue();
            singleValue.Should().Be(54.25f);
            settings.TryGetValue(pluginId, "int", out int intValue).Should().BeTrue();
            intValue.Should().Be(-12345);
            settings.TryGetValue(pluginId, "uint", out uint uintValue).Should().BeTrue();
            uintValue.Should().Be(12345u);
            settings.TryGetValue(pluginId, "long", out long longValue).Should().BeTrue();
            longValue.Should().Be(-9876543210L);
            settings.TryGetValue(pluginId, "ulong", out ulong ulongValue).Should().BeTrue();
            ulongValue.Should().Be(9876543210UL);
            settings.TryGetValue(pluginId, "short", out short shortValue).Should().BeTrue();
            shortValue.Should().Be(-1234);
            settings.TryGetValue(pluginId, "ushort", out ushort ushortValue).Should().BeTrue();
            ushortValue.Should().Be(1234);
            settings.TryGetValue(pluginId, "string", out string stringValue).Should().BeTrue();
            stringValue.Should().Be("plugin value");
            settings.TryGetValue(pluginId, "datetime", out DateTime dateTimeValue).Should().BeTrue();
            dateTimeValue.Should().Be(timestamp);
            settings.TryGetValue(pluginId, "guid", out Guid guidValue).Should().BeTrue();
            guidValue.Should().Be(storedGuid);
        }

        /// <summary>
        /// Verifies that plugin setting updates raise stable plugin/key property names and skip notifications for identical typed values.
        /// </summary>
        [Test]
        public void SetValue_RaisesPluginScopedPropertyNameAndSkipsIdenticalTypedValue() {
            Guid pluginId = Guid.Parse("72d11817-07ef-4b72-b0ce-74f7be59f5c2");
            PluginSettings settings = new PluginSettings();
            List<string> propertyNames = CapturePropertyChanges(settings);

            settings.SetValue(pluginId, "gain", 120);
            settings.SetValue(pluginId, "gain", 120);
            settings.SetValue(pluginId, "gain", 121);

            propertyNames.Should().Equal($"{pluginId}-gain", $"{pluginId}-gain");
        }

        /// <summary>
        /// Verifies that setting the same typed value again skips notifications for every generated plugin setting primitive.
        /// </summary>
        [Test]
        public void SetValue_WithSameTypedValue_SkipsNotificationsForEveryGeneratedPrimitiveType() {
            Guid pluginId = Guid.Parse("0fcaa356-f1bc-44f5-8964-d5fc42c85139");
            PluginSettings settings = new PluginSettings();
            DateTime timestamp = new DateTime(2026, 4, 17, 21, 15, 0, DateTimeKind.Utc);
            Guid storedGuid = Guid.Parse("039ac49a-77c5-4fa7-806a-c992412b3b94");
            SeedGeneratedPrimitiveValues(settings, pluginId, timestamp, storedGuid);
            List<string> propertyNames = CapturePropertyChanges(settings);

            SeedGeneratedPrimitiveValues(settings, pluginId, timestamp, storedGuid);

            propertyNames.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that changing an existing typed value raises one stable plugin/key notification for every generated primitive.
        /// </summary>
        [Test]
        public void SetValue_WithChangedTypedValue_RaisesNotificationsForEveryGeneratedPrimitiveType() {
            Guid pluginId = Guid.Parse("1e039a8d-5034-43f7-b6e0-f67f1ec93e5a");
            PluginSettings settings = new PluginSettings();
            SeedGeneratedPrimitiveValues(
                settings,
                pluginId,
                new DateTime(2026, 4, 17, 21, 15, 0, DateTimeKind.Utc),
                Guid.Parse("7c6d9a1d-88cb-479a-9e3b-a7e37e419f5b"));
            List<string> propertyNames = CapturePropertyChanges(settings);

            settings.SetValue(pluginId, "bool", false);
            settings.SetValue(pluginId, "byte", (byte)252);
            settings.SetValue(pluginId, "sbyte", (sbyte)-13);
            settings.SetValue(pluginId, "char", 'P');
            settings.SetValue(pluginId, "decimal", 654.321m);
            settings.SetValue(pluginId, "double", 12.345d);
            settings.SetValue(pluginId, "single", 12.5f);
            settings.SetValue(pluginId, "int", -54321);
            settings.SetValue(pluginId, "uint", 54321u);
            settings.SetValue(pluginId, "long", -1234567890L);
            settings.SetValue(pluginId, "ulong", 1234567890UL);
            settings.SetValue(pluginId, "short", (short)-4321);
            settings.SetValue(pluginId, "ushort", (ushort)4321);
            settings.SetValue(pluginId, "string", "changed plugin value");
            settings.SetValue(pluginId, "datetime", new DateTime(2026, 4, 18, 21, 15, 0, DateTimeKind.Utc));
            settings.SetValue(pluginId, "guid", Guid.Parse("7fc40faf-b0eb-4754-a1b0-16e33e27bc62"));

            propertyNames.Should().Equal(
                $"{pluginId}-bool",
                $"{pluginId}-byte",
                $"{pluginId}-sbyte",
                $"{pluginId}-char",
                $"{pluginId}-decimal",
                $"{pluginId}-double",
                $"{pluginId}-single",
                $"{pluginId}-int",
                $"{pluginId}-uint",
                $"{pluginId}-long",
                $"{pluginId}-ulong",
                $"{pluginId}-short",
                $"{pluginId}-ushort",
                $"{pluginId}-string",
                $"{pluginId}-datetime",
                $"{pluginId}-guid");
        }

        /// <summary>
        /// Verifies that a failed typed lookup is non-destructive and leaves the correctly typed value available for later callers.
        /// </summary>
        [Test]
        public void TryGetValue_WithIncorrectTypeDoesNotRemoveStoredValue() {
            Guid pluginId = Guid.Parse("49b01447-2953-4e93-a9cd-f51c67a1a31d");
            PluginSettings settings = new PluginSettings();
            settings.SetValue(pluginId, "cameraName", "ASI2600MM");

            bool wrongTypeFound = settings.TryGetValue(pluginId, "cameraName", out int wrongTypeValue);
            bool correctTypeFound = settings.TryGetValue(pluginId, "cameraName", out string correctTypeValue);

            wrongTypeFound.Should().BeFalse();
            wrongTypeValue.Should().Be(default(int));
            correctTypeFound.Should().BeTrue();
            correctTypeValue.Should().Be("ASI2600MM");
        }

        private static void SeedGeneratedPrimitiveValues(PluginSettings settings, Guid pluginId, DateTime timestamp, Guid storedGuid) {
            settings.SetValue(pluginId, "bool", true);
            settings.SetValue(pluginId, "byte", (byte)251);
            settings.SetValue(pluginId, "sbyte", (sbyte)-12);
            settings.SetValue(pluginId, "char", 'N');
            settings.SetValue(pluginId, "decimal", 123.456m);
            settings.SetValue(pluginId, "double", 98.765d);
            settings.SetValue(pluginId, "single", 54.25f);
            settings.SetValue(pluginId, "int", -12345);
            settings.SetValue(pluginId, "uint", 12345u);
            settings.SetValue(pluginId, "long", -9876543210L);
            settings.SetValue(pluginId, "ulong", 9876543210UL);
            settings.SetValue(pluginId, "short", (short)-1234);
            settings.SetValue(pluginId, "ushort", (ushort)1234);
            settings.SetValue(pluginId, "string", "plugin value");
            settings.SetValue(pluginId, "datetime", timestamp);
            settings.SetValue(pluginId, "guid", storedGuid);
        }

        private static List<string> CapturePropertyChanges(INotifyPropertyChanged source) {
            List<string> propertyNames = new List<string>();
            source.PropertyChanged += (object sender, PropertyChangedEventArgs args) => propertyNames.Add(args.PropertyName);
            return propertyNames;
        }
    }
}
