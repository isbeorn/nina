#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.View.Plugins {

    public class PluginImage : System.Windows.Controls.Image {
        private static readonly PluginContentCache sharedCache = new();
        private int loadVersion;

        public static readonly DependencyProperty ImageUrlProperty = DependencyProperty.Register(
            nameof(ImageUrl), typeof(string), typeof(PluginImage), new PropertyMetadata(null, OnImageChanged));

        public static readonly DependencyProperty UseFallbackProperty = DependencyProperty.Register(
            nameof(UseFallback), typeof(bool), typeof(PluginImage), new PropertyMetadata(true, OnImageChanged));

        public static readonly DependencyProperty CacheVersionProperty = DependencyProperty.Register(
            nameof(CacheVersion), typeof(string), typeof(PluginImage), new PropertyMetadata(null, OnImageChanged));

        private static readonly DependencyPropertyKey HasImagePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasImage), typeof(bool), typeof(PluginImage), new PropertyMetadata(false));

        public static readonly DependencyProperty HasImageProperty = HasImagePropertyKey.DependencyProperty;

        public PluginImage() {
            ShowFallback();
        }

        public string ImageUrl {
            get => (string)GetValue(ImageUrlProperty);
            set => SetValue(ImageUrlProperty, value);
        }

        public bool UseFallback {
            get => (bool)GetValue(UseFallbackProperty);
            set => SetValue(UseFallbackProperty, value);
        }

        public string CacheVersion {
            get => (string)GetValue(CacheVersionProperty);
            set => SetValue(CacheVersionProperty, value);
        }

        public bool HasImage => (bool)GetValue(HasImageProperty);

        private static void OnImageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            ((PluginImage)sender).LoadImage();
        }

        private async void LoadImage() {
            int version = ++loadVersion;
            ShowFallback();
            if (string.IsNullOrWhiteSpace(ImageUrl)
                || !Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) {
                return;
            }

            try {
                var pending = await Dispatcher.InvokeAsync(
                    () => version == loadVersion ? sharedCache.LoadImageAsync(uri, CacheVersion) : null,
                    DispatcherPriority.DataBind).Task.ConfigureAwait(false);
                if (pending == null) {
                    return;
                }

                var bitmap = await pending.ConfigureAwait(false);
                await Dispatcher.InvokeAsync(() => {
                    if (version == loadVersion) {
                        Source = bitmap;
                        SetValue(HasImagePropertyKey, true);
                    }
                });
            } catch (Exception) {
                return;
            }
        }

        private void ShowFallback() {
            Source = UseFallback
                ? new BitmapImage(new Uri("pack://application:,,,/NINA;component/Resources/plugin.png", UriKind.Absolute))
                : null;
            SetValue(HasImagePropertyKey, UseFallback);
        }
    }
}
