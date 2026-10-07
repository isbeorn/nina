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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.Test.ViewModel {
    [TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
    public class AberrationSaveCompletionTest {
        [Test]
        public void DequeuedSave_WaitingForAberrationPreparation_CompletesWithoutUiPumping() {
            Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.Dispatcher.CheckAccess().Should().BeTrue();
            IProfile profile = Mock.Of<IProfile>(value => value.ImageFileSettings == new NINA.Profile.ImageFileSettings {
                FilePath = TestContext.CurrentContext.WorkDirectory,
                FilePattern = "aberration-save"
            });
            IProfileService profiles = Mock.Of<IProfileService>(value => value.ActiveProfile == profile);
            AberrationInspectorVM inspector = new AberrationInspectorVM(profiles);
            BitmapSource source = BitmapSource.Create(1024, 1024, 96, 96, PixelFormats.Gray16, null, new ushort[1024 * 1024], 1024 * 2);
            source.Freeze();
            IRenderedImage rendered = Mock.Of<IRenderedImage>(value => value.Image == source);
            Mock<IImageData> image = new Mock<IImageData>();
            image.SetupGet(value => value.MetaData).Returns(new ImageMetaData { Image = new ImageParameter { Id = 1, ImageType = "LIGHT" } });
            image.SetupGet(value => value.Properties).Returns(new ImageProperties(1024, 1024, 16, false, 0, 0));
            image.SetupGet(value => value.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(Mock.Of<IImageStatistics>())));
            image.Setup(value => value.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), false, It.IsAny<IList<ImagePattern>>()))
                .ReturnsAsync(Path.Combine(TestContext.CurrentContext.WorkDirectory, "aberration-save.fits"));
            TaskCompletionSource dequeued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource startRendering = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<IApplicationStatusMediator> status = new Mock<IApplicationStatusMediator>();
            status.Setup(value => value.StatusUpdate(It.IsAny<ApplicationStatus>())).Callback<ApplicationStatus>(value => {
                if (value.Status == string.Empty) completed.TrySetResult();
            });
            ImageSaveController controller = new ImageSaveController(profiles, Mock.Of<IImageSaveMediator>(), status.Object);
            controller.BeforeImageSaved += (_, _) => {
                dequeued.TrySetResult();
                return Task.CompletedTask;
            };
            Task<IRenderedImage> prepare = Task.Run(async () => {
                await startRendering.Task.ConfigureAwait(false);
                await inspector.Initialize(source).ConfigureAwait(false);
                return rendered;
            });

            try {
                controller.Enqueue(image.Object, prepare, null, CancellationToken.None).GetAwaiter().GetResult();
                dequeued.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                startRendering.TrySetResult();

                // Closing waits for the save worker synchronously on the UI thread.
                // A bounded wait reproduces that dependency without invoking the one-minute shutdown timeout.
                completed.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue("an in-flight save must complete even while the UI is waiting for it");

                image.Verify(value => value.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), false, It.IsAny<IList<ImagePattern>>()), Times.Once);
                prepare.GetAwaiter().GetResult().Should().BeSameAs(rendered);
                inspector.MosaicImage.IsFrozen.Should().BeTrue();
            } finally {
                startRendering.TrySetResult();
                PumpUntil(() => completed.Task.IsCompleted);
                controller.Shutdown();
            }
        }

        [Test]
        public void Initialize_CompletesAfterOwnedDispatcherShutsDown() {
            Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.Dispatcher.CheckAccess().Should().BeTrue();
            AberrationInspectorVM inspector = new AberrationInspectorVM(Mock.Of<IProfileService>());
            BitmapSource source = BitmapSource.Create(1024, 1024, 96, 96, PixelFormats.Gray16, null, new ushort[1024 * 1024], 1024 * 2);
            source.Freeze();
            Dispatcher? rendererDispatcher = null;
            ApartmentState apartment = ApartmentState.Unknown;
            inspector.PropertyChanged += (_, args) => {
                if (args.PropertyName != nameof(inspector.MosaicImage)) return;
                rendererDispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                apartment = Thread.CurrentThread.GetApartmentState();
            };

            Task render = inspector.Initialize(source);
            PumpUntil(() => render.IsCompleted);
            render.GetAwaiter().GetResult();

            rendererDispatcher.Should().NotBeNull().And.NotBeSameAs(application.Dispatcher);
            rendererDispatcher!.HasShutdownFinished.Should().BeTrue();
            apartment.Should().Be(ApartmentState.STA);
            inspector.MosaicImage.IsFrozen.Should().BeTrue();
        }

        [Test]
        public async Task Initialize_FailedRender_ReleasesSlotForNextRender() {
            AberrationInspectorVM inspector = new AberrationInspectorVM(Mock.Of<IProfileService>());
            BitmapSource tooSmall = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Gray16, null, new ushort[1], 2);
            tooSmall.Freeze();
            BitmapSource valid = BitmapSource.Create(1024, 1024, 96, 96, PixelFormats.Gray16, null, new ushort[1024 * 1024], 1024 * 2);
            valid.Freeze();

            Func<Task> failedRender = () => inspector.Initialize(tooSmall).WaitAsync(TimeSpan.FromSeconds(5));
            await failedRender.Should().ThrowAsync<Exception>().WithMessage("Image too small for aberration inspector. Must be at least 780x780");
            await inspector.Initialize(valid).WaitAsync(TimeSpan.FromSeconds(5));

            inspector.MosaicImage.IsFrozen.Should().BeTrue();
            inspector.MosaicImage.PixelWidth.Should().Be(780);
            inspector.MosaicImage.PixelHeight.Should().Be(780);
        }

        [Test]
        public async Task Initialize_PublicationFails_ShutsDownRendererAndReleasesSlot() {
            AberrationInspectorVM inspector = new AberrationInspectorVM(Mock.Of<IProfileService>());
            BitmapSource source = BitmapSource.Create(1024, 1024, 96, 96, PixelFormats.Gray16, null, new ushort[1024 * 1024], 1024 * 2);
            source.Freeze();
            Dispatcher? rendererDispatcher = null;
            System.ComponentModel.PropertyChangedEventHandler failingListener = (_, args) => {
                if (args.PropertyName != nameof(inspector.MosaicImage)) return;
                rendererDispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                throw new InvalidOperationException("Diagnostic publication failure.");
            };
            inspector.PropertyChanged += failingListener;

            Func<Task> render = () => inspector.Initialize(source).WaitAsync(TimeSpan.FromSeconds(5));
            await render.Should().ThrowAsync<InvalidOperationException>().WithMessage("Diagnostic publication failure.");

            rendererDispatcher.Should().NotBeNull();
            rendererDispatcher!.HasShutdownFinished.Should().BeTrue();
            inspector.PropertyChanged -= failingListener;
            await inspector.Initialize(source).WaitAsync(TimeSpan.FromSeconds(5));
            inspector.MosaicImage.IsFrozen.Should().BeTrue();
        }

        private static void PumpUntil(Func<bool> completed) {
            Stopwatch timeout = Stopwatch.StartNew();
            while (!completed()) {
                if (timeout.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("The pending aberration save did not finish.");
                DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
        }
    }
}
