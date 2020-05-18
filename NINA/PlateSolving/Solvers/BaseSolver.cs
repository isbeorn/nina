#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NINA.Model;
using NINA.Model.ImageData;

namespace NINA.PlateSolving.Solvers {

    internal abstract class BaseSolver : IPlateSolver {
        protected static string WORKING_DIRECTORY = Path.Combine(Utility.Utility.APPLICATIONTEMPPATH, "PlateSolver");

        public async Task<PlateSolveResult> SolveAsync(IImageData source, PlateSolveParameter parameter, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            EnsureSolverValid(parameter);
            var imageProperties = PlateSolveImageProperties.Create(parameter, source);
            return await SolveAsyncImpl(source, parameter, imageProperties, progress, canceltoken);
        }

        protected abstract Task<PlateSolveResult> SolveAsyncImpl(
            IImageData source,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties,
            IProgress<ApplicationStatus> progress,
            CancellationToken canceltoken);

        protected virtual void EnsureSolverValid(PlateSolveParameter parameter) {
        }
    }
}
