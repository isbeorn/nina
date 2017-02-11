using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NINA.Utility;
using System.Threading.Tasks;
using System.Linq;

namespace NINATest {
    /*[TestClass]
    public class ImageArrayTest {
        [TestMethod]
        public async Task createInstanceAsync_ushort() {
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
            NINA.Utility.ImageArray.ImageArray<UInt16> result = await NINA.Utility.ImageArray.ImageArray<UInt16>.createInstanceAsync(arr);  

            //Assert
            Assert.AreEqual(expX, result.X);
            Assert.AreEqual(expY, result.Y);
            CollectionAssert.AreEqual(expFlatArr, result.FlatArray);
        }

        [TestMethod]
        public async Task createInstanceAsync_byte() {
            //Arrange
            int[,] arr = new int[4, 5];
            arr[0, 0] = 100; arr[1, 0] = 200; arr[2, 0] = 300; arr[3, 0] = 400;
            arr[0, 1] = 500; arr[1, 1] = 600; arr[2, 1] = 700; arr[3, 1] = 800;
            arr[0, 2] = 900; arr[1, 2] = 1000; arr[2, 2] = 1100; arr[3, 2] = 1200;
            arr[0, 3] = 1300; arr[1, 3] = 1400; arr[2, 3] = 1500; arr[3, 3] = 1600;
            arr[0, 4] = 1700; arr[1, 4] = 1800; arr[2, 4] = 1900; arr[3, 4] = 2000;

            byte expX = 4;
            byte expY = 5;
            byte[] expFlatArr = { 100, 200, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };

            //Act
            NINA.Utility.ImageArray.ImageArray<byte> result = await NINA.Utility.ImageArray.ImageArray<byte>.createInstanceAsync(arr); 

            //Assert
            Assert.AreEqual(expX, result.X);
            Assert.AreEqual(expY, result.Y);
            CollectionAssert.AreEqual(expFlatArr, result.FlatArray);
        }
    }*/
}
