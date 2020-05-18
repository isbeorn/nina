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

namespace NINA.Model.MyFlatDevice {

    public interface IFlatDevice : IDevice {
        CoverState CoverState { get; }

        int MaxBrightness { get; }

        int MinBrightness { get; }

        Task<bool> Open(CancellationToken ct, int delay = 300);

        Task<bool> Close(CancellationToken ct, int delay = 300);

        bool LightOn { get; set; }

        double Brightness { get; set; }

        string PortName { get; set; }

        bool SupportsOpenClose { get; }
    }

    public enum CoverState { Unknown, NeitherOpenNorClosed, Closed, Open };
}
