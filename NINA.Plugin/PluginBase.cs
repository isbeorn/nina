#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin {

    /// <summary>
    /// Base class to inherit from when developing a plugin for NINA.
    /// This class will set all relevant meta data info based on the plugin assembly attributes
    /// </summary>
    public class PluginBase : IPluginManifest {

        public string Identifier {
            get {
                var assembly = this.GetType().Assembly;
                return assembly.GetCustomAttribute<GuidAttribute>().Value;
            }
        }

        public IPluginVersion Version {
            get {
                var assembly = this.GetType().Assembly;
                return new PluginVersion(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "1.0.0.0");
            }
        }

        public string Name {
            get {
                var assembly = this.GetType().Assembly;
                return assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? string.Empty;
            }
        }

        public string License => GetCustomMetadata("License");

        public string LicenseURL => GetCustomMetadata("LicenseURL");

        public string Author {
            get {
                var assembly = this.GetType().Assembly;
                return assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;
            }
        }

        public string Homepage => GetCustomMetadata("Homepage");

        public string Repository => GetCustomMetadata("Repository");

        public string ChangelogURL => GetCustomMetadata("ChangelogURL");

        public string[] Tags {
            get {
                var assembly = this.GetType().Assembly;
                var tags = GetCustomMetadata("Tags");
                if (string.IsNullOrEmpty(tags)) {
                    return new string[0];
                } else {
                    return tags.Split(',');
                }
            }
        }

        public IPluginVersion MinimumApplicationVersion {
            get {
                var assembly = this.GetType().Assembly;
                var minVersion = GetCustomMetadata("MinimumApplicationVersion");
                var version = new PluginVersion(string.IsNullOrEmpty(minVersion) ? "1.11.0.0" : minVersion);
                return version;
            }
        }

        public IPluginInstallerDetails Installer => new PluginInstallerDetails() {
            // The installer details are irrelevant in this scope and have to be filled out by the plugin maintainer
            // Things like the download URL and the checksum will change each new Version anyways
            URL = string.Empty,
            Checksum = string.Empty
        };

        public IPluginDescription Descriptions {
            get {
                var assembly = this.GetType().Assembly;
                var shortDescription = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;
                var longDescription = GetCustomMetadata("LongDescription");
                var featuredImageURL = GetCustomMetadata("FeaturedImageURL");
                var screenshotURL = GetCustomMetadata("ScreenshotURL");
                var altScreenshotURL = GetCustomMetadata("AltScreenshotURL");
                return new PluginDescription() {
                    ShortDescription = shortDescription,
                    LongDescription = longDescription,
                    FeaturedImageURL = featuredImageURL,
                    ScreenshotURL = screenshotURL,
                    AltScreenshotURL = altScreenshotURL
                };
            }
        }

        public virtual Task Initialize() {
            return Task.CompletedTask;
        }

        public virtual Task Teardown() {
            return Task.CompletedTask;
        }

        private string GetCustomMetadata(string key) {
            var assembly = this.GetType().Assembly;
            var attribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == key);
            return attribute?.Value ?? string.Empty;
        }
    }
}