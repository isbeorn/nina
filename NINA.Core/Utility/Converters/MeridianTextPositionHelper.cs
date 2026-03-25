#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Core.Utility.Converters {

    public static class MeridianTextPositionHelper {
        private const double OneAndHalfHours = 0.0625; // ~1.5 hours in OxyPlot datetime units (1 day = 1.0)
        private const double MarginWhenNowIsRight = 0.05; // Margin (~1.2 hours) to provide clearance before Now line including text width
        private const double MarginWhenNowIsLeft = 0.06; // Large margin (~1.5 hours) to clear vertical "Now" label
        private const double TextWidthMargin = 0.08; // Approximate text width (~2 hours) to prevent overflow at edges

        public enum PositioningStrategy {
            CenterOnMeridian,
            OffsetLeft,
            OffsetRight
        }

        public static PositioningStrategy GetPositioningStrategy(double nowTime, double meridianTime) {
            if (double.IsNaN(nowTime) || double.IsNaN(meridianTime)) {
                return PositioningStrategy.CenterOnMeridian;
            }

            double distance = Math.Abs(nowTime - meridianTime);

            // If they're far apart (> 1.5 hours), center text on meridian
            if (distance > OneAndHalfHours) {
                return PositioningStrategy.CenterOnMeridian;
            }

            // They're close (< 1.5 hours), position to avoid Now line
            double signedDistance = nowTime - meridianTime;

            if (signedDistance > 0) {
                // Now is to the RIGHT of meridian
                return PositioningStrategy.OffsetRight;
            } else {
                // Now is to the LEFT of meridian
                return PositioningStrategy.OffsetLeft;
            }
        }

        public static double GetXPosition(double nowTime, double meridianTime, PositioningStrategy strategy) {
            double position = strategy switch {
                PositioningStrategy.CenterOnMeridian => meridianTime,
                PositioningStrategy.OffsetRight => nowTime - MarginWhenNowIsRight,
                PositioningStrategy.OffsetLeft => nowTime + MarginWhenNowIsLeft,
                _ => meridianTime
            };

            // Clamp X position to prevent text from going offscreen
            // When using Left alignment (OffsetLeft), text extends rightward, so leave margin on right
            // When using Right alignment (OffsetRight), text extends leftward, so leave margin on left
            // Assume chart spans approximately 1.0 unit (24 hours)
            if (strategy == PositioningStrategy.OffsetLeft) {
                // Text extends to the right, ensure position + text width stays within bounds
                position = Math.Min(position, 1.0 - TextWidthMargin);
            } else if (strategy == PositioningStrategy.OffsetRight) {
                // Text extends to the left, ensure position - text width stays within bounds
                position = Math.Max(position, TextWidthMargin);
            }

            return position;
        }
    }
}
