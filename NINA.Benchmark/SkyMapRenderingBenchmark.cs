#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using BenchmarkDotNet.Attributes;
using NINA.Astrometry;
using NINA.Image.ImageAnalysis;
using NINA.WPF.Base.Model.FramingAssistant;
using NINA.WPF.Base.SkySurvey;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using DrawingPen = System.Drawing.Pen;
using Point = System.Windows.Point;

namespace NINA.Benchmark {

    [MemoryDiagnoser]
    [ShortRunJob]
    public class SkyMapRenderingBenchmark {
        private Bitmap bitmap = null!;
        private IReadOnlyList<ConstellationBoundary> boundaries = null!;
        private IReadOnlyList<Constellation> constellations = null!;
        private IReadOnlyList<DeepSkyObject> deepSkyObjects = null!;
        private Graphics graphics = null!;
        private FrameLineMatrix2 legacyGrid = null!;
        private List<FramingConstellation> legacyConstellations = null!;
        private List<FramingDSO> legacyDeepSkyObjects = null!;
        private SkyMapSceneBuilder renderer = null!;
        private SkyMapRasterRenderer rasterRenderer = null!;
        private SkyMapScene scene = null!;
        private ViewportFoV viewport = null!;

        [GlobalSetup]
        public void Setup() {
            constellations = CreateConstellations();
            deepSkyObjects = CreateDeepSkyObjects();
            boundaries = CreateBoundaries();
            viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);
            renderer = new SkyMapSceneBuilder(constellations, deepSkyObjects, boundaries);
            rasterRenderer = new SkyMapRasterRenderer((int)viewport.Width, (int)viewport.Height);
            scene = renderer.Build(viewport, SkyMapRenderOptions.All);

            bitmap = new Bitmap((int)viewport.Width, (int)viewport.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            legacyGrid = new FrameLineMatrix2();
            legacyConstellations = [];
            legacyDeepSkyObjects = [];
        }

        [GlobalCleanup]
        public void Cleanup() {
            graphics.Dispose();
            bitmap.Dispose();
            legacyGrid.Dispose();
            rasterRenderer.Dispose();
        }

        [Benchmark(Baseline = true)]
        public BitmapSource LegacyFullFrame() {
            graphics.Clear(Color.Transparent);
            DrawLegacyConstellations();
            DrawLegacyDeepSkyObjects();
            DrawLegacyBoundaries();
            legacyGrid.CalculatePoints(viewport);
            legacyGrid.Draw(graphics);
            BitmapSource image = ImageUtility.ConvertBitmap(bitmap, PixelFormats.Bgra32);
            image.Freeze();
            return image;
        }

        [Benchmark]
        public ImageSource NewFullFrame() {
            SkyMapScene scene = renderer.Build(viewport, SkyMapRenderOptions.All);
            return rasterRenderer.Render(scene, [], null);
        }

        [Benchmark]
        public SkyMapScene NewSceneOnly() {
            return renderer.Build(viewport, SkyMapRenderOptions.All);
        }

        [Benchmark]
        public ImageSource NewRasterOnly() {
            return rasterRenderer.Render(scene, [], null);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewConstellationsLayer() {
            return renderer.Build(viewport, SkyMapRenderOptions.Stars | SkyMapRenderOptions.Constellations);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewDeepSkyObjectLayer() {
            return renderer.Build(viewport, SkyMapRenderOptions.DeepSkyObjects);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewBoundaryLayer() {
            return renderer.Build(viewport, SkyMapRenderOptions.ConstellationBoundaries);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewGridLayer() {
            return renderer.Build(viewport, SkyMapRenderOptions.EquatorialGrid);
        }

        private void DrawLegacyConstellations() {
            foreach (Constellation constellation in constellations) {
                FramingConstellation? visible = legacyConstellations.FirstOrDefault(x => x.Id == constellation.Id);
                bool isVisible = constellation.Stars.Any(x => viewport.ContainsCoordinates(x.Coords));
                if (isVisible) {
                    if (visible is null) {
                        visible = new FramingConstellation(constellation, viewport);
                        legacyConstellations.Add(visible);
                    }
                    visible.RecalculateConstellationPoints(viewport, true);
                } else if (visible is not null) {
                    legacyConstellations.Remove(visible);
                }
            }
            foreach (FramingConstellation constellation in legacyConstellations) {
                constellation.DrawAnnotations(graphics);
                constellation.DrawStars(graphics);
            }
        }

        private void DrawLegacyDeepSkyObjects() {
            double minimumSize = 3 * Math.Min(viewport.ArcSecWidth, viewport.ArcSecHeight);
            double maximumSize = AstroUtil.DegreeToArcsec(2 * Math.Max(viewport.HFoV, viewport.VFoV));
            Dictionary<string, DeepSkyObject> visible = deepSkyObjects
                .Where(x => x.Size > minimumSize && x.Size < maximumSize)
                .Where(x => viewport.ContainsCoordinates(x.Coordinates))
                .ToDictionary(x => x.Id, x => x);

            for (int i = legacyDeepSkyObjects.Count - 1; i >= 0; i--) {
                FramingDSO dso = legacyDeepSkyObjects[i];
                if (visible.Remove(dso.Id)) {
                    dso.RecalculateTopLeft(viewport);
                } else {
                    legacyDeepSkyObjects.RemoveAt(i);
                }
            }
            foreach (DeepSkyObject dso in visible.Values) {
                legacyDeepSkyObjects.Add(new FramingDSO(dso, viewport));
            }
            foreach (FramingDSO dso in legacyDeepSkyObjects) {
                dso.Draw(graphics);
            }
        }

        private void DrawLegacyBoundaries() {
            using DrawingPen pen = new DrawingPen(Color.FromArgb(128, Color.Khaki), 0.5f);
            foreach (ConstellationBoundary boundary in boundaries) {
                if (!boundary.Boundaries.Any(viewport.ContainsCoordinates)) {
                    continue;
                }
                PointF[] points = boundary.Boundaries.Select(x => {
                    Point point = x.XYProjection(viewport);
                    return new PointF((float)point.X, (float)point.Y);
                }).ToArray();
                if (points.Length > 1) {
                    graphics.DrawPolygon(pen, points);
                }
            }
        }

        private static IReadOnlyList<Constellation> CreateConstellations() {
            List<Constellation> result = [];
            int starId = 0;
            for (int constellationIndex = 0; constellationIndex < 88; constellationIndex++) {
                double centerRa = constellationIndex * 360d / 88;
                double centerDec = -70 + constellationIndex % 15 * 10;
                List<Star> stars = [];
                for (int i = 0; i < 8; i++) {
                    stars.Add(new Star(starId++, $"S{starId}", CelestialCoordinates(centerRa + i - 4, centerDec + i % 3 - 1), 1 + i * 0.5f));
                }
                Constellation constellation = new Constellation($"B{constellationIndex}") { Stars = stars };
                for (int i = 1; i < stars.Count; i++) {
                    constellation.StarConnections.Add(Tuple.Create(stars[i - 1], stars[i]));
                }
                result.Add(constellation);
            }
            return result;
        }

        private static IReadOnlyList<DeepSkyObject> CreateDeepSkyObjects() {
            Random random = new Random(42);
            List<DeepSkyObject> result = new List<DeepSkyObject>(10_000);
            for (int i = 0; i < 10_000; i++) {
                double declination = Math.Asin(random.NextDouble() * 2 - 1) * 180 / Math.PI;
                result.Add(new DeepSkyObject($"NGC{i}", CelestialCoordinates(random.NextDouble() * 360, declination), null) {
                    DSOType = i % 5 == 0 ? "BRTNB" : "GALXY",
                    Size = 60 + random.NextDouble() * 3600,
                    SizeMin = 30 + random.NextDouble() * 1800,
                    PositionAngle = Angle.ByDegree(random.NextDouble() * 180)
                });
            }
            return result;
        }

        private static IReadOnlyList<ConstellationBoundary> CreateBoundaries() {
            List<ConstellationBoundary> result = [];
            for (int boundaryIndex = 0; boundaryIndex < 88; boundaryIndex++) {
                double centerRa = boundaryIndex * 360d / 88;
                double centerDec = -70 + boundaryIndex % 15 * 10;
                ConstellationBoundary boundary = new ConstellationBoundary { Name = $"B{boundaryIndex}" };
                for (int i = 0; i < 435; i++) {
                    double angle = i * Math.PI * 2 / 435;
                    boundary.Boundaries.Add(CelestialCoordinates(centerRa + Math.Cos(angle) * 8, centerDec + Math.Sin(angle) * 5));
                }
                result.Add(boundary);
            }
            return result;
        }

        private static Coordinates CelestialCoordinates(double rightAscension, double declination) {
            return new Coordinates(AstroUtil.EuclidianModulus(rightAscension, 360), declination, Epoch.J2000, Coordinates.RAType.Degrees);
        }
    }
}
