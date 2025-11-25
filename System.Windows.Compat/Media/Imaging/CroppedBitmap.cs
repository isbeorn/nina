#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OpenCvSharp;
using System.Windows;

namespace System.Windows.Media.Imaging {
    /// <summary>
    /// CroppedBitmap - crops a rectangular region from a bitmap using OpenCV
    /// </summary>
    public class CroppedBitmap : BitmapSource {
        public CroppedBitmap(BitmapSource source, Int32Rect sourceRect) : base() {
            if (source == null) {
                return;
            }

            Mat sourceMat = source;

            // Create OpenCV Rect from Int32Rect
            OpenCvSharp.Rect cvRect = new OpenCvSharp.Rect(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);

            // Ensure the crop rectangle is within bounds
            cvRect = cvRect.Intersect(new OpenCvSharp.Rect(0, 0, sourceMat.Width, sourceMat.Height));

            // Crop the Mat
            Mat croppedMat = new Mat(sourceMat, cvRect);

            _mat = croppedMat;
        }
    }
}
