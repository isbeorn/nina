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
using NINA.Core.Enum;
using System;
using System.Windows;

namespace NINA.WPF.Base.SkySurvey {

    public sealed class SkyMapViewportProjection {
        private const double ArcSecondsPerRadian = 180d * 3600 / Math.PI;
        private readonly double centerLatitudeCosine;
        private readonly double centerLatitudeSine;
        private readonly double centerLongitude;
        private readonly double cosineRadius;
        private readonly double horizontalPixelsPerRadian;
        private readonly double longitudeDirection;
        private readonly SkyMapObserverSnapshot observer;
        private readonly double rotationCosine;
        private readonly double rotationSine;
        private readonly double verticalPixelsPerRadian;
        private readonly ViewportFoV viewport;
        private readonly double x;
        private readonly double y;

        public SkyMapViewportProjection(
            ViewportFoV viewport,
            SkyMapProjectionMode mode = SkyMapProjectionMode.Equatorial,
            SkyMapObserverSnapshot observer = null) {
            this.viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            Mode = mode;
            longitudeDirection = mode == SkyMapProjectionMode.AltAz ? -1 : 1;
            this.observer = mode == SkyMapProjectionMode.AltAz
                ? observer ?? throw new ArgumentNullException(nameof(observer))
                : observer;
            (double centerLongitudeDegrees, double centerLatitudeDegrees) = DisplayCoordinates(viewport.CenterCoordinates);
            centerLongitude = AstroUtil.ToRadians(centerLongitudeDegrees);
            double centerLatitude = AstroUtil.ToRadians(centerLatitudeDegrees);
            centerLatitudeSine = Math.Sin(centerLatitude);
            centerLatitudeCosine = Math.Cos(centerLatitude);
            double rotation = AstroUtil.ToRadians(viewport.Rotation);
            rotationSine = Math.Sin(rotation);
            rotationCosine = Math.Cos(rotation);
            cosineRadius = Math.Cos(AstroUtil.ToRadians(Math.Max(viewport.HFoV, viewport.VFoV)));
            horizontalPixelsPerRadian = ArcSecondsPerRadian / viewport.ArcSecWidth;
            verticalPixelsPerRadian = ArcSecondsPerRadian / viewport.ArcSecHeight;
            x = viewport.ViewPortCenterPoint.X;
            y = viewport.ViewPortCenterPoint.Y;
            double maximumHorizontalOffset = Math.Max(x, viewport.Width - x) / horizontalPixelsPerRadian;
            double maximumVerticalOffset = Math.Max(y, viewport.Height - y) / verticalPixelsPerRadian;
            double maximumRadius = Math.Sqrt(
                maximumHorizontalOffset * maximumHorizontalOffset
                + maximumVerticalOffset * maximumVerticalOffset);
            AngularRadius = AstroUtil.ToDegree(2 * Math.Atan(maximumRadius / 2));
        }

        public double AngularRadius { get; }
        public SkyMapProjectionMode Mode { get; }
        internal SkyMapObserverSnapshot Observer => observer;
        public ViewportFoV Viewport => viewport;

        public bool Contains(Coordinates coordinates) {
            (double longitude, double latitude) = DisplayCoordinates(coordinates);
            return Contains(longitude, latitude);
        }

        public bool Contains(SkyMapHorizontalCoordinates coordinates) {
            EnsureHorizontalProjection();
            return Contains(coordinates.Azimuth, coordinates.Altitude);
        }

        private bool Contains(double longitude, double latitude) {
            double latitudeRadians = AstroUtil.ToRadians(latitude);
            double cosineDistance = Math.Sin(latitudeRadians) * centerLatitudeSine
                + Math.Cos(latitudeRadians) * centerLatitudeCosine
                * Math.Cos(NormalizedLongitude(longitude));
            return cosineDistance > cosineRadius;
        }

        public Point Project(Coordinates coordinates) {
            (double longitude, double latitude) = DisplayCoordinates(coordinates);
            return Project(longitude, latitude);
        }

        public Point Project(SkyMapHorizontalCoordinates coordinates) {
            EnsureHorizontalProjection();
            return Project(coordinates.Azimuth, coordinates.Altitude);
        }

        public Coordinates Unproject(Point point) {
            (double longitude, double latitude) = UnprojectDisplay(point);
            if (Mode == SkyMapProjectionMode.Equatorial) {
                return new Coordinates(longitude, latitude, Epoch.J2000, Coordinates.RAType.Degrees);
            }
            return observer.ToCelestial(new SkyMapHorizontalCoordinates(latitude, longitude));
        }

        public SkyMapHorizontalCoordinates UnprojectHorizontal(Point point) {
            (double longitude, double latitude) = UnprojectDisplay(point);
            if (Mode == SkyMapProjectionMode.AltAz) {
                return new SkyMapHorizontalCoordinates(latitude, longitude);
            }
            if (observer is null) {
                throw new InvalidOperationException("An observer is required to unproject equatorial coordinates into Alt/Az.");
            }
            return observer.ToHorizontal(longitude, latitude);
        }

        public (double Rotation, bool FlipHorizontally) ImageTransformFromEquatorial(
            Coordinates coordinates,
            double equatorialRotation,
            Point projectedCenter) {
            double rotation = RotationForPositionAngle(coordinates, -equatorialRotation, projectedCenter) + 90;
            double right = RotationForPositionAngle(coordinates, 270 - equatorialRotation, projectedCenter);
            double rightDifference = AstroUtil.EuclidianModulus(right - rotation + 180, 360) - 180;
            return (rotation, Math.Abs(rightDifference) > 90);
        }

        public double RotationForPositionAngle(
            Coordinates coordinates,
            double positionAngle,
            Point projectedCenter) {
            const double referenceDistance = 0.01;
            double longitude = AstroUtil.ToRadians(coordinates.RADegrees);
            double latitude = AstroUtil.ToRadians(coordinates.Dec);
            double bearing = AstroUtil.ToRadians(positionAngle);
            double distance = AstroUtil.ToRadians(referenceDistance);
            double referenceLatitude = Math.Asin(
                Math.Sin(latitude) * Math.Cos(distance)
                + Math.Cos(latitude) * Math.Sin(distance) * Math.Cos(bearing));
            double referenceLongitude = longitude + Math.Atan2(
                Math.Sin(bearing) * Math.Sin(distance) * Math.Cos(latitude),
                Math.Cos(distance) - Math.Sin(latitude) * Math.Sin(referenceLatitude));
            double rightAscension = AstroUtil.EuclidianModulus(AstroUtil.ToDegree(referenceLongitude), 360);
            double declination = AstroUtil.ToDegree(referenceLatitude);
            Point projectedReference;
            if (Mode == SkyMapProjectionMode.Equatorial) {
                projectedReference = Project(rightAscension, declination);
            } else {
                SkyMapHorizontalCoordinates horizontal = observer.ToHorizontal(rightAscension, declination);
                projectedReference = Project(horizontal.Azimuth, horizontal.Altitude);
            }
            return AstroUtil.ToDegree(Math.Atan2(
                projectedReference.Y - projectedCenter.Y,
                projectedReference.X - projectedCenter.X));
        }

        public Coordinates ShiftCenter(Vector delta) {
            if (delta.X == 0 && delta.Y == 0) {
                return viewport.CenterCoordinates;
            }
            return Unproject(viewport.ViewPortCenterPoint + delta);
        }

        private (double Longitude, double Latitude) DisplayCoordinates(Coordinates coordinates) {
            if (Mode == SkyMapProjectionMode.Equatorial) {
                return (coordinates.RADegrees, coordinates.Dec);
            }
            SkyMapHorizontalCoordinates horizontal = observer.ToHorizontal(coordinates);
            return (horizontal.Azimuth, horizontal.Altitude);
        }

        private void EnsureHorizontalProjection() {
            if (Mode != SkyMapProjectionMode.AltAz) {
                throw new InvalidOperationException("Horizontal coordinates require an Alt/Az projection.");
            }
        }

        private Point Project(double longitude, double latitude) {
            double latitudeRadians = AstroUtil.ToRadians(latitude);
            double latitudeSine = Math.Sin(latitudeRadians);
            double latitudeCosine = Math.Cos(latitudeRadians);
            double longitudeDifference = NormalizedLongitude(longitude);
            double longitudeCosine = Math.Cos(longitudeDifference);
            double scale = 2 / (1 + latitudeSine * centerLatitudeSine
                + latitudeCosine * centerLatitudeCosine * longitudeCosine);
            double longitudeOffset = longitudeDirection * scale * Math.Sin(longitudeDifference) * latitudeCosine;
            double latitudeOffset = scale * (latitudeSine * centerLatitudeCosine
                - latitudeCosine * centerLatitudeSine * longitudeCosine);
            double rotatedX = longitudeOffset * rotationCosine + latitudeOffset * rotationSine;
            double rotatedY = latitudeOffset * rotationCosine - longitudeOffset * rotationSine;
            return new Point(x - rotatedX * horizontalPixelsPerRadian, y - rotatedY * verticalPixelsPerRadian);
        }

        private (double Longitude, double Latitude) UnprojectDisplay(Point point) {
            double rotatedX = (x - point.X) / horizontalPixelsPerRadian;
            double rotatedY = (y - point.Y) / verticalPixelsPerRadian;
            double longitudeOffset = rotatedX * rotationCosine - rotatedY * rotationSine;
            double latitudeOffset = rotatedX * rotationSine + rotatedY * rotationCosine;
            longitudeOffset *= longitudeDirection;
            double radius = Math.Sqrt(longitudeOffset * longitudeOffset + latitudeOffset * latitudeOffset);
            if (radius < 1E-15) {
                return DisplayCoordinates(viewport.CenterCoordinates);
            }

            double angularDistance = 2 * Math.Atan(radius / 2);
            double angularDistanceSine = Math.Sin(angularDistance);
            double angularDistanceCosine = Math.Cos(angularDistance);
            double latitude = Math.Asin(
                angularDistanceCosine * centerLatitudeSine
                + latitudeOffset * angularDistanceSine * centerLatitudeCosine / radius);
            double longitude = centerLongitude + Math.Atan2(
                longitudeOffset * angularDistanceSine,
                radius * centerLatitudeCosine * angularDistanceCosine
                    - latitudeOffset * centerLatitudeSine * angularDistanceSine);
            return (
                AstroUtil.EuclidianModulus(AstroUtil.ToDegree(longitude), 360),
                AstroUtil.ToDegree(latitude));
        }

        private double NormalizedLongitude(double longitude) {
            double difference = AstroUtil.ToRadians(longitude) - centerLongitude;
            if (difference > Math.PI) {
                difference -= 2 * Math.PI;
            } else if (difference < -Math.PI) {
                difference += 2 * Math.PI;
            }
            return difference;
        }
    }
}