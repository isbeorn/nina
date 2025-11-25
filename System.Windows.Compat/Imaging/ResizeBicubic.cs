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

namespace System.Drawing {
    /// <summary>
    /// Bicubic image resizing using OpenCV
    /// </summary>
    public class ResizeBicubic {
        private readonly int _width;
        private readonly int _height;

        public ResizeBicubic(int width, int height) {
            _width = width;
            _height = height;
        }

        public Mat Apply(Mat image) {
            var resized = new Mat();
            Cv2.Resize(image, resized, new OpenCvSharp.Size(_width, _height), 0, 0, InterpolationFlags.Cubic);
            return resized;
        }
    }
}
