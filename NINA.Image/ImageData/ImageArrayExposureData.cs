#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Image.ImageData {

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
            ImageMetaData metaData,
            IImageDataFactory imageDataFactory)
            : base(bitDepth, metaData, imageDataFactory) {
            this.imageArray = new ImageArray(input);
            this.Width = width;
            this.Height = height;
            this.IsBayered = isBayered;
        }
        public ImageArrayExposureData(
            int[] input,
            int width,
            int height,
            int bitDepth,
            bool isBayered,
            ImageMetaData metaData,
            IImageDataFactory imageDataFactory)
            : base(bitDepth, metaData, imageDataFactory) {
            this.imageArray = new ImageArrayInt(input);
            this.Width = width;
            this.Height = height;
            this.IsBayered = isBayered;
        }

        public override Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default) {
            return Task.FromResult<IImageData>(
                imageDataFactory.CreateBaseImageData(
                    imageArray: this.imageArray,
                    width: this.Width,
                    height: this.Height,
                    bitDepth: this.BitDepth,
                    isBayered: this.IsBayered,
                    metaData: this.MetaData));
        }

        public static async Task<ImageArrayExposureData> FromBitmapSource(BitmapSource source, IImageDataFactory imageDataFactory) {            
            var pixels = await Task.Run(() => ArrayFromSource(source));
            return new ImageArrayExposureData(
                input: pixels,
                width: source.PixelWidth,
                height: source.PixelHeight,
                bitDepth: source.Format.BitsPerPixel,
                isBayered: false,
                metaData: new ImageMetaData(),
                imageDataFactory: imageDataFactory);
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
}