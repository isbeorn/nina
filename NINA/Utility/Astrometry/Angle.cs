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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Astrometry {

    public class Angle {

        public static Angle ByHours(double hours) {
            var degree = Astrometry.HoursToDegrees(hours);
            return new Angle(degree, Astrometry.ToRadians(degree), hours);
        }

        public static Angle ByDegree(double degree) {
            return new Angle(degree, Astrometry.ToRadians(degree), Astrometry.DegreesToHours(degree));
        }

        public static Angle ByRadians(double radians) {
            var degree = Astrometry.ToDegree(radians);
            return new Angle(Astrometry.ToDegree(radians), radians, Astrometry.DegreesToHours(degree));
        }

        private Angle(double degree, double radians, double hours) {
            this.Degree = degree;
            this.Radians = radians;
            this.Hours = hours;
        }

        public double Degree { get; }
        public double ArcMinutes => Degree * 60d;
        public double ArcSeconds => ArcMinutes * 60d;

        public double Radians { get; }
        public double Hours { get; }

        public override string ToString() {
            return Astrometry.DegreesToDMS(Degree);
        }

        public Angle Sin() {
            return Angle.ByRadians(Math.Sin(this.Radians));
        }

        public Angle Asin() {
            return Angle.ByRadians(Math.Asin(this.Radians));
        }

        public Angle Cos() {
            return Angle.ByRadians(Math.Cos(this.Radians));
        }

        public Angle Acos() {
            return Angle.ByRadians(Math.Acos(this.Radians));
        }

        public Angle Atan() {
            return Angle.ByRadians(Math.Atan(this.Radians));
        }

        public Angle Atan2(Angle angle) {
            return Angle.ByRadians(Math.Atan2(angle.Radians, this.Radians));
        }

        public static Angle Atan2(Angle y, Angle x) {
            return Angle.ByRadians(Math.Atan2(y.Radians, x.Radians));
        }

        public static Angle operator +(Angle a, Angle b) {
            return Angle.ByRadians(a.Radians + b.Radians);
        }

        public static Angle operator +(double a, Angle b) {
            return Angle.ByRadians(a + b.Radians);
        }

        public static Angle operator +(Angle a, double b) {
            return Angle.ByRadians(a.Radians + b);
        }

        public static Angle operator -(Angle a, Angle b) {
            return Angle.ByRadians(a.Radians - b.Radians);
        }

        public static Angle operator -(double a, Angle b) {
            return Angle.ByRadians(a - b.Radians);
        }

        public static Angle operator -(Angle a, double b) {
            return Angle.ByRadians(a.Radians - b);
        }

        public static Angle operator *(Angle a, Angle b) {
            return Angle.ByRadians(a.Radians * b.Radians);
        }

        public static Angle operator *(double a, Angle b) {
            return Angle.ByRadians(a * b.Radians);
        }

        public static Angle operator /(Angle a, Angle b) {
            return Angle.ByRadians(a.Radians / b.Radians);
        }

        public static Angle operator /(Angle a, double b) {
            return Angle.ByRadians(a.Radians / b);
        }

        public static Angle operator /(double a, Angle b) {
            return Angle.ByRadians(a / b.Radians);
        }
    }
}