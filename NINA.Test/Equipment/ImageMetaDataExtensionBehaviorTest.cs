#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Utility;
using NINA.Image.ImageData;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class ImageMetaDataExtensionBehaviorTest {

        /// <summary>
        /// Verifies connected camera metadata is copied defensively, including fallbacks when optional driver properties throw.
        /// </summary>
        [Test]
        public void FromCamera_CopiesConnectedCameraAndFallsBackForThrowingOptionalProperties() {
            var camera = new Mock<ICamera>();
            camera.SetupGet(x => x.Connected).Returns(true);
            camera.SetupGet(x => x.Id).Returns("cam-1");
            camera.SetupGet(x => x.Name).Returns("Camera");
            camera.SetupGet(x => x.Temperature).Throws<InvalidOperationException>();
            camera.SetupGet(x => x.CanGetGain).Returns(true);
            camera.SetupGet(x => x.Gain).Throws<InvalidOperationException>();
            camera.SetupGet(x => x.Offset).Throws<InvalidOperationException>();
            camera.SetupGet(x => x.TemperatureSetPoint).Throws<InvalidOperationException>();
            camera.SetupGet(x => x.BinX).Returns(2);
            camera.SetupGet(x => x.BinY).Returns(3);
            camera.SetupGet(x => x.ElectronsPerADU).Returns(0.42);
            camera.SetupGet(x => x.PixelSizeX).Returns(3.76);
            camera.SetupGet(x => x.USBLimit).Returns(70);
            camera.SetupGet(x => x.ReadoutModes).Returns(new List<string> { "Fast", "Low noise" });
            camera.SetupGet(x => x.ReadoutMode).Returns(1);
            camera.SetupGet(x => x.SensorType).Returns(SensorType.RGGB);
            camera.SetupGet(x => x.BayerOffsetX).Returns(1);
            camera.SetupGet(x => x.BayerOffsetY).Returns(2);
            var metadata = new ImageMetaData();

            metadata.FromCamera(camera.Object);

            metadata.Camera.Id.Should().Be("cam-1");
            metadata.Camera.Name.Should().Be("Camera");
            metadata.Camera.Temperature.Should().Be(double.NaN);
            metadata.Camera.Gain.Should().Be(-1);
            metadata.Camera.Offset.Should().Be(-1);
            metadata.Camera.SetPoint.Should().Be(double.NaN);
            metadata.Camera.BinX.Should().Be(2);
            metadata.Camera.BinY.Should().Be(3);
            metadata.Camera.ElectronsPerADU.Should().BeApproximately(0.42, 1e-10);
            metadata.Camera.PixelSize.Should().BeApproximately(3.76, 1e-10);
            metadata.Camera.USBLimit.Should().Be(70);
            metadata.Camera.ReadoutModeIndex.Should().Be(1);
            metadata.Camera.ReadoutModeName.Should().Be("Low noise");
            metadata.Camera.SensorType.Should().Be(SensorType.RGGB);
            metadata.Camera.BayerOffsetX.Should().Be(1);
            metadata.Camera.BayerOffsetY.Should().Be(2);
        }

        /// <summary>
        /// Verifies configured Bayer pattern overrides camera-reported OSC layout and resets offsets to the configured-pattern convention.
        /// </summary>
        [Test]
        public void FromCameraInfo_ConfiguredBayerPatternOverridesDriverSensorType() {
            var metadata = new ImageMetaData();
            metadata.Camera.BayerPattern = BayerPatternEnum.BGGR;
            var info = new CameraInfo {
                Connected = true,
                DeviceId = "cam-2",
                Name = "Info Camera",
                SensorType = SensorType.RGGB,
                BayerOffsetX = 1,
                BayerOffsetY = 1,
                ReadoutModes = new[] { "Only" }
            };

            metadata.FromCameraInfo(info);

            metadata.Camera.SensorType.Should().Be(SensorType.BGGR);
            metadata.Camera.BayerOffsetX.Should().Be(0);
            metadata.Camera.BayerOffsetY.Should().Be(0);
        }

        /// <summary>
        /// Verifies disconnected equipment snapshots leave existing image metadata unchanged.
        /// </summary>
        [Test]
        public void FromInfo_DisconnectedEquipmentDoesNotOverwriteExistingMetadata() {
            var metadata = new ImageMetaData();
            metadata.Telescope.Name = "Original Telescope";
            metadata.FilterWheel.Name = "Original Wheel";
            metadata.Focuser.Name = "Original Focuser";
            metadata.Rotator.Name = "Original Rotator";
            metadata.WeatherData.Temperature = 12.3;

            metadata.FromTelescopeInfo(new TelescopeInfo { Connected = false, Name = "New Telescope" });
            metadata.FromFilterWheelInfo(new FilterWheelInfo { Connected = false, Name = "New Wheel", SelectedFilter = new FilterInfo("L", 0, 0) });
            metadata.FromFocuserInfo(new FocuserInfo { Connected = false, Name = "New Focuser", Position = 123 });
            metadata.FromRotatorInfo(new RotatorInfo { Connected = false, Name = "New Rotator", Position = 45 });
            metadata.FromWeatherDataInfo(new WeatherDataInfo { Connected = false, Temperature = -5 });

            metadata.Telescope.Name.Should().Be("Original Telescope");
            metadata.FilterWheel.Name.Should().Be("Original Wheel");
            metadata.FilterWheel.Filter.Should().BeEmpty();
            metadata.Focuser.Name.Should().Be("Original Focuser");
            metadata.Focuser.Position.Should().BeNull();
            metadata.Rotator.Name.Should().Be("Original Rotator");
            metadata.Rotator.Position.Should().Be(double.NaN);
            metadata.WeatherData.Temperature.Should().BeApproximately(12.3, 1e-10);
        }

        /// <summary>
        /// Verifies connected telescope, filter wheel, focuser, rotator, and weather data snapshots populate the expected FITS metadata fields.
        /// </summary>
        [Test]
        public void FromInfo_ConnectedEquipmentPopulatesScientificMetadata() {
            var metadata = new ImageMetaData();
            Coordinates coordinates = new Coordinates(Angle.ByDegree(187.5), Angle.ByDegree(-22.25), Epoch.J2000);

            metadata.FromTelescopeInfo(new TelescopeInfo {
                Connected = true,
                Name = "Scope",
                Coordinates = coordinates,
                Altitude = 45,
                Azimuth = 180,
                SideOfPier = PierSide.pierWest
            });
            metadata.FromFilterWheelInfo(new FilterWheelInfo {
                Connected = true,
                Name = "Wheel",
                SelectedFilter = new FilterInfo("Ha", 0, 3)
            });
            metadata.FromFocuserInfo(new FocuserInfo {
                Connected = true,
                Name = "Focuser",
                Position = 12345,
                StepSize = 2.5,
                Temperature = -3.2
            });
            metadata.FromRotatorInfo(new RotatorInfo {
                Connected = true,
                Name = "Rotator",
                MechanicalPosition = 12.5f,
                Position = 10.1f,
                StepSize = 0.5f
            });
            metadata.FromWeatherDataInfo(new WeatherDataInfo {
                Connected = true,
                CloudCover = 10,
                DewPoint = -5,
                Humidity = 70,
                Pressure = 1012.5,
                SkyBrightness = 1.2,
                SkyQuality = 20.8,
                SkyTemperature = -15,
                StarFWHM = 2.4,
                Temperature = 1.5,
                WindDirection = 270,
                WindGust = 8,
                WindSpeed = 4
            });

            metadata.Telescope.Coordinates.RADegrees.Should().BeApproximately(187.5, 1e-10);
            metadata.Telescope.Altitude.Should().BeApproximately(45, 1e-10);
            metadata.Telescope.Azimuth.Should().BeApproximately(180, 1e-10);
            metadata.Telescope.Airmass.Should().BeGreaterThan(1);
            metadata.Telescope.SideOfPier.Should().Be(PierSide.pierWest);
            metadata.FilterWheel.Filter.Should().Be("Ha");
            metadata.Focuser.Position.Should().Be(12345);
            metadata.Focuser.StepSize.Should().BeApproximately(2.5, 1e-10);
            metadata.Rotator.MechanicalPosition.Should().BeApproximately(12.5, 1e-10);
            metadata.Rotator.Position.Should().BeApproximately(10.1, 1e-6);
            metadata.WeatherData.CloudCover.Should().Be(10);
            metadata.WeatherData.WindSpeed.Should().Be(4);
        }
    }
}
