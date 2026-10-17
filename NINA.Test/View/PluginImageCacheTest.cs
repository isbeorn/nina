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
using System.Diagnostics;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    [SingleThreaded]
    public class PluginImageCacheTest {
        private string originalApplicationTempPath = null!;
        private string testRoot = null!;
        private string cacheDirectory = null!;

        [SetUp]
        public void SetUp() {
            originalApplicationTempPath = CoreUtil.APPLICATIONTEMPPATH;
            testRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PluginImageCache", Guid.NewGuid().ToString("N"));
            cacheDirectory = Path.Combine(testRoot, "Cache", "PluginIcons");
            Directory.CreateDirectory(testRoot);
            CoreUtil.APPLICATIONTEMPPATH = testRoot;
        }

        [TearDown]
        public void TearDown() {
            CoreUtil.APPLICATIONTEMPPATH = originalApplicationTempPath;
            Directory.Delete(testRoot, true);
        }

        [Test]
        public void ImagesAndChangelogs_KeepTheSameUrlAndVersionInSeparatePersistentFiles() {
            using var server = new LoopbackHttpServer(responses: [
                new(CreatePng(Colors.Red)),
                new(Encoding.UTF8.GetBytes("# Changelog notes")),
                new(CreatePng(Colors.Blue))
            ], "text/plain");
            var cache = new PluginContentCache();
            var uri = new Uri(server.Url + "/content");
            const string version = "1.0.0.0";

            Load(cache, uri, version).Should().Be(Colors.Red);
            var downloaded = cache.FetchChangelogAsync(uri, version, CancellationToken.None).GetAwaiter().GetResult();
            downloaded.Markdown.Should().Be("# Changelog notes");
            string imagePath = Directory.GetFiles(cacheDirectory, "*.png").Single();
            string changelogDirectory = Path.Combine(testRoot, "Cache", "PluginChangelogs");
            Directory.GetFiles(changelogDirectory, "*.json").Should().ContainSingle();

            var persisted = new PluginContentCache();
            Load(persisted, uri, version).Should().Be(Colors.Red);
            persisted.ReadChangelog(uri, version).Markdown.Should().Be(downloaded.Markdown);
            File.SetLastWriteTimeUtc(imagePath, DateTime.UtcNow.AddDays(-8));
            Load(cache, uri, version).Should().Be(Colors.Blue);
            persisted.ReadChangelog(uri, version).Markdown.Should().Be(downloaded.Markdown);
            server.Requests.Should().HaveCount(3);
            Directory.GetFiles(cacheDirectory, "*.tmp").Should().BeEmpty();
            Directory.GetFiles(changelogDirectory, "*.tmp").Should().BeEmpty();
        }

        [Test]
        public void IconDownload_PersistsDecodedImageInTheLocalCache() {
            using var server = new LoopbackHttpServer(CreatePng(Colors.Red), "image/png");
            var image = new PluginImage { ImageUrl = server.Url + "/icon.png" };

            PumpUntil(() => image.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);

            image.HasImage.Should().BeTrue();
            Directory.Exists(cacheDirectory).Should().BeTrue();
            Directory.GetFiles(cacheDirectory, "*.png").Should().ContainSingle();
        }

        [Test]
        public void FreshIcon_IsAvailableToANewCacheInstanceWithoutTheServer() {
            Uri uri;
            using (var server = new LoopbackHttpServer(CreatePng(Colors.Red), "image/png")) {
                uri = new Uri(server.Url + "/icon.png");
                Load(new PluginContentCache(cacheDirectory), uri).Should().Be(Colors.Red);
                server.Requests.Should().ContainSingle();
            }

            Load(new PluginContentCache(cacheDirectory), uri).Should().Be(Colors.Red);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void IconsAndScreenshots_RefetchAnUnchangedUrlForANewVersion(bool useFallback) {
            using var server = new LoopbackHttpServer(responses: [
                new(CreatePng(Colors.Red)),
                new(CreatePng(Colors.Blue))
            ], "image/png");
            var image = new PluginImage {
                UseFallback = useFallback,
                ImageUrl = server.Url + "/artwork.png",
                CacheVersion = "1.0.0.0"
            };
            PumpUntil(() => image.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);
            ReadColor((BitmapSource)image.Source).Should().Be(Colors.Red);

            image.CacheVersion = "2.0.0.0";
            PumpUntil(() => image.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);
            ReadColor((BitmapSource)image.Source).Should().Be(Colors.Blue);
            server.Requests.Should().HaveCount(2);
            Directory.GetFiles(cacheDirectory, "*.png").Should().HaveCount(2);

            image.CacheVersion = "1.0.0.0";
            PumpUntil(() => image.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);
            ReadColor((BitmapSource)image.Source).Should().Be(Colors.Red);
            image.CacheVersion = "2.0.0.0";
            PumpUntil(() => image.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);
            ReadColor((BitmapSource)image.Source).Should().Be(Colors.Blue);
            server.Requests.Should().HaveCount(2);

            var uri = new Uri(image.ImageUrl);
            Load(new PluginContentCache(cacheDirectory), uri, "1.0.0.0").Should().Be(Colors.Red);
            Load(new PluginContentCache(cacheDirectory), uri, "2.0.0.0").Should().Be(Colors.Blue);
            server.Requests.Should().HaveCount(2);
        }

        [Test]
        public void Icon_IsReusedBeforeSevenDaysAndRefreshedAfterwards() {
            using var server = new LoopbackHttpServer(responses: [
                new(CreatePng(Colors.Red)),
                new(CreatePng(Colors.Blue))
            ], "image/png");
            var uri = new Uri(server.Url + "/icon.png");
            Load(new PluginContentCache(cacheDirectory), uri, "1.0.0.0").Should().Be(Colors.Red);
            string path = Directory.GetFiles(cacheDirectory, "*.png").Single();
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-7).AddMinutes(1));

            Load(new PluginContentCache(cacheDirectory), uri, "1.0.0.0").Should().Be(Colors.Red);
            server.Requests.Should().ContainSingle();
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-7).AddMinutes(-1));

            Load(new PluginContentCache(cacheDirectory), uri, "1.0.0.0").Should().Be(Colors.Blue);
            server.Requests.Should().HaveCount(2);
            Load(new PluginContentCache(cacheDirectory), uri, "1.0.0.0").Should().Be(Colors.Blue);
            server.Requests.Should().HaveCount(2);
            Directory.GetFiles(cacheDirectory, "*.tmp").Should().BeEmpty();
        }

        [Test]
        public void FailedRefresh_KeepsTheOldIconAndRetriesWithoutExtendingItsLifetime() {
            using var server = new LoopbackHttpServer(responses: [
                new(CreatePng(Colors.Red)),
                new([], 503),
                new(CreatePng(Colors.Blue))
            ], "image/png");
            var cache = new PluginContentCache(cacheDirectory);
            var uri = new Uri(server.Url + "/icon.png");
            Load(cache, uri).Should().Be(Colors.Red);
            string path = Directory.GetFiles(cacheDirectory, "*.png").Single();
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-8));
            DateTime lastWrite = File.GetLastWriteTimeUtc(path);

            Load(cache, uri).Should().Be(Colors.Red);
            File.GetLastWriteTimeUtc(path).Should().Be(lastWrite);
            server.Requests.Should().HaveCount(2);

            Load(cache, uri).Should().Be(Colors.Blue);
            server.Requests.Should().HaveCount(3);
        }

        [Test]
        public void CorruptCache_IsReplacedByAValidDownload() {
            using var server = new LoopbackHttpServer(responses: [
                new(CreatePng(Colors.Red)),
                new(CreatePng(Colors.Blue))
            ], "image/png");
            var cache = new PluginContentCache(cacheDirectory);
            var uri = new Uri(server.Url + "/icon.png");
            Load(cache, uri).Should().Be(Colors.Red);
            File.WriteAllBytes(Directory.GetFiles(cacheDirectory, "*.png").Single(), [1, 2, 3]);

            Load(cache, uri).Should().Be(Colors.Blue);
            Load(new PluginContentCache(cacheDirectory), uri).Should().Be(Colors.Blue);
            server.Requests.Should().HaveCount(2);
        }

        [Test]
        public void UnwritableCache_DoesNotPreventShowingTheDownloadedIcon() {
            Directory.CreateDirectory(Path.GetDirectoryName(cacheDirectory)!);
            File.WriteAllText(cacheDirectory, "This file prevents creating the icon cache directory.");
            using var server = new LoopbackHttpServer(CreatePng(Colors.Red), "image/png");
            var image = new PluginImage { ImageUrl = server.Url + "/icon.png" };

            PumpUntil(() => image.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);

            ReadColor((BitmapSource)image.Source).Should().Be(Colors.Red);
            image.HasImage.Should().BeTrue();
        }

        [Test]
        public void ConcurrentIconControls_ShareTheSameDownload() {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = new LoopbackHttpServer(CreatePng(Colors.Red), "image/png", responseGate: gate.Task);
            try {
                var first = new PluginImage { ImageUrl = server.Url + "/icon.png" };
                var second = new PluginImage { ImageUrl = first.ImageUrl };
                PumpUntil(() => server.RequestReceived.IsCompleted);
                server.Requests.Should().ContainSingle();
                gate.SetResult();

                PumpUntil(() => first.Source is BitmapSource firstBitmap && firstBitmap.PixelWidth == 2
                    && second.Source is BitmapSource secondBitmap && secondBitmap.PixelWidth == 2);

                first.Source.Should().BeSameAs(second.Source);
                server.Requests.Should().ContainSingle();
            } finally {
                gate.TrySetResult();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ObsoleteDownload_DoesNotReplaceANewImageOrTheFallback(bool clearUrl) {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var oldServer = new LoopbackHttpServer(CreatePng(Colors.Red), "image/png", responseGate: gate.Task);
            using var newServer = new LoopbackHttpServer(CreatePng(Colors.Blue), "image/png");
            try {
                var image = new PluginImage { ImageUrl = oldServer.Url + "/icon.png" };
                var observer = new PluginImage { ImageUrl = image.ImageUrl };
                PumpUntil(() => oldServer.RequestReceived.IsCompleted);
                image.ImageUrl = clearUrl ? "" : newServer.Url + "/icon.png";
                if (!clearUrl) {
                    PumpUntil(() => image.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);
                    ReadColor((BitmapSource)image.Source).Should().Be(Colors.Blue);
                }

                var expected = image.Source;
                gate.SetResult();
                PumpUntil(() => observer.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                image.Source.Should().BeSameAs(expected);
                image.HasImage.Should().BeTrue();
            } finally {
                gate.TrySetResult();
            }
        }

        [Test]
        public void FullUrl_KeepsDifferentQueryVersionsSeparate() {
            using var server = new LoopbackHttpServer(responses: [
                new(CreatePng(Colors.Red)),
                new(CreatePng(Colors.Blue))
            ], "image/png");
            var cache = new PluginContentCache(cacheDirectory);
            var first = new Uri(server.Url + "/icon.png?version=1");
            var second = new Uri(server.Url + "/icon.png?version=2");

            Load(cache, first).Should().Be(Colors.Red);
            Load(cache, second).Should().Be(Colors.Blue);
            Load(cache, first).Should().Be(Colors.Red);
            Load(cache, second).Should().Be(Colors.Blue);

            server.Requests.Should().HaveCount(2);
            Directory.GetFiles(cacheDirectory, "*.png").Should().HaveCount(2);
        }

        [Test]
        public void InvalidDownload_IsNotPersisted() {
            using var server = new LoopbackHttpServer([1, 2, 3], "image/png");
            var cache = new PluginContentCache(cacheDirectory);

            Action load = () => cache.LoadImageAsync(new Uri(server.Url + "/icon.png")).GetAwaiter().GetResult();

            load.Should().Throw<NotSupportedException>();
            Directory.Exists(cacheDirectory).Should().BeFalse();
        }

        [Test]
        public void Screenshots_PersistThroughTheSameCacheAsIcons() {
            using var server = new LoopbackHttpServer(CreatePng(Colors.Red), "image/png");
            var image = new PluginImage { UseFallback = false, ImageUrl = server.Url + "/screenshot.png" };

            PumpUntil(() => image.Source is BitmapSource bitmap && bitmap.PixelWidth == 2);

            image.HasImage.Should().BeTrue();
            Directory.GetFiles(cacheDirectory, "*.png").Should().ContainSingle();
        }

        private static Color Load(PluginContentCache cache, Uri uri, string? version = null) {
            var image = cache.LoadImageAsync(uri, version).GetAwaiter().GetResult();
            image.IsFrozen.Should().BeTrue();
            return ReadColor(image);
        }

        private static Color ReadColor(BitmapSource image) {
            var converted = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            byte[] pixel = new byte[4];
            converted.CopyPixels(new System.Windows.Int32Rect(0, 0, 1, 1), pixel, 4, 0);
            return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
        }

        private static byte[] CreatePng(Color color) {
            byte[] pixels = [color.B, color.G, color.R, 255, color.B, color.G, color.R, 255];
            var bitmap = BitmapSource.Create(2, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        private static void PumpUntil(Func<bool> complete) {
            var timeout = Stopwatch.StartNew();
            while (!complete()) {
                if (timeout.Elapsed > TimeSpan.FromSeconds(10)) {
                    throw new TimeoutException("Plugin image did not finish loading.");
                }

                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
                Thread.Sleep(1);
            }
        }
    }
}
