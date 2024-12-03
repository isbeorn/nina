#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Astrometry;
using Accord.Statistics.Distributions.Univariate;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Core.Model;
using System.Collections.Generic;
using NINA.Equipment.Equipment.MyGuider.PHD2;

namespace NINA.Equipment.Equipment.MyGuider {

    public class DirectGuider : BaseINPC, IGuider, ITelescopeConsumer {
        private readonly IProfileService profileService;
        private readonly ITelescopeMediator telescopeMediator;

        public DirectGuider(IProfileService profileService, ITelescopeMediator telescopeMediator) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
        }

        public string Name => "Mount Dither";
        public string DisplayName => Name;

        public string Id => "Direct_Guider";

        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();

        public void UpdateDeviceInfo(TelescopeInfo telescopeInfo) {
            this.telescopeInfo = telescopeInfo;
            if (Connected && !this.telescopeInfo.Connected) {
                Notification.ShowWarning(Loc.Instance["LblMountDitherMountDisconnect"]);
                Logger.Warning("Telescope is disconnected. Direct Guide will disconnect. Dither will not occur.");
                Disconnect();
            } else {
                // arcseconds per pixel
                PixelScale = AstroUtil.ArcsecPerPixel(profileService.ActiveProfile.CameraSettings.PixelSize, profileService.ActiveProfile.TelescopeSettings.FocalLength);
                WestEastGuideRate = ToNormalizedGuideRate(telescopeInfo.GuideRateRightAscensionArcsecPerSec);
                NorthSouthGuideRate = ToNormalizedGuideRate(telescopeInfo.GuideRateDeclinationArcsecPerSec);

                // arcseconds per second
                var guidingRateArcsecondsPerSecond = Math.Max(WestEastGuideRate, NorthSouthGuideRate);
                // pixels * (arcseconds per pixel) / (arcseconds per second) = seconds
                // This is purely an informational value to know how your settings translate into a typical dither duration
                DirectGuideDuration = profileService.ActiveProfile.GuiderSettings.DitherPixels * PixelScale / guidingRateArcsecondsPerSecond;
            }
        }

        private static double ToNormalizedGuideRate(double arcsecPerSecond) {
            if (double.IsNaN(arcsecPerSecond) || arcsecPerSecond <= 0) {
                // Default guiding rate is 0.5x sidereal
                return AstroUtil.SIDEREAL_RATE_ARCSECONDS_PER_SECOND / 2.0;
            }
            return arcsecPerSecond;
        }

        private bool guiding = false;

        private bool _connected;

        public bool Connected {
            get => _connected;
            set {
                if (_connected != value) {
                    _connected = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _pixelScale = -1.0;

        public double PixelScale {
            get => _pixelScale;
            set {
                if (_pixelScale != value) {
                    _pixelScale = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _directGuideDuration = 0.0;

        public double DirectGuideDuration {
            get => _directGuideDuration;
            set {
                if (_directGuideDuration != value) {
                    _directGuideDuration = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _westEastGuideRate = 0.0;

        public double WestEastGuideRate {
            get => _westEastGuideRate;
            set {
                if (_westEastGuideRate != value) {
                    _westEastGuideRate = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _northSouthGuideRate = 0.0;

        public double NorthSouthGuideRate {
            get => _northSouthGuideRate;
            set {
                if (_northSouthGuideRate != value) {
                    _northSouthGuideRate = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _state = "Idle";

        public string State {
            get => _state;
            set {
                _state = value;
                RaisePropertyChanged();
            }
        }

        public bool HasSetupDialog => false;

        public string Category => "Guiders";

        public string Description => "Mount Dither";

        public string DriverInfo => "Mount Dither";

        public string DriverVersion => "1.0";

        public Task<bool> Connect(CancellationToken token) {
            Connected = false;
            if (telescopeInfo.Connected) {
                Connected = true;
            } else {
                Notification.ShowWarning(Loc.Instance["LblMountDitherConnectionFail"]);
                Connected = false;
            }

            return Task.FromResult(Connected);
        }

        public Task<bool> AutoSelectGuideStar() {
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Connected = false;
        }

        public Task<bool> Pause(bool pause, CancellationToken ct) {
            if (!Connected || !this.guiding) {
                return Task.FromResult(false);
            }
            this.guiding = false;
            return Task.FromResult(true);
        }

        public Task<bool> StartGuiding(bool forceCalibration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (!Connected) {
                return Task.FromResult(false);
            }
            this.guiding = true;
            return Task.FromResult(true);
        }

        public Task<bool> StopGuiding(CancellationToken ct) {
            if (!Connected || !this.guiding) {
                return Task.FromResult(false);
            }
            this.guiding = false;
            return Task.FromResult(true);
        }

        public bool CanClearCalibration => true;

        public bool CanSetShiftRate => false;

        public bool CanGetLockPosition => false;

        public bool ShiftEnabled => false;
        public SiderealShiftTrackingRate ShiftRate => SiderealShiftTrackingRate.Disabled;

        public Task<bool> ClearCalibration(CancellationToken ct) {
            return Task.FromResult(true);
        }

        private readonly Random random = new Random();
        private double previousWestEastOffsetPixels = 0.0;
        private double previousNorthSouthOffsetPixels = 0.0;

        public event EventHandler<IGuideStep> GuideEvent { add { } remove { } }

        public async Task<bool> Dither(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            State = "Dithering...";

            var settleTime = TimeSpan.FromSeconds(profileService.ActiveProfile.GuiderSettings.SettleTime);
            var ditherRAOnly = profileService.ActiveProfile.GuiderSettings.DitherRAOnly;

            // Extra defense against telescope disconnection right before a dithering operation
            if (!telescopeInfo.Connected) {
                return false;
            } else {
                var pulseInstructions = SelectDitherPulse();

                // Note: According to the ASCOM specification, PulseGuide returns immediately (asynchronous) if the mount supports back to back axis moves, otherwise
                // it waits until completion. To be strictly correct here we'd start a counter here instead to avoid a potential extra wait. However, DirectGuiding is
                // primarily aimed at high end mounts which probably can do this anyways.
                telescopeMediator.PulseGuide(pulseInstructions.directionWestEast, (int)Math.Round(pulseInstructions.durationWestEast.TotalMilliseconds));
                var pulseGuideDelayMilliseconds = pulseInstructions.durationWestEast.TotalMilliseconds;
                if (!ditherRAOnly) {
                    telescopeMediator.PulseGuide(pulseInstructions.directionNorthSouth, (int)Math.Round(pulseInstructions.durationNorthSouth.TotalMilliseconds));
                    pulseGuideDelayMilliseconds = Math.Max(pulseGuideDelayMilliseconds, pulseInstructions.durationNorthSouth.TotalMilliseconds);
                }
                await CoreUtil.Delay(TimeSpan.FromMilliseconds(pulseGuideDelayMilliseconds), ct);

                State = "Dither settling...";
                await CoreUtil.Delay(settleTime, ct);
            }
            State = "Idle";
            return true;
        }

        private struct GuidePulses {
            public GuideDirections directionWestEast;
            public GuideDirections directionNorthSouth;
            public TimeSpan durationWestEast;
            public TimeSpan durationNorthSouth;
        }

        /// <summary>
        /// Determines what dither pulses to send in N/S and W/E directions so that deviations are normally distributed
        /// around the target, with standard deviation equal to the configured "DitherPixels", and distances clamped to +- 3 times that.
        /// This is accomplished by computing a vector from the previous randomly chosen offset to the target and sending a pulse guide
        /// accordingly. Durations are chosen by factoring in the mount-reported guiding rate (using 0.5x sidereal as a fallback) and the camera pixel scale,
        /// which also factors in telescope focal length
        /// </summary>
        /// <returns>Parameters for two guide pulses, one in N/S direction and one in E/W direction</returns>

        private GuidePulses SelectDitherPulse() {
            double ditherAngle = random.NextDouble() * Math.PI;
            double cosAngle = Math.Cos(ditherAngle);
            double sinAngle = Math.Sin(ditherAngle);
            var expectedDitherPixels = profileService.ActiveProfile.GuiderSettings.DitherPixels;

            // Generate a normally distributed distance from 0 with standard deviation equal to the configured "Dither Pixels", and clamped to +- 3 standard deviations
            double targetDistancePixels = NormalDistribution.Random(mean: 0.0, stdDev: expectedDitherPixels);
            targetDistancePixels = Math.Min(3.0d * expectedDitherPixels, Math.Max(-3.0d * expectedDitherPixels, targetDistancePixels));

            double targetWestEastOffsetPixels = targetDistancePixels * cosAngle;
            double targetNorthSouthOffsetPixels = targetDistancePixels * sinAngle;

            // RA axis is East/West
            // Dec axis is North/South
            // pixels * (arcseconds per pixel) / (arcseconds per second) = seconds
            double westEastDuration = (targetWestEastOffsetPixels - previousWestEastOffsetPixels) * PixelScale / WestEastGuideRate;
            double northSouthDuration = (targetNorthSouthOffsetPixels - previousNorthSouthOffsetPixels) * PixelScale / NorthSouthGuideRate;
            Logger.Info($"Dither target from ({previousWestEastOffsetPixels}, {previousNorthSouthOffsetPixels}) to ({targetWestEastOffsetPixels}, {targetNorthSouthOffsetPixels}) using guide durations of {westEastDuration} and {northSouthDuration} seconds");

            previousWestEastOffsetPixels = targetWestEastOffsetPixels;
            previousNorthSouthOffsetPixels = targetNorthSouthOffsetPixels;

            GuidePulses resultPulses = new GuidePulses();
            if (westEastDuration >= 0) {
                resultPulses.directionWestEast = GuideDirections.guideEast;
            } else {
                resultPulses.directionWestEast = GuideDirections.guideWest;
            }

            if (northSouthDuration >= 0) {
                resultPulses.directionNorthSouth = GuideDirections.guideNorth;
            } else {
                resultPulses.directionNorthSouth = GuideDirections.guideSouth;
            }

            resultPulses.durationWestEast = TimeSpan.FromSeconds(Math.Abs(westEastDuration));
            resultPulses.durationNorthSouth = TimeSpan.FromSeconds(Math.Abs(northSouthDuration));
            return resultPulses;
        }

        public void Dispose() {
            this.telescopeMediator.RemoveConsumer(this);
        }

        public void SetupDialog() {
        }

        public Task<bool> SetShiftRate(SiderealShiftTrackingRate shiftTrackingRate, CancellationToken ct) {
            return Task.FromResult(false);
        }

        public Task<bool> StopShifting(CancellationToken ct) {
            return Task.FromResult(true);
        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }

        public Task<LockPosition> GetLockPosition() {
            throw new NotImplementedException();
        }
    }
}