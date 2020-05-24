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
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    public interface ICameraMediator : IDeviceMediator<ICameraVM, ICameraConsumer, CameraInfo> {

        Task Capture(CaptureSequence sequence, CancellationToken token,
            IProgress<ApplicationStatus> progress);

        IAsyncEnumerable<IExposureData> LiveView(CancellationToken token);

        Task<IExposureData> Download(CancellationToken token);

        void AbortExposure();

        void SetBinning(short x, short y);

        void SetGain(short gain);

        void SetSubSample(bool subSample);

        void SetSubSampleArea(int x, int y, int width, int height);

        bool AtTargetTemp { get; }

        double TargetTemp { get; }

        Task<bool> CoolCamera(double temperature, TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct);

        Task<bool> WarmCamera(TimeSpan duration, IProgress<ApplicationStatus> progress, CancellationToken ct);
    }
}