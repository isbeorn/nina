#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Image.ImageData;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.Mediator {

    public class ImagingMediator : IImagingMediator {
        protected IImagingVM handler;

        public void RegisterHandler(IImagingVM handler) {
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;
        }

        public Task<IRenderedImage> CaptureAndPrepareImage(
            CaptureSequence sequence,
            PrepareImageParameters parameters,
            CancellationToken token,
            IProgress<ApplicationStatus> progress) {
            return handler.CaptureAndPrepareImage(sequence, parameters, token, progress);
        }

        public Task<IExposureData> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, string targetName = "") {
            return handler.CaptureImage(sequence, token, progress, targetName);
        }

        public Task<IRenderedImage> PrepareImage(
            IImageData data,
            PrepareImageParameters parameters,
            CancellationToken token) {
            return handler.PrepareImage(data, parameters, token);
        }

        public Task<IRenderedImage> PrepareImage(
            IExposureData data,
            PrepareImageParameters parameters,
            CancellationToken token) {
            return handler.PrepareImage(data, parameters, token);
        }

        public void DestroyImage() {
            handler.DestroyImage();
        }

        public void SetImage(BitmapSource img) {
            handler.SetImage(img);
        }

        public Task<bool> StartLiveView(CaptureSequence sequence, CancellationToken ct) {
            return handler.StartLiveView(sequence, ct);
        }

        public event EventHandler<ImagePreparedEventArgs> ImagePrepared {
            add { this.handler.ImagePrepared += value; }
            remove { this.handler.ImagePrepared -= value; }
        }

        public int GetImageRotation() {
            return handler.GetImageRotation();
        }

        public void SetImageRotation(int rotation) {
            handler.SetImageRotation(rotation);
        }
    }
}