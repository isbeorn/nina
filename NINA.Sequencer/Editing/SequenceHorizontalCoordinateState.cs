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
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.Editing {
    internal sealed record SequenceHorizontalCoordinateState(double Altitude, double Azimuth, bool NegativeAlt,
        string AltDefinition, string AzDefinition, bool AltExpression, bool AzExpression) {
        public static SequenceHorizontalCoordinateState Capture(InputTopocentricCoordinates coordinates, Expression altitude, Expression azimuth) => new(
            coordinates.Coordinates.Altitude.Degree, coordinates.Coordinates.Azimuth.Degree, coordinates.NegativeAlt,
            altitude.Definition, azimuth.Definition, altitude.IsExpression, azimuth.IsExpression);

        public static bool Equal(SequenceHorizontalCoordinateState left, SequenceHorizontalCoordinateState right) =>
            left.AltDefinition == right.AltDefinition && left.AzDefinition == right.AzDefinition
            && (left.AltExpression || (left.Altitude == right.Altitude && left.NegativeAlt == right.NegativeAlt))
            && (left.AzExpression || left.Azimuth == right.Azimuth);
    }
}