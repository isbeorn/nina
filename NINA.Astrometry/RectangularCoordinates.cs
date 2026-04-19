using System;

namespace NINA.Astrometry {
    /// <summary>
    /// Position and velocity vectors for a celestial body represented in rectangular coordinates
    /// </summary>
    public class RectangularPV {
        public RectangularPV(RectangularCoordinates position, RectangularCoordinates velocity) {
            this.Position = position;
            this.Velocity = velocity;
        }

        public RectangularCoordinates Position { get; private set; }
        public RectangularCoordinates Velocity { get; private set; }

        public override string ToString() {
            return $"{{{nameof(Position)}={Position}, {nameof(Velocity)}={Velocity}}}";
        }
    }

    /// <summary>
    /// Rectangular (Cartesian) coordinates for a celestial body
    /// </summary>
    public class RectangularCoordinates {

        public static RectangularCoordinates FromPolar(Coordinates coordinates) {
            double ra = Angle.ByDegree(coordinates.RADegrees).Radians;
            double dec = Angle.ByDegree(coordinates.Dec).Radians;
            double cosDec = Math.Cos(dec);
            return new RectangularCoordinates(
                cosDec * Math.Cos(ra),
                cosDec * Math.Sin(ra),
                Math.Sin(dec));
        }

        public RectangularCoordinates(double x, double y, double z) {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
        public double Distance => Math.Sqrt(X * X + Y * Y + Z * Z);

        public RectangularCoordinates RotateEcliptic(Angle meanObliquity) {
            var meanObliquityRad = meanObliquity.Radians;
            var x = this.X;
            var y = this.Y * Math.Cos(meanObliquityRad) - this.Z * Math.Sin(meanObliquityRad);
            var z = this.Y * Math.Sin(meanObliquityRad) + this.Z * Math.Cos(meanObliquityRad);
            return new RectangularCoordinates(x, y, z);
        }

        public RectangularCoordinates Normalize() {
            return this / Distance;
        }

        public double Dot(RectangularCoordinates other) {
            return X * other.X + Y * other.Y + Z * other.Z;
        }

        public RectangularCoordinates Cross(RectangularCoordinates other) {
            return new RectangularCoordinates(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X);
        }

        public RectangularCoordinates RotateAroundAxis(RectangularCoordinates axis, Angle angle) {
            RectangularCoordinates normalizedAxis = axis.Normalize();
            double sinAngle = Math.Sin(angle.Radians);
            double cosAngle = Math.Cos(angle.Radians);
            return this * cosAngle
                + normalizedAxis.Cross(this) * sinAngle
                + normalizedAxis * normalizedAxis.Dot(this) * (1 - cosAngle);
        }

        public static RectangularCoordinates operator +(RectangularCoordinates l, RectangularCoordinates r) {
            return new RectangularCoordinates(
                l.X + r.X,
                l.Y + r.Y,
                l.Z + r.Z);
        }

        public static RectangularCoordinates operator -(RectangularCoordinates l, RectangularCoordinates r) {
            return new RectangularCoordinates(
                l.X - r.X,
                l.Y - r.Y,
                l.Z - r.Z);
        }

        public static RectangularCoordinates operator *(RectangularCoordinates l, double mult) {
            return new RectangularCoordinates(
                l.X * mult,
                l.Y * mult,
                l.Z * mult);
        }

        public static RectangularCoordinates operator /(RectangularCoordinates l, double div) {
            return new RectangularCoordinates(
                l.X / div,
                l.Y / div,
                l.Z / div);
        }

        public Coordinates ToPolar(Epoch epoch = Epoch.J2000) {
            var raRadians = Math.Atan2(this.Y, this.X);
            if (raRadians < 0.0) {
                raRadians += Math.PI * 2.0;
            }
            var ra = Angle.ByRadians(raRadians);
            var dec = Angle.ByRadians(Math.Asin(this.Z / this.Distance));
            return new Coordinates(ra: ra, dec: dec, epoch: epoch);
        }

        public override string ToString() {
            return $"{{{nameof(X)}={X.ToString()}, {nameof(Y)}={Y.ToString()}, {nameof(Z)}={Z.ToString()}}}";
        }
    }
}
