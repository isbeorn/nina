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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;

namespace NINA.PlateSolving.Interfaces {

    public interface IPlateSolverFactory {

        IPlateSolver GetPlateSolver(IPlateSolveSettings plateSolveSettings);

        IPlateSolver GetBlindSolver(IPlateSolveSettings plateSolveSettings);

        IImageSolver GetImageSolver(IPlateSolver plateSolver, IPlateSolver blindSolver);

        ICaptureSolver GetCaptureSolver(IPlateSolver plateSolver, IPlateSolver blindSolver, IImagingMediator imagingMediator, IFilterWheelMediator filterWheelMediator);

        ICenteringSolver GetCenteringSolver(IPlateSolver plateSolver, IPlateSolver blindSolver, IImagingMediator imagingMediator, ITelescopeMediator telescopeMediator, IFilterWheelMediator filterWheelMediator, IDomeMediator domeMediator, IDomeFollower domeFollower);
    }
}