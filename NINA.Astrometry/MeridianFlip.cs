#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using System;
using NINA.Core.Utility;

namespace NINA.Astrometry {

    public static class MeridianFlip {

        public static TimeSpan TimeToMeridian(Coordinates coordinates, Angle localSiderealTime) {
            coordinates = coordinates.Transform(Epoch.JNOW);
            var rightAscension = Angle.ByHours(coordinates.RA);
            var hoursToMeridian = (rightAscension.Hours - localSiderealTime.Hours) % 12.0;
            if (hoursToMeridian < 0.0) {
                hoursToMeridian += 12.0;
            }
            return TimeSpan.FromHours(hoursToMeridian);
        }

        /// <summary>
        /// Returns the expected pier side for the current coordinates
        /// Expected pier side is the pier side where the counter weight down position is expected to be
        /// West: Hour Angle 0..12
        /// East: Hour Angle 12..24
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="localSiderealTime"></param>
        /// <returns></returns>
        public static PierSide ExpectedPierSide(Coordinates coordinates, Angle localSiderealTime) {
            coordinates = coordinates.Transform(Epoch.JNOW);
            var rightAscension = Angle.ByHours(coordinates.RA);
            var hoursToLST = (rightAscension.Hours - localSiderealTime.Hours) % 24.0;
            if (hoursToLST < 0.0) {
                hoursToLST += 24.0;
            }

            if (hoursToLST < 12.0) {
                return PierSide.pierWest;
            } else {
                return PierSide.pierEast;
            }
        }

        public static TimeSpan TimeToMeridianFlip(
            IMeridianFlipSettings settings,
            Coordinates coordinates,
            Angle localSiderealTime,
            PierSide currentSideOfPier) {
            // Shift the sidereal time by the time after the meridian to retrieve the time to the flip instead of the time to the meridian
            // This is critical to do instead of just adding to the meridian time, when the scope is already past the meridian but not past the flip
            var projectedSiderealTime = Angle.ByHours(AstroUtil.EuclidianModulus(localSiderealTime.Hours - settings.MaxMinutesAfterMeridian / 60d, 24));
            var timeToMeridianFlip = TimeToMeridian(coordinates, localSiderealTime: projectedSiderealTime);

            if (settings.UseSideOfPier) {
                if (currentSideOfPier == PierSide.pierUnknown) {
                    Logger.Debug("UseSideOfPier is enabled but pier side is Unknown - ignoring pierside to calculate time to meridian");
                } else {
                    var timeToMeridian = TimeToMeridian(coordinates, localSiderealTime: localSiderealTime);

                    var expectedPierSide = ExpectedPierSide(coordinates: coordinates, localSiderealTime: localSiderealTime);
                    if (timeToMeridian < TimeSpan.FromHours(1) && expectedPierSide != currentSideOfPier) {
                        // The telescope did not yet traverse the meridian, but is close to it
                        // However the current side of pier is not what the expected pier side should be,
                        // which means the scope is already in the flipped state and won't require a flip.
                        // Thus, the next meridian flip won't be for another 12 hours
                        timeToMeridianFlip += TimeSpan.FromHours(12.0);
                    }
                    if (timeToMeridianFlip < TimeSpan.FromHours(1)
                        && timeToMeridian > (TimeSpan.FromHours(12) - TimeSpan.FromMinutes(settings.MaxMinutesAfterMeridian))
                        && expectedPierSide == currentSideOfPier) {
                        // The telescope did travers the meridian recently, but the flip is soon
                        // The side of pier is already what it should be
                        // Thus, the next meridian flip won't be for another 12 hours
                        timeToMeridianFlip += TimeSpan.FromHours(12.0);
                    }
                }
            }

            // Safeguard against unrealistic timespan
            if (timeToMeridianFlip >= TimeSpan.FromDays(1)) {
                timeToMeridianFlip -= TimeSpan.FromDays(1);
            }
            return timeToMeridianFlip;
        }
    }
}