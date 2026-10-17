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
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Plugins {

    public partial class PluginsVM {
        private readonly Dictionary<string, Dictionary<string, VersionHistory>> historySources = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<string> historyRepositories = Array.Empty<string>();
        private CancellationTokenSource historyCts;
        private string historyIdentifier;

        [ObservableProperty]
        public partial bool IsVersionHistoryVisible { get; set; }

        [ObservableProperty]
        public partial bool IsLoadingVersions { get; private set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasVersionHistoryError))]
        public partial string VersionHistoryError { get; private set; } = string.Empty;

        public bool HasVersionHistoryError => !string.IsNullOrEmpty(VersionHistoryError);

        partial void OnIsVersionHistoryVisibleChanged(bool value) {
            if (value) {
                _ = LoadVersionHistory();
            } else {
                CancelVersionHistory();
            }
        }

        private VersionHistory GetVersionHistory(string repository, string identifier) {
            if (!historySources.TryGetValue(repository, out var plugins)) {
                plugins = new Dictionary<string, VersionHistory>(StringComparer.OrdinalIgnoreCase);
                historySources.Add(repository, plugins);
            }

            if (!plugins.TryGetValue(identifier, out var history)) {
                history = new VersionHistory();
                plugins.Add(identifier, history);
            }

            return history;
        }

        private IEnumerable<IPluginManifest> CachedVersions(string identifier) {
            return historySources.Values
                .Where(plugins => plugins.ContainsKey(identifier))
                .SelectMany(plugins => plugins[identifier].Manifests);
        }

        private void UpdateVersionHistorySelection() {
            if (!string.Equals(historyIdentifier, SelectedPlugin?.Identifier, StringComparison.OrdinalIgnoreCase)) {
                CancelVersionHistory();
                historyIdentifier = SelectedPlugin?.Identifier;
                VersionHistoryError = string.Empty;
            }

            if (IsVersionHistoryVisible) {
                _ = LoadVersionHistory();
            }
        }

        private void CancelVersionHistory() {
            var cancellation = historyCts;
            historyCts = null;
            cancellation?.Cancel();
            IsLoadingVersions = false;
        }

        private void InvalidateVersionHistory(IReadOnlyList<string> repositories) {
            CancelVersionHistory();
            historyRepositories = repositories;
            foreach (var removed in historySources.Keys.Where(source => !repositories.Contains(source, StringComparer.OrdinalIgnoreCase)).ToArray()) {
                historySources.Remove(removed);
            }

            foreach (var history in historySources.Values.SelectMany(plugins => plugins.Values)) {
                history.Attempted = false;
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task LoadVersionHistory() {
            var identifier = SelectedPlugin?.Identifier;
            if (!IsVersionHistoryVisible || identifier == null || IsFetching || historyCts != null) {
                return;
            }

            var sources = historyRepositories
                .Select(repository => (Repository: repository, History: GetVersionHistory(repository, identifier)))
                .ToArray();
            VersionHistoryError = sources.Select(source => source.History.Error).FirstOrDefault(error => !string.IsNullOrEmpty(error)) ?? string.Empty;
            var uncheckedSources = sources.Where(source => !source.History.Attempted).ToArray();
            if (uncheckedSources.Length == 0) {
                return;
            }

            using var cancellation = new CancellationTokenSource();
            historyCts = cancellation;
            IsLoadingVersions = true;
            VersionHistoryError = string.Empty;
            try {
                foreach (var source in uncheckedSources) {
                    IList<PluginManifest> manifests = null;
                    string error = string.Empty;
                    try {
                        manifests = await new PluginFetcher(source.Repository).RequestVersions(identifier,
                            new PluginVersion(CoreUtil.Version), cancellation.Token);
                    } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                        throw;
                    } catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed) {
                        error = Loc.Instance["LblPluginManagerHistoryUnsupported"];
                    } catch (Exception ex) {
                        Logger.Error($"Failed to fetch plugin versions from {source.Repository}", ex);
                        error = Loc.Instance["LblPluginManagerHistoryFailed"];
                    }

                    cancellation.Token.ThrowIfCancellationRequested();
                    await OnUI(() => {
                        if (!ReferenceEquals(historyCts, cancellation)) {
                            return;
                        }

                        source.History.Attempted = true;
                        source.History.Error = error;
                        if (manifests != null) {
                            source.History.Manifests = manifests;
                        }

                        RefreshVersions();
                    });
                }
            } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
            } finally {
                await OnUI(() => {
                    if (ReferenceEquals(historyCts, cancellation)) {
                        historyCts = null;
                        IsLoadingVersions = false;
                        VersionHistoryError = sources.Select(source => source.History.Error).FirstOrDefault(error => !string.IsNullOrEmpty(error)) ?? string.Empty;
                    }
                });
            }
        }

        [RelayCommand]
        private Task RetryVersionHistory() {
            CancelVersionHistory();
            if (SelectedPlugin != null) {
                foreach (var plugins in historySources.Values) {
                    if (plugins.TryGetValue(SelectedPlugin.Identifier, out var history)) {
                        history.Attempted = false;
                    }
                }
            }

            return LoadVersionHistory();
        }

        private sealed class VersionHistory {
            public IList<PluginManifest> Manifests { get; set; } = Array.Empty<PluginManifest>();

            public bool Attempted { get; set; }

            public string Error { get; set; } = string.Empty;
        }
    }
}
