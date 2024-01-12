#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Plugin.ManifestDefinition;

namespace NINA.Plugin.Interfaces {

    public interface IPluginInstallerDetails {

        /// <summary>
        /// The url where the plugin can be downloaded.
        /// </summary>
        string URL { get; }

        /// <summary>
        /// The type of installer, for the application to determine how the plugin can be installed.
        /// </summary>
        InstallerType Type { get; }

        /// <summary>
        /// The checksum of the installer file.
        /// </summary>
        string Checksum { get; }

        /// <summary>
        /// The type of the checksum.
        /// </summary>
        InstallerChecksum ChecksumType { get; }
    }
}