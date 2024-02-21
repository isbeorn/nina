#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Model;
using NINA.Image.ImageData;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Equipment.Camera {

    [TestFixture]
    public class GenericCameraTest {
        private ImageDataFactoryTestUtility dataFactoryUtility;

        [SetUp]
        public void Setup() {
            dataFactoryUtility = new ImageDataFactoryTestUtility();
        }

        [Test]
        public void Ctor_AllPropertiesSet() {
            var sdk = new Mock<IGenericCameraSDK>();
            var profile = new Mock<IProfileService>();

            var id = "12345";
            var name = "SomeName";

            var sut = new GenericCamera(id, name, "Generic Camera", "Some SDK Version", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            sut.Id.Should().Be("Generic Camera_" + id.ToString());
            sut.Name.Should().Be(name);
            sut.Category.Should().Be("Generic Camera");
            sut.Description.Should().Be(id);
            sut.DriverInfo.Should().Be("Native driver implementation for Generic Camera Cameras");
            sut.DriverVersion.Should().Be("Some SDK Version");
        }

        [Test]
        public async Task Connect_Unsuccessfully_ReturnFalse() {
            var sdk = new Mock<IGenericCameraSDK>();
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            connected.Should().Be(false);
            sdk.Verify(x => x.Connect(), Times.Once);
        }

        [Test]
        public async Task Connect_Unsuccessfully_WithException_ReturnFalse() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.Setup(x => x.Connect()).Throws(new Exception());
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            connected.Should().Be(false);
            sdk.Verify(x => x.Connect(), Times.Once);
        }

        [Test]
        public async Task Connect_Successfully_ReturnTrue() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            connected.Should().Be(true);
            sdk.Verify(x => x.Connect(), Times.Once);
            sdk.VerifyGet(x => x.Connected, Times.Once);
        }

        [Test]
        public async Task Connect_Successfully_OneBinningInitialized() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetBinningInfo()).Returns(new int[] { });
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            sdk.Verify(x => x.GetBinningInfo(), Times.Once);
            sut.MaxBinX.Should().Be(1);
            sut.MaxBinY.Should().Be(1);
            sut.BinningModes.Count.Should().Be(1);
            sut.BinningModes.Should().BeEquivalentTo(new AsyncObservableCollection<BinningMode>() { new BinningMode(1, 1) });
        }

        [Test]
        public async Task Connect_Successfully_MultiBinningInitialized() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetBinningInfo()).Returns(new int[] { 4, 2, 6, 1 });
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            sdk.Verify(x => x.GetBinningInfo(), Times.Once);
            sut.MaxBinX.Should().Be(6);
            sut.MaxBinY.Should().Be(6);
            sut.BinningModes.Count.Should().Be(4);
            sut.BinningModes.Should().BeEquivalentTo(new AsyncObservableCollection<BinningMode>() { new BinningMode(1, 1), new BinningMode(2, 2), new BinningMode(4, 4), new BinningMode(6, 6) });
        }

        [Test]
        [TestCase(double.NaN, double.NaN)]
        [TestCase(3.8, 3.8)]
        [TestCase(1, 1)]
        [TestCase(-1, double.NaN)]
        [TestCase(0, double.NaN)]
        [TestCase(0.1, 0.1)]
        public async Task Connect_Successfully_PixelSizeInitialized(double sdkReturn, double expected) {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetPixelSize()).Returns(sdkReturn);
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            sdk.Verify(x => x.GetPixelSize(), Times.Once);
            sut.PixelSizeX.Should().Be(expected);
            sut.PixelSizeY.Should().Be(expected);
        }

        [Test]
        public async Task Connect_Successfully_DimensionsInitialized() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetDimensions()).Returns((1920, 1080));
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            sdk.Verify(x => x.GetDimensions(), Times.Once);
            sut.CameraXSize.Should().Be(1920);
            sut.CameraYSize.Should().Be(1080);
        }

        [Test]
        [TestCase(SensorType.Monochrome)]
        [TestCase(SensorType.BGGR)]
        [TestCase(SensorType.RGBG)]
        public async Task Connect_Successfully_GetSensorInfoInitialized(SensorType sensorType) {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetSensorInfo()).Returns(sensorType);
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            sdk.Verify(x => x.GetSensorInfo(), Times.Once);
            sut.SensorType.Should().Be(sensorType);
        }

        [Test]
        public void Disconnect_Successful() {
            var sdk = new Mock<IGenericCameraSDK>();
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            sut.Disconnect();

            sdk.Verify(x => x.Disconnect(), Times.Once);
        }

        [Test]
        public void Disconnect_WithException_Successful() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.Setup(x => x.Disconnect()).Throws(new Exception());
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            sut.Disconnect();

            sdk.Verify(x => x.Disconnect(), Times.Once);
        }

        [Test]
        [TestCase(-1, 1, 1)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(1, 2, 1)]
        [TestCase(2, 1, 1)]
        [TestCase(2, 2, 2)]
        [TestCase(3, 2, 2)]
        public async Task SetBinning_Successful(short bin, short maxbin, short expectedbin) {
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);

            int[] binnings = new int[maxbin];
            for (int i = 0; i < maxbin; i++) { binnings[i] = i + 1; }
            sdk.Setup(x => x.GetBinningInfo()).Returns(binnings);

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);
            await sut.Connect(default);

            sut.SetBinning(bin, bin);

            sut.BinX.Should().Be(expectedbin);
            sut.BinY.Should().Be(expectedbin);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task StartExposure_NoSubSample_ROISetCorrectly(short binning) {
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";
            var sdk = new Mock<IGenericCameraSDK>();

            int[] binnings = new int[binning];
            for (int i = 0; i < binning; i++) { binnings[i] = i + 1; }
            sdk.Setup(x => x.GetBinningInfo()).Returns(binnings);
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetDimensions()).Returns((1920, 1080));
            sdk.Setup(x => x.GetROI()).Returns((0, 0, 1920, 1080, binning));

            sdk.Setup(x => x.IsExposureReady()).Returns(true);
            sdk.Setup(x => x.GetExposure(It.IsAny<double>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ushort[] { });

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);
            await sut.Connect(default);
            sut.SetBinning(binning, binning);

            sut.EnableSubSample = false;
            sut.SubSampleX = 0;
            sut.SubSampleY = 0;
            sut.SubSampleWidth = sut.CameraXSize;
            sut.SubSampleHeight = sut.CameraYSize;

            sut.StartExposure(new CaptureSequence(100, "LGHT", null, new BinningMode(1, 1), binning));
            await sut.WaitUntilExposureIsReady(default);

            sdk.Verify(x => x.SetROI(0, 0, 1920, 1080, binning), Times.Once);
            sdk.Verify(x => x.GetROI(), Times.Once);
            sdk.Verify(x => x.StartExposure(100d, 1920, 1080), Times.Once);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task StartExposure_WithSubSample_ROISetCorrectly(short binning) {
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";
            var sdk = new Mock<IGenericCameraSDK>();

            int[] binnings = new int[binning];
            for (int i = 0; i < binning; i++) { binnings[i] = i + 1; }
            sdk.Setup(x => x.GetBinningInfo()).Returns(binnings);
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetDimensions()).Returns((1920, 1080));
            sdk.Setup(x => x.GetROI()).Returns((0, 0, 600, 500, binning));

            sdk.Setup(x => x.IsExposureReady()).Returns(true);
            sdk.Setup(x => x.GetExposure(It.IsAny<double>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ushort[] { });

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);
            await sut.Connect(default);
            sut.SetBinning(binning, binning);

            sut.EnableSubSample = true;
            sut.SubSampleX = 100;
            sut.SubSampleY = 100;
            sut.SubSampleWidth = 600;
            sut.SubSampleHeight = 500;

            sut.StartExposure(new CaptureSequence(100, "LGHT", null, new BinningMode(1, 1), binning));
            await sut.WaitUntilExposureIsReady(default);

            sdk.Verify(x => x.SetROI(100, 100, 600, 500, binning), Times.Once);
            sdk.Verify(x => x.GetROI(), Times.Once);
            sdk.Verify(x => x.StartExposure(100d, 600, 500), Times.Once);
        }

        [Test]
        public async Task WaitForExposureTask_NoTaskAvailable_ReturnsImmediately() {
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";
            var sdk = new Mock<IGenericCameraSDK>();

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);
            sdk.Setup(x => x.IsExposureReady()).Returns(true);
            var task = sut.WaitUntilExposureIsReady(default);
            await task;

            task.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task DownloadExposure_WithSubSample_12bit_ImageArrayCorrect(short binning) {
            var profile = new Mock<IProfileService>();
            profile.SetupGet(x => x.ActiveProfile.CameraSettings.BitScaling).Returns(false);
            var deviceId = "12345";
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.Setup(x => x.GetBitDepth()).Returns(12);

            int[] binnings = new int[binning];
            for (int i = 0; i < binning; i++) { binnings[i] = i + 1; }
            sdk.Setup(x => x.GetBinningInfo()).Returns(binnings);
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetDimensions()).Returns((1920, 1080));
            sdk.Setup(x => x.GetROI()).Returns((0, 0, 600, 500, binning));

            var data = new ushort[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            sdk.Setup(x => x.IsExposureReady()).Returns(true);
            sdk.Setup(x => x.GetExposure(It.IsAny<double>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(data);

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);
            await sut.Connect(default);
            sut.SetBinning(binning, binning);

            sut.EnableSubSample = true;
            sut.SubSampleX = 100;
            sut.SubSampleY = 100;
            sut.SubSampleWidth = 600;
            sut.SubSampleHeight = 500;

            sut.StartExposure(new CaptureSequence(100, "LGHT", null, new BinningMode(1, 1), binning));
            await sut.WaitUntilExposureIsReady(default);
            var result = (ImageArrayExposureData)await sut.DownloadExposure(default);

            result.BitDepth.Should().Be(12);
            result.Width.Should().Be(600);
            result.Height.Should().Be(500);
            var imgData = await result.ToImageData(default, default);
            imgData.Data.FlatArray.Should().BeEquivalentTo(data);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task DownloadExposure_WithSubSample_12bit_WithBitScaling_ImageArrayCorrect(short binning) {
            var profile = new Mock<IProfileService>();
            profile.SetupGet(x => x.ActiveProfile.CameraSettings.BitScaling).Returns(true);
            var deviceId = "12345";
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.Setup(x => x.GetBitDepth()).Returns(12);

            int[] binnings = new int[binning];
            for (int i = 0; i < binning; i++) { binnings[i] = i + 1; }
            sdk.Setup(x => x.GetBinningInfo()).Returns(binnings);
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.GetDimensions()).Returns((1920, 1080));
            sdk.Setup(x => x.GetROI()).Returns((0, 0, 600, 500, binning));

            var data = new ushort[] { 1, 1, 1, 1, 128, 128, 128, 128 };
            sdk.Setup(x => x.IsExposureReady()).Returns(true);
            sdk.Setup(x => x.GetExposure(It.IsAny<double>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(data);

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);
            await sut.Connect(default);
            sut.SetBinning(binning, binning);

            sut.EnableSubSample = true;
            sut.SubSampleX = 100;
            sut.SubSampleY = 100;
            sut.SubSampleWidth = 600;
            sut.SubSampleHeight = 500;

            sut.StartExposure(new CaptureSequence(100, "LGHT", null, new BinningMode(1, 1), binning));
            await sut.WaitUntilExposureIsReady(default);
            var result = (ImageArrayExposureData)await sut.DownloadExposure(default);

            result.BitDepth.Should().Be(16);
            result.Width.Should().Be(600);
            result.Height.Should().Be(500);
            var imgData = await result.ToImageData(default, default);
            imgData.Data.FlatArray.Should().BeEquivalentTo(new ushort[] { 16, 16, 16, 16, 2048, 2048, 2048, 2048 });
        }

        [Test]
        public async Task HasNoTemperatureControl_ReturnsFalseWhenSdkFalse() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.HasTemperatureControl()).Returns(false);
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            connected.Should().Be(true);
            sut.CanSetTemperature.Should().BeFalse();
        }

        [Test]
        public async Task HasTemperatureControl_ReturnsTrueWhenSdkTrue() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.HasTemperatureControl()).Returns(true);
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);

            connected.Should().Be(true);
            sut.CanSetTemperature.Should().BeTrue();
        }

        [Test]
        public async Task HasTemperatureControl_CanAdjustTemperatureAndCooler() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.HasTemperatureReadout()).Returns(true);
            sdk.Setup(x => x.HasTemperatureControl()).Returns(true);
            sdk.Setup(x => x.HasTemperatureReadout()).Returns(true);
            sdk.Setup(x => x.GetCoolerOnOff()).Returns(true);
            sdk.Setup(x => x.GetCoolerPower()).Returns(30);
            sdk.Setup(x => x.GetTemperature()).Returns(20);
            sdk.Setup(x => x.GetTargetTemperature()).Returns(10);
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);
            sut.TemperatureSetPoint = 10;
            sut.CoolerOn = true;

            connected.Should().Be(true);

            sut.Temperature.Should().Be(20);
            sut.CoolerPower.Should().Be(30);
            sut.TemperatureSetPoint.Should().Be(10);
            sut.CoolerOn.Should().BeTrue();

            sdk.Verify(x => x.SetTargetTemperature(10), Times.Once);
            sdk.Verify(x => x.SetCooler(true), Times.Once);
            sdk.Verify(x => x.GetTargetTemperature(), Times.Once);
            sdk.Verify(x => x.GetTemperature(), Times.Once);
            sdk.Verify(x => x.GetCoolerOnOff(), Times.Once);
            sdk.Verify(x => x.GetCoolerPower(), Times.Once);
        }

        [Test]
        public async Task HasNoTemperatureControl_CannotAdjustTemperatureAndCooler() {
            var sdk = new Mock<IGenericCameraSDK>();
            sdk.SetupGet(x => x.Connected).Returns(true);
            sdk.Setup(x => x.HasTemperatureControl()).Returns(false);
            sdk.Setup(x => x.GetCoolerOnOff()).Returns(true);
            sdk.Setup(x => x.GetCoolerPower()).Returns(30);
            sdk.Setup(x => x.GetTemperature()).Returns(20);
            sdk.Setup(x => x.GetTargetTemperature()).Returns(10);
            var profile = new Mock<IProfileService>();
            var deviceId = "12345";

            var sut = new GenericCamera(deviceId, "", "", "Generic Camera", true, sdk.Object, profile.Object, dataFactoryUtility.ExposureDataFactory);

            var connected = await sut.Connect(default);
            sut.TemperatureSetPoint = 10;
            sut.CoolerOn = true;

            connected.Should().Be(true);

            sut.Temperature.Should().Be(double.NaN);
            sut.CoolerPower.Should().Be(double.NaN);
            sut.TemperatureSetPoint.Should().Be(double.NaN);
            sut.CoolerOn.Should().BeFalse();

            sdk.Verify(x => x.SetTargetTemperature(10), Times.Never);
            sdk.Verify(x => x.SetCooler(true), Times.Never);
            sdk.Verify(x => x.GetTargetTemperature(), Times.Never);
            sdk.Verify(x => x.GetTemperature(), Times.Never);
            sdk.Verify(x => x.GetCoolerOnOff(), Times.Never);
            sdk.Verify(x => x.GetCoolerPower(), Times.Never);
        }
    }
}