#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;

namespace NINA.Sequencer.Editing {
    // Value-only snapshot. InputCoordinates.Clone does not retain the sign of zero.
    internal sealed record SequenceCoordinateValue(double RA, double Dec, Epoch Epoch, bool NegativeDec) {
        public static SequenceCoordinateValue Capture(InputCoordinates input) => input?.Coordinates == null ? null
            : new(input.Coordinates.RA, input.Coordinates.Dec, input.Coordinates.Epoch, input.NegativeDec);
        public Coordinates ToCoordinates() => new(RA, Dec, Epoch, Coordinates.RAType.Hours);
        public void Restore(InputCoordinates input) {
            input.Coordinates = ToCoordinates();
            input.NegativeDec = NegativeDec;
        }
    }
}