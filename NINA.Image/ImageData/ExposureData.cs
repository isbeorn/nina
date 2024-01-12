#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Core.Locale;
using NINA.Image.Interfaces;
using System.Windows.Media.Imaging;
using NINA.Core.Enum;
using NINA.Image.RawConverter;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.Core.Interfaces;

namespace NINA.Image.ImageData {

    public abstract class BaseExposureData : IExposureData {
        protected readonly IImageDataFactory imageDataFactory;

        public int BitDepth { get; private set; }
        public ImageMetaData MetaData { get; private set; }

        protected BaseExposureData(int bitDepth, ImageMetaData metadata, IImageDataFactory imageDataFactory) {
            this.BitDepth = bitDepth;
            this.MetaData = metadata;
            this.imageDataFactory = imageDataFactory;
        }

        public abstract Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default);
    }

    public class CachedExposureData : BaseExposureData {
        private readonly IImageData imageData;

        public CachedExposureData(IImageData imageData, IImageDataFactory imageDataFactory)
            : base(imageData.Properties.BitDepth, imageData.MetaData, imageDataFactory) {
            this.imageData = imageData;
        }

        public override Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default) {
            return Task.FromResult(this.imageData);
        }
    }

    public class Flipped2DExposureData : BaseExposureData {
        private readonly Array flipped2DArray;
        public bool IsBayered { get; private set; }
        public bool Create32BitData { get; }

        public Flipped2DExposureData(
            Array flipped2DArray,
            int bitDepth,
            bool isBayered,
            ImageMetaData metaData,
            IImageDataFactory imageDataFactory,
            bool create32BitData = false)
            : base(bitDepth, metaData, imageDataFactory) {
            if (flipped2DArray.Rank > 2) { throw new NotSupportedException(); }
            this.flipped2DArray = flipped2DArray;
            this.IsBayered = isBayered;
            Create32BitData = create32BitData;
        }

        public override async Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default) {
            try {
                progress?.Report(new ApplicationStatus { Status = Loc.Instance["LblPrepareExposure"] });

                if(this.Create32BitData && this.flipped2DArray.GetType() == typeof(int[,])) {
                    var flatArray = await Task.Run(() => FilpAndProcessAsInt((int[,])this.flipped2DArray, this.flipped2DArray.GetLength(0), this.flipped2DArray.GetLength(1), (int)(this.flipped2DArray.GetLength(0) * this.flipped2DArray.GetLength(1))));
                    return imageDataFactory.CreateBaseImageData(
                        imageArray: new ImageArrayInt(flatArray),
                        width: this.flipped2DArray.GetLength(0),
                        height: this.flipped2DArray.GetLength(1),
                        bitDepth: this.BitDepth,
                        isBayered: this.IsBayered,
                        metaData: this.MetaData);
                } else {
                    var flatArray = await Task.Run(() => FlipAndConvert2d(this.flipped2DArray), cancelToken);
                    return imageDataFactory.CreateBaseImageData(
                        imageArray: new ImageArray(flatArray),
                        width: this.flipped2DArray.GetLength(0),
                        height: this.flipped2DArray.GetLength(1),
                        bitDepth: this.BitDepth,
                        isBayered: this.IsBayered,
                        metaData: this.MetaData);
                }
            } finally {
                progress?.Report(new ApplicationStatus { Status = string.Empty });
            }
        }

        private static ushort[] ProcessAsByte(byte[,] arr, int width, int height, int length) {
            ushort[] flatArray = new ushort[length];
            ushort value;
            unsafe {
                fixed (byte* ptr = arr) {
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

        private static ushort[] ProcessAsShort(short[,] arr, int width, int height, int length) {
            ushort[] flatArray = new ushort[length];
            ushort value;
            unsafe {
                fixed (short* ptr = arr) {
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

        private static ushort[] ProcessAsUShort(ushort[,] arr, int width, int height, int length) {
            ushort[] flatArray = new ushort[length];
            ushort value;
            unsafe {
                fixed (ushort* ptr = arr) {
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

        private static ushort[] ProcessAsUInt(uint[,] arr, int width, int height, int length) {
            ushort[] flatArray = new ushort[length];
            ushort value;
            unsafe {
                fixed (uint* ptr = arr) {
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
        private static ushort[] ProcessAsInt(int[,] arr, int width, int height, int length) {
            ushort[] flatArray = new ushort[length];
            ushort value;
            unsafe {
                fixed (int* ptr = arr) {
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

        private static ushort[] FlipAndConvert2d(Array input) {
            using (MyStopWatch.Measure("FlipAndConvert2d")) {
                int width = input.GetLength(0);
                int height = input.GetLength(1);
                int length = width * height;
                ushort[] flatArray = new ushort[length];

                if (input.GetType() == typeof(byte[,])) {
                    return ProcessAsByte((byte[,])input, width, height, length);
                } else if (input.GetType() == typeof(short[,])) {
                    return ProcessAsShort((short[,])input, width, height, length);
                } else if (input.GetType() == typeof(ushort[,])) {
                    return ProcessAsUShort((ushort[,])input, width, height, length);
                } else if (input.GetType() == typeof(uint[,])) {
                    return ProcessAsUInt((uint[,])input, width, height, length);
                } else if (input.GetType() == typeof(int[,])) {
                    return ProcessAsInt((int[,])input, width, height, length);
                } else {
                    throw new NotSupportedException("Unsupported data type");
                }
            }
        }
        private static int[] FilpAndProcessAsInt(int[,] arr, int width, int height, int length) {
            int[] flatArray = new int[length];
            int value;
            unsafe {
                fixed (int* ptr = arr) {
                    int idx = 0, row = 0;
                    for (int i = 0; i < length; i++) {
                        value = (int)ptr[i];

                        idx = ((i % height) * width) + row;
                        if ((i % (height)) == (height - 1)) row++;

                        int b = value;
                        flatArray[idx] = b;
                    }
                }
            }
            return flatArray;
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
            ImageMetaData metaData,
            IImageDataFactory imageDataFactory)
            : base(bitDepth, metaData, imageDataFactory) {
            this.rawConverter = rawConverter;
            this.rawBytes = rawBytes;
            this.rawType = rawType;
        }

        public override async Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default) {
            try {
                progress?.Report(new ApplicationStatus { Status = Loc.Instance["LblPrepareExposure"] });
                using (var memoryStream = new System.IO.MemoryStream(this.rawBytes)) {
                    var data = await this.rawConverter.Convert(
                        s: memoryStream,
                        rawType: this.rawType,
                        bitDepth: this.BitDepth,
                        metaData: this.MetaData,
                        token: cancelToken);
                    return data;
                }
            } finally {
                progress?.Report(new ApplicationStatus { Status = string.Empty });
            }
        }
    }

    public class ExposureDataFactory : IExposureDataFactory {
        protected readonly IImageDataFactory imageDataFactory;
        protected readonly IProfileService profileService;
        protected readonly IPluggableBehaviorSelector<IStarDetection> starDetectionSelector;
        protected readonly IPluggableBehaviorSelector<IStarAnnotator> starAnnotatorSelector;

        public ExposureDataFactory(IImageDataFactory imageDataFactory, IProfileService profileService, IPluggableBehaviorSelector<IStarDetection> starDetectionSelector, IPluggableBehaviorSelector<IStarAnnotator> starAnnotatorSelector) {
            this.imageDataFactory = imageDataFactory;
            this.profileService = profileService;
            this.starDetectionSelector = starDetectionSelector;
            this.starAnnotatorSelector = starAnnotatorSelector;
        }

        public CachedExposureData CreateCachedExposureData(IImageData imageData) {
            return new CachedExposureData(imageData, imageDataFactory);
        }

        public Flipped2DExposureData CreateFlipped2DExposureData(Array flipped2DArray, int bitDepth, bool isBayered, ImageMetaData metaData) {
            return new Flipped2DExposureData(flipped2DArray, bitDepth, isBayered, metaData, imageDataFactory, profileService.ActiveProfile.CameraSettings.ASCOMCreate32BitData);
        }

        public RAWExposureData CreateRAWExposureData(RawConverterEnum converter, byte[] rawBytes, string rawType, int bitDepth, ImageMetaData metaData) {
            return new RAWExposureData(RawConverterFactory.CreateInstance(converter, imageDataFactory), rawBytes, rawType, bitDepth, metaData, imageDataFactory);
        }

        public ImageArrayExposureData CreateImageArrayExposureData(ushort[] input, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) {
            return new ImageArrayExposureData(input, width, height, bitDepth, isBayered, metaData, imageDataFactory);
        }
        public ImageArrayExposureData CreateImageArrayExposureDataInt(int[] input, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData) {
            return new ImageArrayExposureData(input, width, height, bitDepth, isBayered, metaData, imageDataFactory);
        }

        public Task<ImageArrayExposureData> CreateImageArrayExposureDataFromBitmapSource(BitmapSource source) {
            return ImageArrayExposureData.FromBitmapSource(source, imageDataFactory);
        }

        public Task<IRenderedImage> CreateRenderedImageFromBitmapSource(BitmapSource source, bool calculateStatistics = false) {
            return RenderedImage.FromBitmapSource(source, this, profileService, starDetectionSelector.GetBehavior(), starAnnotatorSelector.GetBehavior(), calculateStatistics);
        }
    }
}