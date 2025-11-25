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
    public interface IINDIFocuser : IINDIDevice {
        bool Absolute { get; }
        bool IsMoving { get; }
        int MaxIncrement { get; }
        int MaxStep { get; set; }
        int Position { get; }
        double StepSize { get; }
        bool TempComp { get; }
        bool TempCompAvailable { get; }
        double Temperature { get; }
        bool Reverse { get; set; }

        void Halt();
        void Move(int Position);
        Task MoveAsync(int position, CancellationToken ct);
        void SyncPosition(int Position);
    }
}
