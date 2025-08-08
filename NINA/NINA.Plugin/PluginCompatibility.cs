#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Newtonsoft.Json;
using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin {
    /// <summary>
    /// This map can be used for known plugin compatibility issues.
    /// A minimum version for the plugin specific version can be specified.
    /// When a plugin is installed with a lower version than specified here, it won't be loaded and shown as an error.
    /// This is necessary due to offline installations not having any information about the latest online plugin manifests
    /// </summary>
    internal class PluginCompatibilityMap {
        public PluginCompatibilityMap() {
            CompatibilityMap = new Dictionary<string, PluginCompatibility>() {
                { "Orbuculum", new PluginCompatibility { Name = "Orbuculum", Identifier = "a6c614f3-c3ab-423a-8be1-d480a253f07c", MinimumVersion = "1.0.4.0" } },
                { "ExoPlanets", new PluginCompatibility { Name = "ExoPlanets", Identifier = "6d0e07f2-8773-4229-dc2b-f451e53c677f", MinimumVersion = "1.1.4.0" } },
                { "DIY Meridian Flip", new PluginCompatibility { Name = "DIY Meridian Flip", Identifier = "38367705-3489-4528-9b4a-c7765bc0eced", MinimumVersion = "1.1.1.0" } },
                { "Smart Meridian Flip", new PluginCompatibility { Name = "Smart Meridian Flip", Identifier = "6d0e07f2-8773-4229-bf2c-f451e53f677a", MinimumVersion = "1.0.1.3" } },
                { "Astro-Physics Tools", new PluginCompatibility { Name = "Astro-Physics Tools", Identifier = "99688A5D-BD28-4D8D-80D5-3D4192BB987D", MinimumVersion = "0.5.0.0" } },
                { "Ground Station", new PluginCompatibility { Name = "Ground Station", Identifier = "2737AFDF-A1AA-48C3-BE17-0F5F03282AEB", MinimumVersion = "1.11.0.0" } },
                { "Moon Angle", new PluginCompatibility { Name = "Moon Angle", Identifier = "036af399-91b0-4a29-a7d3-44af0bfde13e", MinimumVersion = "1.3.0.0" } },
                { "Scope Control", new PluginCompatibility { Name = "Scope Control", Identifier = "0bcbb707-6611-4266-9686-231be457f069", MinimumVersion = "1.2.2.0" } },
                { "Framing Cache Generator", new PluginCompatibility { Name = "Framing Cache Generator", Identifier = "b71ce4a9-17bd-4152-a372-e9e6e127ddfb", MinimumVersion = "65535.0.0.0" } }
            };
        }
        private Dictionary<string, PluginCompatibility> CompatibilityMap { get; }

        public readonly Version MinimumMajorVersion = GetPluginMinimumApplicationVersion();

        public readonly Version  DeprecatedVersion = new Version(65535, 0, 0, 0);

        public bool IsCompatible(IPluginManifest plugin) {

            if (IsNotCompatible(plugin)) {
                return false;
            }

            var version = new Version(plugin.Version.Major, plugin.Version.Minor, plugin.Version.Patch, plugin.Version.Build);
            if (CompatibilityMap.TryGetValue(plugin.Identifier, out var compatibility)) {
                var minimumCompatibleVersion = new Version(compatibility.MinimumVersion);
                if (version < minimumCompatibleVersion) {
                    return false;
                }
            }
            return true;
        }

        public Version GetMinimumVersion(IPluginManifest plugin) {
            if (CompatibilityMap.TryGetValue(plugin.Identifier, out var compatibility)) {
                return new Version(compatibility.MinimumVersion);
            }
            return new Version("0.0.0.0");
        }

        internal class PluginCompatibility {
            [JsonProperty(propertyName: "identifier")]
            public string Identifier { get; set; }
            [JsonProperty(propertyName: "name")]
            public string Name { get; set; }
            [JsonProperty(propertyName: "minimumVersion")]
            public string MinimumVersion { get; set; }
        }

        internal bool IsDeprecated(IPluginManifest plugin) {
            var version = new Version(plugin.Version.Major, plugin.Version.Minor, plugin.Version.Patch, plugin.Version.Build);
            return version >= DeprecatedVersion;
            
        }

        internal bool IsNotCompatible(IPluginManifest plugin) {
            var version = new Version(plugin.MinimumApplicationVersion.Major, plugin.MinimumApplicationVersion.Minor, plugin.MinimumApplicationVersion.Patch, plugin.MinimumApplicationVersion.Build);
            return version < MinimumMajorVersion;
        }

        internal bool IsUpdateRequired(IPluginManifest plugin) {
            var version = new Version(plugin.Version.Major, plugin.Version.Minor, plugin.Version.Patch, plugin.Version.Build);
            return version <= GetMinimumVersion(plugin);
        }

        private static Version GetPluginMinimumApplicationVersion() {
            var assembly = typeof(PluginCompatibilityMap).Assembly;
            var attribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "PluginMinimumApplicationVersion");
            return new Version(attribute?.Value ?? "0.0.0.0") ;
        }
    }
}
