#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NINA.Model;
using NINA.Model.ImageData;

namespace NINA.PlateSolving {

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