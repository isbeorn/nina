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
    public class CaptureSequenceListTest {
        [TestMethod]
        public void DefaultConstructor_ValueTest() {
            //Arrange
            var l = new CaptureSequenceList();
            //Act

            //Assert
            Assert.AreEqual(string.Empty, l.TargetName, "Targetname");
            Assert.AreEqual(0, l.Count);
            Assert.AreEqual(null, l.ActiveSequence);
            Assert.AreEqual(-1, l.ActiveSequenceIndex);
            Assert.AreEqual(0, l.Delay);
        }

        [TestMethod]
        public void SequenceConstructor_ValueTest() {
            //Arrange
            var seq = new CaptureSequence();
            var l = new CaptureSequenceList(seq);
            //Act

            //Assert
            Assert.AreEqual(string.Empty, l.TargetName, "Targetname");
            Assert.AreEqual(1, l.Count);
            Assert.AreEqual(null, l.ActiveSequence);
            Assert.AreEqual(-1, l.ActiveSequenceIndex);
            Assert.AreEqual(0, l.Delay);
        }

        [TestMethod]
        public void SetActiveSequence_ValueTest() {
            //Arrange
            var seq = new CaptureSequence();
            var seq2 = new CaptureSequence();
            var l = new CaptureSequenceList();
            l.Add(seq);
            l.Add(seq2);

            //Act
            var success = l.SetActiveSequence(seq2);

            //Assert
            Assert.IsTrue(success);
            Assert.AreEqual(string.Empty, l.TargetName, "Targetname");
            Assert.AreEqual(2, l.Count);
            Assert.AreSame(seq2, l.ActiveSequence);
            Assert.AreEqual(1, l.ActiveSequenceIndex);
            Assert.AreEqual(0, l.Delay);
        }

        [TestMethod]
        public void SetActiveSequence_DoesNotExist_ReturnFalse() {
            //Arrange
            var seq = new CaptureSequence();
            var seq2 = new CaptureSequence();
            var seq3 = new CaptureSequence();
            var l = new CaptureSequenceList();
            l.Add(seq);
            l.Add(seq2);

            //Act
            var success = l.SetActiveSequence(seq3);

            //Assert
            Assert.IsFalse(success);
            Assert.AreEqual(null, l.ActiveSequence);
            Assert.AreEqual(-1, l.ActiveSequenceIndex);
        }

        [TestMethod]
        public void SetTargetName_ValueTest() {
            //Arrange
            var l = new CaptureSequenceList();
            var target = "Messier 31";
            //Act
            l.TargetName = target;

            //Assert
            Assert.AreEqual(target, l.TargetName);
        }

        [TestMethod]
        public void SetDelay_ValueTest() {
            //Arrange
            var l = new CaptureSequenceList();
            var delay = 5213;
            //Act
            l.Delay = delay;

            //Assert
            Assert.AreEqual(delay, l.Delay);
        }
    }

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
            Assert.AreEqual(1, seq.ExposureNr,"ExposureNr value not as expected");
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
            Assert.AreEqual(1,seq.ExposureNr,"ExposureNr value not as expected");
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
            Assert.AreEqual(exposuresTaken + 1, seq.ExposureNr,"ExposureNr value not as expected");
        }


    }
}
