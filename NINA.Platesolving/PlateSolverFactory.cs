#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.PlateSolving.Solvers;
using NINA.Core.Enum;
using NINA.PlateSolving.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;

namespace NINA.PlateSolving {

    public class PlateSolverFactoryProxy : IPlateSolverFactory {

        public IPlateSolver GetBlindSolver(IPlateSolveSettings plateSolveSettings) {
            return PlateSolverFactory.GetBlindSolver(plateSolveSettings);
        }

        public IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings) {
            return PlateSolverFactory.GetPlateSolver(plateSolveSettings);
        }

        public IImageSolver GetImageSolver(IPlateSolver plateSolver, IPlateSolver blindSolver) {
            return new ImageSolver(plateSolver, blindSolver);
        }

        public ICaptureSolver GetCaptureSolver(IPlateSolver plateSolver, IPlateSolver blindSolver, IImagingMediator imagingMediator, IFilterWheelMediator filterWheelMediator) {
            return new CaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator);
        }

        public ICenteringSolver GetCenteringSolver(IPlateSolver plateSolver, IPlateSolver blindSolver, IImagingMediator imagingMediator, ITelescopeMediator telescopeMediator, IFilterWheelMediator filterWheelMediator, IDomeMediator domeMediator, IDomeFollower domeFollower) {
            return new CenteringSolver(plateSolver, blindSolver, imagingMediator, telescopeMediator, filterWheelMediator, domeMediator, domeFollower);
        }
    }

    public static class PlateSolverFactory {

        /// <summary>
        /// Creates an instance of a Platesolver depending on the solver
        /// </summary>
        /// <param name="plateSolveSettings"></param>
        /// <param name="solver"> Plate Solver that should be used</param>
        /// <returns></returns>
        private static IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings, PlateSolverEnum solver) {
            return solver switch {
                PlateSolverEnum.ASTROMETRY_NET => new AstrometryPlateSolver(plateSolveSettings.AstrometryURL, plateSolveSettings.AstrometryAPIKey),
                PlateSolverEnum.LOCAL => new LocalPlateSolver(plateSolveSettings.CygwinLocation),
                PlateSolverEnum.PLATESOLVE2 => new Platesolve2Solver(plateSolveSettings.PS2Location),
                PlateSolverEnum.PLATESOLVE3 => new Platesolve3Solver(plateSolveSettings.PS3Location),
                PlateSolverEnum.ASPS => new AllSkyPlateSolver(plateSolveSettings.AspsLocation),
                PlateSolverEnum.TSX_IMAGELINK => new TheSkyXImageLinkSolver(plateSolveSettings.TheSkyXHost, plateSolveSettings.TheSkyXPort),
                PlateSolverEnum.PINPONT => new Dc3PinPointSolver(plateSolveSettings),
                _ => new ASTAPSolver(plateSolveSettings.ASTAPLocation),
            };
        }

        public static IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings) {
            return GetPlateSolver(plateSolveSettings, plateSolveSettings.PlateSolverType);
        }

        public static IPlateSolver GetBlindSolver(IPlateSolveSettings plateSolveSettings) {
            var type = plateSolveSettings.BlindSolverType switch {
                BlindSolverEnum.ASTROMETRY_NET => PlateSolverEnum.ASTROMETRY_NET,
                BlindSolverEnum.LOCAL => PlateSolverEnum.LOCAL,
                BlindSolverEnum.PLATESOLVE3 => PlateSolverEnum.PLATESOLVE3,
                BlindSolverEnum.ASPS => PlateSolverEnum.ASPS,
                BlindSolverEnum.PINPOINT => PlateSolverEnum.PINPONT,
                _ => PlateSolverEnum.ASTAP
            };

            return GetPlateSolver(plateSolveSettings, type);
        }
    }
}