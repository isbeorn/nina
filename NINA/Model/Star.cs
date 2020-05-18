#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
