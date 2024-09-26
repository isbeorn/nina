#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Image.ImageData;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Profile.Interfaces;
using NINA.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using NINA.Equipment.Utility;
using NINA.Core.Model.Equipment;
using NINA.Profile;
using NINA.Core.Enum;
using Moq;
using NINA.Equipment.Interfaces;
using FluentAssertions;
using NUnit.Framework.Legacy;

namespace NINA.Test {

    [TestFixture]
    public class ImageMetaDataTest {

        [Test]
        public void DefaultValuesTest() {
            var sut = new ImageMetaData();

            ClassicAssert.AreEqual(DateTime.MinValue, sut.Image.ExposureStart);
            ClassicAssert.AreEqual(-1, sut.Image.ExposureNumber);
            ClassicAssert.AreEqual(string.Empty, sut.Image.ImageType);
            ClassicAssert.AreEqual(string.Empty, sut.Image.Binning);
            ClassicAssert.AreEqual(double.NaN, sut.Image.ExposureTime);
            ClassicAssert.AreEqual(null, sut.Image.RecordedRMS);

            ClassicAssert.AreEqual(string.Empty, sut.Camera.Name);
            ClassicAssert.AreEqual("1x1", sut.Camera.Binning);
            ClassicAssert.AreEqual(1, sut.Camera.BinX);
            ClassicAssert.AreEqual(1, sut.Camera.BinY);
            ClassicAssert.AreEqual(double.NaN, sut.Camera.PixelSize);
            ClassicAssert.AreEqual(double.NaN, sut.Camera.Temperature);
            ClassicAssert.AreEqual(-1, sut.Camera.Gain);
            ClassicAssert.AreEqual(-1, sut.Camera.Offset);
            ClassicAssert.AreEqual(double.NaN, sut.Camera.ElectronsPerADU);
            ClassicAssert.AreEqual(double.NaN, sut.Camera.SetPoint);

            ClassicAssert.AreEqual(string.Empty, sut.Telescope.Name);
            ClassicAssert.AreEqual(double.NaN, sut.Telescope.FocalLength);
            ClassicAssert.AreEqual(double.NaN, sut.Telescope.FocalRatio);
            ClassicAssert.AreEqual(null, sut.Telescope.Coordinates);

            ClassicAssert.AreEqual(string.Empty, sut.Focuser.Name);
            ClassicAssert.AreEqual(null, sut.Focuser.Position);
            ClassicAssert.AreEqual(double.NaN, sut.Focuser.StepSize);
            ClassicAssert.AreEqual(double.NaN, sut.Focuser.Temperature);

            ClassicAssert.AreEqual(string.Empty, sut.Rotator.Name);
            ClassicAssert.AreEqual(double.NaN, sut.Rotator.Position);
            ClassicAssert.AreEqual(double.NaN, sut.Rotator.StepSize);

            ClassicAssert.AreEqual(string.Empty, sut.FilterWheel.Name);
            ClassicAssert.AreEqual(string.Empty, sut.FilterWheel.Filter);

            ClassicAssert.AreEqual(string.Empty, sut.Target.Name);
            ClassicAssert.AreEqual(null, sut.Target.Coordinates);

            ClassicAssert.AreEqual(double.NaN, sut.Observer.Latitude);
            ClassicAssert.AreEqual(double.NaN, sut.Observer.Longitude);
            ClassicAssert.AreEqual(double.NaN, sut.Observer.Elevation);

            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.CloudCover);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.DewPoint);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.Humidity);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.Pressure);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.SkyBrightness);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.SkyQuality);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.SkyTemperature);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.StarFWHM);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.Temperature);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.WindDirection);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.WindGust);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.WindSpeed);
        }

        [Test]
        public void FromProfileTest() {
            var profile = new NINA.Profile.Profile() {
                CameraSettings = {
                    PixelSize = 3.8
                },

                TelescopeSettings = {
                    Name = "TestName",
                    FocalLength = 100,
                    FocalRatio = 5
                },

                AstrometrySettings = {
                    Latitude = 10,
                    Longitude = 20,
                    Elevation = 102.3
                }
            };

            var sut = new ImageMetaData();
            sut.FromProfile(profile);

            ClassicAssert.AreEqual(3.8, sut.Camera.PixelSize);
            ClassicAssert.AreEqual("TestName", sut.Telescope.Name);
            ClassicAssert.AreEqual(100, sut.Telescope.FocalLength);
            ClassicAssert.AreEqual(5, sut.Telescope.FocalRatio);
            ClassicAssert.AreEqual(10, sut.Observer.Latitude);
            ClassicAssert.AreEqual(20, sut.Observer.Longitude);
            ClassicAssert.AreEqual(102.3, sut.Observer.Elevation);
        }

        [Test]
        public void FromCameraNotConnectedTest() {
            var camera = new Mock<ICamera>();
            camera.SetupGet(x => x.Connected).Returns(false);
            camera.SetupGet(x => x.Name).Returns("TEST");
            camera.SetupGet(x => x.Temperature).Returns(20.5);
            camera.SetupGet(x => x.Gain).Returns(139);
            camera.SetupGet(x => x.Offset).Returns(10);
            camera.SetupGet(x => x.TemperatureSetPoint).Returns(-10);
            camera.SetupGet(x => x.BinX).Returns(3);
            camera.SetupGet(x => x.BinY).Returns(2);
            camera.SetupGet(x => x.ElectronsPerADU).Returns(2.43);
            camera.SetupGet(x => x.PixelSizeX).Returns(12);
            camera.SetupGet(x => x.ReadoutMode).Returns(1);
            camera.SetupGet(x => x.ReadoutModes).Returns(new List<string> { "mode1", "mode2" });

            var sut = new ImageMetaData();
            sut.FromCamera(camera.Object);

            sut.Camera.Should()
                .BeEquivalentTo(new {
                    Name = string.Empty,
                    Binning = "1x1",
                    BinX = 1,
                    BinY = 1,
                    PixelSize = double.NaN,
                    Temperature = double.NaN,
                    Gain = -1,
                    Offset = -1,
                    ElectronsPerADU = double.NaN,
                    SetPoint = double.NaN
                });
        }

        [Test]
        public void FromCameraConnectedTest() {
            var camera = new Mock<ICamera>();
            camera.SetupGet(x => x.Connected).Returns(true);
            camera.SetupGet(x => x.Name).Returns("TEST");
            camera.SetupGet(x => x.Temperature).Returns(20.5);
            camera.SetupGet(x => x.CanGetGain).Returns(true);
            camera.SetupGet(x => x.Gain).Returns(139);
            camera.SetupGet(x => x.Offset).Returns(10);
            camera.SetupGet(x => x.TemperatureSetPoint).Returns(-10);
            camera.SetupGet(x => x.BinX).Returns(3);
            camera.SetupGet(x => x.BinY).Returns(2);
            camera.SetupGet(x => x.ElectronsPerADU).Returns(2.43);
            camera.SetupGet(x => x.PixelSizeX).Returns(12);
            camera.SetupGet(x => x.ReadoutMode).Returns(1);
            camera.SetupGet(x => x.ReadoutModes).Returns(new List<string> { "mode1", "mode2" });

            var sut = new ImageMetaData();
            sut.FromCamera(camera.Object);

            sut.Camera.Should()
                .BeEquivalentTo(new {
                    Name = "TEST",
                    Binning = "3x2",
                    BinX = 3,
                    BinY = 2,
                    PixelSize = 12,
                    Temperature = 20.5,
                    Gain = 139,
                    Offset = 10,
                    ElectronsPerADU = 2.43,
                    SetPoint = -10,
                    ReadoutModeName = "mode2"
                });
        }

        [Test]
        public void FromCameraInfoNotConnectedTest() {
            var cameraInfo = new CameraInfo() {
                Connected = false,
                Temperature = 20.5,
                Gain = 139,
                Offset = 10,
                TemperatureSetPoint = -10,
                BinX = 3,
                BinY = 2,
                ElectronsPerADU = 2.43,
                PixelSize = 12
            };

            var sut = new ImageMetaData();
            sut.FromCameraInfo(cameraInfo);

            ClassicAssert.AreEqual(string.Empty, sut.Camera.Name);
            ClassicAssert.AreEqual("1x1", sut.Camera.Binning);
            ClassicAssert.AreEqual(1, sut.Camera.BinX);
            ClassicAssert.AreEqual(1, sut.Camera.BinY);
            ClassicAssert.AreEqual(double.NaN, sut.Camera.PixelSize);
            ClassicAssert.AreEqual(double.NaN, sut.Camera.Temperature);
            ClassicAssert.AreEqual(-1, sut.Camera.Gain);
            ClassicAssert.AreEqual(-1, sut.Camera.Offset);
            ClassicAssert.AreEqual(double.NaN, sut.Camera.ElectronsPerADU);
            ClassicAssert.AreEqual(double.NaN, sut.Camera.SetPoint);
        }

        [Test]
        public void FromCameraInfoConnectedTest() {
            var cameraInfo = new CameraInfo() {
                Connected = true,
                Name = "TEST",
                Temperature = 20.5,
                Gain = 139,
                Offset = 10,
                TemperatureSetPoint = -10,
                BinX = 3,
                BinY = 2,
                ElectronsPerADU = 2.43,
                PixelSize = 12,
                ReadoutMode = 1,
                ReadoutModes = new List<string> { "mode1", "mode2" }
            };

            var sut = new ImageMetaData();
            sut.FromCameraInfo(cameraInfo);

            ClassicAssert.AreEqual("TEST", sut.Camera.Name);
            ClassicAssert.AreEqual("3x2", sut.Camera.Binning);
            ClassicAssert.AreEqual(3, sut.Camera.BinX);
            ClassicAssert.AreEqual(2, sut.Camera.BinY);
            ClassicAssert.AreEqual(12, sut.Camera.PixelSize);
            ClassicAssert.AreEqual(20.5, sut.Camera.Temperature);
            ClassicAssert.AreEqual(139, sut.Camera.Gain);
            ClassicAssert.AreEqual(10, sut.Camera.Offset);
            ClassicAssert.AreEqual(2.43, sut.Camera.ElectronsPerADU);
            ClassicAssert.AreEqual(-10, sut.Camera.SetPoint);
            Assert.That(sut.Camera.ReadoutModeName, Is.EqualTo("mode2"));
        }

        [Test]
        public void FromTelescopeInfoNotConnectedTest() {
            var coordinates = new Coordinates(Angle.ByHours(4), Angle.ByDegree(29), Epoch.J2000);
            var telescopeInfo = new TelescopeInfo() {
                Connected = false,
                Name = "TestName",
                SiteElevation = 120.3,
                Coordinates = coordinates
            };
            var sut = new ImageMetaData();
            sut.FromTelescopeInfo(telescopeInfo);

            ClassicAssert.AreEqual(string.Empty, sut.Telescope.Name);
            ClassicAssert.AreEqual(double.NaN, sut.Telescope.FocalLength);
            ClassicAssert.AreEqual(double.NaN, sut.Telescope.FocalRatio);
            ClassicAssert.AreEqual(null, sut.Telescope.Coordinates);
        }

        [Test]
        public void FromTelescopeInfoConnectedTest() {
            var coordinates = new Coordinates(Angle.ByHours(4), Angle.ByDegree(29), Epoch.JNOW, DateTime.ParseExact("20200615T22:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            var telescopeInfo = new TelescopeInfo() {
                Connected = true,
                Name = "TestName",
                SiteElevation = 120.3,
                Coordinates = coordinates,
                SideOfPier = PierSide.pierWest
            };
            var sut = new ImageMetaData();
            sut.FromTelescopeInfo(telescopeInfo);

            ClassicAssert.AreEqual("TestName", sut.Telescope.Name);
            ClassicAssert.AreEqual(double.NaN, sut.Telescope.FocalLength);
            ClassicAssert.AreEqual(double.NaN, sut.Telescope.FocalRatio);

            ClassicAssert.AreEqual(Epoch.J2000, sut.Telescope.Coordinates.Epoch);
            ClassicAssert.AreEqual(59.694545025696307d, sut.Telescope.Coordinates.RADegrees);
            ClassicAssert.AreEqual(28.945185789035015d, sut.Telescope.Coordinates.Dec);
            ClassicAssert.AreEqual(PierSide.pierWest, sut.Telescope.SideOfPier);
        }

        [Test]
        public void FromTelescopeInfoNoCoordinateTransformTest() {
            var coordinates = new Coordinates(Angle.ByHours(4), Angle.ByDegree(29), Epoch.J2000);
            var telescopeInfo = new TelescopeInfo() {
                Connected = true,
                Coordinates = coordinates
            };
            var sut = new ImageMetaData();
            sut.FromTelescopeInfo(telescopeInfo);

            ClassicAssert.AreEqual(Epoch.J2000, sut.Telescope.Coordinates.Epoch);
            ClassicAssert.AreEqual(60, sut.Telescope.Coordinates.RADegrees);
            ClassicAssert.AreEqual(29, sut.Telescope.Coordinates.Dec);
        }

        [Test]
        public void TargetCoordinateTransformTest() {
            var coordinates = new Coordinates(Angle.ByHours(4), Angle.ByDegree(29), Epoch.JNOW, DateTime.ParseExact("20200615T22:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));

            var sut = new ImageMetaData() {
                Target = new TargetParameter() {
                    Coordinates = coordinates
                }
            };

            ClassicAssert.AreEqual(Epoch.J2000, sut.Target.Coordinates.Epoch);
            ClassicAssert.AreEqual(59.694545025696307d, sut.Target.Coordinates.RADegrees);
            ClassicAssert.AreEqual(28.945185789035015d, sut.Target.Coordinates.Dec);
        }

        [Test]
        public void TargetCoordinateNoTransformTest() {
            var coordinates = new Coordinates(Angle.ByHours(4), Angle.ByDegree(29), Epoch.J2000);

            var sut = new ImageMetaData() {
                Target = new TargetParameter() {
                    Coordinates = coordinates
                }
            };

            ClassicAssert.AreEqual(Epoch.J2000, sut.Target.Coordinates.Epoch);
            ClassicAssert.AreEqual(60, sut.Target.Coordinates.RADegrees);
            ClassicAssert.AreEqual(29, sut.Target.Coordinates.Dec);
        }

        [Test]
        public void FromFilterWheelInfoNotConnectedTest() {
            var info = new FilterWheelInfo() {
                Connected = false,
                Name = "TestFilterWheel",
                SelectedFilter = new FilterInfo { Name = "Red" }
            };

            var sut = new ImageMetaData();
            sut.FromFilterWheelInfo(info);

            ClassicAssert.AreEqual(string.Empty, sut.FilterWheel.Name);
            ClassicAssert.AreEqual(string.Empty, sut.FilterWheel.Filter);
        }

        [Test]
        public void FromFilterWheelInfoConnectedTest() {
            var info = new FilterWheelInfo() {
                Connected = true,
                Name = "TestFilterWheel",
                SelectedFilter = new FilterInfo { Name = "Red" }
            };

            var sut = new ImageMetaData();
            sut.FromFilterWheelInfo(info);

            ClassicAssert.AreEqual("TestFilterWheel", sut.FilterWheel.Name);
            ClassicAssert.AreEqual("Red", sut.FilterWheel.Filter);
        }

        [Test]
        public void FromFocuserInfoNotConnectedTest() {
            var info = new FocuserInfo() {
                Connected = false,
                Name = "TestFocuser",
                Position = 123,
                StepSize = 3.8,
                Temperature = 100
            };

            var sut = new ImageMetaData();
            sut.FromFocuserInfo(info);

            ClassicAssert.AreEqual(string.Empty, sut.Focuser.Name);
            ClassicAssert.AreEqual(null, sut.Focuser.Position);
            ClassicAssert.AreEqual(double.NaN, sut.Focuser.StepSize);
            ClassicAssert.AreEqual(double.NaN, sut.Focuser.Temperature);
        }

        [Test]
        public void FromFocuserInfoConnectedTest() {
            var info = new FocuserInfo() {
                Connected = true,
                Name = "TestFocuser",
                Position = 123,
                StepSize = 3.8,
                Temperature = 100
            };

            var sut = new ImageMetaData();
            sut.FromFocuserInfo(info);

            ClassicAssert.AreEqual("TestFocuser", sut.Focuser.Name);
            ClassicAssert.AreEqual(123, sut.Focuser.Position);
            ClassicAssert.AreEqual(3.8, sut.Focuser.StepSize);
            ClassicAssert.AreEqual(100, sut.Focuser.Temperature);
        }

        [Test]
        public void FromRotatorInfoNotConnectedTest() {
            var info = new RotatorInfo() {
                Connected = false,
                Name = "TestRotator",
                Position = 123,
                StepSize = 3.8f
            };

            var sut = new ImageMetaData();
            sut.FromRotatorInfo(info);

            ClassicAssert.AreEqual(string.Empty, sut.Rotator.Name);
            ClassicAssert.AreEqual(double.NaN, sut.Rotator.Position);
            ClassicAssert.AreEqual(double.NaN, sut.Rotator.StepSize);
        }

        [Test]
        public void FromRotatorInfoConnectedTest() {
            var info = new RotatorInfo() {
                Connected = true,
                Name = "TestRotator",
                Position = 123,
                StepSize = 3.8f
            };

            var sut = new ImageMetaData();
            sut.FromRotatorInfo(info);

            ClassicAssert.AreEqual("TestRotator", sut.Rotator.Name);
            ClassicAssert.AreEqual(123, sut.Rotator.Position);
            ClassicAssert.AreEqual((double)3.8f, sut.Rotator.StepSize);
        }

        [Test]
        public void FromWeatherDataInfoNotConnectedTest() {
            var info = new WeatherDataInfo() {
                Connected = false,
                Temperature = 15,
                Humidity = 99.8f
            };

            var sut = new ImageMetaData();
            sut.FromWeatherDataInfo(info);

            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.Temperature);
            ClassicAssert.AreEqual(double.NaN, sut.WeatherData.Humidity);
        }

        [Test]
        public void FromWeatherDataInfoConnectedTest() {
            var info = new WeatherDataInfo() {
                Connected = true,
                Temperature = 15,
                Humidity = 99.8f
            };

            var sut = new ImageMetaData();
            sut.FromWeatherDataInfo(info);

            ClassicAssert.AreEqual(15, sut.WeatherData.Temperature);
            ClassicAssert.AreEqual((double)99.8f, sut.WeatherData.Humidity);
        }
    }
}