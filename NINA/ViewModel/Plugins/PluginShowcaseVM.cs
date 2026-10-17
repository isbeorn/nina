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
using NINA.Core.Utility;
using NINA.View.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.ViewModel.Plugins {

    internal partial class PluginShowcaseVM : BaseINPC {
        private readonly PluginContentCache cache = new();
        private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private Uri[] urls = [];
        private string identifier;
        private string version;
        private int generation;
        private bool active;
        private bool attempted;

        [ObservableProperty]
        public partial IReadOnlyList<ImageSource> Images { get; private set; } = [];

        [ObservableProperty]
        public partial bool IsLoading { get; private set; }

        [ObservableProperty]
        public partial bool IsEmpty { get; private set; } = true;

        public void Select(string pluginIdentifier, IEnumerable<string> imageUrls, string displayedVersion) {
            var requestedUrls = imageUrls.Select(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) ? uri : null)
                .Where(uri => uri != null)
                .Distinct()
                .ToArray();
            if (identifier == pluginIdentifier && version == displayedVersion && urls.SequenceEqual(requestedUrls)) {
                return;
            }

            generation++;
            identifier = pluginIdentifier;
            version = displayedVersion;
            urls = requestedUrls;
            Images = [];
            IsLoading = false;
            IsEmpty = true;
            attempted = false;
            if (active) {
                _ = LoadAsync();
            }
        }

        public void SetActive(bool value) {
            active = value;
            if (active && !attempted) {
                _ = LoadAsync();
            }
        }

        private async Task LoadAsync() {
            attempted = true;
            if (urls.Length == 0) {
                return;
            }

            int request = generation;
            string requestedVersion = version;
            IsLoading = true;
            IsEmpty = false;
            var images = await Task.WhenAll(urls.Select(async url => {
                try {
                    return (ImageSource)await cache.LoadImageAsync(url, requestedVersion).ConfigureAwait(false);
                } catch (Exception ex) {
                    Logger.Debug($"Unable to load plugin screenshot: {ex.Message}");
                    return null;
                }
            })).ConfigureAwait(false);

            await dispatcher.InvokeAsync(() => {
                if (request == generation) {
                    Images = images.Where(image => image != null).ToArray();
                    IsLoading = false;
                    IsEmpty = Images.Count == 0;
                }
            });
        }
    }
}
