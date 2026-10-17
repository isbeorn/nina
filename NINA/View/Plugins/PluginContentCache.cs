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
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.View.Plugins {

    internal sealed class PluginContentCache {
        private static readonly TimeSpan lifetime = TimeSpan.FromDays(7);
        private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(30);
        private readonly string cacheDirectory;
        private readonly Dictionary<string, Task<BitmapSource>> downloads = new();

        public PluginContentCache(string cacheDirectory = null) {
            this.cacheDirectory = cacheDirectory;
        }

        internal sealed record ChangelogDocument(string Markdown, Uri DocumentUri) {
            public bool IsFresh { get; init; }
        }

        public Task<BitmapSource> LoadImageAsync(Uri uri, string version = null) {
            string path = GetPath(uri, string.IsNullOrEmpty(version) ? null : version, "PluginIcons", ".png");
            lock (downloads) {
                if (!downloads.TryGetValue(path, out var download)) {
                    download = Task.Run(() => LoadImageCoreAsync(uri, path));
                    downloads.Add(path, download);
                }

                return download;
            }
        }

        public ChangelogDocument ReadChangelog(Uri uri, string version) {
            var document = Read(GetChangelogPath(uri, version), DecodeChangelog, out bool isFresh);
            return document == null ? null : document with { IsFresh = isFresh };
        }

        public async Task<ChangelogDocument> FetchChangelogAsync(Uri uri, string version, CancellationToken cancellationToken) {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(requestTimeout);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreUtil.UserAgent);
            using var response = await client.GetAsync(NormalizeChangelogUri(uri), timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string markdown = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            string mediaType = response.Content.Headers.ContentType?.MediaType;
            var documentUri = response.RequestMessage?.RequestUri ?? NormalizeChangelogUri(uri);
            bool supported = mediaType is "text/plain" or "text/markdown" or "text/x-markdown"
                || (string.IsNullOrEmpty(mediaType) || mediaType == "application/octet-stream") && HasMarkdownExtension(documentUri);
            if (!supported || !IsMarkdown(markdown)) {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var document = new ChangelogDocument(markdown, documentUri) { IsFresh = true };
            Save(GetChangelogPath(uri, version), stream => JsonSerializer.Serialize(stream, document));
            return document;
        }

        internal static Uri NormalizeChangelogUri(Uri uri) {
            if (!HasMarkdownExtension(uri)) {
                return uri;
            }

            string[] segments = uri.AbsolutePath.Split('/');
            if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                && segments.Length >= 6 && segments[3] == "blob") {
                return new UriBuilder(uri) {
                    Host = "raw.githubusercontent.com",
                    Path = "/" + string.Join("/", segments[1], segments[2], string.Join("/", segments[4..]))
                }.Uri;
            }

            if (uri.Host.Equals("bitbucket.org", StringComparison.OrdinalIgnoreCase)
                && segments.Length >= 6 && segments[3] == "src") {
                segments[3] = "raw";
                return new UriBuilder(uri) { Path = string.Join("/", segments) }.Uri;
            }

            return uri;
        }

        private async Task<BitmapSource> LoadImageCoreAsync(Uri uri, string path) {
            try {
                var cached = Read(path, DecodeImage, out bool isFresh);
                if (isFresh) {
                    return cached;
                }

                BitmapSource image;
                try {
                    using var timeout = new CancellationTokenSource(requestTimeout);
                    image = await new HttpDownloadImageRequest(uri.AbsoluteUri).Request(timeout.Token).ConfigureAwait(false);
                } catch (Exception) when (cached != null) {
                    return cached;
                }

                Save(path, stream => EncodeImage(stream, image));
                return image;
            } finally {
                lock (downloads) {
                    downloads.Remove(path);
                }
            }
        }

        private static BitmapSource DecodeImage(Stream stream) {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static void EncodeImage(Stream stream, BitmapSource image) {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
        }

        private static ChangelogDocument DecodeChangelog(Stream stream) {
            var document = JsonSerializer.Deserialize<ChangelogDocument>(stream);
            return document != null && IsMarkdown(document.Markdown)
                && document.DocumentUri != null && document.DocumentUri.IsAbsoluteUri
                && (document.DocumentUri.Scheme == Uri.UriSchemeHttp || document.DocumentUri.Scheme == Uri.UriSchemeHttps)
                ? document
                : null;
        }

        private static bool HasMarkdownExtension(Uri uri) =>
            uri.AbsolutePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);

        private static bool IsMarkdown(string text) {
            string trimmed = text?.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            return !string.IsNullOrWhiteSpace(trimmed)
                && !Regex.IsMatch(trimmed, @"^(?:<!doctype\s+html|<(?:html|head|body|div|p|table)\b)", RegexOptions.IgnoreCase);
        }

        private string GetChangelogPath(Uri uri, string version) {
            // Existing changelog keys retain the separator even without a version.
            return GetPath(uri, version ?? string.Empty, "PluginChangelogs", ".json");
        }

        private string GetPath(Uri uri, string version, string directoryName, string extension) {
            string identity = version == null ? uri.AbsoluteUri : uri.AbsoluteUri + "\n" + version;
            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            string directory = cacheDirectory ?? Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Cache", directoryName);
            return Path.Combine(directory, key + extension);
        }

        private static T Read<T>(string path, Func<Stream, T> decode, out bool isFresh) where T : class {
            isFresh = false;
            try {
                if (!File.Exists(path)) {
                    return null;
                }

                DateTime lastWrite = File.GetLastWriteTimeUtc(path);
                using var stream = File.OpenRead(path);
                var content = decode(stream);
                TimeSpan age = DateTime.UtcNow - lastWrite;
                isFresh = content != null && age >= TimeSpan.Zero && age < lifetime;
                return content;
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or FileFormatException or JsonException) {
                Logger.Debug($"Unable to read cached plugin content: {ex.Message}");
                return null;
            }
        }

        private static void Save(string path, Action<Stream> encode) {
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    encode(stream);
                }

                File.Move(temporaryPath, path, true);
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                Logger.Debug($"Unable to save cached plugin content: {ex.Message}");
            } finally {
                try {
                    File.Delete(temporaryPath);
                } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                    Logger.Debug($"Unable to remove temporary plugin content: {ex.Message}");
                }
            }
        }
    }
}
