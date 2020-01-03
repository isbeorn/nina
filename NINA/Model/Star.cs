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

using NINA.Utility.Astrometry;
using System.Drawing;

namespace NINA.Model {

    internal class Star {

        public Star(int id, string name, Coordinates coords, float mag) {
            Id = id;
            Mag = mag;
            Name = name;
            Coords = coords;
        }

        public int Id { get; }

        public string Name { get; }

        public Coordinates Coords { get; }

        public float Mag { get; }

        public float Radius { get; set; }

        public float TextPadding => Position.Y + Radius;

        public PointF Position { get; set; }
    }
}