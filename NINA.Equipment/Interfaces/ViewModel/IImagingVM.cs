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
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Equipment.Equipment.MyCamera;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;

namespace NINA.Equipment.Interfaces.ViewModel {

    public interface IImagingVM : ICameraConsumer, ITelescopeConsumer, IFilterWheelConsumer, IFocuserConsumer, IRotatorConsumer, IWeatherDataConsumer {
        IImageControlVM ImageControl { get; }

        void DestroyImage();

        void SetImage(BitmapSource img);
        int GetImageRotation();
        void SetImageRotation(int rotation);

        Task<IExposureData> CaptureImage(
            CaptureSequence sequence,
            CancellationToken token,
            IProgress<ApplicationStatus> progress,
            string targetName = "");

        Task<IRenderedImage> CaptureAndPrepareImage(
            CaptureSequence sequence,
            PrepareImageParameters parameters,
            CancellationToken token,
            IProgress<ApplicationStatus> progress);

        Task<IRenderedImage> PrepareImage(
            IImageData data,
            PrepareImageParameters parameters,
            CancellationToken token);

        Task<IRenderedImage> PrepareImage(
            IExposureData data,
            PrepareImageParameters parameters,
            CancellationToken token);

        Task<bool> StartLiveView(CaptureSequence sequence, CancellationToken ct);

        event EventHandler<ImagePreparedEventArgs> ImagePrepared;
    }
}