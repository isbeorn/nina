using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.PlateSolving {

    internal class CaptureSolver : ICaptureSolver {
        private IImagingMediator imagingMediator;

        public CaptureSolver(IPlateSolver plateSolver,
                IPlateSolver blindSolver,
                IImagingMediator imagingMediator) {
            this.imagingMediator = imagingMediator;
            this.ImageSolver = new ImageSolver(plateSolver, blindSolver);
        }

        public IImageSolver ImageSolver { get; set; }

        public async Task<PlateSolveResult> Solve(CaptureSequence seq, CaptureSolverParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var remainingAttempts = parameter.Attempts;
            PlateSolveResult plateSolveResult;
            do {
                remainingAttempts--;
                var renderedImage = await imagingMediator.CaptureAndPrepareImage(seq, new PrepareImageParameters(), ct, progress);

                solveProgress?.Report(
                    new PlateSolveProgress {
                        Thumbnail = await renderedImage.GetThumbnail()
                    }
                );

                ct.ThrowIfCancellationRequested();

                if (renderedImage != null) {
                    plateSolveResult = await ImageSolver.Solve(renderedImage.RawImageData, parameter, progress, ct);
                } else {
                    plateSolveResult = new PlateSolveResult() { Success = false };
                }

                solveProgress?.Report(
                    new PlateSolveProgress {
                        PlateSolveResult = plateSolveResult
                    }
                );

                if (!plateSolveResult.Success && remainingAttempts > 0) {
                    await Utility.Utility.Wait(parameter.ReattemptDelay, ct, progress);
                }
            } while (!plateSolveResult.Success && remainingAttempts > 0);
            return plateSolveResult;
        }
    }
}