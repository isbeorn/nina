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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FreeImageAPI;
using FreeImageAPI.Metadata;
using NINA.Model.MyCamera;

namespace NINA.Utility.RawConverter {

    internal class FreeImageConverter : IRawConverter {

        public FreeImageConverter() {
            DllLoader.LoadDll("FreeImage/FreeImage.dll");
        }

        public async Task<ImageArray> ConvertToImageArray(MemoryStream s, int bitDepth, int histogramResolution, CancellationToken token) {
            return await Task.Run(async () => {
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

                    var memStream = new MemoryStream();
                    FreeImage.SaveToStream(img, memStream, FREE_IMAGE_FORMAT.FIF_TIFF, FREE_IMAGE_SAVE_FLAGS.TIFF_NONE);
                    memStream.Position = 0;

                    var decoder = new TiffBitmapDecoder(memStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

                    CroppedBitmap cropped = new CroppedBitmap(decoder.Frames[0], new System.Windows.Int32Rect(left, top, imgWidth, imgHeight));

                    ushort[] outArray = new ushort[cropped.PixelWidth * cropped.PixelHeight];
                    cropped.CopyPixels(outArray, 2 * cropped.PixelWidth, 0);
                    memStream.Dispose();
                    FreeImage.UnloadEx(ref img);
                    var iarr = await ImageArray.CreateInstance(outArray, cropped.PixelWidth, cropped.PixelHeight, bitDepth, true, true, histogramResolution);
                    iarr.RAWData = s.ToArray();
                    return iarr;
                }
            });
        }
    }
}