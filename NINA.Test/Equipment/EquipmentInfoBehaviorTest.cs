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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class EquipmentInfoBehaviorTest {

        /// <summary>
        /// Verifies camera info defaults use sentinel values and representative camera capabilities update state and notifications.
        /// </summary>
        [Test]
        public void CameraInfo_DefaultsAndCapabilityUpdatesAreObservable() {
            var sut = new CameraInfo();
            var changed = new List<string>();
            sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
            var binning = new AsyncObservableCollection<BinningMode> {
                new BinningMode(1, 1),
                new BinningMode(2, 2)
            };

            sut.Gain.Should().Be(-1);
            sut.DefaultGain.Should().Be(-1);
            sut.DefaultOffset.Should().Be(-1);
            sut.SensorType.Should().Be(SensorType.Monochrome);

            sut.CanSetTemperature = true;
            sut.Temperature = -12.5;
            sut.TemperatureSetPoint = -15;
            sut.Gain = 139;
            sut.Offset = 21;
            sut.BinX = 2;
            sut.BinY = 2;
            sut.BitDepth = 16;
            sut.SensorType = SensorType.RGGB;
            sut.BayerOffsetX = 1;
            sut.BayerOffsetY = 1;
            sut.BinningModes = binning;
            sut.ReadoutModes = new[] { "High Gain", "Low Noise" };
            sut.ReadoutMode = 1;
            sut.ReadoutModeForSnapImages = 0;
            sut.ReadoutModeForNormalImages = 1;
            sut.ExposureMin = 0.001;
            sut.ExposureMax = 1200;
            sut.CanShowLiveView = true;
            sut.LiveViewEnabled = true;

            sut.Temperature.Should().Be(-12.5);
            sut.Gain.Should().Be(139);
            sut.Offset.Should().Be(21);
            sut.BinningModes.Should().BeSameAs(binning);
            sut.ReadoutModes.Should().Equal("High Gain", "Low Noise");
            sut.ExposureMax.Should().Be(1200);
            changed.Should().Contain(new[] {
                nameof(CameraInfo.CanSetTemperature),
                nameof(CameraInfo.Temperature),
                nameof(CameraInfo.SensorType),
                nameof(CameraInfo.BinningModes),
                nameof(CameraInfo.LiveViewEnabled)
            });
        }

        /// <summary>
        /// Verifies telescope info preserves astrometric coordinates, mount limits, and tracking metadata supplied by a telescope driver.
        /// </summary>
        [Test]
        public void TelescopeInfo_AstrometryAndMountCapabilitiesRoundTrip() {
            var sut = new TelescopeInfo();
            var changed = new List<string>();
            sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
            var coordinates = new Coordinates(5.59175, -5.39111, Epoch.J2000, Coordinates.RAType.Hours);
            var trackingRate = new TrackingRate {
                TrackingMode = TrackingMode.Custom,
                CustomRightAscensionRate = 0.15,
                CustomDeclinationRate = -0.05
            };

            sut.Coordinates = coordinates;
            sut.RightAscension = coordinates.RA;
            sut.Declination = coordinates.Dec;
            sut.SiteLatitude = 48.137154;
            sut.SiteLongitude = 11.576124;
            sut.SiteElevation = 519;
            sut.Altitude = 42.5;
            sut.Azimuth = 183.25;
            sut.SideOfPier = PierSide.pierWest;
            sut.TrackingRate = trackingRate;
            sut.TrackingModes = new[] { TrackingMode.Sidereal, TrackingMode.Custom, TrackingMode.Stopped };
            sut.TargetCoordinates = new Coordinates(6.7525, -16.7161, Epoch.J2000, Coordinates.RAType.Hours);
            sut.TargetSideOfPier = PierSide.pierEast;
            sut.PrimaryAxisRates = new List<(double, double)> { (-2.0, 2.0) };
            sut.SecondaryAxisRates = new List<(double, double)> { (-1.0, 1.0) };
            sut.CanMovePrimaryAxis = true;
            sut.CanMoveSecondaryAxis = true;
            sut.CanPulseGuide = true;
            sut.UTCDate = new DateTime(2026, 04, 17, 01, 02, 03, DateTimeKind.Utc);

            sut.Coordinates.Should().BeSameAs(coordinates);
            sut.SiteElevation.Should().Be(519);
            sut.TrackingRate.Should().Be(trackingRate);
            sut.TrackingModes.Should().ContainInOrder(TrackingMode.Sidereal, TrackingMode.Custom, TrackingMode.Stopped);
            sut.TargetSideOfPier.Should().Be(PierSide.pierEast);
            sut.PrimaryAxisRates.Should().ContainSingle().Which.Should().Be((-2.0, 2.0));
            changed.Should().Contain(nameof(TelescopeInfo.TrackingRate));
            changed.Should().Contain("TrackingMode");
            changed.Should().Contain(nameof(TelescopeInfo.UTCDate));
        }

        /// <summary>
        /// Verifies dome following status, angular display strings, and dependent notifications stay consistent for driver and NINA following modes.
        /// </summary>
        [Test]
        public void DomeInfo_FollowingAndAngleDisplayUseDependentProperties() {
            var sut = new DomeInfo();
            var changed = new List<string>();
            sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            sut.FollowingType.Should().Be(Loc.Instance["LblOff"]);
            sut.AzimuthDMS.Should().BeEmpty();
            sut.AltitudeDMS.Should().BeEmpty();

            sut.ApplicationFollowing = true;
            sut.DriverFollowing = true;
            sut.Azimuth = 270.5;
            sut.Altitude = 33.25;
            sut.ShutterStatus = ShutterState.ShutterOpen;

            sut.FollowingType.Should().Be(Loc.Instance["LblDomeFollowingViaDriver"]);
            sut.AzimuthDMS.Should().Be(AstroUtil.DegreesToDMS(270.5));
            sut.AltitudeDMS.Should().Be(AstroUtil.DegreesToDMS(33.25));
            sut.ShutterStatus.Should().Be(ShutterState.ShutterOpen);
            changed.Should().Contain(nameof(DomeInfo.FollowingType));
            changed.Should().Contain(nameof(DomeInfo.AzimuthDMS));
            changed.Should().Contain(nameof(DomeInfo.AltitudeDMS));
        }

        /// <summary>
        /// Verifies flat-device state exposes localized dependent state for cover and light transitions.
        /// </summary>
        [Test]
        public void FlatDeviceInfo_LocalizedDependentStateTracksCoverAndLight() {
            var sut = new FlatDeviceInfo();
            var changed = new List<string>();
            sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            sut.CoverState = CoverState.Open;
            sut.LightOn = true;
            sut.MinBrightness = 5;
            sut.MaxBrightness = 255;
            sut.Brightness = 128;
            sut.SupportsOnOff = true;
            sut.SupportsOpenClose = true;

            sut.LocalizedCoverState.Should().Be(Loc.Instance["LblFlatDeviceOpen"]);
            sut.LocalizedLightOnState.Should().Be(Loc.Instance["LblOn"]);
            sut.Brightness.Should().Be(128);
            sut.MinBrightness.Should().Be(5);
            sut.MaxBrightness.Should().Be(255);
            changed.Should().Contain(nameof(FlatDeviceInfo.LocalizedCoverState));
            changed.Should().Contain(nameof(FlatDeviceInfo.LocalizedLightOnState));
        }

        /// <summary>
        /// Verifies simple equipment info models retain switch/filter/rotator/guider/weather/safety state used by equipment mediators.
        /// </summary>
        [Test]
        public void SimpleEquipmentInfoModels_RetainMediatorState() {
            var filter = new FilterInfo("Ha 7nm", 12, 2);
            var filterWheel = new FilterWheelInfo {
                IsMoving = true,
                SelectedFilter = filter,
                SupportedActions = new[] { "sync" }
            };
            var rotator = new RotatorInfo {
                CanReverse = true,
                Reverse = true,
                Position = 42.5f,
                MechanicalPosition = 402.5f,
                StepSize = 0.1f,
                IsMoving = true,
                Synced = true
            };
            var guider = new GuiderInfo {
                CanClearCalibration = true,
                CanSetShiftRate = true,
                CanGetLockPosition = true,
                PixelScale = 1.23,
                RMSError = new RMSError(1, 2, 3, 4, 5, 0.5)
            };
            var weather = new WeatherDataInfo {
                CloudCover = 12,
                DewPoint = -3.4,
                Humidity = 67,
                Pressure = 1013.25,
                RainRate = 0,
                SkyQuality = 21.4,
                Temperature = -1.2,
                WindDirection = 185,
                WindGust = 6.7,
                WindSpeed = 3.4,
                SupportedActions = new[] { "refresh" }
            };
            var safety = new SafetyMonitorInfo {
                IsSafe = true,
                SupportedActions = new[] { "status" }
            };
            var switches = new SwitchInfo {
                WritableSwitches = Array.AsReadOnly(Array.Empty<IWritableSwitch>()),
                ReadonlySwitches = Array.AsReadOnly(Array.Empty<ISwitch>()),
                SupportedActions = new[] { "toggle" }
            };

            filterWheel.SelectedFilter.Should().BeSameAs(filter);
            filterWheel.SupportedActions.Should().Equal("sync");
            rotator.MechanicalPosition.Should().Be(402.5f);
            rotator.Synced.Should().BeTrue();
            guider.RMSError.Total.Arcseconds.Should().BeApproximately(2.5, 1e-10);
            guider.PixelScale.Should().Be(1.23);
            weather.Pressure.Should().Be(1013.25);
            weather.SkyQuality.Should().Be(21.4);
            safety.IsSafe.Should().BeTrue();
            switches.WritableSwitches.Should().BeEmpty();
            switches.SupportedActions.Should().Equal("toggle");
        }
    }
}
