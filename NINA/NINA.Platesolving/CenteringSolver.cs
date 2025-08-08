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
using System.Collections.Generic;

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

        public async Task<CenteringSolveResult> CenterWithMeasurements(CaptureSequence seq, CenterSolveParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return await Center(seq, parameter, solveProgress, progress, ct) as CenteringSolveResult;
        }

        public async Task<PlateSolveResult> Center(CaptureSequence seq, CenterSolveParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (parameter?.Coordinates == null) { throw new ArgumentException(nameof(CenterSolveParameter.Coordinates)); }
            if (parameter?.Threshold <= 0) { throw new ArgumentException(nameof(CenterSolveParameter.Threshold)); }

            var startTime = DateTimeOffset.UtcNow;
            FilterInfo oldFilter = null;
            if (seq.FilterType != null) {
                oldFilter = filterWheelMediator.GetInfo()?.SelectedFilter;
                await filterWheelMediator.ChangeFilter(seq.FilterType, ct, progress);
            }

            try {
                List<CenteringMeasurement> centeringAttempts = new List<CenteringMeasurement>();
                var centered = false;
                var maxSlewAttempts = 10;
                PlateSolveResult result;
                Separation offset = new Separation();
                do {
                    var centeringAttempt = new CenteringMeasurement();
                    centeringAttempts.Add(centeringAttempt);

                    maxSlewAttempts--;

                    var solveMeasurement = new Measurement("CaptureAndSolve").Start();
                    result = await CaptureSolver.Solve(seq, parameter, solveProgress, progress, ct);
                    centeringAttempt.AddSolveResult(result);
                    centeringAttempt.AddSubMeasurement(solveMeasurement.Stop());

                    if (result.Success == false) {
                        centeringAttempt.Stop();
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
                        var syncMeasurement = new Measurement("Sync").Start();

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

                            centeringAttempt.AddSubMeasurement(syncMeasurement.Stop());
                        }

                        var scopePosition = telescopeMediator.GetCurrentPosition();
                        Logger.Info($"Slewing to target after sync. Current Position: {scopePosition}; Target coordinates: {parameterCoordinates}; Offset {offset}");
                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceReslew"] });

                        var slewMeasurement = new Measurement("Reslew").Start();
                        await telescopeMediator.SlewToCoordinatesAsync(parameterCoordinates + offset, ct);
                        slewMeasurement.Stop();
                        centeringAttempt.AddSubMeasurement(slewMeasurement);

                        var domeInfo = domeMediator.GetInfo();
                        if (domeInfo.Connected && domeInfo.CanSetAzimuth && !domeFollower.IsFollowing) {
                            var domeSyncMeasurement = new Measurement("DomeSync").Start();
                            progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblSynchronizingDome"] });
                            Logger.Info($"Centering Solver - Synchronize dome to scope since dome following is not enabled");
                            if (!await domeFollower.TriggerTelescopeSync()) {
                                Notification.ShowWarning(Loc.Instance["LblDomeSyncFailureDuringCentering"]);
                                Logger.Warning("Centering Solver - Synchronize dome operation didn't complete successfully. Moving on");
                            }
                            centeringAttempt.AddSubMeasurement(domeSyncMeasurement.Stop());
                        }

                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceRepeating"] });
                    } else {
                        centered = true;
                    }
                    centeringAttempt.Stop();
                } while (!centered && maxSlewAttempts > 0);
                if (!centered && maxSlewAttempts <= 0) {
                    result.Success = false;
                    Logger.Error("Cancelling centering after 10 unsuccessful slew attempts");
                }
                return new CenteringSolveResult(result, seq, startTime, DateTimeOffset.UtcNow, centeringAttempts);
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

    public class CenteringSolveResult : PlateSolveResult {
        private List<CenteringMeasurement> attempts;
        public CenteringSolveResult(PlateSolveResult result, CaptureSequence captureSequence, DateTimeOffset startTime, DateTimeOffset endTime, List<CenteringMeasurement> attempts) : base(result.SolveTime) {
            this.Coordinates = result.Coordinates;
            this.Flipped = result.Flipped;
            this.Pixscale = result.Pixscale;
            this.PositionAngle = result.PositionAngle;
            this.Radius = result.Radius;
            this.Separation = result.Separation;
            this.Success = result.Success;

            this.StartTime = startTime;
            this.EndTime = endTime;
            this.CaptureSequence = captureSequence;
            this.attempts = attempts;
        }

        public IReadOnlyCollection<CenteringMeasurement> Attempts { get => attempts.AsReadOnly(); }
        public CaptureSequence CaptureSequence { get; set; }
        public DateTimeOffset StartTime { get; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }

    public class CenteringMeasurement : Measurement {
        public CenteringMeasurement() : base("Centering") {
        }

        public PlateSolveResult PlateSolveResult { get; private set; }

        public void AddSolveResult(PlateSolveResult result) {
            PlateSolveResult = result;
        }
    }
}