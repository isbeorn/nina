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
using NINA.View.Plugins;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Plugins {

    internal partial class PluginChangelogVM : BaseINPC {
        private readonly PluginContentCache cache = new();
        private CancellationTokenSource download;
        private Uri uri;
        private string version;
        private bool active;
        private bool attempted;

        public Uri DocumentUri { get; private set; }

        [ObservableProperty]
        public partial string Markdown { get; private set; } = string.Empty;

        [ObservableProperty]
        public partial string AssetPathRoot { get; private set; }

        [ObservableProperty]
        public partial string BrowserUrl { get; private set; }

        [ObservableProperty]
        public partial string Notice { get; private set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsLoading { get; private set; }

        [ObservableProperty]
        public partial bool CanRetry { get; private set; }

        public void Select(string url, string displayedVersion) {
            if (BrowserUrl == url && version == displayedVersion) {
                return;
            }

            Cancel();
            BrowserUrl = url;
            version = displayedVersion;
            uri = Uri.TryCreate(url, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps) ? parsed : null;
            attempted = false;
            Markdown = string.Empty;
            DocumentUri = null;
            AssetPathRoot = null;
            Notice = string.Empty;
            CanRetry = false;
        }

        public void SetActive(bool value) {
            active = value;
            if (!value) {
                Cancel();
                attempted = false;
            } else if (!attempted && !IsLoading) {
                _ = LoadAsync();
            }
        }

        [RelayCommand]
        private Task Retry() => LoadAsync();

        private async Task LoadAsync() {
            if (!active) {
                return;
            }

            Cancel();
            if (uri == null) {
                Notice = Loc.Instance["LblPluginManagerChangelogUnsupported"];
                attempted = true;
                return;
            }

            var request = new CancellationTokenSource();
            download = request;
            var cached = cache.ReadChangelog(uri, version);
            if (cached != null) {
                Show(cached);
            }

            Notice = string.Empty;
            CanRetry = false;
            attempted = true;
            if (cached?.IsFresh == true) {
                download = null;
                request.Dispose();
                return;
            }

            IsLoading = true;
            try {
                var document = await cache.FetchChangelogAsync(uri, version, request.Token);
                if (download != request || !active) {
                    return;
                }

                if (document != null) {
                    Show(document);
                } else {
                    Notice = Loc.Instance[cached != null ? "LblPluginManagerChangelogStale" : "LblPluginManagerChangelogUnsupported"];
                    CanRetry = true;
                }
            } catch (OperationCanceledException) when (request.IsCancellationRequested) {
            } catch (Exception ex) {
                if (download == request && active) {
                    Logger.Debug($"Unable to load plugin changelog: {ex.Message}");
                    Notice = Loc.Instance[cached != null ? "LblPluginManagerChangelogStale" : "LblPluginManagerChangelogFailed"];
                    CanRetry = true;
                }
            } finally {
                if (download == request) {
                    download = null;
                    IsLoading = false;
                }

                request.Dispose();
            }
        }

        private void Show(PluginContentCache.ChangelogDocument document) {
            DocumentUri = document.DocumentUri;
            AssetPathRoot = new Uri(document.DocumentUri, ".").AbsoluteUri;
            Markdown = document.Markdown;
        }

        private void Cancel() {
            if (download != null) {
                download.Cancel();
                download = null;
                attempted = false;
            }

            IsLoading = false;
        }
    }
}
