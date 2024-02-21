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
using NINA.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.Equipment.Model;
using NINA.PlateSolving.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.Core.Utility.Notification;
using NINA.Core.Model.Equipment;

namespace NINA.PlateSolving {

    public class CenteringSolver : ICenteringSolver {
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeFollower domeFollower;

        public CenteringSolver(IPlateSolver plateSolver,
                IPlateSolver blindSolver,
                IImagingMediator imagingMediator,
                ITelescopeMediator telescopeMediator,
                IFilterWheelMediator filterWheelMediator,
                IDomeMediator domeMediator,
                IDomeFollower domeFollower) {
            this.telescopeMediator = telescopeMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.filterWheelMediator = filterWheelMediator;
            this.CaptureSolver = new CaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator);
        }

        public ICaptureSolver CaptureSolver { get; set; }

        public async Task<PlateSolveResult> Center(CaptureSequence seq, CenterSolveParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (parameter?.Coordinates == null) { throw new ArgumentException(nameof(CenterSolveParameter.Coordinates)); }
            if (parameter?.Threshold <= 0) { throw new ArgumentException(nameof(CenterSolveParameter.Threshold)); }

            FilterInfo oldFilter = null;
            if (seq.FilterType != null) {
                oldFilter = filterWheelMediator.GetInfo()?.SelectedFilter;
                await filterWheelMediator.ChangeFilter(seq.FilterType, ct, progress);
            }

            try {
                var centered = false;
                var maxSlewAttempts = 10;
                PlateSolveResult result;
                Separation offset = new Separation();
                do {
                    maxSlewAttempts--;
                                        
                    result = await CaptureSolver.Solve(seq, parameter, solveProgress, progress, ct);

                    if (result.Success == false) {
                        //Solving failed. Give up.
                        break;
                    }

                    // All coordinates need to be in the same epoch as the scope for offsets to correctly be calculated
                    var position = telescopeMediator.GetCurrentPosition();
                    var resultCoordinates = result.Coordinates.Transform(position.Epoch);
                    var parameterCoordinates = parameter.Coordinates.Transform(position.Epoch);
                    result.Separation = parameterCoordinates - resultCoordinates;

                    var positionWithOffset = position - offset;
                    Logger.Info($"Centering Solver - Scope Position: {position}; Offset: {offset}; Centering Coordinates: {parameterCoordinates}; Solved: {resultCoordinates}; Separation {result.Separation}; Threshold: {parameter.Threshold}");

                    solveProgress?.Report(new PlateSolveProgress() { PlateSolveResult = result });

                    if (Math.Abs(result.Separation.Distance.ArcMinutes) > parameter.Threshold) {
                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceSyncing"] });
                        if (parameter.NoSync || !await telescopeMediator.Sync(resultCoordinates)) {
                            var oldOffset = offset;
                            offset = position - resultCoordinates;

                            Logger.Info($"Sync {(parameter.NoSync ? "disabled" : "failed")} - calculating offset instead to compensate.  Original: {positionWithOffset}; Original Offset {oldOffset}; Solved: {resultCoordinates}; New Offset: {offset}");
                        } else {
                            var positionAfterSync = telescopeMediator.GetCurrentPosition();

                        // If Sync affects the scope position by at least 1 arcsecond, then continue iterating without
                        // using an offset
                        var syncEffect = positionAfterSync - position;
                        if (Math.Abs(syncEffect.Distance.ArcSeconds) < 1.0d) {
                            var syncDistance = positionAfterSync - resultCoordinates;
                            offset = syncDistance;
                            Logger.Warning($"Sync failed silently - calculating offset instead to compensate.  Position after sync: {positionAfterSync}; Solved: {resultCoordinates}; New Offset: {offset}");
                        } else {
                            // Sync worked - reset offset
                            Logger.Debug($"Synced sucessfully. Position after sync: {positionAfterSync}");
                            offset = new Separation();
                        }
                    }

                        var scopePosition = telescopeMediator.GetCurrentPosition();
                        Logger.Info($"Slewing to target after sync. Current Position: {scopePosition}; Target coordinates: {parameterCoordinates}; Offset {offset}");
                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceReslew"] });

                        await telescopeMediator.SlewToCoordinatesAsync(parameterCoordinates + offset, ct);
                        var domeInfo = domeMediator.GetInfo();
                        if (domeInfo.Connected && domeInfo.CanSetAzimuth && !domeFollower.IsFollowing) {
                            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSynchronizingDome"] });
                            Logger.Info($"Centering Solver - Synchronize dome to scope since dome following is not enabled");
                            if (!await domeFollower.TriggerTelescopeSync()) {
                                Notification.ShowWarning(Loc.Instance["LblDomeSyncFailureDuringCentering"]);
                                Logger.Warning("Centering Solver - Synchronize dome operation didn't complete successfully. Moving on");
                            }
                        }

                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceRepeating"] });
                    } else {
                        centered = true;
                    }
                } while (!centered && maxSlewAttempts > 0);
                if (!centered && maxSlewAttempts <= 0) {
                    result.Success = false;
                    Logger.Error("Cancelling centering after 10 unsuccessful slew attempts");
                }
                return result;
            } finally {
                if (oldFilter != null) {
                    Logger.Info($"Restoring filter to {oldFilter} after centering");

                    // Set an absurdly high timeout, but at least make sure that this cannot go on forever. The existing token may have been cancelled already, so we need
                    // to use a new one
                    var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                    await filterWheelMediator.ChangeFilter(oldFilter, timeoutCts.Token, progress);
                }
            }
        }
    }
}