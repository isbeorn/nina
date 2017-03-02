using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NINA.Utility;
using System.Threading.Tasks;
using System.Linq;

namespace NINATest {
    [TestClass]
    public class UtilityTest {



        [TestMethod]
        public async Task convert2DArray_FlatArraySequenceTest() {
            //Arrange
            int[,] arr = new int[4, 5];
            arr[0, 0] = 100; arr[1, 0] = 200; arr[2, 0] = 300; arr[3, 0] = 400;
            arr[0, 1] = 500; arr[1, 1] = 600; arr[2, 1] = 700; arr[3, 1] = 800;
            arr[0, 2] = 900; arr[1, 2] = 1000; arr[2, 2] = 1100; arr[3, 2] = 1200;
            arr[0, 3] = 1300; arr[1, 3] = 1400; arr[2, 3] = 1500; arr[3, 3] = 1600;
            arr[0, 4] = 1700; arr[1, 4] = 1800; arr[2, 4] = 1900; arr[3, 4] = 2000;
            
            ushort expX = 4;
            ushort expY = 5;
            ushort[] expFlatArr = { 100,200,300,400,500,600,700,800,900,1000,1100,1200,1300,1400,1500,1600,1700,1800,1900,2000 };

            //Act
            Utility.ImageArray result = await Utility.Convert2DArray(arr);

            //Assert
            Assert.AreEqual(expX, result.X);
            Assert.AreEqual(expY, result.Y);
            CollectionAssert.AreEqual(expFlatArr, result.FlatArray);
        }


        [TestMethod]
        public async Task convert2DArray_Min0_StDevTest() {
            //Arrange
            int[,] arr = new int[8, 8];

             arr[0,0] = 440; 
             arr[1,1] = 4700; 
             arr[2,2] = 5000; 
             arr[3,3] = 5100; 
             arr[4,4] = 4700; 
             arr[0,0] = 460; 
             arr[1,1] = 4800; 
             arr[2,2] = 4600; 
             arr[3,3] = 4600; 
             arr[4,4] = 5700;
             arr[0,0] = 430;
             arr[1,1] = 5400;
             arr[2,2] = 5600;
             arr[3,3] = 5400;
             arr[4,4] = 4400;
             arr[0,0] = 5600;
             arr[1,1] = 5000;
             arr[2,2] = 4800;
             arr[3,3] = 5100;
             arr[4,4] = 490;

            ushort expMaxStdDev = 3433;
            ushort expMinStdDev = 0;
            
            //Act
            Utility.ImageArray result = await Utility.Convert2DArray(arr);

            //Assert
            Assert.AreEqual(expMinStdDev, result.MinStDev);
            Assert.AreEqual(expMaxStdDev, result.MaxStDev);
        }

        [TestMethod]
        public async Task convert2DArray_ExtremeDistribution_StDevTest() {
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

            ushort expMaxStdDev = ushort.MaxValue;
            ushort expMinStdDev = 0;

            //Act
            Utility.ImageArray result = await Utility.Convert2DArray(arr);

            //Assert
            Assert.AreEqual(expMinStdDev, result.MinStDev);
            Assert.AreEqual(expMaxStdDev, result.MaxStDev);
        }


        /*[TestMethod]
        public async Task TstretchArray_StretchTest() {
            //Arrange
            Utility.TImageArray<ushort> iarr = new Utility.TImageArray<ushort>();
            ushort[] flatarr = { 1, 2, 1, 2, 20, 20, 20, 20, 21, 21, 21, 21, 21, 21, 20, 49, 51, 50, 60000, 1 };

            iarr.FlatArray = flatarr;
            iarr.minStDev = 0;
            iarr.maxStDev = 50;

            ushort[] expFlatarr = { 1310, 2621, 1310, 2621, 26214, 26214, 26214, 26214, 27524, 27524, 27524, 27524, 27524, 27524, 26214, 64224, 65535, 65535, 65535, 1310 };

            //Act
            ushort[] result = await Utility.TstretchArray<ushort>(iarr);

            //Assert
            CollectionAssert.AreEqual(expFlatarr, result);

        }*/


    }
}
