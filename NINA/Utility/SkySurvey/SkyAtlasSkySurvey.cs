#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility.SkySurvey {

    internal class SkyAtlasSkySurvey : ISkySurvey {

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            byte[] arr = new byte[width * height];
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = 30;
            }

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256, arr, width);

            bitmap.Freeze();

            return new SkySurveyImage {
                Name = name,
                Source = nameof(SkyAtlasSkySurvey),
                Image = bitmap,
                FoVHeight = fieldOfView,
                FoVWidth = ((double)width / height) * fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }
    }
}
