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
using NINA.Plugin.ManifestDefinition;
using NINA.ViewModel.Plugins;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class PluginManifestViewModelBehaviorTest {

        /// <summary>
        /// Verifies extended plugin manifests copy immutable manifest metadata and initialize UI state as not installed.
        /// </summary>
        [Test]
        public void ExtendedPluginManifest_CopiesManifestMetadataAndDefaultsState() {
            var version = new PluginVersion("1.2.3.4");
            var minimum = new PluginVersion("3.1.0.0");
            var description = new PluginDescription {
                ShortDescription = "Short",
                LongDescription = "Long"
            };
            var installer = new PluginInstallerDetails {
                URL = "https://example.invalid/plugin.zip",
                Checksum = "abc123",
                ChecksumType = InstallerChecksum.SHA256,
                Type = InstallerType.ARCHIVE
            };
            var manifest = new PluginManifest {
                Author = "Author",
                ChangelogURL = "https://example.invalid/changelog",
                Descriptions = description,
                Homepage = "https://example.invalid",
                Identifier = Guid.NewGuid().ToString(),
                Installer = installer,
                License = "MPL-2.0",
                LicenseURL = "https://example.invalid/license",
                MinimumApplicationVersion = minimum,
                Name = "Plugin",
                Repository = "https://example.invalid/repo",
                Tags = new[] { "camera", "sequencer" },
                Version = version
            };

            var sut = new ExtendedPluginManifest(manifest);

            sut.Author.Should().Be(manifest.Author);
            sut.ChangelogURL.Should().Be(manifest.ChangelogURL);
            sut.Descriptions.Should().BeSameAs(description);
            sut.Homepage.Should().Be(manifest.Homepage);
            sut.Identifier.Should().Be(manifest.Identifier);
            sut.Installer.Should().BeSameAs(installer);
            sut.License.Should().Be(manifest.License);
            sut.MinimumApplicationVersion.Should().BeSameAs(minimum);
            sut.Name.Should().Be(manifest.Name);
            sut.Repository.Should().Be(manifest.Repository);
            sut.Tags.Should().Equal("camera", "sequencer");
            sut.Version.Should().BeSameAs(version);
            sut.State.Should().Be(PluginState.NotInstalled);
        }

        /// <summary>
        /// Verifies changing plugin UI state raises a precise property notification for plugin list refreshes.
        /// </summary>
        [Test]
        public void ExtendedPluginManifest_StateRaisesPropertyChanged() {
            var sut = new ExtendedPluginManifest(new PluginManifest {
                Name = "Plugin"
            });
            var changed = new List<string>();
            sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            sut.State = PluginState.UpdateAvailable;
            sut.State = PluginState.InstalledAndRequiresRestart;

            sut.State.Should().Be(PluginState.InstalledAndRequiresRestart);
            changed.Should().Equal(nameof(ExtendedPluginManifest.State), nameof(ExtendedPluginManifest.State));
        }
    }
}
