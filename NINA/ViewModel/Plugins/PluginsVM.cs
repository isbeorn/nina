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
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RelayCommand = NINA.Core.Utility.RelayCommand;

namespace NINA.ViewModel.Plugins {

    public partial class PluginsVM : BaseVM, IPluginsVM {
        private readonly Dictionary<string, PluginEntryVM> entries = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IList<PluginManifest>> catalogSources = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim mutationGate = new(1, 1);
        private readonly SemaphoreSlim catalogGate = new(1, 1);
        private readonly Dispatcher dispatcher;
        private CancellationTokenSource installCts;
        private CancellationTokenSource fetchCts;
        private PluginEntryVM activeEntry;
        private bool rebuilding;
        private bool rebuildPending;
        private bool restoreBrowserSelection;
        private bool firstTime = true;
        private (string SearchText, string[] Tags, string Identifier) availableBrowser;
        private (string SearchText, string[] Tags, string Identifier) installedBrowser;

        private IDictionary<IPluginManifest, bool> installedPlugins = new Dictionary<IPluginManifest, bool>();
        private IList<ExtendedPluginManifest> availablePlugins = new List<ExtendedPluginManifest>();
        private KeyValuePair<IPluginManifest, bool> selectedInstalledPlugin;
        private ExtendedPluginManifest selectedAvailablePlugin;

        public PluginsVM(IPluginLoader pluginProvider, IProfileService profileService) : base(profileService) {
            dispatcher = Application.Current?.Dispatcher;
            SelectedTags.CollectionChanged += (_, _) => RebuildPresentation();
            FetchPluginsCommand = new AsyncCommand<bool>(parameter => {
                if (parameter?.ToString() == "Initial") {
                    if (!firstTime) {
                        return Task.FromResult(false);
                    }

                    firstTime = false;
                }

                return FetchPlugins();
            }, _ => Initialized && !IsFetching);
            InstallPluginCommand = new AsyncCommand<bool>(() => InstallPlugin(false), _ => SelectedPlugin?.CanInstall == true);
            UpdatePluginCommand = new AsyncCommand<bool>(() => InstallPlugin(true), _ => SelectedPlugin?.CanUpdate == true);
            UninstallPluginCommand = new AsyncCommand<bool>(UninstallPlugin, _ => SelectedPlugin?.CanUninstall == true);
            UpdateAllPluginsCommand = new AsyncRelayCommand(UpdateAllPlugins, () => AvailablePluginUpdateCount > 0 && !IsMutating);
            CancelInstallPluginCommand = new RelayCommand(_ => CancelMutation());
            CancelFetchPluginsCommand = new RelayCommand(_ => fetchCts?.Cancel());
            RestartCommand = new RelayCommand(Restart, _ => !IsMutating);
            ClearFiltersCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ClearFilters);
            ShowAvailableCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => {
                SelectedTabIndex = 1;
                ClearFilters();
            });
            RebuildPresentation();
            _ = Initialize(pluginProvider);
        }

        private async Task Initialize(IPluginLoader pluginProvider) {
            try {
                await Task.Run(pluginProvider.Load);
                bool hasInstalledPlugins = false;
                await OnUI(() => {
                    InstalledPlugins = pluginProvider.Plugins;
                    hasInstalledPlugins = InstalledPlugins.Count > 0;
                    Initialized = true;
                });

                if (hasInstalledPlugins) {
                    await FetchPluginsCommand.ExecuteAsync("Initial");
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                await OnUI(() => {
                    CatalogError = ex.Message;
                    Initialized = true;
                });
            }
        }

        [ObservableProperty]
        public partial bool Initialized { get; private set; }

        public IDictionary<IPluginManifest, bool> InstalledPlugins {
            get => installedPlugins;
            set {
                installedPlugins = value;
                OnInstalledPluginsChanged(value);
                RaisePropertyChanged();
            }
        }

        public IList<ExtendedPluginManifest> AvailablePlugins {
            get => availablePlugins;
            set {
                availablePlugins = value;
                OnAvailablePluginsChanged(value);
                RaisePropertyChanged();
            }
        }

        public KeyValuePair<IPluginManifest, bool> SelectedInstalledPlugin {
            get => selectedInstalledPlugin;
            set {
                selectedInstalledPlugin = value;
                OnSelectedInstalledPluginChanged(value);
                RaisePropertyChanged();
            }
        }

        public ExtendedPluginManifest SelectedAvailablePlugin {
            get => selectedAvailablePlugin;
            set {
                selectedAvailablePlugin = value;
                OnSelectedAvailablePluginChanged(value);
                RaisePropertyChanged();
            }
        }

        [ObservableProperty]
        public partial PluginEntryVM SelectedPlugin { get; set; }

        [ObservableProperty]
        public partial IPluginManifest SelectedVersion { get; set; }

        [ObservableProperty]
        public partial int SelectedTabIndex { get; set; }

        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        public ObservableCollection<PluginTag> SelectedTags { get; } = [];

        public string TagFilterText => SelectedTags.Count == 0
            ? Loc.Instance["LblPluginManagerAllTags"]
            : string.Join(", ", SelectedTags.Select(tag => tag.Name));

        [ObservableProperty]
        public partial bool IsFetching { get; private set; }

        [ObservableProperty]
        public partial bool IsMutating { get; private set; }

        [ObservableProperty]
        public partial string CatalogError { get; private set; } = string.Empty;

        [ObservableProperty]
        public partial int AvailablePluginUpdateCount { get; private set; }

        [ObservableProperty]
        public partial bool UpdateAllPluginsRequiresRestart { get; private set; }

        public IReadOnlyList<PluginEntryVM> FilteredPlugins { get; private set; } = Array.Empty<PluginEntryVM>();

        public IReadOnlyList<PluginTag> AvailableTags { get; private set; } = Array.Empty<PluginTag>();

        public bool IsInstalledTab => SelectedTabIndex == 0;

        public IReadOnlyList<IPluginManifest> CompatibleVersions { get; private set; } = Array.Empty<IPluginManifest>();

        public IReadOnlyList<PluginVersionVM> VersionEntries { get; private set; } = Array.Empty<PluginVersionVM>();

        public string VersionActionText => VersionEntries.FirstOrDefault(version => ReferenceEquals(version.Manifest, SelectedVersion))?.ActionText
            ?? Loc.Instance["LblPluginManagerInstallVersion"];

        public bool HasPendingChanges => entries.Values.Any(entry => entry.RequiresRestart);

        public bool HasCatalogError => !string.IsNullOrEmpty(CatalogError);

        public string OperationErrors => string.Join(Environment.NewLine, entries.Values
            .Where(entry => entry.HasOperationError)
            .Select(entry => $"{entry.Name}: {entry.OperationError}"));

        public bool HasResults => FilteredPlugins.Count > 0;

        public string EmptyMessage => IsInstalledTab && !entries.Values.Any(entry => entry.IsInstalled)
            ? Loc.Instance["LblPluginManagerNoInstalled"]
            : !IsInstalledTab && !entries.Values.Any(entry => entry.AvailableManifest != null)
                ? Loc.Instance["LblPluginManagerNoAvailable"]
                : Loc.Instance["LblPluginManagerNoMatches"];

        public string ActivityText => activeEntry?.StatusText
            ?? (IsFetching ? Loc.Instance["LblPluginManagerLoadingCatalog"] : string.Empty);

        public string UpdateAllButtonText => string.Format(Loc.Instance["LblPluginManagerUpdateAllCount"], AvailablePluginUpdateCount);

        public IAsyncCommand FetchPluginsCommand { get; }
        public IAsyncCommand InstallPluginCommand { get; }
        public ICommand CancelFetchPluginsCommand { get; }
        public ICommand CancelInstallPluginCommand { get; }
        public ICommand RestartCommand { get; }
        public IAsyncCommand UpdatePluginCommand { get; }
        public IAsyncRelayCommand UpdateAllPluginsCommand { get; }
        public IAsyncCommand UninstallPluginCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ShowAvailableCommand { get; }

        private void OnInstalledPluginsChanged(IDictionary<IPluginManifest, bool> value) {
            if (rebuilding) {
                return;
            }

            foreach (var entry in entries.Values) {
                entry.InstalledManifest = null;
                entry.LoadSucceeded = false;
            }

            foreach (var installed in value ?? new Dictionary<IPluginManifest, bool>()) {
                var entry = GetEntry(installed.Key.Identifier);
                entry.InstalledManifest = installed.Key;
                entry.LoadSucceeded = installed.Value;
            }

            RebuildPresentation();
        }

        private void OnAvailablePluginsChanged(IList<ExtendedPluginManifest> value) {
            if (rebuilding) {
                return;
            }

            foreach (var entry in entries.Values) {
                entry.AvailableManifest = null;
            }

            foreach (var available in value ?? Array.Empty<ExtendedPluginManifest>()) {
                GetEntry(available.Identifier).AvailableManifest = available;
            }

            RebuildPresentation();
        }

        private void OnSelectedInstalledPluginChanged(KeyValuePair<IPluginManifest, bool> value) {
            if (!rebuilding) {
                SelectedPlugin = value.Key == null ? null : GetEntry(value.Key.Identifier);
            }
        }

        private void OnSelectedAvailablePluginChanged(ExtendedPluginManifest value) {
            if (!rebuilding) {
                SelectedPlugin = value == null ? null : GetEntry(value.Identifier);
            }
        }

        partial void OnSelectedPluginChanged(PluginEntryVM value) {
            bool wasRebuilding = rebuilding;
            rebuilding = true;
            SelectedInstalledPlugin = value?.InstalledManifest == null
                ? default
                : new KeyValuePair<IPluginManifest, bool>(value.InstalledManifest, value.LoadSucceeded);
            SelectedAvailablePlugin = value?.AvailableManifest;
            rebuilding = wasRebuilding;
            RefreshVersions();
            UpdateVersionHistorySelection();
            InvalidateCommands();
        }

        partial void OnSelectedVersionChanged(IPluginManifest value) {
            RaisePropertyChanged(nameof(VersionActionText));
            InstallSelectedVersionCommand.NotifyCanExecuteChanged();
        }

        private void RefreshVersions() {
            var previous = SelectedVersion;
            CompatibleVersions = SelectedPlugin == null
                ? Array.Empty<IPluginManifest>()
                : new IPluginManifest[] { SelectedPlugin.AvailableManifest }
                    .Concat(catalogSources.Values.SelectMany(source => source))
                    .Concat(CachedVersions(SelectedPlugin.Identifier))
                    .Where(manifest => manifest?.Version != null
                        && string.Equals(manifest.Identifier, SelectedPlugin.Identifier, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(manifest => (manifest.Version.Major, manifest.Version.Minor, manifest.Version.Patch, manifest.Version.Build))
                    .OrderByDescending(group => group.Key)
                    .Select(group => group.First())
                    .ToArray();
            IEnumerable<IPluginManifest> displayed = CompatibleVersions;
            if (SelectedPlugin?.InstalledManifest?.Version != null) {
                displayed = displayed.Append(SelectedPlugin.InstalledManifest);
            }

            if (SelectedPlugin?.PendingManifest?.Version != null && SelectedPlugin.PendingChange != PluginPendingChange.Removal) {
                displayed = displayed.Append(SelectedPlugin.PendingManifest);
            }

            VersionEntries = displayed
                .GroupBy(manifest => (manifest.Version.Major, manifest.Version.Minor, manifest.Version.Patch, manifest.Version.Build))
                .OrderByDescending(group => group.Key)
                .Select(group => group.First())
                .Select(manifest => new PluginVersionVM(manifest, SelectedPlugin, CompatibleVersions.Contains(manifest), IsMutating))
                .ToArray();
            RaisePropertyChanged(nameof(CompatibleVersions));
            RaisePropertyChanged(nameof(VersionEntries));
            SelectedVersion = VersionEntries.Select(version => version.Manifest).FirstOrDefault(manifest =>
                string.Equals(manifest.Identifier, previous?.Identifier, StringComparison.OrdinalIgnoreCase)
                && PluginVersionCompare(manifest.Version, previous?.Version) == 0)
                ?? VersionEntries.FirstOrDefault(version => version.IsInstalled)?.Manifest
                ?? VersionEntries.FirstOrDefault()?.Manifest;
            RaisePropertyChanged(nameof(VersionActionText));
        }

        partial void OnSelectedTabIndexChanging(int value) {
            var state = (SearchText, SelectedTags.Select(tag => tag.Name).ToArray(), SelectedPlugin?.Identifier);
            if (IsInstalledTab) {
                installedBrowser = state;
            } else {
                availableBrowser = state;
            }
        }

        partial void OnSelectedTabIndexChanged(int value) {
            var state = IsInstalledTab ? installedBrowser : availableBrowser;
            bool wasRebuilding = rebuilding;
            rebuilding = true;
            try {
                SearchText = state.SearchText ?? string.Empty;
            } finally {
                rebuilding = wasRebuilding;
            }

            restoreBrowserSelection = true;
            RebuildPresentation();
        }

        partial void OnSearchTextChanged(string value) => RebuildPresentation();

        partial void OnIsFetchingChanged(bool value) {
            RaisePropertyChanged(nameof(ActivityText));
            InvalidateCommands();
        }

        partial void OnCatalogErrorChanged(string value) => RaisePropertyChanged(nameof(HasCatalogError));

        private PluginEntryVM GetEntry(string identifier) {
            if (!entries.TryGetValue(identifier, out var entry)) {
                entry = new PluginEntryVM(identifier);
                entries.Add(identifier, entry);
            }

            return entry;
        }

        private void RebuildPresentation() {
            if (rebuilding) {
                rebuildPending = true;
                return;
            }

            rebuilding = true;
            rebuildPending = false;
            try {
                foreach (var entry in entries.Values) {
                    entry.IsInstalledContext = IsInstalledTab;
                    entry.IsMutationBlocked = IsMutating;
                    if (entry.AvailableManifest != null) {
                        entry.AvailableManifest.State = entry.RequiresRestart
                            ? PluginState.InstalledAndRequiresRestart
                            : entry.HasUpdate ? PluginState.UpdateAvailable
                            : entry.IsInstalled ? PluginState.Installed
                            : PluginState.NotInstalled;
                    }
                }

                var candidates = entries.Values.Where(entry => IsInstalledTab ? entry.IsInstalled : entry.AvailableManifest != null).ToList();
                var requestedTags = restoreBrowserSelection
                    ? (IsInstalledTab ? installedBrowser : availableBrowser).Tags ?? []
                    : SelectedTags.Select(tag => tag.Name).ToArray();
                var sectionTagCounts = candidates.SelectMany(entry => GetNormalizedTags(entry.DisplayManifest))
                    .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
                var availableTags = entries.Values.Where(entry => entry.AvailableManifest != null)
                    .SelectMany(entry => GetNormalizedTags(entry.AvailableManifest))
                    .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() >= 2 && sectionTagCounts.ContainsKey(group.Key))
                    .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
                    .Select(group => new PluginTag(group.Key, sectionTagCounts[group.Key])).ToArray();
                if (!AvailableTags.SequenceEqual(availableTags)) {
                    AvailableTags = availableTags;
                    RaisePropertyChanged(nameof(AvailableTags));
                }

                var selectedTags = requestedTags
                    .Select(tag => AvailableTags.FirstOrDefault(candidate => string.Equals(candidate.Name, tag, StringComparison.OrdinalIgnoreCase)))
                    .Where(tag => tag != null)
                    .Distinct()
                    .ToArray();
                if (!SelectedTags.SequenceEqual(selectedTags)) {
                    SelectedTags.Clear();
                    foreach (var tag in selectedTags) {
                        SelectedTags.Add(tag);
                    }
                }

                var query = SearchText?.Trim() ?? string.Empty;
                FilteredPlugins = candidates
                    .Where(entry => SelectedTags.Count == 0 || GetNormalizedTags(entry.DisplayManifest)
                        .Any(tag => SelectedTags.Any(selected => string.Equals(selected.Name, tag, StringComparison.OrdinalIgnoreCase))))
                    .Where(entry => string.IsNullOrEmpty(query)
                        || Contains(entry.Name, query)
                        || Contains(entry.Author, query)
                        || Contains(entry.ShortDescription, query)
                        || (entry.DisplayManifest?.Tags ?? Array.Empty<string>()).Any(tag => Contains(tag, query)))
                    .OrderBy(entry => IsInstalledTab && entry.HasUpdate ? 0 : 1)
                    .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray();
                string selectedIdentifier = restoreBrowserSelection
                    ? (IsInstalledTab ? installedBrowser : availableBrowser).Identifier
                    : SelectedPlugin?.Identifier;
                restoreBrowserSelection = false;
                SelectedPlugin = FilteredPlugins.FirstOrDefault(entry => string.Equals(entry.Identifier, selectedIdentifier, StringComparison.OrdinalIgnoreCase))
                    ?? FilteredPlugins.FirstOrDefault();
                OnSelectedPluginChanged(SelectedPlugin);
                AvailablePluginUpdateCount = entries.Values.Count(entry => entry.HasUpdate);
                UpdateAllPluginsRequiresRestart = HasPendingChanges;
                RaisePropertyChanged(nameof(FilteredPlugins));
                RaisePropertyChanged(nameof(TagFilterText));
                RaisePropertyChanged(nameof(IsInstalledTab));
                RaisePropertyChanged(nameof(HasPendingChanges));
                RaisePropertyChanged(nameof(OperationErrors));
                RaisePropertyChanged(nameof(HasResults));
                RaisePropertyChanged(nameof(EmptyMessage));
                RaisePropertyChanged(nameof(ActivityText));
                RaisePropertyChanged(nameof(UpdateAllButtonText));
                InvalidateCommands();
            } finally {
                rebuilding = false;
            }

            if (rebuildPending) {
                RebuildPresentation();
            }
        }

        private static bool Contains(string value, string query) => value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;

        private static IEnumerable<string> GetNormalizedTags(IPluginManifest manifest) => (manifest?.Tags ?? Array.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        private void ClearFilters() {
            SearchText = string.Empty;
            SelectedTags.Clear();
        }

        private void InvalidateCommands() {
            UpdateAllPluginsCommand?.NotifyCanExecuteChanged();
            InstallSelectedVersionCommand.NotifyCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
        }

        public Task<bool> FetchPlugins() {
            try {
                var repositories = CoreUtil.DeserializeList<string>(Properties.Settings.Default.PluginRepositories);
                if (!repositories.Contains(Constants.MainPluginRepository)) {
                    repositories.Add(Constants.MainPluginRepository);
                }

                return FetchRepositories(repositories.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
            } catch (Exception ex) {
                Logger.Error(ex);
                CatalogError = Loc.Instance["LblPluginManagerCatalogFailed"];
                return Task.FromResult(false);
            }
        }

        private async Task<bool> FetchRepositories(IReadOnlyList<string> repositories) {
            if (!await catalogGate.WaitAsync(0)) {
                return false;
            }

            using var cancellation = new CancellationTokenSource();
            fetchCts = cancellation;
            await OnUI(() => {
                IsFetching = true;
                InvalidateVersionHistory(repositories);
                CatalogError = string.Empty;
            });
            var fetched = new Dictionary<string, IList<PluginManifest>>(StringComparer.OrdinalIgnoreCase);
            int failures = 0;
            try {
                foreach (var repository in repositories) {
                    cancellation.Token.ThrowIfCancellationRequested();
                    try {
                        fetched[repository] = await new PluginFetcher(repository).RequestAll(
                            new PluginVersion(CoreUtil.Version), new Progress<ApplicationStatus>(), cancellation.Token);
                    } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                        throw;
                    } catch (Exception ex) {
                        Logger.Error($"Failed to fetch plugins from {repository}", ex);
                        failures++;
                    }
                }

                cancellation.Token.ThrowIfCancellationRequested();
                await OnUI(() => {
                    if (fetched.Count == 0) {
                        CatalogError = Loc.Instance["LblPluginManagerCatalogFailed"];
                        return;
                    }

                    bool hadSources = catalogSources.Count > 0;
                    foreach (var source in fetched) {
                        catalogSources[source.Key] = source.Value;
                    }

                    foreach (var removed in catalogSources.Keys.Where(source => !repositories.Contains(source, StringComparer.OrdinalIgnoreCase)).ToArray()) {
                        catalogSources.Remove(removed);
                    }

                    IEnumerable<IPluginManifest> manifests = catalogSources.Values.SelectMany(source => source);
                    if (failures > 0 && !hadSources) {
                        manifests = manifests.Concat(AvailablePlugins);
                    }

                    AvailablePlugins = manifests.GroupBy(manifest => manifest.Identifier, StringComparer.OrdinalIgnoreCase)
                        .Select(group => new ExtendedPluginManifest(group.Aggregate((best, current) =>
                            PluginVersionCompare(current.Version, best.Version) > 0 ? current : best)))
                        .OrderBy(manifest => manifest.Name, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();
                    CatalogError = failures > 0 ? Loc.Instance["LblPluginManagerCatalogPartial"] : string.Empty;
                });
                return fetched.Count > 0;
            } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                return false;
            } finally {
                await OnUI(() => {
                    fetchCts = null;
                    IsFetching = false;
                    RebuildPresentation();
                });
                catalogGate.Release();
            }
        }

        private Task<bool> InstallPlugin(bool update) {
            var entry = SelectedPlugin;
            if (entry == null || (update ? !entry.CanUpdate : !entry.CanInstall)) {
                return Task.FromResult(false);
            }

            return MutatePlugins(new[] { (Entry: entry, Manifest: (IPluginManifest)entry.AvailableManifest) },
                update ? PluginActivity.Updating : PluginActivity.Installing);
        }

        private bool CanInstallSelectedVersion() {
            return SelectedPlugin != null
                && SelectedVersion != null
                && CompatibleVersions.Contains(SelectedVersion)
                && !IsMutating
                && !SelectedPlugin.IsBusy
                && !SelectedPlugin.RequiresRestart
                && (SelectedPlugin.InstalledManifest == null
                    || PluginVersionCompare(SelectedVersion.Version, SelectedPlugin.InstalledManifest.Version) != 0);
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand(CanExecute = nameof(CanInstallSelectedVersion))]
        private Task InstallSelectedVersion() {
            if (!CanInstallSelectedVersion()) {
                return Task.CompletedTask;
            }

            var entry = SelectedPlugin;
            var manifest = SelectedVersion;
            return MutatePlugins(new[] { (Entry: entry, Manifest: manifest) },
                entry.InstalledManifest == null ? PluginActivity.Installing : PluginActivity.Updating);
        }

        private Task<bool> UninstallPlugin() {
            var entry = SelectedPlugin;
            if (entry?.CanUninstall != true) {
                return Task.FromResult(false);
            }

            return MutatePlugins(new[] { (Entry: entry, Manifest: entry.InstalledManifest) }, PluginActivity.Removing);
        }

        private Task UpdateAllPlugins() {
            var targets = entries.Values.Where(entry => entry.CanUpdate)
                .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(entry => (Entry: entry, Manifest: (IPluginManifest)entry.AvailableManifest)).ToArray();
            return MutatePlugins(targets, PluginActivity.Updating);
        }

        private async Task<bool> MutatePlugins((PluginEntryVM Entry, IPluginManifest Manifest)[] targets, PluginActivity activity) {
            if (targets.Length == 0 || !await mutationGate.WaitAsync(0)) {
                return false;
            }

            using var cancellation = new CancellationTokenSource();
            bool succeeded = true;
            try {
                await OnUI(() => {
                    installCts = cancellation;
                    IsMutating = true;
                    RebuildPresentation();
                });
                var installer = new PluginInstaller();
                foreach (var target in targets) {
                    cancellation.Token.ThrowIfCancellationRequested();
                    await OnUI(() => {
                        activeEntry = target.Entry;
                        target.Entry.OperationError = string.Empty;
                        target.Entry.Activity = activity;
                        RebuildPresentation();
                    });
                    try {
                        await Task.Run(async () => {
                            if (activity == PluginActivity.Removing) {
                                installer.Uninstall(target.Manifest);
                            } else {
                                await installer.Install(target.Manifest, activity == PluginActivity.Updating, cancellation.Token);
                            }
                        });
                        await OnUI(() => {
                            target.Entry.PendingManifest = target.Manifest;
                            target.Entry.PendingChange = activity switch {
                                PluginActivity.Removing => PluginPendingChange.Removal,
                                PluginActivity.Updating => PluginPendingChange.Update,
                                _ => PluginPendingChange.Install
                            };
                        });
                    } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                        throw;
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        succeeded = false;
                        await OnUI(() => target.Entry.OperationError = ex.Message);
                    } finally {
                        await OnUI(() => {
                            target.Entry.Activity = PluginActivity.None;
                            activeEntry = null;
                            RebuildPresentation();
                        });
                    }
                }
            } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                succeeded = false;
            } finally {
                await OnUI(() => {
                    installCts = null;
                    IsMutating = false;
                    RebuildPresentation();
                });
                mutationGate.Release();
            }

            return succeeded;
        }

        private void CancelMutation() {
            if (installCts == null) {
                return;
            }

            if (activeEntry != null) {
                activeEntry.Activity = PluginActivity.Cancelling;
                RaisePropertyChanged(nameof(ActivityText));
            }

            installCts.Cancel();
        }

        private Task OnUI(Action action) {
            if (dispatcher == null || dispatcher.CheckAccess()) {
                action();
                return Task.CompletedTask;
            }

            return dispatcher.InvokeAsync(action).Task;
        }

        private void Restart(object parameter) {
            profileService.Release();
            Logger.Info("Restarting application for plugin update");
            Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false });
            Application.Current.Shutdown();
        }

        public int PluginVersionCompare(IPluginVersion first, IPluginVersion second) => PluginEntryVM.CompareVersions(first, second);
    }
}
