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
using NINA.Utility.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.ImageData {

    public interface IRenderedImage {
        IImageData RawImageData { get; }

        BitmapSource Image { get; }

        IDebayeredImage Debayer(bool saveColorChannels = false, bool saveLumChannel = false, SensorType bayerPattern = SensorType.RGGB);

        IRenderedImage ReRender();

        Task<IRenderedImage> Stretch(double factor, double blackClipping, bool unlinked);

        Task<IRenderedImage> DetectStars(
            bool annotateImage,
            StarSensitivityEnum sensitivity,
            NoiseReductionEnum noiseReduction,
            CancellationToken cancelToken = default,
            IProgress<ApplicationStatus> progress = default(Progress<ApplicationStatus>));

        Task<BitmapSource> GetThumbnail();
    }
}