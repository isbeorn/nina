#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Equipment.Model;
using NINA.Image.Interfaces;
using NINA.Equipment.Equipment.MyCamera;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Equipment.Interfaces.Mediator {

    public interface ICameraMediator : IDeviceMediator<ICameraVM, ICameraConsumer, CameraInfo> {

        Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress);

        IAsyncEnumerable<IExposureData> LiveView(CancellationToken token);

        IAsyncEnumerable<IExposureData> LiveView(CaptureSequence sequence, CancellationToken token);

        Task<IExposureData> Download(CancellationToken token);

        void AbortExposure();

        void SetReadoutMode(short mode);

        void SetReadoutModeForNormalImages(short mode);

        void SetBinning(short x, short y);

        void SetDewHeater(bool onOff);

        bool AtTargetTemp { get; }

        double TargetTemp { get; }

        Task<bool> CoolCamera(double temperature, TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct);

        Task<bool> WarmCamera(TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct);

        void RegisterCaptureBlock(ICameraConsumer cameraConsumer);

        void ReleaseCaptureBlock(ICameraConsumer cameraConsumer);

        bool IsFreeToCapture(ICameraConsumer cameraConsumer);

        void RegisterCaptureBlock(object cameraConsumer);

        void ReleaseCaptureBlock(object cameraConsumer);

        bool IsFreeToCapture(object cameraConsumer);
        void SetUSBLimit(int usbLimit);
        event Func<object, EventArgs, Task> DownloadTimeout;
    }
}