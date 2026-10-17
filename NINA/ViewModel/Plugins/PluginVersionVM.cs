#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Plugin.Interfaces;

namespace NINA.ViewModel.Plugins {

    public sealed class PluginVersionVM {
        public PluginVersionVM(IPluginManifest manifest, PluginEntryVM plugin, bool isCatalogRelease, bool mutationActive) {
            Manifest = manifest;
            VersionText = FormatVersion(manifest.Version);
            MinimumApplicationVersion = FormatVersion(manifest.MinimumApplicationVersion);

            bool hasInstalledVersion = plugin.InstalledManifest?.Version != null;
            int comparison = PluginEntryVM.CompareVersions(manifest.Version, plugin.InstalledManifest?.Version);
            IsInstalled = hasInstalledVersion && comparison == 0;
            bool isPending = (plugin.PendingChange is PluginPendingChange.Install or PluginPendingChange.Update)
                && plugin.PendingManifest?.Version != null
                && PluginEntryVM.CompareVersions(manifest.Version, plugin.PendingManifest.Version) == 0;
            bool canInstall = isCatalogRelease
                && !IsInstalled
                && !mutationActive
                && !plugin.IsBusy
                && !plugin.RequiresRestart;

            if (IsInstalled) {
                StatusText = Loc.Instance["LblPluginManagerCurrentlyInstalled"];
            } else if (isPending) {
                StatusText = Loc.Instance["LblPluginManagerPendingRestart"];
            } else if (hasInstalledVersion && comparison < 0) {
                StatusText = Loc.Instance[canInstall ? "LblPluginManagerRollbackAvailable" : "LblPluginManagerOlderVersion"];
            } else if (hasInstalledVersion && comparison > 0) {
                StatusText = Loc.Instance[canInstall ? "LblPluginManagerUpgradeAvailable" : "LblPluginManagerNewerVersion"];
            } else {
                StatusText = Loc.Instance["LblAvailable"];
            }

            if (plugin.RequiresRestart) {
                ActionText = Loc.Instance["LblPluginManagerPendingRestart"];
            } else if (IsInstalled) {
                ActionText = Loc.Instance["LblPluginManagerCurrentlyInstalled"];
            } else {
                string actionKey = hasInstalledVersion
                    ? comparison < 0 ? "LblPluginManagerRollBackTo" : "LblPluginManagerUpgradeTo"
                    : "LblPluginManagerInstallVersionFormat";
                ActionText = string.Format(Loc.Instance[actionKey], VersionText);
            }
        }

        public IPluginManifest Manifest { get; }

        public string VersionText { get; }

        public string MinimumApplicationVersion { get; }

        public bool IsInstalled { get; }

        public string StatusText { get; }

        public string ActionText { get; }

        private static string FormatVersion(IPluginVersion version) => version == null
            ? null
            : $"{version.Major}.{version.Minor}.{version.Patch}.{version.Build}";
    }
}
