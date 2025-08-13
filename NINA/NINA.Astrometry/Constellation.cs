#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using System;
using System.Collections.Generic;

namespace NINA.Astrometry {

    public class Constellation {

        public Constellation(string id) {
            Id = id;
            Name = Loc.Instance["LblConstellation_" + id];
            StarConnections = new List<Tuple<Star, Star>>();
        }

        public string Id { get; }

        public bool GoesOverRaZero { get; set; }

        public string Name { get; private set; }

        public List<Star> Stars { get; set; }

        public List<Tuple<Star, Star>> StarConnections { get; private set; }
    }
}