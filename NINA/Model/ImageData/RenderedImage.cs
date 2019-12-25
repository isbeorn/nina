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

using NINA.Model.MyCamera;
using NINA.Utility.Enum;
using NINA.Utility.ImageAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.Model.ImageData {

    public class RenderedImage : IRenderedImage {
        public IImageData RawImageData { get; private set; }

        public BitmapSource Image { get; private set; }

        public RenderedImage(BitmapSource image, IImageData rawImageData) {
            this.Image = image;
            this.RawImageData = rawImageData;
        }

        public static async Task<IRenderedImage> FromBitmapSource(BitmapSource source, bool calculateStatistics = false) {
            var exposureData = await ImageArrayExposureData.FromBitmapSource(source);
            var rawImageData = await exposureData.ToImageData();
            return Create(source: source, rawImageData: rawImageData, calculateStatistics: calculateStatistics);
        }

        public static RenderedImage Create(BitmapSource source, IImageData rawImageData, bool calculateStatistics = false) {
            return new RenderedImage(image: source, rawImageData: rawImageData);
        }

        public virtual IRenderedImage ReRender() {
            return new RenderedImage(image: this.RawImageData.RenderBitmapSource(), rawImageData: this.RawImageData);
        }

        public IDebayeredImage Debayer(bool saveColorChannels = false, bool saveLumChannel = false) {
            return DebayeredImage.Debayer(this, saveColorChannels: saveColorChannels, saveLumChannel: saveLumChannel);
        }

        public virtual async Task<IRenderedImage> Stretch(double factor, double blackClipping, bool unlinked) {
            var stretchedImage = await ImageUtility.Stretch(this, factor, blackClipping);
            return new RenderedImage(image: stretchedImage, rawImageData: this.RawImageData);
        }

        public async Task<IRenderedImage> DetectStars(
            bool annotateImage,
            StarSensitivityEnum sensitivity,
            NoiseReductionEnum noiseReduction,
            CancellationToken cancelToken = default,
            IProgress<ApplicationStatus> progress = default(Progress<ApplicationStatus>)) {
            var starDetection = new StarDetection(this, this.Image.Format, sensitivity, noiseReduction);
            await starDetection.DetectAsync(progress, cancelToken);
            var image = annotateImage ? starDetection.GetAnnotatedImage() : this.Image;
            this.RawImageData.StarDetectionAnalysis.HFR = starDetection.AverageHFR;
            this.RawImageData.StarDetectionAnalysis.DetectedStars = starDetection.DetectedStars;
            return new RenderedImage(image: image, rawImageData: this.RawImageData);
        }

        public async Task<BitmapSource> GetThumbnail() {
            BitmapSource image = null;
            await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                var factor = 300 / this.Image.Width;
                image = new WriteableBitmap(new TransformedBitmap(this.Image, new ScaleTransform(factor, factor)));
                image.Freeze();
            }));
            return image;
        }

        private static Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }
}