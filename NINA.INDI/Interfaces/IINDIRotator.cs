#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Threading;
using System.Threading.Tasks;

namespace NINA.INDI.Interfaces {
    public interface IINDIRotator : IINDIDevice {
        bool CanReverse { get; }
        bool IsMoving { get; }
        float MechanicalPosition { get; }
        float Position { get; }
        bool Reverse { get; set; }
        float StepSize { get; }
        float TargetPosition { get; }

        void Halt();
        Task MoveAsync(float Position, CancellationToken ct = default);
        void MoveAbsolute(float Position);
        bool MoveMechanical(float Position);
        void Sync(float Position);
    }
}
