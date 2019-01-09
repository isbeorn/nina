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

        private Angle raAngle;
        private Angle decAngle;

        /// <summary>
        /// Right Ascension in hours
        /// </summary>
        [XmlElement(nameof(RA))]
        public double RA {
            get => raAngle.Hours;
            set {
                raAngle = Angle.ByHours(value);
            }
        }

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
                return raAngle.Degree;
            }
        }

        /// <summary>
        /// Declination in Degrees
        /// </summary>
        [XmlElement(nameof(Dec))]
        public double Dec {
            get => decAngle.Degree;
            set {
                decAngle = Angle.ByDegree(value);
            }
        }

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
        public Coordinates(double ra, double dec, Epoch epoch, RAType ratype)
            : this(
                  ratype == RAType.Hours
                    ? Angle.ByHours(ra)
                    : Angle.ByDegree(ra),
                  Angle.ByDegree(dec),
                  epoch
            ) {
        }

        /// <summary>
        /// Creates new coordinates
        /// </summary>
        /// <param name="ra">    Right Ascension</param>
        /// <param name="dec">   Declination</param>
        /// <param name="epoch"> J2000|JNOW</param>
        public Coordinates(Angle ra, Angle dec, Epoch epoch) {
            this.raAngle = ra;
            this.decAngle = dec;
            this.Epoch = epoch;
        }

        /// <summary>
        /// Converts from one Epoch into another.
        /// </summary>
        /// <param name="targetEpoch"></param>
        /// <returns></returns>
        public Coordinates Transform(Epoch targetEpoch) {
            if (Epoch == targetEpoch) {
                return new Coordinates(this.raAngle, this.decAngle, this.Epoch);
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
            SOFA.CelestialToIntermediate(raAngle.Radians, decAngle.Radians, 0.0, 0.0, 0.0, 0.0, jdTT, 0.0, ref ri, ref di, ref eo);

            var raApparent = Angle.ByRadians(SOFA.Anp(ri - eo));
            var decApparent = Angle.ByRadians(di);

            return new Coordinates(raApparent, decApparent, Epoch.JNOW);
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
            SOFA.IntermediateToCelestial(SOFA.Anp(raAngle.Radians + SOFA.Eo06a(jdUTC, 0.0)), decAngle.Radians, jdTT, 0.0, ref rc, ref dc, ref eo);

            var raCelestial = Angle.ByRadians(rc);
            var decCelestial = Angle.ByRadians(dc);

            return new Coordinates(raCelestial, decCelestial, Epoch.J2000);
        }

        /// <summary>
        /// Shift coordinates by a delta in degree
        /// </summary>
        /// <param name="deltaX">delta x in degree</param>
        /// <param name="deltaY">delta y in degree</param>
        /// <param name="rotation">rotation relative to delta values</param>
        /// <returns></returns>
        /// <remarks>
        ///     based on http://faculty.wcas.northwestern.edu/nchapman/coding/worldpos.py
        /// </remarks>
        public Coordinates Shift(double deltaX, double deltaY, double rotation) {
            var deltaXAngle = Angle.ByDegree(-deltaX);
            var deltaYAngle = Angle.ByDegree(-deltaY);

            var rotationAngle = Angle.ByDegree(rotation);

            if (rotationAngle.Degree != 0) {
                //Recalculate delta based on rotation
                //No spherical or other aberrations are assumed
                var originalDeltaX = deltaXAngle;
                var rotationAngleSin = rotationAngle.Sin();
                var rotationAngleCos = rotationAngle.Cos();
                deltaXAngle = deltaXAngle * rotationAngleCos - deltaYAngle * rotationAngleSin;
                deltaYAngle = deltaYAngle * rotationAngleCos + originalDeltaX * rotationAngleSin;
            }

            var originRA = this.raAngle;

            var originDec = this.decAngle;
            var originDecSin = originDec.Sin();
            var originDecCos = originDec.Cos();

            var targetRA = originRA + Angle.Atan2(deltaXAngle, originDecCos - deltaYAngle * originDecSin);

            var targetDec = (
                (targetRA - originRA).Cos()
                * (deltaYAngle * originDecCos + originDecSin)
                / (originDecCos - deltaYAngle * originDecSin)
            ).Atan();

            if (targetRA.Degree < 0) { targetRA = Angle.ByDegree(targetRA.Degree + 360); }
            if (targetRA.Degree >= 360) { targetRA = Angle.ByDegree(targetRA.Degree - 360); }

            return new Coordinates(
                targetRA,
                targetDec,
                Epoch
            );
        }

        public Point GnomonicTanProjection(ViewportFoV viewPort) {
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
        /// <remarks>
        ///     based on http://faculty.wcas.northwestern.edu/nchapman/coding/worldpos.py
        /// </remarks>
        public Point GnomonicTanProjection(Coordinates center, Point centerPointPixels, double horizResArcSecPx, double vertResArcSecPix, double rotation) {
            var raDegreesSanitized = RADegrees;
            var deltaRa = (raDegreesSanitized - center.RADegrees);
            if (deltaRa > 180) {
                raDegreesSanitized -= 360;
            }

            if (deltaRa < -180) {
                raDegreesSanitized += 360;
            }

            var centerRa = Angle.ByDegree(center.RADegrees);
            var centerDec = Angle.ByDegree(center.Dec);
            var targetRa = Angle.ByDegree(raDegreesSanitized);
            var targetDec = decAngle;
            var imageRotation = Angle.ByDegree(rotation);

            var targetDecSin = targetDec.Sin();
            var targetDecCos = targetDec.Cos();

            var centerDegSin = centerDec.Sin();
            var centerDegCos = centerDec.Cos();

            var substraction = targetRa - centerRa;
            var targetRaSubCenterRaSin = substraction.Sin();
            var targetRaSubCenterRaCos = substraction.Cos();

            var imageRotationSin = imageRotation.Sin();
            var imageRotationCos = imageRotation.Cos();

            var modDivisor = (targetDecSin * centerDegSin + targetDecCos * centerDegCos * targetRaSubCenterRaCos);

            var raMod = (targetRaSubCenterRaSin * targetDecCos) / modDivisor;
            var decMod = (targetDecSin * centerDegCos - targetDecCos * centerDegSin * targetRaSubCenterRaCos) / modDivisor;

            var deltaX = raMod;
            var deltaY = decMod;

            if (imageRotation.Degree != 0) {
                deltaX = raMod * imageRotationCos + decMod * imageRotationSin;
                deltaY = decMod * imageRotationCos - raMod * imageRotationSin;
            }

            return new Point(centerPointPixels.X - deltaX.ArcSeconds / horizResArcSecPx,
                centerPointPixels.Y - deltaY.ArcSeconds / vertResArcSecPix);
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