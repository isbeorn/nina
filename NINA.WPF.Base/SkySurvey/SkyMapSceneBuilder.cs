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
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NINA.WPF.Base.SkySurvey {

    [Flags]
    public enum SkyMapRenderOptions {
        None = 0,
        Stars = 1,
        Constellations = 2,
        DeepSkyObjects = 4,
        ConstellationBoundaries = 8,
        EquatorialGrid = 16,
        HorizontalGrid = 32,
        Horizon = 64,
        All = Stars | Constellations | DeepSkyObjects | ConstellationBoundaries | EquatorialGrid
    }

    public sealed class SkyMapScene {
        public SkyMapScene(
            IReadOnlyList<SkyMapStar> stars,
            IReadOnlyList<SkyMapLine> constellationLines,
            IReadOnlyList<SkyMapDeepSkyObject> deepSkyObjects,
            IReadOnlyList<SkyMapPath> constellationBoundaries,
            IReadOnlyList<SkyMapPath> gridLines,
            IReadOnlyList<SkyMapLabel> labels = null,
            IReadOnlyList<SkyMapLine> horizonLines = null,
            IReadOnlyList<SkyMapPath> horizonMaskAreas = null) {
            Stars = stars;
            ConstellationLines = constellationLines;
            DeepSkyObjects = deepSkyObjects;
            ConstellationBoundaries = constellationBoundaries;
            GridLines = gridLines;
            HorizonLines = horizonLines ?? [];
            HorizonMaskAreas = horizonMaskAreas ?? [];
            Labels = labels ?? [];
        }

        public IReadOnlyList<SkyMapStar> Stars { get; }
        public IReadOnlyList<SkyMapLine> ConstellationLines { get; }
        public IReadOnlyList<SkyMapDeepSkyObject> DeepSkyObjects { get; }
        public IReadOnlyList<SkyMapPath> ConstellationBoundaries { get; }
        public IReadOnlyList<SkyMapPath> GridLines { get; }
        public IReadOnlyList<SkyMapLine> HorizonLines { get; }
        public IReadOnlyList<SkyMapPath> HorizonMaskAreas { get; }
        public IReadOnlyList<SkyMapLabel> Labels { get; }
    }

    public enum SkyMapLabelKind {
        Star,
        Constellation,
        Grid,
        CardinalDirection
    }

    public readonly record struct SkyMapLabel(string Text, Point Position, SkyMapLabelKind Kind);

    public readonly record struct SkyMapStar(Point Center, double Radius);

    public readonly record struct SkyMapLine(Point Start, Point End);

    public readonly record struct SkyMapDeepSkyObject(
        string Name,
        string Type,
        Point Center,
        double RadiusX,
        double RadiusY,
        double PositionAngle);

    public sealed class SkyMapPath {
        public SkyMapPath(IReadOnlyList<Point> points, bool closed = false, double strokeThickness = 1) {
            Points = points;
            Closed = closed;
            StrokeThickness = strokeThickness;
        }

        public IReadOnlyList<Point> Points { get; }
        public bool Closed { get; }
        public double StrokeThickness { get; }
    }

    public sealed class SkyMapSceneBuilder {
        private static readonly (double Azimuth, string Direction)[] CardinalDirections = [
            (0, "N"),
            (45, "NE"),
            (90, "E"),
            (135, "SE"),
            (180, "S"),
            (225, "SW"),
            (270, "W"),
            (315, "NW")
        ];
        private static readonly double[] DeclinationSteps = [0.5, 1, 2, 4, 12, 20];
        private static readonly double[] HorizontalSteps = [1, 2, 5, 10, 15, 30];
        private static readonly double[] RightAscensionSteps = [1.25, 2.5, 3.75, 7.5, 15];
        private readonly IReadOnlyList<ConstellationBoundary> boundaries;
        private readonly IReadOnlyList<ConstellationData> constellations;
        private readonly DeepSkyObjectIndex deepSkyObjects;
        private readonly IReadOnlyList<Star> stars;

        public SkyMapSceneBuilder(
            IReadOnlyList<Constellation> constellations,
            IReadOnlyList<DeepSkyObject> deepSkyObjects,
            IReadOnlyList<ConstellationBoundary> boundaries) {
            this.constellations = constellations.Select(x => new ConstellationData(x, ConstellationCenter(x))).ToArray();
            this.deepSkyObjects = new DeepSkyObjectIndex(deepSkyObjects);
            this.boundaries = boundaries;
            stars = constellations.SelectMany(x => x.Stars).DistinctBy(x => x.Id).ToArray();
        }

        public SkyMapScene Build(
            SkyMapViewportProjection projection,
            SkyMapRenderOptions options,
            IReadOnlyList<string> disabledCatalogues = null) {
            SkyMapObserverSnapshot observer = projection.Observer;
            if ((options & (SkyMapRenderOptions.HorizontalGrid | SkyMapRenderOptions.Horizon)) != 0 && observer is null) {
                throw new ArgumentException("Horizontal rendering requires a projection with an observer.", nameof(projection));
            }
            SkyMapObserverSnapshot visibilityObserver = (options & SkyMapRenderOptions.Horizon) != 0 ? observer : null;
            ViewportFoV viewport = projection.Viewport;
            List<SkyMapStar> stars = [];
            List<SkyMapLine> constellationLines = [];
            List<SkyMapDeepSkyObject> dsos = [];
            List<SkyMapPath> constellationBoundaries = [];
            List<SkyMapPath> gridLines = [];
            List<SkyMapLine> horizonLines = [];
            List<SkyMapPath> horizonMaskAreas = [];
            List<SkyMapLabel> labels = [];

            if ((options & (SkyMapRenderOptions.Stars | SkyMapRenderOptions.Constellations)) != 0) {
                BuildConstellations(viewport, projection, options, visibilityObserver, stars, constellationLines, labels);
            }
            if ((options & SkyMapRenderOptions.DeepSkyObjects) != 0) {
                BuildDeepSkyObjects(viewport, projection, visibilityObserver, disabledCatalogues, dsos);
            }
            if ((options & SkyMapRenderOptions.ConstellationBoundaries) != 0) {
                BuildBoundaries(projection, visibilityObserver, constellationBoundaries);
            }
            if ((options & SkyMapRenderOptions.EquatorialGrid) != 0) {
                BuildGrid(viewport, projection, visibilityObserver, gridLines, labels);
            }
            if ((options & SkyMapRenderOptions.HorizontalGrid) != 0 && observer is not null) {
                BuildHorizontalGrid(
                    viewport,
                    projection,
                    observer,
                    (options & SkyMapRenderOptions.Horizon) != 0,
                    gridLines,
                    labels);
            }
            if ((options & SkyMapRenderOptions.Horizon) != 0 && observer is not null) {
                BuildHorizon(viewport, projection, observer, horizonLines, horizonMaskAreas);
            }

            return new SkyMapScene(
                stars,
                constellationLines,
                dsos,
                constellationBoundaries,
                gridLines,
                labels,
                horizonLines,
                horizonMaskAreas);
        }

        private void BuildConstellations(
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            SkyMapRenderOptions options,
            SkyMapObserverSnapshot visibilityObserver,
            List<SkyMapStar> stars,
            List<SkyMapLine> lines,
            List<SkyMapLabel> labels) {
            if ((options & SkyMapRenderOptions.Stars) != 0) {
                foreach (Star star in this.stars) {
                    if (projection.Contains(star.Coords) && IsVisible(visibilityObserver, star.Coords)) {
                        double radius = Math.Max(1, (-3.375 * star.Mag + 23.25) / (viewport.VFoV / 8));
                        Point center = projection.Project(star.Coords);
                        stars.Add(new SkyMapStar(center, radius));
                        if ((options & SkyMapRenderOptions.Constellations) != 0 && !string.IsNullOrWhiteSpace(star.Name)) {
                            labels.Add(new SkyMapLabel(star.Name, new Point(center.X + radius, center.Y + radius * 2 + 5), SkyMapLabelKind.Star));
                        }
                    }
                }
            }

            if ((options & SkyMapRenderOptions.Constellations) != 0) {
                foreach (ConstellationData data in constellations) {
                    Constellation constellation = data.Constellation;
                    foreach (Tuple<Star, Star> connection in constellation.StarConnections) {
                        if ((projection.Contains(connection.Item1.Coords) || projection.Contains(connection.Item2.Coords))
                            && IsVisible(visibilityObserver, connection.Item1.Coords)
                            && IsVisible(visibilityObserver, connection.Item2.Coords)) {
                            lines.Add(new SkyMapLine(projection.Project(connection.Item1.Coords), projection.Project(connection.Item2.Coords)));
                        }
                    }
                    if (projection.Contains(data.Center) && IsVisible(visibilityObserver, data.Center)) {
                        labels.Add(new SkyMapLabel(constellation.Name, projection.Project(data.Center), SkyMapLabelKind.Constellation));
                    }
                }
            }
        }

        private static Coordinates ConstellationCenter(Constellation constellation) {
            double minimumDeclination = constellation.Stars.Min(x => x.Coords.Dec);
            double maximumDeclination = constellation.Stars.Max(x => x.Coords.Dec);
            double rightAscension;
            if (constellation.GoesOverRaZero) {
                double westernEdge = constellation.Stars.Where(x => x.Coords.RADegrees > 180).Min(x => x.Coords.RADegrees);
                double easternEdge = constellation.Stars.Where(x => x.Coords.RADegrees <= 180).Max(x => x.Coords.RADegrees);
                rightAscension = AstroUtil.EuclidianModulus(westernEdge + (easternEdge + 360 - westernEdge) / 2, 360);
            } else {
                double minimumRightAscension = constellation.Stars.Min(x => x.Coords.RADegrees);
                double maximumRightAscension = constellation.Stars.Max(x => x.Coords.RADegrees);
                rightAscension = (minimumRightAscension + maximumRightAscension) / 2;
            }
            return CelestialCoordinates(rightAscension, (minimumDeclination + maximumDeclination) / 2);
        }

        private void BuildDeepSkyObjects(
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            SkyMapObserverSnapshot visibilityObserver,
            IReadOnlyList<string> disabledCatalogues,
            List<SkyMapDeepSkyObject> result) {
            double minimumSize = Math.Min(viewport.HFoV, viewport.VFoV) < 10
                ? 0
                : 3 * Math.Min(viewport.ArcSecWidth, viewport.ArcSecHeight);
            double maximumSize = AstroUtil.DegreeToArcsec(2 * Math.Max(viewport.HFoV, viewport.VFoV));

            foreach (DeepSkyObject dso in deepSkyObjects.Query(viewport)) {
                if (!projection.Contains(dso.Coordinates)
                    || !IsVisible(visibilityObserver, dso.Coordinates)
                    || IsDisabled(dso.Name, disabledCatalogues)
                    || (viewport.VFoV > 10 && (dso.Size is null || dso.Size <= minimumSize || dso.Size >= maximumSize))) {
                    continue;
                }

                double width = dso.Size >= viewport.ArcSecWidth ? dso.Size.Value : 30;
                double height = dso.PositionAngle is null
                    ? width
                    : dso.SizeMin >= viewport.ArcSecHeight ? dso.SizeMin.Value : 30;
                Point center = projection.Project(dso.Coordinates);
                result.Add(new SkyMapDeepSkyObject(
                    DsoLabel(dso),
                    dso.DSOType,
                    center,
                    width / viewport.ArcSecWidth / 2,
                    height / viewport.ArcSecHeight / 2,
                    AdjustedPositionAngle(dso, viewport, projection, center)));
            }
        }

        private static double AdjustedPositionAngle(
            DeepSkyObject dso,
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            Point center) {
            if (dso.PositionAngle is null) {
                return 0;
            }

            double positionAngle = 90 - dso.PositionAngle.Degree;
            if (projection.Mode == SkyMapProjectionMode.AltAz) {
                return projection.RotationForPositionAngle(dso.Coordinates, dso.PositionAngle.Degree, center);
            }

            if (Math.Abs(viewport.CenterCoordinates.RA - dso.Coordinates.RA) <= 1E-13
                && Math.Abs(viewport.CenterCoordinates.Dec - dso.Coordinates.Dec) <= 1E-13) {
                return positionAngle;
            }

            double panelDeltaX = center.X - viewport.ViewPortCenterPoint.X;
            double panelDeltaY = center.Y - viewport.ViewPortCenterPoint.Y;
            Coordinates referenceCenter = viewport.CenterCoordinates.Shift(
                panelDeltaX < 1E-10 ? 1 : 0,
                panelDeltaY,
                viewport.Rotation,
                viewport.ArcSecWidth,
                viewport.ArcSecHeight);
            double adjusted = positionAngle - (90 - AstroUtil.CalculatePositionAngle(
                referenceCenter.RADegrees,
                dso.Coordinates.RADegrees,
                referenceCenter.Dec,
                dso.Coordinates.Dec));
            return adjusted;
        }

        private static string DsoLabel(DeepSkyObject dso) {
            string first = dso.Name;
            string second = dso.AlsoKnownAs.FirstOrDefault(x => x.StartsWith("M ", StringComparison.Ordinal));
            string third = dso.AlsoKnownAs.FirstOrDefault(x => x.StartsWith("NGC ", StringComparison.Ordinal));
            if (third is not null && first == third.Replace(" ", string.Empty)) {
                first = null;
            }

            bool includeFirst = !string.IsNullOrWhiteSpace(first);
            bool includeSecond = !string.IsNullOrWhiteSpace(second)
                && (!includeFirst || !string.Equals(first, second, StringComparison.Ordinal));
            bool includeThird = !string.IsNullOrWhiteSpace(third)
                && (!includeFirst || !string.Equals(first, third, StringComparison.Ordinal))
                && (!includeSecond || !string.Equals(second, third, StringComparison.Ordinal));
            if (includeFirst) {
                if (includeSecond) {
                    return includeThird
                        ? string.Concat(first, Environment.NewLine, second, Environment.NewLine, third)
                        : string.Concat(first, Environment.NewLine, second);
                }
                return includeThird ? string.Concat(first, Environment.NewLine, third) : first;
            }
            if (includeSecond) {
                return includeThird ? string.Concat(second, Environment.NewLine, third) : second;
            }
            return includeThird ? third : string.Empty;
        }

        private static bool IsDisabled(string name, IReadOnlyList<string> disabledCatalogues) {
            if (disabledCatalogues is null) {
                return false;
            }
            foreach (string catalogue in disabledCatalogues) {
                if (name.StartsWith(catalogue, StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }

        private void BuildBoundaries(SkyMapViewportProjection projection, SkyMapObserverSnapshot visibilityObserver, List<SkyMapPath> result) {
            foreach (ConstellationBoundary boundary in boundaries) {
                IReadOnlyList<Coordinates> coordinates = boundary.Boundaries;
                bool intersects = false;
                for (int i = 0; i < coordinates.Count; i++) {
                    if (projection.Contains(coordinates[i])) {
                        intersects = true;
                        break;
                    }
                }
                if (!intersects) {
                    continue;
                }

                bool allVisible = true;
                for (int i = 0; i < coordinates.Count; i++) {
                    if (!IsVisible(visibilityObserver, coordinates[i])) {
                        allVisible = false;
                        break;
                    }
                }
                if (!allVisible) {
                    AddVisiblePath(coordinates, projection, visibilityObserver, result);
                    continue;
                }

                Point[] points = new Point[coordinates.Count];
                for (int i = 0; i < coordinates.Count; i++) {
                    points[i] = projection.Project(coordinates[i]);
                }
                result.Add(new SkyMapPath(points, closed: true));
            }
        }

        private static void BuildGrid(
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            SkyMapObserverSnapshot visibilityObserver,
            List<SkyMapPath> result,
            List<SkyMapLabel> labels) {
            double declinationStep = ClosestStep(DeclinationSteps, viewport.VFoV / 4);
            double rightAscensionStep = ClosestStep(RightAscensionSteps, viewport.HFoV / 4);
            double sampleStep = Math.Min(declinationStep, rightAscensionStep) / 4;
            double radius = Math.Max(viewport.HFoV, viewport.VFoV) + sampleStep;

            for (double ra = 0; ra < 360; ra += rightAscensionStep) {
                Coordinates center = CelestialCoordinates(ra, viewport.CenterCoordinates.Dec);
                if (!projection.Contains(center)) {
                    continue;
                }

                List<Coordinates> points = [];
                double from = Math.Max(-89.999, viewport.CenterCoordinates.Dec - radius);
                double through = Math.Min(89.999, viewport.CenterCoordinates.Dec + radius);
                for (double dec = from; dec <= through; dec += sampleStep) {
                    points.Add(CelestialCoordinates(ra, dec));
                }
                int previousCount = result.Count;
                AddVisiblePath(points, projection, visibilityObserver, result);
                AddGridLabels(result, previousCount, labels, FormatRightAscension(ra), viewport);
            }

            double raRadius = Math.Min(180, radius / Math.Max(0.05, Math.Cos(AstroUtil.ToRadians(viewport.CenterCoordinates.Dec))));
            for (double dec = -Math.Floor(89.999 / declinationStep) * declinationStep; dec < 90; dec += declinationStep) {
                Coordinates center = CelestialCoordinates(viewport.CenterCoordinates.RADegrees, dec);
                if (!projection.Contains(center)) {
                    continue;
                }

                List<Coordinates> points = [];
                for (double offset = -raRadius; offset <= raRadius; offset += sampleStep) {
                    points.Add(CelestialCoordinates(viewport.CenterCoordinates.RADegrees + offset, dec));
                }
                int previousCount = result.Count;
                AddVisiblePath(points, projection, visibilityObserver, result, dec == 0 ? 3 : 1);
                AddGridLabels(result, previousCount, labels, $"{dec:N2}°", viewport);
            }
        }

        private static void AddGridLabels(
            List<SkyMapPath> paths,
            int fromIndex,
            List<SkyMapLabel> labels,
            string text,
            ViewportFoV viewport) {
            for (int i = fromIndex; i < paths.Count; i++) {
                Point position = default;
                IReadOnlyList<Point> points = paths[i].Points;
                for (int j = 0; j < points.Count; j++) {
                    Point candidate = points[j];
                    if (candidate.X >= 0
                        && candidate.X < viewport.Width
                        && candidate.Y >= 0
                        && candidate.Y < viewport.Height) {
                        position = candidate;
                        break;
                    }
                }
                if (position != default) {
                    labels.Add(new SkyMapLabel(text, position, SkyMapLabelKind.Grid));
                }
            }
        }

        private static void BuildHorizontalGrid(
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            SkyMapObserverSnapshot observer,
            bool hideBelowHorizon,
            List<SkyMapPath> result,
            List<SkyMapLabel> labels) {
            double step = ClosestStep(HorizontalSteps, Math.Max(viewport.HFoV, viewport.VFoV) / 4);
            double sampleStep = step / 4;
            double radius = Math.Max(viewport.HFoV, viewport.VFoV) + sampleStep;
            SkyMapHorizontalCoordinates center = observer.ToHorizontal(viewport.CenterCoordinates);

            for (double azimuth = 0; azimuth < 360; azimuth += step) {
                SkyMapHorizontalCoordinates lineCenter = new SkyMapHorizontalCoordinates(center.Altitude, azimuth);
                if (!projection.Contains(lineCenter)) {
                    continue;
                }

                int previousCount = result.Count;
                List<Point> points = [];
                double from = Math.Max(-89.999, center.Altitude - radius);
                double through = Math.Min(89.999, center.Altitude + radius);
                for (double altitude = from; altitude <= through; altitude += sampleStep) {
                    SkyMapHorizontalCoordinates horizontal = new SkyMapHorizontalCoordinates(altitude, azimuth);
                    if (projection.Contains(horizontal)
                        && (!hideBelowHorizon || observer.HorizonClearance(horizontal) >= 0)) {
                        points.Add(projection.Project(horizontal));
                    } else {
                        points = CompletePath(points, result);
                    }
                }
                AddPathIfDrawable(points, result);
                AddGridLabels(result, previousCount, labels, $"{azimuth:N0}°", viewport);
            }

            for (double altitude = -Math.Floor(89.999 / step) * step; altitude < 90; altitude += step) {
                SkyMapHorizontalCoordinates lineCenter = new SkyMapHorizontalCoordinates(altitude, center.Azimuth);
                if (!projection.Contains(lineCenter)) {
                    continue;
                }

                int previousCount = result.Count;
                List<Point> points = [];
                for (double azimuth = 0; azimuth < 360; azimuth += sampleStep) {
                    SkyMapHorizontalCoordinates horizontal = new SkyMapHorizontalCoordinates(altitude, azimuth);
                    if (projection.Contains(horizontal)
                        && (!hideBelowHorizon || observer.HorizonClearance(horizontal) >= 0)) {
                        points.Add(projection.Project(horizontal));
                    } else {
                        points = CompletePath(points, result, altitude == 0 ? 3 : 1);
                    }
                }
                AddPathIfDrawable(points, result, altitude == 0 ? 3 : 1);
                AddGridLabels(result, previousCount, labels, $"{altitude:N0}°", viewport);
            }

            AddCardinalDirectionLabels(viewport, projection, labels);
        }

        private static void AddCardinalDirectionLabels(
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            List<SkyMapLabel> labels) {
            foreach ((double azimuth, string direction) in CardinalDirections) {
                SkyMapHorizontalCoordinates horizontal = new SkyMapHorizontalCoordinates(0, azimuth);
                if (!projection.Contains(horizontal)) {
                    continue;
                }

                Point position = projection.Project(horizontal);
                if (position.X >= 0
                    && position.X < viewport.Width
                    && position.Y >= 0
                    && position.Y < viewport.Height) {
                    labels.Add(new SkyMapLabel(direction, position, SkyMapLabelKind.CardinalDirection));
                }
            }
        }

        private static void BuildHorizon(
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            SkyMapObserverSnapshot observer,
            List<SkyMapLine> result,
            List<SkyMapPath> maskAreas) {
            BuildHorizonMask(viewport, projection, observer, maskAreas, result);
        }

        private static void BuildHorizonMask(
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            SkyMapObserverSnapshot observer,
            List<SkyMapPath> result,
            List<SkyMapLine> horizonLines) {
            if (observer.HasFlatHorizon) {
                double centerAltitude = observer.ToHorizontal(viewport.CenterCoordinates).Altitude;
                if (centerAltitude >= projection.AngularRadius) {
                    return;
                }
                if (centerAltitude <= -projection.AngularRadius) {
                    result.Add(ViewportRectangle(viewport));
                    return;
                }
            }

            const double targetCellSize = 12;
            int columns = Math.Max(1, (int)Math.Ceiling(viewport.Width / targetCellSize));
            int rows = Math.Max(1, (int)Math.Ceiling(viewport.Height / targetCellSize));
            double cellWidth = viewport.Width / columns;
            double cellHeight = viewport.Height / rows;
            double[,] clearance = new double[rows + 1, columns + 1];
            bool allHidden = true;
            bool allVisible = true;
            for (int row = 0; row <= rows; row++) {
                for (int column = 0; column <= columns; column++) {
                    Point point = new Point(column * cellWidth, row * cellHeight);
                    double value = observer.HorizonClearance(projection.UnprojectHorizontal(point));
                    clearance[row, column] = value;
                    allHidden &= value < 0;
                    allVisible &= value >= 0;
                }
            }

            if (allVisible) {
                return;
            }
            if (allHidden) {
                result.Add(ViewportRectangle(viewport));
                return;
            }

            for (int row = 0; row < rows; row++) {
                int hiddenRunStart = -1;
                for (int column = 0; column < columns; column++) {
                    Point topLeft = new Point(column * cellWidth, row * cellHeight);
                    Point topRight = new Point((column + 1) * cellWidth, row * cellHeight);
                    Point bottomRight = new Point((column + 1) * cellWidth, (row + 1) * cellHeight);
                    Point bottomLeft = new Point(column * cellWidth, (row + 1) * cellHeight);
                    double topLeftClearance = clearance[row, column];
                    double topRightClearance = clearance[row, column + 1];
                    double bottomRightClearance = clearance[row + 1, column + 1];
                    double bottomLeftClearance = clearance[row + 1, column];
                    bool cellHidden = topLeftClearance < 0
                        && topRightClearance < 0
                        && bottomRightClearance < 0
                        && bottomLeftClearance < 0;
                    if (cellHidden) {
                        hiddenRunStart = hiddenRunStart < 0 ? column : hiddenRunStart;
                        continue;
                    }

                    AddHiddenRun(result, hiddenRunStart, column, row, cellWidth, cellHeight);
                    hiddenRunStart = -1;
                    bool cellVisible = topLeftClearance >= 0
                        && topRightClearance >= 0
                        && bottomRightClearance >= 0
                        && bottomLeftClearance >= 0;
                    if (cellVisible) {
                        continue;
                    }
                    ReadOnlySpan<Point> corners = [topLeft, topRight, bottomRight, bottomLeft];
                    ReadOnlySpan<double> cornerClearance = [
                        topLeftClearance,
                        topRightClearance,
                        bottomRightClearance,
                        bottomLeftClearance
                    ];
                    AddPartiallyHiddenCell(
                        result,
                        horizonLines,
                        projection,
                        observer,
                        corners,
                        cornerClearance);
                }
                AddHiddenRun(result, hiddenRunStart, columns, row, cellWidth, cellHeight);
            }
        }

        private static SkyMapPath ViewportRectangle(ViewportFoV viewport) {
            return new SkyMapPath(
                [
                    new Point(0, 0),
                    new Point(viewport.Width, 0),
                    new Point(viewport.Width, viewport.Height),
                    new Point(0, viewport.Height)
                ],
                closed: true);
        }

        private static void AddHiddenRun(
            List<SkyMapPath> result,
            int fromColumn,
            int throughColumn,
            int row,
            double cellWidth,
            double cellHeight) {
            if (fromColumn < 0) {
                return;
            }
            double left = fromColumn * cellWidth;
            double right = throughColumn * cellWidth;
            double top = row * cellHeight;
            double bottom = (row + 1) * cellHeight;
            result.Add(new SkyMapPath(
                [new Point(left, top), new Point(right, top), new Point(right, bottom), new Point(left, bottom)],
                closed: true));
        }

        private static void AddPartiallyHiddenCell(
            List<SkyMapPath> result,
            List<SkyMapLine> horizonLines,
            SkyMapViewportProjection projection,
            SkyMapObserverSnapshot observer,
            ReadOnlySpan<Point> corners,
            ReadOnlySpan<double> clearance) {
            int hiddenCorners = 0;
            int pattern = 0;
            for (int i = 0; i < corners.Length; i++) {
                if (clearance[i] < 0) {
                    hiddenCorners++;
                    pattern |= 1 << i;
                }
            }
            if (hiddenCorners == 0 || hiddenCorners == corners.Length) {
                return;
            }

            if (pattern == 5 || pattern == 10) {
                for (int i = 0; i < corners.Length; i++) {
                    if (clearance[i] >= 0) {
                        continue;
                    }
                    int previous = (i + corners.Length - 1) % corners.Length;
                    int next = (i + 1) % corners.Length;
                    Point nextIntersection = HorizonIntersection(
                        projection,
                        observer,
                        corners[i],
                        clearance[i],
                        corners[next],
                        clearance[next]);
                    Point previousIntersection = HorizonIntersection(
                        projection,
                        observer,
                        corners[i],
                        clearance[i],
                        corners[previous],
                        clearance[previous]);
                    result.Add(new SkyMapPath(
                        [
                            corners[i],
                            nextIntersection,
                            previousIntersection
                        ],
                        closed: true));
                    horizonLines.Add(new SkyMapLine(nextIntersection, previousIntersection));
                }
                return;
            }

            List<Point> polygon = [];
            Point firstIntersection = default;
            Point secondIntersection = default;
            int intersectionCount = 0;
            int previousIndex = corners.Length - 1;
            for (int currentIndex = 0; currentIndex < corners.Length; currentIndex++) {
                bool previousHidden = clearance[previousIndex] < 0;
                bool currentHidden = clearance[currentIndex] < 0;
                if (previousHidden != currentHidden) {
                    Point intersection = HorizonIntersection(
                        projection,
                        observer,
                        corners[previousIndex],
                        clearance[previousIndex],
                        corners[currentIndex],
                        clearance[currentIndex]);
                    polygon.Add(intersection);
                    if (intersectionCount == 0) {
                        firstIntersection = intersection;
                    } else {
                        secondIntersection = intersection;
                    }
                    intersectionCount++;
                }
                if (currentHidden) {
                    polygon.Add(corners[currentIndex]);
                }
                previousIndex = currentIndex;
            }
            result.Add(new SkyMapPath(polygon, closed: true));
            if (intersectionCount == 2) {
                horizonLines.Add(new SkyMapLine(firstIntersection, secondIntersection));
            }
        }

        private static Point HorizonIntersection(
            SkyMapViewportProjection projection,
            SkyMapObserverSnapshot observer,
            Point from,
            double fromClearance,
            Point through,
            double throughClearance) {
            const double clearanceTolerance = 0.01;
            const int maximumIterations = 12;
            Point intersection = default;
            for (int i = 0; i < maximumIterations; i++) {
                double amount = i == 0
                    ? fromClearance / (fromClearance - throughClearance)
                    : 0.5;
                intersection = new Point(
                    from.X + (through.X - from.X) * amount,
                    from.Y + (through.Y - from.Y) * amount);
                if (observer.HasFlatHorizon) {
                    break;
                }
                double intersectionClearance = observer.HorizonClearance(projection.UnprojectHorizontal(intersection));
                if (Math.Abs(intersectionClearance) <= clearanceTolerance) {
                    break;
                }

                if ((intersectionClearance < 0) == (fromClearance < 0)) {
                    from = intersection;
                    fromClearance = intersectionClearance;
                } else {
                    through = intersection;
                    throughClearance = intersectionClearance;
                }
            }
            return intersection;
        }

        private static string FormatRightAscension(double rightAscension) {
            string text = AstroUtil.HoursToHMS(AstroUtil.DegreesToHours(rightAscension));
            return $"{text[..^3]}h";
        }

        private static void AddVisiblePath(
            IReadOnlyList<Coordinates> coordinates,
            SkyMapViewportProjection projection,
            SkyMapObserverSnapshot visibilityObserver,
            List<SkyMapPath> result,
            double strokeThickness = 1) {
            List<Point> points = [];
            foreach (Coordinates coordinate in coordinates) {
                if (IsVisible(visibilityObserver, coordinate)) {
                    points.Add(projection.Project(coordinate));
                } else {
                    points = CompletePath(points, result, strokeThickness);
                }
            }
            AddPathIfDrawable(points, result, strokeThickness);
        }

        private static List<Point> CompletePath(
            List<Point> points,
            List<SkyMapPath> result,
            double strokeThickness = 1) {
            if (points.Count > 1) {
                result.Add(new SkyMapPath(points, strokeThickness: strokeThickness));
                return [];
            }
            points.Clear();
            return points;
        }

        private static void AddPathIfDrawable(
            List<Point> points,
            List<SkyMapPath> result,
            double strokeThickness = 1) {
            if (points.Count > 1) {
                result.Add(new SkyMapPath(points, strokeThickness: strokeThickness));
            }
        }

        private static bool IsVisible(SkyMapObserverSnapshot observer, Coordinates coordinates) {
            return observer is null || observer.IsVisible(coordinates);
        }

        private static Coordinates CelestialCoordinates(double rightAscension, double declination) {
            return new Coordinates(AstroUtil.EuclidianModulus(rightAscension, 360), declination, Epoch.J2000, Coordinates.RAType.Degrees);
        }

        private static double ClosestStep(double[] steps, double desired) {
            double closest = steps[0];
            double difference = Math.Abs(closest - desired);
            for (int i = 1; i < steps.Length; i++) {
                double candidateDifference = Math.Abs(steps[i] - desired);
                if (candidateDifference < difference) {
                    closest = steps[i];
                    difference = candidateDifference;
                }
            }
            return closest;
        }

        private readonly record struct ConstellationData(Constellation Constellation, Coordinates Center);

        private sealed class DeepSkyObjectIndex {
            private const double BinSize = 5;
            private const int DeclinationBinCount = 36;
            private const int RightAscensionBinCount = 72;
            private readonly List<DeepSkyObject>[,] bins = new List<DeepSkyObject>[RightAscensionBinCount, DeclinationBinCount];

            public DeepSkyObjectIndex(IReadOnlyList<DeepSkyObject> deepSkyObjects) {
                foreach (DeepSkyObject dso in deepSkyObjects) {
                    int rightAscensionBin = RightAscensionBin(dso.Coordinates.RADegrees);
                    int declinationBin = DeclinationBin(dso.Coordinates.Dec);
                    (bins[rightAscensionBin, declinationBin] ??= []).Add(dso);
                }
            }

            public IEnumerable<DeepSkyObject> Query(ViewportFoV viewport) {
                double radius = Math.Max(viewport.HFoV, viewport.VFoV);
                int minimumDeclinationBin = DeclinationBin(Math.Max(-90, viewport.CenterCoordinates.Dec - radius));
                int maximumDeclinationBin = DeclinationBin(Math.Min(90, viewport.CenterCoordinates.Dec + radius));
                foreach (int rightAscensionBin in RightAscensionBins(viewport.CenterCoordinates, radius)) {
                    for (int declinationBin = minimumDeclinationBin; declinationBin <= maximumDeclinationBin; declinationBin++) {
                        List<DeepSkyObject> bin = bins[rightAscensionBin, declinationBin];
                        if (bin is null) {
                            continue;
                        }
                        foreach (DeepSkyObject dso in bin) {
                            yield return dso;
                        }
                    }
                }
            }

            private static IEnumerable<int> RightAscensionBins(Coordinates center, double radius) {
                if (radius >= 90 - Math.Abs(center.Dec)) {
                    for (int i = 0; i < RightAscensionBinCount; i++) {
                        yield return i;
                    }
                    yield break;
                }

                double sineRatio = Math.Sin(AstroUtil.ToRadians(radius))
                    / Math.Cos(AstroUtil.ToRadians(center.Dec));
                double rightAscensionRadius = AstroUtil.ToDegree(Math.Asin(Math.Clamp(Math.Abs(sineRatio), 0, 1)));

                int start = RightAscensionBin(center.RADegrees - rightAscensionRadius);
                int end = RightAscensionBin(center.RADegrees + rightAscensionRadius);
                int current = start;
                while (true) {
                    yield return current;
                    if (current == end) {
                        yield break;
                    }
                    current = (current + 1) % RightAscensionBinCount;
                }
            }

            private static int RightAscensionBin(double rightAscension) {
                return (int)(AstroUtil.EuclidianModulus(rightAscension, 360) / BinSize) % RightAscensionBinCount;
            }

            private static int DeclinationBin(double declination) {
                return Math.Clamp((int)((declination + 90) / BinSize), 0, DeclinationBinCount - 1);
            }
        }
    }
}
