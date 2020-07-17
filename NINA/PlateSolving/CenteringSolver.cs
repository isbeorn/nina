#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal class CenteringSolver : ICenteringSolver {
        private ITelescopeMediator telescopeMediator;

        public CenteringSolver(IPlateSolver plateSolver,
                IPlateSolver blindSolver,
                IImagingMediator imagingMediator,
                ITelescopeMediator telescopeMediator) {
            this.telescopeMediator = telescopeMediator;
            this.CaptureSolver = new CaptureSolver(plateSolver, blindSolver, imagingMediator);
        }

        public ICaptureSolver CaptureSolver { get; set; }

        public async Task<PlateSolveResult> Center(CaptureSequence seq, CenterSolveParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (parameter?.Coordinates == null) { throw new ArgumentException(nameof(CenterSolveParameter.Coordinates)); }
            if (parameter?.Threshold <= 0) { throw new ArgumentException(nameof(CenterSolveParameter.Threshold)); }

            var centered = false;
            PlateSolveResult result;
            Separation offset = new Separation();
            do {
                result = await CaptureSolver.Solve(seq, parameter, solveProgress, progress, ct);

                if (result.Success == false) {
                    //Solving failed. Give up.
                    break;
                }

                var position = (telescopeMediator.GetCurrentPosition() - offset).Transform(result.Coordinates.Epoch);
                result.Separation = result.DetermineSeparation(position);

                Logger.Debug($"Centering Solver - Scope Position: {position}; Centering Coordinates: {parameter.Coordinates}; Solve Result: {result.Coordinates}; Separation {result.Separation}");

                solveProgress?.Report(new PlateSolveProgress() { PlateSolveResult = result });

                if (Math.Abs(result.Separation.Distance.ArcMinutes) > parameter.Threshold) {
                    progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveNotInsideToleranceSyncing"] });
                    if (parameter.NoSync || !await telescopeMediator.Sync(result.Coordinates)) {
                        offset = result.DetermineSeparation(position + offset);

                        Logger.Debug($"Sync failed - calculating offset instead to compensate.  Original: {position.Transform(result.Coordinates.Epoch)}; Solved: {result.Coordinates}; Offset: {offset}");
                        progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveSyncViaTargetOffset"] });
                    } else {
                        var positionAfterSync = telescopeMediator.GetCurrentPosition().Transform(result.Coordinates.Epoch);

                        if (Astrometry.DegreeToArcsec(Math.Abs(positionAfterSync.RADegrees - result.Coordinates.RADegrees)) > 1
                            || Astrometry.DegreeToArcsec(Math.Abs(positionAfterSync.Dec - result.Coordinates.Dec)) > 1) {
                            offset = result.DetermineSeparation(positionAfterSync);
                            Logger.Debug($"Sync failed silently - calculating offset instead to compensate.  Original: {positionAfterSync}; Solved: {result.Coordinates}; Offset: {offset}");
                        } else {
                            // Sync worked - reset offset
                            Logger.Debug("Synced sucessfully");
                            offset = new Separation();
                        }
                    }

                    Logger.Trace($"Slewing to target after sync. Target coordinates RA: {parameter.Coordinates.RAString} Dec: {parameter.Coordinates.DecString} Epoch: {parameter.Coordinates.Epoch}");
                    progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveNotInsideToleranceReslew"] });

                    await telescopeMediator.SlewToCoordinatesAsync(parameter.Coordinates + offset);

                    progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveNotInsideToleranceRepeating"] });
                } else {
                    centered = true;
                }
            } while (!centered);
            return result;
        }
    }
}