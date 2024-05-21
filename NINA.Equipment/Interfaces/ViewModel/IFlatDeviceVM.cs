#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Threading;
using NINA.Equipment.Equipment.MyFlatDevice;
using System.Threading.Tasks;
using NINA.Core.Model;
using System;
using NINA.Equipment.Interfaces.Mediator;

namespace NINA.Equipment.Interfaces.ViewModel {

    public interface IFlatDeviceVM : IDeviceVM<FlatDeviceInfo>, IDockableVM {

        Task<bool> OpenCover(IProgress<ApplicationStatus> progress, CancellationToken token);

        Task<bool> CloseCover(IProgress<ApplicationStatus> progress, CancellationToken token);

        int Brightness { get; set; }
        bool LightOn { get; set; }
        FlatDeviceInfo FlatDeviceInfo { get; set; }

        Task<bool> ToggleLight(bool onOff, IProgress<ApplicationStatus> progress, CancellationToken token);

        Task<bool> SetBrightness(int value, IProgress<ApplicationStatus> progress, CancellationToken token);
        event Func<object, EventArgs, Task> Opened;
        event Func<object, EventArgs, Task> Closed;
        event Func<object, FlatDeviceBrightnessChangedEventArgs, Task> BrightnessChanged;
        event Func<object, EventArgs, Task> LightToggled;
    }
}