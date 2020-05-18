#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
