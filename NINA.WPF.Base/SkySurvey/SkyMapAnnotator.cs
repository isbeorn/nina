#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model.FramingAssistant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;
using PointF = System.Drawing.PointF;

namespace NINA.WPF.Base.SkySurvey {

    public partial class SkyMapAnnotator : BaseINPC, ITelescopeConsumer, ISkyMapAnnotator {
        private readonly DatabaseInteraction dbInstance;
        private readonly IProfileService profileService;
        private readonly ITelescopeMediator telescopeMediator;
        private CacheSkySurvey cache;
        private IReadOnlyList<ConstellationBoundary> constellationBoundaries = [];
        private IReadOnlyList<Constellation> dbConstellations = [];
        private IReadOnlyDictionary<string, DeepSkyObject> dbDeepSkyObjects = new Dictionary<string, DeepSkyObject>();
        private CancellationTokenSource imageLoadCancellation;
        private SkyMapImageCache imageCache;
        private int renderVersion;
        private SkyMapRasterRenderer rasterRenderer;
        private SkyMapSceneBuilder sceneBuilder;
        private bool telescopeConnected;
        private Coordinates telescopeCoordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);

        public SkyMapAnnotator() {
            dbInstance = new DatabaseInteraction();
            DSOInViewport = [];
            ConstellationsInViewport = [];
            ConstellationBoundariesInViewPort = [];
            FrameLineMatrix = new FrameLineMatrix2();
            AnnotateDSO = true;
            AnnotateGrid = true;
        }

        public SkyMapAnnotator(ITelescopeMediator mediator, IProfileService profileService) : this() {
            telescopeMediator = mediator;
            this.profileService = profileService;
            LoadSettings();
        }

        public ViewportFoV ViewportFoV { get; private set; }
        public ICommand DragCommand { get; private set; }
        public FrameLineMatrix2 FrameLineMatrix { get; }
        public List<FramingDSO> DSOInViewport { get; }
        public List<FramingConstellation> ConstellationsInViewport { get; }
        public List<FramingConstellationBoundary> ConstellationBoundariesInViewPort { get; }

        [ObservableProperty]
        private bool initialized;

        [ObservableProperty]
        private IList<ActiveCatalogue> activeCatalogues;

        [ObservableProperty]
        private bool annotateConstellationBoundaries;

        [ObservableProperty]
        private bool dynamicFoV;

        [ObservableProperty]
        private bool annotateConstellations;

        [ObservableProperty]
        private bool annotateGrid;

        [ObservableProperty]
        private bool annotateDSO;

        [ObservableProperty]
        private bool useCachedImages;

        [ObservableProperty]
        private ImageSource skyMapOverlay;

        [ObservableProperty]
        private bool showAllCatalogues = true;

        private List<string> DisabledCatalogues =>
            profileService?.ActiveProfile?.FramingAssistantSettings?.DisabledCatalogues ?? [];

        public async Task Initialize(
            Coordinates centerCoordinates,
            double vFoVDegrees,
            double imageWidth,
            double imageHeight,
            double imageRotation,
            CacheSkySurvey cache,
            CancellationToken token) {
            telescopeMediator?.RemoveConsumer(this);
            CancelImageLoad();
            this.cache = cache;
            ViewportFoV = new ViewportFoV(centerCoordinates, vFoVDegrees, imageWidth, imageHeight, imageRotation);

            if (dbConstellations.Count == 0) {
                dbConstellations = await dbInstance.GetConstellationsWithStars(token);
            }
            if (dbDeepSkyObjects.Count == 0) {
                dbDeepSkyObjects = (await dbInstance.GetDeepSkyObjects(
                    string.Empty,
                    null,
                    new DatabaseInteraction.DeepSkyObjectSearchParams(),
                    token)).ToDictionary(x => x.Id, x => x);
            }
            if (constellationBoundaries.Count == 0) {
                constellationBoundaries = await dbInstance.GetConstellationBoundaries(token);
            }
            if (ActiveCatalogues is null) {
                await LoadCatalogues(token);
            }

            sceneBuilder = new SkyMapSceneBuilder(
                dbConstellations,
                dbDeepSkyObjects.Values.ToArray(),
                constellationBoundaries);
            rasterRenderer?.Dispose();
            rasterRenderer = new SkyMapRasterRenderer((int)ViewportFoV.Width, (int)ViewportFoV.Height);
            imageCache = new SkyMapImageCache(cache);
            DSOInViewport.Clear();
            ConstellationsInViewport.Clear();
            ConstellationBoundariesInViewPort.Clear();
            ClearFrameLineMatrix();

            telescopeMediator?.RegisterConsumer(this);
            Initialized = true;
            UpdateSkyMap();
        }

        public ViewportFoV ChangeFoV(double vFoVDegrees) {
            ViewportFoV = new ViewportFoV(
                ViewportFoV.CenterCoordinates,
                vFoVDegrees,
                ViewportFoV.Width,
                ViewportFoV.Height,
                ViewportFoV.Rotation);
            UpdateSkyMap();
            return ViewportFoV;
        }

        public Coordinates ShiftViewport(Vector delta) {
            ViewportFoV.Shift(delta);
            return ViewportFoV.CenterCoordinates;
        }

        public void UpdateSkyMap() {
            if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess()) {
                _ = dispatcher.InvokeAsync(UpdateSkyMap);
                return;
            }
            if (!Initialized || sceneBuilder is null) {
                return;
            }

            int version = Interlocked.Increment(ref renderVersion);
            RenderCurrentFrame();
            QueueMissingImages(version);
        }

        private void RenderCurrentFrame() {
            SkyMapRenderOptions options = SkyMapRenderOptions.None;
            if (AnnotateConstellations || AnnotateDSO) {
                options |= SkyMapRenderOptions.Stars;
            }
            if (AnnotateConstellations) {
                options |= SkyMapRenderOptions.Constellations;
            }
            if (AnnotateDSO) {
                options |= SkyMapRenderOptions.DeepSkyObjects;
            }
            if (AnnotateConstellationBoundaries) {
                options |= SkyMapRenderOptions.ConstellationBoundaries;
            }
            if (AnnotateGrid) {
                options |= SkyMapRenderOptions.EquatorialGrid;
            }

            HashSet<string> disabledCatalogues = DisabledCatalogues.ToHashSet(StringComparer.Ordinal);
            SkyMapScene scene = sceneBuilder.Build(ViewportFoV, options, disabledCatalogues);
            IReadOnlyList<SkyMapImagePlacement> images = UseCachedImages && imageCache is not null
                ? imageCache.GetPlacements(ViewportFoV)
                : [];
            Point? telescopePosition = telescopeConnected && ViewportFoV.ContainsCoordinates(telescopeCoordinates)
                ? telescopeCoordinates.XYProjection(ViewportFoV)
                : null;
            SkyMapOverlay = rasterRenderer.Render(scene, images, telescopePosition);
        }

        private void QueueMissingImages(int version) {
            CancelImageLoad();
            if (!UseCachedImages || imageCache is null) {
                return;
            }

            imageLoadCancellation = new CancellationTokenSource();
            CancellationToken token = imageLoadCancellation.Token;
            SkyMapImageCache cache = imageCache;
            ViewportFoV viewport = new ViewportFoV(
                ViewportFoV.CenterCoordinates,
                ViewportFoV.VFoV,
                ViewportFoV.Width,
                ViewportFoV.Height,
                ViewportFoV.Rotation);
            _ = LoadMissingImages(cache, version, viewport, token);
        }

        private async Task LoadMissingImages(SkyMapImageCache cache, int version, ViewportFoV viewport, CancellationToken token) {
            try {
                bool changed = await cache.LoadAsync(viewport, token);
                if (!changed || token.IsCancellationRequested || version != renderVersion) {
                    return;
                }

                if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess()) {
                    await dispatcher.InvokeAsync(() => {
                        if (version == renderVersion) {
                            RenderCurrentFrame();
                        }
                    });
                } else {
                    RenderCurrentFrame();
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error("Failed to load an offline sky map image.", ex);
            }
        }

        public Dictionary<string, DeepSkyObject> GetDeepSkyObjectsForViewport() {
            double minimumSize = Math.Min(ViewportFoV.HFoV, ViewportFoV.VFoV) < 10
                ? 0
                : 3 * Math.Min(ViewportFoV.ArcSecWidth, ViewportFoV.ArcSecHeight);
            double maximumSize = AstroUtil.DegreeToArcsec(2 * Math.Max(ViewportFoV.HFoV, ViewportFoV.VFoV));
            HashSet<string> disabledCatalogues = DisabledCatalogues.ToHashSet(StringComparer.Ordinal);
            return dbDeepSkyObjects
                .Where(x => ViewportFoV.ContainsCoordinates(x.Value.Coordinates))
                .Where(x => ViewportFoV.VFoV <= 10 || (x.Value.Size > minimumSize && x.Value.Size < maximumSize))
                .Where(x => !disabledCatalogues.Any(catalogue => x.Value.Name.StartsWith(catalogue, StringComparison.Ordinal)))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public void ClearImagesForViewport() {
            CancelImageLoad();
            imageCache = new SkyMapImageCache(cache);
        }

        public void ClearFrameLineMatrix() {
            FrameLineMatrix.RAPoints.Clear();
            FrameLineMatrix.DecPoints.Clear();
        }

        public void CalculateFrameLineMatrix() {
            FrameLineMatrix.CalculatePoints(ViewportFoV);
        }

        public void CalculateConstellationBoundaries() {
            ConstellationBoundariesInViewPort.Clear();
            foreach (ConstellationBoundary boundary in constellationBoundaries) {
                if (!boundary.Boundaries.Any(ViewportFoV.ContainsCoordinates)) {
                    continue;
                }

                FramingConstellationBoundary line = new FramingConstellationBoundary();
                foreach (Coordinates coordinates in boundary.Boundaries) {
                    Point point = coordinates.XYProjection(ViewportFoV);
                    line.Points.Add(new PointF((float)point.X, (float)point.Y));
                }
                ConstellationBoundariesInViewPort.Add(line);
            }
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            if (deviceInfo.Connected && deviceInfo.Coordinates is not null) {
                Coordinates coordinates = deviceInfo.Coordinates.Transform(Epoch.J2000);
                bool moved = Math.Abs(telescopeCoordinates.RADegrees - coordinates.RADegrees) > 0.01
                    || Math.Abs(telescopeCoordinates.Dec - coordinates.Dec) > 0.01;
                telescopeConnected = true;
                telescopeCoordinates = coordinates;
                if (moved && ViewportFoV?.ContainsCoordinates(coordinates) == true) {
                    UpdateSkyMap();
                }
            } else if (telescopeConnected) {
                telescopeConnected = false;
                UpdateSkyMap();
            }
        }

        public void Dispose() {
            telescopeMediator?.RemoveConsumer(this);
            CancelImageLoad();
            rasterRenderer?.Dispose();
            FrameLineMatrix.Dispose();
        }

        partial void OnAnnotateConstellationBoundariesChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.AnnotateConstellationBoundaries = newValue;
            }
        }

        partial void OnAnnotateConstellationsChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.AnnotateConstellations = newValue;
            }
        }

        partial void OnAnnotateDSOChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.AnnotateDSO = newValue;
            }
        }

        partial void OnAnnotateGridChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.AnnotateGrid = newValue;
            }
        }

        partial void OnShowAllCataloguesChanged(bool oldValue, bool newValue) {
            if (ActiveCatalogues is null) {
                return;
            }
            foreach (ActiveCatalogue catalogue in ActiveCatalogues) {
                catalogue.Active = newValue;
            }
            SaveDisabledCatalogues();
            UpdateSkyMap();
        }

        private async Task LoadCatalogues(CancellationToken token) {
            List<string> catalogues = await dbInstance.GetCatalogues(50, token);
            ActiveCatalogues = catalogues?.Select(name => {
                ActiveCatalogue catalogue = new ActiveCatalogue(name, !DisabledCatalogues.Contains(name));
                catalogue.PropertyChanged += (_, args) => {
                    if (args.PropertyName == nameof(ActiveCatalogue.Active)) {
                        SaveDisabledCatalogues();
                        UpdateShowAllCataloguesState();
                        UpdateSkyMap();
                    }
                };
                return catalogue;
            }).ToList() ?? [];
            UpdateShowAllCataloguesState();
        }

        private void SaveDisabledCatalogues() {
            if (ActiveCatalogues is null || profileService?.ActiveProfile?.FramingAssistantSettings is not { } settings) {
                return;
            }
            settings.DisabledCatalogues = ActiveCatalogues.Where(x => !x.Active).Select(x => x.Name).ToList();
        }

        private void UpdateShowAllCataloguesState() {
            if (ActiveCatalogues is null || ActiveCatalogues.Count == 0) {
                return;
            }
            if (ActiveCatalogues.All(x => x.Active)) {
                ShowAllCatalogues = true;
            } else if (ActiveCatalogues.All(x => !x.Active)) {
                ShowAllCatalogues = false;
            }
        }

        private void LoadSettings() {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is not { } settings) {
                return;
            }
            AnnotateConstellationBoundaries = settings.AnnotateConstellationBoundaries;
            AnnotateConstellations = settings.AnnotateConstellations;
            AnnotateDSO = settings.AnnotateDSO;
            AnnotateGrid = settings.AnnotateGrid;
        }

        private void CancelImageLoad() {
            try {
                imageLoadCancellation?.Cancel();
            } catch (ObjectDisposedException) {
            }
            imageLoadCancellation?.Dispose();
            imageLoadCancellation = null;
        }
    }
}
