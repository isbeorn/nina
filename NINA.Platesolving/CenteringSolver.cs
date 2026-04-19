#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

                    // All coordinates need to be in the same epoch as the scope for corrections to correctly be calculated
                    var position = telescopeMediator.GetCurrentPosition();
                    var resultCoordinates = result.Coordinates.Transform(position.Epoch);
                    var parameterCoordinates = parameter.Coordinates.Transform(position.Epoch);
                    result.Separation = parameterCoordinates - resultCoordinates;

                    Logger.Info($"Centering Solver - Scope Position: {position}; Centering Coordinates: {parameterCoordinates}; Solved: {resultCoordinates}; Separation {result.Separation}; Threshold: {parameter.Threshold}");

                    solveProgress?.Report(new PlateSolveProgress() { PlateSolveResult = result });

                    if (Math.Abs(result.Separation.Distance.ArcMinutes) > parameter.Threshold) {
                        var syncMeasurement = new Measurement("Sync").Start();
                        bool useMeasuredCorrection = false;

                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceSyncing"] });
                        if (parameter.NoSync || !await telescopeMediator.Sync(resultCoordinates)) {
                            useMeasuredCorrection = true;
                            Logger.Info($"Sync {(parameter.NoSync ? "disabled" : "failed")} - using measured centering correction instead. Reported: {position}; Solved: {resultCoordinates}");
                        } else {
                            var positionAfterSync = telescopeMediator.GetCurrentPosition();

                            // If Sync affects the scope position by at least 1 arcsecond, then continue iterating without
                            // applying an additional measured correction.
                            var syncEffect = positionAfterSync - position;
                            if (Math.Abs(syncEffect.Distance.ArcSeconds) < 1.0d) {
                                useMeasuredCorrection = true;
                                Logger.Warning($"Sync failed silently - using measured centering correction instead. Position after sync: {positionAfterSync}; Solved: {resultCoordinates}");
                            } else {
                                Logger.Debug($"Synced sucessfully. Position after sync: {positionAfterSync}");
                            }

                            centeringAttempt.AddSubMeasurement(syncMeasurement.Stop());
                        }

                        var scopePosition = telescopeMediator.GetCurrentPosition();
                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceReslew"] });

                        var slewMeasurement = new Measurement("Reslew").Start();
                        // The measured correction needs the mount-reported position and the solved sky position.
                        // Using the solve result for both sides would erase the mount-vs-sky error that no-sync fallback corrects.
                        Coordinates correctedTarget;
                        if (useMeasuredCorrection) {
                            correctedTarget = CalculateCorrectedTarget(parameterCoordinates, scopePosition, resultCoordinates);
                            Logger.Info($"Slewing to corrected target after sync fallback. Current Position: {scopePosition}; Target coordinates: {parameterCoordinates}; Solved: {resultCoordinates}; Corrected target: {correctedTarget}");
                        } else {
                            correctedTarget = parameterCoordinates;
                            Logger.Info($"Slewing to target after sync. Current Position: {scopePosition}; Target coordinates: {parameterCoordinates}; Solved: {resultCoordinates}");
                        }

                        bool slewSuccessful = await telescopeMediator.SlewToCoordinatesAsync(correctedTarget, ct);
                        slewMeasurement.Stop();
                        centeringAttempt.AddSubMeasurement(slewMeasurement);
                        if (!slewSuccessful) {
                            result.Success = false;
                            Logger.Error($"Centering correction slew failed. Current Position: {scopePosition}; Corrected Target: {correctedTarget}; Solved: {resultCoordinates}");
                            centeringAttempt.Stop();
                            break;
                        }

                        var domeInfo = domeMediator.GetInfo();
                        if (domeInfo.Connected && domeInfo.CanSetAzimuth && !domeFollower.IsFollowing) {
                            var domeSyncMeasurement = new Measurement("DomeSync").Start();
                            progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblSynchronizingDome"] });
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

        private static Coordinates CalculateCorrectedTarget(Coordinates targetCoordinates, Coordinates reportedCoordinates, Coordinates solvedCoordinates) {
            Coordinates target = targetCoordinates.Transform(reportedCoordinates.Epoch);
            Coordinates reported = reportedCoordinates.Transform(reportedCoordinates.Epoch);
            Coordinates solved = solvedCoordinates.Transform(reportedCoordinates.Epoch);

            RectangularCoordinates solvedVector = RectangularCoordinates.FromPolar(solved);
            RectangularCoordinates reportedVector = RectangularCoordinates.FromPolar(reported);
            RectangularCoordinates targetVector = RectangularCoordinates.FromPolar(target);

            RectangularCoordinates rotationAxis = solvedVector.Cross(reportedVector);
            double sinAngle = rotationAxis.Distance;
            double cosAngle = solvedVector.Dot(reportedVector);
            if (!IsFinite(sinAngle) || !IsFinite(cosAngle)) {
                Logger.Warning($"Centering correction rotation produced invalid values. Slewing to the requested target without measured correction. Reported: {reported}; Solved: {solved}");
                return target;
            }

            if (sinAngle < 1e-12) {
                if (cosAngle < 0) {
                    Logger.Warning($"Centering correction rotation is ambiguous for nearly opposite coordinates. Slewing to the requested target without measured correction. Reported: {reported}; Solved: {solved}");
                }

                return target;
            }

            RectangularCoordinates correctedVector = targetVector.RotateAroundAxis(rotationAxis / sinAngle, Angle.ByRadians(Math.Atan2(sinAngle, cosAngle)));
            Coordinates correctedTarget = correctedVector.ToPolar(target.Epoch);
            if (!IsFinite(correctedTarget.RADegrees) || !IsFinite(correctedTarget.Dec)) {
                Logger.Warning($"Centering correction produced invalid coordinates. Slewing to the requested target without measured correction. Target: {target}; Reported: {reported}; Solved: {solved}");
                return target;
            }

            return correctedTarget;
        }

        private static bool IsFinite(double value) {
            return !double.IsNaN(value) && !double.IsInfinity(value);
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
