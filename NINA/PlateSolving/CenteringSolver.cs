#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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

                var position = telescopeMediator.GetCurrentPosition() - offset;
                result.Separation = result.DetermineSeparation(position);

                solveProgress?.Report(new PlateSolveProgress() { PlateSolveResult = result });

                if (Math.Abs(result.Separation.Distance.ArcMinutes) > parameter.Threshold) {
                    progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveNotInsideToleranceSyncing"] });
                    if (parameter.NoSync || !telescopeMediator.Sync(result.Coordinates)) {
                        offset = result.DetermineSeparation(position + offset);

                        Logger.Debug($"Sync failed - calculating offset instead to compensate.  Original: {position.Transform(Epoch.J2000)}; Solved: {result.Coordinates}; Offset: {offset}");
                        progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveSyncViaTargetOffset"] });
                    } else {
                        var positionAfterSync = telescopeMediator.GetCurrentPosition().Transform(result.Coordinates.Epoch);

                        if (Astrometry.DegreeToArcsec(Math.Abs(positionAfterSync.RADegrees - result.Coordinates.RADegrees)) > 1
                            || Astrometry.DegreeToArcsec(Math.Abs(positionAfterSync.Dec - result.Coordinates.Dec)) > 1) {
                            offset = result.DetermineSeparation(positionAfterSync);
                            Logger.Debug($"Sync failed silently - calculating offset instead to compensate.  Original: {positionAfterSync.Transform(Epoch.J2000)}; Solved: {result.Coordinates}; Offset: {offset}");
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