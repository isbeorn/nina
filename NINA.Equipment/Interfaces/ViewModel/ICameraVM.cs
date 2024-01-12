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

namespace NINA.Equipment.Interfaces.ViewModel {

    public interface ICameraVM : IDeviceVM<CameraInfo>, IDockableVM {

        void SetReadoutMode(short mode);

        void SetReadoutModeForNormalImages(short mode);

        void SetBinning(short x, short y);

        void AbortExposure();

        void SetDewHeater(bool onOff);

        bool AtTargetTemp { get; }

        double TargetTemp { get; }

        void SetTemperature(double temperature);

        void SetCooler(bool onOff);

        Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress);

        IAsyncEnumerable<IExposureData> LiveView(CaptureSequence sequence, CancellationToken token);

        Task<IExposureData> Download(CancellationToken token);

        Task<bool> CoolCamera(double temperature, TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct);

        Task<bool> WarmCamera(TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct);

        IDeviceChooserVM DeviceChooserVM { get; set; }
        void SetUSBLimit(int usbLimit);
        event Func<object, EventArgs, Task> DownloadTimeout;
    }
}