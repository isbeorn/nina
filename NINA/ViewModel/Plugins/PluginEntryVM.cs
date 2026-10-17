#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using System;

namespace NINA.ViewModel.Plugins {

    public enum PluginPendingChange {
        None,
        Install,
        Update,
        Removal
    }

    public enum PluginActivity {
        None,
        Installing,
        Updating,
        Removing,
        Cancelling
    }

    public partial class PluginEntryVM : BaseINPC {
        public PluginEntryVM(string identifier) {
            Identifier = identifier;
        }

        public string Identifier { get; }

        [ObservableProperty]
        public partial IPluginManifest InstalledManifest { get; internal set; }

        [ObservableProperty]
        public partial ExtendedPluginManifest AvailableManifest { get; internal set; }

        [ObservableProperty]
        public partial bool LoadSucceeded { get; internal set; }

        [ObservableProperty]
        public partial PluginPendingChange PendingChange { get; internal set; }

        [ObservableProperty]
        public partial PluginActivity Activity { get; internal set; }

        [ObservableProperty]
        public partial string OperationError { get; internal set; } = string.Empty;

        [ObservableProperty]
        internal partial bool IsInstalledContext { get; set; }

        [ObservableProperty]
        internal partial bool IsMutationBlocked { get; set; }

        [ObservableProperty]
        internal partial IPluginManifest PendingManifest { get; set; }

        public IPluginManifest DisplayManifest => IsInstalledContext
            ? InstalledManifest ?? PendingManifest ?? AvailableManifest
            : AvailableManifest ?? InstalledManifest ?? PendingManifest;

        public IPluginManifest SettingsManifest => CanShowSettings ? InstalledManifest : null;

        public string Name => DisplayManifest?.Name ?? Identifier;

        public string Author => DisplayManifest?.Author;

        public string ShortDescription => DisplayManifest?.Descriptions?.ShortDescription;

        public string InstalledVersion => InstalledManifest?.Version?.ToString();

        public string AvailableVersion => AvailableManifest?.Version?.ToString();

        public string LoadError => InstalledManifest != null && !LoadSucceeded
            ? InstalledManifest.Descriptions?.LongDescription ?? Loc.Instance["LblPluginManagerFailedToLoad"]
            : string.Empty;

        public bool HasLoadError => InstalledManifest != null && !LoadSucceeded;

        public bool HasOperationError => !string.IsNullOrEmpty(OperationError);

        public bool IsBusy => Activity != PluginActivity.None;

        public bool RequiresRestart => PendingChange != PluginPendingChange.None;

        public bool IsInstalled => InstalledManifest != null || RequiresRestart;

        public bool ShowInstalledMarker => !IsInstalledContext && IsInstalled && !HasUpdate;

        public string ListStatusText => IsBusy || RequiresRestart || HasLoadError ? StatusText : string.Empty;

        public bool HasUpdate => InstalledManifest != null
            && AvailableManifest != null
            && !RequiresRestart
            && CompareVersions(AvailableManifest.Version, InstalledManifest.Version) > 0;

        public bool CanInstall => !IsMutationBlocked && !IsBusy && !IsInstalled && AvailableManifest != null;

        public bool CanUpdate => !IsMutationBlocked && !IsBusy && HasUpdate;

        public bool CanUninstall => !IsMutationBlocked && !IsBusy && InstalledManifest != null && !RequiresRestart;

        public bool CanShowSettings => InstalledManifest != null && LoadSucceeded && PendingChange != PluginPendingChange.Removal;

        public string StatusText {
            get {
                if (IsBusy) {
                    return Loc.Instance[Activity switch {
                        PluginActivity.Installing => "LblPluginManagerInstalling",
                        PluginActivity.Updating => "LblPluginManagerUpdating",
                        PluginActivity.Removing => "LblPluginManagerRemoving",
                        _ => "LblPluginManagerCancelling"
                    }];
                }

                if (PendingChange == PluginPendingChange.Removal) {
                    return Loc.Instance["LblPluginManagerRemovalPending"];
                }

                if (RequiresRestart) {
                    return Loc.Instance["LblPluginManagerPendingRestart"];
                }

                if (HasLoadError) {
                    return Loc.Instance["LblPluginManagerFailedToLoad"];
                }

                if (HasUpdate) {
                    return Loc.Instance["LblPluginManagerUpdateAvailable"];
                }

                return IsInstalled ? Loc.Instance["LblInstalled"] : string.Empty;
            }
        }

        partial void OnInstalledManifestChanged(IPluginManifest value) => RaiseAllPropertiesChanged();

        partial void OnAvailableManifestChanged(ExtendedPluginManifest value) => RaiseAllPropertiesChanged();

        partial void OnLoadSucceededChanged(bool value) => RaiseAllPropertiesChanged();

        partial void OnPendingChangeChanged(PluginPendingChange value) => RaiseAllPropertiesChanged();

        partial void OnActivityChanged(PluginActivity value) => RaiseAllPropertiesChanged();

        partial void OnOperationErrorChanged(string value) => RaiseAllPropertiesChanged();

        partial void OnIsInstalledContextChanged(bool value) => RaiseAllPropertiesChanged();

        partial void OnIsMutationBlockedChanged(bool value) => RaiseAllPropertiesChanged();

        partial void OnPendingManifestChanged(IPluginManifest value) => RaiseAllPropertiesChanged();

        internal static int CompareVersions(IPluginVersion first, IPluginVersion second) {
            if (first == null) {
                return second == null ? 0 : -1;
            }

            if (second == null) {
                return 1;
            }

            return (first.Major, first.Minor, first.Patch, first.Build)
                .CompareTo((second.Major, second.Minor, second.Patch, second.Build));
        }
    }
}
