#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using FluentAssertions;
using Moq;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NINA.Test {

    [TestFixture]
    public class FilePatternTest {
        private ImageMetaData metaData;
        private ushort[] arr = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };
        private ushort width = 4;
        private ushort height = 5;
        private ImageDataFactoryTestUtility dataFactoryUtility;

        [SetUp]
        public void Setup() {
            dataFactoryUtility = new ImageDataFactoryTestUtility();
            metaData = new ImageMetaData();
        }

        [Test]
        public void Pattern_Remove_TrailingAndLeading_Whitespace_FromFilesAndFolders() {
            var date = new DateTime(2022,1,1,12,0,0,DateTimeKind.Utc);
            string filePattern = "$$DATE$$\\Telescope = $$TELESCOPE$$\\   Target = $$TARGETNAME$$ \\Type = $$IMAGETYPE$$\\ Filter = $$FILTER$$\\ $$DATE$$ @ $$TIME$$; Target = $$TARGETNAME$$; Type = $$IMAGETYPE$$; Filter = $$FILTER$$; Gain = $$GAIN$$; Bin = $$BINNING$$; Exp = $$EXPOSURETIME$$ s; Temp = $$SENSORTEMP$$ C; Frame # = $$FRAMENR$$ ";
            metaData.Target.Name = @"C/2020 F3 NEOWISE ?//_\\-A Comet";
            metaData.Image.ExposureStart = date;
            string expectedResult = $"{date.ToLocalTime():yyyy-MM-dd}\\Telescope =\\Target = C-2020 F3 NEOWISE _--_---A Comet\\Type =\\Filter =\\{date.ToLocalTime():yyyy-MM-dd} @ {date.ToLocalTime():HH-mm-ss}; Target = C-2020 F3 NEOWISE _--_---A Comet; Type = ; Filter = ; Gain = ; Bin = 1x1; Exp =  s; Temp =  C; Frame # = -0001";
                                    
            BaseImageData result = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(arr, width, height, 16, false, metaData);
            string parsedPattern = result.GetImagePatterns().GetImageFileString(filePattern);

            parsedPattern.Should().Be(expectedResult);
        }

        [Test]
        public void StringFilePattern() {
            //Arrange
            string filePattern = "$$TARGETNAME$$";
            metaData.Target.Name = @"C/2020 F3 NEOWISE ?//_\\-A Comet";
            string expectedResult = "C-2020 F3 NEOWISE _--_---A Comet";

            //Act
            BaseImageData result = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(arr, width, height, 16, false, metaData);
            string parsedPattern = result.GetImagePatterns().GetImageFileString(filePattern);

            //Assert
            ClassicAssert.AreEqual(expectedResult, parsedPattern);
        }

        [Test]
        public void DoubleFilePattern() {
            //Arrange
            string filePattern = "$$SQM$$";
            metaData.WeatherData.SkyQuality = 20;
            string expectedResult = "20.00";

            //Act
            BaseImageData result = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(arr, width, height, 16, false, metaData);
            string parsedPattern = result.GetImagePatterns().GetImageFileString(filePattern);

            //Assert
            ClassicAssert.AreEqual(expectedResult, parsedPattern);
        }

        [Test]
        public void IntegerFilePattern() {
            //Arrange
            string filePattern = "$$GAIN$$";
            metaData.Camera.Gain = 139;
            string expectedResult = "139";

            //Act
            BaseImageData result = dataFactoryUtility.ImageDataFactory.CreateBaseImageData(arr, width, height, 16, false, metaData);
            string parsedPattern = result.GetImagePatterns().GetImageFileString(filePattern);

            //Assert
            ClassicAssert.AreEqual(expectedResult, parsedPattern);
        }
    }
}