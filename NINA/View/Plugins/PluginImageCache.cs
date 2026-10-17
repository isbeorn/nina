#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.View.Plugins {

    internal sealed class PluginImageCache {
        private static readonly TimeSpan lifetime = TimeSpan.FromDays(7);
        private readonly string cacheDirectory;
        private readonly Dictionary<string, Task<BitmapSource>> downloads = new();

        public PluginImageCache(string cacheDirectory = null) {
            this.cacheDirectory = cacheDirectory;
        }

        public Task<BitmapSource> LoadAsync(Uri uri, string version = null) {
            string identity = string.IsNullOrEmpty(version) ? uri.AbsoluteUri : uri.AbsoluteUri + "\n" + version;
            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            string directory = cacheDirectory ?? Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Cache", "PluginIcons");
            string path = Path.Combine(directory, key + ".png");
            lock (downloads) {
                if (!downloads.TryGetValue(path, out var download)) {
                    download = Task.Run(() => LoadCoreAsync(uri, path));
                    downloads.Add(path, download);
                }

                return download;
            }
        }

        private async Task<BitmapSource> LoadCoreAsync(Uri uri, string path) {
            try {
                BitmapSource cached = null;
                try {
                    if (File.Exists(path)) {
                        DateTime lastWrite = File.GetLastWriteTimeUtc(path);
                        using var stream = File.OpenRead(path);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        cached = bitmap;
                        TimeSpan age = DateTime.UtcNow - lastWrite;
                        if (age >= TimeSpan.Zero && age < lifetime) {
                            return cached;
                        }
                    }
                } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or FileFormatException) {
                    Logger.Debug($"Unable to read cached plugin image: {ex.Message}");
                }

                BitmapSource image;
                try {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    image = await new HttpDownloadImageRequest(uri.AbsoluteUri).Request(timeout.Token).ConfigureAwait(false);
                } catch (Exception) when (cached != null) {
                    return cached;
                }

                SaveImage(path, image);
                return image;
            } finally {
                lock (downloads) {
                    downloads.Remove(path);
                }
            }
        }

        private static void SaveImage(string path, BitmapSource image) {
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(stream);
                }

                File.Move(temporaryPath, path, true);
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                Logger.Debug($"Unable to save cached plugin image: {ex.Message}");
            } finally {
                try {
                    File.Delete(temporaryPath);
                } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                    Logger.Debug($"Unable to remove temporary plugin image: {ex.Message}");
                }
            }
        }
    }
}
