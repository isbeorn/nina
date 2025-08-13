#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Astrometry;
using NINA.Core.Utility.Extensions;
using NINA.Core.Interfaces;
using NINA.Equipment.Equipment.MyGuider.PHD2;

namespace NINA.WPF.Base.Mediator {

    public class GuiderMediator : DeviceMediator<IGuiderVM, IGuiderConsumer, GuiderInfo>, IGuiderMediator {

        public Task<bool> Dither(CancellationToken token) {
            return handler.Dither(token);
        }

        public Guid StartRMSRecording() {
            return handler.StartRMSRecording();
        }

        public RMS GetRMSRecording(Guid handle) {
            return handler.GetRMSRecording(handle);
        }

        public RMS StopRMSRecording(Guid handle) {
            return handler.StopRMSRecording(handle);
        }

        public Task<bool> StartGuiding(bool forceCalibration, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return handler.StartGuiding(forceCalibration, progress, token);
        }

        public Task<bool> StopGuiding(CancellationToken token) {
            return handler.StopGuiding(token);
        }

        public Task<bool> AutoSelectGuideStar(CancellationToken token) {
            return handler.AutoSelectGuideStar(token);
        }

        public Task<bool> ClearCalibration(CancellationToken token) {
            return handler.ClearCalibration(token);
        }

        public Task<bool> SetShiftRate(SiderealShiftTrackingRate shiftTrackingRate, CancellationToken ct) {
            return handler.SetShiftRate(shiftTrackingRate, ct);
        }

        public Task<bool> StopShifting(CancellationToken ct) {
            return handler.StopShifting(ct);
        }

        public LockPosition GetLockPosition() {
            return handler.GetLockPosition();
        }

        /// <summary>
        /// Will be raised each time the application receives guide pulse info from the guider
        /// </summary>
        public event EventHandler<IGuideStep> GuideEvent {
            add { this.handler.GuideEvent += value; }
            remove { this.handler.GuideEvent -= value; }
        }

        public event Func<object, EventArgs, Task> AfterDither {
            add { this.handler.AfterDither += value; }
            remove { this.handler.AfterDither -= value; }
        }

        public event Func<object, EventArgs, Task> GuidingStarted {
            add { this.handler.GuidingStarted += value; }
            remove { this.handler.GuidingStarted -= value; }
        }

        public event Func<object, EventArgs, Task> GuidingStopped {
            add { this.handler.GuidingStopped += value; }
            remove { this.handler.GuidingStopped -= value; }
        }
    }
}