#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyCamera;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Core.Utility.Extensions;

namespace NINA.WPF.Base.Mediator {

    public class CameraMediator : DeviceMediator<ICameraVM, ICameraConsumer, CameraInfo>, ICameraMediator {
        private object blockingConsumer;

        public event Func<object, EventArgs, Task> DownloadTimeout {
            add { this.handler.DownloadTimeout += value; }
            remove { this.handler.DownloadTimeout -= value; }
        }

        public Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress) {
            return handler.Capture(sequence, token, progress);
        }

        public IAsyncEnumerable<IExposureData> LiveView(CancellationToken token) {
            return handler.LiveView(new CaptureSequence(), token);
        }

        public IAsyncEnumerable<IExposureData> LiveView(CaptureSequence sequence, CancellationToken token) {
            return handler.LiveView(sequence, token);
        }

        public Task<IExposureData> Download(CancellationToken token) {
            return handler.Download(token);
        }

        public Task<bool> CoolCamera(double temperature, TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return handler.CoolCamera(temperature, duration, progress, ct);
        }

        public Task<bool> WarmCamera(TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return handler.WarmCamera(duration, progress, ct);
        }

        public void AbortExposure() {
            handler.AbortExposure();
        }

        public void SetReadoutMode(short value) {
            handler.SetReadoutMode(value);
        }

        public void SetReadoutModeForNormalImages(short value) {
            handler.SetReadoutModeForNormalImages(value);
        }

        public void SetBinning(short x, short y) {
            handler.SetBinning(x, y);
        }

        public void RegisterCaptureBlock(object cameraConsumer) {
            if (this.blockingConsumer != null) {
                throw new Exception("CameraMediator already blocked by " + blockingConsumer);
            }

            blockingConsumer = cameraConsumer;
        }

        public void ReleaseCaptureBlock(object cameraConsumer) {
            if (this.blockingConsumer == cameraConsumer) {
                blockingConsumer = null;
            }
        }

        public bool IsFreeToCapture(object cameraConsumer) {
            return blockingConsumer == null ? true : cameraConsumer == blockingConsumer;
        }

        public bool AtTargetTemp => handler.AtTargetTemp;

        public double TargetTemp => handler.TargetTemp;

        public void SetDewHeater(bool onOff) {
            handler.SetDewHeater(onOff);
        }

        public void RegisterCaptureBlock(ICameraConsumer cameraConsumer) {
            RegisterCaptureBlock((object)cameraConsumer);
        }

        public void ReleaseCaptureBlock(ICameraConsumer cameraConsumer) {
            ReleaseCaptureBlock((object)cameraConsumer);
        }

        public bool IsFreeToCapture(ICameraConsumer cameraConsumer) {
            return IsFreeToCapture((object)cameraConsumer);
        }

        public void SetUSBLimit(int usbLimit) {
            handler.SetUSBLimit(usbLimit);
        }
    }
}