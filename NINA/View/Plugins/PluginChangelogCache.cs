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
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.View.Plugins {

    internal sealed class PluginChangelogCache {
        private static readonly TimeSpan lifetime = TimeSpan.FromDays(7);
        private readonly string cacheDirectory;

        public PluginChangelogCache(string cacheDirectory = null) {
            this.cacheDirectory = cacheDirectory;
        }

        internal sealed record Document(string Markdown, Uri DocumentUri) {
            public bool IsFresh { get; init; }
        }

        public Document Read(Uri uri, string version) {
            try {
                string path = GetPath(uri, version);
                if (!File.Exists(path)) {
                    return null;
                }

                var document = JsonSerializer.Deserialize<Document>(File.ReadAllText(path));
                if (document == null || !IsMarkdown(document.Markdown)
                    || document.DocumentUri == null || !IsHttp(document.DocumentUri)) {
                    return null;
                }

                TimeSpan age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
                return document with { IsFresh = age >= TimeSpan.Zero && age < lifetime };
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException) {
                Logger.Debug($"Unable to read cached plugin changelog: {ex.Message}");
                return null;
            }
        }

        public async Task<Document> FetchAsync(Uri uri, string version, CancellationToken cancellationToken) {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreUtil.UserAgent);
            using var response = await client.GetAsync(NormalizeUri(uri), timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string markdown = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            string mediaType = response.Content.Headers.ContentType?.MediaType;
            var documentUri = response.RequestMessage?.RequestUri ?? NormalizeUri(uri);
            bool supported = mediaType is "text/plain" or "text/markdown" or "text/x-markdown"
                || (string.IsNullOrEmpty(mediaType) || mediaType == "application/octet-stream") && HasMarkdownExtension(documentUri);
            if (!supported || !IsMarkdown(markdown)) {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var document = new Document(markdown, documentUri) { IsFresh = true };
            Save(GetPath(uri, version), document);
            return document;
        }

        internal static Uri NormalizeUri(Uri uri) {
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

        private static bool HasMarkdownExtension(Uri uri) =>
            uri.AbsolutePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);

        private static bool IsHttp(Uri uri) => uri.IsAbsoluteUri
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static bool IsMarkdown(string text) {
            string trimmed = text?.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            return !string.IsNullOrWhiteSpace(trimmed)
                && !Regex.IsMatch(trimmed, @"^(?:<!doctype\s+html|<(?:html|head|body|div|p|table)\b)", RegexOptions.IgnoreCase);
        }

        private string GetPath(Uri uri, string version) {
            string identity = uri.AbsoluteUri + "\n" + version;
            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            string directory = cacheDirectory ?? Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Cache", "PluginChangelogs");
            return Path.Combine(directory, key + ".json");
        }

        private static void Save(string path, Document document) {
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document));
                File.Move(temporaryPath, path, true);
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                Logger.Debug($"Unable to save cached plugin changelog: {ex.Message}");
            } finally {
                try {
                    File.Delete(temporaryPath);
                } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                    Logger.Debug($"Unable to remove temporary plugin changelog: {ex.Message}");
                }
            }
        }
    }
}
