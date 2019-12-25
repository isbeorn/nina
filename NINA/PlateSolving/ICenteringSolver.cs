using NINA.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal interface ICenteringSolver {
        ICaptureSolver CaptureSolver { get; set; }

        Task<PlateSolveResult> Center(CaptureSequence seq, CenterSolveParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct);
    }
}