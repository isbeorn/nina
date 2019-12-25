using NINA.Model;
using NINA.Model.ImageData;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {
    public interface IImageSolver {
        Task<PlateSolveResult> Solve(IImageData source, PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken ct);
    }
}