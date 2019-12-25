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
            do {
                result = await CaptureSolver.Solve(seq, parameter, solveProgress, progress, ct);

                if (result.Success == false) {
                    //Solving failed. Give up.
                    break;
                }

                result.DetermineSeparation(telescopeMediator.GetCurrentPosition());

                solveProgress?.Report(new PlateSolveProgress() { PlateSolveResult = result });

                if (Math.Abs(result.Separation.Distance.ArcMinutes) > parameter.Threshold) {
                    progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveNotInsideToleranceSyncing"] });
                    if (!telescopeMediator.Sync(result.Coordinates)) {
                        Logger.Warning("Sync to coordinates failed");
                        Notification.ShowWarning(Locale.Loc.Instance["LblSyncFailed"]);
                    }

                    Logger.Trace($"Slewing to target after sync. Target coordinates RA: {parameter.Coordinates.RAString} Dec: {parameter.Coordinates.DecString} Epoch: {parameter.Coordinates.Epoch}");
                    progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveNotInsideToleranceReslew"] });
                    await telescopeMediator.SlewToCoordinatesAsync(parameter.Coordinates);
                    progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolveNotInsideToleranceRepeating"] });
                } else {
                    centered = true;
                }
            } while (!centered);
            return result;
        }
    }
}