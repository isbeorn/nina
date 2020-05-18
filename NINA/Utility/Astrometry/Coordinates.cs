#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
            double jdTT = GetJdTT(DateTime.Now);

            double ri = 0, di = 0, eo = 0;
            SOFA.CelestialToIntermediate(raAngle.Radians, decAngle.Radians, 0.0, 0.0, 0.0, 0.0, jdTT, 0.0, ref ri, ref di, ref eo);

            var raApparent = Angle.ByRadians(SOFA.Anp(ri - eo));
            var decApparent = Angle.ByRadians(di);

            return new Coordinates(raApparent, decApparent, Epoch.JNOW);
        }

        private double GetJdTT(DateTime date) {
            var utcDate = date.ToUniversalTime();
            double tai1 = 0, tai2 = 0, tt1 = 0, tt2 = 0;
            var utc = Astrometry.GetJulianDate(utcDate);

            SOFA.UtcTai(utc, 0.0, ref tai1, ref tai2);
            SOFA.TaiTt(tai1, tai2, ref tt1, ref tt2);

            return tt1 + tt2;
        }

        /// <summary>
        /// Transforms coordinates from JNOW to J2000
        /// </summary>
        /// <returns></returns>
        private Coordinates TransformToJ2000() {
            var now = DateTime.Now;
            var jdTT = GetJdTT(now);
            var jdUTC = Astrometry.GetJulianDate(now);
            double rc = 0, dc = 0, eo = 0;
            SOFA.IntermediateToCelestial(SOFA.Anp(raAngle.Radians + SOFA.Eo06a(jdUTC, 0.0)), decAngle.Radians, jdTT, 0.0, ref rc, ref dc, ref eo);

            var raCelestial = Angle.ByRadians(rc);
            var decCelestial = Angle.ByRadians(dc);

            return new Coordinates(raCelestial, decCelestial, Epoch.J2000);
        }

        public TopocentricCoordinates Transform(Angle latitude, Angle longitude) {
            return this.Transform(latitude, longitude, 0.0);
        }

        public TopocentricCoordinates Transform(Angle latitude, Angle longitude, double elevation) {
            var transform = this.Transform(Epoch.J2000);

            var now = DateTime.Now;
            var jdUTC = Astrometry.GetJulianDate(now);

            var deltaUT = Astrometry.DeltaUT(now);
            double aob = 0d, zob = 0d, hob = 0d, dob = 0d, rob = 0d, eo = 0d;
            SOFA.CelestialToTopocentric(transform.raAngle.Radians, transform.decAngle.Radians, 0d, 0d, 0d, 0d, jdUTC, 0d, deltaUT, longitude.Radians, latitude.Radians, elevation, 0d, 0d, 0d, 0d, 0d, 0d, ref aob, ref zob, ref hob, ref dob, ref rob, ref eo);

            var az = Angle.ByRadians(aob);
            var alt = Angle.ByDegree(90) - Angle.ByRadians(zob);

            return new TopocentricCoordinates(az, alt, latitude, longitude);
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
        private Coordinates ShiftGnomonic(double deltaX, double deltaY, double rotation) {
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

        private Coordinates ShiftStenographic(double deltaX, double deltaY, double rotation) {
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

            var originDecSin = decAngle.Sin();
            var originDecCos = decAngle.Cos();

            var sins = deltaXAngle * deltaXAngle + deltaYAngle * deltaYAngle;

            var dz = (4.0 - sins) / (4.0 + sins);

            var targetDec = (dz * originDecSin + deltaYAngle * originDecCos * (1.0 + dz) / 2.0).Asin();
            var targetRA = (deltaXAngle * (1.0 + dz) / (2.0 * targetDec.Cos())).Asin();

            var mg = 2 * (targetDec.Sin() * originDecCos - targetDec.Cos() * originDecSin * targetRA.Cos()) /
                (1.0 + targetDec.Sin() * originDecSin + targetDec.Cos() * originDecCos * targetRA.Cos());

            if (Math.Abs((mg - deltaYAngle).Radians) > 1.0e-5) {
                targetRA = Math.PI - targetRA;
            }

            targetRA += raAngle;

            if (targetRA.Degree < 0) { targetRA = Angle.ByDegree(targetRA.Degree + 360); }
            if (targetRA.Degree >= 360) { targetRA = Angle.ByDegree(targetRA.Degree - 360); }

            return new Coordinates(
                targetRA,
                targetDec,
                Epoch
            );
        }

        public enum ProjectionType {
            Gnomonic,
            Stereographic
        }

        public Coordinates Shift(
            double deltaX,
            double deltaY,
            double rotation,
            double scaleX,
            double scaleY,
            ProjectionType type = ProjectionType.Stereographic
        ) {
            var deltaXDeg = deltaX * Astrometry.ArcsecToDegree(scaleX);
            var deltaYDeg = deltaY * Astrometry.ArcsecToDegree(scaleY);
            return this.Shift(deltaXDeg, deltaYDeg, rotation, type);
        }

        public Coordinates Shift(double deltaX, double deltaY, double rotation, ProjectionType type = ProjectionType.Stereographic) {
            switch (type) {
                case ProjectionType.Gnomonic:
                    return ShiftGnomonic(deltaX, deltaY, rotation);

                case ProjectionType.Stereographic:
                    return ShiftStenographic(deltaX, deltaY, rotation);

                default:
                    return ShiftGnomonic(deltaX, deltaY, rotation);
            }
        }

        public Point XYProjection(ViewportFoV viewPort, ProjectionType type = ProjectionType.Stereographic) {
            return XYProjection(
                viewPort.CenterCoordinates,
                viewPort.ViewPortCenterPoint,
                viewPort.ArcSecWidth,
                viewPort.ArcSecHeight,
                viewPort.Rotation);
        }

        public Point XYProjection(Coordinates center, Point centerPointPixels, double horizResArcSecPx, double vertResArcSecPix, double rotation, ProjectionType type = ProjectionType.Stereographic) {
            switch (type) {
                case ProjectionType.Gnomonic:
                    return GnomonicTanProjection(center, centerPointPixels, horizResArcSecPx, vertResArcSecPix, rotation);

                case ProjectionType.Stereographic:
                    return StenographicProjection(center, centerPointPixels, horizResArcSecPx, vertResArcSecPix, rotation);

                default:
                    return GnomonicTanProjection(center, centerPointPixels, horizResArcSecPx, vertResArcSecPix, rotation);
            }
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
        private Point GnomonicTanProjection(Coordinates center, Point centerPointPixels, double horizResArcSecPx, double vertResArcSecPix, double rotation) {
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
        /// Generates a Point with relative X/Y values for centering the current coordinates relative to a given point using steonographic projection.
        /// </summary>
        /// <remarks>
        ///     based on http://faculty.wcas.northwestern.edu/nchapman/coding/worldpos.py
        /// </remarks>
        private Point StenographicProjection(Coordinates center, Point centerPointPixels, double horizResArcSecPx, double vertResArcSecPix, double rotation) {
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

            var raDiff = targetRa - centerRa;
            var raDiffCos = raDiff.Cos();

            var imageRotationSin = imageRotation.Sin();
            var imageRotationCos = imageRotation.Cos();

            var dd = 2.0 / (1.0 + targetDecSin * centerDegSin + targetDecCos * centerDegCos * raDiffCos);
            var raMod = dd * raDiff.Sin() * targetDecCos;
            var decMod = dd * (targetDecSin * centerDegCos - targetDecCos * centerDegSin * raDiffCos);

            var deltaX = raMod;
            var deltaY = decMod;

            if (imageRotation.Degree != 0) {
                deltaX = raMod * imageRotationCos + decMod * imageRotationSin;
                deltaY = decMod * imageRotationCos - raMod * imageRotationSin;
            }

            return new Point(centerPointPixels.X - deltaX.ArcSeconds / horizResArcSecPx,
                centerPointPixels.Y - deltaY.ArcSeconds / vertResArcSecPix);
        }

        public static Separation operator -(Coordinates a, Coordinates b) {
            if (a.Epoch != b.Epoch) {
                b = b.Transform(a.Epoch);
            }

            var raDiff = a.raAngle - b.raAngle;
            var decDiff = a.decAngle - b.decAngle;
            var distance = (a.decAngle.Sin() * b.decAngle.Sin() + a.decAngle.Cos() * b.decAngle.Cos() * raDiff.Cos()).Acos();

            var y = raDiff.Sin() * b.decAngle.Cos();
            var x = a.decAngle.Cos() * b.decAngle.Sin() - a.decAngle.Sin() * b.decAngle.Cos() * raDiff.Cos();
            var bearing = Angle.Atan2(y, x);

            return new Separation() {
                RA = raDiff,
                Dec = decDiff,
                Distance = distance,
                Bearing = bearing
            };
        }

        public static Coordinates operator +(Coordinates a, Separation b) {
            return new Coordinates(Angle.ByDegree(a.RADegrees) + b.RA, Angle.ByDegree(a.Dec) + b.Dec, a.Epoch);
        }

        public static Coordinates operator -(Coordinates a, Separation b) {
            return new Coordinates(Angle.ByDegree(a.RADegrees) - b.RA, Angle.ByDegree(a.Dec) - b.Dec, a.Epoch);
        }

        public override string ToString() {
            return $"RA: {this.RAString}; Dec: {this.DecString}; Epoch: {Epoch}";
        }
    }

    /// <summary>
    /// Separation properties between two coordinates
    /// </summary>
    public class Separation {
        public Angle RA { get; set; } = Angle.ByDegree(0);
        public Angle Dec { get; set; } = Angle.ByDegree(0);
        public Angle Distance { get; set; } = Angle.ByDegree(0);
        public Angle Bearing { get; set; } = Angle.ByDegree(0);

        public override string ToString() {
            return $"RA: {Astrometry.HoursToHMS(RA.Hours)}; Dec: {Astrometry.DegreesToDMS(Dec.Degree)}; Distance: {Distance.Degree}; Bearing: {Bearing.Degree}";
        }
    }
}
