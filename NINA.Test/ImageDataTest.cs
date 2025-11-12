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
using NINA.Image.ImageData;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Core.Utility;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using NINA.Image.Interfaces;
using NINA.Image.FileFormat;
using NINA.Core.Model;
using Moq;
using NUnit.Framework.Legacy;

namespace NINA.Test {

    [TestFixture]
    public class ImageDataTest {
        private ImageMetaData MetaData;
        private ImageDataFactoryTestUtility dataFactoryUtility;

        [SetUp]
        public void Setup() {
            MetaData = new ImageMetaData() {
                Image = {
                    ExposureStart  = new DateTime(2019,1,1,12,2,3,333, DateTimeKind.Utc),
                    ExposureNumber  = 5,
                    ImageType  = "LIGHT",
                    Binning  = "1x1",
                    ExposureTime  = 300,
                    RecordedRMS  = new RMS() {
                        Total = 10,
                    }
                },
                Camera = {
                    Id = "MyUniqueId1234",
                    Name = "TestCamera",
                    BinX = 2,
                    BinY  = 3,
                    PixelSize  = 3.9,
                    Temperature = -10,
                    Gain = 139,
                    Offset = 10,
                    ElectronsPerADU = 3.1,
                    SetPoint = -11
                },
                Telescope = {
                    Name = "TestTelescope",
                    FocalLength = 500,
                    FocalRatio = 4,
                    Coordinates = new Coordinates(Angle.ByDegree(10), Angle.ByDegree(1), Epoch.J2000)
                },
                Focuser = {
                    Name = "TestFocuser",
                    Position = 100,
                    StepSize = 20,
                    Temperature = 10
                },
                Rotator = {
                    Name = "TestRotator",
                    Position = 100,
                    StepSize = 20,
                },
                FilterWheel = {
                    Name = "TestFilterWheel",
                    Filter = "RED"
                },
                Target = {
                    Name = "M81",
                    Coordinates = new Coordinates(Angle.ByDegree(11), Angle.ByDegree(2), Epoch.J2000)
                },
                Observer = {
                    Latitude = 10,
                    Longitude = 20,
                    Elevation = 100
                }
            };
            MetaData.Image.RecordedRMS.SetScale(5);
            dataFactoryUtility = new ImageDataFactoryTestUtility();
            dataFactoryUtility.ProfileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.ASCOMCreate32BitData).Returns(false);
        }

        [Test]
        public async Task CreateInstance2dArray() {
            //Arrange
            int[,] arr = new int[4, 5];
            arr[0, 0] = 100; arr[1, 0] = 200; arr[2, 0] = 300; arr[3, 0] = 400;
            arr[0, 1] = 500; arr[1, 1] = 600; arr[2, 1] = 700; arr[3, 1] = 800;
            arr[0, 2] = 900; arr[1, 2] = 1000; arr[2, 2] = 1100; arr[3, 2] = 1200;
            arr[0, 3] = 1300; arr[1, 3] = 1400; arr[2, 3] = 1500; arr[3, 3] = 1600;
            arr[0, 4] = 1700; arr[1, 4] = 1800; arr[2, 4] = 1900; arr[3, 4] = 2000;

            ushort expX = 4;
            ushort expY = 5;
            ushort[] expFlatArr = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };

            //Act
            IImageData result = await dataFactoryUtility.ExposureDataFactory.CreateFlipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();

            //Assert
            ClassicAssert.AreEqual(expX, result.Properties.Width);
            ClassicAssert.AreEqual(expY, result.Properties.Height);
            CollectionAssert.AreEqual(expFlatArr, result.Data.FlatArray);
        }

        [Test]
        public void CreateInstanceFlatArrArray() {
            //Arrange
            ushort[] arr = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };
            ushort width = 4;
            ushort height = 5;

            ushort expX = 4;
            ushort expY = 5;
            ushort[] expFlatArr = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };

            //Act
            BaseImageData result = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(arr, width, height, 16, false, new ImageMetaData());

            //Assert
            ClassicAssert.AreEqual(expX, result.Properties.Width);
            ClassicAssert.AreEqual(expY, result.Properties.Height);
            CollectionAssert.AreEqual(expFlatArr, result.Data.FlatArray);
        }

        [Test]
        public void CreateInstance3dArray_ExceptionThrown() {
            Assert.ThrowsAsync<NotSupportedException>(async () => {
                //Arrange
                var arr = new Int32[5, 5, 5];
                //Act
                IImageData result = await dataFactoryUtility.ExposureDataFactory.CreateFlipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            }
            );
        }

        [Test]
        public async Task StDevTest() {
            //Arrange
            int[,] arr = new int[4, 5];

            arr[0, 0] = 440;
            arr[0, 1] = 4700;
            arr[0, 2] = 5000;
            arr[0, 3] = 5100;
            arr[0, 4] = 4700;
            arr[1, 0] = 460;
            arr[1, 1] = 4800;
            arr[1, 2] = 4600;
            arr[1, 3] = 4600;
            arr[1, 4] = 5700;
            arr[2, 0] = 430;
            arr[2, 1] = 5400;
            arr[2, 2] = 5600;
            arr[2, 3] = 5400;
            arr[2, 4] = 4400;
            arr[3, 0] = 5600;
            arr[3, 1] = 5000;
            arr[3, 2] = 4800;
            arr[3, 3] = 5100;
            arr[3, 4] = 490;

            double stdev = 1864.0155578749873;
            double mean = 4116;

            //Act
            IImageData result = await dataFactoryUtility.ExposureDataFactory.CreateFlipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            //Assert
            ClassicAssert.AreEqual(stdev, resultStatistics.StDev);
            ClassicAssert.AreEqual(mean, resultStatistics.Mean);
        }

        [Test]
        public async Task StDevTest_ExtremeDistribution() {
            //Arrange
            int[,] arr = new int[4, 5];

            arr[0, 0] = 65535;
            arr[0, 1] = 65535;
            arr[0, 2] = 65535;
            arr[0, 3] = 65535;
            arr[0, 4] = 65535;
            arr[1, 0] = 65535;
            arr[1, 1] = 65535;
            arr[1, 2] = 65535;
            arr[1, 3] = 65535;
            arr[1, 4] = 65535;
            arr[2, 0] = 0;
            arr[2, 1] = 0;
            arr[2, 2] = 0;
            arr[2, 3] = 0;
            arr[2, 4] = 0;
            arr[3, 0] = 0;
            arr[3, 1] = 0;
            arr[3, 2] = 0;
            arr[3, 3] = 0;
            arr[3, 4] = 0;

            double stdev = 32767.5;
            double mean = 32767.5;

            //Act
            IImageData result = await dataFactoryUtility.ExposureDataFactory.CreateFlipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            //Assert
            ClassicAssert.AreEqual(stdev, resultStatistics.StDev);
            ClassicAssert.AreEqual(mean, resultStatistics.Mean);
        }

        [Test]
        [TestCase(4, 3, 12345, 35483, 23914, 11569)]
        [TestCase(46, 35, 12345, 35483, 23914, 11569)]
        [TestCase(460, 350, 12345, 35483, 23914, 11569)]
        public async Task StDevTest_LargeDataSetTest(int width, int height, int value1, int value2, double mean, double stdev) {
            //Arrange
            int[,] arr = new int[width, height];
            for (int x = 0; x < arr.GetLength(0); x += 1) {
                for (int y = 0; y < arr.GetLength(1); y += 1) {
                    arr[x, y] = (x + y) % 2 == 0 ? value1 : value2;
                }
            }

            //Act
            IImageData result = await dataFactoryUtility.ExposureDataFactory.CreateFlipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            //Assert
            ClassicAssert.AreEqual(stdev, resultStatistics.StDev, 0.000001);
            ClassicAssert.AreEqual(mean, resultStatistics.Mean);
        }

        [Test]
        [TestCase(10, 20, 12, true, 5)]
        [TestCase(20, 10, 10, true, 5)]
        [TestCase(5, 5, 16, false, 5)]
        public async Task StatisticsInitializedCorrectly(int width, int height, int bitDepth, bool isBayered, int resolution) {
            //Arrange
            ushort[] arr = new ushort[width * height];
            for (ushort i = 0; i < width * height; i++) {
                arr[i] = i;
            }

            //Act
            var imgData = await dataFactoryUtility.ExposureDataFactory.CreateImageArrayExposureData(arr, width, height, bitDepth, isBayered, new ImageMetaData()).ToImageData();

            //Assert
            ClassicAssert.AreEqual(width, imgData.Properties.Width);
            ClassicAssert.AreEqual(height, imgData.Properties.Height);
            ClassicAssert.AreEqual(bitDepth, imgData.Properties.BitDepth);
            ClassicAssert.AreEqual(isBayered, imgData.Properties.IsBayered);
        }

        [Test]
        [TestCase(10, 20, 12, true, 5)]
        [TestCase(20, 10, 10, true, 5)]
        [TestCase(5, 5, 16, false, 5)]
        public async Task StatisticsInitializedCorrectly2(int width, int height, int bitDepth, bool isBayered, int resolution) {
            //Arrange
            int[,] arr = new int[width, height];
            for (int x = 0; x < arr.GetLength(0); x += 1) {
                for (int y = 0; y < arr.GetLength(1); y += 1) {
                    arr[x, y] = x * y;
                }
            }

            //Act
            var imgData = await dataFactoryUtility.ExposureDataFactory.CreateFlipped2DExposureData(arr, bitDepth, isBayered, new ImageMetaData()).ToImageData();

            //Assert
            ClassicAssert.AreEqual(width, imgData.Properties.Width);
            ClassicAssert.AreEqual(height, imgData.Properties.Height);
            ClassicAssert.AreEqual(bitDepth, imgData.Properties.BitDepth);
            ClassicAssert.AreEqual(isBayered, imgData.Properties.IsBayered);
        }

        [Test]
        public async Task MinMaxTest() {
            //Arrange
            int[,] arr = new int[4, 5];

            arr[0, 0] = 10;
            arr[0, 1] = 10;
            arr[0, 2] = 20;
            arr[0, 3] = 20;
            arr[0, 4] = 30;
            arr[1, 0] = 30;
            arr[1, 1] = 50;
            arr[1, 2] = 50;
            arr[1, 3] = 50;
            arr[1, 4] = 50;
            arr[2, 0] = 80;
            arr[2, 1] = 80;
            arr[2, 2] = 80;
            arr[2, 3] = 10;
            arr[2, 4] = 10;
            arr[3, 0] = 10;
            arr[3, 1] = 7;
            arr[3, 2] = 7;
            arr[3, 3] = 5;
            arr[3, 4] = 5;

            //Act
            var result = await dataFactoryUtility.ExposureDataFactory.CreateFlipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            //Assert
            ClassicAssert.AreEqual(5, resultStatistics.Min);
            ClassicAssert.AreEqual(2, resultStatistics.MinOccurrences);
            ClassicAssert.AreEqual(80, resultStatistics.Max);
            ClassicAssert.AreEqual(3, resultStatistics.MaxOccurrences);
        }

        [Test]
        [TestCase(new ushort[] { 1, 2, 3, 4, 5, 6 }, 10, 3.5, 1.5)]
        [TestCase(new ushort[] { 1, 3, 3, 2, 3, 4, 5, 6 }, 10, 3, 1)]
        [TestCase(new ushort[] { 3, 3, 3, 3, 3, 3 }, 10, 3, 0)]
        [TestCase(new ushort[] { 5, 9, 155, 8, 5, 66, 7, 5, 6, 88, 5, 4, 56, 6 }, 10, 6.5, 1.5)]
        [TestCase(new ushort[] { 10, 10, 10, 10, 10, 10, 10, 10, 15, 20, 20, 20, 20, 20, 50, 50, 50, 50 }, 10, 17.5, 7.5)]
        [TestCase(new ushort[] { 0, 0, 65535, 65535 }, 16, 32767.5, 32767.5)]
        public async Task MedianTest(ushort[] arr, int bitDepth, double expectedMedian, double expectedMAD) {
            var result = await dataFactoryUtility.ExposureDataFactory.CreateImageArrayExposureData(arr, arr.Length / 2, 2, bitDepth, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            ClassicAssert.AreEqual(expectedMedian, resultStatistics.Median);
            ClassicAssert.AreEqual(expectedMAD, resultStatistics.MedianAbsoluteDeviation);
        }

        [Test]
        [TestCase(NINA.Core.Enum.FileTypeEnum.XISF, ".xisf")]
        [TestCase(NINA.Core.Enum.FileTypeEnum.FITS, ".fits")]
        [TestCase(NINA.Core.Enum.FileTypeEnum.TIFF, ".tif")]
        public async Task SaveToDiskSimpleTest(NINA.Core.Enum.FileTypeEnum fileType, string extension) {
            dataFactoryUtility.ProfileServiceMock.Setup(x => x.ActiveProfile.ImageFileSettings.FITSAddFzExtension).Returns(false);
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };

            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = "TestFile",
                FileType = fileType
            };

            var sut = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(data, 3, 3, 16, false, MetaData);

            var file = await sut.SaveToDisk(fileSaveInfo, default);

            File.Exists(file).Should().BeTrue();
            File.Delete(file);
            Path.GetFileName(file).Should().Be($"{fileSaveInfo.FilePattern}{extension}");
        }

        [Test]
        [TestCase(NINA.Core.Enum.FileTypeEnum.XISF)]
        [TestCase(NINA.Core.Enum.FileTypeEnum.FITS)]
        [TestCase(NINA.Core.Enum.FileTypeEnum.TIFF)]
        public async Task SaveToDiskForceExtensionTest(NINA.Core.Enum.FileTypeEnum fileType) {
            dataFactoryUtility.ProfileServiceMock.Setup(x => x.ActiveProfile.ImageFileSettings.FITSAddFzExtension).Returns(false);
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };

            var extension = ".bar";
            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = "TestFile",
                ForceExtension = extension,
                FileType = fileType
            };

            var sut = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(data, 3, 3, 16, false, MetaData);

            var file = await sut.SaveToDisk(fileSaveInfo, default);

            File.Exists(file).Should().BeTrue();
            File.Delete(file);
            Path.GetFileName(file).Should().Be($"{fileSaveInfo.FilePattern}{extension}");
        }

        [Test]
        public async Task SaveToDiskPatternMetaDataTest() {
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };

            var pattern = $"$$FILTER$$" +
                $"#$$DATE$$" +
                $"#$$DATEMINUS12$$" +
                $"#$$DATETIME$$" +
                $"#$$TIME$$" +
                $"#$$FRAMENR$$" +
                $"#$$IMAGETYPE$$" +
                $"#$$BINNING$$" +
                $"#$$SENSORTEMP$$" +
                $"#$$EXPOSURETIME$$" +
                $"#$$TARGETNAME$$" +
                $"#$$GAIN$$" +
                $"#$$OFFSET$$" +
                $"#$$RMS$$" +
                $"#$$RMSARCSEC$$" +
                $"#$$FOCUSERPOSITION$$" +
                $"#$$APPLICATIONSTARTDATE$$";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = pattern,
                FileType = NINA.Core.Enum.FileTypeEnum.XISF
            };

            var sut = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(data, 3, 3, 16, false, MetaData);
            var file = await sut.SaveToDisk(fileSaveInfo, default);
            File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().AddHours(-12).ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber.ToString("0000")}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Camera.Temperature)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.ExposureTime)}" +
                $"#{MetaData.Target.Name}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Gain)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Offset)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0}", MetaData.Focuser.Position)}" +
                $"#{NINA.Core.Utility.CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd")}";

            Path.GetFileName(file).Should().Be($"{expectedPattern}.{fileSaveInfo.FileType.ToString().ToLower()}");
        }

        [Test]
        public async Task SaveToDiskPatternEmptyMetaDataTest() {
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };

            var pattern = $"$$FILTER$$" +
                $"#$$DATE$$" +
                $"#$$DATEMINUS12$$" +
                $"#$$DATETIME$$" +
                $"#$$TIME$$" +
                $"#$$FRAMENR$$" +
                $"#$$IMAGETYPE$$" +
                $"#$$BINNING$$" +
                $"#$$SENSORTEMP$$" +
                $"#$$EXPOSURETIME$$" +
                $"#$$TARGETNAME$$" +
                $"#$$GAIN$$" +
                $"#$$OFFSET$$" +
                $"#$$RMS$$" +
                $"#$$RMSARCSEC$$" +
                $"#$$FOCUSERPOSITION$$" +
                $"#$$APPLICATIONSTARTDATE$$";
            var date = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = pattern,
                FileType = NINA.Core.Enum.FileTypeEnum.TIFF,
                TIFFCompressionType = NINA.Core.Enum.TIFFCompressionTypeEnum.LZW
            };

            var sut = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(data, 3, 3, 16, false, new ImageMetaData() { Image = new ImageParameter() { ExposureStart = date } });
            var file = await sut.SaveToDisk(fileSaveInfo, default);
            File.Delete(file);

            var expectedPattern = $"#{date.ToLocalTime():yyyy-MM-dd}#{date.AddHours(-12).ToLocalTime():yyyy-MM-dd}#{date.ToLocalTime():yyyy-MM-dd_HH-mm-ss}#{date.ToLocalTime():HH-mm-ss}#-0001##1x1#########{NINA.Core.Utility.CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd")}.tif";

            Path.GetFileName(file).Should().Be($"{expectedPattern}");
        }

        [Test]
        public async Task PrepareFinalizeSavePatternMetaDataTest() {
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };

            var pattern = $"$$FILTER$$" +
                $"#$$DATE$$" +
                $"#$$DATEMINUS12$$" +
                $"#$$DATETIME$$" +
                $"#$$TIME$$" +
                $"#$$FRAMENR$$" +
                $"#$$IMAGETYPE$$" +
                $"#$$BINNING$$" +
                $"#$$SENSORTEMP$$" +
                $"#$$EXPOSURETIME$$" +
                $"#$$TARGETNAME$$" +
                $"#$$GAIN$$" +
                $"#$$OFFSET$$" +
                $"#$$RMS$$" +
                $"#$$RMSARCSEC$$" +
                $"#$$FOCUSERPOSITION$$" +
                $"#$$APPLICATIONSTARTDATE$$";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = pattern,
                FileType = NINA.Core.Enum.FileTypeEnum.XISF
            };

            var sut = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(data, 3, 3, 16, false, MetaData);
            var file = await sut.PrepareSave(fileSaveInfo, default);
            file = sut.FinalizeSave(file, pattern, new List<ImagePattern>());
            File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().AddHours(-12).ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber.ToString("0000")}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Camera.Temperature)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.ExposureTime)}" +
                $"#{MetaData.Target.Name}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Gain)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Offset)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0}", MetaData.Focuser.Position)}" +
                $"#{NINA.Core.Utility.CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd")}";

            Path.GetFileName(file).Should().Be($"{expectedPattern}.{fileSaveInfo.FileType.ToString().ToLower()}");
        }

        [Test]
        public async Task PrepareFinalize_IllegalCharacters_SavePatternMetaDataTest() {
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };

            var pattern = $"$$FILTER$$" +
                $"#$$DATE$$" +
                $"#$$DATEMINUS12$$" +
                $"#$$DATETIME$$" +
                $"#$$TIME$$" +
                $"#$$FRAMENR$$" +
                $"#$$IMAGETYPE$$" +
                $"#$$BINNING$$" +
                $"#$$SENSORTEMP$$" +
                $"#$$EXPOSURETIME$$" +
                $"#$$TARGETNAME$$" +
                $"#$$GAIN$$" +
                $"#$$OFFSET$$" +
                $"#$$RMS$$" +
                $"#$$RMSARCSEC$$" +
                $"#$$FOCUSERPOSITION$$" +
                $"#$$APPLICATIONSTARTDATE$$";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = pattern,
                FileType = NINA.Core.Enum.FileTypeEnum.XISF
            };

            var invalidChars = Path.GetInvalidPathChars();
            MetaData.Target.Name = string.Join("", invalidChars);

            var sut = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(data, 3, 3, 16, false, MetaData);
            var file = await sut.PrepareSave(fileSaveInfo, default);
            file = sut.FinalizeSave(file, pattern, new List<ImagePattern>());
            File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().AddHours(-12).ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber.ToString("0000")}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Camera.Temperature)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.ExposureTime)}" +
                $"#{new string('_', invalidChars.Length)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Gain)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Offset)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0}", MetaData.Focuser.Position)}" +
                $"#{NINA.Core.Utility.CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd")}";

            Path.GetFileName(file).Should().Be($"{expectedPattern}.{fileSaveInfo.FileType.ToString().ToLower()}");
        }

        [Test]
        public async Task SaveToDisk_SingleStep_PatternMetaDataTest() {
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };

            var pattern = $"$$FILTER$$" +
                $"#$$DATE$$" +
                $"#$$DATEMINUS12$$" +
                $"#$$DATETIME$$" +
                $"#$$TIME$$" +
                $"#$$FRAMENR$$" +
                $"#$$IMAGETYPE$$" +
                $"#$$BINNING$$" +
                $"#$$SENSORTEMP$$" +
                $"#$$EXPOSURETIME$$" +
                $"#$$TARGETNAME$$" +
                $"#$$GAIN$$" +
                $"#$$OFFSET$$" +
                $"#$$RMS$$" +
                $"#$$RMSARCSEC$$" +
                $"#$$FOCUSERPOSITION$$" +
                $"#$$APPLICATIONSTARTDATE$$";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = pattern,
                FileType = NINA.Core.Enum.FileTypeEnum.XISF
            };

            var sut = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(data, 3, 3, 16, false, MetaData);
            var file = await sut.SaveToDisk(fileSaveInfo, default, false, new List<ImagePattern>());
            File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().AddHours(-12).ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber.ToString("0000")}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Camera.Temperature)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.ExposureTime)}" +
                $"#{MetaData.Target.Name}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Gain)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Offset)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0}", MetaData.Focuser.Position)}" +
                $"#{NINA.Core.Utility.CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd")}";

            Path.GetFileName(file).Should().Be($"{expectedPattern}.{fileSaveInfo.FileType.ToString().ToLower()}");
        }

        [Test]
        public async Task SaveToDisk_IllegalCharacters_SavePatternMetaDataTest() {
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };

            var pattern = $"$$FILTER$$" +
                $"#$$DATE$$" +
                $"#$$DATEMINUS12$$" +
                $"#$$DATETIME$$" +
                $"#$$TIME$$" +
                $"#$$FRAMENR$$" +
                $"#$$IMAGETYPE$$" +
                $"#$$BINNING$$" +
                $"#$$SENSORTEMP$$" +
                $"#$$EXPOSURETIME$$" +
                $"#$$TARGETNAME$$" +
                $"#$$GAIN$$" +
                $"#$$OFFSET$$" +
                $"#$$RMS$$" +
                $"#$$RMSARCSEC$$" +
                $"#$$FOCUSERPOSITION$$" +
                $"#$$APPLICATIONSTARTDATE$$";

            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = pattern,
                FileType = NINA.Core.Enum.FileTypeEnum.XISF
            };

            var invalidChars = Path.GetInvalidPathChars();
            MetaData.Target.Name = string.Join("", invalidChars);

            var sut = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(data, 3, 3, 16, false, MetaData);
            var file = await sut.SaveToDisk(fileSaveInfo, default, false, new List<ImagePattern>());
            File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().AddHours(-12).ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToLocalTime().ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber.ToString("0000")}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Camera.Temperature)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.ExposureTime)}" +
                $"#{new string('_', invalidChars.Length)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Gain)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Offset)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0.00}", MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale)}" +
                $"#{string.Format(CultureInfo.InvariantCulture, "{0:0}", MetaData.Focuser.Position)}" +
                $"#{NINA.Core.Utility.CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd")}";

            Path.GetFileName(file).Should().Be($"{expectedPattern}.{fileSaveInfo.FileType.ToString().ToLower()}");
        }

        //[Test]
        //public async Task SaveToDiskXISFDeepMetaDataTest() {
        //    Uncomment once xisf loading knows to extract meta data
        //    var fileType = NINA.Core.Enum.FileTypeEnum.XISF;
        //    var data = new ushort[] {
        //        3,1,1,
        //        3,4,5,
        //        3,2,3
        //    };
        //    var folder = TestContext.CurrentContext.TestDirectory;
        //    var pattern = "XisfTestFile";

        //    var sut = new ImageData(data, 3, 3, 16, false);
        //    sut.MetaData = MetaData;
        //    var file = await sut.SaveToDisk(folder, pattern, fileType, default);

        //    var xisf = await XISF.Load(new Uri(file), false);
        //    System.IO.File.Delete(file);

        //    xisf.MetaData.Should().BeEquivalentTo(MetaData);
        //}
    }
}