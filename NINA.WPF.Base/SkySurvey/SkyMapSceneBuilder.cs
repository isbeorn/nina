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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NINA.WPF.Base.SkySurvey {

    public interface ISkyMapVisibility {
        bool IsVisible(Coordinates coordinates);
    }

    [Flags]
    public enum SkyMapRenderOptions {
        None = 0,
        Stars = 1,
        Constellations = 2,
        DeepSkyObjects = 4,
        ConstellationBoundaries = 8,
        EquatorialGrid = 16,
        All = Stars | Constellations | DeepSkyObjects | ConstellationBoundaries | EquatorialGrid
    }

    public sealed class SkyMapScene {
        public SkyMapScene(
            IReadOnlyList<SkyMapStar> stars,
            IReadOnlyList<SkyMapLine> constellationLines,
            IReadOnlyList<SkyMapDeepSkyObject> deepSkyObjects,
            IReadOnlyList<SkyMapPath> constellationBoundaries,
            IReadOnlyList<SkyMapPath> gridLines) {
            Stars = stars;
            ConstellationLines = constellationLines;
            DeepSkyObjects = deepSkyObjects;
            ConstellationBoundaries = constellationBoundaries;
            GridLines = gridLines;
            Labels = [];
        }

        public SkyMapScene(
            IReadOnlyList<SkyMapStar> stars,
            IReadOnlyList<SkyMapLine> constellationLines,
            IReadOnlyList<SkyMapDeepSkyObject> deepSkyObjects,
            IReadOnlyList<SkyMapPath> constellationBoundaries,
            IReadOnlyList<SkyMapPath> gridLines,
            IReadOnlyList<SkyMapLabel> labels) {
            Stars = stars;
            ConstellationLines = constellationLines;
            DeepSkyObjects = deepSkyObjects;
            ConstellationBoundaries = constellationBoundaries;
            GridLines = gridLines;
            Labels = labels;
        }

        public IReadOnlyList<SkyMapStar> Stars { get; }
        public IReadOnlyList<SkyMapLine> ConstellationLines { get; }
        public IReadOnlyList<SkyMapDeepSkyObject> DeepSkyObjects { get; }
        public IReadOnlyList<SkyMapPath> ConstellationBoundaries { get; }
        public IReadOnlyList<SkyMapPath> GridLines { get; }
        public IReadOnlyList<SkyMapLabel> Labels { get; }
    }

    public enum SkyMapLabelKind {
        Star,
        Constellation,
        Grid
    }

    public readonly record struct SkyMapLabel(string Text, Point Position, SkyMapLabelKind Kind);

    public readonly record struct SkyMapStar(int Id, string Name, Point Center, double Radius);

    public readonly record struct SkyMapLine(Point Start, Point End);

    public readonly record struct SkyMapDeepSkyObject(
        string Id,
        string Name,
        string Type,
        Point Center,
        double RadiusX,
        double RadiusY,
        double PositionAngle);

    public sealed class SkyMapPath {
        public SkyMapPath(IReadOnlyList<Point> points, double value = 0, bool closed = false, double strokeThickness = 1) {
            Points = points;
            Value = value;
            Closed = closed;
            StrokeThickness = strokeThickness;
        }

        public IReadOnlyList<Point> Points { get; }
        public double Value { get; }
        public bool Closed { get; }
        public double StrokeThickness { get; }
    }

    public sealed class SkyMapSceneBuilder {
        private static readonly double[] DeclinationSteps = [0.5, 1, 2, 4, 12, 20];
        private static readonly double[] RightAscensionSteps = [1.25, 2.5, 3.75, 7.5, 15];
        private readonly IReadOnlyList<ConstellationBoundary> boundaries;
        private readonly IReadOnlyList<ConstellationData> constellations;
        private readonly DeepSkyObjectIndex deepSkyObjects;

        public SkyMapSceneBuilder(
            IReadOnlyList<Constellation> constellations,
            IReadOnlyList<DeepSkyObject> deepSkyObjects,
            IReadOnlyList<ConstellationBoundary> boundaries) {
            this.constellations = constellations.Select(x => new ConstellationData(x, ConstellationCenter(x))).ToArray();
            this.deepSkyObjects = new DeepSkyObjectIndex(deepSkyObjects);
            this.boundaries = boundaries;
        }

        public SkyMapScene Build(ViewportFoV viewport, SkyMapRenderOptions options) {
            return Build(viewport, options, AllSkyMapVisibility.Instance, null);
        }

        public SkyMapScene Build(ViewportFoV viewport, SkyMapRenderOptions options, ISkyMapVisibility visibility) {
            return Build(viewport, options, visibility, null);
        }

        public SkyMapScene Build(ViewportFoV viewport, SkyMapRenderOptions options, IReadOnlySet<string> disabledCatalogues) {
            return Build(viewport, options, AllSkyMapVisibility.Instance, disabledCatalogues);
        }

        public SkyMapScene Build(
            ViewportFoV viewport,
            SkyMapRenderOptions options,
            ISkyMapVisibility visibility,
            IReadOnlySet<string> disabledCatalogues) {
            SkyMapProjection projection = new SkyMapProjection(viewport);
            List<SkyMapStar> stars = [];
            List<SkyMapLine> constellationLines = [];
            List<SkyMapDeepSkyObject> dsos = [];
            List<SkyMapPath> constellationBoundaries = [];
            List<SkyMapPath> gridLines = [];
            List<SkyMapLabel> labels = [];

            if ((options & (SkyMapRenderOptions.Stars | SkyMapRenderOptions.Constellations)) != 0) {
                BuildConstellations(viewport, projection, options, visibility, stars, constellationLines, labels);
            }
            if ((options & SkyMapRenderOptions.DeepSkyObjects) != 0) {
                BuildDeepSkyObjects(viewport, projection, visibility, disabledCatalogues, dsos);
            }
            if ((options & SkyMapRenderOptions.ConstellationBoundaries) != 0) {
                BuildBoundaries(projection, visibility, constellationBoundaries);
            }
            if ((options & SkyMapRenderOptions.EquatorialGrid) != 0) {
                BuildGrid(viewport, projection, visibility, gridLines, labels);
            }

            return new SkyMapScene(stars, constellationLines, dsos, constellationBoundaries, gridLines, labels);
        }

        private void BuildConstellations(
            ViewportFoV viewport,
            SkyMapProjection projection,
            SkyMapRenderOptions options,
            ISkyMapVisibility visibility,
            List<SkyMapStar> stars,
            List<SkyMapLine> lines,
            List<SkyMapLabel> labels) {
            HashSet<int> addedStars = [];
            foreach (ConstellationData data in constellations) {
                Constellation constellation = data.Constellation;
                if ((options & SkyMapRenderOptions.Stars) != 0) {
                    foreach (Star star in constellation.Stars) {
                        if (addedStars.Add(star.Id) && projection.Contains(star.Coords) && visibility.IsVisible(star.Coords)) {
                            double radius = Math.Max(1, (-3.375 * star.Mag + 23.25) / (viewport.VFoV / 8));
                            Point center = projection.Project(star.Coords);
                            stars.Add(new SkyMapStar(star.Id, star.Name, center, radius));
                            if ((options & SkyMapRenderOptions.Constellations) != 0 && !string.IsNullOrWhiteSpace(star.Name)) {
                                labels.Add(new SkyMapLabel(star.Name, new Point(center.X + radius, center.Y + radius * 2 + 5), SkyMapLabelKind.Star));
                            }
                        }
                    }
                }

                if ((options & SkyMapRenderOptions.Constellations) != 0) {
                    foreach (Tuple<Star, Star> connection in constellation.StarConnections) {
                        if ((projection.Contains(connection.Item1.Coords) || projection.Contains(connection.Item2.Coords))
                            && visibility.IsVisible(connection.Item1.Coords)
                            && visibility.IsVisible(connection.Item2.Coords)) {
                            lines.Add(new SkyMapLine(projection.Project(connection.Item1.Coords), projection.Project(connection.Item2.Coords)));
                        }
                    }
                    if (projection.Contains(data.Center) && visibility.IsVisible(data.Center)) {
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
            SkyMapProjection projection,
            ISkyMapVisibility visibility,
            IReadOnlySet<string> disabledCatalogues,
            List<SkyMapDeepSkyObject> result) {
            double minimumSize = Math.Min(viewport.HFoV, viewport.VFoV) < 10
                ? 0
                : 3 * Math.Min(viewport.ArcSecWidth, viewport.ArcSecHeight);
            double maximumSize = AstroUtil.DegreeToArcsec(2 * Math.Max(viewport.HFoV, viewport.VFoV));

            foreach (DeepSkyObject dso in deepSkyObjects.Query(viewport)) {
                if (!projection.Contains(dso.Coordinates)
                    || !visibility.IsVisible(dso.Coordinates)
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
                    dso.Id,
                    DsoLabel(dso),
                    dso.DSOType,
                    center,
                    width / viewport.ArcSecWidth / 2,
                    height / viewport.ArcSecHeight / 2,
                    AdjustedPositionAngle(dso, viewport, center)));
            }
        }

        private static double AdjustedPositionAngle(DeepSkyObject dso, ViewportFoV viewport, Point center) {
            if (dso.PositionAngle is null) {
                return 0;
            }

            double positionAngle = 90 - dso.PositionAngle.Degree;
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
            return positionAngle - (90 - AstroUtil.CalculatePositionAngle(
                referenceCenter.RADegrees,
                dso.Coordinates.RADegrees,
                referenceCenter.Dec,
                dso.Coordinates.Dec));
        }

        private static string DsoLabel(DeepSkyObject dso) {
            string first = dso.Name;
            string second = dso.AlsoKnownAs.FirstOrDefault(x => x.StartsWith("M ", StringComparison.Ordinal));
            string third = dso.AlsoKnownAs.FirstOrDefault(x => x.StartsWith("NGC ", StringComparison.Ordinal));
            if (third is not null && first == third.Replace(" ", string.Empty)) {
                first = null;
            }
            return string.Join(Environment.NewLine, new[] { first, second, third }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        }

        private static bool IsDisabled(string name, IReadOnlySet<string> disabledCatalogues) {
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

        private void BuildBoundaries(SkyMapProjection projection, ISkyMapVisibility visibility, List<SkyMapPath> result) {
            foreach (ConstellationBoundary boundary in boundaries) {
                if (boundary.Boundaries.Any(projection.Contains)) {
                    if (boundary.Boundaries.All(visibility.IsVisible)) {
                        result.Add(new SkyMapPath(boundary.Boundaries.Select(projection.Project).ToArray(), closed: true));
                    } else {
                        AddVisiblePath(boundary.Boundaries, projection, visibility, result);
                    }
                }
            }
        }

        private static void BuildGrid(
            ViewportFoV viewport,
            SkyMapProjection projection,
            ISkyMapVisibility visibility,
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
                AddVisiblePath(points, projection, visibility, result, ra);
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
                AddVisiblePath(points, projection, visibility, result, dec, dec == 0 ? 3 : 1);
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
                Point position = paths[i].Points.FirstOrDefault(x =>
                    x.X >= 0
                    && x.X < viewport.Width
                    && x.Y >= 0
                    && x.Y < viewport.Height);
                if (position != default) {
                    labels.Add(new SkyMapLabel(text, position, SkyMapLabelKind.Grid));
                }
            }
        }

        private static string FormatRightAscension(double rightAscension) {
            string text = AstroUtil.HoursToHMS(AstroUtil.DegreesToHours(rightAscension));
            return $"{text[..^3]}h";
        }

        private static void AddVisiblePath(
            IReadOnlyList<Coordinates> coordinates,
            SkyMapProjection projection,
            ISkyMapVisibility visibility,
            List<SkyMapPath> result,
            double value = 0,
            double strokeThickness = 1) {
            List<Point> points = [];
            foreach (Coordinates coordinate in coordinates) {
                if (visibility.IsVisible(coordinate)) {
                    points.Add(projection.Project(coordinate));
                } else {
                    AddPathIfDrawable(points, result, value, strokeThickness);
                    points = [];
                }
            }
            AddPathIfDrawable(points, result, value, strokeThickness);
        }

        private static void AddPathIfDrawable(List<Point> points, List<SkyMapPath> result, double value, double strokeThickness) {
            if (points.Count > 1) {
                result.Add(new SkyMapPath(points, value, strokeThickness: strokeThickness));
            }
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

        private readonly struct SkyMapProjection {
            private const double ArcSecondsPerRadian = 180d * 3600 / Math.PI;
            private readonly double centerDeclinationCosine;
            private readonly double centerDeclinationSine;
            private readonly double centerRightAscension;
            private readonly double cosineRadius;
            private readonly double horizontalPixelsPerRadian;
            private readonly double rotationCosine;
            private readonly double rotationSine;
            private readonly double verticalPixelsPerRadian;
            private readonly double x;
            private readonly double y;

            public SkyMapProjection(ViewportFoV viewport) {
                centerRightAscension = AstroUtil.ToRadians(viewport.CenterCoordinates.RADegrees);
                double centerDeclination = AstroUtil.ToRadians(viewport.CenterCoordinates.Dec);
                centerDeclinationSine = Math.Sin(centerDeclination);
                centerDeclinationCosine = Math.Cos(centerDeclination);
                double rotation = AstroUtil.ToRadians(viewport.Rotation);
                rotationSine = Math.Sin(rotation);
                rotationCosine = Math.Cos(rotation);
                cosineRadius = Math.Cos(AstroUtil.ToRadians(Math.Max(viewport.HFoV, viewport.VFoV)));
                horizontalPixelsPerRadian = ArcSecondsPerRadian / viewport.ArcSecWidth;
                verticalPixelsPerRadian = ArcSecondsPerRadian / viewport.ArcSecHeight;
                x = viewport.ViewPortCenterPoint.X;
                y = viewport.ViewPortCenterPoint.Y;
            }

            public bool Contains(Coordinates coordinates) {
                return Contains(coordinates.RADegrees, coordinates.Dec);
            }

            public bool Contains(double rightAscension, double declination) {
                double declinationRadians = AstroUtil.ToRadians(declination);
                double cosineDistance = Math.Sin(declinationRadians) * centerDeclinationSine
                    + Math.Cos(declinationRadians) * centerDeclinationCosine
                    * Math.Cos(NormalizedRightAscension(rightAscension));
                return cosineDistance > cosineRadius;
            }

            public Point Project(Coordinates coordinates) {
                return Project(coordinates.RADegrees, coordinates.Dec);
            }

            public Point Project(double rightAscension, double declination) {
                double declinationRadians = AstroUtil.ToRadians(declination);
                double declinationSine = Math.Sin(declinationRadians);
                double declinationCosine = Math.Cos(declinationRadians);
                double rightAscensionDifference = NormalizedRightAscension(rightAscension);
                double rightAscensionCosine = Math.Cos(rightAscensionDifference);
                double scale = 2 / (1 + declinationSine * centerDeclinationSine
                    + declinationCosine * centerDeclinationCosine * rightAscensionCosine);
                double rightAscensionOffset = scale * Math.Sin(rightAscensionDifference) * declinationCosine;
                double declinationOffset = scale * (declinationSine * centerDeclinationCosine
                    - declinationCosine * centerDeclinationSine * rightAscensionCosine);
                double rotatedX = rightAscensionOffset * rotationCosine + declinationOffset * rotationSine;
                double rotatedY = declinationOffset * rotationCosine - rightAscensionOffset * rotationSine;
                return new Point(x - rotatedX * horizontalPixelsPerRadian, y - rotatedY * verticalPixelsPerRadian);
            }

            private double NormalizedRightAscension(double rightAscension) {
                double difference = AstroUtil.ToRadians(rightAscension) - centerRightAscension;
                if (difference > Math.PI) {
                    difference -= 2 * Math.PI;
                } else if (difference < -Math.PI) {
                    difference += 2 * Math.PI;
                }
                return difference;
            }
        }

        private sealed class AllSkyMapVisibility : ISkyMapVisibility {
            public static AllSkyMapVisibility Instance { get; } = new AllSkyMapVisibility();

            public bool IsVisible(Coordinates coordinates) {
                return true;
            }
        }

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
