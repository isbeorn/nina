using FluentAssertions;
using Moq;
using NINA.Core.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Image {
    [TestFixture]
    public class ExposureDataFactoryTest {

        [Test]
        public async Task CreateFlipped2DExposureData_Given2DArrayInt32_ReturnsCorrectlyFlippedImageData() {

            var imageDataFactoryMock = new Mock<IImageDataFactory>();
            imageDataFactoryMock
                .Setup(x => x.CreateBaseImageData(It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<ImageMetaData>()))
                .Returns((ushort[] arr, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) => new BaseImageData(arr, width, height, bitDepth, isBayered, metaData, null, null, null));
            var profileServiceMock = new Mock<IProfileService>();
            var profileService = profileServiceMock.Object;
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.ASCOMCreate32BitData).Returns(false);
            var starDetectionSelectorMock = new Mock<IPluggableBehaviorSelector<IStarDetection>>();
            starDetectionSelectorMock.Setup(x => x.GetBehavior()).Returns(new Mock<IStarDetection>().Object);
            var starDetectionSelector = starDetectionSelectorMock.Object;
            var starAnnotatorSelector = new Mock<IPluggableBehaviorSelector<IStarAnnotator>>().Object;
            var imageFactory = new ImageDataFactory(profileService, starDetectionSelector, starAnnotatorSelector);

            var factory = new ExposureDataFactory(imageFactory, profileService, starDetectionSelector, starAnnotatorSelector);

            int[,] testData = new int[,]
            {
                { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
                { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 },
                { 31, 32, 33, 34, 35, 36, 37, 38, 39, 40 },
                { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 },
                { 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 },
                { 61, 62, 63, 64, 65, 66, 67, 68, 69, 70 },
                { 71, 72, 73, 74, 75, 76, 77, 78, 79, 80 },
                { 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 },
                { 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 }
            };

            var data = factory.CreateFlipped2DExposureData((Array)testData, 16, false, new ImageMetaData());
            var result = await data.ToImageData(default, default);

            var expected = new uint[] {
                1, 11, 21, 31, 41, 51, 61, 71, 81, 91,
                2, 12, 22, 32, 42, 52, 62, 72, 82, 92,
                3, 13, 23, 33, 43, 53, 63, 73, 83, 93,
                4, 14, 24, 34, 44, 54, 64, 74, 84, 94,
                5, 15, 25, 35, 45, 55, 65, 75, 85, 95,
                6, 16, 26, 36, 46, 56, 66, 76, 86, 96,
                7, 17, 27, 37, 47, 57, 67, 77, 87, 97,
                8, 18, 28, 38, 48, 58, 68, 78, 88, 98,
                9, 19, 29, 39, 49, 59, 69, 79, 89, 99,
                10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            result.Data.FlatArray.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task CreateFlipped2DExposureData_Given2DArrayUInt32_ReturnsCorrectlyFlippedImageData() {

            var imageDataFactoryMock = new Mock<IImageDataFactory>();
            imageDataFactoryMock
                .Setup(x => x.CreateBaseImageData(It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<ImageMetaData>()))
                .Returns((ushort[] arr, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) => new BaseImageData(arr, width, height, bitDepth, isBayered, metaData, null, null, null));
            var profileServiceMock = new Mock<IProfileService>();
            var profileService = profileServiceMock.Object;
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.ASCOMCreate32BitData).Returns(false);
            var starDetectionSelectorMock = new Mock<IPluggableBehaviorSelector<IStarDetection>>();
            starDetectionSelectorMock.Setup(x => x.GetBehavior()).Returns(new Mock<IStarDetection>().Object);
            var starDetectionSelector = starDetectionSelectorMock.Object;
            var starAnnotatorSelector = new Mock<IPluggableBehaviorSelector<IStarAnnotator>>().Object;
            var imageFactory = new ImageDataFactory(profileService, starDetectionSelector, starAnnotatorSelector);

            var factory = new ExposureDataFactory(imageFactory, profileService, starDetectionSelector, starAnnotatorSelector);

            uint[,] testData = new uint[,]
            {
                { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
                { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 },
                { 31, 32, 33, 34, 35, 36, 37, 38, 39, 40 },
                { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 },
                { 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 },
                { 61, 62, 63, 64, 65, 66, 67, 68, 69, 70 },
                { 71, 72, 73, 74, 75, 76, 77, 78, 79, 80 },
                { 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 },
                { 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 }
            };

            var data = factory.CreateFlipped2DExposureData((Array)testData, 16, false, new ImageMetaData());
            var result = await data.ToImageData(default, default);

            var expected = new uint[] {
                1, 11, 21, 31, 41, 51, 61, 71, 81, 91,
                2, 12, 22, 32, 42, 52, 62, 72, 82, 92,
                3, 13, 23, 33, 43, 53, 63, 73, 83, 93,
                4, 14, 24, 34, 44, 54, 64, 74, 84, 94,
                5, 15, 25, 35, 45, 55, 65, 75, 85, 95,
                6, 16, 26, 36, 46, 56, 66, 76, 86, 96,
                7, 17, 27, 37, 47, 57, 67, 77, 87, 97,
                8, 18, 28, 38, 48, 58, 68, 78, 88, 98,
                9, 19, 29, 39, 49, 59, 69, 79, 89, 99,
                10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            result.Data.FlatArray.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task CreateFlipped2DExposureData_Given2DArrayInt8_ReturnsCorrectlyFlippedImageData() {

            var imageDataFactoryMock = new Mock<IImageDataFactory>();
            imageDataFactoryMock
                .Setup(x => x.CreateBaseImageData(It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<ImageMetaData>()))
                .Returns((ushort[] arr, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) => new BaseImageData(arr, width, height, bitDepth, isBayered, metaData, null, null, null));
            var profileServiceMock = new Mock<IProfileService>();
            var profileService = profileServiceMock.Object;
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.ASCOMCreate32BitData).Returns(false);
            var starDetectionSelectorMock = new Mock<IPluggableBehaviorSelector<IStarDetection>>();
            starDetectionSelectorMock.Setup(x => x.GetBehavior()).Returns(new Mock<IStarDetection>().Object);
            var starDetectionSelector = starDetectionSelectorMock.Object;
            var starAnnotatorSelector = new Mock<IPluggableBehaviorSelector<IStarAnnotator>>().Object;
            var imageFactory = new ImageDataFactory(profileService, starDetectionSelector, starAnnotatorSelector);

            var factory = new ExposureDataFactory(imageFactory, profileService, starDetectionSelector, starAnnotatorSelector);

            byte[,] testData = new byte[,]
            {
                { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
                { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 },
                { 31, 32, 33, 34, 35, 36, 37, 38, 39, 40 },
                { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 },
                { 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 },
                { 61, 62, 63, 64, 65, 66, 67, 68, 69, 70 },
                { 71, 72, 73, 74, 75, 76, 77, 78, 79, 80 },
                { 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 },
                { 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 }
            };

            var data = factory.CreateFlipped2DExposureData((Array)testData, 16, false, new ImageMetaData());
            var result = await data.ToImageData(default, default);

            var expected = new uint[] {
                1, 11, 21, 31, 41, 51, 61, 71, 81, 91,
                2, 12, 22, 32, 42, 52, 62, 72, 82, 92,
                3, 13, 23, 33, 43, 53, 63, 73, 83, 93,
                4, 14, 24, 34, 44, 54, 64, 74, 84, 94,
                5, 15, 25, 35, 45, 55, 65, 75, 85, 95,
                6, 16, 26, 36, 46, 56, 66, 76, 86, 96,
                7, 17, 27, 37, 47, 57, 67, 77, 87, 97,
                8, 18, 28, 38, 48, 58, 68, 78, 88, 98,
                9, 19, 29, 39, 49, 59, 69, 79, 89, 99,
                10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            result.Data.FlatArray.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task CreateFlipped2DExposureData_Given2DArrayInt16_ReturnsCorrectlyFlippedImageData() {
            
            var imageDataFactoryMock = new Mock<IImageDataFactory>();
            imageDataFactoryMock
                .Setup(x => x.CreateBaseImageData(It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<ImageMetaData>()))
                .Returns((ushort[] arr, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) => new BaseImageData(arr, width, height, bitDepth, isBayered, metaData, null, null, null));
            var profileServiceMock = new Mock<IProfileService>();
            var profileService = profileServiceMock.Object;
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.ASCOMCreate32BitData).Returns(false);
            var starDetectionSelectorMock = new Mock<IPluggableBehaviorSelector<IStarDetection>>();
            starDetectionSelectorMock.Setup(x => x.GetBehavior()).Returns(new Mock<IStarDetection>().Object);
            var starDetectionSelector = starDetectionSelectorMock.Object;
            var starAnnotatorSelector = new Mock<IPluggableBehaviorSelector<IStarAnnotator>>().Object;
            var imageFactory = new ImageDataFactory(profileService, starDetectionSelector, starAnnotatorSelector);

            var factory = new ExposureDataFactory(imageFactory, profileService, starDetectionSelector, starAnnotatorSelector);

            short[,] testData = new short[,]
            {
                { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
                { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 },
                { 31, 32, 33, 34, 35, 36, 37, 38, 39, 40 },
                { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 },
                { 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 },
                { 61, 62, 63, 64, 65, 66, 67, 68, 69, 70 },
                { 71, 72, 73, 74, 75, 76, 77, 78, 79, 80 },
                { 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 },
                { 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 }
            };

            var data = factory.CreateFlipped2DExposureData((Array)testData, 16, false, new ImageMetaData());
            var result = await data.ToImageData(default, default);

            var expected = new uint[] {
                1, 11, 21, 31, 41, 51, 61, 71, 81, 91,
                2, 12, 22, 32, 42, 52, 62, 72, 82, 92,
                3, 13, 23, 33, 43, 53, 63, 73, 83, 93,
                4, 14, 24, 34, 44, 54, 64, 74, 84, 94,
                5, 15, 25, 35, 45, 55, 65, 75, 85, 95,
                6, 16, 26, 36, 46, 56, 66, 76, 86, 96,
                7, 17, 27, 37, 47, 57, 67, 77, 87, 97,
                8, 18, 28, 38, 48, 58, 68, 78, 88, 98,
                9, 19, 29, 39, 49, 59, 69, 79, 89, 99,
                10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            result.Data.FlatArray.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task CreateFlipped2DExposureData_Given2DArrayUInt16_ReturnsCorrectlyFlippedImageData() {

            var imageDataFactoryMock = new Mock<IImageDataFactory>();
            imageDataFactoryMock
                .Setup(x => x.CreateBaseImageData(It.IsAny<ushort[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<ImageMetaData>()))
                .Returns((ushort[] arr, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) => new BaseImageData(arr, width, height, bitDepth, isBayered, metaData, null, null, null));
            var profileServiceMock = new Mock<IProfileService>();
            var profileService = profileServiceMock.Object;
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.ASCOMCreate32BitData).Returns(false);
            var starDetectionSelectorMock = new Mock<IPluggableBehaviorSelector<IStarDetection>>();
            starDetectionSelectorMock.Setup(x => x.GetBehavior()).Returns(new Mock<IStarDetection>().Object);
            var starDetectionSelector = starDetectionSelectorMock.Object;
            var starAnnotatorSelector = new Mock<IPluggableBehaviorSelector<IStarAnnotator>>().Object;
            var imageFactory = new ImageDataFactory(profileService, starDetectionSelector, starAnnotatorSelector);

            var factory = new ExposureDataFactory(imageFactory, profileService, starDetectionSelector, starAnnotatorSelector);

            ushort[,] testData = new ushort[,]
            {
                { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
                { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 },
                { 31, 32, 33, 34, 35, 36, 37, 38, 39, 40 },
                { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 },
                { 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 },
                { 61, 62, 63, 64, 65, 66, 67, 68, 69, 70 },
                { 71, 72, 73, 74, 75, 76, 77, 78, 79, 80 },
                { 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 },
                { 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 }
            };

            var data = factory.CreateFlipped2DExposureData((Array)testData, 16, false, new ImageMetaData());
            var result = await data.ToImageData(default, default);

            var expected = new uint[] {
                1, 11, 21, 31, 41, 51, 61, 71, 81, 91,
                2, 12, 22, 32, 42, 52, 62, 72, 82, 92,
                3, 13, 23, 33, 43, 53, 63, 73, 83, 93,
                4, 14, 24, 34, 44, 54, 64, 74, 84, 94,
                5, 15, 25, 35, 45, 55, 65, 75, 85, 95,
                6, 16, 26, 36, 46, 56, 66, 76, 86, 96,
                7, 17, 27, 37, 47, 57, 67, 77, 87, 97,
                8, 18, 28, 38, 48, 58, 68, 78, 88, 98,
                9, 19, 29, 39, 49, 59, 69, 79, 89, 99,
                10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            result.Data.FlatArray.Should().BeEquivalentTo(expected);
        }
    }
}
