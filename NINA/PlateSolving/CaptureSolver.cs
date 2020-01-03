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