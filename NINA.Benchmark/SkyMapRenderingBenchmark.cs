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
using NINA.Core.Enum;
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
        private SkyMapViewportProjection altAzProjection = null!;
        private SkyMapViewportProjection alternateAltAzProjection = null!;
        private SkyMapViewportProjection equatorialObserverProjection = null!;
        private FramingRectangle[] alternateCameraRectangles = null!;
        private IReadOnlyList<ConstellationBoundary> boundaries = null!;
        private FramingRectangle[] cameraRectangles = null!;
        private SkyMapCameraRectanglePlacement[] cameraRectanglePlacements = null!;
        private BitmapSource cachedImage = null!;
        private IReadOnlyList<Constellation> constellations = null!;
        private IReadOnlyList<DeepSkyObject> deepSkyObjects = null!;
        private Graphics graphics = null!;
        private Point[] imageCenters = null!;
        private Coordinates[] imageCoordinates = null!;
        private FrameLineMatrix2 legacyGrid = null!;
        private List<FramingConstellation> legacyConstellations = null!;
        private List<FramingDSO> legacyDeepSkyObjects = null!;
        private SkyMapSceneBuilder renderer = null!;
        private SkyMapRasterRenderer rasterRenderer = null!;
        private SkyMapScene scene = null!;
        private SkyMapObserverSnapshot observer = null!;
        private ViewportFoV viewport = null!;
        private ViewportFoV wideViewport = null!;
        private SkyMapViewportProjection wideAltAzProjection = null!;
        private bool useAlternateCameraProjection;

        [GlobalSetup]
        public void Setup() {
            constellations = CreateConstellations();
            deepSkyObjects = CreateDeepSkyObjects();
            boundaries = CreateBoundaries();
            viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);
            observer = new SkyMapObserverSnapshot(50, new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc), 16.5);
            altAzProjection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            equatorialObserverProjection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.Equatorial, observer);
            alternateAltAzProjection = new SkyMapViewportProjection(
                new ViewportFoV(CelestialCoordinates(86, 1), 30, 1200, 800, 3),
                SkyMapProjectionMode.AltAz,
                observer);
            imageCoordinates = Enumerable.Range(0, 32)
                .Select(i => observer.ToCelestial(new SkyMapHorizontalCoordinates(15 + i % 7 * 10, i * 360d / 32)))
                .ToArray();
            imageCenters = imageCoordinates.Select(altAzProjection.Project).ToArray();
            cameraRectangles = imageCoordinates
                .Select((coordinates, index) => new FramingRectangle(0, 0, 0, 320, 180) {
                    Coordinates = coordinates,
                    Id = index + 1
                })
                .ToArray();
            alternateCameraRectangles = imageCoordinates
                .Select((coordinates, index) => new FramingRectangle(0, 0, 0, 320, 180) {
                    Coordinates = coordinates,
                    Id = index + 1
                })
                .ToArray();
            cameraRectanglePlacements = cameraRectangles
                .Select(rectangle => new SkyMapCameraRectanglePlacement(rectangle))
                .ToArray();
            cachedImage = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] { 255, 255, 255, 255 },
                4);
            cachedImage.Freeze();
            Coordinates wideCenter = observer.ToCelestial(new SkyMapHorizontalCoordinates(30, 180));
            wideViewport = new ViewportFoV(wideCenter, 140, 1200, 800, 37);
            wideAltAzProjection = new SkyMapViewportProjection(wideViewport, SkyMapProjectionMode.AltAz, observer);
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
        public ImageSource NewAltAzHorizonFullFrame() {
            SkyMapRenderOptions options = (SkyMapRenderOptions.All & ~SkyMapRenderOptions.EquatorialGrid)
                | SkyMapRenderOptions.HorizontalGrid
                | SkyMapRenderOptions.Horizon;
            SkyMapScene scene = renderer.Build(altAzProjection, options);
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

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewAltAzGridLayer() {
            return renderer.Build(altAzProjection, SkyMapRenderOptions.HorizontalGrid);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewHorizonLayer() {
            return renderer.Build(equatorialObserverProjection, SkyMapRenderOptions.Horizon);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewWideAltAzHorizonLayer() {
            return renderer.Build(wideAltAzProjection, SkyMapRenderOptions.Horizon);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public double NewAltAzCachedImageOrientations() {
            double total = 0;
            for (int i = 0; i < imageCoordinates.Length; i++) {
                (double rotation, bool flipHorizontally) = altAzProjection.ImageTransformFromEquatorial(
                    imageCoordinates[i],
                    17,
                    imageCenters[i]);
                total += rotation + (flipHorizontally ? 1 : 0);
            }
            return total;
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public double NewAltAzCameraRectanglePlacements() {
            SkyMapViewportProjection projection = useAlternateCameraProjection
                ? alternateAltAzProjection
                : altAzProjection;
            FramingRectangle[] rectangles = useAlternateCameraProjection
                ? alternateCameraRectangles
                : cameraRectangles;
            useAlternateCameraProjection = !useAlternateCameraProjection;
            double total = 0;
            for (int i = 0; i < cameraRectanglePlacements.Length; i++) {
                SkyMapCameraRectanglePlacement placement = cameraRectanglePlacements[i];
                placement.SetRectangle(rectangles[i]);
                placement.Update(projection, 343);
                total += placement.X + placement.Y + placement.Rotation;
            }
            return total;
        }

        [Benchmark]
        [BenchmarkCategory("Interaction")]
        public ImageSource NewAltAzDragFrame() {
            SkyMapViewportProjection projection = useAlternateCameraProjection
                ? alternateAltAzProjection
                : altAzProjection;
            FramingRectangle[] rectangles = useAlternateCameraProjection
                ? alternateCameraRectangles
                : cameraRectangles;
            useAlternateCameraProjection = !useAlternateCameraProjection;
            SkyMapRenderOptions options = (SkyMapRenderOptions.All & ~SkyMapRenderOptions.EquatorialGrid)
                | SkyMapRenderOptions.HorizontalGrid
                | SkyMapRenderOptions.Horizon;
            SkyMapScene currentScene = renderer.Build(projection, options);
            SkyMapImagePlacement[] images = new SkyMapImagePlacement[imageCoordinates.Length];
            double total = 0;
            for (int i = 0; i < imageCoordinates.Length; i++) {
                Point center = projection.Project(imageCoordinates[i]);
                (double rotation, bool flipHorizontally) = projection.ImageTransformFromEquatorial(
                    imageCoordinates[i],
                    17,
                    center);
                images[i] = new SkyMapImagePlacement(cachedImage, center, 80, 80, rotation, flipHorizontally);

                SkyMapCameraRectanglePlacement placement = cameraRectanglePlacements[i];
                placement.SetRectangle(rectangles[i]);
                placement.Update(projection, 343);
                total += placement.X + placement.Y + placement.Rotation;
            }
            GC.KeepAlive(total);
            return rasterRenderer.Render(currentScene, images, null);
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