using NINA.Model;
using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel.Interfaces {

    internal interface IImagingVM {

        bool SetDetectStars(bool value);

        bool SetAutoStretch(bool value);

        void DestroyImage();

        Task<BitmapSource> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress);

        Task<bool> CaptureAndSaveImage(CaptureSequence seq, bool bsave, CancellationToken ct, IProgress<ApplicationStatus> progress, string targetname = "");

        Task<ImageArray> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, bool bSave = false, string targetname = "");

        Task<ImageArray> CaptureImageWithoutSaving(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress);

        Task<BitmapSource> PrepareImage(ImageArray iarr, CancellationToken token, bool bSave = false, ImageParameters parameters = null);
    }
}