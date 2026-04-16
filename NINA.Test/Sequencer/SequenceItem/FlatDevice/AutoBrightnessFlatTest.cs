using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.FlatDevice {
    [TestFixture]
    public class AutoBrightnessFlatTest {
        Mock<IProfileService> profileService;
        Mock<ICameraMediator> cameraMediator;
        Mock<IImagingMediator> imagingMediator;
        Mock<IFlatDeviceMediator> flatDeviceMediator;
        Mock<IImageSaveMediator> imageSaveMediator;
        Mock<IImageHistoryVM> imageHistoryVM;
        Mock<IFilterWheelMediator> filterWheelMediator;

        AutoBrightnessFlat sut;
        [SetUp]
        public void Setup() {
            profileService = new Mock<IProfileService>();
            cameraMediator = new Mock<ICameraMediator>();
            imagingMediator = new Mock<IImagingMediator>();
            flatDeviceMediator = new Mock<IFlatDeviceMediator>();
            imageSaveMediator = new Mock<IImageSaveMediator>();
            imageHistoryVM = new Mock<IImageHistoryVM>();
            filterWheelMediator = new Mock<IFilterWheelMediator>();

            var imageFileSettings = new Mock<IImageFileSettings>();
            imageFileSettings.SetupGet(x => x.FilePath).Returns(TestContext.CurrentContext.TestDirectory);
            profileService.Setup(x => x.ActiveProfile.ImageFileSettings).Returns(imageFileSettings.Object);
            cameraMediator.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });
            flatDeviceMediator.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo());

            sut = new AutoBrightnessFlat(profileService.Object, cameraMediator.Object, imagingMediator.Object, imageSaveMediator.Object, imageHistoryVM.Object, filterWheelMediator.Object, flatDeviceMediator.Object);
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();

            sut.KeepPanelClosed = true;            
            sut.MinBrightness = 111;
            sut.MaxBrightness = 222;
            sut.HistogramTargetPercentage = 0.7;
            sut.HistogramTolerancePercentage = 0.3;


            var item2 = (AutoBrightnessFlat)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().Be(sut.Icon);
            item2.KeepPanelClosed.Should().Be(sut.KeepPanelClosed);
            item2.MinBrightness.Should().Be(sut.MinBrightness);
            item2.MaxBrightness.Should().Be(sut.MaxBrightness);
            item2.HistogramTargetPercentage.Should().Be(sut.HistogramTargetPercentage);
            item2.HistogramTolerancePercentage.Should().Be(sut.HistogramTolerancePercentage);
        }

        [Test]
        public async Task Execute_ImageIsAlreadyWithinTolerance_TakeImagesWithExpectedExposureTime() {
            sut.MinBrightness = 0;
            sut.MaxBrightness = 10000;

            var exposureData = new Mock<IExposureData>();
            exposureData.SetupGet(x => x.BitDepth).Returns(16);
            var imageData = new Mock<IImageData>();
            var statistics = new Mock<IImageStatistics>();
            statistics.SetupGet(x => x.Mean).Returns(30000);

            imagingMediator.Setup(x => x.CaptureImage(It.IsAny<CaptureSequence>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>(), "")).ReturnsAsync(exposureData.Object);
            imageData.Setup(x => x.Statistics).Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => statistics.Object));
            exposureData.Setup(x => x.ToImageData(It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>())).ReturnsAsync(imageData.Object);

            await sut.Execute(default, default);

            sut.GetSetBrightnessItem().Brightness.Should().Be(5000);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_ImageNeedsThreeUpwardsIterationsToBeWithin_TakeImagesWithExpectedExposureTime() {
            sut.MinBrightness = 0;
            sut.MaxBrightness = 10000;

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

            sut.GetSetBrightnessItem().Brightness.Should().Be(8750);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_ImageNeedsThreeUpwardsButOvershootsIterationsToBeWithin_TakeImagesWithExpectedExposureTime() {
            sut.MinBrightness = 0;
            sut.MaxBrightness = 10000;

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

            sut.GetSetBrightnessItem().Brightness.Should().Be(8125);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_ImageNeedsThreeDownwardsIterationsToBeWithin_TakeImagesWithExpectedExposureTime() {
            sut.MinBrightness = 0;
            sut.MaxBrightness = 10000;

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

            sut.GetSetBrightnessItem().Brightness.Should().Be(1250);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Execute_ImageNeedsThreeDownwardsButOvershootsIterationsToBeWithin_TakeImagesWithExpectedExposureTime() {
            sut.MinBrightness = 0;
            sut.MaxBrightness = 10000;

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

            sut.GetSetBrightnessItem().Brightness.Should().Be(1875);
            imageSaveMediator.Verify(x => x.Enqueue(imageData.Object, It.IsAny<Task<IRenderedImage>>(), It.IsAny<IProgress<ApplicationStatus>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies deserialization rebuilds the immutable Auto Brightness Flat instruction set from a clean item, condition, and trigger state.
        /// </summary>
        [Test]
        public void OnDeserializing_ClearsExistingImmutableChildrenConditionsAndTriggers() {
            sut.Add(new LoopCondition());
            sut.Add(Mock.Of<ISequenceTrigger>());

            sut.OnDeserializing(new StreamingContext());

            sut.Items.Should().BeEmpty();
            sut.Conditions.Should().BeEmpty();
            sut.Triggers.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies retry and error-behavior settings propagate to every child instruction in the Auto Brightness Flat set.
        /// </summary>
        [Test]
        public void ErrorBehaviorAndAttempts_PropagateToImmutableChildrenAndIgnoreInvalidAttempts() {
            sut.ErrorBehavior = InstructionErrorBehavior.AbortOnError;
            sut.Attempts = 4;

            sut.Items.Should().OnlyContain(x => x.ErrorBehavior == InstructionErrorBehavior.AbortOnError);
            sut.Items.Should().OnlyContain(x => x.Attempts == 4);

            sut.Attempts = 0;

            sut.Attempts.Should().Be(4);
            sut.Items.Should().OnlyContain(x => x.Attempts == 4);
        }

        /// <summary>
        /// Verifies histogram percentage inputs are clamped to their supported zero-to-one range and the panel-closed setting is retained.
        /// </summary>
        [Test]
        public void HistogramPercentagesAndKeepPanelClosed_ClampAndStoreUserSettings() {
            sut.HistogramTargetPercentage = -0.5;
            sut.HistogramTolerancePercentage = 1.5;
            sut.KeepPanelClosed = true;

            sut.HistogramTargetPercentage.Should().Be(0);
            sut.HistogramTolerancePercentage.Should().Be(1);
            sut.KeepPanelClosed.Should().BeTrue();

            sut.HistogramTargetPercentage = 1.5;
            sut.HistogramTolerancePercentage = -0.5;

            sut.HistogramTargetPercentage.Should().Be(1);
            sut.HistogramTolerancePercentage.Should().Be(0);
        }

        /// <summary>
        /// Verifies validation wires flat-device brightness limits into expressions and reports an invalid min/max brightness range.
        /// </summary>
        [Test]
        public void Validate_UpdatesBrightnessExpressionRangesAndReportsInvalidInputRange() {
            flatDeviceMediator.Setup(x => x.GetInfo()).Returns(new FlatDeviceInfo {
                Connected = true,
                MinBrightness = 10,
                MaxBrightness = 90,
                SupportsOnOff = true,
                SupportsOpenClose = true
            });
            sut.MinBrightness = 80;
            sut.MaxBrightness = 20;

            sut.Validate().Should().BeFalse();

            sut.Issues.Should().Contain(x => x == NINA.Core.Locale.Loc.Instance["Lbl_SequenceItem_FlatDevice_AutoBrightnessFlat_Validation_InputRangeInvalid"]);
            sut.MinBrightnessExpression.Range.Should().Equal(10, 90, 0);
            sut.MaxBrightnessExpression.Range.Should().Equal(10, 90, 0);
            sut.MinBrightnessExpression.DefaultString.Should().Be("{10}");
            sut.MaxBrightnessExpression.DefaultString.Should().Be("{90}");
        }
    }
}
