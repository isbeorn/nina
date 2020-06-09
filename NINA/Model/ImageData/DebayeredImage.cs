#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Utility.ImageAnalysis;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.ImageData {

    public class DebayeredImage : RenderedImage, IDebayeredImage {
        public LRGBArrays DebayeredData { get; private set; }
        private bool _saveColorChannels;
        private bool _saveLumChannel;

        protected DebayeredImage(
            BitmapSource image,
            IImageData rawImageData,
            LRGBArrays debayeredData,
            bool saveColorChannels,
            bool saveLumChannels) :
            base(image: image, rawImageData: rawImageData) {
            this.DebayeredData = debayeredData;
            this._saveColorChannels = saveColorChannels;
            this._saveLumChannel = saveLumChannels;
        }

        public static IDebayeredImage Debayer(
            IRenderedImage imageData,
            bool saveColorChannels = false,
            bool saveLumChannel = false,
            SensorType bayerPattern = SensorType.RGGB) {
            var debayeredImage = ImageUtility.Debayer(imageData.Image, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale, saveColorChannels, saveLumChannel, bayerPattern);
            return new DebayeredImage(
                image: debayeredImage.ImageSource,
                rawImageData: imageData.RawImageData,
                debayeredData: debayeredImage.Data,
                saveColorChannels: saveColorChannels,
                saveLumChannels: saveLumChannel);
        }

        public override async Task<IRenderedImage> Stretch(double factor, double blackClipping, bool unlinked) {
            if (this.DebayeredData == null) {
                // Unlinked stretch is only possible when the RGB Array was saved separately during debayer
                // This scenario will happen when the options are changed after the debayer has happened and the image is re-stretched again
                unlinked = false;
            }
            var stretchedImage = unlinked ? await ImageUtility.StretchUnlinked(this, factor, blackClipping) : await ImageUtility.Stretch(this, factor, blackClipping);
            return new DebayeredImage(
                image: stretchedImage,
                rawImageData: this.RawImageData,
                debayeredData: this.DebayeredData,
                saveColorChannels: this._saveColorChannels,
                saveLumChannels: this._saveLumChannel);
        }

        public override IRenderedImage ReRender() {
            var reRenderedImage = base.ReRender();
            return reRenderedImage.Debayer(saveColorChannels: this._saveColorChannels, saveLumChannel: this._saveLumChannel);
        }
    }
}