#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Model;
using NINA.PlateSolving.Interfaces;
using NINA.Core.Locale;

namespace NINA.PlateSolving {

    public class CaptureSolver : ICaptureSolver {
        private IImagingMediator imagingMediator;
        private IFilterWheelMediator filterWheelMediator;

        public CaptureSolver(IPlateSolver plateSolver,
                IPlateSolver blindSolver,
                IImagingMediator imagingMediator,
                IFilterWheelMediator filterWheelMediator) {
            this.imagingMediator = imagingMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.ImageSolver = new ImageSolver(plateSolver, blindSolver);
        }

        public IImageSolver ImageSolver { get; set; }

        public async Task<PlateSolveResult> Solve(CaptureSequence seq, CaptureSolverParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            var remainingAttempts = parameter.Attempts;
            PlateSolveResult plateSolveResult;
            do {
                remainingAttempts--;
                var oldFilter = filterWheelMediator.GetInfo()?.SelectedFilter;
                try {
                    progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblCameraStateExposing"] });
                    var renderedImage = await imagingMediator.CaptureAndPrepareImage(seq, new PrepareImageParameters(detectStars: false), ct, progress);
                    progress?.Report(new ApplicationStatus() { Status = string.Empty });

                    if (renderedImage != null) {
                        solveProgress?.Report(
                            new PlateSolveProgress {
                                Thumbnail = await renderedImage.GetThumbnail()
                            }
                        );

                        ct.ThrowIfCancellationRequested();

                        plateSolveResult = await ImageSolver.Solve(renderedImage.RawImageData, parameter, progress, ct);
                    } else {
                        plateSolveResult = new PlateSolveResult() { Success = false };
                    }

                    solveProgress?.Report(
                        new PlateSolveProgress {
                            PlateSolveResult = plateSolveResult
                        }
                    );
                } finally {
                    progress?.Report(new ApplicationStatus() { Status = string.Empty });
                    if (oldFilter != null) {
                        Logger.Info($"Restoring filter to {oldFilter} after capture solve attempt");
                        var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                        await filterWheelMediator.ChangeFilter(oldFilter, timeoutCts.Token, progress);
                    }
                }

                if (!plateSolveResult.Success && remainingAttempts > 0) {
                    await CoreUtil.Wait(parameter.ReattemptDelay, true, ct, progress, "");
                }
            } while (!plateSolveResult.Success && remainingAttempts > 0);
            return plateSolveResult;
        }
    }
}
