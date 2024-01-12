#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyGuider.PHD2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Interfaces.ViewModel {

    public interface IGuiderVM : IDeviceVM<GuiderInfo>, IDockableVM {

        Task<bool> Dither(CancellationToken token);

        Guid StartRMSRecording();

        RMS GetRMSRecording(Guid handle);

        Task<bool> StartGuiding(bool forceCalibration, IProgress<ApplicationStatus> progress, CancellationToken token);

        Task<bool> StopGuiding(CancellationToken token);

        Task<bool> AutoSelectGuideStar(CancellationToken token);

        Task<bool> ClearCalibration(CancellationToken token);

        RMS StopRMSRecording(Guid handle);

        Task<bool> SetShiftRate(SiderealShiftTrackingRate shiftTrackingRate, CancellationToken ct);

        Task<bool> StopShifting(CancellationToken ct);

        LockPosition GetLockPosition();
        event Func<object, EventArgs, Task> AfterDither;

        event EventHandler<IGuideStep> GuideEvent;
    }
}