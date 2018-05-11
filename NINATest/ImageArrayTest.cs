using NINA.Model.MyCamera;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class ImageArrayTest {

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
            ImageArray result = await ImageArray.CreateInstance(arr, false, true, 100);

            //Assert
            Assert.AreEqual(expX, result.Statistics.Width);
            Assert.AreEqual(expY, result.Statistics.Height);
            CollectionAssert.AreEqual(expFlatArr, result.FlatArray);
        }

        [Test]
        public async Task CreateInstanceFlatArrArray() {
            //Arrange
            ushort[] arr = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };
            ushort width = 4;
            ushort height = 5;

            ushort expX = 4;
            ushort expY = 5;
            ushort[] expFlatArr = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };

            //Act
            ImageArray result = await ImageArray.CreateInstance(arr, width, height, false, true, 100);

            //Assert
            Assert.AreEqual(expX, result.Statistics.Width);
            Assert.AreEqual(expY, result.Statistics.Height);
            CollectionAssert.AreEqual(expFlatArr, result.FlatArray);
        }

        [Test]
        public void CreateInstance3dArray_ExceptionThrown() {
            Assert.ThrowsAsync<NotSupportedException>(async () => { 
                //Arrange
                var arr = new Int32[5, 5, 5];
                //Act
                ImageArray result = await ImageArray.CreateInstance(arr, false, true, 100);
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
            ImageArray result = await ImageArray.CreateInstance(arr, false, true, 100);

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
            ImageArray result = await ImageArray.CreateInstance(arr, false, true, 100);

            //Assert
            Assert.AreEqual(stdev, result.Statistics.StDev);
            Assert.AreEqual(mean, result.Statistics.Mean);
        }

        [Test]
        public async Task StDevTest_LargeDataSetTest() {
            //Arrange
            int[,] arr = new int[4656, 3520];
            for (int x = 0; x < arr.GetLength(0); x += 1) {
                for (int y = 0; y < arr.GetLength(1); y += 1) {
                    arr[x, y] = 65535;
                }
            }

            double stdev = 0;
            double mean = 65535;

            //Act
            ImageArray result = await ImageArray.CreateInstance(arr, false, true, 100);

            //Assert
            Assert.AreEqual(stdev, result.Statistics.StDev);
            Assert.AreEqual(mean, result.Statistics.Mean);
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
            ImageArray result = await ImageArray.CreateInstance(arr, false, true, 100);

            //Assert
            Assert.AreEqual(5, result.Statistics.Min);
            Assert.AreEqual(2, result.Statistics.MinOccurrences);
            Assert.AreEqual(80, result.Statistics.Max);
            Assert.AreEqual(3, result.Statistics.MaxOccurrences);
        }
    }
}