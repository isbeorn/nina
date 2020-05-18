#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyGuider;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Equipment.Guider {

    public interface IGuiderVM : IDeviceVM<GuiderInfo> {

        Task<bool> Dither(CancellationToken token);

        Guid StartRMSRecording();

        Task<bool> StartGuiding(CancellationToken token);

        Task<bool> StopGuiding(CancellationToken token);

        Task<bool> AutoSelectGuideStar(CancellationToken token);

        RMS StopRMSRecording(Guid handle);

        bool GuiderIsSynchronized { get; }
    }
}
