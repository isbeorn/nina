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
using NUnit.Framework;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class PluginAssemblyReaderTest {

        /// <summary>
        /// Verifies that the metadata-only assembly reader can identify NINA.Plugin dependencies
        /// without loading the target assembly into the process.
        /// </summary>
        [Test]
        public void GrabAssemblyReferences_ReturnsReferencedAssemblyNames() {
            string assemblyPath = typeof(PluginLoader).Assembly.Location;

            List<string> references = PluginAssemblyReader.GrabAssemblyReferences(assemblyPath);

            references.Should().Contain("NINA.Core");
            references.Should().Contain("NINA.Sequencer");
            references.Should().Contain("System.Runtime");
        }

        /// <summary>
        /// Verifies that metadata needed for failed-plugin fallback manifests can be read from a
        /// plugin assembly file without executing plugin code.
        /// </summary>
        [Test]
        public void GrabPluginMetaData_ReturnsAssemblyAttributesAndPluginMetadata() {
            string assemblyPath = typeof(PluginLoader).Assembly.Location;
            string expectedFileVersion = typeof(PluginLoader).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
            string expectedMinimumApplicationVersion = typeof(PluginLoader).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(x => x.Key == "PluginMinimumApplicationVersion")
                .Value;

            Dictionary<string, string> metadata = PluginAssemblyReader.GrabPluginMetaData(assemblyPath);

            metadata[nameof(AssemblyTitleAttribute)].Should().Be("NINA.Plugin");
            metadata[nameof(AssemblyDescriptionAttribute)].Should().Be("This assembly contains the plugin related components of N.I.N.A.");
            metadata[nameof(GuidAttribute)].Should().Be("03ace0fa-069a-43cf-8b20-7bb3c32d8c1d");
            metadata[nameof(AssemblyFileVersionAttribute)].Should().Be(expectedFileVersion);
            metadata["PluginMinimumApplicationVersion"].Should().Be(expectedMinimumApplicationVersion);
        }
    }
}
