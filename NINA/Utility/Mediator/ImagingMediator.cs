#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Mediator {

    internal class ImagingMediator : IImagingMediator {
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

        public Task<IExposureData> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress) {
            return handler.CaptureImage(sequence, token, progress);
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

        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public void OnImageSaved(ImageSavedEventArgs e) {
            ImageSaved?.Invoke(handler, e);
        }

        public void DestroyImage() {
            handler.DestroyImage();
        }

        public bool IsLooping {
            get {
                return handler.IsLooping;
            }
        }

        public void SetImage(BitmapSource img) {
            handler.SetImage(img);
        }
    }
}