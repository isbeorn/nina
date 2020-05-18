#region "copyright"

/*
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using System;
using System.Runtime.Serialization;

namespace NINA.Utility.Exceptions {

    [Serializable]
    internal class PlanetariumFailedToGetCoordinates : Exception {

        public PlanetariumFailedToGetCoordinates() {
        }

        public PlanetariumFailedToGetCoordinates(string message) : base(message) {
        }

        public PlanetariumFailedToGetCoordinates(string message, Exception innerException) : base(message, innerException) {
        }

        protected PlanetariumFailedToGetCoordinates(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
