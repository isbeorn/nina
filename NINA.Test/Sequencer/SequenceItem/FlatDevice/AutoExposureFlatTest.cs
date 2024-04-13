using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.FlatDevice {
    [TestFixture]
    public class AutoExposureFlatTest {
        Mock<IProfileService> profileService;
        Mock<ICameraMediator> cameraMediator;
        Mock<IImagingMediator> imagingMediator;
        Mock<IFlatDeviceMediator> flatDeviceMediator;
        Mock<IImageSaveMediator> imageSaveMediator;
        Mock<IImageHistoryVM> imageHistoryVM;
        Mock<IFilterWheelMediator> filterWheelMediator;

        AutoExposureFlat sut;
        [SetUp]
        public void Setup() {
            profileService = new Mock<IProfileService>();
            cameraMediator = new Mock<ICameraMediator>();
            imagingMediator = new Mock<IImagingMediator>();
            flatDeviceMediator = new Mock<IFlatDeviceMediator>();
            imageSaveMediator = new Mock<IImageSaveMediator>();
            imageHistoryVM = new Mock<IImageHistoryVM>();
            filterWheelMediator = new Mock<IFilterWheelMediator>();

            profileService.Setup(x => x.ActiveProfile.ImageFileSettings).Returns(new Mock<IImageFileSettings>().Object);
            cameraMediator.Setup(x => x.GetInfo()).Returns(new CameraInfo());
            flatDeviceMediator.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo());

            sut = new AutoExposureFlat(profileService.Object, cameraMediator.Object, imagingMediator.Object, imageSaveMediator.Object, imageHistoryVM.Object, filterWheelMediator.Object, flatDeviceMediator.Object);
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();

            sut.KeepPanelClosed = true;
            sut.MinExposure = 111;
            sut.MaxExposure = 222;
            sut.HistogramTargetPercentage = 0.7;
            sut.HistogramTolerancePercentage = 0.3;
            

            var item2 = (AutoExposureFlat)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().Be(sut.Icon);
            item2.KeepPanelClosed.Should().Be(sut.KeepPanelClosed);
            item2.MinExposure.Should().Be(sut.MinExposure);
            item2.MaxExposure.Should().Be(sut.MaxExposure);
            item2.HistogramTargetPercentage.Should().Be(sut.HistogramTargetPercentage);
            item2.HistogramTolerancePercentage.Should().Be(sut.HistogramTolerancePercentage);
        }

        [Test]
        public async Task Execute_ImageIsAlreadyWithinTolerance_TakeImagesWithExpectedExposureTime() {
            sut.MinExposure = 0;
            sut.MaxExposure = 10;

            var exposureData = new Mock<IExposureData>();
            exposureData.SetupGet(x => x.BitDepth).Returns(16);
            var imageData = new Mock<IImageData>();
            var statistics = new Mock<IImageStatistics>();
            statistics.SetupGet(x => x.Mean).Returns(30000);

            imagingMediator.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), "")).ReturnsAsync(exposureData.Object);
            imageData.Setup(x => x.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics.Object));
            exposureData.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(imageData.Object);

            await sut.Execute(default, default);

            sut.GetExposureItem().ExposureTime.Should().Be(5);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_ImageNeedsThreeUpwardsIterationsToBeWithin_TakeImagesWithExpectedExposureTime() {
            sut.MinExposure = 0;
            sut.MaxExposure = 10;

            var exposureData = new Mock<IExposureData>();
            exposureData.SetupGet(x => x.BitDepth).Returns(16);
            var imageData = new Mock<IImageData>();

            var statistics1 = new Mock<IImageStatistics>();
            statistics1.SetupGet(x => x.Mean).Returns(10000);

            var statistics2 = new Mock<IImageStatistics>();
            statistics2.SetupGet(x => x.Mean).Returns(20000);

            var statistics3 = new Mock<IImageStatistics>();
            statistics3.SetupGet(x => x.Mean).Returns(30000);

            imagingMediator.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), "")).ReturnsAsync(exposureData.Object);
            imageData.SetupSequence(x => x.Statistics)
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics1.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics2.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics3.Object));
            exposureData.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(imageData.Object);

            await sut.Execute(default, default);

            sut.GetExposureItem().ExposureTime.Should().Be(8.75);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_ImageNeedsThreeUpwardsButOvershootsIterationsToBeWithin_TakeImagesWithExpectedExposureTime() {
            sut.MinExposure = 0;
            sut.MaxExposure = 10;

            var exposureData = new Mock<IExposureData>();
            exposureData.SetupGet(x => x.BitDepth).Returns(16);
            var imageData = new Mock<IImageData>();

            var statistics1 = new Mock<IImageStatistics>();
            statistics1.SetupGet(x => x.Mean).Returns(10000);

            var statistics2 = new Mock<IImageStatistics>();
            statistics2.SetupGet(x => x.Mean).Returns(20000);

            var statistics3 = new Mock<IImageStatistics>();
            statistics3.SetupGet(x => x.Mean).Returns(50000);

            var statistics4 = new Mock<IImageStatistics>();
            statistics4.SetupGet(x => x.Mean).Returns(30000);

            imagingMediator.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), "")).ReturnsAsync(exposureData.Object);
            imageData.SetupSequence(x => x.Statistics)
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics1.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics2.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics3.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics4.Object));
            exposureData.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(imageData.Object);

            await sut.Execute(default, default);

            sut.GetExposureItem().ExposureTime.Should().Be(8.125);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_ImageNeedsThreeDownwardsIterationsToBeWithin_TakeImagesWithExpectedExposureTime() {
            sut.MinExposure = 0;
            sut.MaxExposure = 10;

            var exposureData = new Mock<IExposureData>();
            exposureData.SetupGet(x => x.BitDepth).Returns(16);
            var imageData = new Mock<IImageData>();

            var statistics1 = new Mock<IImageStatistics>();
            statistics1.SetupGet(x => x.Mean).Returns(50000);

            var statistics2 = new Mock<IImageStatistics>();
            statistics2.SetupGet(x => x.Mean).Returns(40000);

            var statistics3 = new Mock<IImageStatistics>();
            statistics3.SetupGet(x => x.Mean).Returns(30000);

            imagingMediator.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), "")).ReturnsAsync(exposureData.Object);
            imageData.SetupSequence(x => x.Statistics)
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics1.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics2.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics3.Object));
            exposureData.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(imageData.Object);

            await sut.Execute(default, default);

            sut.GetExposureItem().ExposureTime.Should().Be(1.25);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_ImageNeedsThreeDownwardsButOvershootsIterationsToBeWithin_TakeImagesWithExpectedExposureTime() {
            sut.MinExposure = 0;
            sut.MaxExposure = 10;

            var exposureData = new Mock<IExposureData>();
            exposureData.SetupGet(x => x.BitDepth).Returns(16);
            var imageData = new Mock<IImageData>();

            var statistics1 = new Mock<IImageStatistics>();
            statistics1.SetupGet(x => x.Mean).Returns(50000);

            var statistics2 = new Mock<IImageStatistics>();
            statistics2.SetupGet(x => x.Mean).Returns(40000);

            var statistics3 = new Mock<IImageStatistics>();
            statistics3.SetupGet(x => x.Mean).Returns(10000);

            var statistics4 = new Mock<IImageStatistics>();
            statistics4.SetupGet(x => x.Mean).Returns(30000);

            imagingMediator.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), "")).ReturnsAsync(exposureData.Object);
            imageData.SetupSequence(x => x.Statistics)
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics1.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics2.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics3.Object))
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics4.Object));
            exposureData.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(imageData.Object);

            await sut.Execute(default, default);

            sut.GetExposureItem().ExposureTime.Should().Be(1.875);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
