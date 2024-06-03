#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;
using Math = System.Math;
using Point = System.Windows.Point;

namespace NINA.Astrometry {

    public class ViewportFoV {
        public Coordinates CenterCoordinates { get; private set; }
        public double Width { get; }
        public double Height { get; }
        public double ArcSecWidth { get; }
        public double ArcSecHeight { get; }
        public Point ViewPortCenterPoint { get; }
        public double Rotation { get; }
        public double VFoV { get; }
        public double HFoV { get; }

        public ViewportFoV(Coordinates centerCoordinates, double vFoVDegrees, double width, double height, double rotation) {
            Rotation = rotation;

            Width = width;
            Height = height;

            VFoV = vFoVDegrees;
            HFoV = (vFoVDegrees / height) * width;

            ArcSecWidth = AstroUtil.DegreeToArcsec(HFoV) / Width;
            ArcSecHeight = AstroUtil.DegreeToArcsec(VFoV) / Height;

            CenterCoordinates = centerCoordinates;

            ViewPortCenterPoint = new Point(width / 2, height / 2);
        }

        public bool ContainsCoordinates(Coordinates coordinates) {
            var distance = coordinates - CenterCoordinates;
            return distance.Distance.Degree < Math.Max(HFoV, VFoV);
        }

        public bool ContainsCoordinates(double ra, double dec) {
            return ContainsCoordinates(new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees));
        }

        public void Shift(Vector delta) {
            if (delta.X == 0 && delta.Y == 0) {
                return;
            }

            CenterCoordinates = CenterCoordinates.Shift(delta.X, delta.Y, Rotation, ArcSecWidth, ArcSecHeight);
        }
    }
}