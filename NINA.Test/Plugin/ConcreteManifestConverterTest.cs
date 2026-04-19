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
using Newtonsoft.Json;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NUnit.Framework;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class ConcreteManifestConverterTest {

        /// <summary>
        /// Verifies that interface-backed manifest properties serialize through their concrete
        /// converter and can be read back into the same concrete plugin version shape.
        /// </summary>
        [Test]
        public void Converter_WritesAndReadsConcreteManifestProperty() {
            var manifest = new PluginManifest {
                Name = "Converter Fixture",
                Identifier = "63a6fe09-6e9a-4524-b258-91e7efbb4cda",
                Version = new PluginVersion("2.4.6.8"),
                Author = "NINA Test Observatory",
                Repository = "https://example.invalid/converter-fixture.git",
                License = "MPL-2.0",
                LicenseURL = "https://www.mozilla.org/en-US/MPL/2.0/",
                MinimumApplicationVersion = new PluginVersion("3.0.0.0"),
                Descriptions = new PluginDescription {
                    ShortDescription = "Converter fixture"
                },
                Installer = new PluginInstallerDetails {
                    URL = "https://example.invalid/converter-fixture.dll",
                    Type = InstallerType.DLL,
                    Checksum = new string('a', 64),
                    ChecksumType = InstallerChecksum.SHA256
                }
            };

            string json = JsonConvert.SerializeObject(manifest);
            PluginManifest roundTrip = JsonConvert.DeserializeObject<PluginManifest>(json);

            json.Should().Contain("\"Major\":2");
            roundTrip.Version.Should().BeOfType<PluginVersion>();
            roundTrip.Version.ToString().Should().Be("2.4.6.8");
        }

        /// <summary>
        /// Verifies that the generic manifest converter advertises support for interface-shaped
        /// properties because manifest JSON stores interface contracts with concrete values.
        /// </summary>
        [Test]
        public void CanConvert_ReturnsTrueForInterfacePropertyTypes() {
            var converter = new ConcreteManifestConverter<PluginVersion>();

            converter.CanConvert(typeof(IPluginVersion)).Should().BeTrue();
        }
    }
}
