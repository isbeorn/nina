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
            bool saveLumChannel = false) {
            var debayeredImage = ImageUtility.Debayer(imageData.Image, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale, saveColorChannels, saveLumChannel);
            return new DebayeredImage(
                image: debayeredImage.ImageSource,
                rawImageData: imageData.RawImageData,
                debayeredData: debayeredImage.Data,
                saveColorChannels: saveColorChannels,
                saveLumChannels: saveLumChannel);
        }

        public override async Task<IRenderedImage> Stretch(double factor, double blackClipping, bool unlinked) {
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