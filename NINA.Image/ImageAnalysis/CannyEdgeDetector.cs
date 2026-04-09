#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using Accord.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace NINA.Image.ImageAnalysis {
    // Accord bit-exact blurred Canny wrapper for NINA. Gaussian blur stays here, while the shared
    // no-blur Canny core lives in NoBlurCannyEdgeDetector so both variants use the same edge logic.
    public class CannyEdgeDetector : BaseUsingCopyPartialFilter {
        private readonly GaussianBlur gaussianFilter = new GaussianBlur();
        private byte lowThreshold = 20;
        private byte highThreshold = 100;

        private readonly Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();

        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations => formatTranslations;

        public byte LowThreshold {
            get => lowThreshold;
            set => lowThreshold = value;
        }

        public byte HighThreshold {
            get => highThreshold;
            set => highThreshold = value;
        }

        public double GaussianSigma {
            get => gaussianFilter.Sigma;
            set => gaussianFilter.Sigma = value;
        }

        public int GaussianSize {
            get => gaussianFilter.Size;
            set => gaussianFilter.Size = value;
        }

        public CannyEdgeDetector() {
            // initialize format translation dictionary
            formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
        }

        public CannyEdgeDetector(byte lowThreshold, byte highThreshold) : this() {
            this.lowThreshold = lowThreshold;
            this.highThreshold = highThreshold;
        }

        public CannyEdgeDetector(byte lowThreshold, byte highThreshold, double sigma) : this() {
            this.lowThreshold = lowThreshold;
            this.highThreshold = highThreshold;
            gaussianFilter.Sigma = sigma;
        }

        protected override unsafe void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData, Rectangle rect) {
            // This temporary image is created here, so this wrapper owns it and disposes it here.
            using (UnmanagedImage blurredImage = gaussianFilter.Apply(sourceData)) {
                NoBlurCannyEdgeDetector.ProcessFilter(blurredImage, destinationData, rect, lowThreshold, highThreshold);
            }
        }
    }
}
