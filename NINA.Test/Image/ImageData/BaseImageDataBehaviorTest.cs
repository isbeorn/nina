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
        /// Verifies raw file loading does not inherit the camera bit-scaling preference.
        /// </summary>
        [Test]
        public async Task FromFile_RawFilePassesBitScalingFalseToRawConverter() {
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "BaseImageDataBehaviorTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try {
                string rawPath = Path.Combine(directory, "raw.DNG");
                File.WriteAllBytes(rawPath, new byte[] { 10, 20, 30 });

                bool? capturedBitScaling = null;
                var expectedImageData = Mock.Of<IImageData>();
                var rawConverter = new Mock<IRawConverter>();
                rawConverter
                    .Setup(x => x.Convert(
                        It.IsAny<MemoryStream>(),
                        12,
                        It.IsAny<bool>(),
                        "dng",
                        It.IsAny<ImageMetaData>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<MemoryStream, int, bool, string, ImageMetaData, CancellationToken>(
                        (_, _, bitScaling, _, _, _) => capturedBitScaling = bitScaling)
                    .ReturnsAsync(expectedImageData);

                var loaded = await BaseImageData.FromFile(rawPath, 12, isBayered: true, rawConverter.Object, Mock.Of<IImageDataFactory>());

                loaded.Should().BeSameAs(expectedImageData);
                capturedBitScaling.Should().BeFalse();
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies the legacy RAWExposureData constructor keeps old callers on unscaled RAW conversion.
        /// </summary>
        [Test]
        public async Task RawExposureData_ObsoleteConstructorPassesBitScalingFalseToRawConverter() {
            bool? capturedBitScaling = null;
            var expectedImageData = Mock.Of<IImageData>();
            var rawConverter = new Mock<IRawConverter>();
            rawConverter
                .Setup(x => x.Convert(
                    It.IsAny<MemoryStream>(),
                    12,
                    It.IsAny<bool>(),
                    "dng",
                    It.IsAny<ImageMetaData>(),
                    It.IsAny<CancellationToken>()))
                .Callback<MemoryStream, int, bool, string, ImageMetaData, CancellationToken>(
                    (_, _, bitScaling, _, _, _) => capturedBitScaling = bitScaling)
                .ReturnsAsync(expectedImageData);

#pragma warning disable CS0618
            var exposureData = new RAWExposureData(
                rawConverter.Object,
                new byte[] { 10, 20, 30 },
                "dng",
                12,
                new ImageMetaData(),
                Mock.Of<IImageDataFactory>());
#pragma warning restore CS0618

            var imageData = await exposureData.ToImageData();

            imageData.Should().BeSameAs(expectedImageData);
            capturedBitScaling.Should().BeFalse();
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
        /// Verifies raw saves prefer original RAW bytes while the native camera RAW option is enabled.
        /// </summary>
        [Test]
        public async Task SaveToDisk_RawDataUsesOriginalBytesWhenNativeRawSaveIsEnabled() {
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "BaseImageDataSaveTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try {
                var imageArray = new ImageArray(new ushort[] { 1, 2, 3, 4 }, rawData: new byte[] { 10, 20, 30 }, rawType: "cr2");
                BaseImageData imageData = CreateImageData(CreateMetadata(), hfr: double.NaN, detectedStars: -1, imageArray);
                var saveInfo = new FileSaveInfo {
                    FilePath = directory,
                    FilePattern = "raw-frame",
                    FileType = FileTypeEnum.XISF,
                    SaveNativeCameraRaw = true
                };

                string savedPath = await imageData.SaveToDisk(saveInfo, CancellationToken.None, forceFileType: false);

                Path.GetExtension(savedPath).Should().Be(".cr2");
                File.ReadAllBytes(savedPath).Should().Equal(10, 20, 30);
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies raw-capable camera data is saved in the selected file format when native RAW saving is disabled.
        /// </summary>
        [Test]
        public async Task SaveToDisk_RawDataUsesRequestedFileTypeWhenNativeRawSaveIsDisabled() {
            string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "BaseImageDataSaveTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try {
                var imageArray = new ImageArray(new ushort[] { 1, 2, 3, 4 }, rawData: new byte[] { 10, 20, 30 }, rawType: "cr2");
                BaseImageData imageData = CreateImageData(CreateMetadata(), hfr: double.NaN, detectedStars: -1, imageArray);
                var saveInfo = new FileSaveInfo {
                    FilePath = directory,
                    FilePattern = "converted-frame",
                    FileType = FileTypeEnum.XISF,
                    SaveNativeCameraRaw = false
                };

                string savedPath = await imageData.SaveToDisk(saveInfo, CancellationToken.None, forceFileType: false);

                Path.GetExtension(savedPath).Should().Be(".xisf");
                File.Exists(savedPath).Should().BeTrue();
                File.ReadAllBytes(savedPath).Should().NotEqual(new byte[] { 10, 20, 30 });
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies FileSaveInfo carries the camera-scoped native RAW save preference into the image save layer.
        /// </summary>
        [Test]
        public void FileSaveInfo_CopiesNativeRawSaveSettingFromActiveProfile() {
            var profile = new NINA.Profile.Profile();
            profile.CameraSettings.SaveNativeCameraRaw = false;

            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile);

            var saveInfo = new FileSaveInfo(profileService.Object);

            saveInfo.SaveNativeCameraRaw.Should().BeFalse();
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
