#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using NINA.ViewModel.Plugins;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Interfaces {

    public interface IPluginsVM {
        IList<ExtendedPluginManifest> AvailablePlugins { get; set; }
        IDictionary<IPluginManifest, bool> InstalledPlugins { get; set; }
        IAsyncCommand FetchPluginsCommand { get; }
        IAsyncCommand InstallPluginCommand { get; }
        IAsyncCommand UninstallPluginCommand { get; }
        ExtendedPluginManifest SelectedAvailablePlugin { get; set; }
        KeyValuePair<IPluginManifest, bool> SelectedInstalledPlugin { get; set; }
    }
}