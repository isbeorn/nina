#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

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
            Assert.That(l.TargetName, Is.Empty, "Targetname");
            Assert.That(l.Count, Is.EqualTo(0));
            Assert.That(l.Delay, Is.EqualTo(0));
        }

        [Test]
        public void SequenceConstructor_ValueTest() {
            //Arrange
            var seq = new CaptureSequence();
            var l = new CaptureSequenceList(seq);
            //Act

            //Assert
            Assert.That(l.TargetName, Is.Empty, "Targetname");
            Assert.That(l.Count, Is.EqualTo(1));
            Assert.That(l.Delay, Is.EqualTo(0));
        }

        [Test]
        public void SetTargetName_ValueTest() {
            //Arrange
            var l = new CaptureSequenceList();
            var target = "Messier 31";
            //Act
            l.TargetName = target;

            //Assert
            Assert.That(l.TargetName, Is.EqualTo(target));
        }

        [Test]
        public void SetDelay_ValueTest() {
            //Arrange
            var l = new CaptureSequenceList();
            var delay = 5213;
            //Act
            l.Delay = delay;

            //Assert
            Assert.That(l.Delay, Is.EqualTo(delay));
        }

        [Test]
        public void CoordinatesTest_SetCoordinates_RaDecPartialsEqualCoordinates() {
            var l = new CaptureSequenceList();
            var coordinates = new Coordinates(10, 10, Epoch.J2000, Coordinates.RAType.Hours);

            l.Coordinates = coordinates.Transform(Epoch.J2000);

            Assert.That(l.RAHours + l.RAMinutes + l.RASeconds, Is.EqualTo(coordinates.RA));
            Assert.That(l.DecDegrees + l.DecMinutes + l.DecSeconds, Is.EqualTo(coordinates.Dec));
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

            Assert.That(l.Coordinates.RA, Is.EqualTo(expected).Within(0.000001), "Coordinates failed");
            Assert.That(l.RAHours, Is.EqualTo(raHours).Within(0.000001), "Hours failed");
            Assert.That(l.RAMinutes, Is.EqualTo(raMinutes).Within(0.000001), "Minutes failed");
            Assert.That(l.RASeconds, Is.EqualTo(raSeconds).Within(0.000001), "Seconds failed");
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

            Assert.That(l.Coordinates.Dec, Is.EqualTo(expected).Within(0.000001), "Coordinates failed");
            Assert.That(l.DecDegrees, Is.EqualTo(decDegrees).Within(0.000001), "Degrees failed");
            Assert.That(l.DecMinutes, Is.EqualTo(Math.Abs(decMinutes)).Within(0.000001), "Minutes failed");
            Assert.That(l.DecSeconds, Is.EqualTo(Math.Abs(decSeconds)).Within(0.000001), "Seconds failed");
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
            Assert.That(seq.Binning.X, Is.EqualTo(1), "Binning X value not as expected");
            Assert.That(seq.Binning.Y, Is.EqualTo(1), "Binning X value not as expected");
            Assert.That(seq.Dither, Is.EqualTo(false), "Dither value not as expected");
            Assert.That(seq.DitherAmount, Is.EqualTo(1), "DitherAmount value not as expected");
            Assert.That(seq.ExposureTime, Is.EqualTo(1), "ExposureTime value not as expected");
            Assert.That(seq.FilterType, Is.EqualTo(null), "FilterType value not as expected");
            Assert.That(seq.Gain, Is.EqualTo(-1), "Gain value not as expected");
            Assert.That(seq.ImageType, Is.EqualTo(CaptureSequence.ImageTypes.LIGHT), "ImageType value not as expected");
            Assert.That(seq.ProgressExposureCount, Is.EqualTo(0), "ProgressExposureCount value not as expected");
            Assert.That(seq.TotalExposureCount, Is.EqualTo(1), "TotalExposureCount value not as expected");
            Assert.That(seq.Enabled, Is.EqualTo(true), "Enabled value not as expected");
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
            Assert.That(seq.Binning.X, Is.EqualTo(binning.X), "Binning X value not as expected");
            Assert.That(seq.Binning.Y, Is.EqualTo(binning.Y), "Binning X value not as expected");
            Assert.That(seq.Dither, Is.EqualTo(false), "Dither value not as expected");
            Assert.That(seq.DitherAmount, Is.EqualTo(1), "DitherAmount value not as expected");
            Assert.That(seq.ExposureTime, Is.EqualTo(exposureTime), "ExposureTime value not as expected");
            Assert.That(seq.FilterType, Is.EqualTo(filter), "FilterType value not as expected");
            Assert.That(seq.Gain, Is.EqualTo(-1), "Gain value not as expected");
            Assert.That(seq.ImageType, Is.EqualTo(imageType), "ImageType value not as expected");
            Assert.That(seq.ProgressExposureCount, Is.EqualTo(0), "ProgressExposureCount value not as expected");
            Assert.That(seq.TotalExposureCount, Is.EqualTo(exposureCount), "TotalExposureCount value not as expected");
            Assert.That(seq.Enabled, Is.EqualTo(true), "Enabled value not as expected");
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
            Assert.That(seq.ProgressExposureCount, Is.EqualTo(exposuresTaken), "ProgressExposureCount value not as expected");
            Assert.That(seq.TotalExposureCount, Is.EqualTo(exposureCount), "TotalExposureCount value not as expected");
        }
    }
}