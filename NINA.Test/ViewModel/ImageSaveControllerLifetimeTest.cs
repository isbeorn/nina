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
using Moq;
using NINA.Core.Model;
using NINA.Image.FileFormat;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.ViewModel {
    [TestFixture]
    [NonParallelizable]
    public class ImageSaveControllerLifetimeTest {
        [Test]
        public void CompletedEnqueue_DoesNotRetainCallerCancellationSource() {
            using var context = new SaveContext();

            WeakReference caller = EnqueueWithTemporaryCaller(context);

            AssertCollected(caller);
            GC.KeepAlive(context.Controller);
        }

        [Test]
        public async Task PendingEnqueue_RemainsLinkedUntilCallerCancels() {
            using var context = new SaveContext();
            context.BlockWorkerAndFillQueue();
            using var caller = new CancellationTokenSource();

            Task pending = context.Enqueue(caller.Token);
            pending.IsCompleted.Should().BeFalse();
            caller.Cancel();

            Func<Task> completion = () => pending.WaitAsync(TimeSpan.FromSeconds(10));
            OperationCanceledException canceled = (await completion.Should().ThrowAsync<OperationCanceledException>()).Which;
            canceled.CancellationToken.CanBeCanceled.Should().BeTrue();
            Action accessWaitHandle = () => _ = canceled.CancellationToken.WaitHandle;
            accessWaitHandle.Should().Throw<ObjectDisposedException>("the enqueue must dispose its linked cancellation source after cancellation");
        }

        [Test]
        public async Task PendingEnqueue_RemainsLinkedUntilWorkerStops() {
            using var context = new SaveContext();
            context.BlockWorkerAndFillQueue();
            Task pending = context.Enqueue(CancellationToken.None);
            pending.IsCompleted.Should().BeFalse();

            Task shutdown = Task.Run(context.Controller.Shutdown);
            try {
                Func<Task> completion = () => pending.WaitAsync(TimeSpan.FromSeconds(10));
                await completion.Should().ThrowAsync<OperationCanceledException>();
            } finally {
                context.ReleaseWorker();
                await shutdown.WaitAsync(TimeSpan.FromSeconds(10));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task FinishedSave_DisposesWriteTimeoutSource(bool failWrite) {
            using var context = new SaveContext(failWrite);

            await context.Enqueue(CancellationToken.None);
            await context.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

            context.WriteToken.CanBeCanceled.Should().BeTrue();
            Action accessWaitHandle = () => _ = context.WriteToken.WaitHandle;
            accessWaitHandle.Should().Throw<ObjectDisposedException>();
            context.WriteAttempts.Should().Be(failWrite ? 3 : 1);
            context.Failures.Should().Be(failWrite ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference EnqueueWithTemporaryCaller(SaveContext context) {
            var caller = new CancellationTokenSource();
            context.Enqueue(caller.Token).GetAwaiter().GetResult();
            return new WeakReference(caller);
        }

        private static void AssertCollected(WeakReference reference) {
            Stopwatch timeout = Stopwatch.StartNew();
            while (reference.IsAlive && timeout.Elapsed < TimeSpan.FromSeconds(3)) {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                if (reference.IsAlive) Thread.Sleep(10);
            }
            reference.IsAlive.Should().BeFalse("a completed enqueue must release its link to the caller's cancellation source");
        }

        private sealed class SaveContext : IDisposable {
            private readonly Mock<IImageData> image = new Mock<IImageData>();
            private readonly TaskCompletionSource release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource Completed { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            public ImageSaveController Controller { get; }
            public CancellationToken WriteToken { get; private set; }
            public int WriteAttempts { get; private set; }
            public int Failures { get; private set; }

            public SaveContext(bool failWrite = false) {
                var settings = new NINA.Profile.ImageFileSettings { FilePath = Path.GetTempPath(), FilePattern = "probe" };
                var profile = Mock.Of<IProfile>(value => value.ImageFileSettings == settings);
                var profileService = Mock.Of<IProfileService>(value => value.ActiveProfile == profile);
                var status = new Mock<IApplicationStatusMediator>();
                status.Setup(value => value.StatusUpdate(It.IsAny<ApplicationStatus>())).Callback<ApplicationStatus>(value => {
                    if (value.Status == string.Empty) Completed.TrySetResult();
                });
                image.SetupGet(value => value.MetaData).Returns(new ImageMetaData { Image = new ImageParameter { Id = 1, ImageType = "LIGHT" } });
                image.SetupGet(value => value.Properties).Returns(new ImageProperties(2, 2, 16, false, 0, 0));
                image.SetupGet(value => value.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(Mock.Of<IImageStatistics>())));
                image.Setup(value => value.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), false, It.IsAny<IList<ImagePattern>>()))
                    .Returns((FileSaveInfo _, CancellationToken token, bool _, IList<ImagePattern> _) => {
                        WriteToken = token;
                        WriteAttempts++;
                        return failWrite ? Task.FromException<string>(new IOException("Diagnostic write failure.")) : Task.FromResult(Path.Combine(Path.GetTempPath(), "probe.fit"));
                    });
                Controller = new ImageSaveController(profileService, Mock.Of<IImageSaveMediator>(), status.Object);
                Controller.ImageSaveFailed += (_, _) => { Failures++; return Task.CompletedTask; };
            }

            public Task Enqueue(CancellationToken token) => Controller.Enqueue(image.Object, Task.FromResult(Mock.Of<IRenderedImage>()), null, token);

            public void BlockWorkerAndFillQueue() {
                var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Controller.BeforeImageSaved += async (_, _) => { entered.TrySetResult(); await release.Task; };
                Enqueue(CancellationToken.None).GetAwaiter().GetResult();
                entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                // Stop at the first blocked enqueue instead of depending on the configured queue capacity.
                for (int attempt = 0; attempt < 1024; attempt++) {
                    if (!Enqueue(CancellationToken.None).IsCompleted) return;
                }
                throw new AssertionException("The image-save queue did not fill.");
            }

            public void ReleaseWorker() => release.TrySetResult();

            public void Dispose() {
                ReleaseWorker();
                Controller.Shutdown();
            }
        }
    }
}
