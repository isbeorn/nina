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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.Mediator {

    public class FlatDeviceMediator : DeviceMediator<IFlatDeviceVM, IFlatDeviceConsumer, FlatDeviceInfo>, IFlatDeviceMediator {

        public Task SetBrightness(int brightness, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return handler.SetBrightness(brightness, progress, token);
        }

        public Task CloseCover(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return handler.CloseCover(progress, token);
        }

        public Task ToggleLight(bool onOff, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return handler.ToggleLight(onOff, progress, token);
        }

        public Task OpenCover(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return handler.OpenCover(progress, token);
        }
    }
}