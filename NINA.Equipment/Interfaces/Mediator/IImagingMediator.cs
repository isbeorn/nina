#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Equipment.Equipment.MyCamera;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.Equipment.Interfaces.Mediator {

    public interface IImagingMediator : IMediator<IImagingVM> {

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
            IImageData imageData,
            PrepareImageParameters parameters,
            CancellationToken token);

        Task<IRenderedImage> PrepareImage(
            IExposureData imageData,
            PrepareImageParameters parameters,
            CancellationToken token);

        Task<bool> StartLiveView(CaptureSequence sequence, CancellationToken ct);

        void DestroyImage();

        void SetImage(BitmapSource img);
        int GetImageRotation();
        void SetImageRotation(int rotation);        

        event EventHandler<ImagePreparedEventArgs> ImagePrepared;        
    }

    public class ImagePreparedEventArgs : EventArgs {
        public IRenderedImage RenderedImage { get; set; }
        public PrepareImageParameters Parameters { get; set; }
    }
}