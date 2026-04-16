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
using NINA.Plugin;
using NUnit.Framework;
using System.Reflection;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class ConstantsTest {

        /// <summary>
        /// Verifies that plugin runtime folders are version-scoped by the plugin API floor rather
        /// than the full nightly application revision, preserving installed plugins across patch builds.
        /// </summary>
        [Test]
        public void PluginFolders_UseMinimumApplicationVersionScope() {
            string expectedMinimumApplicationVersion = typeof(PluginLoader).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(x => x.Key == "PluginMinimumApplicationVersion")
                .Value;
            var expectedVersion = new Version(expectedMinimumApplicationVersion);
            string expectedVersionFolder = $"{expectedVersion.Major}.{expectedVersion.Minor}.{expectedVersion.Build}";

            Constants.ApplicationVersion.Should().Be(new Version(CoreUtil.Version));
            Constants.ApplicationVersionWithoutRevision.Should().Be(expectedVersionFolder);

            Constants.BaseUserExtensionsFolder.Should().Be(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Plugins"));
            Constants.UserExtensionsFolder.Should().Be(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Plugins", expectedVersionFolder));
            Constants.StagingFolder.Should().Be(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginStaging", expectedVersionFolder));
            Constants.DeletionFolder.Should().Be(Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginDeletion", expectedVersionFolder));
            Constants.MainPluginRepository.Should().Be("https://nighttime-imaging.eu/wp-json/nina/v1");
        }
    }
}
