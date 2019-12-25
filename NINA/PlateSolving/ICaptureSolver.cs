using NINA.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    public interface ICaptureSolver {
        IImageSolver ImageSolver { get; set; }

        Task<PlateSolveResult> Solve(CaptureSequence seq, CaptureSolverParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct);
    }
}