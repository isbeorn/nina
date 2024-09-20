#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces;
using NINA.Core.Locale;
using NINA.Equipment.Equipment;

namespace NINA.WPF.Base.ViewModel.Equipment.Dome {

    public class DomeFollower : BaseINPC, IDomeFollower, ITelescopeConsumer, IDomeConsumer {
        private const double RA_DEC_WARN_THRESHOLD = 2.0;
        private readonly IProfileService profileService;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeSynchronization domeSynchronization;
        private Task domeFollowerTask;
        private CancellationTokenSource domeFollowerTaskCTS;
        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
        private DomeInfo domeInfo = DeviceInfo.CreateDefaultInstance<DomeInfo>();
        private Task<bool> domeRotationTask = Task.FromResult(true);
        private CancellationTokenSource domeRotationCTS;

        public DomeFollower(
            IProfileService profileService,
            ITelescopeMediator telescopeMediator,
            IDomeMediator domeMediator,
            IDomeSynchronization domeSynchronization) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.domeMediator = domeMediator;
            this.domeMediator.RegisterConsumer(this);
            this.domeSynchronization = domeSynchronization;
        }

        public void Dispose() {
            this.telescopeMediator?.RemoveConsumer(this);
            this.domeMediator?.RemoveConsumer(this);
        }

        public async Task Start() {
            if (domeFollowerTask != null) {
                throw new InvalidOperationException("Dome follower is running already");
            }

            IsFollowing = true;
            domeFollowerTask = Task.Run(async () => {
                domeFollowerTaskCTS?.Dispose();
                domeFollowerTaskCTS = new CancellationTokenSource();
                try {
                    do {
                        if (!this.IsSynchronized) {
                            await this.TriggerTelescopeSync();
                        }

                        await Task.Delay(TimeSpan.FromSeconds(2), domeFollowerTaskCTS.Token);
                    } while (true);
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Loc.Instance["LblDomeFollowError"]);
                } finally {
                    IsFollowing = false;
                }
            });
            await domeFollowerTask;
            domeFollowerTask = null;
        }

        public async Task Stop() {
            try { domeFollowerTaskCTS?.Cancel(); } catch { }
            try { domeRotationCTS?.Cancel(); } catch { }
            while (domeFollowerTask?.IsCompleted == false) {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            domeFollowerTask = null;
            domeRotationCTS = null;
        }

        public async Task WaitForDomeSynchronization(CancellationToken cancellationToken) {
            if (!IsFollowing || IsSynchronized) {
                return;
            }
            var timeoutTime = TimeSpan.FromSeconds(profileService.ActiveProfile.DomeSettings.DomeSyncTimeoutSeconds);
            using var timeoutCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCTS.CancelAfter(timeoutTime);
            var timeoutOrClientCancellationToken = timeoutCTS.Token;

            Logger.Info("Waiting for dome synchronization");
            try {
                while (IsFollowing && !IsSynchronized) {
                    timeoutOrClientCancellationToken.ThrowIfCancellationRequested();
                    Logger.Trace("Dome not synchronized. Waiting...");
                    await Task.Delay(TimeSpan.FromSeconds(1), timeoutOrClientCancellationToken);
                }
                Logger.Info("Dome synchronized");
            } catch (OperationCanceledException) {
                if (cancellationToken.IsCancellationRequested != true) {
                    Logger.Error($"Dome synchronization has timed out after {timeoutTime.TotalMinutes} minutes");
                    Notification.ShowError(Loc.Instance["LblDomeSyncError_SyncTimeout"]);
                } else {
                    Logger.Info("Waiting for dome synchronization cancelled");
                }
            }
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            this.telescopeInfo = deviceInfo;
            if (!IsFollowing) {
                return;
            }

            if (!this.telescopeInfo.Connected) {
                IsFollowing = false;
                return;
            }

            try {
                var calculatedTargetDomeCoordinates = GetSynchronizedDomeCoordinates(this.telescopeInfo);
                if(calculatedTargetDomeCoordinates != null) { 
                    var currentAzimuth = Angle.ByDegree(this.domeInfo.Azimuth);
                    this.IsSynchronized = IsDomeWithinTolerance(currentAzimuth, calculatedTargetDomeCoordinates);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblDomeFollowError"]);
                IsFollowing = false;
            }
        }

        public void UpdateDeviceInfo(DomeInfo deviceInfo) {
            this.domeInfo = deviceInfo;
            if (!IsFollowing) {
                return;
            }

            if (this.domeInfo.DriverCanFollow && this.domeInfo.DriverFollowing) {
                Notification.ShowError(Loc.Instance["LblDomeFollowError_DriverFollowing"]);
                IsFollowing = false;
                return;
            }

            if (!this.domeInfo.Connected) {
                IsFollowing = false;
                return;
            }
        }

        private bool CanSyncDome() {
            if (!domeInfo.Connected || !telescopeInfo.Connected) {
                return false;
            }

            var isLastSyncStillRotating = domeRotationTask?.IsCompleted == false;
            if (isLastSyncStillRotating) {
                return false;
            }

            if (domeInfo.Slewing) {
                Logger.Trace("Cannot synchronize with telescope while dome is slewing");
                return false;
            }

            // If TargetCoordinates is not null then NINA initiated the slew and we know the target coordinates
            if (telescopeInfo.Slewing && telescopeInfo.TargetCoordinates == null && !profileService.ActiveProfile.DomeSettings.SynchronizeDuringMountSlew) {
                Logger.Info("Will not synchronize telescope while it is slewing since SynchronizeDuringMountSlew is disabled");
                return false;
            }
            return true;
        }

        public async Task<bool> TriggerTelescopeSync() {
            await WaitForPreviousSlew();

            if (!CanSyncDome()) {
                Logger.Warning("Cannot sync dome at this time");
                return false;
            }

            var calculatedTargetDomeCoordinates = GetSynchronizedDomeCoordinates(telescopeInfo);
            if(calculatedTargetDomeCoordinates != null) { 
                return await SyncToDomeAzimuth(calculatedTargetDomeCoordinates, CancellationToken.None);
            } else {
                return false;
            }
        }

        private async Task WaitForPreviousSlew() {
            if (domeInfo.Slewing || domeRotationTask?.IsCompleted == false) {
                Logger.Info("Dome is already slewing. Waiting for previous slew to finish");
                await (domeRotationTask ?? Task.CompletedTask);
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5))) {
                    try {
                        while (domeInfo.Slewing) {
                            await Task.Delay(TimeSpan.FromSeconds(profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval), cts.Token);
                        }
                    } catch (OperationCanceledException) { }
                }
            }
        }

        public async Task<bool> SyncToScopeCoordinates(Coordinates coordinates, PierSide sideOfPier, CancellationToken cancellationToken) {
            await WaitForPreviousSlew();

            if (!CanSyncDome()) {
                Logger.Warning("Cannot sync dome at this time");
                return false;
            }

            var calculatedTargetDomeCoordinates = GetSynchronizedDomeCoordinates(coordinates, sideOfPier);
            return await SyncToDomeAzimuth(calculatedTargetDomeCoordinates, cancellationToken);
        }

        public bool IsDomeWithinTolerance(Angle currentDomeAzimuth, TopocentricCoordinates targetDomeCoordinates) {
            var tolerance = Angle.ByDegree(this.profileService.ActiveProfile.DomeSettings.AzimuthTolerance_degrees);
            return targetDomeCoordinates.Azimuth.Equals(currentDomeAzimuth, tolerance);
        }

        public TopocentricCoordinates GetSynchronizedDomeCoordinates(TelescopeInfo telescopeInfo) {
            var targetCoordinates = telescopeInfo.TargetCoordinates ?? telescopeInfo.Coordinates;
            if (targetCoordinates == null) { return null; }
            PierSide targetSideOfPier = PierSide.pierUnknown;
            if (this.profileService.ActiveProfile.MeridianFlipSettings.UseSideOfPier) {
                targetSideOfPier = telescopeInfo.TargetSideOfPier ?? telescopeInfo.SideOfPier;
            }
            if (targetSideOfPier == PierSide.pierUnknown) {
                targetSideOfPier = MeridianFlip.ExpectedPierSide(targetCoordinates, Angle.ByHours(telescopeInfo.SiderealTime));
            }
            Logger.Trace($"Using {targetCoordinates} on {targetSideOfPier} side of pier for dome calculations");
            return GetSynchronizedDomeCoordinates(targetCoordinates, targetSideOfPier);
        }

        private TopocentricCoordinates GetSynchronizedDomeCoordinates(Coordinates targetCoordinates, PierSide targetSideOfPier) {
            var targetDomeCoordinates = domeSynchronization.TargetDomeCoordinates(
                scopeCoordinates: targetCoordinates,
                localSiderealTime: telescopeInfo.SiderealTime,
                siteLatitude: Angle.ByDegree(telescopeInfo.SiteLatitude),
                siteLongitude: Angle.ByDegree(telescopeInfo.SiteLongitude),
                sideOfPier: targetSideOfPier);
            return targetDomeCoordinates;
        }

        private Task<bool> SyncToDomeAzimuth(TopocentricCoordinates calculatedTargetDomeCoordinates, CancellationToken cancellationToken) {
            var currentAzimuth = Angle.ByDegree(domeInfo.Azimuth);
            if (!IsDomeWithinTolerance(currentAzimuth, calculatedTargetDomeCoordinates)) {
                Logger.Trace($"Dome direct telescope follow slew. Current azimuth={currentAzimuth}, Target azimuth={calculatedTargetDomeCoordinates.Azimuth}, Target altitude={calculatedTargetDomeCoordinates.Altitude}");
                IsSynchronized = false;
                domeRotationCTS = new CancellationTokenSource();
                domeRotationTask = this.domeMediator.SlewToAzimuth(calculatedTargetDomeCoordinates.Azimuth.Degree, domeRotationCTS.Token);
                return domeRotationTask;
            }
            return Task.FromResult(true);
        }

        private bool isSynchronized = false;

        public bool IsSynchronized {
            get => this.isSynchronized;
            private set {
                if (this.isSynchronized != value) {
                    this.isSynchronized = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool following = false;

        public bool IsFollowing {
            get => this.following;
            private set {
                if (this.following != value) {
                    this.following = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}