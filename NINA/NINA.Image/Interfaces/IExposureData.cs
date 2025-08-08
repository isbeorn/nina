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
using NINA.Core.Model;
using NINA.Image.ImageData;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Image.Interfaces {

    public interface IExposureData {
        int BitDepth { get; }
        ImageMetaData MetaData { get; }

        Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default);
    }

    public interface IExposureDataFactory {

        CachedExposureData CreateCachedExposureData(IImageData imageData);

        Flipped2DExposureData CreateFlipped2DExposureData(Array flipped2DArray, int bitDepth, bool isBayered, ImageMetaData metaData);

        RAWExposureData CreateRAWExposureData(RawConverterEnum converter, byte[] rawBytes, string rawType, int bitDepth, ImageMetaData metaData);

        ImageArrayExposureData CreateImageArrayExposureData(ushort[] input, int width, int height, int bitDepth, bool isBayered, ImageMetaData metaData);

        Task<ImageArrayExposureData> CreateImageArrayExposureDataFromBitmapSource(BitmapSource source);

        Task<IRenderedImage> CreateRenderedImageFromBitmapSource(BitmapSource source, bool calculateStatistics = false);
    }
}