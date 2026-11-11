#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Core.Model {

    /// <summary>
    /// Describes the part of the sky that is available for observation by combining
    /// an optional lower horizon (obstacles from below) and an optional upper horizon
    /// (obstacles from above, e.g. a balcony). Both profiles use the same azimuth/altitude model.
    /// </summary>
    public class SkyWindow {
        public const double DefaultLowerAltitude = 0d;
        public const double DefaultUpperAltitude = 90d;

        public SkyWindow(CustomHorizon lower, CustomHorizon upper) {
            Lower = lower;
            Upper = upper;
        }

        /// <summary>Lower horizon. When null the lower bound is 0°.</summary>
        public CustomHorizon Lower { get; }

        /// <summary>Upper horizon. When null the upper bound is 90°.</summary>
        public CustomHorizon Upper { get; }

        public bool HasLower => Lower != null;

        public bool HasUpper => Upper != null;

        /// <summary>
        /// Creates the sky window of a horizon that was read from a horizon file: the horizon itself as lower horizon
        /// (unless the file only defined upper values) and the optional upper horizon from its third column.
        /// </summary>
        public static SkyWindow From(CustomHorizon horizon) {
            var lower = horizon?.HasLowerData == true ? horizon : null;

            return new SkyWindow(lower, horizon?.EmbeddedUpper);
        }

        /// <summary>
        /// A horizon converts to its sky window including an embedded upper horizon. This keeps existing
        /// call sites that pass a <see cref="CustomHorizon"/> (including a null literal) unambiguous.
        /// </summary>
        public static implicit operator SkyWindow(CustomHorizon horizon) => From(horizon);

        public double GetLower(double azimuth) => Lower?.GetAltitude(azimuth) ?? DefaultLowerAltitude;

        public double GetUpper(double azimuth) => Upper?.GetAltitude(azimuth) ?? DefaultUpperAltitude;
    }
}
