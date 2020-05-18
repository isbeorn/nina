#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.ImageData {

    public interface IImageData {
        IImageArray Data { get; }

        ImageProperties Properties { get; }

        Nito.AsyncEx.AsyncLazy<IImageStatistics> Statistics { get; }

        IStarDetectionAnalysis StarDetectionAnalysis { get; }

        ImageMetaData MetaData { get; }

        IRenderedImage RenderImage();

        BitmapSource RenderBitmapSource();

        Task<string> SaveToDisk(FileSaveInfo fileSaveInfo, CancellationToken cancelToken = default, bool forceFileType = false);

        Task<string> PrepareSave(FileSaveInfo fileSaveInfo, CancellationToken cancelToken = default);

        string FinalizeSave(string file, string pattern);
    }
}
