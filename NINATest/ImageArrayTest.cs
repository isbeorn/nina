using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NINA.Utility;
using System.Threading.Tasks;
using System.Linq;
using NINA.Model.MyCamera;

namespace NINATest {
    [TestClass]
    public class ImageArrayTest {
        [TestMethod]
        public async Task createInstance2dArray() {
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
            ImageArray result = await ImageArray.CreateInstance(arr);  

            //Assert
            Assert.AreEqual(expX, result.Statistics.Width);
            Assert.AreEqual(expY, result.Statistics.Height);
            CollectionAssert.AreEqual(expFlatArr, result.FlatArray);
        }

        [TestMethod]
        public async Task createInstanceFlatArrArray() {
            //Arrange
            ushort[] arr = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };
            ushort width = 4;
            ushort height = 5;

            ushort expX = 4;
            ushort expY = 5;
            ushort[] expFlatArr = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };

            //Act
            ImageArray result = await ImageArray.CreateInstance(arr, width, height);

            //Assert
            Assert.AreEqual(expX, result.Statistics.Width);
            Assert.AreEqual(expY, result.Statistics.Height);
            CollectionAssert.AreEqual(expFlatArr, result.FlatArray);
        }


        [TestMethod]
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
            ImageArray result = await ImageArray.CreateInstance(arr);

            //Assert
            Assert.AreEqual(stdev, result.Statistics.StDev);
            Assert.AreEqual(mean, result.Statistics.Mean);
        }

        [TestMethod]
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
            ImageArray result = await ImageArray.CreateInstance(arr);

            //Assert
            Assert.AreEqual(stdev, result.Statistics.StDev);
            Assert.AreEqual(stdev, result.Statistics.Mean);
        }
    }
}
