#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.Common.DeviceInterfaces;
using ASCOM.Com.DriverAccess;
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
using PierSide = NINA.Core.Enum.PierSide;
using TelescopeAxes = NINA.Core.Enum.TelescopeAxes;
using GuideDirections = NINA.Core.Enum.GuideDirections;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using ASCOM.Common;
using ASCOM;
using NINA.Equipment.Utility;
using ASCOM.Alpaca.Discovery;

namespace NINA.Equipment.Equipment.MyTelescope {

    internal class AscomTelescope : AscomDevice<ITelescopeV4>, ITelescope, IDisposable {
        private static readonly TimeSpan MERIDIAN_FLIP_SLEW_RETRY_WAIT = TimeSpan.FromMinutes(1);
        private const int MERIDIAN_FLIP_SLEW_RETRY_ATTEMPTS = 20;
        private const double TRACKING_RATE_EPSILON = 0.000001;

        public AscomTelescope(string telescopeId, string name, IProfileService profileService) : base(telescopeId, name) {
            this.profileService = profileService;
        }
        public AscomTelescope(AscomDevice deviceMeta, IProfileService profileService) : base(deviceMeta) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        private void Initialize() {
            _hasUnknownEpoch = false;
        }

        public Core.Enum.AlignmentMode AlignmentMode => (Core.Enum.AlignmentMode)GetProperty(nameof(Telescope.AlignmentMode), ASCOM.Common.DeviceInterfaces.AlignmentMode.GermanPolar);

        public double Altitude => GetProperty(nameof(Telescope.Altitude), double.NaN);

        public string AltitudeString => double.IsNaN(Altitude) ? string.Empty : AstroUtil.DegreesToDMS(Altitude);

        public double Azimuth => GetProperty(nameof(Telescope.Azimuth), double.NaN);

        public string AzimuthString => double.IsNaN(Azimuth) ? string.Empty : AstroUtil.DegreesToDMS(Azimuth);

        public double ApertureArea => GetProperty(nameof(Telescope.ApertureArea), -1d);

        public double ApertureDiameter => GetProperty(nameof(Telescope.ApertureDiameter), -1d);

        public bool AtHome => GetProperty(nameof(Telescope.AtHome), false);

        public bool AtPark => GetProperty(nameof(Telescope.AtPark), false);

        public bool CanFindHome => GetProperty(nameof(Telescope.CanFindHome), false);

        public bool CanPark => GetProperty(nameof(Telescope.CanPark), false);

        public bool CanPulseGuide => GetProperty(nameof(Telescope.CanPulseGuide), false);

        public bool CanSetDeclinationRate => GetProperty(nameof(Telescope.CanSetDeclinationRate), false);

        public bool CanSetGuideRates => GetProperty(nameof(Telescope.CanSetGuideRates), false);

        public bool CanSetPark => GetProperty(nameof(Telescope.CanSetPark), false);

        public bool CanSetPierSide => GetProperty(nameof(Telescope.CanSetPierSide), false);

        public bool CanSetRightAscensionRate => GetProperty(nameof(Telescope.CanSetRightAscensionRate), false);


        public bool CanSlew => GetProperty(nameof(Telescope.CanSlew), false);

        public bool CanSlewAltAz => GetProperty(nameof(Telescope.CanSlewAltAz), false);

        public bool CanSlewAltAzAsync => GetProperty(nameof(Telescope.CanSlewAltAzAsync), false);

        public bool CanSlewAsync => GetProperty(nameof(Telescope.CanSlewAsync), false);

        public bool CanSync => GetProperty(nameof(Telescope.CanSync), false);

        public bool CanSyncAltAz => GetProperty(nameof(Telescope.CanSyncAltAz), false);

        public bool CanUnpark => GetProperty(nameof(Telescope.CanUnpark), false);

        public Coordinates Coordinates => new Coordinates(RightAscension, Declination, EquatorialSystem, Coordinates.RAType.Hours);

        public double Declination => GetProperty(nameof(Telescope.Declination), -1d);

        public string DeclinationString => AstroUtil.DegreesToDMS(Declination);

        public double DeclinationRate {
            get => GetProperty(nameof(Telescope.DeclinationRate), 0d);
            set {
                if (CanSetDeclinationRate) {
                    SetProperty(nameof(Telescope.DeclinationRate), value);
                }
            }
        }

        public double RightAscensionRate {
            get => GetProperty(nameof(Telescope.RightAscensionRate), 0d);
            set {
                if (CanSetRightAscensionRate) {
                    SetProperty(nameof(Telescope.RightAscensionRate), value);
                }
            }
        }

        public PierSide SideOfPier {
            get {
                var pierside = GetProperty(nameof(Telescope.SideOfPier), ASCOM.Common.DeviceInterfaces.PointingState.Unknown);
                return (PierSide)pierside;
            }
            set {
                if (CanSetPierSide) {
                    SetProperty(nameof(Telescope.SideOfPier), (ASCOM.Common.DeviceInterfaces.PointingState)value);
                }
            }
        }

        public bool DoesRefraction => GetProperty(nameof(Telescope.DoesRefraction), false);

        private Epoch equatorialSystem = Epoch.J2000;

        public Epoch EquatorialSystem {
            get => equatorialSystem;
            private set {
                equatorialSystem = value;
                RaisePropertyChanged();
            }
        }

        public double FocalLength => GetProperty(nameof(Telescope.FocalLength), -1d);

        public double RightAscension => GetProperty(nameof(Telescope.RightAscension), -1d);

        public string RightAscensionString => AstroUtil.HoursToHMS(RightAscension);

        public double SiderealTime => GetProperty(nameof(Telescope.SiderealTime), -1d);

        public string SiderealTimeString => AstroUtil.HoursToHMS(SiderealTime);

        public bool Slewing => GetProperty(nameof(Telescope.Slewing), false);

        public bool IsPulseGuiding {
            get {
                if (CanPulseGuide) {
                    return GetProperty(nameof(Telescope.IsPulseGuiding), false);
                } else {
                    return false;
                }
            }
        }

        public double GuideRateDeclinationArcsecPerSec {
            get {
                if (CanSetGuideRates) {
                    var rate = GetProperty(nameof(Telescope.GuideRateDeclination), double.NaN);
                    if (!double.IsNaN(rate)) {
                        return rate * 3600.0;
                    }
                }
                return double.NaN;
            }
            set {
                if (CanSetGuideRates) {
                    SetProperty(nameof(Telescope.GuideRateDeclination), value);
                }
            }
        }

        public double GuideRateRightAscensionArcsecPerSec {
            get {
                if (CanSetGuideRates) {
                    var rate = GetProperty(nameof(Telescope.GuideRateRightAscension), double.NaN);
                    if (!double.IsNaN(rate)) {
                        return rate * 3600.0;
                    }
                }
                return double.NaN;
            }
            set {
                if (CanSetGuideRates) {
                    SetProperty(nameof(Telescope.GuideRateRightAscension), value);
                }
            }
        }

        public DateTime UTCDate {
            get => GetProperty(nameof(Telescope.UTCDate), DateTime.MinValue);
            set => SetProperty(nameof(Telescope.UTCDate), value);
        }

        public double SiteElevation {
            get => GetProperty(nameof(Telescope.SiteElevation), 0d);
            set => SetProperty(nameof(Telescope.SiteElevation), value);
        }

        public double SiteLatitude {
            get => GetProperty(nameof(Telescope.SiteLatitude), -1d);
            set => SetProperty(nameof(Telescope.SiteLatitude), value);
        }

        public double SiteLongitude {
            get => GetProperty(nameof(Telescope.SiteLongitude), -1d);
            set => SetProperty(nameof(Telescope.SiteLongitude), value);
        }

        public short SlewSettleTime {
            get => GetProperty<short>(nameof(Telescope.SlewSettleTime), -1);
            set => SetProperty(nameof(Telescope.SlewSettleTime), value);
        }

        public double TargetDeclination {
            get {
                double val = double.NaN;
                if (!Slewing) {
                    val = GetProperty(nameof(Telescope.TargetDeclination), double.NaN);
                }
                return val;
            }
            set => SetProperty(nameof(Telescope.TargetDeclination), value);
        }

        public double TargetRightAscension {
            get {
                double val = double.NaN;
                if (!Slewing) {
                    val = GetProperty(nameof(Telescope.TargetRightAscension), double.NaN);
                }
                return val;
            }
            set => SetProperty(nameof(Telescope.TargetRightAscension), value);
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

                // Add some padding to the current side of pier projection in case the meridian flip settings are very close to meridian
                var projectedSiderealTime = Angle.ByHours(AstroUtil.EuclidianModulus(SiderealTime + 0.2, 24));
                var targetSideOfPier = NINA.Astrometry.MeridianFlip.ExpectedPierSide(
                    coordinates: targetCoordinates,
                    localSiderealTime: projectedSiderealTime);
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

                    if(checkAndSetPierSideAfterFirstSlew) {
                        if(SideOfPier != targetSideOfPier) {
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

        private ASCOM.Common.DeviceInterfaces.TelescopeAxis TransformAxis(TelescopeAxes axis) {
            ASCOM.Common.DeviceInterfaces.TelescopeAxis translatedAxis;
            switch (axis) {
                case TelescopeAxes.Primary:
                    translatedAxis = ASCOM.Common.DeviceInterfaces.TelescopeAxis.Primary;
                    break;

                case TelescopeAxes.Secondary:
                    translatedAxis = ASCOM.Common.DeviceInterfaces.TelescopeAxis.Secondary;
                    break;

                case TelescopeAxes.Tertiary:
                    translatedAxis = ASCOM.Common.DeviceInterfaces.TelescopeAxis.Tertiary;
                    break;

                default:
                    translatedAxis = ASCOM.Common.DeviceInterfaces.TelescopeAxis.Primary;
                    break;
            }
            return translatedAxis;
        }

        /// <summary>
        /// Retrieves axis rates for a given axis
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>A collection of touples of (minimum, maximum) Rates</returns>
        public IList<(double, double)> GetAxisRates(TelescopeAxes axis) {
            var translatedAxis = TransformAxis(axis);
            List<(double, double)> axisRates = new List<(double, double)>();
            try {
                var rates = device.AxisRates(translatedAxis);
                foreach (IRate item in rates) {
                    axisRates.Add((item.Minimum, item.Maximum));
                }
            } catch (Exception) {
            }
            return axisRates;
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            if (ShouldBeConnected) {
                if (CanSlew) {
                    if (!AtPark) {
                        var actualRate = rate;
                        try {
                            var translatedAxis = TransformAxis(axis);

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
                                    actualRate = GetAdjustedMovingRate(Math.Abs(rate), Math.Abs(rate), translatedAxis) * sign;
                                }
                                Logger.Info($"Moving {translatedAxis} Telescope Axis using rate {actualRate}.");
                                device.MoveAxis(translatedAxis, actualRate);
                                InvalidatePropertyCache();
                            }
                        } catch (ASCOM.InvalidValueException e) {
                            Logger.Error(e);
                            Notification.ShowExternalError(string.Format(Loc.Instance["LblASCOMTelescopeDriveRateInvalid"], actualRate), Loc.Instance["LblASCOMDriverError"]);
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
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            if (ShouldBeConnected) {
                if (CanPulseGuide) {
                    if (!AtPark) {
                        try {
                            device.PulseGuide((ASCOM.Common.DeviceInterfaces.GuideDirection)direction, duration);
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
        }

        public async Task Park(CancellationToken token) {
            if (CanPark) {
                try {
                    await device.ParkAsync(token);
                    InvalidatePropertyCache();
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                }
            }
        }
        public void Setpark() {
            if (CanSetPark) {
                try {
                    device.SetPark();
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                }
            }
        }

        public bool CanSetTrackingEnabled => ShouldBeConnected && GetProperty(nameof(Telescope.CanSetTracking), false);

        public bool TrackingEnabled {
            get => GetProperty(nameof(Telescope.Tracking), false);
            set {
                if (CanSetTrackingEnabled) {
                    if (SetProperty(nameof(Telescope.Tracking), value)) {
                        RaisePropertyChanged(nameof(TrackingMode));
                        RaisePropertyChanged(nameof(TrackingRate));
                    }
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
            if (CanSlew) {
                device.AbortSlew();
            }
        }

        public bool Sync(Coordinates coordinates) {
            bool success = false;
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
            return success;
        }

        public async Task FindHome(CancellationToken token) {
            if (CanFindHome) {
                try {
                    await device.FindHomeAsync(token);
                    InvalidatePropertyCache();
                } catch(OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                }
            }
        }

        public async Task Unpark(CancellationToken token) {
            if (CanUnpark) {
                try {
                    await device.UnparkAsync(token);
                    InvalidatePropertyCache();
                } catch(OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    Logger.Error(e);
                    Notification.ShowExternalError(e.Message, Loc.Instance["LblASCOMDriverError"]);
                }
            }
        }

        public double HoursToMeridian {
            get {
                if (TrackingEnabled) {
                    return NINA.Astrometry.MeridianFlip.TimeToMeridian(
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
                        return NINA.Astrometry.MeridianFlip.TimeToMeridianFlip(
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
                    return device.CanMoveAxis(ASCOM.Common.DeviceInterfaces.TelescopeAxis.Primary);
                }
                return false;
            }
        }

        public bool CanMoveSecondaryAxis {
            get {
                if (ShouldBeConnected) {
                    return device.CanMoveAxis(ASCOM.Common.DeviceInterfaces.TelescopeAxis.Secondary);
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
                    primaryMovingRate = GetAdjustedMovingRate(value, primaryMovingRate, ASCOM.Common.DeviceInterfaces.TelescopeAxis.Primary);
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
                    secondaryMovingRate = GetAdjustedMovingRate(value, secondaryMovingRate, ASCOM.Common.DeviceInterfaces.TelescopeAxis.Secondary);
                    RaisePropertyChanged();
                }
            }
        }

        private double GetAdjustedMovingRate(double value, double oldValue, ASCOM.Common.DeviceInterfaces.TelescopeAxis axis) {
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

        private Epoch DetermineEquatorialSystem() {
            Epoch epoch = Epoch.JNOW;

            if (InterfaceVersion > 1) {
                EquatorialCoordinateType mountEqSystem = device.EquatorialSystem;

                switch (mountEqSystem) {
                    case EquatorialCoordinateType.B1950:
                        epoch = Epoch.B1950;
                        break;

                    case EquatorialCoordinateType.J2000:
                        epoch = Epoch.J2000;
                        break;

                    case EquatorialCoordinateType.J2050:
                        epoch = Epoch.J2050;
                        break;

                    case EquatorialCoordinateType.Other:
                        epoch = Epoch.J2000;
                        HasUnknownEpoch = true;
                        break;
                }
            }

            return epoch;
        }

        private void CheckMountTime() {
            double warningThreshold = 10; // Time difference, in seconds, greater than which we will issue a warning
            DateTime mountTime;

            try {
                mountTime = device.UTCDate;
            } catch (ASCOM.InvalidOperationException) {
                // In this case, ASCOM says we have to first write the time before being able to read it.
                try {
                    mountTime = UTCDate = DateTime.UtcNow;
                } catch (Exception) {
                    // It seems we cannot do anything with time on this mount. This is theoretically possible, so stop now.
                    Logger.Info("Unable to check or set mount time");
                    return;
                }
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
                    // ASCOM docs are confused - online docs says to expect PropertyNotImplementedException, but method comment says NotImplementedException.
                    // Whatever; we'll test for both
                    if (ex is ASCOM.PropertyNotImplementedException || ex is ASCOM.NotImplementedException) {
                        string message = "Mount driver does not allow the mount's time to be set.";

                        if (timeDiff >= warningThreshold) {
                            Logger.Warning($"{message} Mount and system have an excessive time difference of {timeDiff:0.0##} seconds.");
                            Notification.ShowWarning(string.Format(Loc.Instance["LblMountTimeDifferenceTooLarge"], timeDiff));
                            return;
                        }

                        Logger.Info(message);
                        return;
                    }

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

        private ImmutableList<TrackingMode> GetTrackingModes() {
            var trackingModes = ImmutableList.CreateBuilder<TrackingMode>();
            trackingModes.Add(TrackingMode.Sidereal);

            foreach(DriveRate trackingRate in device.TrackingRates) {
                switch (trackingRate) {
                    case DriveRate.King:
                        trackingModes.Add(TrackingMode.King);
                        break;

                    case DriveRate.Lunar:
                        trackingModes.Add(TrackingMode.Lunar);
                        break;

                    case DriveRate.Solar:
                        trackingModes.Add(TrackingMode.Solar);
                        break;
                }
            }

            if (device.CanSetRightAscensionRate && device.CanSetDeclinationRate) {
                trackingModes.Add(TrackingMode.Custom);
            }
            trackingModes.Add(TrackingMode.Stopped);
            return trackingModes.ToImmutable();
        }

        private ImmutableList<TrackingMode> trackingModes = ImmutableList.Create<TrackingMode>();

        public IList<TrackingMode> TrackingModes => trackingModes;

        public TrackingRate TrackingRate {
            get {
                if (!ShouldBeConnected || !TrackingEnabled) {
                    return new TrackingRate() { TrackingMode = TrackingMode.Stopped };
                } else if (!CanSetTrackingEnabled) {
                    return new TrackingRate() { TrackingMode = TrackingMode.Sidereal };
                }

                var ascomTrackingRate = GetProperty(nameof(Telescope.TrackingRate), DriveRate.Sidereal);
                if (ascomTrackingRate == DriveRate.Sidereal && (
                    Math.Abs(DeclinationRate) >= TRACKING_RATE_EPSILON) || (Math.Abs(RightAscensionRate) >= TRACKING_RATE_EPSILON)) {
                    return new TrackingRate() {
                        TrackingMode = TrackingMode.Custom,
                        CustomRightAscensionRate = RightAscensionRate,
                        CustomDeclinationRate = DeclinationRate
                    };
                }
                var trackingMode = TrackingMode.Sidereal;
                switch (ascomTrackingRate) {
                    case DriveRate.King:
                        trackingMode = TrackingMode.King;
                        break;

                    case DriveRate.Lunar:
                        trackingMode = TrackingMode.Lunar;
                        break;

                    case DriveRate.Solar:
                        trackingMode = TrackingMode.Solar;
                        break;
                }
                return new TrackingRate() { TrackingMode = trackingMode };
            }
        }

        public TrackingMode TrackingMode {
            get => TrackingRate.TrackingMode;
            set {
                if (value == TrackingMode.Custom) {
                    throw new ArgumentException("TrackingMode cannot be set to Custom. Use SetCustomTrackingRate");
                }

                if (ShouldBeConnected && device.CanSetTracking) {
                    try {
                        // Set the mode regardless of whether it is the same as what is currently set
                        // Some ASCOM drivers incorrectly report custom rates as Sidereal, and this can help force set the tracking mode to the desired value
                        var currentTrackingMode = TrackingRate.TrackingMode;
                        try {
                            switch (value) {
                                case TrackingMode.Sidereal:
                                    device.TrackingRate = DriveRate.Sidereal;
                                    break;

                                case TrackingMode.Lunar:
                                    device.TrackingRate = DriveRate.Lunar;
                                    break;

                                case TrackingMode.Solar:
                                    device.TrackingRate = DriveRate.Solar;
                                    break;

                                case TrackingMode.King:
                                    device.TrackingRate = DriveRate.King;
                                    break;
                            }
                        } catch (ASCOM.NotImplementedException pnie) {
                            // TrackingRate Write can throw a PropertyNotImplementedException.
                            Logger.Debug(pnie.Message);
                        }
                        device.Tracking = (value != TrackingMode.Stopped);

                        if (currentTrackingMode != value) {
                            RaisePropertyChanged();
                            RaisePropertyChanged(nameof(TrackingRate));
                            RaisePropertyChanged(nameof(TrackingEnabled));
                        }
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowExternalError(ex.Message, Loc.Instance["LblASCOMDriverError"]);
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
                    this.device.TrackingRate = DriveRate.Sidereal;
                } catch (ASCOM.NotImplementedException pnie) {
                    // TrackingRate Write can throw a PropertyNotImplementedException.
                    Logger.Debug(pnie.Message);
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

        protected override Task PostConnect() {
            Initialize();
            EquatorialSystem = DetermineEquatorialSystem();
            trackingModes = GetTrackingModes();
            CheckMountTime();

            return Task.CompletedTask;
        }

        protected override ITelescopeV4 GetInstance() {
            if (!IsAlpacaDevice()) {
                return new Telescope(Id);
            } else {
                return new ASCOM.Alpaca.Clients.AlpacaTelescope(deviceMeta.ServiceType, deviceMeta.IpAddress, deviceMeta.IpPort, deviceMeta.AlpacaDeviceNumber, false, null);
            }
        }

        public PierSide DestinationSideOfPier(Coordinates coordinates) {
            coordinates = coordinates.Transform(EquatorialSystem);
            var pierSide = device.DestinationSideOfPier(coordinates.RA, coordinates.Dec);
            return (PierSide)pierSide;
        }
    }
}