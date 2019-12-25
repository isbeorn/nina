using NINA.Model;
using NINA.Model.ImageData;
using NINA.Profile;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    public class ImageSolver : IImageSolver {

        public ImageSolver(IPlateSolver plateSolver, IPlateSolver blindSolver) {
            this.plateSolver = plateSolver;
            this.blindSolver = blindSolver;
        }

        public async Task<PlateSolveResult> Solve(IImageData source, PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            ValidatePrerequisites(parameter);
            var solver = GetSolver(parameter);

            Logger.Trace($"Solving with parameters: {Environment.NewLine + parameter.ToString()}");
            progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlateSolving"] });

            var result = await solver.SolveAsync(source, parameter, progress, ct);
            if (result.Success == false && parameter.Coordinates != null) {
                //Blind solve failover
                var blindParameter = parameter.Clone();
                blindParameter.Coordinates = null;
                result = await Solve(source, blindParameter, progress, ct);
            }

            progress?.Report(new ApplicationStatus() { Status = string.Empty });

            return result;
        }

        protected IProfileService profileService;
        private IPlateSolver plateSolver;
        private IPlateSolver blindSolver;

        protected IPlateSolver GetSolver(PlateSolveParameter parameter) {
            if (parameter.Coordinates == null) {
                return blindSolver;
            } else {
                return plateSolver;
            }
        }

        /// <summary>
        /// Validates general prerequisites that need to be set up to use the plate solvers
        /// </summary>
        protected void ValidatePrerequisites(PlateSolveParameter parameter) {
            if (parameter == null) {
                throw new ArgumentNullException(nameof(PlateSolveParameter));
            }

            double focalLength = parameter.FocalLength;

            // Check to make sure user has supplied the telescope's effective focal length (in mm)
            if (double.IsNaN(focalLength) || focalLength <= 0) {
                throw new Exception(Locale.Loc.Instance["LblPlateSolveNoFocalLength"]);
            }
        }
    }
}