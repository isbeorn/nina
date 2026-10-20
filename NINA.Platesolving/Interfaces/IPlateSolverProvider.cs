#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;

namespace NINA.PlateSolving.Interfaces {

    public interface IPlateSolverProvider {
        string Id { get; }

        string Name { get; }

        bool SupportsPrimary { get; }

        bool SupportsBlind { get; }

        IPlateSolver CreateSolver(IPlateSolveSettings settings);
    }
}
