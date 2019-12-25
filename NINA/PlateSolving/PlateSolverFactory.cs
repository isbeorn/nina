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

using NINA.Utility.Enum;
using NINA.Profile;
using System;
using NINA.PlateSolving.Solvers;

namespace NINA.PlateSolving {

    internal static class PlateSolverFactory {

        /// <summary>
        /// Creates an instance of a Platesolver depending on the solver
        /// </summary>
        /// <param name="plateSolveSettings"></param>
        /// <param name="solver"> Plate Solver that should be used</param>
        /// <returns></returns>
        private static IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings, PlateSolverEnum solver) {
            switch (solver) {
                case PlateSolverEnum.ASTROMETRY_NET:
                    return new AstrometryPlateSolver(plateSolveSettings.AstrometryURL, plateSolveSettings.AstrometryAPIKey);

                case PlateSolverEnum.LOCAL:
                    return new LocalPlateSolver(plateSolveSettings.CygwinLocation);

                case PlateSolverEnum.PLATESOLVE2:
                    return new Platesolve2Solver(plateSolveSettings.PS2Location);

                case PlateSolverEnum.ASPS:
                    return new AllSkyPlateSolver(plateSolveSettings.AspsLocation);

                default:
                    return new ASTAPSolver(plateSolveSettings.ASTAPLocation);
            }
        }

        public static IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings) {
            return GetPlateSolver(plateSolveSettings, plateSolveSettings.PlateSolverType);
        }

        public static IPlateSolver GetBlindSolver(IPlateSolveSettings plateSolveSettings) {
            var type = PlateSolverEnum.ASTAP;
            if (plateSolveSettings.BlindSolverType == BlindSolverEnum.LOCAL) {
                type = PlateSolverEnum.LOCAL;
            } else if (plateSolveSettings.BlindSolverType == BlindSolverEnum.ASPS) {
                type = PlateSolverEnum.ASPS;
            } else if (plateSolveSettings.BlindSolverType == BlindSolverEnum.ASTROMETRY_NET) {
                type = PlateSolverEnum.ASTROMETRY_NET;
            }

            return GetPlateSolver(plateSolveSettings, type);
        }

        internal static IPlateSolver GetBlindSolver(object plateSolveSettings) {
            throw new NotImplementedException();
        }
    }
}