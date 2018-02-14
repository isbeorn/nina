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
            Assert.AreEqual(seq, l.ActiveSequence);
            Assert.AreEqual(1, l.ActiveSequenceIndex);
            Assert.AreEqual(0, l.Delay);
        }

        [TestMethod]
        public void GetNextSequence_ModeStandard_Initial() {
            //Arrange
            var seq = new CaptureSequence();
            var seq2 = new CaptureSequence();
            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.STANDARD;
            l.Add(seq);
            l.Add(seq2);

            //Act
            var nextSeq = l.Next();

            //Assert
            Assert.AreEqual(string.Empty, l.TargetName, "Targetname");
            Assert.AreSame(seq, nextSeq);
            Assert.AreEqual(2, l.Count);
            Assert.AreSame(seq, l.ActiveSequence);
            Assert.AreEqual(1, l.ActiveSequenceIndex);
            Assert.AreEqual(0, l.Delay);
        }

        [TestMethod]
        public void GetNextSequence_ModeStandard_NextSequenceSelected() {
            //Arrange
            var seq = new CaptureSequence() { ProgressExposureCount = 2 };
            var seq2 = new CaptureSequence();
            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.STANDARD;
            l.Add(seq);
            l.Add(seq2);

            //Act
            var nextSeq = l.Next();
            nextSeq = l.Next();
            nextSeq = l.Next();

            //Assert
            Assert.AreEqual(string.Empty, l.TargetName, "Targetname");
            Assert.AreSame(seq2, nextSeq);
            Assert.AreEqual(2, l.Count);
            Assert.AreSame(seq2, l.ActiveSequence);
            Assert.AreEqual(2, l.ActiveSequenceIndex);
            Assert.AreEqual(0, l.Delay);
        }

        [TestMethod]
        public void GetNextSequence_ModeStandard_AllFinished() {
            //Arrange
            var seq = new CaptureSequence() { ProgressExposureCount = 5 };
            var seq2 = new CaptureSequence() { ProgressExposureCount = 5 };
            var seq3 = new CaptureSequence() { ProgressExposureCount = 5 };
            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.STANDARD;

            l.Add(seq);
            l.Add(seq2);
            l.Add(seq3);

            //Act
            CaptureSequence actualSeq;
            while ((actualSeq = l.Next()) != null) {

            }

            //Assert
            Assert.AreEqual(null, l.ActiveSequence);
            Assert.AreEqual(-1, l.ActiveSequenceIndex);
            Assert.AreEqual(0, l.Items.Where(x => x.ProgressExposureCount < x.TotalExposureCount).Count());
        }

        [TestMethod]
        public void GetNextSequence_ModeStandard_EmptyListNextNull() {
            //Arrange
            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.STANDARD;

            //Act
            var actual = l.Next();

            //Assert
            Assert.AreSame(null, actual);
            Assert.AreEqual(null, l.ActiveSequence);
            Assert.AreEqual(-1, l.ActiveSequenceIndex);
        }

        [TestMethod]
        public void GetNextSequence_ModeRotate_EmptyListNextNull() {
            //Arrange
            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.ROTATE;
            
            //Act
            var actual = l.Next();

            //Assert
            Assert.AreSame(null, actual);
            Assert.AreEqual(null, l.ActiveSequence);
            Assert.AreEqual(-1, l.ActiveSequenceIndex);
        }

        [TestMethod]
        public void GetNextSequence_ModeRotate_NextSequenceSelected() {
            //Arrange
            var seq = new CaptureSequence() { TotalExposureCount = 5 };
            var seq2 = new CaptureSequence() { TotalExposureCount = 5 };
            var seq3 = new CaptureSequence() { TotalExposureCount = 5 };
            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.ROTATE;

            l.Add(seq);
            l.Add(seq2);
            l.Add(seq3);

            //Act
            var actualFirst = l.Next();
            var actualSecond = l.Next();
            var actualThird = l.Next();
            var actualFourth = l.Next();

            //Assert
            Assert.AreSame(seq, actualFirst, "First wrong");
            Assert.AreSame(seq2, actualSecond, "Second wrong");
            Assert.AreSame(seq3, actualThird, "Third wrong");
            Assert.AreSame(seq, actualFourth, "Fourth wrong");
        }

        [TestMethod]
        public void GetNextSequence_ModeRotate_FirstEmptySecondSelected() {
            //Arrange
            var seq = new CaptureSequence() { ProgressExposureCount = 0, TotalExposureCount = 0 };
            var seq2 = new CaptureSequence() { ProgressExposureCount = 5, TotalExposureCount = 10 };
            var seq3 = new CaptureSequence() { ProgressExposureCount = 5, TotalExposureCount = 7 };
            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.ROTATE;

            l.Add(seq);
            l.Add(seq2);
            l.Add(seq3);

            //Act
            var actual = l.Next();

            //Assert
            Assert.AreSame(seq2, actual);
        }

        [TestMethod]
        public void GetNextSequence_ModeRotate_AllFinished() {
            //Arrange
            var seq = new CaptureSequence() { TotalExposureCount = 5 };
            var seq2 = new CaptureSequence() { TotalExposureCount = 5 };
            var seq3 = new CaptureSequence() { TotalExposureCount = 5 };
            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.ROTATE;

            l.Add(seq);
            l.Add(seq2);
            l.Add(seq3);

            //Act
            CaptureSequence actualSeq;
            while ((actualSeq = l.Next()) != null) {

            }

            //Assert
            Assert.AreEqual(null, l.ActiveSequence);
            Assert.AreEqual(-1, l.ActiveSequenceIndex);
            Assert.AreEqual(0, l.Items.Where(x => x.ProgressExposureCount < x.TotalExposureCount || x.ProgressExposureCount > x.TotalExposureCount).Count());
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


        [TestMethod]
        public void DeleteSequenceDuringPause_NextItemSelected() {
            var seq = new CaptureSequence() { ProgressExposureCount = 0, TotalExposureCount = 5 };
            var seq2 = new CaptureSequence() { TotalExposureCount = 10 };

            var l = new CaptureSequenceList();
            l.Add(seq);


            l.Next();
            l.Next();
            l.Next();

            l.RemoveAt(l.ActiveSequenceIndex - 1);

            l.Add(seq2);

            Assert.AreEqual(seq2, l.ActiveSequence);
        }

        [TestMethod]
        public void DeleteSequenceDuringPause_ModeRotate_NextItemSelected() {
            var seq = new CaptureSequence() { ProgressExposureCount = 0, TotalExposureCount = 5 };
            var seq2 = new CaptureSequence() { TotalExposureCount = 10 };

            var l = new CaptureSequenceList();
            l.Mode = SequenceMode.ROTATE;
            l.Add(seq);


            l.Next();
            l.Next();
            l.Next();

            l.RemoveAt(l.ActiveSequenceIndex - 1);

            l.Add(seq2);

            Assert.AreEqual(seq2, l.ActiveSequence);
        }

        [TestMethod]
        public void AddFirstSequence_ActiveSequenceSet() {
            var seq = new CaptureSequence() { ProgressExposureCount = 0, TotalExposureCount = 5 };

            var l = new CaptureSequenceList();
            l.Add(seq);
            
            Assert.AreEqual(seq, l.ActiveSequence);
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
                seq.ProgressExposureCount++;
            }

            //Assert
            Assert.AreEqual(exposuresTaken, seq.ProgressExposureCount, "ProgressExposureCount value not as expected");
            Assert.AreEqual(exposureCount, seq.TotalExposureCount, "TotalExposureCount value not as expected");
        }


    }
}
