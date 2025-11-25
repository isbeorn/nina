#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace Accord.Imaging {
    /// <summary>
    /// Represents a line detected by Hough transform
    /// </summary>
    public class HoughLine {
        /// <summary>
        /// Gets or sets the radius (distance from origin)
        /// </summary>
        public int Radius { get; set; }

        /// <summary>
        /// Gets or sets the angle in degrees
        /// </summary>
        public double Theta { get; set; }

        /// <summary>
        /// Gets or sets the intensity (number of votes)
        /// </summary>
        public short Intensity { get; set; }

        /// <summary>
        /// Gets or sets the relative intensity (0-1)
        /// </summary>
        public double RelativeIntensity { get; set; }

        public HoughLine() { }

        public HoughLine(int radius, double theta, short intensity, double relativeIntensity) {
            Radius = radius;
            Theta = theta;
            Intensity = intensity;
            RelativeIntensity = relativeIntensity;
        }
    }
}
