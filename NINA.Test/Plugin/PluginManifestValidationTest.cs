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
using Newtonsoft.Json.Linq;
using NINA.Plugin;
using NINA.Plugin.ManifestDefinition;
using NUnit.Framework;
using System.Reflection;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class PluginManifestValidationTest {

        /// <summary>
        /// Verifies that a repository manifest with realistic astronomy plugin metadata validates
        /// against the schema and materializes concrete manifest, version, description, and installer types.
        /// </summary>
        [Test]
        public async Task ValidateAndParseManifest_AcceptsCompleteRepositoryManifest() {
            var token = CreateValidManifestToken();

            PluginManifest manifest = await ValidateAndParseManifest(token);

            manifest.Should().NotBeNull();
            manifest.Name.Should().Be("Spectral Focus Planner");
            manifest.Identifier.Should().Be("5c26f12d-8f12-49ed-8f7d-5df71f0c923a");
            manifest.Version.Should().BeOfType<PluginVersion>();
            manifest.Version.ToString().Should().Be("1.2.3.4");
            manifest.MinimumApplicationVersion.Should().BeOfType<PluginVersion>();
            manifest.MinimumApplicationVersion.ToString().Should().Be("3.0.0.0");
            manifest.Descriptions.Should().BeOfType<PluginDescription>();
            manifest.Descriptions.ShortDescription.Should().Be("Plans focus offsets for narrowband filter changes.");
            manifest.Installer.Should().BeOfType<PluginInstallerDetails>();
            manifest.Installer.Type.Should().Be(InstallerType.DLL);
            manifest.Installer.ChecksumType.Should().Be(InstallerChecksum.SHA256);
            manifest.Tags.Should().Equal("focus", "filter-wheel", "narrowband");
        }

        /// <summary>
        /// Verifies that a manifest missing the required installer contract is rejected before it can
        /// participate in plugin discovery or compatibility checks.
        /// </summary>
        [Test]
        public async Task ValidateAndParseManifest_RejectsMissingRequiredInstaller() {
            var token = CreateValidManifestToken();
            token["Installer"]?.Parent?.Remove();

            PluginManifest manifest = await ValidateAndParseManifest(token);

            manifest.Should().BeNull();
        }

        /// <summary>
        /// Verifies that malformed checksum data is rejected by the repository schema so corrupted
        /// or ambiguous installer metadata is not accepted as a downloadable plugin version.
        /// </summary>
        [Test]
        public async Task ValidateAndParseManifest_RejectsInvalidChecksumShape() {
            var token = CreateValidManifestToken();
            token["Installer"]!["Checksum"] = "not-a-hex-checksum";

            PluginManifest manifest = await ValidateAndParseManifest(token);

            manifest.Should().BeNull();
        }

        private static JObject CreateValidManifestToken() {
            return JObject.Parse(@"{
                'Name': 'Spectral Focus Planner',
                'Identifier': '5c26f12d-8f12-49ed-8f7d-5df71f0c923a',
                'Version': {
                    'Major': '1',
                    'Minor': 2,
                    'Patch': 3,
                    'Build': 4
                },
                'Author': 'NINA Test Observatory',
                'Homepage': 'https://example.invalid/spectral-focus-planner',
                'Repository': 'https://example.invalid/spectral-focus-planner.git',
                'License': 'MPL-2.0',
                'LicenseURL': 'https://www.mozilla.org/en-US/MPL/2.0/',
                'ChangelogURL': 'https://example.invalid/spectral-focus-planner/changelog',
                'Tags': [
                    'focus',
                    'filter-wheel',
                    'narrowband'
                ],
                'MinimumApplicationVersion': {
                    'Major': 3,
                    'Minor': 0,
                    'Patch': 0,
                    'Build': 0
                },
                'Descriptions': {
                    'ShortDescription': 'Plans focus offsets for narrowband filter changes.',
                    'LongDescription': 'Uses filter-specific offsets to plan refocus operations for repeatable narrowband imaging sessions.',
                    'FeaturedImageURL': 'https://example.invalid/images/focus-planner.png',
                    'ScreenshotURL': 'https://example.invalid/images/focus-planner-main.png',
                    'AltScreenshotURL': 'https://example.invalid/images/focus-planner-alt.png'
                },
                'Installer': {
                    'URL': 'https://example.invalid/downloads/SpectralFocusPlanner.dll',
                    'Checksum': '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
                    'ChecksumType': 'SHA256',
                    'Type': 'DLL'
                }
            }");
        }

        private static async Task<PluginManifest> ValidateAndParseManifest(JToken token) {
            var fetcher = new PluginFetcher("https://example.invalid");
            MethodInfo method = typeof(PluginFetcher).GetMethod("ValidateAndParseManifest", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var task = (Task<PluginManifest>)method.Invoke(fetcher, new object[] { token })!;
            return await task;
        }
    }
}
