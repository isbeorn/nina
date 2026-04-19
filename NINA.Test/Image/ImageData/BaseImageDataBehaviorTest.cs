using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Image.FileFormat;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Test.Image.ImageData {

    [TestFixture]
    public class BaseImageDataBehaviorTest {

        /// <summary>
        /// Verifies supported-image detection is case-insensitive, recognizes raw formats, rejects unsupported extensions, and throws for missing files.
        /// </summary>
        [Test]
        public void FileIsSupported_RecognizesSupportedExtensionsAndGuardsMissingFiles() {
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "BaseImageDataBehaviorTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try {
                string fits = Path.Combine(directory, "frame.FITS");
                string dng = Path.Combine(directory, "raw.DNG");
                string text = Path.Combine(directory, "notes.txt");
                File.WriteAllText(fits, string.Empty);
                File.WriteAllText(dng, string.Empty);
                File.WriteAllText(text, string.Empty);

                BaseImageData.FileIsSupported(fits).Should().BeTrue();
                BaseImageData.FileIsSupported(dng).Should().BeTrue();
                BaseImageData.FileIsSupported(text).Should().BeFalse();
                Action missing = () => BaseImageData.FileIsSupported(Path.Combine(directory, "missing.fit"));
                missing.Should().Throw<FileNotFoundException>();
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies image filename pattern data combines capture metadata and star analysis values without evaluating DateMinus12 near DateTime.MinValue.
        /// </summary>
        [Test]
        public void GetImagePatterns_UsesMetadataAndAnalysisWhileGuardingDateMinus12AtMinimumDate() {
            ImageMetaData metadata = CreateMetadata();
            metadata.Image.ExposureStart = DateTime.MinValue.AddHours(1);
            BaseImageData imageData = CreateImageData(metadata, hfr: 2.34, detectedStars: 42);

            ImagePatterns patterns = imageData.GetImagePatterns();
            string fileName = patterns.GetImageFileString(
                $"{ImagePatternKeys.ImageType}_{ImagePatternKeys.Filter}_{ImagePatternKeys.Binning}_{ImagePatternKeys.HFR}_{ImagePatternKeys.StarCount}_{ImagePatternKeys.DateMinus12}");

            fileName.Should().Be("LIGHT_Ha_2x2_2.34_42_");
        }

        /// <summary>
        /// Verifies raw saves prefer original RAW bytes unless the caller explicitly forces the requested file type.
        /// </summary>
        [Test]
        public async Task SaveToDisk_RawDataUsesOriginalBytesUnlessFileTypeIsForced() {
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "BaseImageDataSaveTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try {
                var imageArray = new ImageArray(new ushort[] { 1, 2, 3, 4 }, rawData: new byte[] { 10, 20, 30 }, rawType: "cr2");
                BaseImageData imageData = CreateImageData(CreateMetadata(), hfr: double.NaN, detectedStars: -1, imageArray);
                var saveInfo = new FileSaveInfo {
                    FilePath = directory,
                    FilePattern = "raw-frame",
                    FileType = NINA.Core.Enum.FileTypeEnum.TIFF
                };

                string savedPath = await imageData.SaveToDisk(saveInfo, CancellationToken.None, forceFileType: false);

                Path.GetExtension(savedPath).Should().Be(".cr2");
                File.ReadAllBytes(savedPath).Should().Equal(10, 20, 30);
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies rendered star detection forwards profile crop settings, annotation limits, and analysis updates to the pluggable behaviors.
        /// </summary>
        [Test]
        public async Task RenderedImage_DetectStarsBuildsParamsAnnotatesAndUpdatesAnalysis() {
            var analysis = new StarDetectionAnalysisStub();
            var detectionResult = new StarDetectionResult {
                AverageHFR = 1.23,
                DetectedStars = 5,
                StarList = new System.Collections.Generic.List<DetectedStar>()
            };
            StarDetectionParams capturedParams = null;
            int capturedMaxStars = 0;

            var starDetection = new Mock<IStarDetection>();
            starDetection.Setup(x => x.CreateAnalysis()).Returns(analysis);
            starDetection
                .Setup(x => x.Detect(
                    It.IsAny<IRenderedImage>(),
                    It.IsAny<System.Windows.Media.PixelFormat>(),
                    It.IsAny<StarDetectionParams>(),
                    It.IsAny<IProgress<ApplicationStatus>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IRenderedImage, System.Windows.Media.PixelFormat, StarDetectionParams, IProgress<ApplicationStatus>, CancellationToken>(
                    (_, _, p, _, _) => capturedParams = p)
                .ReturnsAsync(detectionResult);

            var starAnnotator = new Mock<IStarAnnotator>();
            starAnnotator
                .Setup(x => x.GetAnnotatedImage(
                    It.IsAny<StarDetectionParams>(),
                    detectionResult,
                    It.IsAny<BitmapSource>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Callback<StarDetectionParams, StarDetectionResult, BitmapSource, int, CancellationToken>(
                    (_, _, _, maxStars, _) => capturedMaxStars = maxStars)
                .ReturnsAsync((StarDetectionParams _, StarDetectionResult _, BitmapSource image, int _, CancellationToken _) => image);

            IProfileService profileService = CreateProfileService(innerCrop: 0.4, outerCrop: 0.8, brightestStars: 7, annotateUnlimitedStars: false);
            var imageData = new BaseImageData(
                new ushort[] { 1, 2, 3, 4 },
                width: 2,
                height: 2,
                bitDepth: 16,
                isBayered: false,
                metaData: CreateMetadata(),
                profileService,
                starDetection.Object,
                starAnnotator.Object);

            IRenderedImage rendered = imageData.RenderImage();
            IRenderedImage returned = await rendered.DetectStars(
                annotateImage: true,
                sensitivity: StarSensitivityEnum.High,
                noiseReduction: NoiseReductionEnum.Median);

            returned.Should().BeSameAs(rendered);
            capturedParams.Should().NotBeNull();
            capturedParams.Sensitivity.Should().Be(StarSensitivityEnum.High);
            capturedParams.NoiseReduction.Should().Be(NoiseReductionEnum.Median);
            capturedParams.UseROI.Should().BeTrue();
            capturedParams.InnerCropRatio.Should().Be(0.4);
            capturedParams.OuterCropRatio.Should().Be(0.8);
            capturedParams.NumberOfAFStars.Should().Be(7);
            capturedMaxStars.Should().Be(200);
            starDetection.Verify(x => x.UpdateAnalysis(analysis, capturedParams, detectionResult), Times.Once);
            starAnnotator.Verify(x => x.GetAnnotatedImage(capturedParams, detectionResult, rendered.OriginalImage, 200, It.IsAny<CancellationToken>()), Times.Once);
        }

        private static BaseImageData CreateImageData(ImageMetaData metadata, double hfr, int detectedStars, IImageArray imageArray = null) {
            var analysis = new StarDetectionAnalysisStub {
                HFR = hfr,
                DetectedStars = detectedStars
            };

            var starDetection = new Mock<IStarDetection>();
            starDetection.Setup(x => x.CreateAnalysis()).Returns(analysis);

            return new BaseImageData(
                imageArray ?? new ImageArray(new ushort[] { 1, 2, 3, 4 }),
                width: 2,
                height: 2,
                bitDepth: 16,
                isBayered: false,
                metadata,
                profileService: null,
                starDetection: starDetection.Object,
                starAnnotator: Mock.Of<IStarAnnotator>());
        }

        private static IProfileService CreateProfileService(double innerCrop, double outerCrop, int brightestStars, bool annotateUnlimitedStars) {
            var focuserSettings = new Mock<IFocuserSettings>();
            focuserSettings.SetupGet(x => x.AutoFocusInnerCropRatio).Returns(innerCrop);
            focuserSettings.SetupGet(x => x.AutoFocusOuterCropRatio).Returns(outerCrop);
            focuserSettings.SetupGet(x => x.AutoFocusUseBrightestStars).Returns(brightestStars);

            var imageSettings = new Mock<IImageSettings>();
            imageSettings.SetupGet(x => x.AnnotateUnlimitedStars).Returns(annotateUnlimitedStars);

            var profile = new Mock<IProfile>();
            profile.SetupGet(x => x.FocuserSettings).Returns(focuserSettings.Object);
            profile.SetupGet(x => x.ImageSettings).Returns(imageSettings.Object);

            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            return profileService.Object;
        }

        private static ImageMetaData CreateMetadata() {
            var metadata = new ImageMetaData();
            metadata.Image.ImageType = "LIGHT";
            metadata.Image.ExposureTime = 120;
            metadata.Image.ExposureNumber = 7;
            metadata.Camera.BinX = 2;
            metadata.Camera.BinY = 2;
            metadata.FilterWheel.Filter = "Ha";
            metadata.Sequence.Title = "Night Plan";
            return metadata;
        }

        private class StarDetectionAnalysisStub : IStarDetectionAnalysis {
            public event PropertyChangedEventHandler? PropertyChanged {
                add { }
                remove { }
            }
            public double HFR { get; set; }
            public double FWHM { get; set; }
            public double Eccentricity { get; set; }
            public double HFRStDev { get; set; }
            public int DetectedStars { get; set; }
            public System.Collections.Generic.List<DetectedStar> StarList { get; set; } = new System.Collections.Generic.List<DetectedStar>();
        }
    }
}
