#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.Astrometry;
using System;
using Accord.Math;
using NINA.Core.Enum;
using NINA.Equipment.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Utility.Notification;

namespace NINA.Equipment.Equipment.MyDome {

    public class DomeSynchronization : IDomeSynchronization {
        private static double TWO_PI = 2.0 * Math.PI;
        private static double HALF_PI = Math.PI / 2.0;

        private readonly IProfileService profileService;

        public DomeSynchronization(IProfileService profileService) {
            this.profileService = profileService;
        }

        /// <summary>
        /// Gets the dome coordinates required so the scope points directly out of the shutter. This works for Alt-Az, EQ mounts, and fork mounts on a wedge
        /// and depends on careful user measurements including:
        ///  1) Dome radius, in mm
        ///  2) GEM axis length, in mm - starting from where the RA and DEC axes intersect, measured laterally to the center of the scope aperture
        ///     Alt-Az mounts set this value to 0. This applies only to Equatorial Mounts
        ///  3) DEC horizontal offset, in mm - the horizontal distance from the DEC axis (the base of the mount) to the center of the OTA. Positive is "to the right".
        ///  4) Lateral axis length, in mm - starting from the saddle plate. This is used in side by side setups where the scope is not centered on the saddle
        ///      It points in the same direction as the y-axis (to the East)
        ///  5) Mount offset as a 3D vector relative to the center of the dome sphere. This is the point where the RA and DEC axes intersect.
        ///     a) The x-axis in the positive direction points North
        ///     b) The y-axis in the positive direction points East
        ///     c) The z-axis points up
        ///  6) The latitude and longitude of the scope site
        ///
        /// This method uses an algorithm that solves equations that are derived in the Wikipedia article
        /// entitled Line-sphere intersection (https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection)
        /// </summary>
        /// <param name="scopeCoordinates">The scope coordinates to derive the dome azimuth from</param>
        /// <param name="localSiderealTime">The local sidereal time</param>
        /// <param name="siteLatitude">The site latitude</param>
        /// <param name="siteLongitude">The site longitude</param>
        /// <param name="sideOfPier">The side of pier. If this is unknown, this method will throw an exception</param>
        /// <returns>Topocentric Coordinates representing the altitude and azimuth for the Dome that lines us with the scope axis</returns>
        public TopocentricCoordinates TargetDomeCoordinates(
            Coordinates scopeCoordinates,
            double localSiderealTime,
            Angle siteLatitude,
            Angle siteLongitude,
            PierSide sideOfPier) {
            scopeCoordinates = scopeCoordinates.Transform(Epoch.JNOW);
            var domeSettings = profileService.ActiveProfile.DomeSettings;
            // To calculate the effect of rotations in the southern hemisphere we augment a few of the rotations to pretend as if it were the northern hemisphere,
            // and then add 180 degrees to the final result

            var origin = new Vector4(0, 0, 0, 1);
            Matrix4x4 scopeOriginTranslation;
            if (domeSettings.MountType == MountTypeEnum.EQUATORIAL) {
                scopeOriginTranslation = CalculateGEM(scopeCoordinates, localSiderealTime, siteLatitude, sideOfPier);
            } else {
                scopeOriginTranslation = CalculateForkOnWedge(scopeCoordinates, localSiderealTime, siteLatitude);
            }

            var scopeApertureOrigin = scopeOriginTranslation * origin;

            // The OTA points along the positive X-axis before any transformations take place. Transforming the point (1, 0, 0) provides a unit
            // direction vector from (0, 0, 0) which is the scope aperture origin
            var scopeDirection = scopeOriginTranslation * Matrix4x4.CreateTranslation(new Vector3(1.0f, 0.0f, 0.0f)) * origin - scopeApertureOrigin;

            // Calculate the distance along the unit vector, originating from the scope aperture origin, to where the line
            // intersects the sphere
            var dotProduct = Vector4.Dot(scopeDirection, scopeApertureOrigin);
            var domeRadius = domeSettings.DomeRadius_mm;
            if (Vector4.Dot(scopeApertureOrigin, scopeApertureOrigin) > (domeRadius * domeRadius)) {
                Notification.ShowError(Loc.Instance["LblDomeRadiusMisconfigured"]);
                throw new Exception(Loc.Instance["LblDomeRadiusMisconfigured"]);
            }

            var underRoot = (dotProduct * dotProduct) - Vector4.Dot(scopeApertureOrigin, scopeApertureOrigin) + (domeRadius * domeRadius);
            var distance = -dotProduct + Math.Sqrt(underRoot);

            // Calculate the intersection point with the sphere
            var intersection = scopeApertureOrigin + scopeDirection * (float)distance;

            // Finally, calculate the azimuth of that intersection point, and ensure it is within [0, 2PI)
            var domeAzimuthRadians = (-Math.Atan2(intersection.Y, intersection.X) + TWO_PI) % TWO_PI;
            // For the southern hemisphere, we inverted all rotations to emulate being north-facing. We now take the final result and add 180 degrees
            if (siteLatitude.Radians < 0) {
                domeAzimuthRadians = (domeAzimuthRadians + Math.PI) % TWO_PI;
            }

            var domeAltitudeRadians = Math.Atan2(Math.Abs(intersection.Z), Math.Abs(intersection.X));
            return new TopocentricCoordinates(
                azimuth: Angle.ByRadians(domeAzimuthRadians),
                altitude: Angle.ByRadians(domeAltitudeRadians),
                latitude: siteLatitude,
                longitude: siteLongitude);
        }

        private Matrix4x4 CalculateForkOnWedge(
            Coordinates scopeCoordinates,
            double localSiderealTime,
            Angle siteLatitude) {
            var domeSettings = profileService.ActiveProfile.DomeSettings;
            var latitudeFactor = (siteLatitude.Radians >= 0) ? 1.0 : -1.0;
            var mountOffset = Matrix4x4.CreateTranslation(
                new Vector3(
                    (float)(domeSettings.ScopePositionNorthSouth_mm * latitudeFactor),
                    -(float)(domeSettings.ScopePositionEastWest_mm * latitudeFactor),
                    (float)domeSettings.ScopePositionUpDown_mm));
            // At either pole (90 degrees) we need to rotate counter-clockwise around the Y-axis.
            var latitudeRotationRadians = -Math.Abs(siteLatitude.Radians);

            var latitudeAdjustment = Matrix4x4.CreateRotationY((float)latitudeRotationRadians);
            // Rotation around the RA axis does not depend on the side of pier for a fork mount
            var localHour = Angle.ByHours(localSiderealTime - scopeCoordinates.RA);
            // In the southern hemisphere, RA goes the opposite direction it does in the north
            var raRotationRadians = -localHour.Radians * latitudeFactor;
            var raRotationAdjustment = Matrix4x4.CreateRotationX((float)raRotationRadians);
            // Rotation around Dec is along the Y axis for a fork mount
            // The north celestial pole is at +90 DEC, and the south pole is at -90 DEC
            var decRotationRadians = HALF_PI - (Angle.ByDegree(scopeCoordinates.Dec).Radians * latitudeFactor);
            var decRotationAdjustment = Matrix4x4.CreateRotationY(-(float)decRotationRadians);
            var raOffsetAdjustment = Matrix4x4.CreateTranslation(
                new Vector3(
                    0.0f,
                    -(float)domeSettings.DecOffsetHorizontal_mm,
                    0.0f));
            // lateral axis is used for side by side saddles
            var decOffsetAdjustment = Matrix4x4.CreateTranslation(
                new Vector3(
                    0.0f,
                    0.0f,
                    (float)domeSettings.LateralAxis_mm));

            return mountOffset * latitudeAdjustment * raRotationAdjustment * raOffsetAdjustment * decRotationAdjustment * decOffsetAdjustment;
        }

        private Matrix4x4 CalculateGEM(
            Coordinates scopeCoordinates,
            double localSiderealTime,
            Angle siteLatitude,
            PierSide sideOfPier) {
            if (sideOfPier == PierSide.pierUnknown) {
                throw new InvalidOperationException("Side of Pier is unknown");
            }

            var domeSettings = profileService.ActiveProfile.DomeSettings;
            var latitudeFactor = (siteLatitude.Radians >= 0) ? 1.0 : -1.0;

            // The coordinate system has the y-axis positive in the left direction when facing the celestial pole, so E/W mount offset and lateral offset
            // need to be inverted. This also needs to be inverted for the Southern hemisphere, since the configuration is always E/W instead of left/right when
            // facing the pole
            var mountOffset = Matrix4x4.CreateTranslation(
                new Vector3(
                    (float)(domeSettings.ScopePositionNorthSouth_mm * latitudeFactor),
                    -(float)(domeSettings.ScopePositionEastWest_mm * latitudeFactor),
                    (float)domeSettings.ScopePositionUpDown_mm));
            // At either pole (90 degrees) we need to rotate counter-clockwise around the Y-axis.
            var latitudeRotationRadians = -Math.Abs(siteLatitude.Radians);

            var latitudeAdjustment = Matrix4x4.CreateRotationY((float)latitudeRotationRadians);
            // Rotation around the RA axis depends on the side of pier. On the east side, 6 hours is North, and on the west side, 18 hours is north
            var localHour = Angle.ByHours(localSiderealTime - scopeCoordinates.RA);
            var pierFactor = ((sideOfPier == PierSide.pierEast) ? 1.0 : -1.0) * latitudeFactor;
            // In the southern hemisphere, RA goes the opposite direction it does in the north
            var raRotationRadians = pierFactor * HALF_PI - (localHour.Radians * latitudeFactor);
            var raRotationAdjustment = Matrix4x4.CreateRotationX((float)raRotationRadians);
            // Rotation around Dec is along the Z axis
            // The north celestial pole is at +90 DEC, and the south pole is at -90 DEC
            var decRotationRadians = pierFactor * (HALF_PI - (Angle.ByDegree(scopeCoordinates.Dec).Radians * latitudeFactor));
            var decRotationAdjustment = Matrix4x4.CreateRotationZ((float)decRotationRadians);
            // Lateral axis does not need to be flipped because it is always "to the right" instead of "to the east"
            var gemAdjustment = Matrix4x4.CreateTranslation(
                new Vector3(
                    0.0f,
                    -(float)domeSettings.LateralAxis_mm,
                    (float)domeSettings.GemAxis_mm));

            return mountOffset * latitudeAdjustment * raRotationAdjustment * decRotationAdjustment * gemAdjustment;
        }
    }
}