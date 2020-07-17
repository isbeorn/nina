#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {

    public interface IFocuser : IDevice {
        bool IsMoving { get; }
        int MaxIncrement { get; }
        int MaxStep { get; }
        int Position { get; }
        double StepSize { get; }
        bool TempCompAvailable { get; }
        bool TempComp { get; set; }
        double Temperature { get; }

        Task Move(int position, CancellationToken ct);

        void Halt();
    }
}