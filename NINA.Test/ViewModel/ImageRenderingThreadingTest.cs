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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.SkySurvey;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class ImageRenderingThreadingTest {
        [SetUp]
        public void SetUp() {
            Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.Dispatcher.CheckAccess().Should().BeTrue();
            application.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            application.Resources["HistorySVG"] = new GeometryGroup();
            application.Resources["PictureSVG"] = new GeometryGroup();
        }

        [TestCase(true, true, true)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        [TestCase(false, false, false)]
        public void PrepareImageCommand_FromDispatcher_ProcessesOffThreadAndPreservesOptions(
            bool autoStretch, bool detectStars, bool annotateImage) {
            var settings = new ImageSettings {
                AutoStretch = autoStretch,
                DetectStars = detectStars,
                AnnotateImage = annotateImage,
                AutoStretchFactor = 0.3,
                BlackClipping = -3,
                StarSensitivity = StarSensitivityEnum.Highest,
                NoiseReduction = NoiseReductionEnum.High
            };
            using ImageControlVM sut = CreateImageControl(settings);
            int dispatcherThread = Environment.CurrentManagedThreadId;
            var processingThreads = new List<int>();
            BitmapSource originalPixels = CreateImage(8, 8, (_, _) => Colors.Red);
            BitmapSource stretchedPixels = CreateImage(8, 8, (_, _) => Colors.Lime);
            BitmapSource annotatedPixels = CreateImage(8, 8, (_, _) => Colors.Blue);
            var rawData = new Mock<IImageData>();
            rawData.SetupGet(x => x.Properties).Returns(new ImageProperties(8, 8, 16, false, 0, 0));
            var original = new Mock<IRenderedImage>();
            var rerendered = new Mock<IRenderedImage>();
            rerendered.SetupGet(x => x.RawImageData).Returns(rawData.Object);
            rerendered.SetupGet(x => x.Image).Returns(originalPixels);
            var stretched = new Mock<IRenderedImage>();
            stretched.SetupGet(x => x.Image).Returns(stretchedPixels);
            var annotated = new Mock<IRenderedImage>();
            annotated.SetupGet(x => x.Image).Returns(annotatedPixels);
            original.Setup(x => x.ReRender()).Callback(() => processingThreads.Add(Environment.CurrentManagedThreadId))
                .Returns(rerendered.Object);
            rerendered.Setup(x => x.Stretch(0.3, -3, false))
                .Callback(() => processingThreads.Add(Environment.CurrentManagedThreadId))
                .ReturnsAsync(stretched.Object);
            stretched.Setup(x => x.DetectStars(annotateImage, StarSensitivityEnum.Highest, NoiseReductionEnum.High,
                    It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()))
                .Callback(() => processingThreads.Add(Environment.CurrentManagedThreadId))
                .ReturnsAsync(annotateImage ? annotated.Object : stretched.Object);
            sut.RenderedImage = original.Object;
            ImagePreparedEventArgs? prepared = null;
            sut.ImagePrepared += (_, args) => prepared = args;

            ExecutePrepareImageCommand(sut);

            sut.PrepareImageCommand.Execution.IsSuccessfullyCompleted.Should().BeTrue();
            processingThreads.Should().HaveCount(1 + (autoStretch ? 1 : 0) + (detectStars ? 1 : 0));
            processingThreads.Should().NotContain(dispatcherThread, "manual preview processing must leave the dispatcher available");
            IRenderedImage expected = detectStars && annotateImage ? annotated.Object : autoStretch ? stretched.Object : rerendered.Object;
            sut.RenderedImage.Should().BeSameAs(expected);
            sut.Image.Should().BeSameAs(expected.Image);
            prepared.Should().NotBeNull();
            prepared!.RenderedImage.Should().BeSameAs(rerendered.Object);
            original.Verify(x => x.ReRender(), Times.Once);
            rerendered.Verify(x => x.Stretch(0.3, -3, false), autoStretch ? Times.Once() : Times.Never());
            stretched.Verify(x => x.DetectStars(annotateImage, StarSensitivityEnum.Highest, NoiseReductionEnum.High,
                It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>()), detectStars ? Times.Once() : Times.Never());
        }

        [Test]
        public void PrepareImageCommand_WithoutImage_CompletesWithoutPublishingPreview() {
            using ImageControlVM sut = CreateImageControl(new ImageSettings());
            int prepared = 0;
            sut.ImagePrepared += (_, _) => prepared++;

            ExecutePrepareImageCommand(sut);

            sut.PrepareImageCommand.Execution.IsSuccessfullyCompleted.Should().BeTrue();
            sut.PrepareImageCommand.Execution.Result.Should().BeTrue();
            sut.RenderedImage.Should().BeNull();
            sut.Image.Should().BeNull();
            prepared.Should().Be(0);
        }

        [Test]
        public void PrepareImageCommand_WhenRenderingFails_ExposesFailureWithoutReplacingPreview() {
            using ImageControlVM sut = CreateImageControl(new ImageSettings());
            var failure = new InvalidOperationException("Rendering failed");
            var original = new Mock<IRenderedImage>();
            original.Setup(x => x.ReRender()).Throws(failure);
            BitmapSource originalPixels = CreateImage(8, 8, (_, _) => Colors.Red);
            sut.RenderedImage = original.Object;
            sut.Image = originalPixels;
            int prepared = 0;
            sut.ImagePrepared += (_, _) => prepared++;

            ExecutePrepareImageCommand(sut);

            sut.PrepareImageCommand.Execution.IsFaulted.Should().BeTrue();
            sut.PrepareImageCommand.Execution.InnerException.Should().BeSameAs(failure);
            sut.PrepareImageCommand.IsRunning.Should().BeFalse();
            sut.RenderedImage.Should().BeSameAs(original.Object);
            sut.Image.Should().BeSameAs(originalPixels);
            prepared.Should().Be(0);
        }

        [Test]
        public void ImageSaved_FromWorker_AddsFrozenScaledThumbnailWithMetadata() {
            Mock<IImageSaveMediator> saveMediator = new Mock<IImageSaveMediator>();
            Mock<IImagingMediator> imagingMediator = new Mock<IImagingMediator>();
            ThumbnailVM sut = new ThumbnailVM(Mock.Of<IProfileService>(), imagingMediator.Object, saveMediator.Object, Mock.Of<IImageDataFactory>());
            BitmapSource source = CreateImage(400, 200, (x, y) => x < 200
                ? (y < 100 ? Colors.Red : Colors.Blue)
                : (y < 100 ? Colors.Lime : Colors.White));
            ImageSavedEventArgs args = new ImageSavedEventArgs {
                Image = source,
                PathToImage = ImagePath(0),
                FileType = FileTypeEnum.FITS,
                Duration = 120,
                Filter = "L",
                IsBayered = true,
                Statistics = Mock.Of<IImageStatistics>(),
                StarDetectionAnalysis = Mock.Of<IStarDetectionAnalysis>()
            };

            Task eventTask = Task.Run(() => saveMediator.Raise(x => x.ImageSaved += null, args));
            PumpUntil(() => eventTask.IsCompleted && sut.SelectedThumbnail != null);
            eventTask.GetAwaiter().GetResult();

            sut.Thumbnails.Count.Should().Be(1);
            var thumbnail = sut.SelectedThumbnail;
            thumbnail.ImagePath.Should().Be(args.PathToImage);
            thumbnail.FileType.Should().Be(args.FileType);
            thumbnail.Duration.Should().Be(args.Duration);
            thumbnail.Filter.Should().Be(args.Filter);
            thumbnail.IsBayered.Should().BeTrue();
            thumbnail.ImageStatistics.Should().BeSameAs(args.Statistics);
            thumbnail.StarDetectionAnalysis.Should().BeSameAs(args.StarDetectionAnalysis);
            thumbnail.ThumbnailImage.IsFrozen.Should().BeTrue();
            thumbnail.ThumbnailImage.PixelWidth.Should().Be(100);
            thumbnail.ThumbnailImage.PixelHeight.Should().Be(50);
            Pixel(thumbnail.ThumbnailImage, 25, 12).Should().Be(Colors.Red);
            Pixel(thumbnail.ThumbnailImage, 75, 12).Should().Be(Colors.Lime);
            Pixel(thumbnail.ThumbnailImage, 25, 37).Should().Be(Colors.Blue);
            Pixel(thumbnail.ThumbnailImage, 75, 37).Should().Be(Colors.White);

            imagingMediator.Raise(x => x.ImagePrepared += null, new ImagePreparedEventArgs());
            sut.SelectedThumbnail.Should().BeNull();
        }

        [Test]
        public void ImageSaved_RepeatedWorkerEvents_KeepLatestFiftyThumbnails() {
            Mock<IImageSaveMediator> saveMediator = new Mock<IImageSaveMediator>();
            ThumbnailVM sut = new ThumbnailVM(Mock.Of<IProfileService>(), Mock.Of<IImagingMediator>(), saveMediator.Object, Mock.Of<IImageDataFactory>());
            BitmapSource source = CreateImage(200, 100, (_, _) => Colors.Red);

            for (int index = 0; index < 55; index++) {
                Uri path = ImagePath(index);
                Task eventTask = Task.Run(() => saveMediator.Raise(x => x.ImageSaved += null,
                    new ImageSavedEventArgs { Image = source, PathToImage = path }));
                PumpUntil(() => eventTask.IsCompleted && sut.SelectedThumbnail?.ImagePath == path);
                eventTask.GetAwaiter().GetResult();
            }

            sut.Thumbnails.Count.Should().Be(50);
            sut.Thumbnails.First().Value.ImagePath.Should().Be(ImagePath(5));
            sut.SelectedThumbnail.ImagePath.Should().Be(ImagePath(54));
        }

        [Test]
        public void Initialize_FromWorker_PreservesAberrationCrops() {
            AberrationInspectorVM sut = new AberrationInspectorVM(Mock.Of<IProfileService>()) {
                Columns = 3,
                CellSize = 4,
                SeparationSize = 2
            };
            BitmapSource source = CreateImage(36, 30, SourceColor);

            Task initializeTask = Task.Run(() => sut.Initialize(source));
            PumpUntil(() => initializeTask.IsCompleted);
            initializeTask.GetAwaiter().GetResult();

            sut.MosaicImage.IsFrozen.Should().BeTrue();
            sut.MosaicImage.PixelWidth.Should().Be(18);
            sut.MosaicImage.PixelHeight.Should().Be(18);
            for (int column = 0; column < 3; column++) {
                for (int row = 0; row < 3; row++) {
                    Pixel(sut.MosaicImage, column * 6 + 1, row * 6 + 1)
                        .Should().Be(SourceColor(column * 16 + 1, row * 13 + 1));
                }
            }
            Pixel(sut.MosaicImage, 4, 1).Should().Be(Colors.Black);
            Pixel(sut.MosaicImage, 1, 4).Should().Be(Colors.Black);
            Pixel(sut.MosaicImage, 17, 17).A.Should().Be(0);
        }

        [Test]
        public void Initialize_TooSmallImage_FaultsTaskWithoutReplacingMosaic() {
            AberrationInspectorVM sut = new AberrationInspectorVM(Mock.Of<IProfileService>()) {
                CellSize = 4,
                SeparationSize = 2
            };
            Task first = sut.Initialize(CreateImage(18, 18, SourceColor));
            PumpUntil(() => first.IsCompleted);
            first.GetAwaiter().GetResult();
            BitmapSource original = sut.MosaicImage;

            Task invalid = sut.Initialize(CreateImage(17, 18, SourceColor));
            PumpUntil(() => invalid.IsCompleted);

            Action observe = () => invalid.GetAwaiter().GetResult();
            observe.Should().Throw<Exception>().WithMessage("Image too small for aberration inspector. Must be at least 18x18");
            sut.MosaicImage.Should().BeSameAs(original);
        }

        [TestCase(8, 16, true)]
        [TestCase(8, 16, false)]
        [TestCase(1280, 1280, true)]
        public void GetImage_FromWorker_PreservesMosaicTileLayoutAndScaling(int centerSize, int mosaicSize, bool frozenTiles) {
            Color[] colors = [Colors.Red, Colors.Lime, Colors.Blue, Colors.Yellow, Colors.Cyan, Colors.Magenta, Colors.Gray, Colors.White, Colors.Black];
            int calls = 0;
            LocalSurvey sut = new LocalSurvey((width, height, _) => {
                int index = calls++;
                BitmapSource image = CreateImage(
                    (int)(centerSize * width / 60),
                    (int)(centerSize * height / 60),
                    (_, _) => colors[index]);
                return Task.FromResult(frozenTiles ? image : image.Clone());
            });
            Coordinates coordinates = new Coordinates(40, 30, Epoch.J2000, Coordinates.RAType.Degrees);

            var imageTask = Task.Run(() => sut.GetImage("Mosaic", coordinates, 120, centerSize, centerSize, CancellationToken.None, null));
            PumpUntil(() => imageTask.IsCompleted);
            var image = imageTask.GetAwaiter().GetResult();

            calls.Should().Be(9);
            image.Name.Should().Be("Mosaic");
            image.Coordinates.Should().BeSameAs(coordinates);
            image.FoVWidth.Should().Be(120);
            image.FoVHeight.Should().Be(120);
            image.Image.IsFrozen.Should().BeTrue();
            image.Image.PixelWidth.Should().Be(mosaicSize);
            image.Image.PixelHeight.Should().Be(mosaicSize);
            int[] tileOrder = [1, 2, 3, 4, 0, 5, 6, 7, 8];
            int[] samplePositions = [mosaicSize / 8, mosaicSize / 2, mosaicSize * 7 / 8];
            for (int row = 0; row < 3; row++) {
                for (int column = 0; column < 3; column++) {
                    Pixel(image.Image, samplePositions[column], samplePositions[row])
                        .Should().Be(colors[tileOrder[row * 3 + column]]);
                }
            }
        }

        [Test]
        public void GetImage_AtSingleImageBoundary_ReturnsOriginalFrozenImage() {
            BitmapSource source = CreateImage(8, 8, SourceColor);
            int calls = 0;
            LocalSurvey sut = new LocalSurvey((_, _, _) => {
                calls++;
                return Task.FromResult(source);
            });

            var imageTask = Task.Run(() => sut.GetImage("Single", new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees), 60, 8, 8, CancellationToken.None, null));
            PumpUntil(() => imageTask.IsCompleted);

            imageTask.GetAwaiter().GetResult().Image.Should().BeSameAs(source);
            calls.Should().Be(1);
        }

        [Test]
        public void GetImage_CanceledAfterDownload_DoesNotRenderMosaic() {
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            BitmapSource source = CreateImage(8, 8, SourceColor);
            int calls = 0;
            LocalSurvey sut = new LocalSurvey((_, _, _) => {
                if (++calls == 9) {
                    cancellation.Cancel();
                }
                return Task.FromResult(source);
            });

            var imageTask = Task.Run(() => sut.GetImage("Canceled", new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees), 120, 8, 8, cancellation.Token, null));
            PumpUntil(() => imageTask.IsCompleted);

            Action observe = () => imageTask.GetAwaiter().GetResult();
            observe.Should().Throw<OperationCanceledException>();
            calls.Should().Be(9);
        }

        [Test]
        public void GetImage_CanceledDuringDownload_PropagatesCancellation() {
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            int calls = 0;
            LocalSurvey sut = new LocalSurvey(async (_, _, token) => {
                Interlocked.Increment(ref calls);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                throw new AssertionException("The canceled download should not complete.");
            });
            var imageTask = Task.Run(() => sut.GetImage("Canceled", new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees), 120, 8, 8, cancellation.Token, null));
            try {
                PumpUntil(() => Volatile.Read(ref calls) == 9);
                imageTask.IsCompleted.Should().BeFalse();
            } finally {
                cancellation.Cancel();
            }
            PumpUntil(() => imageTask.IsCompleted);

            Action observe = () => imageTask.GetAwaiter().GetResult();
            observe.Should().Throw<OperationCanceledException>();
        }

        private static ImageControlVM CreateImageControl(ImageSettings settings) {
            var profile = new Mock<IProfile>();
            profile.SetupGet(x => x.ImageSettings).Returns(settings);
            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            return new ImageControlVM(profileService.Object, Mock.Of<ICameraMediator>(), Mock.Of<ITelescopeMediator>(),
                Mock.Of<IImagingMediator>(), Mock.Of<IApplicationStatusMediator>());
        }

        private static void ExecutePrepareImageCommand(ImageControlVM imageControl) {
            Task command = Dispatcher.CurrentDispatcher.InvokeAsync(() => imageControl.PrepareImageCommand.ExecuteAsync(null)).Task.Unwrap();
            PumpUntil(() => command.IsCompleted);
            command.GetAwaiter().GetResult();
        }

        private static Uri ImagePath(int index) {
            return new Uri(Path.Combine(TestContext.CurrentContext.WorkDirectory, $"rendering-{index}.fits"));
        }

        private static Color SourceColor(int x, int y) {
            return Color.FromRgb((byte)(x * 5), (byte)(y * 7), (byte)((x + y) * 3));
        }

        private static BitmapSource CreateImage(int width, int height, Func<int, int, Color> colorAt) {
            byte[] pixels = new byte[width * height * 4];
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Color color = colorAt(x, y);
                    int offset = (y * width + x) * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                    pixels[offset + 3] = color.A;
                }
            }
            BitmapSource image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            image.Freeze();
            return image;
        }

        private static Color Pixel(BitmapSource image, int x, int y) {
            byte[] pixel = new byte[4];
            image.Format.Should().Be(PixelFormats.Pbgra32);
            image.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
            return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
        }

        private static void PumpUntil(Func<bool> completed) {
            Stopwatch timeout = Stopwatch.StartNew();
            while (!completed()) {
                if (timeout.Elapsed > TimeSpan.FromSeconds(10)) {
                    throw new TimeoutException("The image rendering operation did not complete.");
                }
                DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
        }

        private sealed class LocalSurvey : MosaicSkySurvey {
            private readonly Func<double, double, CancellationToken, Task<BitmapSource>> getImage;

            public LocalSurvey(Func<double, double, CancellationToken, Task<BitmapSource>> getImage) {
                this.getImage = getImage;
            }

            protected override Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct, int width, int height) {
                return getImage(fovW, fovH, ct);
            }
        }
    }
}