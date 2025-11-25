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

namespace Accord.Imaging.Filters {
    /// <summary>
    /// Brightness correction filter - adjusts image brightness
    /// </summary>
    public class BrightnessCorrection {
        private int adjustValue;

        public BrightnessCorrection(int adjustValue) {
            this.adjustValue = adjustValue;
        }

        public void ApplyInPlace(Bitmap image) {
            Mat mat = image;
            if (mat == null || mat.Empty()) return;

            // Add brightness value to all pixels
            mat.ConvertTo(mat, mat.Type(), 1.0, adjustValue);
        }
    }

    /// <summary>
    /// Contrast correction filter - adjusts image contrast
    /// </summary>
    public class ContrastCorrection {
        private int contrastFactor;

        public ContrastCorrection(int contrastFactor) {
            this.contrastFactor = contrastFactor;
        }

        public void ApplyInPlace(Bitmap image) {
            Mat mat = image;
            if (mat == null || mat.Empty()) return;

            // Contrast adjustment: pixel = (pixel - 128) * factor/128 + 128
            // Simplified: scale around 128 (middle gray)
            double factor = contrastFactor / 128.0;
            mat.ConvertTo(mat, mat.Type(), factor, 128 * (1 - factor));
        }
    }
}
