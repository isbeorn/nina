#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

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
using Grpc.Core;

namespace NINA.Test {

    [TestFixture]
    public class ImageMetaDataTest {

        [Test]
        public void DefaultValuesTest() {
            var sut = new ImageMetaData();

            Assert.That(sut.Image.ExposureStart, Is.EqualTo(DateTime.MinValue));
            Assert.That(sut.Image.ExposureNumber, Is.EqualTo(-1));
            Assert.That(sut.Image.ImageType, Is.Empty);
            Assert.That(sut.Image.Binning, Is.Empty);
            Assert.That(sut.Image.ExposureTime, Is.EqualTo(double.NaN));
            Assert.That(sut.Image.RecordedRMS, Is.EqualTo(null));

            Assert.That(sut.Camera.Name, Is.Empty);
            Assert.That(sut.Camera.Binning, Is.EqualTo("1x1"));
            Assert.That(sut.Camera.BinX, Is.EqualTo(1));
            Assert.That(sut.Camera.BinY, Is.EqualTo(1));
            Assert.That(sut.Camera.PixelSize, Is.EqualTo(double.NaN));
            Assert.That(sut.Camera.Temperature, Is.EqualTo(double.NaN));
            Assert.That(sut.Camera.Gain, Is.EqualTo(-1));
            Assert.That(sut.Camera.Offset, Is.EqualTo(-1));
            Assert.That(sut.Camera.ElectronsPerADU, Is.EqualTo(double.NaN));
            Assert.That(sut.Camera.SetPoint, Is.EqualTo(double.NaN));

            Assert.That(sut.Telescope.Name, Is.Empty);
            Assert.That(sut.Telescope.FocalLength, Is.EqualTo(double.NaN));
            Assert.That(sut.Telescope.FocalRatio, Is.EqualTo(double.NaN));
            Assert.That(sut.Telescope.Coordinates, Is.EqualTo(null));

            Assert.That(sut.Focuser.Name, Is.Empty);
            Assert.That(sut.Focuser.Position, Is.EqualTo(null));
            Assert.That(sut.Focuser.StepSize, Is.EqualTo(double.NaN));
            Assert.That(sut.Focuser.Temperature, Is.EqualTo(double.NaN));

            Assert.That(sut.Rotator.Name, Is.Empty);
            Assert.That(sut.Rotator.Position, Is.EqualTo(double.NaN));
            Assert.That(sut.Rotator.StepSize, Is.EqualTo(double.NaN));

            Assert.That(sut.FilterWheel.Name, Is.Empty);
            Assert.That(sut.FilterWheel.Filter, Is.Empty);

            Assert.That(sut.Target.Name, Is.Empty);
            Assert.That(sut.Target.Coordinates, Is.EqualTo(null));

            Assert.That(sut.Observer.Latitude, Is.EqualTo(double.NaN));
            Assert.That(sut.Observer.Longitude, Is.EqualTo(double.NaN));
            Assert.That(sut.Observer.Elevation, Is.EqualTo(double.NaN));

            Assert.That(sut.WeatherData.CloudCover, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.DewPoint, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.Humidity, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.Pressure, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.SkyBrightness, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.SkyQuality, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.SkyTemperature, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.StarFWHM, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.Temperature, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.WindDirection, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.WindGust, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.WindSpeed, Is.EqualTo(double.NaN));
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
                    Elevation = 102.3,
                    Observer = "Jon Doe",
                    Observatory = "Jon Observatory 1",
                    Site = "Jon Town"

                }
            };

            var sut = new ImageMetaData();
            sut.FromProfile(profile);

            Assert.That(sut.Camera.PixelSize, Is.EqualTo(3.8));
            Assert.That(sut.Telescope.Name, Is.EqualTo("TestName"));
            Assert.That(sut.Telescope.FocalLength, Is.EqualTo(100));
            Assert.That(sut.Telescope.FocalRatio, Is.EqualTo(5));
            Assert.That(sut.Observer.Latitude, Is.EqualTo(10));
            Assert.That(sut.Observer.Longitude, Is.EqualTo(20));
            Assert.That(sut.Observer.Elevation, Is.EqualTo(102.3));
            Assert.That(sut.Observer.Name, Is.EqualTo("Jon Doe"));
            Assert.That(sut.Observer.Observatory, Is.EqualTo("Jon Observatory 1"));
            Assert.That(sut.Observer.Site, Is.EqualTo("Jon Town"));
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

            Assert.That(sut.Camera.Name, Is.Empty);
            Assert.That(sut.Camera.Binning, Is.EqualTo("1x1"));
            Assert.That(sut.Camera.BinX, Is.EqualTo(1));
            Assert.That(sut.Camera.BinY, Is.EqualTo(1));
            Assert.That(sut.Camera.PixelSize, Is.EqualTo(double.NaN));
            Assert.That(sut.Camera.Temperature, Is.EqualTo(double.NaN));
            Assert.That(sut.Camera.Gain, Is.EqualTo(-1));
            Assert.That(sut.Camera.Offset, Is.EqualTo(-1));
            Assert.That(sut.Camera.ElectronsPerADU, Is.EqualTo(double.NaN));
            Assert.That(sut.Camera.SetPoint, Is.EqualTo(double.NaN));
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

            Assert.That(sut.Camera.Name, Is.EqualTo("TEST"));
            Assert.That(sut.Camera.Binning, Is.EqualTo("3x2"));
            Assert.That(sut.Camera.BinX, Is.EqualTo(3));
            Assert.That(sut.Camera.BinY, Is.EqualTo(2));
            Assert.That(sut.Camera.PixelSize, Is.EqualTo(12));
            Assert.That(sut.Camera.Temperature, Is.EqualTo(20.5));
            Assert.That(sut.Camera.Gain, Is.EqualTo(139));
            Assert.That(sut.Camera.Offset, Is.EqualTo(10));
            Assert.That(sut.Camera.ElectronsPerADU, Is.EqualTo(2.43));
            Assert.That(sut.Camera.SetPoint, Is.EqualTo(-10));
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

            Assert.That(sut.Telescope.Name, Is.Empty);
            Assert.That(sut.Telescope.FocalLength, Is.EqualTo(double.NaN));
            Assert.That(sut.Telescope.FocalRatio, Is.EqualTo(double.NaN));
            Assert.That(sut.Telescope.Coordinates, Is.EqualTo(null));
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

            Assert.That(sut.Telescope.Name, Is.EqualTo("TestName"));
            Assert.That(sut.Telescope.FocalLength, Is.EqualTo(double.NaN));
            Assert.That(sut.Telescope.FocalRatio, Is.EqualTo(double.NaN));

            Assert.That(sut.Telescope.Coordinates.Epoch, Is.EqualTo(Epoch.J2000));
            Assert.That(sut.Telescope.Coordinates.RADegrees, Is.EqualTo(59.694545007067305d));
            Assert.That(sut.Telescope.Coordinates.Dec, Is.EqualTo(28.945185789004316d));
            Assert.That(sut.Telescope.SideOfPier, Is.EqualTo(PierSide.pierWest));
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

            Assert.That(sut.Telescope.Coordinates.Epoch, Is.EqualTo(Epoch.J2000));
            Assert.That(sut.Telescope.Coordinates.RADegrees, Is.EqualTo(60));
            Assert.That(sut.Telescope.Coordinates.Dec, Is.EqualTo(29));
        }

        [Test]
        public void TargetCoordinateTransformTest() {
            var coordinates = new Coordinates(Angle.ByHours(4), Angle.ByDegree(29), Epoch.JNOW, DateTime.ParseExact("20200615T22:00:00Z", "yyyyMMddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));

            var sut = new ImageMetaData() {
                Target = new TargetParameter() {
                    Coordinates = coordinates
                }
            };

            Assert.That(sut.Target.Coordinates.Epoch, Is.EqualTo(Epoch.J2000));
            Assert.That(sut.Target.Coordinates.RADegrees, Is.EqualTo(59.694545007067305d));
            Assert.That(sut.Target.Coordinates.Dec, Is.EqualTo(28.945185789004316d));
        }

        [Test]
        public void TargetCoordinateNoTransformTest() {
            var coordinates = new Coordinates(Angle.ByHours(4), Angle.ByDegree(29), Epoch.J2000);

            var sut = new ImageMetaData() {
                Target = new TargetParameter() {
                    Coordinates = coordinates
                }
            };

            Assert.That(sut.Target.Coordinates.Epoch, Is.EqualTo(Epoch.J2000));
            Assert.That(sut.Target.Coordinates.RADegrees, Is.EqualTo(60));
            Assert.That(sut.Target.Coordinates.Dec, Is.EqualTo(29));
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

            Assert.That(sut.FilterWheel.Name, Is.Empty);
            Assert.That(sut.FilterWheel.Filter, Is.Empty);
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

            Assert.That(sut.FilterWheel.Name, Is.EqualTo("TestFilterWheel"));
            Assert.That(sut.FilterWheel.Filter, Is.EqualTo("Red"));
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

            Assert.That(sut.Focuser.Name, Is.Empty);
            Assert.That(sut.Focuser.Position, Is.EqualTo(null));
            Assert.That(sut.Focuser.StepSize, Is.EqualTo(double.NaN));
            Assert.That(sut.Focuser.Temperature, Is.EqualTo(double.NaN));
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

            Assert.That(sut.Focuser.Name, Is.EqualTo("TestFocuser"));
            Assert.That(sut.Focuser.Position, Is.EqualTo(123));
            Assert.That(sut.Focuser.StepSize, Is.EqualTo(3.8));
            Assert.That(sut.Focuser.Temperature, Is.EqualTo(100));
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

            Assert.That(sut.Rotator.Name, Is.Empty);
            Assert.That(sut.Rotator.Position, Is.EqualTo(double.NaN));
            Assert.That(sut.Rotator.StepSize, Is.EqualTo(double.NaN));
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

            Assert.That(sut.Rotator.Name, Is.EqualTo("TestRotator"));
            Assert.That(sut.Rotator.Position, Is.EqualTo(123));
            Assert.That(sut.Rotator.StepSize, Is.EqualTo((double)3.8f));
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

            Assert.That(sut.WeatherData.Temperature, Is.EqualTo(double.NaN));
            Assert.That(sut.WeatherData.Humidity, Is.EqualTo(double.NaN));
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

            Assert.That(sut.WeatherData.Temperature, Is.EqualTo(15));
            Assert.That(sut.WeatherData.Humidity, Is.EqualTo((double)99.8f));
        }
    }
}