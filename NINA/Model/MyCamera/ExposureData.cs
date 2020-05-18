#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.ImageData;
using NINA.Utility;
using NINA.Utility.RawConverter;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Model.MyCamera {

    public abstract class BaseExposureData : IExposureData {
        public int BitDepth { get; private set; }
        public ImageMetaData MetaData { get; private set; }

        protected BaseExposureData(int bitDepth, ImageMetaData metadata) {
            this.BitDepth = bitDepth;
            this.MetaData = metadata;
        }

        public abstract Task<IImageData> ToImageData(CancellationToken cancelToken);
    }

    public class CachedExposureData : BaseExposureData {
        private readonly IImageData imageData;

        public CachedExposureData(IImageData imageData)
            : base(imageData.Properties.BitDepth, imageData.MetaData) {
            this.imageData = imageData;
        }

        public override Task<IImageData> ToImageData(CancellationToken cancelToken) {
            return Task.FromResult(this.imageData);
        }
    }

    public class ImageArrayExposureData : BaseExposureData {
        private readonly IImageArray imageArray;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsBayered { get; private set; }

        public ImageArrayExposureData(
            ushort[] input,
            int width,
            int height,
            int bitDepth,
            bool isBayered,
            ImageMetaData metaData)
            : base(bitDepth, metaData) {
            this.imageArray = new ImageArray(input);
            this.Width = width;
            this.Height = height;
            this.IsBayered = isBayered;
        }

        public override Task<IImageData> ToImageData(CancellationToken cancelToken = default) {
            return Task.FromResult<IImageData>(
                new ImageData.ImageData(
                    imageArray: this.imageArray,
                    width: this.Width,
                    height: this.Height,
                    bitDepth: this.BitDepth,
                    isBayered: this.IsBayered,
                    metaData: this.MetaData));
        }

        public static async Task<ImageArrayExposureData> FromBitmapSource(BitmapSource source) {
            var pixels = await Task.Run(() => ArrayFromSource(source));
            return new ImageArrayExposureData(
                input: pixels,
                width: source.PixelWidth,
                height: source.PixelHeight,
                bitDepth: source.Format.BitsPerPixel,
                isBayered: false,
                metaData: new ImageMetaData());
        }

        private static ushort[] ArrayFromSource(BitmapSource source) {
            if (source.Format == PixelFormats.Gray16) {
                return ArrayFrom16BitSource(source);
            } else if (source.Format == PixelFormats.Gray8 || source.Format == PixelFormats.Indexed8) {
                return ArrayFrom8BitSource(source);
            } else if (source.Format == PixelFormats.Bgr24 || source.Format == PixelFormats.Bgr32 || source.Format == PixelFormats.Pbgra32) {
                WriteableBitmap convertedSource = new WriteableBitmap(
                   (BitmapSource)(new FormatConvertedBitmap(source, PixelFormats.Gray8, null, 0))
                );
                return ArrayFrom8BitSource(convertedSource);
            } else {
                throw new FormatException(string.Format("Pixelformat {0} not supported", source.Format));
            }
        }

        private static ushort[] ArrayFrom8BitSource(BitmapSource source) {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            int arraySize = stride * source.PixelHeight;
            byte[] pixels = new byte[arraySize];
            source.CopyPixels(pixels, stride, 0);

            ushort[] array = new ushort[pixels.Length];
            for (int i = 0; i < array.Length; i++) {
                array[i] = (ushort)(pixels[i] * (ushort.MaxValue / (double)byte.MaxValue));
            }

            return array;
        }

        private static ushort[] ArrayFrom16BitSource(BitmapSource source) {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            int arraySize = stride * source.PixelHeight;
            ushort[] pixels = new ushort[arraySize];
            source.CopyPixels(pixels, stride, 0);

            return pixels;
        }
    }

    public class Flipped2DExposureData : BaseExposureData {
        private readonly Array flipped2DArray;
        public bool IsBayered { get; private set; }

        public Flipped2DExposureData(
            Array flipped2DArray,
            int bitDepth,
            bool isBayered,
            ImageMetaData metaData)
            : base(bitDepth, metaData) {
            if (flipped2DArray.Rank > 2) { throw new NotSupportedException(); }
            this.flipped2DArray = flipped2DArray;
            this.IsBayered = isBayered;
        }

        public override async Task<IImageData> ToImageData(CancellationToken cancelToken = default) {
            var flatArray = await Task.Run(() => FlipAndConvert2d(this.flipped2DArray), cancelToken);
            return new ImageData.ImageData(
                imageArray: new ImageArray(flatArray),
                width: this.flipped2DArray.GetLength(0),
                height: this.flipped2DArray.GetLength(1),
                bitDepth: this.BitDepth,
                isBayered: this.IsBayered,
                metaData: this.MetaData);
        }

        private static ushort[] FlipAndConvert2d(Array input) {
            using (MyStopWatch.Measure("FlipAndConvert2d")) {
                Int32[,] arr = (Int32[,])input;
                int width = arr.GetLength(0);
                int height = arr.GetLength(1);

                int length = width * height;
                ushort[] flatArray = new ushort[length];
                ushort value;

                unsafe {
                    fixed (Int32* ptr = arr) {
                        int idx = 0, row = 0;
                        for (int i = 0; i < length; i++) {
                            value = (ushort)ptr[i];

                            idx = ((i % height) * width) + row;
                            if ((i % (height)) == (height - 1)) row++;

                            ushort b = value;
                            flatArray[idx] = b;
                        }
                    }
                }
                return flatArray;
            }
        }
    }

    public class RAWExposureData : BaseExposureData {
        private readonly byte[] rawBytes;
        private readonly IRawConverter rawConverter;
        private readonly string rawType;

        public RAWExposureData(
            IRawConverter rawConverter,
            byte[] rawBytes,
            string rawType,
            int bitDepth,
            ImageMetaData metaData)
            : base(bitDepth, metaData) {
            this.rawConverter = rawConverter;
            this.rawBytes = rawBytes;
            this.rawType = rawType;
        }

        public override async Task<IImageData> ToImageData(CancellationToken cancelToken = default) {
            using (var memoryStream = new System.IO.MemoryStream(this.rawBytes)) {
                return await this.rawConverter.Convert(
                    s: memoryStream,
                    rawType: this.rawType,
                    bitDepth: this.BitDepth,
                    metaData: this.MetaData,
                    token: cancelToken);
            }
        }
    }
}
