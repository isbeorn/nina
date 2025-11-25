#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using OpenCvSharp;
using System.Drawing;

namespace Accord.Imaging.Filters {
    /// <summary>
    /// Canny Edge Detector - detects edges using Canny algorithm with Gaussian blur preprocessing
    /// </summary>
    public class CannyEdgeDetector : BaseUsingCopyPartialFilter {
        private byte lowThreshold = 20;
        private byte highThreshold = 100;
        private int gaussianSize = 5;

        /// <summary>
        /// Low threshold.
        /// </summary>
        public byte LowThreshold {
            get => lowThreshold;
            set => lowThreshold = value;
        }

        /// <summary>
        /// High threshold.
        /// </summary>
        public byte HighThreshold {
            get => highThreshold;
            set => highThreshold = value;
        }

        /// <summary>
        /// Size of Gaussian blur kernel (must be odd). Default is 5.
        /// </summary>
        public int GaussianSize {
            get => gaussianSize;
            set => gaussianSize = value % 2 == 0 ? value + 1 : value; // Ensure odd
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CannyEdgeDetector"/> class.
        /// </summary>
        public CannyEdgeDetector() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CannyEdgeDetector"/> class.
        /// </summary>
        /// <param name="lowThreshold">Low threshold.</param>
        /// <param name="highThreshold">High threshold.</param>
        public CannyEdgeDetector(byte lowThreshold, byte highThreshold) : this() {
            this.lowThreshold = lowThreshold;
            this.highThreshold = highThreshold;
        }

        protected override unsafe void ProcessFilter(UnmanagedImage source, UnmanagedImage destination, Rectangle rect) {
            // Convert UnmanagedImage to Mat for OpenCV processing
            Mat srcMat = Mat.FromPixelData(source.Height, source.Width, MatType.CV_8UC1, source.ImageData, source.Stride);
            Mat dstMat = Mat.FromPixelData(destination.Height, destination.Width, MatType.CV_8UC1, destination.ImageData, destination.Stride);

            // Apply Gaussian blur first (this is what differentiates it from NoBlurCannyEdgeDetector)
            Mat blurred = new Mat();
            int kernelSize = gaussianSize > 0 ? gaussianSize : 5;
            Cv2.GaussianBlur(srcMat, blurred, new OpenCvSharp.Size(kernelSize, kernelSize), 1.4);

            // Apply Canny edge detection
            Cv2.Canny(blurred, dstMat, lowThreshold, highThreshold);

            blurred.Dispose();
        }
    }
}
