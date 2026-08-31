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
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces.Mediator;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [NonParallelizable]
    [Apartment(ApartmentState.STA)]
    public class ImageControlVMBayerPatternTest {

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            var application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

            application.Resources["PuzzlePieceSVG"] = new GeometryGroup();
            application.Resources["PictureSVG"] = new GeometryGroup();
        }

        [TestCase(0, 0, SensorType.RGGB)]
        [TestCase(1, 0, SensorType.GRBG)]
        [TestCase(0, 1, SensorType.GBRG)]
        [TestCase(1, 1, SensorType.BGGR)]
        public async Task PrepareImage_AutoPatternWithBayerOffsets_UsesShiftedMetadataPattern(
            int offsetX,
            int offsetY,
            SensorType expectedPattern) {
            var (sut, _) = CreateImageControl(BayerPatternEnum.Auto);
            var metadata = new ImageMetaData();
            metadata.Camera.SensorType = SensorType.RGGB;
            metadata.Camera.BayerOffsetX = offsetX;
            metadata.Camera.BayerOffsetY = offsetY;

            SensorType selectedPattern = await PrepareImage(sut, metadata);

            selectedPattern.Should().Be(expectedPattern);
            metadata.Camera.SensorType.Should().Be(SensorType.RGGB);
            metadata.Camera.BayerOffsetX.Should().Be(offsetX);
            metadata.Camera.BayerOffsetY.Should().Be(offsetY);
        }

        [Test]
        public async Task PrepareImage_AutoPatternWithoutUsableMetadata_UsesShiftedCameraPattern() {
            var (sut, _) = CreateImageControl(BayerPatternEnum.Auto);
            sut.UpdateDeviceInfo(new CameraInfo {
                SensorType = SensorType.RGGB,
                BayerOffsetX = 0,
                BayerOffsetY = 1
            });
            var metadata = new ImageMetaData();

            SensorType selectedPattern = await PrepareImage(sut, metadata);

            selectedPattern.Should().Be(SensorType.GBRG);
        }

        [Test]
        public async Task PrepareImage_ExplicitPattern_IgnoresMetadataAndCameraOffsets() {
            var (sut, _) = CreateImageControl(BayerPatternEnum.BGGR);
            sut.UpdateDeviceInfo(new CameraInfo {
                SensorType = SensorType.RGGB,
                BayerOffsetX = 1,
                BayerOffsetY = 0
            });
            var metadata = new ImageMetaData();
            metadata.Camera.SensorType = SensorType.RGGB;
            metadata.Camera.BayerOffsetX = 0;
            metadata.Camera.BayerOffsetY = 1;

            SensorType selectedPattern = await PrepareImage(sut, metadata);

            selectedPattern.Should().Be(SensorType.BGGR);
        }

        private static (ImageControlVM ImageControl, Mock<ICameraMediator> CameraMediator) CreateImageControl(
            BayerPatternEnum configuredPattern) {
            var imageSettings = new Mock<IImageSettings>();
            imageSettings.SetupGet(x => x.DebayerImage).Returns(true);
            imageSettings.SetupGet(x => x.AutoStretch).Returns(false);
            imageSettings.SetupGet(x => x.DetectStars).Returns(false);

            var cameraSettings = new Mock<ICameraSettings>();
            cameraSettings.SetupGet(x => x.BayerPattern).Returns(configuredPattern);

            var profile = new Mock<IProfile>();
            profile.SetupGet(x => x.ImageSettings).Returns(imageSettings.Object);
            profile.SetupGet(x => x.CameraSettings).Returns(cameraSettings.Object);

            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);

            var cameraMediator = new Mock<ICameraMediator>();
            var imageControl = new ImageControlVM(
                profileService.Object,
                cameraMediator.Object,
                Mock.Of<ITelescopeMediator>(),
                Mock.Of<IImagingMediator>(),
                Mock.Of<IApplicationStatusMediator>());

            cameraMediator.Verify(x => x.RegisterConsumer(imageControl), Times.Once);
            return (imageControl, cameraMediator);
        }

        private static async Task<SensorType> PrepareImage(ImageControlVM imageControl, ImageMetaData metadata) {
            var imageData = new Mock<IImageData>();
            imageData.SetupGet(x => x.Properties).Returns(new ImageProperties(2, 2, 16, true, 0, 0));
            imageData.SetupGet(x => x.MetaData).Returns(metadata);

            SensorType? selectedPattern = null;
            var debayeredImage = new Mock<IDebayeredImage>();
            var renderedImage = new Mock<IRenderedImage>();
            renderedImage
                .Setup(x => x.Debayer(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<SensorType>()))
                .Callback<bool, bool, SensorType>((_, _, pattern) => selectedPattern = pattern)
                .Returns(debayeredImage.Object);
            imageData.Setup(x => x.RenderImage()).Returns(renderedImage.Object);

            await imageControl.PrepareImage(
                imageData.Object,
                new PrepareImageParameters(autoStretch: false, detectStars: false),
                CancellationToken.None);

            return selectedPattern ?? throw new AssertionException("Debayer was not invoked.");
        }
    }
}
