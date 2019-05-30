#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Utility.Enum;
using NINA.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Mediator.Interfaces {

    internal interface IImagingMediator : IMediator<IImagingVM> {

        bool SetDetectStars(bool value);

        bool SetAutoStretch(bool value);

        Task<IImageData> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress);

        Task<IImageData> PrepareImage(IImageData iarr, CancellationToken token);

        void DestroyImage();

        bool IsLooping { get; }

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