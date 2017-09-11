using Microsoft.VisualStudio.TestTools.UnitTesting;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {
    [TestClass]
    public class CaptureSequenceTest {
        [TestMethod]
        public void DefaultConstructor_ValueTest() {
            //Arrange

            //Act
            var seq = new CaptureSequence();

            //Assert
            Assert.AreEqual(1, seq.Binning.X, "Binning X value not as expected");
            Assert.AreEqual(1, seq.Binning.Y, "Binning X value not as expected");
            Assert.AreEqual(false, seq.Dither, "Dither value not as expected");
            Assert.AreEqual(1, seq.DitherAmount, "DitherAmount value not as expected");
            Assert.AreEqual(1, seq.ExposureCount, "ExposureCount value not as expected");
            Assert.AreEqual(1, seq.ExposureTime, "ExposureTime value not as expected");
            Assert.AreEqual(null, seq.FilterType, "FilterType value not as expected");
            Assert.AreEqual(-1, seq.Gain, "Gain value not as expected");
            Assert.AreEqual(CaptureSequence.ImageTypes.LIGHT, seq.ImageType, "ImageType value not as expected");
            Assert.AreEqual(0, seq.ProgressExposureCount, "ProgressExposureCount value not as expected");
            Assert.AreEqual(1, seq.TotalExposureCount, "TotalExposureCount value not as expected");        
        }

        [TestMethod]
        public void Constructor_ValueTest() {
            //Arrange
            var exposureTime = 5;
            var imageType = CaptureSequence.ImageTypes.BIAS;
            var filter = new FilterInfo("Red", 1234, 3);
            var binning = new BinningMode(2, 3);
            var exposureCount = 20;

            //Act
            var seq = new CaptureSequence(exposureTime, imageType, filter, binning, exposureCount);

            //Assert
            Assert.AreEqual(binning.X, seq.Binning.X, "Binning X value not as expected");
            Assert.AreEqual(binning.Y, seq.Binning.Y, "Binning X value not as expected");
            Assert.AreEqual(false, seq.Dither, "Dither value not as expected");
            Assert.AreEqual(1, seq.DitherAmount, "DitherAmount value not as expected");
            Assert.AreEqual(exposureCount, seq.ExposureCount, "ExposureCount value not as expected");
            Assert.AreEqual(exposureTime, seq.ExposureTime, "ExposureTime value not as expected");
            Assert.AreEqual(filter, seq.FilterType, "FilterType value not as expected");
            Assert.AreEqual(-1, seq.Gain, "Gain value not as expected");
            Assert.AreEqual(imageType, seq.ImageType, "ImageType value not as expected");
            Assert.AreEqual(0, seq.ProgressExposureCount, "ProgressExposureCount value not as expected");
            Assert.AreEqual(exposureCount, seq.TotalExposureCount, "TotalExposureCount value not as expected");
        }

        [TestMethod]
        public void ReduceExposureCount_ProgressReflectedCorrectly() {
            //Arrange
            var exposureTime = 5;
            var imageType = CaptureSequence.ImageTypes.BIAS;
            var filter = new FilterInfo("Red", 1234, 3);
            var binning = new BinningMode(2, 3);
            var exposureCount = 20;
            var seq = new CaptureSequence(exposureTime, imageType, filter, binning, exposureCount);

            var exposuresTaken = 5;

            //Act
            for(int i = 0; i < exposuresTaken; i++) {
                seq.ExposureCount--;
            }

            //Assert
            Assert.AreEqual(exposureCount - exposuresTaken, seq.ExposureCount, "ExposureCount value not as expected");
            Assert.AreEqual(exposuresTaken, seq.ProgressExposureCount, "ProgressExposureCount value not as expected");
            Assert.AreEqual(exposureCount, seq.TotalExposureCount, "TotalExposureCount value not as expected");
        }


    }
}
