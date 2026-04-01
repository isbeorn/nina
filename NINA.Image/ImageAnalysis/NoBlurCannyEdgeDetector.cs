#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Accord.Imaging;
using Accord.Imaging.Filters;
using System.Threading.Tasks;

namespace NINA.Image.ImageAnalysis {
    /// <summary>
    /// Legacy no-blur Canny wrapper that reuses the shared optimized core. It skips the Gaussian stage
    /// entirely, but otherwise preserves the old thresholding and hysteresis behavior.
    /// </summary>
    public class NoBlurCannyEdgeDetector : BaseUsingCopyPartialFilter {
        private byte lowThreshold = 20;
        private byte highThreshold = 100;

        // private format translation dictionary
        private readonly Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations => formatTranslations;

        /// <summary>
        /// Low threshold.
        /// </summary>
        ///
        /// <remarks><para>Low threshold value used for hysteresis
        /// (see  <a href="http://www.pages.drexel.edu/~weg22/can_tut.html">tutorial</a>
        /// for more information).</para>
        ///
        /// <para>Default value is set to <b>20</b>.</para>
        /// </remarks>
        ///
        public byte LowThreshold {
            get => lowThreshold;
            set => lowThreshold = value;
        }

        /// <summary>
        /// High threshold.
        /// </summary>
        ///
        /// <remarks><para>High threshold value used for hysteresis
        /// (see  <a href="http://www.pages.drexel.edu/~weg22/can_tut.html">tutorial</a>
        /// for more information).</para>
        ///
        /// <para>Default value is set to <b>100</b>.</para>
        /// </remarks>
        ///
        public byte HighThreshold {
            get => highThreshold;
            set => highThreshold = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CannyEdgeDetector"/> class.
        /// </summary>
        ///
        public NoBlurCannyEdgeDetector() {
            // initialize format translation dictionary
            formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CannyEdgeDetector"/> class.
        /// </summary>
        ///
        /// <param name="lowThreshold">Low threshold.</param>
        /// <param name="highThreshold">High threshold.</param>
        ///
        public NoBlurCannyEdgeDetector(byte lowThreshold, byte highThreshold) : this() {
            this.lowThreshold = lowThreshold;
            this.highThreshold = highThreshold;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        ///
        /// <param name="source">Source image data.</param>
        /// <param name="destination">Destination image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        ///
        protected override unsafe void ProcessFilter(UnmanagedImage source, UnmanagedImage destination, Rectangle rect) {
            CannyEdgeDetectorCore.Process(source, destination, rect, lowThreshold, highThreshold);
            source.Dispose();
        }
    }
}
