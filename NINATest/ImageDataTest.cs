#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class ImageDataTest {
        private ImageMetaData MetaData;

        [OneTimeSetUp]
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
            ImageData result = await ImageData.Create(arr, 16, false);

            //Assert
            Assert.AreEqual(expX, result.Statistics.Width);
            Assert.AreEqual(expY, result.Statistics.Height);
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
            ImageData result = new ImageData(arr, width, height, 16, false);

            //Assert
            Assert.AreEqual(expX, result.Statistics.Width);
            Assert.AreEqual(expY, result.Statistics.Height);
            CollectionAssert.AreEqual(expFlatArr, result.Data.FlatArray);
        }

        [Test]
        public void CreateInstance3dArray_ExceptionThrown() {
            Assert.ThrowsAsync<NotSupportedException>(async () => {
                //Arrange
                var arr = new Int32[5, 5, 5];
                //Act
                ImageData result = await ImageData.Create(arr, 16, false);
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
            ImageData result = await ImageData.Create(arr, 16, false);
            await result.CalculateStatistics();

            //Assert
            Assert.AreEqual(stdev, result.Statistics.StDev);
            Assert.AreEqual(mean, result.Statistics.Mean);
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
            ImageData result = await ImageData.Create(arr, 16, false);
            await result.CalculateStatistics();

            //Assert
            Assert.AreEqual(stdev, result.Statistics.StDev);
            Assert.AreEqual(mean, result.Statistics.Mean);
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
            ImageData result = await ImageData.Create(arr, 16, false);
            await result.CalculateStatistics();

            //Assert
            Assert.AreEqual(stdev, result.Statistics.StDev, 0.000001);
            Assert.AreEqual(mean, result.Statistics.Mean);
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
            var imgData = new ImageData(arr, width, height, bitDepth, isBayered);

            //Assert
            Assert.AreEqual(width, imgData.Statistics.Width);
            Assert.AreEqual(height, imgData.Statistics.Height);
            Assert.AreEqual(bitDepth, imgData.Statistics.BitDepth);
            Assert.AreEqual(isBayered, imgData.Statistics.IsBayered);
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
            var imgData = await ImageData.Create(arr, bitDepth, isBayered);

            //Assert
            Assert.AreEqual(width, imgData.Statistics.Width);
            Assert.AreEqual(height, imgData.Statistics.Height);
            Assert.AreEqual(bitDepth, imgData.Statistics.BitDepth);
            Assert.AreEqual(isBayered, imgData.Statistics.IsBayered);
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
            var result = await ImageData.Create(arr, 16, false);
            await result.CalculateStatistics();

            //Assert
            Assert.AreEqual(5, result.Statistics.Min);
            Assert.AreEqual(2, result.Statistics.MinOccurrences);
            Assert.AreEqual(80, result.Statistics.Max);
            Assert.AreEqual(3, result.Statistics.MaxOccurrences);
        }

        [Test]
        [TestCase(new ushort[] { 1, 2, 3, 4, 5, 6 }, 10, 3.5, 1.5)]
        [TestCase(new ushort[] { 1, 3, 3, 2, 3, 4, 5, 6 }, 10, 3, 1)]
        [TestCase(new ushort[] { 3, 3, 3, 3, 3, 3 }, 10, 3, 0)]
        [TestCase(new ushort[] { 5, 9, 155, 8, 5, 66, 7, 5, 6, 88, 5, 4, 56, 6 }, 10, 6.5, 1.5)]
        [TestCase(new ushort[] { 10, 10, 10, 10, 10, 10, 10, 10, 15, 20, 20, 20, 20, 20, 50, 50, 50, 50 }, 10, 17.5, 7.5)]
        [TestCase(new ushort[] { 0, 0, 65535, 65535 }, 16, 32767.5, 32767.5)]
        public async Task MedianTest(ushort[] arr, int bitDepth, double expectedMedian, double expectedMAD) {
            var result = new ImageData(arr, arr.Length / 2, arr.Length / 2, bitDepth, false);
            await result.CalculateStatistics();

            Assert.AreEqual(expectedMedian, result.Statistics.Median);
            Assert.AreEqual(expectedMAD, result.Statistics.MedianAbsoluteDeviation);
        }

        [Test]
        [TestCase(NINA.Utility.Enum.FileTypeEnum.XISF)]
        [TestCase(NINA.Utility.Enum.FileTypeEnum.FITS)]
        [TestCase(NINA.Utility.Enum.FileTypeEnum.TIFF)]
        [TestCase(NINA.Utility.Enum.FileTypeEnum.TIFF_LZW)]
        [TestCase(NINA.Utility.Enum.FileTypeEnum.TIFF_ZIP)]
        public async Task SaveToDiskXISFSimpleTest(NINA.Utility.Enum.FileTypeEnum fileType) {
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };
            var folder = TestContext.CurrentContext.TestDirectory;
            var pattern = "TestFile";

            var sut = new ImageData(data, 3, 3, 16, false);

            var file = await sut.SaveToDisk(folder, pattern, fileType, default);

            System.IO.File.Exists(file).Should().BeTrue();
            System.IO.File.Delete(file);
            System.IO.Path.GetFileNameWithoutExtension(file).Should().Equals(pattern);
        }

        [Test]
        public async Task SaveToDiskPatternMetaDataTest() {
            var fileType = NINA.Utility.Enum.FileTypeEnum.XISF;
            var data = new ushort[] {
                3,1,1,
                3,4,5,
                3,2,3
            };
            var folder = TestContext.CurrentContext.TestDirectory;
            var pattern = $"$$FILTER$$" +
                $"#$$DATE$$" +
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

            var sut = new ImageData(data, 3, 3, 16, false);
            sut.MetaData = MetaData;
            var file = await sut.SaveToDisk(folder, pattern, fileType, default);
            System.IO.File.Delete(file);

            var expectedPattern = $"{MetaData.FilterWheel.Filter}" +
                $"#{MetaData.Image.ExposureStart.ToString("yyyy-MM-dd")}" +
                $"#{MetaData.Image.ExposureStart.ToString("yyyy-MM-dd_HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureStart.ToString("HH-mm-ss")}" +
                $"#{MetaData.Image.ExposureNumber}" +
                $"#{MetaData.Image.ImageType}" +
                $"#{MetaData.Camera.Binning}" +
                $"#{MetaData.Camera.Temperature}" +
                $"#{MetaData.Image.ExposureTime}" +
                $"#{MetaData.Target.Name}" +
                $"#{MetaData.Camera.Gain}" +
                $"#{MetaData.Camera.Offset}" +
                $"#{MetaData.Image.RecordedRMS.Total}" +
                $"#{MetaData.Image.RecordedRMS.Total * MetaData.Image.RecordedRMS.Scale}" +
                $"#{MetaData.Focuser.Position}" +
                $"#{Utility.ApplicationStartDate.ToString("yyyy-MM-dd")}";

            System.IO.Path.GetFileNameWithoutExtension(file).Should().Equals(expectedPattern);
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