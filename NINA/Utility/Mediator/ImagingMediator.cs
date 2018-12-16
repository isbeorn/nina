#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Model.MyCamera;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public bool SetDetectStars(bool value) {
            return handler.SetDetectStars(value);
        }

        public bool SetAutoStretch(bool value) {
            return handler.SetAutoStretch(value);
        }

        public Task<BitmapSource> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress) {
            return handler.CaptureAndPrepareImage(sequence, token, progress);
        }

        public Task<bool> CaptureAndSaveImage(CaptureSequence seq, bool bsave, CancellationToken ct, IProgress<ApplicationStatus> progress, string targetname = "") {
            return handler.CaptureAndSaveImage(seq, bsave, ct, progress, targetname);
        }

        public Task<ImageArray> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, bool bSave = false, bool bCalculateStatistics = true, string targetname = "") {
            return handler.CaptureImage(sequence, token, progress, bSave, bCalculateStatistics, targetname);
        }

        public Task<ImageArray> CaptureImageWithoutSavingToHistoryAndThumbnail(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, bool bSave = false, bool bCalculateStatistics = false, string targetName = "") {
            return handler.CaptureImageWithoutHistoryAndThumbnail(sequence, token, progress, bSave, bCalculateStatistics, targetName);
        }

        public Task<BitmapSource> PrepareImage(
                ImageArray iarr,
                CancellationToken token,
                bool bSave = false,
                ImageParameters parameters = null) {
            return handler.PrepareImage(iarr, token, bSave, parameters);
        }

        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public void OnImageSaved(ImageSavedEventArgs e) {
            ImageSaved?.Invoke(handler, e);
        }

        public void DestroyImage() {
            handler.DestroyImage();
        }
    }
}