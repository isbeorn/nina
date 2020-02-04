#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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