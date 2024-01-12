#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Image.ImageData {

    public class DebayeredImage : RenderedImage, IDebayeredImage {
        public LRGBArrays DebayeredData { get; private set; }

        public bool SaveColorChannels { get; private set; }
        public bool SaveLumChannel { get; private set; }

        public SensorType BayerPattern { get; private set; }

        protected DebayeredImage(
            BitmapSource image,
            IImageData rawImageData,
            LRGBArrays debayeredData,
            bool saveColorChannels,
            bool saveLumChannels,
            SensorType bayerPattern,
            IProfileService profileService,
            IStarDetection starDetection,
            IStarAnnotator starAnnotator) :
            base(image, rawImageData, profileService, starDetection, starAnnotator) {
            this.DebayeredData = debayeredData;
            this.SaveColorChannels = saveColorChannels;
            this.SaveLumChannel = saveLumChannels;
            this.BayerPattern = bayerPattern;
        }

        public static IDebayeredImage Debayer(
            IRenderedImage imageData,
            IProfileService profileService,
            IStarDetection starDetection,
            IStarAnnotator starAnnotator,
            bool saveColorChannels = false,
            bool saveLumChannel = false,
            SensorType bayerPattern = SensorType.RGGB) {
            var debayeredImage = ImageUtility.Debayer(imageData.Image, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale, saveColorChannels, saveLumChannel, bayerPattern);
            return new DebayeredImage(
                image: debayeredImage.ImageSource,
                rawImageData: imageData.RawImageData,
                debayeredData: debayeredImage.Data,
                saveColorChannels: saveColorChannels,
                saveLumChannels: saveLumChannel,
                bayerPattern: bayerPattern,
                profileService: profileService,
                starDetection: starDetection,
                starAnnotator: starAnnotator);
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
                saveColorChannels: this.SaveColorChannels,
                saveLumChannels: this.SaveLumChannel,
                bayerPattern: this.BayerPattern,
                profileService: this.profileService,
                starDetection: this.starDetection,
                starAnnotator: this.starAnnotator);
        }

        public override IRenderedImage ReRender() {
            var reRenderedImage = base.ReRender();
            return reRenderedImage.Debayer(saveColorChannels: this.SaveColorChannels, saveLumChannel: this.SaveLumChannel);
        }
    }
}