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
using NINA.Core.Model;
using NINA.WPF.Base.SkySurvey;
using System.IO;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfImage = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace NINA.Benchmark {

    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 5, iterationCount: 10)]
    public class SkyMapRenderingBenchmark {
        private const string SharpHorizon = """
            0, 15
            32, 36
            33, 12
            37, 39
            88, 28
            360, 15
            """;
        private SkyMapViewportProjection altAzProjection = null!;
        private SkyMapViewportProjection alternateAltAzProjection = null!;
        private SkyMapViewportProjection equatorialObserverProjection = null!;
        private SkyMapViewportProjection equatorialProjection = null!;
        private FramingRectangle[] alternateCameraRectangles = null!;
        private IReadOnlyList<ConstellationBoundary> boundaries = null!;
        private FramingRectangle[] cameraRectangles = null!;
        private SkyMapCameraRectanglePlacement[] cameraRectanglePlacements = null!;
        private BitmapSource[] cachedImages = null!;
        private IReadOnlyList<Constellation> constellations = null!;
        private IReadOnlyList<DeepSkyObject> deepSkyObjects = null!;
        private Point[] imageCenters = null!;
        private Coordinates[] imageCoordinates = null!;
        private LegacySkyMapRenderer legacyRenderer = null!;
        private SkyMapSceneBuilder renderer = null!;
        private SkyMapRasterRenderer rasterRenderer = null!;
        private SkyMapScene scene = null!;
        private SkyMapViewportProjection sharpHorizonProjection = null!;
        private SkyMapObserverSnapshot observer = null!;
        private ViewportFoV viewport = null!;
        private ViewportFoV wideViewport = null!;
        private SkyMapViewportProjection wideAltAzProjection = null!;
        private bool useAlternateCameraProjection;
        private WpfImage materializedImage = null!;
        private MaterializedFrameSource materializedFrameSource = null!;
        private RenderTargetBitmap materializedTarget = null!;
        private Dispatcher materializedDispatcher = null!;
        private SkyMapRasterRenderer materializedRenderer = null!;
        private Thread materializedThread = null!;

        [GlobalSetup]
        public void Setup() {
            constellations = CreateConstellations();
            deepSkyObjects = CreateDeepSkyObjects();
            boundaries = CreateBoundaries();
            viewport = new ViewportFoV(CelestialCoordinates(85, 0), 30, 1200, 800, 0);
            observer = new SkyMapObserverSnapshot(50, new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc), 16.5);
            altAzProjection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            equatorialProjection = new SkyMapViewportProjection(viewport);
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
            cachedImages = Enumerable.Range(0, 32).Select(CreatePatternedImage).ToArray();
            Coordinates wideCenter = observer.ToCelestial(new SkyMapHorizontalCoordinates(30, 180));
            wideViewport = new ViewportFoV(wideCenter, 140, 1200, 800, 37);
            wideAltAzProjection = new SkyMapViewportProjection(wideViewport, SkyMapProjectionMode.AltAz, observer);
            using StringReader sharpHorizonReader = new StringReader(SharpHorizon);
            CustomHorizon sharpHorizon = CustomHorizon.FromReader_Standard(sharpHorizonReader);
            SkyMapObserverSnapshot sharpHorizonObserver = new SkyMapObserverSnapshot(
                50,
                new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc),
                16.5,
                sharpHorizon.GetAltitude);
            Coordinates sharpHorizonCenter = sharpHorizonObserver.ToCelestial(new SkyMapHorizontalCoordinates(25, 32.5));
            sharpHorizonProjection = new SkyMapViewportProjection(
                new ViewportFoV(sharpHorizonCenter, 40, 1200, 800, 17),
                SkyMapProjectionMode.AltAz,
                sharpHorizonObserver);
            renderer = new SkyMapSceneBuilder(constellations, deepSkyObjects, boundaries);
            rasterRenderer = new SkyMapRasterRenderer((int)viewport.Width, (int)viewport.Height);
            scene = renderer.Build(equatorialProjection, SkyMapRenderOptions.All);

            legacyRenderer = new LegacySkyMapRenderer(viewport, constellations, deepSkyObjects, boundaries);

            using ManualResetEventSlim materializedReady = new ManualResetEventSlim();
            materializedThread = new Thread(() => {
                RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                materializedDispatcher = Dispatcher.CurrentDispatcher;
                materializedFrameSource = new MaterializedFrameSource();
                materializedImage = new WpfImage {
                    Width = viewport.Width,
                    Height = viewport.Height,
                    Stretch = Stretch.Fill,
                    DataContext = materializedFrameSource
                };
                RenderOptions.SetBitmapScalingMode(materializedImage, BitmapScalingMode.NearestNeighbor);
                BindingOperations.SetBinding(
                    materializedImage,
                    WpfImage.SourceProperty,
                    new Binding(nameof(MaterializedFrameSource.Frame)));
                materializedImage.Measure(new System.Windows.Size(viewport.Width, viewport.Height));
                materializedImage.Arrange(new System.Windows.Rect(0, 0, viewport.Width, viewport.Height));
                materializedTarget = new RenderTargetBitmap(
                    (int)viewport.Width,
                    (int)viewport.Height,
                    96,
                    96,
                    PixelFormats.Pbgra32);
                materializedRenderer = new SkyMapRasterRenderer((int)viewport.Width, (int)viewport.Height);
                materializedFrameSource.Frame = PrepareAltAzDragFrame(
                    materializedRenderer,
                    SkyMapRenderQuality.InteractionPreview);
                materializedReady.Set();
                Dispatcher.Run();
            });
            materializedThread.SetApartmentState(ApartmentState.STA);
            materializedThread.Start();
            materializedReady.Wait();
        }

        [GlobalCleanup]
        public void Cleanup() {
            legacyRenderer.Dispose();
            materializedDispatcher.InvokeShutdown();
            materializedThread.Join();
        }

        [Benchmark(Baseline = true)]
        public BitmapSource LegacyFullFrame() {
            return legacyRenderer.Render();
        }

        [Benchmark]
        public ImageSource NewFullFrame() {
            SkyMapScene scene = renderer.Build(equatorialProjection, SkyMapRenderOptions.All);
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
            return renderer.Build(equatorialProjection, SkyMapRenderOptions.All);
        }

        [Benchmark]
        public ImageSource NewRasterOnly() {
            return rasterRenderer.Render(scene, [], null);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewConstellationsLayer() {
            return renderer.Build(equatorialProjection, SkyMapRenderOptions.Stars | SkyMapRenderOptions.Constellations);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewDeepSkyObjectLayer() {
            return renderer.Build(equatorialProjection, SkyMapRenderOptions.DeepSkyObjects);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewBoundaryLayer() {
            return renderer.Build(equatorialProjection, SkyMapRenderOptions.ConstellationBoundaries);
        }

        [Benchmark]
        [BenchmarkCategory("Layers")]
        public SkyMapScene NewGridLayer() {
            return renderer.Build(equatorialProjection, SkyMapRenderOptions.EquatorialGrid);
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
        public SkyMapScene NewSharpHorizonLayer() {
            return renderer.Build(sharpHorizonProjection, SkyMapRenderOptions.Horizon);
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
        public ImageSource NewAltAzDragFramePreparation() {
            return PrepareAltAzDragFrame(rasterRenderer);
        }

        [Benchmark]
        [BenchmarkCategory("Interaction")]
        public void NewAltAzSoftwareDragPreviewMaterialized() {
            materializedDispatcher.Invoke(() => {
                materializedFrameSource.Frame = PrepareAltAzDragFrame(
                    materializedRenderer,
                    SkyMapRenderQuality.InteractionPreview);
                materializedTarget.Render(materializedImage);
            });
        }

        [Benchmark]
        [BenchmarkCategory("InteractionDiagnostics")]
        public void NewAltAzSoftwareDragPreviewPreparation() {
            materializedDispatcher.Invoke(() => {
                materializedFrameSource.Frame = PrepareAltAzDragFrame(
                    materializedRenderer,
                    SkyMapRenderQuality.InteractionPreview);
            });
        }

        [Benchmark]
        [BenchmarkCategory("InteractionDiagnostics")]
        public void NewAltAzSoftwarePresentationOnly() {
            materializedDispatcher.Invoke(() => materializedTarget.Render(materializedImage));
        }

        [Benchmark]
        [BenchmarkCategory("Interaction")]
        public void NewAltAzSoftwareFinalFrameMaterialized() {
            materializedDispatcher.Invoke(() => {
                materializedFrameSource.Frame = PrepareAltAzDragFrame(materializedRenderer);
                materializedTarget.Render(materializedImage);
            });
        }

        private ImageSource PrepareAltAzDragFrame(
            SkyMapRasterRenderer frameRenderer,
            SkyMapRenderQuality quality = SkyMapRenderQuality.Final) {
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
                images[i] = new SkyMapImagePlacement(cachedImages[i], center, 80, 80, rotation, flipHorizontally);

                SkyMapCameraRectanglePlacement placement = cameraRectanglePlacements[i];
                placement.SetRectangle(rectangles[i]);
                placement.Update(projection, 343);
                total += placement.X + placement.Y + placement.Rotation;
            }
            GC.KeepAlive(total);
            return frameRenderer.Render(currentScene, images, null, quality);
        }

        private static BitmapSource CreatePatternedImage(int index) {
            const int size = 500;
            const int bytesPerPixel = 4;
            byte[] pixels = new byte[size * size * bytesPerPixel];
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    int offset = (y * size + x) * bytesPerPixel;
                    pixels[offset] = (byte)((x * 3 + index * 17) & 0xff);
                    pixels[offset + 1] = (byte)((y * 5 + index * 29) & 0xff);
                    pixels[offset + 2] = (byte)(((x ^ y) + index * 11) & 0xff);
                    pixels[offset + 3] = 255;
                }
            }
            BitmapSource image = BitmapSource.Create(
                size,
                size,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                size * bytesPerPixel);
            image.Freeze();
            return image;
        }

        private sealed class MaterializedFrameSource : INotifyPropertyChanged {
            private ImageSource frame = null!;

            public event PropertyChangedEventHandler? PropertyChanged;

            public ImageSource Frame {
                get => frame;
                set {
                    frame = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Frame)));
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
