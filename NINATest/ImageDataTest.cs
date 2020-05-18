#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class ImageDataTest {
        private ImageMetaData MetaData;

        [SetUp]
        public void Setup() {
            MetaData = new ImageMetaData() {
                Image = {
                    ExposureStart  = new DateTime(2019,1,1,12,2,3,333),
                    ExposureNumber  = 5,
                    ImageType  = "LIGHT",
                    Binning  = "1x1",
                    ExposureTime  = 300,
                    RecordedRMS  = new NINA.Model.RMS() {
                        Total = 10,
                    }
                },
                Camera = {
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
            IImageData result = await new Flipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();

            //Assert
            Assert.AreEqual(expX, result.Properties.Width);
            Assert.AreEqual(expY, result.Properties.Height);
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
            ImageData result = new ImageData(arr, width, height, 16, false, new ImageMetaData());

            //Assert
            Assert.AreEqual(expX, result.Properties.Width);
            Assert.AreEqual(expY, result.Properties.Height);
            CollectionAssert.AreEqual(expFlatArr, result.Data.FlatArray);
        }

        [Test]
        public void CreateInstance3dArray_ExceptionThrown() {
            Assert.ThrowsAsync<NotSupportedException>(async () => {
                //Arrange
                var arr = new Int32[5, 5, 5];
                //Act
                IImageData result = await new Flipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
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
            IImageData result = await new Flipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            //Assert
            Assert.AreEqual(stdev, resultStatistics.StDev);
            Assert.AreEqual(mean, resultStatistics.Mean);
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
            IImageData result = await new Flipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            //Assert
            Assert.AreEqual(stdev, resultStatistics.StDev);
            Assert.AreEqual(mean, resultStatistics.Mean);
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
            IImageData result = await new Flipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            //Assert
            Assert.AreEqual(stdev, resultStatistics.StDev, 0.000001);
            Assert.AreEqual(mean, resultStatistics.Mean);
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
            var imgData = await new ImageArrayExposureData(arr, width, height, bitDepth, isBayered, new ImageMetaData()).ToImageData();

            //Assert
            Assert.AreEqual(width, imgData.Properties.Width);
            Assert.AreEqual(height, imgData.Properties.Height);
            Assert.AreEqual(bitDepth, imgData.Properties.BitDepth);
            Assert.AreEqual(isBayered, imgData.Properties.IsBayered);
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
            var imgData = await new Flipped2DExposureData(arr, bitDepth, isBayered, new ImageMetaData()).ToImageData();

            //Assert
            Assert.AreEqual(width, imgData.Properties.Width);
            Assert.AreEqual(height, imgData.Properties.Height);
            Assert.AreEqual(bitDepth, imgData.Properties.BitDepth);
            Assert.AreEqual(isBayered, imgData.Properties.IsBayered);
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
            var result = await new Flipped2DExposureData(arr, 16, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            //Assert
            Assert.AreEqual(5, resultStatistics.Min);
            Assert.AreEqual(2, resultStatistics.MinOccurrences);
            Assert.AreEqual(80, resultStatistics.Max);
            Assert.AreEqual(3, resultStatistics.MaxOccurrences);
        }

        [Test]
        [TestCase(new ushort[] { 1, 2, 3, 4, 5, 6 }, 10, 3.5, 1.5)]
        [TestCase(new ushort[] { 1, 3, 3, 2, 3, 4, 5, 6 }, 10, 3, 1)]
        [TestCase(new ushort[] { 3, 3, 3, 3, 3, 3 }, 10, 3, 0)]
        [TestCase(new ushort[] { 5, 9, 155, 8, 5, 66, 7, 5, 6, 88, 5, 4, 56, 6 }, 10, 6.5, 1.5)]
        [TestCase(new ushort[] { 10, 10, 10, 10, 10, 10, 10, 10, 15, 20, 20, 20, 20, 20, 50, 50, 50, 50 }, 10, 17.5, 7.5)]
        [TestCase(new ushort[] { 0, 0, 65535, 65535 }, 16, 32767.5, 32767.5)]
        public async Task MedianTest(ushort[] arr, int bitDepth, double expectedMedian, double expectedMAD) {
            var result = await new ImageArrayExposureData(arr, arr.Length / 2, 2, bitDepth, false, new ImageMetaData()).ToImageData();
            var resultStatistics = await result.Statistics.Task;

            Assert.AreEqual(expectedMedian, resultStatistics.Median);
            Assert.AreEqual(expectedMAD, resultStatistics.MedianAbsoluteDeviation);
        }

        [Test]
        [TestCase(NINA.Utility.Enum.FileTypeEnum.XISF, ".xisf")]
        [TestCase(NINA.Utility.Enum.FileTypeEnum.FITS, ".fits")]
        [TestCase(NINA.Utility.Enum.FileTypeEnum.TIFF, ".tif")]
        public async Task SaveToDiskXISFSimpleTest(NINA.Utility.Enum.FileTypeEnum fileType, string extension) {
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

            //var sut = await new ImageArrayExposureData(data, 3, 3, 16, false, new ImageMetaData()).ToImageData();
            var sut = new ImageData(data, 3, 3, 16, false, MetaData);

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
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF
            };

            var sut = new ImageData(data, 3, 3, 16, false, MetaData);
            var file = await sut.SaveToDisk(fileSaveInfo, default);
            File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.AddHours(-12).ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber.ToString("0000")}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{string.Format("{0:0.00}", MetaData.Camera.Temperature)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.ExposureTime)}" +
                $"#{MetaData.Target.Name}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Gain)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Offset)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.RecordedRMS.Total)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale)}" +
                $"#{string.Format("{0:0.00}", MetaData.Focuser.Position)}" +
                $"#{Utility.ApplicationStartDate.ToString("yyyy-MM-dd")}";

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

            var fileSaveInfo = new FileSaveInfo {
                FilePath = TestContext.CurrentContext.TestDirectory,
                FilePattern = pattern,
                FileType = NINA.Utility.Enum.FileTypeEnum.TIFF,
                TIFFCompressionType = NINA.Utility.Enum.TIFFCompressionTypeEnum.LZW
            };

            var sut = new ImageData(data, 3, 3, 16, false, new ImageMetaData());
            var file = await sut.SaveToDisk(fileSaveInfo, default);
            File.Delete(file);

            var expectedPattern = $"#0001-01-01##0001-01-01_00-00-00#00-00-00#-0001##1x1#########{Utility.ApplicationStartDate.ToString("yyyy-MM-dd")}.tif";

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
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF
            };

            var sut = new ImageData(data, 3, 3, 16, false, MetaData);
            var file = await sut.PrepareSave(fileSaveInfo, default);
            file = sut.FinalizeSave(file, pattern);
            File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.AddHours(-12).ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber.ToString("0000")}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{string.Format("{0:0.00}", MetaData.Camera.Temperature)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.ExposureTime)}" +
                $"#{MetaData.Target.Name}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Gain)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Offset)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.RecordedRMS.Total)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale)}" +
                $"#{string.Format("{0:0.00}", MetaData.Focuser.Position)}" +
                $"#{Utility.ApplicationStartDate.ToString("yyyy-MM-dd")}";

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
                FileType = NINA.Utility.Enum.FileTypeEnum.XISF
            };

            var invalidChars = Path.GetInvalidPathChars();
            MetaData.Target.Name = string.Join("", invalidChars);

            var sut = new ImageData(data, 3, 3, 16, false, MetaData);
            var file = await sut.PrepareSave(fileSaveInfo, default);
            file = sut.FinalizeSave(file, pattern);
            File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.AddHours(-12).ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber.ToString("0000")}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{string.Format("{0:0.00}", MetaData.Camera.Temperature)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.ExposureTime)}" +
                $"#{new string('_', invalidChars.Length)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Gain)}" +
                $"#{string.Format("{0:0}", MetaData.Camera.Offset)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.RecordedRMS.Total)}" +
                $"#{string.Format("{0:0.00}", MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale)}" +
                $"#{string.Format("{0:0.00}", MetaData.Focuser.Position)}" +
                $"#{Utility.ApplicationStartDate.ToString("yyyy-MM-dd")}";

            Path.GetFileName(file).Should().Be($"{expectedPattern}.{fileSaveInfo.FileType.ToString().ToLower()}");
        }

        //[Test]
        //public async Task SaveToDiskXISFDeepMetaDataTest() {
        //    Uncomment once xisf loading knows to extract meta data
        //    var fileType = NINA.Utility.Enum.FileTypeEnum.XISF;
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