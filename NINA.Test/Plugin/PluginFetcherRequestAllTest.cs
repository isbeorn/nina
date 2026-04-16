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
using NINA.Core.Utility;
using NINA.Core.Model;
using NINA.Plugin;
using NINA.Plugin.ManifestDefinition;
using NUnit.Framework;
using System.Reflection;
using System.Text;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class PluginFetcherRequestAllTest {

        /// <summary>
        /// Verifies that repository fetching accepts only schema-valid manifests compatible with the
        /// requested application version and the current plugin API floor.
        /// </summary>
        [Test]
        public async Task RequestAll_FiltersRepositoryManifestsBySchemaAndCompatibility() {
            Version minimumPluginApiVersion = CurrentMinimumApplicationVersion();
            Version futureApplicationVersion = new Version(minimumPluginApiVersion.Major + 1, 0, 0, 0);
            Version legacyApplicationVersion = VersionBelow(minimumPluginApiVersion);
            string manifests = "[" +
                CreateManifestJson("Compatible Spectroscopy Planner", "20e79561-6a8d-48fd-a584-13c4eb543fbf", minimumPluginApiVersion.ToString()) + "," +
                CreateManifestJson("Future API Planner", "985a03bb-cb30-4256-a6f9-0370eed5673f", futureApplicationVersion.ToString()) + "," +
                CreateManifestJson("Legacy API Planner", "d343d3b8-9a4a-4690-aefc-0bd9cd1de840", legacyApplicationVersion.ToString()) + "," +
                "{ 'Name': 'Invalid Missing Installer' }" +
                "]";
            using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes(manifests), "application/json");
            var fetcher = new PluginFetcher(server.Url);

            IList<PluginManifest> plugins = await fetcher.RequestAll(new PluginVersion(CoreUtil.Version), new Progress<ApplicationStatus>(), CancellationToken.None);

            plugins.Should().ContainSingle();
            plugins.Single().Name.Should().Be("Compatible Spectroscopy Planner");
            plugins.Single().Identifier.Should().Be("20e79561-6a8d-48fd-a584-13c4eb543fbf");
        }

        private static string CreateManifestJson(string name, string identifier, string minimumApplicationVersion) {
            return @"{
                'Name': '" + name + @"',
                'Identifier': '" + identifier + @"',
                'Version': {
                    'Major': 1,
                    'Minor': 0,
                    'Patch': 0,
                    'Build': 0
                },
                'Author': 'NINA Test Observatory',
                'Repository': 'https://example.invalid/repository.git',
                'License': 'MPL-2.0',
                'LicenseURL': 'https://www.mozilla.org/en-US/MPL/2.0/',
                'MinimumApplicationVersion': {
                    'Major': " + new PluginVersion(minimumApplicationVersion).Major + @",
                    'Minor': " + new PluginVersion(minimumApplicationVersion).Minor + @",
                    'Patch': " + new PluginVersion(minimumApplicationVersion).Patch + @",
                    'Build': " + new PluginVersion(minimumApplicationVersion).Build + @"
                },
                'Descriptions': {
                    'ShortDescription': 'Repository compatibility fixture'
                },
                'Installer': {
                    'URL': 'https://example.invalid/download/plugin.dll',
                    'Checksum': '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
                    'ChecksumType': 'SHA256',
                    'Type': 'DLL'
                }
            }";
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
