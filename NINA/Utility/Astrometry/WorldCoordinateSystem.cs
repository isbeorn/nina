#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Utility.Astrometry {

    /// <summary>
    /// Class to handle world coordinate systems based on tangential projection
    /// </summary>
    /// <remarks>
    /// http://hosting.astro.cornell.edu/~vassilis/isocont/node17.html
    /// https://www.astro.rug.nl/~gipsy/tsk/fitsreproj.dc1
    /// </remarks>
    public class WorldCoordinateSystem {

        /// <summary>
        /// Handles WCS calculations based on CDn_m matrix and tangential projection
        /// </summary>
        /// <param name="crval1">Reference x pixel value</param>
        /// <param name="crval2">Reference y pixel value</param>
        /// <param name="crpix1">Reference pixel x</param>
        /// <param name="crpix2">Reference pixel y</param>
        /// <param name="cd1_1">CDn_m tranformation matrix</param>
        /// <param name="cd1_2">CDn_m tranformation matrix</param>
        /// <param name="cd2_1">CDn_m tranformation matrix</param>
        /// <param name="cd2_2">CDn_m tranformation matrix</param>
        /// <remarks>
        /// http://hosting.astro.cornell.edu/~vassilis/isocont/node17.html
        /// https://www.astro.rug.nl/~gipsy/tsk/fitsreproj.dc1
        /// Vertically and horizontally flipped images don't work as of now
        /// </remarks>
        public WorldCoordinateSystem(
            double crval1,
            double crval2,
            double crpix1,
            double crpix2,
            double cd1_1,
            double cd1_2,
            double cd2_1,
            double cd2_2
        ) {
            Point = new Point(crpix1, crpix2);
            Coordinates = new Coordinates(Angle.ByDegree(crval1), Angle.ByDegree(crval2), Epoch.J2000);

            var determinant = cd1_1 * cd2_2 - cd1_2 * cd2_1;

            var sign = 1;
            if (determinant < 0) {
                sign = -1;
            }

            var cdelta1 = sign * Math.Sqrt(cd1_1 * cd1_1 + cd2_1 * cd2_1);
            var cdelta2 = Math.Sqrt(cd1_2 * cd1_2 + cd2_2 * cd2_2);

            if (cdelta1 >= 0 || cdelta2 < 0) {
                Flipped = true;
            }

            var rot1_cd = Math.Atan2(sign * cd1_2, cd2_2);
            var rot2_cd = Math.Atan2(sign * cd1_1, cd2_1) - Math.PI / 2d;
            var rotation = Astrometry.ToDegree(Flipped ? -rot2_cd : rot2_cd);
            var skew = Astrometry.ToDegree(Math.Abs(rot1_cd - rot2_cd));

            //Approximation as the matrix can account for skewed axes
            Rotation = Astrometry.EuclidianModulus(rotation, 360);

            PixelScaleX = Astrometry.DegreeToArcsec(Math.Abs(cdelta1));
            PixelScaleY = Astrometry.DegreeToArcsec(Math.Abs(cdelta2));
        }

        /// <summary>
        /// Handles WCS calculations based on CROTA2, CDELT and tangential projection
        /// </summary>
        /// <param name="crval1">Reference x pixel value</param>
        /// <param name="crval2">Reference y pixel value</param>
        /// <param name="crpix1">Reference pixel x</param>
        /// <param name="crpix2">Reference pixel y</param>
        /// <param name="cdelta1">per pixel increment along RA</param>
        /// <param name="cdelta2">per pixel increment along DEC</param>
        /// <param name="crota2">Rotation in degrees</param>
        /// <remarks>Vertically and horizontally flipped images don't work as of now</remarks>
        public WorldCoordinateSystem(
            double crval1,
            double crval2,
            double crpix1,
            double crpix2,
            double cdelta1,
            double cdelta2,
            double crota2
        ) {
            Point = new Point(crpix1, crpix2);
            Coordinates = new Coordinates(Angle.ByDegree(crval1), Angle.ByDegree(crval2), Epoch.J2000);

            if (cdelta1 >= 0 || cdelta2 < 0) {
                Flipped = true;
            }

            if (!Flipped) {
                Rotation = Astrometry.EuclidianModulus(crota2, 360);
            } else {
                Rotation = Astrometry.EuclidianModulus(-crota2, 360);
            }

            PixelScaleX = Astrometry.DegreeToArcsec(Math.Abs(cdelta1));
            PixelScaleY = Astrometry.DegreeToArcsec(Math.Abs(cdelta2));
        }

        public Coordinates Coordinates { get; }
        public Point Point { get; }
        public double Rotation { get; }

        public double PixelScaleX { get; }
        public double PixelScaleY { get; }

        /// <summary>
        /// Indicator that either the x xor y axis is flipped and the rotation has been adjusted for it
        /// </summary>
        public bool Flipped { get; }

        /// <summary>
        /// Gets coordinates for a given point based on the WCS
        /// </summary>
        /// <param name="pixelX">x pixel value</param>
        /// <param name="pixelY">y pixel value</param>
        /// <returns>Coordinates at position x|y</returns>
        public Coordinates GetCoordinates(int pixelX, int pixelY) {
            return Coordinates.Shift(pixelX - Point.X, pixelY - Point.Y, Rotation, PixelScaleX, PixelScaleY, Coordinates.ProjectionType.Gnomonic);
        }
    }
}