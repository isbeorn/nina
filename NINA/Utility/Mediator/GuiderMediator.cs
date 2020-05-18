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
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class GuiderMediator : DeviceMediator<IGuiderVM, IGuiderConsumer, GuiderInfo>, IGuiderMediator {
        public bool IsUsingSynchronizedGuider => handler.GuiderIsSynchronized;

        public Task<bool> Dither(CancellationToken token) {
            return handler.Dither(token);
        }

        public Guid StartRMSRecording() {
            return handler.StartRMSRecording();
        }

        public RMS StopRMSRecording(Guid handle) {
            return handler.StopRMSRecording(handle);
        }

        public Task<bool> StartGuiding(CancellationToken token) {
            return handler.StartGuiding(token);
        }

        public Task<bool> StopGuiding(CancellationToken token) {
            return handler.StopGuiding(token);
        }

        public Task<bool> AutoSelectGuideStar(CancellationToken token) {
            return handler.AutoSelectGuideStar(token);
        }
    }
}
