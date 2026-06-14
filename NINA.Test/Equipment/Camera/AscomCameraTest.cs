#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Common.DeviceInterfaces;
using FluentAssertions;
using Moq;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Image.Interfaces;
using NINA.Image.ImageData;
using NINA.Core.Model;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using System.Threading;

namespace NINA.Test.Equipment.Camera {

    [TestFixture]
    public class AscomCameraTest {

        [Test]
        public async Task Connect_WhenAsymmetricBinningIsWithinLimit_ProvidesEveryCombination() {
            var (camera, _) = CreateCamera(2, 3, true);

            var connected = await camera.Connect(CancellationToken.None);

            connected.Should().BeTrue();
            camera.BinningModes.Should().Equal(
                new BinningMode(1, 1),
                new BinningMode(1, 2),
                new BinningMode(1, 3),
                new BinningMode(2, 1),
                new BinningMode(2, 2),
                new BinningMode(2, 3));
        }

        [Test]
        public async Task Connect_WhenAsymmetricBinningExceedsLimit_ClampsSelectableModes() {
            var (camera, _) = CreateCamera(17, 17, true);

            var connected = await camera.Connect(CancellationToken.None);

            connected.Should().BeTrue();
            camera.BinningModes.Should().HaveCount(256);
            camera.BinningModes.Should().Contain(new BinningMode(16, 16));
            camera.BinningModes.Should().NotContain(new BinningMode(17, 1));
            camera.BinningModes.Should().NotContain(new BinningMode(1, 17));
        }

        [TestCase(16, 16, 16, 16)]
        [TestCase(17, 2, 16, 2)]
        [TestCase(2, 17, 2, 16)]
        public async Task Connect_WhenAsymmetricBinningReachesLimit_ClampsAxesIndependently(
            short maxBinX,
            short maxBinY,
            short expectedMaxBinX,
            short expectedMaxBinY) {
            var (camera, _) = CreateCamera(maxBinX, maxBinY, true);

            var connected = await camera.Connect(CancellationToken.None);

            connected.Should().BeTrue();
            camera.BinningModes.Should().HaveCount(expectedMaxBinX * expectedMaxBinY);
            camera.BinningModes.Should().Contain(new BinningMode(expectedMaxBinX, expectedMaxBinY));
            camera.BinningModes.Should().OnlyContain(mode => mode.X <= expectedMaxBinX && mode.Y <= expectedMaxBinY);
        }

        [TestCase(0, 2)]
        [TestCase(2, 0)]
        [TestCase(-1, 2)]
        [TestCase(2, -1)]
        public async Task Connect_WhenAsymmetricBinningLimitIsNotPositive_FallsBackToOne(short maxBinX, short maxBinY) {
            var (camera, _) = CreateCamera(maxBinX, maxBinY, true);

            var connected = await camera.Connect(CancellationToken.None);

            connected.Should().BeTrue();
            camera.BinningModes.Should().HaveCount(2);
            camera.BinningModes.Should().Contain(new BinningMode(1, 1));
            camera.BinningModes.Should().Contain(new BinningMode(Math.Max(maxBinX, (short)1), Math.Max(maxBinY, (short)1)));
        }

        [TestCase(2048)]
        [TestCase(short.MaxValue)]
        public async Task Connect_WhenAsymmetricBinningLimitIsExtreme_BoundsModesAndPreservesReportedCapabilities(short reportedMaximum) {
            var (camera, driver) = CreateCamera(reportedMaximum, reportedMaximum, true);

            var connected = await camera.Connect(CancellationToken.None);

            connected.Should().BeTrue();
            camera.BinningModes.Should().HaveCount(256);
            camera.BinningModes.Should().Contain(new BinningMode(1, 1));
            camera.BinningModes.Should().Contain(new BinningMode(16, 16));
            driver.VerifyGet(x => x.MaxBinX, Times.Once);
            driver.VerifyGet(x => x.MaxBinY, Times.Once);
            driver.VerifyGet(x => x.CanAsymmetricBin, Times.Once);
            camera.MaxBinX.Should().Be(reportedMaximum);
            camera.MaxBinY.Should().Be(reportedMaximum);
        }

        [Test]
        public async Task Connect_WhenSymmetricBinningExceedsLimit_ProvidesBoundedSquareModes() {
            var (camera, _) = CreateCamera(17, 17, false);

            var connected = await camera.Connect(CancellationToken.None);

            connected.Should().BeTrue();
            camera.BinningModes.Should().HaveCount(16);
            camera.BinningModes.Should().ContainInOrder(Enumerable.Range(1, 16).Select(bin => new BinningMode((short)bin, (short)bin)));
            camera.BinningModes.Should().OnlyContain(mode => mode.X == mode.Y);
        }

        [TestCase("2026-09-10T01:02:03.125", 1.5)]
        [TestCase("2026-09-10T01:02:03.125Z", 0)]
        [TestCase("2026-09-10T03:02:03.125+02:00", 60)]
        public async Task DownloadExposure_UsesValidDriverTimingAsUtc(string timestamp, double duration) {
            var factory = new Mock<IExposureDataFactory>();
            ImageMetaData metadata = null;
            factory.Setup(x => x.CreateFlipped2DExposureData(It.IsAny<Array>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<ImageMetaData>()))
                .Callback<Array, int, bool, ImageMetaData>((_, _, _, value) => metadata = value);
            var (camera, driver) = CreateCamera(1, 1, false, factory);
            driver.SetupGet(x => x.LastExposureStartTime).Returns(timestamp);
            driver.SetupGet(x => x.LastExposureDuration).Returns(duration);
            driver.SetupGet(x => x.ImageReady).Returns(true);
            await camera.Connect(CancellationToken.None);
            camera.StartExposure(new CaptureSequence { ExposureTime = 2 });
            await camera.WaitUntilExposureIsReady(CancellationToken.None);
            await camera.DownloadExposure(CancellationToken.None);
            metadata.Should().NotBeNull();
            var expectedStart = new DateTime(2026, 9, 10, 1, 2, 3, 125, DateTimeKind.Utc);
            metadata.Image.ExposureStart.Should().Be(expectedStart);
            metadata.Image.ExposureStart.Kind.Should().Be(DateTimeKind.Utc);
            metadata.Image.ExposureMidPoint.Should().Be(expectedStart.AddSeconds(duration / 2));
            metadata.Image.ExposureTime.Should().Be(duration);
        }

        [TestCase("invalid", double.NaN)]
        [TestCase("", -1)]
        [TestCase(null, double.PositiveInfinity)]
        [TestCase("invalid", double.MaxValue)]
        public async Task DownloadExposure_InvalidDriverTiming_PreservesObservedTiming(string? timestamp, double duration) {
            var factory = new Mock<IExposureDataFactory>();
            ImageMetaData metadata = null;
            factory.Setup(x => x.CreateFlipped2DExposureData(It.IsAny<Array>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<ImageMetaData>()))
                .Callback<Array, int, bool, ImageMetaData>((_, _, _, value) => metadata = value);
            var (camera, driver) = CreateCamera(1, 1, false, factory);
            driver.SetupGet(x => x.LastExposureStartTime).Returns(timestamp);
            driver.SetupGet(x => x.LastExposureDuration).Returns(duration);
            driver.SetupGet(x => x.ImageReady).Returns(true);
            await camera.Connect(CancellationToken.None);
            var before = DateTime.UtcNow;
            camera.StartExposure(new CaptureSequence { ExposureTime = 2 });
            await camera.WaitUntilExposureIsReady(CancellationToken.None);
            var after = DateTime.UtcNow;
            await camera.DownloadExposure(CancellationToken.None);
            metadata.Should().NotBeNull();
            metadata.Image.ExposureStart.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
            metadata.Image.ExposureMidPoint.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
            double.IsNaN(metadata.Image.ExposureTime).Should().BeTrue();
        }

        [Test]
        public async Task OptionalTimingProperties_AreIndependentAndKeepLegacyTypes() {
            var (camera, driver) = CreateCamera(1, 1, false);
            driver.SetupGet(x => x.LastExposureDuration).Throws(new ASCOM.NotImplementedException());
            driver.SetupGet(x => x.LastExposureStartTime).Returns("2026-09-10T01:02:03");
            await camera.Connect(CancellationToken.None);
            double duration = camera.LastExposureDuration;
            string timestamp = camera.LastExposureStartTime;
            duration.Should().Be(-1);
            timestamp.Should().Be("2026-09-10T01:02:03");
        }

        private static (AscomCamera Camera, Mock<ICameraV4> Driver) CreateCamera(short maxBinX, short maxBinY, bool canAsymmetricBin, Mock<IExposureDataFactory> factory = null) {
            var driver = new Mock<ICameraV4>();
            driver.SetupProperty(x => x.Connected, false);
            driver.SetupGet(x => x.SensorType).Returns(SensorType.Monochrome);
            driver.SetupGet(x => x.MaxBinX).Returns(maxBinX);
            driver.SetupGet(x => x.MaxBinY).Returns(maxBinY);
            driver.SetupGet(x => x.CanAsymmetricBin).Returns(canAsymmetricBin);
            driver.SetupGet(x => x.Name).Returns("Test ASCOM Camera");

            driver.SetupProperty(x => x.BinX, (short)1);
            driver.SetupProperty(x => x.BinY, (short)1);
            driver.SetupGet(x => x.CameraXSize).Returns(2);
            driver.SetupGet(x => x.CameraYSize).Returns(2);
            driver.SetupGet(x => x.ImageArray).Returns(new int[2, 2]);
            driver.SetupGet(x => x.ReadoutModes).Returns(new List<string> { "Normal" });
            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile.CameraSettings.ASCOMAllowUnevenPixelDimension).Returns(false);
            var exposureDataFactory = factory ?? new Mock<IExposureDataFactory>();
            var camera = new TestAscomCamera(driver.Object, profileService.Object, exposureDataFactory.Object);
            return (camera, driver);
        }

        private sealed class TestAscomCamera : AscomCamera {
            private readonly ICameraV4 driver;

            public TestAscomCamera(ICameraV4 driver, IProfileService profileService, IExposureDataFactory exposureDataFactory)
                : base("Test.Camera", "Test ASCOM Camera", profileService, exposureDataFactory) {
                this.driver = driver;
            }

            protected override ICameraV4 GetInstance() {
                return driver;
            }
        }
    }
}