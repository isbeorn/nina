using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility.Enum;
using NINA.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IImagingMediator : IMediator<IImagingVM> {

        bool SetDetectStars(bool value);

        bool SetAutoStretch(bool value);

        Task<BitmapSource> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress);

        Task<bool> CaptureAndSaveImage(CaptureSequence seq, bool bsave, CancellationToken ct, IProgress<ApplicationStatus> progress, string targetname = "");

        Task<ImageArray> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, bool bSave = false, string targetname = "");

        Task<BitmapSource> PrepareImage(ImageArray iarr, CancellationToken token, bool bSave = false, ImageParameters parameters = null);

        event EventHandler<ImageSavedEventArgs> ImageSaved;

        void OnImageSaved(ImageSavedEventArgs e);
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
        public int StatisticsId { get; internal set; }
    }
}