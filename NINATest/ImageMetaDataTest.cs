using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class ImageMetaDataTest {

        [Test]
        public void DefaultValuesTest() {
            var sut = new ImageMetaData();

            Assert.AreEqual(DateTime.MinValue, sut.Image.ExposureStart);
            Assert.AreEqual(-1, sut.Image.ExposureNumber);
            Assert.AreEqual(string.Empty, sut.Image.ImageType);
            Assert.AreEqual(string.Empty, sut.Image.Binning);
            Assert.AreEqual(double.NaN, sut.Image.ExposureTime);
            Assert.AreEqual(null, sut.Image.RecordedRMS);

            Assert.AreEqual(string.Empty, sut.Camera.Name);
            Assert.AreEqual("1x1", sut.Camera.Binning);
            Assert.AreEqual(1, sut.Camera.BinX);
            Assert.AreEqual(1, sut.Camera.BinY);
            Assert.AreEqual(double.NaN, sut.Camera.PixelSize);
            Assert.AreEqual(double.NaN, sut.Camera.Temperature);
            Assert.AreEqual(double.NaN, sut.Camera.Gain);
            Assert.AreEqual(double.NaN, sut.Camera.Offset);
            Assert.AreEqual(double.NaN, sut.Camera.ElectronsPerADU);
            Assert.AreEqual(double.NaN, sut.Camera.SetPoint);

            Assert.AreEqual(string.Empty, sut.Telescope.Name);
            Assert.AreEqual(double.NaN, sut.Telescope.FocalLength);
            Assert.AreEqual(double.NaN, sut.Telescope.FocalRatio);
            Assert.AreEqual(null, sut.Telescope.Coordinates);

            Assert.AreEqual(string.Empty, sut.Focuser.Name);
            Assert.AreEqual(double.NaN, sut.Focuser.Position);
            Assert.AreEqual(double.NaN, sut.Focuser.StepSize);
            Assert.AreEqual(double.NaN, sut.Focuser.Temperature);

            Assert.AreEqual(string.Empty, sut.Rotator.Name);
            Assert.AreEqual(double.NaN, sut.Rotator.Position);
            Assert.AreEqual(double.NaN, sut.Rotator.StepSize);

            Assert.AreEqual(string.Empty, sut.FilterWheel.Name);
            Assert.AreEqual(string.Empty, sut.FilterWheel.Filter);

            Assert.AreEqual(string.Empty, sut.Target.Name);
            Assert.AreEqual(null, sut.Target.Coordinates);

            Assert.AreEqual(double.NaN, sut.Observer.Latitude);
            Assert.AreEqual(double.NaN, sut.Observer.Longitude);
            Assert.AreEqual(double.NaN, sut.Observer.Elevation);
        }

        [Test]
        public void FromProfileTest() {
            var profile = new Profile() {
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
                    Longitude = 20
                }
            };

            var sut = new ImageMetaData();
            sut.FromProfile(profile);

            Assert.AreEqual(3.8, sut.Camera.PixelSize);
            Assert.AreEqual("TestName", sut.Telescope.Name);
            Assert.AreEqual(100, sut.Telescope.FocalLength);
            Assert.AreEqual(5, sut.Telescope.FocalRatio);
            Assert.AreEqual(10, sut.Observer.Latitude);
            Assert.AreEqual(20, sut.Observer.Longitude);
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

            Assert.AreEqual(string.Empty, sut.Camera.Name);
            Assert.AreEqual("1x1", sut.Camera.Binning);
            Assert.AreEqual(1, sut.Camera.BinX);
            Assert.AreEqual(1, sut.Camera.BinY);
            Assert.AreEqual(double.NaN, sut.Camera.PixelSize);
            Assert.AreEqual(double.NaN, sut.Camera.Temperature);
            Assert.AreEqual(double.NaN, sut.Camera.Gain);
            Assert.AreEqual(double.NaN, sut.Camera.Offset);
            Assert.AreEqual(double.NaN, sut.Camera.ElectronsPerADU);
            Assert.AreEqual(double.NaN, sut.Camera.SetPoint);
        }

        [Test]
        public void FromCameraInfoConnectedTest() {
            var cameraInfo = new CameraInfo() {
                Connected = true,
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

            Assert.AreEqual(string.Empty, sut.Camera.Name);
            Assert.AreEqual("3x2", sut.Camera.Binning);
            Assert.AreEqual(3, sut.Camera.BinX);
            Assert.AreEqual(2, sut.Camera.BinY);
            Assert.AreEqual(12, sut.Camera.PixelSize);
            Assert.AreEqual(20.5, sut.Camera.Temperature);
            Assert.AreEqual(139, sut.Camera.Gain);
            Assert.AreEqual(10, sut.Camera.Offset);
            Assert.AreEqual(2.43, sut.Camera.ElectronsPerADU);
            Assert.AreEqual(-10, sut.Camera.SetPoint);
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

            Assert.AreEqual(string.Empty, sut.Telescope.Name);
            Assert.AreEqual(double.NaN, sut.Telescope.FocalLength);
            Assert.AreEqual(double.NaN, sut.Telescope.FocalRatio);
            Assert.AreEqual(null, sut.Telescope.Coordinates);
        }

        [Test]
        public void FromTelescopeInfoConnectedTest() {
            var coordinates = new Coordinates(Angle.ByHours(4), Angle.ByDegree(29), Epoch.J2000);
            var telescopeInfo = new TelescopeInfo() {
                Connected = true,
                Name = "TestName",
                SiteElevation = 120.3,
                Coordinates = coordinates
            };
            var sut = new ImageMetaData();
            sut.FromTelescopeInfo(telescopeInfo);

            Assert.AreEqual("TestName", sut.Telescope.Name);
            Assert.AreEqual(120.3, sut.Observer.Elevation);
            Assert.AreEqual(double.NaN, sut.Telescope.FocalLength);
            Assert.AreEqual(double.NaN, sut.Telescope.FocalRatio);
            Assert.AreSame(coordinates, sut.Telescope.Coordinates);
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

            Assert.AreEqual(string.Empty, sut.FilterWheel.Name);
            Assert.AreEqual(string.Empty, sut.FilterWheel.Filter);
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

            Assert.AreEqual("TestFilterWheel", sut.FilterWheel.Name);
            Assert.AreEqual("Red", sut.FilterWheel.Filter);
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

            Assert.AreEqual(string.Empty, sut.Focuser.Name);
            Assert.AreEqual(double.NaN, sut.Focuser.Position);
            Assert.AreEqual(double.NaN, sut.Focuser.StepSize);
            Assert.AreEqual(double.NaN, sut.Focuser.Temperature);
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

            Assert.AreEqual("TestFocuser", sut.Focuser.Name);
            Assert.AreEqual(123, sut.Focuser.Position);
            Assert.AreEqual(3.8, sut.Focuser.StepSize);
            Assert.AreEqual(100, sut.Focuser.Temperature);
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

            Assert.AreEqual(string.Empty, sut.Rotator.Name);
            Assert.AreEqual(double.NaN, sut.Rotator.Position);
            Assert.AreEqual(double.NaN, sut.Rotator.StepSize);
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

            Assert.AreEqual("TestRotator", sut.Rotator.Name);
            Assert.AreEqual(123, sut.Rotator.Position);
            Assert.AreEqual((double)3.8f, sut.Rotator.StepSize);
        }
    }
}