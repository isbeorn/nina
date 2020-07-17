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
using NINA.Utility.Enum;
using NINA.ViewModel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Mediator.Interfaces {

    public interface IImagingMediator : IMediator<IImagingVM> {

        Task<IExposureData> CaptureImage(
            CaptureSequence sequence,
            CancellationToken token,
            IProgress<ApplicationStatus> progress);

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

        Task<bool> StartLiveView(CancellationToken ct);

        void DestroyImage();

        event EventHandler<ImageSavedEventArgs> ImageSaved;

        void OnImageSaved(ImageSavedEventArgs e);

        void SetImage(BitmapSource img);
    }

    public class ImageSavedEventArgs : EventArgs {
        public BitmapSource Image { get; set; }
        public double Mean { get; set; }
        public Uri PathToImage { get; set; }
        public FileTypeEnum FileType { get; set; }
        public double HFR { get; internal set; }
        public bool IsBayered { get; internal set; }
        public double Duration { get; internal set; }
        public string Filter { get; internal set; }
    }
}