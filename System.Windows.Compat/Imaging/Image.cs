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
using System.Drawing;

namespace Accord.Imaging {
    /// <summary>
    /// Image utility methods for Accord.Imaging compatibility
    /// </summary>
    public static class Image {
        /// <summary>
        /// Converts a 16-bit grayscale bitmap to 8-bit
        /// </summary>
        public static Bitmap Convert16bppTo8bpp(Bitmap bitmap) {
            Mat srcMat = bitmap;
            Mat dstMat = new Mat();
            
            // If already 8-bit, just return a copy
            if (srcMat.Depth() == MatType.CV_8U) {
                srcMat.CopyTo(dstMat);
                return new Bitmap(dstMat);
            }
            
            // Convert 16-bit to 8-bit with scaling
            // Scale from 16-bit range [0, 65535] to 8-bit range [0, 255]
            srcMat.ConvertTo(dstMat, MatType.CV_8UC1, 1.0 / 256.0);
            
            return new Bitmap(dstMat);
        }
    }
}
