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

    internal class Angle {

        public static Angle CreateByHours(double hours) {
            var degree = Astrometry.HoursToDegrees(hours);
            return new Angle() {
                Degree = degree,
                Radians = Astrometry.ToRadians(degree),
                Hours = hours
            };
        }

        public static Angle CreateByDegree(double degree) {
            return new Angle() {
                Degree = degree,
                Radians = Astrometry.ToRadians(degree),
                Hours = Astrometry.DegreesToHours(degree)
            };
        }

        public static Angle CreateByRadians(double radians) {
            var degree = Astrometry.ToDegree(radians);
            return new Angle() {
                Degree = Astrometry.ToDegree(radians),
                Radians = radians,
                Hours = Astrometry.DegreesToHours(degree)
            };
        }

        private Angle() {
        }

        public double Degree { get; private set; }
        public double ArcMinutes => Degree * 60d;
        public double ArcSeconds => ArcMinutes * 60d;

        public double Radians { get; private set; }
        public double Hours { get; private set; }

        public override string ToString() {
            return Astrometry.DegreesToDMS(Degree);
        }

        public Angle Sin() {
            return Angle.CreateByRadians(Math.Sin(this.Radians));
        }

        public Angle Cos() {
            return Angle.CreateByRadians(Math.Cos(this.Radians));
        }

        public Angle Acos() {
            return Angle.CreateByRadians(Math.Acos(this.Radians));
        }

        public Angle Atan() {
            return Angle.CreateByRadians(Math.Atan(this.Radians));
        }

        public Angle Atan2(Angle angle) {
            return Angle.CreateByRadians(Math.Atan2(angle.Radians, this.Radians));
        }
    }
}