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
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NUnit.Framework;
using System.Reflection;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class PluginCompatibilityMapTest {

        private const string OrbuculumIdentifier = "a6c614f3-c3ab-423a-8be1-d480a253f07c";

        /// <summary>
        /// Verifies that an unknown plugin compiled for the current plugin API floor is accepted
        /// and does not inherit an artificial minimum plugin version from the hard-coded map.
        /// </summary>
        [Test]
        public void IsCompatible_AcceptsUnknownPluginCompiledForCurrentPluginApi() {
            var compatibilityMap = new PluginCompatibilityMap();
            var manifest = CreateManifest("f67eeaf0-7123-4c5c-9579-83032bbf6f85", "1.2.3.4", CurrentMinimumApplicationVersion().ToString());

            compatibilityMap.IsCompatible(manifest).Should().BeTrue();
            compatibilityMap.IsNotCompatible(manifest).Should().BeFalse();
            compatibilityMap.GetMinimumVersion(manifest).Should().Be(new Version(0, 0, 0, 0));
        }

        /// <summary>
        /// Verifies that a plugin built against an application API lower than the assembly metadata
        /// floor is rejected even when the plugin itself is not listed in the compatibility map.
        /// </summary>
        [Test]
        public void IsCompatible_RejectsPluginCompiledForLegacyApplicationApi() {
            var compatibilityMap = new PluginCompatibilityMap();
            var manifest = CreateManifest("11fca23c-ce52-44f5-9889-b6a7764eae2d", "4.0.0.0", VersionBelow(CurrentMinimumApplicationVersion()).ToString());

            compatibilityMap.IsCompatible(manifest).Should().BeFalse();
            compatibilityMap.IsNotCompatible(manifest).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that a known plugin with a version below its minimum safe version is rejected
        /// and reported as requiring an update instead of being treated like an unknown plugin.
        /// </summary>
        [Test]
        public void IsCompatible_RejectsKnownPluginBelowRequiredVersion() {
            var compatibilityMap = new PluginCompatibilityMap();
            var manifest = CreateManifest(OrbuculumIdentifier, "1.0.3.9", CurrentMinimumApplicationVersion().ToString());

            compatibilityMap.IsCompatible(manifest).Should().BeFalse();
            compatibilityMap.IsUpdateRequired(manifest).Should().BeTrue();
            compatibilityMap.GetMinimumVersion(manifest).Should().Be(new Version(1, 0, 4, 0));
        }

        /// <summary>
        /// Verifies that a known plugin newer than the compatibility-map floor is accepted and no
        /// longer reports an update-required state.
        /// </summary>
        [Test]
        public void IsCompatible_AcceptsKnownPluginAboveRequiredVersion() {
            var compatibilityMap = new PluginCompatibilityMap();
            var manifest = CreateManifest(OrbuculumIdentifier, "1.0.4.1", CurrentMinimumApplicationVersion().ToString());

            compatibilityMap.IsCompatible(manifest).Should().BeTrue();
            compatibilityMap.IsUpdateRequired(manifest).Should().BeFalse();
        }

        /// <summary>
        /// Verifies the sentinel version used by the compatibility model to identify manifests that
        /// should be considered deprecated by version value alone.
        /// </summary>
        [Test]
        public void IsDeprecated_UsesDocumentedSentinelVersion() {
            var compatibilityMap = new PluginCompatibilityMap();
            var deprecatedManifest = CreateManifest("0529e68b-fcc5-4ef3-b116-13ac6e83fc33", "65535.0.0.0", CurrentMinimumApplicationVersion().ToString());
            var supportedManifest = CreateManifest("0529e68b-fcc5-4ef3-b116-13ac6e83fc33", "65534.9999.9999.9999", CurrentMinimumApplicationVersion().ToString());

            compatibilityMap.IsDeprecated(deprecatedManifest).Should().BeTrue();
            compatibilityMap.IsDeprecated(supportedManifest).Should().BeFalse();
        }

        private static IPluginManifest CreateManifest(string identifier, string version, string minimumApplicationVersion) {
            return new PluginManifest {
                Identifier = identifier,
                Name = "Compatibility Test Plugin",
                Version = new PluginVersion(version),
                MinimumApplicationVersion = new PluginVersion(minimumApplicationVersion),
                Descriptions = new PluginDescription {
                    ShortDescription = "Compatibility test fixture"
                },
                Installer = new PluginInstallerDetails {
                    URL = "https://example.invalid/plugin.dll",
                    Checksum = new string('a', 64),
                    ChecksumType = InstallerChecksum.SHA256,
                    Type = InstallerType.DLL
                }
            };
        }

        private static Version CurrentMinimumApplicationVersion() {
            string value = typeof(PluginLoader).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(x => x.Key == "PluginMinimumApplicationVersion")
                .Value;
            return new Version(value);
        }

        private static Version VersionBelow(Version version) {
            if (version.Build > 0) {
                return new Version(version.Major, version.Minor, version.Build - 1, version.Revision);
            }
            if (version.Minor > 0) {
                return new Version(version.Major, version.Minor - 1, 0, version.Revision);
            }
            return new Version(Math.Max(0, version.Major - 1), 0, 0, version.Revision);
        }
    }
}
