#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin.Interfaces {

    /// <summary>
    /// The manifest definition of a plugin, describing the plugin details.
    /// </summary>
    public interface IPluginManifest {

        /// <summary>
        /// A unique identifier for the plugin.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// The name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The license that the plugin code is using.
        /// </summary>
        string License { get; }

        /// <summary>
        /// The URL to the license that the code is using.
        /// </summary>
        string LicenseURL { get; }

        /// <summary>
        /// Author of the plugin.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Homepage URL of the plugin.
        /// </summary>
        string Homepage { get; }

        /// <summary>
        /// Repository URL where the plugin code is hosted.
        /// </summary>
        string Repository { get; }

        /// <summary>
        /// A link to the list of changes for each version of the plugin.
        /// </summary>
        string ChangelogURL { get; }

        /// <summary>
        /// Short tags to find the plugin searching by tags.
        /// </summary>
        string[] Tags { get; }

        /// <summary>
        /// The plugin's version.
        /// </summary>
        IPluginVersion Version { get; }

        /// <summary>
        /// Defines the minimum application version this plugin is compatible with.
        /// </summary>
        IPluginVersion MinimumApplicationVersion { get; }

        /// <summary>
        /// Defines the plugin installer location, its checksum information and installer type to identify how it should be deployed.
        /// </summary>
        IPluginInstallerDetails Installer { get; }

        /// <summary>
        /// Describe the plugin in detail to give an overview about its capabilities.
        /// </summary>
        IPluginDescription Descriptions { get; }

        Task Initialize();

        Task Teardown();
    }
}