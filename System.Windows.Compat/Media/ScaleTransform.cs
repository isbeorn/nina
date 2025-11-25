#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Media {
    /// <summary>
    /// Base class for transforms
    /// </summary>
    public abstract class Transform {
    }

    /// <summary>
    /// ScaleTransform - represents a scaling transformation
    /// </summary>
    public class ScaleTransform : Transform {
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }

        public ScaleTransform() {
            ScaleX = 1.0;
            ScaleY = 1.0;
        }

        public ScaleTransform(double scaleX, double scaleY) {
            ScaleX = scaleX;
            ScaleY = scaleY;
        }

        public ScaleTransform(double scaleX, double scaleY, double centerX, double centerY) {
            ScaleX = scaleX;
            ScaleY = scaleY;
            CenterX = centerX;
            CenterY = centerY;
        }
    }
}
