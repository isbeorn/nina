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