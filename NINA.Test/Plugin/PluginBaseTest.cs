#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Plugin;
using NINA.Plugin.ManifestDefinition;
using NUnit.Framework;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class PluginBaseTest {

        /// <summary>
        /// Verifies that plugin assemblies expose their runtime manifest through assembly attributes
        /// and assembly metadata, including descriptions and astronomy-relevant tag data.
        /// </summary>
        [Test]
        public void ManifestProperties_ReadAssemblyAttributesAndMetadata() {
            var plugin = CreatePluginBase(
                "a00f6af4-f9f1-4e77-b271-490d68f8578f",
                "Filter Wheel Meridian Planner",
                "Schedules filter changes around meridian-safe windows.",
                "NINA Test Observatory",
                "2.1.3.5",
                new Dictionary<string, string> {
                    ["License"] = "MPL-2.0",
                    ["LicenseURL"] = "https://www.mozilla.org/en-US/MPL/2.0/",
                    ["Homepage"] = "https://example.invalid/filter-wheel-meridian-planner",
                    ["Repository"] = "https://example.invalid/filter-wheel-meridian-planner.git",
                    ["ChangelogURL"] = "https://example.invalid/filter-wheel-meridian-planner/changelog",
                    ["Tags"] = "filter-wheel,meridian,planning",
                    ["MinimumApplicationVersion"] = "3.0.0.0",
                    ["LongDescription"] = "Coordinates planned filter changes with meridian-safe imaging windows.",
                    ["FeaturedImageURL"] = "https://example.invalid/filter-wheel-meridian-planner/featured.png",
                    ["ScreenshotURL"] = "https://example.invalid/filter-wheel-meridian-planner/main.png",
                    ["AltScreenshotURL"] = "https://example.invalid/filter-wheel-meridian-planner/secondary.png"
                });

            plugin.Identifier.Should().Be("a00f6af4-f9f1-4e77-b271-490d68f8578f");
            plugin.Name.Should().Be("Filter Wheel Meridian Planner");
            plugin.Author.Should().Be("NINA Test Observatory");
            plugin.Version.ToString().Should().Be("2.1.3.5");
            plugin.MinimumApplicationVersion.ToString().Should().Be("3.0.0.0");
            plugin.License.Should().Be("MPL-2.0");
            plugin.LicenseURL.Should().Be("https://www.mozilla.org/en-US/MPL/2.0/");
            plugin.Homepage.Should().Be("https://example.invalid/filter-wheel-meridian-planner");
            plugin.Repository.Should().Be("https://example.invalid/filter-wheel-meridian-planner.git");
            plugin.ChangelogURL.Should().Be("https://example.invalid/filter-wheel-meridian-planner/changelog");
            plugin.Tags.Should().Equal("filter-wheel", "meridian", "planning");
            plugin.Descriptions.ShortDescription.Should().Be("Schedules filter changes around meridian-safe windows.");
            plugin.Descriptions.LongDescription.Should().Be("Coordinates planned filter changes with meridian-safe imaging windows.");
            plugin.Descriptions.FeaturedImageURL.Should().Be("https://example.invalid/filter-wheel-meridian-planner/featured.png");
            plugin.Descriptions.ScreenshotURL.Should().Be("https://example.invalid/filter-wheel-meridian-planner/main.png");
            plugin.Descriptions.AltScreenshotURL.Should().Be("https://example.invalid/filter-wheel-meridian-planner/secondary.png");
        }

        /// <summary>
        /// Verifies the conservative defaults used when optional plugin metadata is absent from the
        /// assembly so plugin authors get predictable empty values instead of nulls.
        /// </summary>
        [Test]
        public void ManifestProperties_UseDefaultsWhenOptionalMetadataIsMissing() {
            var plugin = CreatePluginBase(
                "fef529e1-9826-49d0-9182-84efb1701f98",
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                new Dictionary<string, string>());

            plugin.Version.ToString().Should().Be("1.0.0.0");
            plugin.MinimumApplicationVersion.ToString().Should().Be("1.11.0.0");
            plugin.Tags.Should().BeEmpty();
            plugin.License.Should().BeEmpty();
            plugin.LicenseURL.Should().BeEmpty();
            plugin.Homepage.Should().BeEmpty();
            plugin.Repository.Should().BeEmpty();
            plugin.ChangelogURL.Should().BeEmpty();
            plugin.Descriptions.ShortDescription.Should().BeEmpty();
            plugin.Descriptions.LongDescription.Should().BeEmpty();
            plugin.Installer.Should().BeOfType<PluginInstallerDetails>();
            plugin.Installer.URL.Should().BeEmpty();
            plugin.Installer.Checksum.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the base plugin lifecycle is intentionally no-op and immediately completed
        /// unless a plugin overrides initialization or teardown behavior.
        /// </summary>
        [Test]
        public async Task LifecycleMethods_CompleteWithoutSideEffectsByDefault() {
            var plugin = CreatePluginBase(
                "ea68f114-44dd-4c40-9714-40cda2fe77d9",
                "Lifecycle Fixture",
                string.Empty,
                string.Empty,
                "1.0.0.0",
                new Dictionary<string, string>());

            await plugin.Initialize();
            await plugin.Teardown();
        }

        private static PluginBase CreatePluginBase(
            string identifier,
            string title,
            string description,
            string company,
            string fileVersion,
            IDictionary<string, string> metadata) {
            var assemblyName = new AssemblyName("NINA.Test.Plugin.PluginBaseFixture" + Guid.NewGuid().ToString("N"));
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            SetAttribute<GuidAttribute>(assemblyBuilder, identifier);
            SetAttribute<AssemblyTitleAttribute>(assemblyBuilder, title);
            SetAttribute<AssemblyDescriptionAttribute>(assemblyBuilder, description);
            SetAttribute<AssemblyCompanyAttribute>(assemblyBuilder, company);
            if (!string.IsNullOrEmpty(fileVersion)) {
                SetAttribute<AssemblyFileVersionAttribute>(assemblyBuilder, fileVersion);
            }
            foreach (KeyValuePair<string, string> item in metadata) {
                ConstructorInfo constructor = typeof(AssemblyMetadataAttribute).GetConstructor(new[] { typeof(string), typeof(string) })!;
                assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { item.Key, item.Value }));
            }

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("PluginBaseFixtureModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("SyntheticPlugin", TypeAttributes.Public, typeof(PluginBase));
            Type pluginType = typeBuilder.CreateType()!;
            return (PluginBase)Activator.CreateInstance(pluginType)!;
        }

        private static void SetAttribute<TAttribute>(AssemblyBuilder assemblyBuilder, string value) where TAttribute : Attribute {
            ConstructorInfo constructor = typeof(TAttribute).GetConstructor(new[] { typeof(string) })!;
            assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { value }));
        }
    }
}
