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

        public async Task<ImageArray> ConvertToImageArray(MemoryStream s, CancellationToken token) {
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
                    return await ImageArray.CreateInstance(outArray, cropped.PixelWidth, cropped.PixelHeight, true);
                }
            });
        }
    }
}