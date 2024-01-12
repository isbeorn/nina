#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NUnit.Framework;
using FluentAssertions;
using System;
using System.IO;
using NINA.Core.Model;
using System.Reflection;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class CustomHorizonTest {

        [Test]        
        [TestCase("commas.hrz", 17)]
        [TestCase("full360.hrz", 361)]
        [TestCase("incomplete.hrz", 5)]
        [TestCase("mw4.hpts", 19)]
        [TestCase("partial.hrz", 17)]
        [TestCase("tabs.hrz", 17)]
        [TestCase("mixed.hrz", 17)]
        public void TestFiles_WorkCorrectly_And_Have_All_Entries_Parsed(string file, int expectedEntries) {
            var testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", file);
            var customHorizon = CustomHorizon.FromFilePath(testFile);

            customHorizon.Should().NotBeNull();
            ((double[])typeof(CustomHorizon).GetField("azimuths", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(customHorizon)).Length.Should().Be(expectedEntries);
            ((double[])typeof(CustomHorizon).GetField("altitudes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(customHorizon)).Length.Should().Be(expectedEntries);

        }

        [Test]
        public void CompleteAzimuth_MinMaxAltitudeRetrievedCorrectly() {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "full360.hrz");

            var sut = CustomHorizon.FromFilePath(data);

            sut.GetMaxAltitude().Should().Be(70.9);
            sut.GetMinAltitude().Should().Be(7.6);
        }

        [Test]
        [TestCase(0, 36.8)]
        [TestCase(359, 36.9)]
        [TestCase(-1, 36.9)]
        [TestCase(360, 36.8)]
        [TestCase(1111160, 33.7)]
        [TestCase(90, 8.4)]
        [TestCase(180, 43.5)]
        [TestCase(270, 29.0)]
        [TestCase(291, 43.3)]
        public void CompleteAzimuth_AbsoluteValuesRetrievedCorrectly(double azimuth, double expectedAltitude) {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "full360.hrz");

            var sut = CustomHorizon.FromFilePath(data);

            sut.GetAltitude(azimuth).Should().Be(expectedAltitude);
        }

        [Test]
        [TestCase(0, 14)]
        [TestCase(359, 14.6)]
        [TestCase(100, 46.6666666666)]
        [TestCase(200, 20.833333333333336)]
        public void PartialAzimuth_InterpolationRetrievedCorrectly(double azimuth, double expectedAltitude) {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "partial.hrz");

            var sut = CustomHorizon.FromFilePath(data);

            sut.GetAltitude(azimuth).Should().BeApproximately(expectedAltitude, 0.0000001);
        }

        [Test]
        [TestCase(0, 30)]
        [TestCase(359, 29.833333333333)]
        [TestCase(100, 25.124555160)]
        [TestCase(200, 16.22775800)]
        public void IncompleteAzimuth_InterpolationRetrievedCorrectly(double azimuth, double expectedAltitude) {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "incomplete.hrz");

            var sut = CustomHorizon.FromFilePath(data);

            sut.GetAltitude(azimuth).Should().BeApproximately(expectedAltitude, 0.0000001);
        }

        [Test]
        public void FileNotFound_FileNotFoundExceptionThrown() {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "invalid.abc");

            Action act = () => CustomHorizon.FromFilePath(data);

            act.Should().Throw<FileNotFoundException>();
        }

        [Test]
        public void FileNotFound_ArgumentExceptionThrown() {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "empty.hrz");

            Action act = () => CustomHorizon.FromFilePath(data);

            act.Should().Throw<ArgumentException>().WithMessage("Horizon file does not contain enough entries or is invalid");
        }

        [Test]
        [TestCase(0, 2.8767)]
        [TestCase(0.567, 2.0548)]
        [TestCase(111.685, 16.0274)]
        [TestCase(299.9055, 2.4658)]
        [TestCase(360.0, 2.8767)]
        public void MW4_Format_ParsedCorrectly(double azimuth, double expectedAltitude) {
            var data = Path.Combine(TestContext.CurrentContext.TestDirectory, "AstrometryTest", "HorizonData", "mw4.hpts");

            var sut = CustomHorizon.FromFilePath(data);
            sut.GetAltitude(azimuth).Should().BeApproximately(expectedAltitude, 0.001);
        }
    }
}