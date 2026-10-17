#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Core.Utility;
using NINA.Test.Plugin;
using NINA.View.Plugins;
using System.Text;

namespace NINA.Test.View {

    [TestFixture]
    public class PluginChangelogCacheTest {
        private string root = null!;
        private string directory = null!;

        [SetUp]
        public void SetUp() {
            root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PluginChangelogCache", Guid.NewGuid().ToString("N"));
            directory = Path.Combine(root, "Cache");
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown() {
            Directory.Delete(root, true);
        }

        [Test]
        public async Task Notes_PersistAcrossCacheInstancesWithSevenDayFreshness() {
            using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes("\uFEFF# Changelog\n\nSaved notes."), "text/plain");
            var uri = new Uri(server.Url + "/CHANGELOG.md");
            var cache = new PluginContentCache(directory);
            var downloaded = await cache.FetchChangelogAsync(uri, "1.0", CancellationToken.None);
            downloaded.Markdown.Should().Contain("Saved notes.");
            downloaded.DocumentUri.Should().Be(uri);
            server.Requests.Single().Should().Contain("User-Agent: " + CoreUtil.UserAgent);
            var persisted = new PluginContentCache(directory);
            persisted.ReadChangelog(uri, "1.0").Markdown.Should().Be(downloaded.Markdown);
            persisted.ReadChangelog(uri, "1.0").IsFresh.Should().BeTrue();
            string path = Directory.GetFiles(directory, "*.json").Single();

            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-7).AddMinutes(1));
            persisted.ReadChangelog(uri, "1.0").IsFresh.Should().BeTrue();
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-7).AddMinutes(-1));
            persisted.ReadChangelog(uri, "1.0").IsFresh.Should().BeFalse();
            persisted.ReadChangelog(uri, "1.0").Markdown.Should().Be(downloaded.Markdown);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
            persisted.ReadChangelog(uri, "1.0").IsFresh.Should().BeFalse("a future timestamp cannot establish freshness");
            Directory.GetFiles(directory, "*.tmp").Should().BeEmpty();
            server.Requests.Should().ContainSingle();
        }

        [Test]
        public async Task Identity_SeparatesDisplayedVersionsAndTheFullUrl() {
            using var server = new LoopbackHttpServer(responses: [
                new(Encoding.UTF8.GetBytes("# First")),
                new(Encoding.UTF8.GetBytes("# Second")),
                new(Encoding.UTF8.GetBytes("# Third"))
            ], "text/markdown");
            var cache = new PluginContentCache(directory);
            var first = new Uri(server.Url + "/notes.md?v=1");
            var second = new Uri(server.Url + "/notes.md?v=2");
            await cache.FetchChangelogAsync(first, "1.0", CancellationToken.None);
            cache.ReadChangelog(first, "2.0").Should().BeNull();
            cache.ReadChangelog(second, "1.0").Should().BeNull();
            await cache.FetchChangelogAsync(first, "2.0", CancellationToken.None);
            await cache.FetchChangelogAsync(second, "1.0", CancellationToken.None);

            cache.ReadChangelog(first, "1.0").Markdown.Should().Be("# First");
            cache.ReadChangelog(first, "2.0").Markdown.Should().Be("# Second");
            cache.ReadChangelog(second, "1.0").Markdown.Should().Be("# Third");
            Directory.GetFiles(directory, "*.json").Should().HaveCount(3);
        }

        [Test]
        public async Task FailedRefresh_LeavesStaleContentAndTimestampIntactForRetry() {
            using var server = new LoopbackHttpServer(responses: [
                new(Encoding.UTF8.GetBytes("# Saved")),
                new([], 503),
                new(Encoding.UTF8.GetBytes("# Refreshed"))
            ], "text/markdown");
            var cache = new PluginContentCache(directory);
            var uri = new Uri(server.Url + "/notes.md");
            await cache.FetchChangelogAsync(uri, "1", CancellationToken.None);
            string path = Directory.GetFiles(directory, "*.json").Single();
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-8));
            DateTime timestamp = File.GetLastWriteTimeUtc(path);

            Func<Task> refresh = () => cache.FetchChangelogAsync(uri, "1", CancellationToken.None);
            await refresh.Should().ThrowAsync<System.Net.Http.HttpRequestException>();
            cache.ReadChangelog(uri, "1").Markdown.Should().Be("# Saved");
            cache.ReadChangelog(uri, "1").IsFresh.Should().BeFalse();
            File.GetLastWriteTimeUtc(path).Should().Be(timestamp);

            (await cache.FetchChangelogAsync(uri, "1", CancellationToken.None)).Markdown.Should().Be("# Refreshed");
            cache.ReadChangelog(uri, "1").IsFresh.Should().BeTrue();
            server.Requests.Should().HaveCount(3);
        }

        [TestCase("text/html", "<html><body>Release notes</body></html>")]
        [TestCase("text/plain", "\uFEFF  <!DOCTYPE html><html>Release notes</html>")]
        [TestCase("application/json", "{\"notes\":\"release\"}")]
        [TestCase("text/markdown", " \r\n\uFEFF")]
        public async Task EmptyHtmlAndUnsupportedResponses_AreNotCached(string contentType, string body) {
            using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes(body), contentType);
            var cache = new PluginContentCache(directory);

            (await cache.FetchChangelogAsync(new Uri(server.Url + "/notes.md"), "1", CancellationToken.None)).Should().BeNull();
            Directory.Exists(directory).Should().BeFalse();
        }

        [Test]
        public async Task CorruptCache_IsIgnoredAndReplacedByUsableNotes() {
            using var server = new LoopbackHttpServer(responses: [
                new(Encoding.UTF8.GetBytes("# First")),
                new(Encoding.UTF8.GetBytes("# Replacement"))
            ], "text/markdown");
            var cache = new PluginContentCache(directory);
            var uri = new Uri(server.Url + "/notes.md");
            await cache.FetchChangelogAsync(uri, "1", CancellationToken.None);
            string path = Directory.GetFiles(directory, "*.json").Single();
            File.WriteAllText(path, "not a cached document");
            cache.ReadChangelog(uri, "1").Should().BeNull();

            await cache.FetchChangelogAsync(uri, "1", CancellationToken.None);
            new PluginContentCache(directory).ReadChangelog(uri, "1").Markdown.Should().Be("# Replacement");
        }

        [Test]
        public async Task UnwritableCache_DoesNotDiscardDownloadedNotes() {
            File.WriteAllText(directory, "Blocks cache directory creation.");
            using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes("# Usable"), "text/plain");
            var cache = new PluginContentCache(directory);
            var uri = new Uri(server.Url + "/notes.md");

            (await cache.FetchChangelogAsync(uri, "1", CancellationToken.None)).Markdown.Should().Be("# Usable");
            cache.ReadChangelog(uri, "1").Should().BeNull();
            Directory.GetFiles(root).Should().ContainSingle();
        }

        [TestCase("https://github.com/owner/repo/blob/main/docs/CHANGELOG.md?view=1", "https://raw.githubusercontent.com/owner/repo/main/docs/CHANGELOG.md?view=1")]
        [TestCase("https://bitbucket.org/owner/repo/src/master/docs/Changelog.md", "https://bitbucket.org/owner/repo/raw/master/docs/Changelog.md")]
        [TestCase("https://github.com/owner/repo/releases", "https://github.com/owner/repo/releases")]
        [TestCase("https://github.com/owner/repo/CHANGELOG.md", "https://github.com/owner/repo/CHANGELOG.md")]
        [TestCase("https://example.org/docs/changes.markdown", "https://example.org/docs/changes.markdown")]
        public void MarkdownFileLinks_NormalizeOnlyKnownRawRoutes(string original, string expected) {
            PluginContentCache.NormalizeChangelogUri(new Uri(original)).AbsoluteUri.Should().Be(expected);
        }

        [Test]
        public async Task CancelledDownload_DoesNotPersistAResponse() {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = new LoopbackHttpServer(Encoding.UTF8.GetBytes("# Obsolete"), "text/plain", responseGate: gate.Task);
            using var cancellation = new CancellationTokenSource();
            var cache = new PluginContentCache(directory);
            try {
                var pending = cache.FetchChangelogAsync(new Uri(server.Url + "/notes.md"), "1", cancellation.Token);
                await server.RequestReceived.WaitAsync(TimeSpan.FromSeconds(10));
                cancellation.Cancel();
                Func<Task> finish = () => pending;
                await finish.Should().ThrowAsync<OperationCanceledException>();
                Directory.Exists(directory).Should().BeFalse();
            } finally {
                gate.TrySetResult();
            }
        }
    }
}
