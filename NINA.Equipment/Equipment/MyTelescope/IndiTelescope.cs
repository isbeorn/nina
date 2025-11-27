#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.INDI.Interfaces;
using NINA.INDI;
using NINA.Core.Enum;
using NINA.INDI.Devices;

namespace NINA.Equipment.Equipment.MyTelescope {

    internal class IndiTelescope : IndiDevice<IINDITelescope>, ITelescope, IDisposable {
        private static readonly TimeSpan MERIDIAN_FLIP_SLEW_RETRY_WAIT = TimeSpan.FromMinutes(1);
        private const int MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS = 20;
        private const double TRACKING_RATE_EPSILON = 0.000001;

        public IndiTelescope(INDIDeviceInfo info, IProfileService profileService) : base(info) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        private void Initialize() {
            _hasUnknownEpoch = false;
        }

        public AlignmentMode AlignmentMode => GetProperty(nameof(IINDITelescope.AlignmentMode), AlignmentMode.GermanPolar);

        public double Altitude => GetProperty(nameof(IINDITelescope.Altitude), double.NaN);
        public string AltitudeString {
            get {
                var altitude = Altitude;
                return double.IsNaN(altitude) ? string.Empty : AstroUtil.DegreesToDMS(altitude);
            }
        }

        public double Azimuth => GetProperty(nameof(IINDITelescope.Azimuth), double.NaN);

        public string AzimuthString {
            get {
                var azimuth = Azimuth;
                return double.IsNaN(azimuth) ? string.Empty : AstroUtil.DegreesToDMS(azimuth);
            }
        }

        public double ApertureArea => GetProperty(nameof(IINDITelescope.ApertureArea), -1d);

        public double ApertureDiameter => GetProperty(nameof(IINDITelescope.ApertureDiameter), -1d);

        public bool AtHome => GetProperty(nameof(IINDITelescope.AtHome), false);

        public bool AtPark => GetProperty(nameof(IINDITelescope.AtPark), false);

        private bool canFindHome = true;
        public bool CanFindHome => canFindHome;

        private bool canPark = true;
        public bool CanPark => canPark;

        private bool canPulseGuide = true;
        public bool CanPulseGuide => canPulseGuide;

        private bool canSetDeclinationRate = true;
        public bool CanSetDeclinationRate => canSetDeclinationRate;

        private bool canSetGuideRates = true;
        public bool CanSetGuideRates => canSetGuideRates;

        private bool canSetPark = true;
        public bool CanSetPark => canSetPark;

        private bool canSetPierSide = true;
        public bool CanSetPierSide => canSetPierSide;

        private bool canSetRightAscensionRate = true;
        public bool CanSetRightAscensionRate => canSetRightAscensionRate;


        private bool canSlew = true;
        public bool CanSlew => canSlew;

        private bool canSlewAltAz = true;
        public bool CanSlewAltAz => canSlewAltAz;

        private bool canSlewAltAzAsync = true;
        public bool CanSlewAltAzAsync => canSlewAltAzAsync;

        private bool canSlewAsync = true;
        public bool CanSlewAsync => canSlewAsync;


        private bool canSync = true;
        public bool CanSync => canSync;

        private bool canSyncAltAz = true;
        public bool CanSyncAltAz => canSyncAltAz;

        private bool canUnpark = true;
        public bool CanUnpark => canUnpark;

        public Coordinates Coordinates => new Coordinates(RightAscension, Declination, EquatorialSystem, Coordinates.RAType.Hours);

        public double Declination => GetProperty(nameof(IINDITelescope.Declination), -1d);

        public string DeclinationString => AstroUtil.DegreesToDMS(Declination);

        public double DeclinationRate {
            get => GetProperty(nameof(IINDITelescope.DeclinationRate), 0d);
            set {
                try {
                    if (CanSetDeclinationRate) {
                        SetProperty(nameof(IINDITelescope.DeclinationRate), value);
                    }
                } catch (NotImplementedException) {
                    canSetDeclinationRate = false;
                }
            }
        }

        public double RightAscensionRate {
            get => GetProperty(nameof(IINDITelescope.RightAscensionRate), 0d);
            set {
                try {
                    if (CanSetRightAscensionRate) {
                        SetProperty(nameof(IINDITelescope.RightAscensionRate), value);
                    }
                } catch (NotImplementedException) {
                    canSetRightAscensionRate = false;
                }
            }
        }

        public PierSide SideOfPier {
            get {
                var pierside = GetProperty(nameof(IINDITelescope.SideOfPier), PierSide.pierUnknown);
                return (PierSide)pierside;
            }
            set {
                try {
                    if (CanSetPierSide) {
                        SetProperty(nameof(IINDITelescope.SideOfPier), value);
                    }
                } catch (NotImplementedException) {
                    canSetPierSide = false;
                }
            }
        }

        public bool DoesRefraction => GetProperty(nameof(IINDITelescope.DoesRefraction), false);

        private Epoch equatorialSystem = Epoch.JNOW;

        public Epoch EquatorialSystem {
            get => equatorialSystem;
            private set {
                equatorialSystem = value;
                RaisePropertyChanged();
            }
        }

        public double FocalLength => GetProperty(nameof(IINDITelescope.FocalLength), -1d);

        public double RightAscension => GetProperty(nameof(IINDITelescope.RightAscension), -1d);

        public string RightAscensionString => AstroUtil.HoursToHMS(RightAscension);

        public double SiderealTime => GetProperty(nameof(IINDITelescope.SiderealTime), -1d);

        public string SiderealTimeString => AstroUtil.HoursToHMS(SiderealTime);

        public bool Slewing => GetProperty(nameof(IINDITelescope.Slewing), false);

        public bool IsPulseGuiding {
            get {
                try {
                    if (CanPulseGuide) {
                        return GetProperty(nameof(IINDITelescope.IsPulseGuiding), false);
                    } else {
                        return false;
                    }
                } catch (NotImplementedException) {
                    canPulseGuide = false;
                }
                return false;
            }
        }

        public double GuideRateDeclinationArcsecPerSec {
            get {
                try {
                    if (CanSetGuideRates) {
                        var rate = GetProperty(nameof(IINDITelescope.GuideRateDeclination), double.NaN);
                        if (!double.IsNaN(rate)) {
                            return rate * 3600.0;
                        }
                    }
                } catch (NotImplementedException) {
                    canSetGuideRates = false;
                }
                return double.NaN;
            }
            set {
                try {
                    if (CanSetGuideRates) {
                        SetProperty(nameof(IINDITelescope.GuideRateDeclination), value);
                    }
                } catch (NotImplementedException) {
                    canSetGuideRates = false;
                }
            }
        }

        public double GuideRateRightAscensionArcsecPerSec {
            get {
                try {
                    if (CanSetGuideRates) {
                        var rate = GetProperty(nameof(IINDITelescope.GuideRateRightAscension), double.NaN);
                        if (!double.IsNaN(rate)) {
                            return rate * 3600.0;
                        }
                    }
                } catch (NotImplementedException) {
                    canSetGuideRates = false;
                }
                return double.NaN;
            }
            set {
                try {
                    if (CanSetGuideRates) {
                        SetProperty(nameof(IINDITelescope.GuideRateRightAscension), value);
                    }
                } catch (NotImplementedException) {
                    canSetGuideRates = false;
                }
            }
        }

        public DateTime UTCDate {
            get => GetProperty(nameof(IINDITelescope.UTCDate), DateTime.MinValue);
            set => SetProperty(nameof(IINDITelescope.UTCDate), value);
        }

        public double SiteElevation {
            get => GetProperty(nameof(IINDITelescope.SiteElevation), 0d);
            set => SetProperty(nameof(IINDITelescope.SiteElevation), value);
        }

        public double SiteLatitude {
            get => GetProperty(nameof(IINDITelescope.SiteLatitude), -1d);
            set => SetProperty(nameof(IINDITelescope.SiteLatitude), value);
        }

        public double SiteLongitude {
            get => GetProperty(nameof(IINDITelescope.SiteLongitude), -1d);
            set => SetProperty(nameof(IINDITelescope.SiteLongitude), value);
        }

        public short SlewSettleTime {
            get => GetProperty<short>(nameof(IINDITelescope.SlewSettleTime), -1);
            set => SetProperty(nameof(IINDITelescope.SlewSettleTime), value);
        }

        public double TargetDeclination {
            get {
                double val = double.NaN;
                if (!Slewing) {
                    val = GetProperty(nameof(IINDITelescope.TargetDeclination), double.NaN);
                }
                return val;
            }
            set => SetProperty(nameof(IINDITelescope.TargetDeclination), value);
        }

        public double TargetRightAscension {
            get {
                double val = double.NaN;
                if (!Slewing) {
                    val = GetProperty(nameof(IINDITelescope.TargetRightAscension), double.NaN);
                }
                return val;
            }
            set => SetProperty(nameof(IINDITelescope.TargetRightAscension), value);
        }

        private Coordinates _targetCoordinates;

        public Coordinates TargetCoordinates {
            get {
                if (ShouldBeConnected) {
                    return _targetCoordinates;
                }
                return null;
            }
            private set {
                _targetCoordinates = value;
                RaisePropertyChanged();
            }
        }

        private PierSide? targetSideOfPier;

        public PierSide? TargetSideOfPier {
            get {
                if (ShouldBeConnected) {
                    return targetSideOfPier;
                } else {
                    return null;
                }
            }
            set {
                targetSideOfPier = value;
                RaisePropertyChanged();
            }
        }

        private bool _hasUnknownEpoch;

        public bool HasUnknownEpoch {
            get => _hasUnknownEpoch;
            private set { _hasUnknownEpoch = value; RaisePropertyChanged(); }
        }

        private async Task<bool> SetPierSide(PierSide targetPierSide) {
            try {
                var pierside = SideOfPier;
                Logger.Debug($"Setting pier side from {pierside} to {targetPierSide}");

                TargetSideOfPier = targetPierSide;
                SideOfPier = targetPierSide;

                //Check if setting the pier side will result already in a flip
                await CoreUtil.Wait(TimeSpan.FromSeconds(2));
                while (Slewing) {
                    await CoreUtil.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval));
                }
                return true;
            } catch (Exception ex) {
                Logger.Error("Failed to flip side of pier", ex);
                return false;
            }
        }

        public async Task<bool> MeridianFlip(Coordinates targetCoordinates, CancellationToken token) {
            var success = false;
            try {
                if (!TrackingEnabled) {
                    TrackingEnabled = true;
                }

                var targetSideOfPier = NINA.Astrometry.MeridianFlip.ExpectedPierSide(
                    coordinates: targetCoordinates,
                    localSiderealTime: Angle.ByHours(SiderealTime));
                if (profileService.ActiveProfile.MeridianFlipSettings.UseSideOfPier) {
                    var sop = SideOfPier;
                    Logger.Info($"Mount side of pier is currently {sop}, and target is {targetSideOfPier}");
                    if (targetSideOfPier == sop) {
                        Logger.Info($"Current Side of Pier ({sop}) is equal to Target Side of Pier ({targetSideOfPier}). No flip is required");
                        // No flip required
                        return true;
                    }
                }


                targetCoordinates = targetCoordinates.Transform(EquatorialSystem);
                TargetCoordinates = targetCoordinates;
                // If we can't set the side of pier, consider our work done up front already                
                bool pierSideSuccess = !CanSetPierSide;
                bool checkAndSetPierSideAfterFirstSlew = false;

                // check for the CURRENT mount position
                // This is not necessarily equals to the target position after the flip (e.g. pause before meridian stops tracking)
                var currentExpectedSideOfPier = NINA.Astrometry.MeridianFlip.ExpectedPierSide(
                    coordinates: this.Coordinates,
                    localSiderealTime: Angle.ByHours(SiderealTime));
                var currentSoP = this.SideOfPier;
                if (currentExpectedSideOfPier == currentSoP) {
                    // we are not yet past meridian - the mount is in Counter Weight DOWN position
                    // Hence it should be avoided setting SoP as a SoP change slew could result in a Counter Weight UP position
                    Logger.Info($"Current side of pier is {currentSoP}, which should be a counter weight down position already. Setting side of pier will be done after the first slew attempt if the mount did not flip the pier side by then.");
                    pierSideSuccess = true;
                    checkAndSetPierSideAfterFirstSlew = true;
                }


                bool slewSuccess = false;
                int retries = 0;
                do {
                    if (!pierSideSuccess) {
                        Logger.Info($"Setting pier side to {targetSideOfPier}");
                        pierSideSuccess = await SetPierSide(targetSideOfPier);
                    }
                    // Keep attempting slews as well, in case that's what it takes to flip to the other side of pier
                    Logger.Info($"Slewing to coordinates {targetCoordinates}. Attempt {retries + 1} / {MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS}");
                    slewSuccess = await SlewToCoordinates(targetCoordinates, token);

                    if (checkAndSetPierSideAfterFirstSlew) {
                        if (SideOfPier != targetSideOfPier) {
                            Logger.Info($"Setting pier side to {targetSideOfPier} after initial slew as the mount seems to not have flipped yet.");
                            pierSideSuccess = await SetPierSide(targetSideOfPier);
                            checkAndSetPierSideAfterFirstSlew = false;
                        }
                    }

                    if (!pierSideSuccess) {
                        pierSideSuccess = SideOfPier == targetSideOfPier;
                    }
                    success = slewSuccess && pierSideSuccess;
                    Logger.Info($"Finished slewing to coordinates. Slew was {(slewSuccess ? "successful" : "NOT successful")}. Setting pier side was {(pierSideSuccess ? "successful" : "NOT successful")}");
                    if (!success) {
                        if (retries++ >= MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS) {
                            Logger.Error("Failed to slew for Meridian Flip, even after retrying");
                            Notification.ShowError(Loc.Instance["LblMeridianFlipRetryFailed"]);
                            break;
                        } else {
                            var jsnowCoordinates = targetCoordinates.Transform(Epoch.JNOW);
                            var topocentricCoordinates = jsnowCoordinates.Transform(latitude: Angle.ByDegree(SiteLatitude), longitude: Angle.ByDegree(SiteLongitude), SiteElevation);
                            Logger.Warning($"Failed to slew for Meridian Flip. Retry {retries} of {MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS} times with a {MERIDIAN_FLIP_SLEW_RETRY_WAIT} wait between each.  " +
                                $"SideOfPier: {SideOfPier}, RA: {jsnowCoordinates.RAString}, Dec: {jsnowCoordinates.DecString}, Azimuth: {topocentricCoordinates.Azimuth}");

                            Notification.ShowWarning(string.Format(Loc.Instance["LblMeridianFlipRetry"], MERIDIAN_FLIP_SLEW_RETRY_WAIT.TotalSeconds, retries, MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS));
                            await Task.Delay(MERIDIAN_FLIP_SLEW_RETRY_WAIT, token);
                        }
                    }
                } while (!success);

                if (success && retries > 0) {
                    Logger.Info("Successfully slewed for Meridian Flip after retrying");
                    Notification.ShowWarning(string.Format(Loc.Instance["LblMeridianFlipWaitLonger"], retries));
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowExternalError(Loc.Instance["LblMeridianFlipFailed"] + "" + ex.Message, Loc.Instance["LblASCOMDriverError"]);
            } finally {
                TargetCoordinates = null;
                TargetSideOfPier = null;
            }
            return success;
        }

        /// <summary>
        /// Retrieves axis rates for a given axis
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>A collection of touples of (minimum, maximum) Rates</returns>

        public IList<(double, double)> GetAxisRates(TelescopeAxes axis) {
            List<(double, double)> axisRates = new List<(double, double)>();
            try {
                var rates = device.AxisRates(axis);
                foreach (IRate item in rates) {
                    axisRates.Add((item.Minimum, item.Maximum));
                }
            } catch (Exception) {
            }
            return axisRates;
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            try {
                if (ShouldBeConnected) {
                    if (CanSlew) {
                        if (!AtPark) {
                            var actualRate = rate;
                            try {
                                if (axis == TelescopeAxes.Primary && !CanMovePrimaryAxis) {
                                    Logger.Warning("Telescope cannot move primary axis");
                                    Notification.ShowWarning(Loc.Instance["LblTelescopeCannotMovePrimaryAxis"]);
                                } else if (axis == TelescopeAxes.Secondary && !CanMoveSecondaryAxis) {
                                    Logger.Warning("Telescope cannot move secondary axis");
                                    Notification.ShowWarning(Loc.Instance["LblTelescopeCannotMoveSecondaryAxis"]);
                                } else {
                                    if (actualRate != 0) {
                                        //Check that the given rate falls into the values of acceptable rates and adjust to the nearest rate if outside
                                        var sign = 1;
                                        if (actualRate < 0) { sign = -1; }
                                        actualRate = GetAdjustedMovingRate(Math.Abs(rate), Math.Abs(rate), axis) * sign;
                                    }
                                    Logger.Info($"Moving {axis} Telescope Axis using rate {actualRate}.");
                                    device.MoveAxis(axis, actualRate);
                                    InvalidatePropertyCache();
                                }
                            } catch (Exception e) {
                                Logger.Error(e);
                                Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                            }
                        } else {
                            Logger.Warning("Telescope parked");
                            Notification.ShowWarning(Loc.Instance["LblTelescopeParkedWarn"]);
                        }
                    } else {
                        Logger.Warning("Telescope cannot slew");
                        Notification.ShowWarning(Loc.Instance["LblTelescopeCannotSlew"]);
                    }
                } else {
                    Logger.Warning("Telescope not connected");
                    Notification.ShowWarning(Loc.Instance["LblTelescopeNotConnected"]);
                }
            } catch (NotImplementedException) {
                canSlew = false;
            }
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            try {
                if (ShouldBeConnected) {
                    if (CanPulseGuide) {
                        if (!AtPark) {
                            try {
                                device.PulseGuide(direction, duration);
                                InvalidatePropertyCache();
                            } catch (Exception e) {
                                Logger.Error(e);
                                Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                            }
                        } else {
                            Notification.ShowWarning(Loc.Instance["LblTelescopeParkedWarn"]);
                        }
                    } else {
                        Notification.ShowWarning(Loc.Instance["LblTelescopeCannotPulseGuide"]);
                    }
                } else {
                    Notification.ShowWarning(Loc.Instance["LblTelescopeNotConnected"]);
                }
            } catch (NotImplementedException) {
                canPulseGuide = false;
            }
        }

        public async Task Park(CancellationToken token) {
            try {
                if (CanPark) {
                    try {
                        await device.ParkAsync(token);
                        InvalidatePropertyCache();
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (NotImplementedException) {
                        canPark = false;
                    } catch (Exception e) {
                        Logger.Error(e);
                        Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                    }
                }
            } catch (NotImplementedException) {
                canPark = false;
            }
        }

        public void Setpark() {
            try {
                if (CanSetPark) {
                    try {
                        device.SetPark();
                    } catch (Exception e) {
                        Logger.Error(e);
                        Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                    }
                }
            } catch (NotImplementedException) {
                canSetPark = false;
            }
        }

        private bool canSetTrackingEnabled = true;
        public bool CanSetTrackingEnabled => ShouldBeConnected && canSetTrackingEnabled;

        public bool TrackingEnabled {
            get => GetProperty(nameof(IINDITelescope.Tracking), false);
            set {
                try {
                    if (CanSetTrackingEnabled) {
                        if (SetProperty(nameof(IINDITelescope.Tracking), value)) {
                            RaisePropertyChanged(nameof(TrackingMode));
                            RaisePropertyChanged(nameof(TrackingRate));
                        }
                    }
                } catch (NotImplementedException) {
                    canSetTrackingEnabled = false;
                }
            }
        }

        public async Task<bool> SlewToCoordinates(Coordinates coordinates, CancellationToken token) {
            if (ShouldBeConnected && !AtPark) {
                try {
                    TrackingEnabled = true;
                    TargetCoordinates = coordinates.Transform(EquatorialSystem);

                    if (CanSlewAsync) {
                        await device.SlewToCoordinatesTaskAsync(TargetCoordinates.RA, TargetCoordinates.Dec, token);
                        InvalidatePropertyCache();
                    } else {
                        device.SlewToCoordinates(TargetCoordinates.RA, TargetCoordinates.Dec);
                        await Task.Delay(200, token);
                        while (Slewing) {
                            await CoreUtil.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval), token);
                        }
                    }

                    return true;
                } catch (OperationCanceledException) {
                    throw;
                } catch (NotImplementedException) {
                    canSlewAsync = false;
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                } finally {
                    TargetCoordinates = null;
                }
            }
            return false;
        }

        public async Task<bool> SlewToAltAz(TopocentricCoordinates coordinates, CancellationToken token) {
            if (ShouldBeConnected && !AtPark && CanSlewAltAz) {
                try {
                    TrackingEnabled = false;
                    TargetCoordinates = coordinates.Transform(EquatorialSystem);

                    if (CanSlewAltAzAsync) {
                        await device.SlewToAltAzTaskAsync(azimuth: coordinates.Azimuth.Degree, altitude: coordinates.Altitude.Degree, cancellationToken: token);
                        InvalidatePropertyCache();
                    } else {
                        device.SlewToAltAz(Azimuth: coordinates.Azimuth.Degree, Altitude: coordinates.Altitude.Degree);
                        await Task.Delay(200, token);
                        while (Slewing) {
                            await CoreUtil.Wait(TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval), token);
                        }
                    }

                    return true;
                } catch (OperationCanceledException) {
                    throw;
                } catch (NotImplementedException) {
                    canSlewAltAz = canSlewAltAzAsync = false;
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                } finally {
                    TargetCoordinates = null;
                }
            }
            return false;
        }

        public void StopSlew() {
            try {
                if (CanSlew) {
                    device.AbortSlew();
                }
            } catch (NotImplementedException) {
                canSlew = false;
            }
        }

        public bool Sync(Coordinates coordinates) {
            bool success = false;
            try {
                if (CanSync) {
                    if (TrackingEnabled) {
                        try {
                            coordinates = coordinates.Transform(EquatorialSystem);
                            device.SyncToCoordinates(coordinates.RA, coordinates.Dec);
                            InvalidatePropertyCache();
                            success = true;
                        } catch (Exception ex) {
                            Logger.Error(ex);
                            Notification.ShowExternalError(ex.Message, Loc.Instance["LblASCOMDriverError"]);
                        }
                    } else {
                        Logger.Error("Telescope is not tracking to be able to sync");
                        Notification.ShowError(Loc.Instance["LblTelescopeNotTrackingForSync"]);
                    }
                }
            } catch (NotImplementedException) {
                canSync = false;
            }
            return success;
        }

        public async Task FindHome(CancellationToken token) {
            if (CanFindHome) {
                try {
                    await device.FindHomeAsync(token);
                    InvalidatePropertyCache();
                } catch (OperationCanceledException) {
                    throw;
                } catch (NotImplementedException) {
                    canFindHome = false;
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                }
            }
        }

        public async Task Unpark(CancellationToken token) {
            try {
                if (CanUnpark) {
                    try {
                        await device.UnparkAsync(token);
                        InvalidatePropertyCache();
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (Exception e) {
                        Logger.Error(e);
                        Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                    }
                }
            } catch (NotImplementedException) {
                canUnpark = false;
            }
        }

        public double HoursToMeridian {
            get {
                if (TrackingEnabled) {
                    return Astrometry.MeridianFlip.TimeToMeridian(
                    coordinates: Coordinates,
                    localSiderealTime: Angle.ByHours(SiderealTime)).TotalHours;
                }
                return 24;
            }
        }

        public string HoursToMeridianString => AstroUtil.HoursToHMS(HoursToMeridian);

        public double TimeToMeridianFlip {
            get {
                try {
                    if (TrackingEnabled) {
                        return Astrometry.MeridianFlip.TimeToMeridianFlip(
                            settings: profileService.ActiveProfile.MeridianFlipSettings,
                            coordinates: Coordinates,
                            localSiderealTime: Angle.ByHours(SiderealTime),
                            currentSideOfPier: SideOfPier).TotalHours;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowExternalError(ex.Message, Loc.Instance["LblASCOMDriverError"]);
                }
                return 24;
            }
        }

        public string TimeToMeridianFlipString => AstroUtil.HoursToHMS(TimeToMeridianFlip);

        public bool CanMovePrimaryAxis {
            get {
                if (ShouldBeConnected) {
                    return device.CanMoveAxis(TelescopeAxes.Primary);
                }
                return false;
            }
        }

        public bool CanMoveSecondaryAxis {
            get {
                if (ShouldBeConnected) {
                    return device.CanMoveAxis(TelescopeAxes.Secondary);
                }
                return false;
            }
        }

        private double primaryMovingRate = double.NaN;

        public double PrimaryMovingRate {
            get {
                if (double.IsNaN(primaryMovingRate)) {
                    PrimaryMovingRate = primaryMovingRate;
                }

                return primaryMovingRate;
            }
            set {
                if (ShouldBeConnected) {
                    primaryMovingRate = GetAdjustedMovingRate(value, primaryMovingRate, TelescopeAxes.Primary);
                    RaisePropertyChanged();
                }
            }
        }

        private double secondaryMovingRate = double.NaN;

        public double SecondaryMovingRate {
            get {
                if (double.IsNaN(secondaryMovingRate)) {
                    SecondaryMovingRate = secondaryMovingRate;
                }

                return secondaryMovingRate;
            }
            set {
                if (ShouldBeConnected) {
                    secondaryMovingRate = GetAdjustedMovingRate(value, secondaryMovingRate, TelescopeAxes.Secondary);
                    RaisePropertyChanged();
                }
            }
        }

        private double GetAdjustedMovingRate(double value, double oldValue, TelescopeAxes axis) {
            double result = value;
            if (result < 0) result = 0;
            bool incr = result > oldValue;

            double max = double.MinValue;
            double min = double.MaxValue;
            IAxisRates r = device.AxisRates(axis);
            IEnumerator e = r.GetEnumerator();
            foreach (IRate item in r) {
                if (min > item.Minimum) {
                    min = item.Minimum;
                }
                if (max < item.Maximum) {
                    max = item.Maximum;
                }

                if (item.Minimum <= value && value <= item.Maximum) {
                    result = value;
                    break;
                } else if (incr && value < item.Minimum) {
                    result = item.Minimum;
                } else if (!incr && value > item.Maximum) {
                    result = item.Maximum;
                }
            }
            if (result > max || double.IsNaN(oldValue)) result = max;
            if (result < min) result = min;

            return result;
        }

        private void CheckMountTime() {
            double warningThreshold = 10; // Time difference, in seconds, greater than which we will issue a warning
            DateTime mountTime;

            try {
                mountTime = device.UTCDate;
            } catch (Exception e) {
                // e.g. InvalidValueException, DriverException - docs are not entirely clear
                Logger.Error("Unexpected exception when reading mount time. Skipping clock comparison and setting of mount time.", e);
                return;
            }

            var systemTime = DateTime.UtcNow;
            var timeDiff = Math.Abs((mountTime - systemTime).TotalSeconds);

            Logger.Info($"Mount UTC Time: {mountTime:u} / System UTC Time: {systemTime:u}; Difference: {timeDiff:0.0##} seconds");


            if (profileService.ActiveProfile.TelescopeSettings.TimeSync) {
                // Sync system's time to the mount
                try {
                    device.UTCDate = DateTime.UtcNow;
                    Logger.Info($"System time has been synced to the mount");
                } catch (Exception ex) {
                    Logger.Error($"Unexpected exception when trying to set UTCDate:{Environment.NewLine}{ex}");
                    return;
                }

                // One last check
                timeDiff = Math.Abs((device.UTCDate - DateTime.UtcNow).TotalSeconds);
            }

            if (timeDiff >= warningThreshold) {
                Logger.Warning($"System and mount time differ by {timeDiff:0.0##} seconds.");
                Notification.ShowWarning(string.Format(Loc.Instance["LblMountTimeDifferenceTooLarge"], timeDiff));
            }

        }

        private TrackingMode GetNINATrackingMode(INDI.Enums.TrackingMode mode) {
            switch (mode) {
                case INDI.Enums.TrackingMode.Sidereal: return TrackingMode.Sidereal;
                case INDI.Enums.TrackingMode.Lunar: return TrackingMode.Lunar;
                case INDI.Enums.TrackingMode.Solar: return TrackingMode.Solar;
                case INDI.Enums.TrackingMode.King: return TrackingMode.King;
                case INDI.Enums.TrackingMode.Custom: return TrackingMode.Custom;
                default: return TrackingMode.Stopped;
            }
        }

        private INDI.Enums.TrackingMode GetINDITrackingMode(TrackingMode mode) {
            switch (mode) {
                case TrackingMode.Sidereal: return INDI.Enums.TrackingMode.Sidereal;
                case TrackingMode.Lunar: return INDI.Enums.TrackingMode.Lunar;
                case TrackingMode.Solar: return INDI.Enums.TrackingMode.Solar;
                case TrackingMode.King: return INDI.Enums.TrackingMode.King;
                case TrackingMode.Custom: return INDI.Enums.TrackingMode.Custom;
                default: return INDI.Enums.TrackingMode.Stopped;
            }
        }

        private ImmutableList<TrackingMode> GetTrackingModes() {
            var trackingModes = ImmutableList.CreateBuilder<TrackingMode>();
            try {
                // Get the supported modes from the INDI device
                var supportedModes = device.GetSupportedTrackingModes();
                foreach (var mode in supportedModes) {
                    trackingModes.Add(GetNINATrackingMode(mode));
                }

                // Add Custom mode if we can set RA/Dec rates
                if (CanSetRightAscensionRate && CanSetDeclinationRate && !trackingModes.Contains(TrackingMode.Custom)) {
                    // Insert Custom before Stopped
                    var temp = trackingModes.ToImmutable();
                    trackingModes.Clear();
                    foreach (var mode in temp) {
                        if (mode == TrackingMode.Stopped) {
                            trackingModes.Add(TrackingMode.Custom);
                        }
                        trackingModes.Add(mode);
                    }
                }
            } catch (Exception ex) {
                Logger.Debug($"Failed to get supported tracking modes from INDI device: {ex.Message}");
                // Fallback to basic modes
                trackingModes.Add(TrackingMode.Sidereal);
                if (CanSetRightAscensionRate && CanSetDeclinationRate) {
                    trackingModes.Add(TrackingMode.Custom);
                }
                trackingModes.Add(TrackingMode.Stopped);
            }
            return trackingModes.ToImmutable();
        }

        private ImmutableList<TrackingMode> trackingModes = ImmutableList.Create<TrackingMode>();

        public IList<TrackingMode> TrackingModes => trackingModes;

        public TrackingRate TrackingRate {
            get {
                // Cache TrackingEnabled to avoid multiple INDI queries
                var trackingEnabled = TrackingEnabled;

                if (!ShouldBeConnected || !trackingEnabled) {
                    return new TrackingRate() { TrackingMode = TrackingMode.Stopped };
                } else if (!CanSetTrackingEnabled) {
                    return new TrackingRate() { TrackingMode = TrackingMode.Sidereal };
                }

                try {
                    // Get the current tracking mode from the INDI device
                    var mode = device.GetTrackingMode();

                    // If custom mode, check for custom rates
                    if (mode == INDI.Enums.TrackingMode.King || mode == INDI.Enums.TrackingMode.Custom) {
                        if (Math.Abs(DeclinationRate) >= TRACKING_RATE_EPSILON || Math.Abs(RightAscensionRate) >= TRACKING_RATE_EPSILON) {
                            return new TrackingRate() {
                                TrackingMode = TrackingMode.Custom,
                                CustomRightAscensionRate = RightAscensionRate,
                                CustomDeclinationRate = DeclinationRate
                            };
                        }
                        return new TrackingRate() { TrackingMode = TrackingMode.King };
                    }

                    return new TrackingRate() { TrackingMode = GetNINATrackingMode(mode) };
                } catch (Exception ex) {
                    Logger.Debug($"Failed to get tracking mode from INDI device: {ex.Message}");
                    return new TrackingRate() { TrackingMode = TrackingMode.Sidereal };
                }
            }
        }

        public TrackingMode TrackingMode {
            get => TrackingRate.TrackingMode;
            set {
                if (value == TrackingMode.Custom) {
                    throw new ArgumentException("TrackingMode cannot be set to Custom. Use SetCustomTrackingRate");
                }

                if (ShouldBeConnected && CanSetTrackingEnabled) {
                    try {
                        var currentTrackingMode = TrackingRate.TrackingMode;

                        // Call the INDI device method directly with the TrackingMode enum
                        device.SetTrackingMode(GetINDITrackingMode(value));

                        if (currentTrackingMode != value) {
                            RaisePropertyChanged();
                            RaisePropertyChanged(nameof(TrackingRate));
                            RaisePropertyChanged(nameof(TrackingEnabled));
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowExternalError(ex.Message, Loc.Instance["LblINDIDriverError"]);
                    }
                }
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblTelescopeConnectionLost"];

        public void SetCustomTrackingRate(double rightAscensionRate, double declinationRate) {
            if (!this.TrackingModes.Contains(TrackingMode.Custom)) {
                throw new NotSupportedException("Custom tracking rate not supported");
            }

            if (rightAscensionRate != 0 || declinationRate != 0) {
                try {
                    this.device.SetTrackingMode(INDI.Enums.TrackingMode.Sidereal);
                } catch (NotImplementedException ex) {
                    // TrackingRate Write can throw a PropertyNotImplementedException.
                    Logger.Debug(ex.Message);
                }
                if (this.CanSetTrackingEnabled) {
                    this.device.Tracking = true;
                }
                RaisePropertyChanged(nameof(TrackingMode));
                RaisePropertyChanged(nameof(TrackingRate));
                RaisePropertyChanged(nameof(TrackingEnabled));
            }
            this.device.RightAscensionRate = rightAscensionRate;
            this.device.DeclinationRate = declinationRate;
        }

        protected override Task PreConnect() {
            // Configure connection properties from profile before connecting
            var settings = profileService.ActiveProfile.TelescopeSettings;
            var instance = GetInstance();
            instance.ConfigureConnectionProperties(settings.ConnectionMode, settings.DevicePort, settings.BaudRate);
            return Task.CompletedTask;
        }

        protected override Task PostConnect() {
            Initialize();
            // Configure INDI to use JNOW (EOD) coordinates
            device.ConfigureJNOW();
            EquatorialSystem = Epoch.JNOW;
            trackingModes = GetTrackingModes();
            CheckMountTime();

            return Task.CompletedTask;
        }

        protected override IINDITelescope GetInstance() {
            return device ??= new INDITelescope(_device);
        }

        public PierSide DestinationSideOfPier(Coordinates coordinates) {
            coordinates = coordinates.Transform(EquatorialSystem);
            var pierSide = device.DestinationSideOfPier(coordinates.RA, coordinates.Dec);
            return (PierSide)pierSide;
        }

        public override bool HasSetupDialog => !Connected;
    }
}
