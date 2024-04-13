#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NUnit.Framework;
using System;
using System.Linq;
using NUnit.Framework.Legacy;

namespace NINA.Test {

    [TestFixture]
    public class CaptureSequenceListTest {

        [Test]
        public void DefaultConstructor_ValueTest() {
            //Arrange
            var l = new CaptureSequenceList();
            //Act

            //Assert
            ClassicAssert.AreEqual(string.Empty, l.TargetName, "Targetname");
            ClassicAssert.AreEqual(0, l.Count);
            ClassicAssert.AreEqual(0, l.Delay);
        }

        [Test]
        public void SequenceConstructor_ValueTest() {
            //Arrange
            var seq = new CaptureSequence();
            var l = new CaptureSequenceList(seq);
            //Act

            //Assert
            ClassicAssert.AreEqual(string.Empty, l.TargetName, "Targetname");
            ClassicAssert.AreEqual(1, l.Count);
            ClassicAssert.AreEqual(0, l.Delay);
        }

        [Test]
        public void SetTargetName_ValueTest() {
            //Arrange
            var l = new CaptureSequenceList();
            var target = "Messier 31";
            //Act
            l.TargetName = target;

            //Assert
            ClassicAssert.AreEqual(target, l.TargetName);
        }

        [Test]
        public void SetDelay_ValueTest() {
            //Arrange
            var l = new CaptureSequenceList();
            var delay = 5213;
            //Act
            l.Delay = delay;

            //Assert
            ClassicAssert.AreEqual(delay, l.Delay);
        }

        [Test]
        public void CoordinatesTest_SetCoordinates_RaDecPartialsEqualCoordinates() {
            var l = new CaptureSequenceList();
            var coordinates = new Coordinates(10, 10, Epoch.J2000, Coordinates.RAType.Hours);

            l.Coordinates = coordinates.Transform(Epoch.J2000);

            ClassicAssert.AreEqual(coordinates.RA, l.RAHours + l.RAMinutes + l.RASeconds);
            ClassicAssert.AreEqual(coordinates.Dec, l.DecDegrees + l.DecMinutes + l.DecSeconds);
        }

        [TestCase(5, 10, 15, 5.17083333333333)]
        [TestCase(0, 0, 0, 0)]
        [TestCase(15, 01, 01, 15.01694444444444)]
        [TestCase(0, 0, 1, 0.00027777777)]  //Lower bound
        [TestCase(23, 59, 59, 23.99972222222222)]   //upper bound
        [TestCase(0, 0, 0, 0)]  //Lowest bound
        //[TestCase(24, 0, 0, 0)] //Overflow
        //[TestCase(0, 0, -1, 0)] //Overflow
        public void CoordinatesTest_ManualInput_RACheck(int raHours, int raMinutes, double raSeconds, double expected) {
            var l = new CaptureSequenceList();
            var coordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours);

            l.RAHours = raHours;
            l.RAMinutes = raMinutes;
            l.RASeconds = raSeconds;

            ClassicAssert.AreEqual(expected, l.Coordinates.RA, 0.000001, "Coordinates failed");
            ClassicAssert.AreEqual(raHours, l.RAHours, 0.000001, "Hours failed");
            ClassicAssert.AreEqual(raMinutes, l.RAMinutes, 0.000001, "Minutes failed");
            ClassicAssert.AreEqual(raSeconds, l.RASeconds, 0.000001, "Seconds failed");
        }

        [TestCase(5, 10, 15, 5.17083333333333)]
        [TestCase(0, 0, 0, 0)]
        [TestCase(15, 01, 01, 15.01694444444444)]
        [TestCase(-15, 01, 01, -15.01694444444444)]
        [TestCase(0, 0, 1, 0.00027777777)] //Low bound
        [TestCase(89, 59, 59, 89.99972222222222)] //high bound
        [TestCase(-90, 0, 0, -90)] //Lowest bound
        [TestCase(90, 0, 0, 90)] //Highest bound
        [TestCase(0, 0, -1, -0.00027777777)] //Low bound
        [TestCase(-89, 59, 59, -89.99972222222222)] //high bound
        //[TestCase(90, 0, 1, 90)] //overflow
        //[TestCase(-90, 0, 1, 90)] //overflow
        public void CoordinatesTest_ManualInput_DecCheck(int decDegrees, int decMinutes, double decSeconds, double expected) {
            var l = new CaptureSequenceList();
            var coordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours);

            l.DecDegrees = decDegrees;
            l.DecMinutes = decMinutes;
            l.DecSeconds = decSeconds;

            ClassicAssert.AreEqual(expected, l.Coordinates.Dec, 0.000001, "Coordinates failed");
            ClassicAssert.AreEqual(decDegrees, l.DecDegrees, 0.000001, "Degrees failed");
            ClassicAssert.AreEqual(Math.Abs(decMinutes), l.DecMinutes, 0.000001, "Minutes failed");
            ClassicAssert.AreEqual(Math.Abs(decSeconds), l.DecSeconds, 0.000001, "Seconds failed");
        }
    }

    [TestFixture]
    public class CaptureSequenceTest {

        [Test]
        public void DefaultConstructor_ValueTest() {
            //Arrange

            //Act
            var seq = new CaptureSequence();

            //Assert
            ClassicAssert.AreEqual(1, seq.Binning.X, "Binning X value not as expected");
            ClassicAssert.AreEqual(1, seq.Binning.Y, "Binning X value not as expected");
            ClassicAssert.AreEqual(false, seq.Dither, "Dither value not as expected");
            ClassicAssert.AreEqual(1, seq.DitherAmount, "DitherAmount value not as expected");
            ClassicAssert.AreEqual(1, seq.ExposureTime, "ExposureTime value not as expected");
            ClassicAssert.AreEqual(null, seq.FilterType, "FilterType value not as expected");
            ClassicAssert.AreEqual(-1, seq.Gain, "Gain value not as expected");
            ClassicAssert.AreEqual(CaptureSequence.ImageTypes.LIGHT, seq.ImageType, "ImageType value not as expected");
            ClassicAssert.AreEqual(0, seq.ProgressExposureCount, "ProgressExposureCount value not as expected");
            ClassicAssert.AreEqual(1, seq.TotalExposureCount, "TotalExposureCount value not as expected");
            ClassicAssert.AreEqual(true, seq.Enabled, "Enabled value not as expected");
        }

        [Test]
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
            ClassicAssert.AreEqual(binning.X, seq.Binning.X, "Binning X value not as expected");
            ClassicAssert.AreEqual(binning.Y, seq.Binning.Y, "Binning X value not as expected");
            ClassicAssert.AreEqual(false, seq.Dither, "Dither value not as expected");
            ClassicAssert.AreEqual(1, seq.DitherAmount, "DitherAmount value not as expected");
            ClassicAssert.AreEqual(exposureTime, seq.ExposureTime, "ExposureTime value not as expected");
            ClassicAssert.AreEqual(filter, seq.FilterType, "FilterType value not as expected");
            ClassicAssert.AreEqual(-1, seq.Gain, "Gain value not as expected");
            ClassicAssert.AreEqual(imageType, seq.ImageType, "ImageType value not as expected");
            ClassicAssert.AreEqual(0, seq.ProgressExposureCount, "ProgressExposureCount value not as expected");
            ClassicAssert.AreEqual(exposureCount, seq.TotalExposureCount, "TotalExposureCount value not as expected");
            ClassicAssert.AreEqual(true, seq.Enabled, "Enabled value not as expected");
        }

        [Test]
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
            for (int i = 0; i < exposuresTaken; i++) {
                seq.ProgressExposureCount++;
            }

            //Assert
            ClassicAssert.AreEqual(exposuresTaken, seq.ProgressExposureCount, "ProgressExposureCount value not as expected");
            ClassicAssert.AreEqual(exposureCount, seq.TotalExposureCount, "TotalExposureCount value not as expected");
        }
    }
}