#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Equipment.Interfaces.Mediator {

    public interface IFlatDeviceMediator : IDeviceMediator<IFlatDeviceVM, IFlatDeviceConsumer, FlatDeviceInfo> {

        Task SetBrightness(int brightness, IProgress<ApplicationStatus> progress, CancellationToken token);

        Task CloseCover(IProgress<ApplicationStatus> progress, CancellationToken token);

        Task ToggleLight(bool onOff, IProgress<ApplicationStatus> progress, CancellationToken token);

        Task OpenCover(IProgress<ApplicationStatus> progress, CancellationToken token);

        event Func<object, EventArgs, Task> Opened;
        event Func<object, EventArgs, Task> Closed;
        event Func<object, FlatDeviceBrightnessChangedEventArgs, Task> BrightnessChanged;
        event Func<object, EventArgs, Task> LightToggled;
    }

    public class FlatDeviceBrightnessChangedEventArgs : EventArgs {
        public FlatDeviceBrightnessChangedEventArgs(int from, int to) {
            From = from;
            To = to;
        }

        public int From { get; }
        public int To { get; }
    }
}