#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.ViewModel.FramingAssistant;
using System;
using System.Windows;
using System.Xml.Serialization;

namespace NINA.Utility.Astrometry {

    [Serializable()]
    [XmlRoot(nameof(Coordinates))]
    public class Coordinates {

        private Coordinates() {
        }

        public enum RAType {
            Degrees,
            Hours
        }

        /// <summary>
        /// Right Ascension in hours
        /// </summary>
        [XmlElement(nameof(RA))]
        public double RA { get; set; }

        [XmlIgnore]
        public string RAString {
            get {
                return Astrometry.DegreesToHMS(RADegrees);
            }
        }

        /// <summary>
        /// Right Ascension in degrees
        /// </summary>
        public double RADegrees {
            get {
                return RA * 360 / 24;
            }
        }

        /// <summary>
        /// Declination in Degrees
        /// </summary>
        [XmlElement(nameof(Dec))]
        public double Dec { get; set; }

        [XmlIgnore]
        public string DecString {
            get {
                return Astrometry.DegreesToDMS(Dec);
            }
        }

        /// <summary>
        /// Epoch the coordinates are stored in. Either J2000 or JNOW
        /// </summary>
        [XmlElement(nameof(Epoch))]
        public Epoch Epoch { get; set; }

        /// <summary>
        /// Creates new coordinates
        /// </summary>
        /// <param name="ra">    Right Ascension in degrees or hours. RAType has to be set accordingly</param>
        /// <param name="dec">   Declination in degrees</param>
        /// <param name="epoch"> J2000|JNOW</param>
        /// <param name="ratype">Degrees|Hours</param>
        public Coordinates(double ra, double dec, Epoch epoch, RAType ratype) {
            this.RA = ra;
            this.Dec = dec;
            this.Epoch = epoch;

            if (ratype == RAType.Degrees) {
                this.RA = (this.RA * 24) / 360;
            }
        }

        /// <summary>
        /// Converts from one Epoch into another.
        /// </summary>
        /// <param name="targetEpoch"></param>
        /// <returns></returns>
        public Coordinates Transform(Epoch targetEpoch) {
            if (Epoch == targetEpoch) {
                return new Coordinates(this.RA, this.Dec, this.Epoch, RAType.Hours);
            }

            if (targetEpoch == Epoch.JNOW) {
                return TransformToJNOW();
            } else if (targetEpoch == Epoch.J2000) {
                return TransformToJ2000();
            } else {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Transforms coordinates from J2000 to JNOW
        /// </summary>
        /// <returns></returns>
        private Coordinates TransformToJNOW() {
            double jdTT = GetJdTTNow();

            double ri = 0, di = 0, eo = 0;
            SOFA.CelestialToIntermediate(Astrometry.ToRadians(RADegrees), Astrometry.ToRadians(Dec), 0.0, 0.0, 0.0, 0.0, jdTT, 0.0, ref ri, ref di, ref eo);

            double raApparent = Astrometry.ToDegree(SOFA.Anp(ri - eo));
            double decApparent = Astrometry.ToDegree(di);

            return new Coordinates(raApparent, decApparent, Epoch.JNOW, RAType.Degrees);
        }

        private double GetJdTTNow() {
            var utcNow = DateTime.UtcNow;
            double utc1 = 0, utc2 = 0, tai1 = 0, tai2 = 0, tt1 = 0, tt2 = 0;
            GetJdUTCNow(ref utc1, ref utc2);
            SOFA.UtcTai(utc1, utc2, ref tai1, ref tai2);
            SOFA.TaiTt(tai1, tai2, ref tt1, ref tt2);

            return tt1 + tt2;
        }

        private void GetJdUTCNow(ref double utc1, ref double utc2) {
            var utcNow = DateTime.UtcNow;
            SOFA.Dtf2d("UTC", utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, (double)utcNow.Second + (double)utcNow.Millisecond / 1000.0, ref utc1, ref utc2);
        }

        private double GetJdUTCNow() {
            var utcNow = DateTime.UtcNow;
            double utc1 = 0, utc2 = 0;
            GetJdUTCNow(ref utc1, ref utc2);
            return utc1 + utc2;
        }

        /// <summary>
        /// Transforms coordinates from JNOW to J2000
        /// </summary>
        /// <returns></returns>
        private Coordinates TransformToJ2000() {
            var jdTT = GetJdTTNow();
            var jdUTC = GetJdUTCNow();
            double rc = 0, dc = 0, eo = 0;
            SOFA.IntermediateToCelestial(SOFA.Anp(Astrometry.ToRadians(RADegrees) + SOFA.Eo06a(jdUTC, 0.0)), Astrometry.ToRadians(Dec), jdTT, 0.0, ref rc, ref dc, ref eo);

            var raCelestial = Astrometry.ToDegree(rc);
            var decCelestial = Astrometry.ToDegree(dc);

            return new Coordinates(raCelestial, decCelestial, Epoch.J2000, RAType.Degrees);
        }

        /// <summary>
        /// Shift coordinates by a delta in degree
        /// </summary>
        /// <param name="deltaX">delta x in degree</param>
        /// <param name="deltaY">delta y in degree</param>
        /// <param name="rotation">rotation relative to delta values</param>
        /// <returns></returns>
        public Coordinates Shift(double deltaX, double deltaY, double rotation) {
            var deltaXDeg = -deltaX;
            var deltaYDeg = -deltaY;
            var rotationRad = Astrometry.ToRadians(rotation);

            if (rotation != 0) {
                //Recalculate delta based on rotation
                //No spherical or other aberrations are assumed
                var originalDeltaX = deltaXDeg;
                deltaXDeg = deltaXDeg * Math.Cos(rotationRad) - deltaYDeg * Math.Sin(rotationRad);
                deltaYDeg = deltaYDeg * Math.Cos(rotationRad) + originalDeltaX * Math.Sin(rotationRad);
            }

            var originRARad = Astrometry.ToRadians(this.RADegrees);
            var originDecRad = Astrometry.ToRadians(this.Dec);

            var deltaXRad = Astrometry.ToRadians(deltaXDeg);
            var deltaYRad = Astrometry.ToRadians(deltaYDeg);

            // refer to http://faculty.wcas.northwestern.edu/nchapman/coding/worldpos.py

            var targetRARad = originRARad + Math.Atan2(deltaXRad, Math.Cos(originDecRad) - deltaYRad * Math.Sin(originDecRad));
            var targetDecRad =
                Math.Atan(
                    Math.Cos(targetRARad - originRARad)
                    * (deltaYRad * Math.Cos(originDecRad) + Math.Sin(originDecRad))
                    / (Math.Cos(originDecRad) - deltaYRad * Math.Sin(originDecRad))
                );

            var targetRA = Astrometry.ToDegree(targetRARad);
            if (targetRA < 0) { targetRA += 360; }
            if (targetRA >= 360) { targetRA -= 360; }

            var targetDec = Astrometry.ToDegree(targetDecRad);

            return new Coordinates(
                targetRA,
                targetDec,
                Epoch.J2000,
                Coordinates.RAType.Degrees
            );
        }

        public Point ProjectFromCenterToXY(ViewportFoV viewPort) {
            return GnomonicTanProjection(viewPort.CenterCoordinates, viewPort.ViewPortCenterPoint, viewPort.ArcSecWidth,
                viewPort.ArcSecHeight, viewPort.Rotation);
        }

        /// <summary>
        /// Generates a Point with relative X/Y values for centering the current coordinates relative to a given point using a tangential gnomonic projection.
        /// </summary>
        /// <param name="center">Center coordinates of the image</param>
        /// <param name="centerPointPixels">Center point in pixels of the image</param>
        /// <param name="horizResArcSecPx">Horizontal resolution in ArcSec/Px</param>
        /// <param name="vertResArcSecPix">Vertical resolution in ArcSec/Px</param>
        /// <param name="rotation">Rotation in degrees</param>
        /// <returns></returns>
        public Point GnomonicTanProjection(Coordinates center, Point centerPointPixels, double horizResArcSecPx, double vertResArcSecPix, double rotation) {
            var raDegreesSanitized = RADegrees;
            var deltaRa = (raDegreesSanitized - center.RADegrees);
            if (deltaRa > 180) {
                raDegreesSanitized -= 360;
            }

            if (deltaRa < -180) {
                raDegreesSanitized += 360;
            }

            var centerRaRad = Astrometry.ToRadians(center.RADegrees);
            var centerDegRad = Astrometry.ToRadians(center.Dec);
            var targetRaRad = Astrometry.ToRadians(raDegreesSanitized);
            var targetDecRad = Astrometry.ToRadians(Dec);
            var imageRotationRad = Astrometry.ToRadians(rotation);

            // _xypix -tan projection
            // refer to http://faculty.wcas.northwestern.edu/nchapman/coding/worldpos.py

            var modDivisor = (Math.Sin(targetDecRad) * Math.Sin(centerDegRad) + Math.Cos(targetDecRad) *
                              Math.Cos(centerDegRad) * Math.Cos(targetRaRad - centerRaRad));

            var raModRad = (Math.Sin(targetRaRad - centerRaRad) * Math.Cos(targetDecRad)) /
                           modDivisor;
            var decModRad = (Math.Sin(targetDecRad) * Math.Cos(centerDegRad) - Math.Cos(targetDecRad) *
                             Math.Sin(centerDegRad) * Math.Cos(targetRaRad - centerRaRad)) /
                            modDivisor;

            double deltaXDegrees;
            double deltaYDegrees;

            if (imageRotationRad != 0) {
                deltaXDegrees = Astrometry.ToDegree(raModRad) * Math.Cos(imageRotationRad) +
                                    Astrometry.ToDegree(decModRad) * Math.Sin(imageRotationRad);
                deltaYDegrees = Astrometry.ToDegree(decModRad) * Math.Cos(imageRotationRad) -
                                    Astrometry.ToDegree(raModRad) * Math.Sin(imageRotationRad);
            } else {
                deltaXDegrees = Astrometry.ToDegree(raModRad);
                deltaYDegrees = Astrometry.ToDegree(decModRad);
            }

            return new Point(centerPointPixels.X - Astrometry.DegreeToArcsec(deltaXDegrees) / horizResArcSecPx,
                centerPointPixels.Y - Astrometry.DegreeToArcsec(deltaYDegrees) / vertResArcSecPix);
        }

        /// <summary>
        /// Shift coordinates by a delta in pixel
        /// </summary>
        /// <param name="origin">Coordinates to shift from</param>
        /// <param name="deltaX">delta x</param>
        /// <param name="deltaY">delta y</param>
        /// <param name="rotation">rotation relative to delta values</param>
        /// <param name="scaleX">scale relative to deltaX in arcsecs</param>
        /// <param name="scaleY">scale raltive to deltaY in arcsecs</param>
        /// <returns></returns>
        public Coordinates Shift(
                double deltaX,
                double deltaY,
                double rotation,
                double scaleX,
                double scaleY
        ) {
            var deltaXDeg = deltaX * Astrometry.ArcsecToDegree(scaleX);
            var deltaYDeg = deltaY * Astrometry.ArcsecToDegree(scaleY);
            return this.Shift(deltaXDeg, deltaYDeg, rotation);
        }
    }
}