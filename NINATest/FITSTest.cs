#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Model.ImageData;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.FileFormat.FITS;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NINATest {

    [TestFixture]
    internal class FITSTest {

        [Test]
        public void FITSConstructorTest() {
            //Arrange
            var width = 2;
            var height = 2;
            ushort[] data = new ushort[width * height];
            for (var i = 0; i < width; i++) {
                for (var j = 0; j < height; j++) {
                    data[i + j] = 1;
                }
            }

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("SIMPLE", true, "C# FITS"),
                new FITSHeaderCard("BITPIX", 16, ""),
                new FITSHeaderCard("NAXIS", 2, "Dimensionality"),
                new FITSHeaderCard("NAXIS1", width, ""),
                new FITSHeaderCard("NAXIS2", height, ""),
                new FITSHeaderCard("BZERO", 32768, ""),
                new FITSHeaderCard("EXTEND", true, "Extensions are permitted")
            };

            //Act
            var sut = new FITS(data, width, height);

            //Assert
            sut.Data.Data.Should().BeEquivalentTo(data);
            sut.Header.HeaderCards.Count.Should().Be(expectedHeaderCards.Count);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSDefaultMetaDataPopulated() {
            //Arrange
            var metaData = new ImageMetaData();

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("XBINNING",1, "X axis binning factor"),
                new FITSHeaderCard("YBINNING",1, "Y axis binning factor"),
                new FITSHeaderCard("SWCREATE",string.Format("N.I.N.A. {0} ({1})", Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file"),
            };

            //Act
            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            //Assert
            sut.Header.HeaderCards.Count.Should().Be(expectedHeaderCards.Count + 7); // 7 is the default header size
            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSImageMetaDataPopulated() {
            //Arrange
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.Image.ImageType = "TEST";
            metaData.Image.ExposureStart = now;
            metaData.Image.ExposureTime = 10.23;

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("IMAGETYP", metaData.Image.ImageType, "Type of exposure"),
                new FITSHeaderCard("EXPOSURE", metaData.Image.ExposureTime, "[s] Exposure duration"),
                new FITSHeaderCard("EXPTIME", metaData.Image.ExposureTime, "[s] Exposure duration"),
                new FITSHeaderCard("DATE-LOC", metaData.Image.ExposureStart.ToLocalTime(), "Time of observation (local)"),
                new FITSHeaderCard("DATE-OBS", metaData.Image.ExposureStart.ToUniversalTime(), "Time of observation (UTC)"),
            };

            //Act
            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            //Assert
            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSImageMetaDataSNAPPopulated() {
            //Arrange
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.Image.ImageType = "SNAPSHOT";
            metaData.Image.ExposureStart = now;
            metaData.Image.ExposureTime = 10.23;

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("IMAGETYP", "LIGHT", "Type of exposure")
            };

            //Act
            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            //Assert
            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSCameraMetaDataPopulated() {
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.Camera.Name = "TEST";
            metaData.Camera.BinX = 2;
            metaData.Camera.BinY = 3;
            metaData.Camera.Gain = 200;
            metaData.Camera.Offset = 22;
            metaData.Camera.ElectronsPerADU = 11;
            metaData.Camera.PixelSize = 12;
            metaData.Camera.SetPoint = -5;
            metaData.Camera.Temperature = -4.454;
            metaData.Camera.ReadoutModeName = "1 Hz";

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("INSTRUME", metaData.Camera.Name, "Imaging instrument name"),
                new FITSHeaderCard("XBINNING", metaData.Camera.BinX, "X axis binning factor"),
                new FITSHeaderCard("YBINNING", metaData.Camera.BinY, "Y axis binning factor"),
                new FITSHeaderCard("GAIN", metaData.Camera.Gain, "Sensor gain"),
                new FITSHeaderCard("OFFSET", metaData.Camera.Offset, "Sensor gain offset"),
                new FITSHeaderCard("EGAIN", metaData.Camera.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit"),
                new FITSHeaderCard("XPIXSZ", metaData.Camera.PixelSize * metaData.Camera.BinX, "[um] Pixel X axis size"),
                new FITSHeaderCard("YPIXSZ", metaData.Camera.PixelSize * metaData.Camera.BinY, "[um] Pixel Y axis size"),
                new FITSHeaderCard("SET-TEMP", metaData.Camera.SetPoint, "[degC] CCD temperature setpoint"),
                new FITSHeaderCard("CCD-TEMP", metaData.Camera.Temperature, "[degC] CCD temperature"),
                new FITSHeaderCard("READOUTM", metaData.Camera.ReadoutModeName, "Sensor readout mode")
            };

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSTelescopeMetaDataPopulated() {
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.Telescope.Name = "TEST";
            metaData.Telescope.FocalLength = 200;
            metaData.Telescope.FocalRatio = 5;
            metaData.Telescope.Coordinates = new NINA.Utility.Astrometry.Coordinates(Angle.ByHours(2.125), Angle.ByDegree(10.154), Epoch.J2000);

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("TELESCOP", metaData.Telescope.Name, "Name of telescope"),
                new FITSHeaderCard("FOCALLEN", metaData.Telescope.FocalLength, "[mm] Focal length"),
                new FITSHeaderCard("FOCRATIO", metaData.Telescope.FocalRatio, "Focal ratio"),
                new FITSHeaderCard("RA", metaData.Telescope.Coordinates.RADegrees, "[deg] RA of telescope"),
                new FITSHeaderCard("DEC", metaData.Telescope.Coordinates.Dec, "[deg] Declination of telescope")
            };

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSObserverMetaDataPopulated() {
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.Observer.Latitude = 10;
            metaData.Observer.Longitude = 20;
            metaData.Observer.Elevation = 30;

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("SITEELEV", metaData.Observer.Elevation, "[m] Observation site elevation"),
                new FITSHeaderCard("SITELAT", metaData.Observer.Latitude, "[deg] Observation site latitude"),
                new FITSHeaderCard("SITELONG", metaData.Observer.Longitude, "[deg] Observation site longitude")
            };

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSFilterMetaDataPopulated() {
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.FilterWheel.Name = "TEST";
            metaData.FilterWheel.Filter = "FILTERTEST";

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("FWHEEL", metaData.FilterWheel.Name, "Filter Wheel name"),
                new FITSHeaderCard("FILTER", metaData.FilterWheel.Filter, "Active filter name")
            };

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSTargetMetaDataPopulated() {
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.Target.Name = "TEST";
            metaData.Target.Coordinates = new NINA.Utility.Astrometry.Coordinates(Angle.ByHours(2.125), Angle.ByDegree(10.154), Epoch.J2000);

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("OBJECT", metaData.Target.Name, "Name of the object of interest"),
                new FITSHeaderCard("OBJCTRA", Astrometry.HoursToFitsHMS(metaData.Target.Coordinates.RA), "[H M S] RA of imaged object"),
                new FITSHeaderCard("OBJCTDEC", Astrometry.DegreesToFitsDMS(metaData.Target.Coordinates.Dec), "[D M S] Declination of imaged object"),
            };

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSFocuserMetaDataPopulated() {
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.Focuser.Name = "TEST";
            metaData.Focuser.Position = 123.11;
            metaData.Focuser.StepSize = 10.23;
            metaData.Focuser.Temperature = 125.12;

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("FOCNAME", metaData.Focuser.Name, "Focusing equipment name"),
                new FITSHeaderCard("FOCPOS", metaData.Focuser.Position, "[step] Focuser position"),
                new FITSHeaderCard("FOCUSPOS", metaData.Focuser.Position, "[step] Focuser position"),
                new FITSHeaderCard("FOCUSSZ", metaData.Focuser.StepSize, "[um] Focuser step size"),
                new FITSHeaderCard("FOCTEMP", metaData.Focuser.Temperature, "[degC] Focuser temperature"),
                new FITSHeaderCard("FOCUSTEM", metaData.Focuser.Temperature, "[degC] Focuser temperature"),
            };

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSRotatorMetaDataPopulated() {
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.Rotator.Name = "TEST";
            metaData.Rotator.Position = 123.11;
            metaData.Rotator.StepSize = 10.23;

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("ROTNAME", metaData.Rotator.Name, "Rotator equipment name"),
                new FITSHeaderCard("ROTATOR", metaData.Rotator.Position, "[deg] Rotator angle"),
                new FITSHeaderCard("ROTATANG", metaData.Rotator.Position, "[deg] Rotator angle"),
                new FITSHeaderCard("ROTSTPSZ", metaData.Rotator.StepSize, "[deg] Rotator step size"),
            };

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSWeatherDataMetaDataPopulated() {
            var now = DateTime.Now;
            var metaData = new ImageMetaData();
            metaData.WeatherData.CloudCover = 99.11;
            metaData.WeatherData.DewPoint = 18.91;
            metaData.WeatherData.Humidity = 46.52;
            metaData.WeatherData.Pressure = 1010.4;
            metaData.WeatherData.SkyBrightness = 43;
            metaData.WeatherData.SkyQuality = 17.84;
            metaData.WeatherData.SkyTemperature = -42;
            metaData.WeatherData.StarFWHM = 2.34;
            metaData.WeatherData.Temperature = 17.2;
            metaData.WeatherData.WindDirection = 284.23;
            metaData.WeatherData.WindGust = 1.76;
            metaData.WeatherData.WindSpeed = 0.54;

            var expectedHeaderCards = new List<FITSHeaderCard>() {
                new FITSHeaderCard("CLOUDCVR", metaData.WeatherData.CloudCover, "[percent] Cloud cover"),
                new FITSHeaderCard("DEWPOINT", metaData.WeatherData.DewPoint, "[degC] Dew point"),
                new FITSHeaderCard("HUMIDITY", metaData.WeatherData.Humidity, "[percent] Relative humidity"),
                new FITSHeaderCard("PRESSURE", metaData.WeatherData.Pressure, "[hPa] Air pressure"),
                new FITSHeaderCard("SKYBRGHT", metaData.WeatherData.SkyBrightness, "[lux] Sky brightness"),
                new FITSHeaderCard("MPSAS", metaData.WeatherData.SkyQuality, "[mags/arcsec^2] Sky quality"),
                new FITSHeaderCard("SKYTEMP", metaData.WeatherData.SkyTemperature, "[degC] Sky temperature"),
                new FITSHeaderCard("STARFWHM", metaData.WeatherData.StarFWHM, "Star FWHM"),
                new FITSHeaderCard("AMBTEMP", metaData.WeatherData.Temperature, "[degC] Ambient air temperature"),
                new FITSHeaderCard("WINDDIR", metaData.WeatherData.WindDirection, "[deg] Wind direction: 0=N, 180=S, 90=E, 270=W"),
                new FITSHeaderCard("WINDGUST", metaData.WeatherData.WindGust * 3.6, "[kph] Wind gust"),
                new FITSHeaderCard("WINDSPD", metaData.WeatherData.WindSpeed * 3.6, "[kph] Wind speed"),
            };

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            foreach (var expectedCard in expectedHeaderCards) {
                sut.Header.HeaderCards.First(x => x.Key == expectedCard.Key).Should().BeEquivalentTo(expectedCard);
            }
        }

        [Test]
        public void FITSWriteTest() {
            //Arragne
            var width = 9;
            var height = 3;
            ushort[] data = new ushort[] {
                1,2,3,4,5,6,7,8,9,
                9,8,7,6,5,4,3,2,1,
                5,6,7,8,9,1,2,3,4
            };

            byte[] expectedByteData = new byte[] {
                /* Header Cards */
                83,73,77,80,76,69,32,32,61,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,84,32,47,32,67,35,32,70,73,84,83,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                66,73,84,80,73,88,32,32,61,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,49,54,32,47,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                78,65,88,73,83,32,32,32,61,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,50,32,47,32,68,105,109,101,110,115,105,111,110,97,108,105,116,121,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,78,65,88,73,83,49,32,32,61,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,57,32,47,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,78,65,88,73,83,50,32,32,61,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,51,32,47,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,66,90,69,82,79,32,32,32,61,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,51,50,55,54,56,32,47,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,69,88,84,69,78,68,32,32,61,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,84,32,47,32,69,120,116,101,110,115,105,111,110,115,32,97,114,101,32,112,101,114,109,105,116,116,101,100,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,

                /* END */
                69,78,68,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,

                /* Header block padding (Fill remaining bytes for block of size 2880) */
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
                32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,

                /* Data */
                128,1,128,2,128,3,128,4,128,5,128,6,128,7,128,8,128,9,128,9,128,8,128,7,128,6,128,5,128,4,128,3,128,2,128,1,128,5,128,6,128,7,128,8,128,9,128,1,128,2,128,3,128,4,

                /* Block Padding (Fill remaining bytes for block of size 2880)*/
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
            };

            //Act
            var sut = new FITS(data, width, height);
            byte[] byteData;
            using (var s = new MemoryStream()) {
                sut.Write(s);
                byteData = s.ToArray();
            }

            //Assert
            byteData.Should().BeEquivalentTo(expectedByteData);
        }

        [Test]
        public void FITSHeaderCard_NullTest() {
            var key = "SOME";
            string value = null;
            string comment = null;

            var sut = new FITSHeaderCard(key, value, comment);

            sut.Key.Should().Be(key);
            sut.Value.Should().Be("''                  ");
            sut.Comment.Should().Be(string.Empty);
        }

        [Test]
        public void FITSHeaderCardStringTest() {
            var key = "SOME";
            var value = "someone's value";
            var comment = "some comment";

            var sut = new FITSHeaderCard(key, value, comment);

            var expectedValue = "'someone''s value'  ";

            sut.Key.Should().Be(key);
            sut.Value.Should().Be(expectedValue);
            sut.Comment.Should().Be(comment);
        }

        [Test]
        public void FITSHeaderCardLongStringTest() {
            var key = "SOME";
            var value = "QXuUfwRN6t5OumSP9fFoWki4vUIvBXwFVYIDKROyCscAZ9ealUZdQKFzukzaNR6byrZdVUfKCHUwFfzex1iYNFBf1uVobcEX1e5m";
            var comment = "QXuUfwRN6t5OumSP9fFoWki4vUIvBXwFVYIDKROyCscAZ9ealUZdQKFzukzaNR6byrZdVUfKCHUwFfzex1iYNFBf1uVobcEX1e5m";

            var sut = new FITSHeaderCard(key, value, comment);

            var expectedValue = "'QXuUfwRN6t5OumSP9f'";
            var expectedComment = "QXuUfwRN6t5OumSP9fFoWki4vUIvBXwFVYIDKROyCscAZ";

            sut.Key.Should().Be(key);
            sut.Value.Should().Be(expectedValue);
            sut.Comment.Should().Be(expectedComment);
        }

        [Test]
        public void FITSHeaderCardBoolTest() {
            var key = "SOME";
            var value = true;
            var comment = "some comment";

            var sut = new FITSHeaderCard(key, value, comment);
            var sut2 = new FITSHeaderCard(key, !value, comment);

            var expectedValue = "T";

            sut.Key.Should().Be(key);
            sut.Value.Should().Be(expectedValue);
            sut.Comment.Should().Be(comment);

            var expectedValue2 = "F";

            sut2.Key.Should().Be(key);
            sut2.Value.Should().Be(expectedValue2);
            sut2.Comment.Should().Be(comment);
        }

        [Test]
        public void FITSHeaderCardDoubleTest() {
            var key = "SOME";
            var value = 12.45678965432129497;
            var comment = "some comment";

            var sut = new FITSHeaderCard(key, value, comment);

            var expectedValue = "12.4567896543213";

            sut.Key.Should().Be(key);
            sut.Value.Should().Be(expectedValue);
            sut.Comment.Should().Be(comment);
        }

        [Test]
        public void FITSHeaderCardDateTest() {
            var key = "SOME";
            var value = new DateTime(2012, 1, 10, 1, 20, 12, 111);
            var comment = "some comment";

            var sut = new FITSHeaderCard(key, value, comment);

            var expectedValue = "'2012-01-10T01:20:12.111'";

            sut.Key.Should().Be(key);
            sut.Value.Should().Be(expectedValue);
            sut.Comment.Should().Be(comment);
        }

        [Test]
        public void FITSHeaderCardIntTest() {
            var key = "SOME";
            var value = 113;
            var comment = "some comment";

            var sut = new FITSHeaderCard(key, value, comment);

            sut.Key.Should().Be(key);
            sut.Value.Should().Be(value.ToString());
            sut.Comment.Should().Be(comment);
        }

        [Test]
        public void FITSGainNegativeValueTest() {
            var metaData = new ImageMetaData();
            metaData.Camera.Gain = -1;

            var notExpectedCard = new FITSHeaderCard("GAIN", metaData.Camera.Gain, "Sensor gain");

            var sut = new FITS(new ushort[] { 1, 2 }, 1, 1);
            sut.PopulateHeaderCards(metaData);

            sut.Header.HeaderCards.Should().NotContain(notExpectedCard, "Negative Gain values are not allowed");
        }

        [Test]
        [TestCase("Some String")]
        [TestCase("Bode's Nebula")]
        [TestCase("' A bit Weird '")]
        [TestCase("")]
        public void FITSOriginalValue_StringTest(string value) {
            var card = new FITSHeaderCard("KEY", value, string.Empty);
            card.OriginalValue.Should().Be(value);
        }

        [Test]
        [TestCase(0)]
        [TestCase(100)]
        [TestCase(-100)]
        public void FITSOriginalValue_StringTest(int value) {
            var card = new FITSHeaderCard("KEY", value, string.Empty);
            card.OriginalValue.Should().Be(value.ToString());
        }

        [Test]
        [TestCase(0)]
        [TestCase(100)]
        [TestCase(-100)]
        [TestCase(-100)]
        [TestCase(200.1234)]
        [TestCase(-200.4321)]
        public void FITSOriginalValue_DoubleTest(double value) {
            var card = new FITSHeaderCard("KEY", value, string.Empty);
            card.OriginalValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}