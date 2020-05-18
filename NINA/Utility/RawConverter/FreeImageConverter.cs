#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FreeImageAPI;
using FreeImageAPI.Metadata;
using NINA.Model.ImageData;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.RawConverter {

    internal class FreeImageConverter : IRawConverter {

        public FreeImageConverter() {
            DllLoader.LoadDll(Path.Combine("FreeImage", "FreeImage.dll"));
        }

        public Task<IImageData> Convert(
            MemoryStream s,
            int bitDepth,
            string rawType,
            ImageMetaData metaData,
            CancellationToken token = default) {
            return Task.Run(() => {
                using (MyStopWatch.Measure()) {
                    FIBITMAP img;
                    int left, top, imgWidth, imgHeight;
                    FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_RAW;
                    img = FreeImage.LoadFromStream(s, (FREE_IMAGE_LOAD_FLAGS)8, ref format);

                    FreeImage.GetMetadata(FREE_IMAGE_MDMODEL.FIMD_COMMENTS, img, "Raw.Frame.Width", out MetadataTag widthTag);
                    FreeImage.GetMetadata(FREE_IMAGE_MDMODEL.FIMD_COMMENTS, img, "Raw.Frame.Height", out MetadataTag heightTag);
                    FreeImage.GetMetadata(FREE_IMAGE_MDMODEL.FIMD_COMMENTS, img, "Raw.Frame.Left", out MetadataTag leftTag);
                    FreeImage.GetMetadata(FREE_IMAGE_MDMODEL.FIMD_COMMENTS, img, "Raw.Frame.Top", out MetadataTag topTag);
                    left = int.Parse(leftTag.ToString());
                    top = int.Parse(topTag.ToString());
                    imgWidth = int.Parse(widthTag.ToString());
                    imgHeight = int.Parse(heightTag.ToString());

                    using (var memStream = new MemoryStream()) {
                        FreeImage.SaveToStream(img, memStream, FREE_IMAGE_FORMAT.FIF_TIFF, FREE_IMAGE_SAVE_FLAGS.TIFF_NONE);
                        memStream.Position = 0;

                        var decoder = new TiffBitmapDecoder(memStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

                        CroppedBitmap cropped = new CroppedBitmap(decoder.Frames[0], new System.Windows.Int32Rect(left, top, imgWidth, imgHeight));

                        ushort[] outArray = new ushort[cropped.PixelWidth * cropped.PixelHeight];
                        cropped.CopyPixels(outArray, 2 * cropped.PixelWidth, 0);
                        FreeImage.UnloadEx(ref img);

                        var imageArray = new ImageArray(flatArray: outArray, rawData: s.ToArray(), rawType: rawType);
                        var data = new ImageData(
                            imageArray: imageArray,
                            width: cropped.PixelWidth,
                            height: cropped.PixelHeight,
                            bitDepth: bitDepth,
                            isBayered: true,
                            metaData: metaData);
                        return Task.FromResult<IImageData>(data);
                    }
                }
            });
        }
    }
}
