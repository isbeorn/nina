using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Image.FileFormat;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces.Mediator;
using Nito.AsyncEx;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NINA.Test.ViewModel {
    [TestFixture]
    [NonParallelizable]
    public class ImageSaveControllerTest {
        private ImageSaveController controller;
        private Mock<IImageData> image;
        private TaskCompletionSource<bool> saved;
        private int expectedSaves;

        [SetUp]
        public void SetUp() {
            var profile = new Mock<IProfileService> { DefaultValue = DefaultValue.Mock };
            profile.SetupGet(x => x.ActiveProfile.ImageFileSettings.FilePath).Returns(TestContext.CurrentContext.WorkDirectory);
            profile.Setup(x => x.ActiveProfile.ImageFileSettings.GetFilePattern(It.IsAny<string>())).Returns("test");
            controller = new ImageSaveController(profile.Object, Mock.Of<IImageSaveMediator>(), Mock.Of<IApplicationStatusMediator>());
            image = new Mock<IImageData>();
            image.SetupGet(x => x.MetaData).Returns(new ImageMetaData());
            image.SetupGet(x => x.Properties).Returns(new ImageProperties(2, 2, 16, false, 0, 0));
            image.SetupGet(x => x.Statistics).Returns(new AsyncLazy<IImageStatistics>(() => Task.FromResult(Mock.Of<IImageStatistics>())));
            image.Setup(x => x.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), false, It.IsAny<IList<ImagePattern>>()))
                .ReturnsAsync(Path.Combine(TestContext.CurrentContext.WorkDirectory, "saved.fit"));
            saved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            expectedSaves = 1;
            int saves = 0;
            controller.ImageSaved += (_, _) => { if (Interlocked.Increment(ref saves) == expectedSaves) { saved.TrySetResult(true); } };
        }

        [TearDown]
        public void TearDown() => controller.Shutdown();

        [TestCase(0x70)]
        [TestCase(0x27)]
        public void DiskFullNotification_IsThrottledWithoutHidingOtherFailures(int error) {
            var diskFull = new AggregateException(new InvalidOperationException("Other error"),
                new IOException("Wrapper", new IOException("Disk full", unchecked((int)0x80070000) | error)));
            var method = typeof(ImageSaveController).GetMethod("ShouldShowFailureNotification", BindingFlags.NonPublic | BindingFlags.Instance);
            bool ShouldShow(Exception ex) => (bool)method.Invoke(controller, new object[] { ex });
            ShouldShow(diskFull).Should().BeTrue();
            ShouldShow(diskFull).Should().BeFalse();
            ShouldShow(new IOException("Permission denied", unchecked((int)0x80070005))).Should().BeTrue();
            var lastNotification = typeof(ImageSaveController).GetField("lastDiskFullNotificationUtc", BindingFlags.NonPublic | BindingFlags.Instance);
            lastNotification.SetValue(controller, DateTime.UtcNow.AddMinutes(-5).AddSeconds(1));
            ShouldShow(diskFull).Should().BeFalse();
            lastNotification.SetValue(controller, DateTime.UtcNow.AddMinutes(-5).AddSeconds(-1));
            ShouldShow(diskFull).Should().BeTrue();
        }

        [TestCase("prepare")]
        [TestCase("before")]
        [TestCase("finalize")]
        public async Task Queue_ContinuesAfterFailureAndRaisesExistingSuccessEvent(string stage) {
            expectedSaves = stage == "prepare" ? 1 : 2;
            int calls = 0;
            if (stage == "before") {
                controller.BeforeImageSaved += (_, _) => ++calls == 1 ? Task.FromException(new IOException("Before save failed")) : Task.CompletedTask;
            } else if (stage == "finalize") {
                controller.BeforeFinalizeImageSaved += (_, _) => ++calls == 1 ? Task.FromException(new IOException("Finalize failed")) : Task.CompletedTask;
            }
            var prepare = stage == "prepare" ? Task.FromException<IRenderedImage>(new IOException("Prepare failed")) : Task.FromResult(Mock.Of<IRenderedImage>());
            await controller.Enqueue(image.Object, prepare, null, default);
            await controller.Enqueue(image.Object, Task.FromResult(Mock.Of<IRenderedImage>()), null, default);
            await saved.Task.WaitAsync(TimeSpan.FromSeconds(5));
            image.Verify(x => x.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), false, It.IsAny<IList<ImagePattern>>()), Times.Exactly(expectedSaves));
        }

        [Test]
        [Explicit("Exercises the production five-minute image-write timeout; run in isolation.")]
        public async Task Queue_ContinuesAfterRealWriteTimeout() {
            int attempts = 0;
            CancellationToken firstToken = default;
            image.Setup(x => x.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), false, It.IsAny<IList<ImagePattern>>()))
                .Returns(async (FileSaveInfo _, CancellationToken token, bool _, IList<ImagePattern> _) => {
                    if (Interlocked.Increment(ref attempts) == 1) { firstToken = token; }
                    if (token == firstToken) { await Task.Delay(Timeout.Infinite, token); }
                    return Path.Combine(TestContext.CurrentContext.WorkDirectory, "after-timeout.fit");
                });
            var started = DateTime.UtcNow;
            await controller.Enqueue(image.Object, Task.FromResult(Mock.Of<IRenderedImage>()), null, default);
            await controller.Enqueue(image.Object, Task.FromResult(Mock.Of<IRenderedImage>()), null, default);
            await saved.Task.WaitAsync(TimeSpan.FromMinutes(6));
            (DateTime.UtcNow - started).Should().BeGreaterThan(TimeSpan.FromMinutes(4.9)).And.BeLessThan(TimeSpan.FromMinutes(5.5));
            attempts.Should().Be(4);
        }
    }
}
