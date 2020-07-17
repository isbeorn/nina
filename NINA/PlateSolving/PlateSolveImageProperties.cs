#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.ImageData;
using NINA.Utility.Astrometry;

namespace NINA.PlateSolving {

    internal class PlateSolveImageProperties {
        public double FocalLength { get; private set; }
        public double PixelSize { get; private set; }
        public double ImageWidth { get; private set; }
        public double ImageHeight { get; private set; }

        public double ArcSecPerPixel {
            get {
                return Astrometry.ArcsecPerPixel(PixelSize, FocalLength);
            }
        }

        public double FoVH {
            get {
                return Astrometry.ArcminToDegree(Astrometry.FieldOfView(ArcSecPerPixel, ImageHeight));
            }
        }

        public double FoVW {
            get {
                return Astrometry.ArcminToDegree(Astrometry.FieldOfView(ArcSecPerPixel, ImageWidth));
            }
        }

        public static PlateSolveImageProperties Create(PlateSolveParameter parameter, IImageData source) {
            return new PlateSolveImageProperties() {
                FocalLength = parameter.FocalLength,
                PixelSize = parameter.PixelSize,
                ImageWidth = source.Properties.Width,
                ImageHeight = source.Properties.Height
            };
        }
    }
}