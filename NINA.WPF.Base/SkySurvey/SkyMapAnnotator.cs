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
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Point = System.Windows.Point;

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
        private bool interactionRenderPending;
        private readonly DispatcherTimer interactionRenderTimer;
        private bool isInteracting;
        private int renderVersion;
        private readonly DispatcherTimer observerRefreshTimer;
        private SkyMapObserverSnapshot observerSnapshot;
        private SkyMapRasterRenderer rasterRenderer;
        private SkyMapSceneBuilder sceneBuilder;
        private bool telescopeConnected;
        private bool suppressRender;
        private Coordinates telescopeCoordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);

        public SkyMapAnnotator() {
            dbInstance = new DatabaseInteraction();
            AnnotateDSO = true;
            AnnotateGrid = true;
            interactionRenderTimer = new DispatcherTimer(DispatcherPriority.Render) {
                Interval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60)
            };
            interactionRenderTimer.Tick += InteractionRenderTimer_Tick;
        }

        public SkyMapAnnotator(ITelescopeMediator mediator, IProfileService profileService) : this() {
            telescopeMediator = mediator;
            this.profileService = profileService;
            LoadSettings();
            profileService.ProfileChanged += ProfileService_ProfileChanged;
            profileService.LocationChanged += ProfileService_LocationOrHorizonChanged;
            profileService.HorizonChanged += ProfileService_LocationOrHorizonChanged;
            observerRefreshTimer = new DispatcherTimer(DispatcherPriority.Background) {
                Interval = TimeSpan.FromSeconds(10)
            };
            observerRefreshTimer.Tick += ObserverRefreshTimer_Tick;
            observerRefreshTimer.Start();
        }

        public ViewportFoV ViewportFoV { get; private set; }
        public SkyMapViewportProjection Projection { get; private set; }
        public event EventHandler ProjectionChanged;
        [ObservableProperty]
        private bool initialized;

        [ObservableProperty]
        private IList<ActiveCatalogue> activeCatalogues;

        [ObservableProperty]
        private bool annotateConstellationBoundaries;

        [ObservableProperty]
        private bool dynamicFoV;

        public SkyMapProjectionMode EffectiveProjectionMode => DynamicFoV
            ? ProjectionMode
            : SkyMapProjectionMode.Equatorial;

        [ObservableProperty]
        private bool annotateConstellations;

        [ObservableProperty]
        private bool annotateGrid;

        [ObservableProperty]
        private DateTime? observationTime;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EffectiveProjectionMode))]
        private SkyMapProjectionMode projectionMode;

        [ObservableProperty]
        private bool showHorizon;

        [ObservableProperty]
        private bool annotateDSO;

        [ObservableProperty]
        private bool useCachedImages;

        [ObservableProperty]
        private ImageSource skyMapOverlay;

        [ObservableProperty]
        private bool showAllCatalogues = true;

        private IReadOnlyList<string> DisabledCatalogues =>
            profileService?.ActiveProfile?.FramingAssistantSettings?.DisabledCatalogues ?? [];

        private bool UsesObserver => EffectiveProjectionMode == SkyMapProjectionMode.AltAz || ShowHorizon;

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
            imageCache?.Dispose();
            rasterRenderer?.Dispose();
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

            sceneBuilder ??= new SkyMapSceneBuilder(
                dbConstellations,
                dbDeepSkyObjects.Values.ToArray(),
                constellationBoundaries);
            rasterRenderer = new SkyMapRasterRenderer((int)ViewportFoV.Width, (int)ViewportFoV.Height);
            imageCache = new SkyMapImageCache(cache);

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
            return ViewportFoV;
        }

        public void BeginInteraction() {
            CancelImageLoad();
            interactionRenderPending = false;
            interactionRenderTimer.Stop();
            isInteracting = true;
        }

        public void EndInteraction() {
            interactionRenderPending = false;
            interactionRenderTimer.Stop();
            isInteracting = false;
            UpdateSkyMap();
        }

        public Coordinates ShiftViewport(Vector delta) {
            SkyMapObserverSnapshot observer = UsesObserver ? CreateObserverSnapshot() : null;
            SkyMapViewportProjection projection = GetProjection(observer, out _);
            Coordinates center = projection.ShiftCenter(delta);
            ViewportFoV = new ViewportFoV(
                center,
                ViewportFoV.VFoV,
                ViewportFoV.Width,
                ViewportFoV.Height,
                ViewportFoV.Rotation);
            return ViewportFoV.CenterCoordinates;
        }

        public void UpdateSkyMap() {
            if (suppressRender) {
                return;
            }
            if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess()) {
                _ = dispatcher.InvokeAsync(UpdateSkyMap);
                return;
            }
            if (!Initialized || sceneBuilder is null) {
                return;
            }

            int version = Interlocked.Increment(ref renderVersion);
            if (isInteracting) {
                interactionRenderPending = true;
                if (!interactionRenderTimer.IsEnabled) {
                    interactionRenderTimer.Start();
                }
                return;
            }

            RenderCurrentFrame(SkyMapRenderQuality.Final);
            QueueMissingImages(version);
        }

        private void RenderCurrentFrame(SkyMapRenderQuality quality = SkyMapRenderQuality.Final) {
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
                options |= EffectiveProjectionMode == SkyMapProjectionMode.AltAz
                    ? SkyMapRenderOptions.HorizontalGrid
                    : SkyMapRenderOptions.EquatorialGrid;
            }
            if (ShowHorizon) {
                options |= SkyMapRenderOptions.Horizon;
            }

            SkyMapObserverSnapshot observer = UsesObserver ? CreateObserverSnapshot() : null;
            Projection = GetProjection(observer, out bool projectionChanged);
            SkyMapScene scene = sceneBuilder.Build(Projection, options, DisabledCatalogues);
            bool telescopeVisible = telescopeConnected
                && Projection.Contains(telescopeCoordinates)
                && (!ShowHorizon || observer.IsVisible(telescopeCoordinates));
            Point? telescopePosition = telescopeVisible
                ? Projection.Project(telescopeCoordinates)
                : null;
            SkyMapOverlay = UseCachedImages && imageCache is not null
                ? imageCache.UsePlacements(
                    Projection,
                    images => rasterRenderer.Render(scene, images, telescopePosition, quality))
                : rasterRenderer.Render(scene, [], telescopePosition, quality);
            if (projectionChanged) {
                ProjectionChanged?.Invoke(this, EventArgs.Empty);
            }
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
            } catch (ObjectDisposedException) when (token.IsCancellationRequested) {
            } catch (Exception ex) {
                Logger.Error("Failed to load an offline sky map image.", ex);
            }
        }

        public void ClearImagesForViewport() {
            CancelImageLoad();
            imageCache?.Dispose();
            imageCache = new SkyMapImageCache(cache);
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
            Interlocked.Increment(ref renderVersion);
            Initialized = false;
            telescopeMediator?.RemoveConsumer(this);
            if (profileService is not null) {
                profileService.ProfileChanged -= ProfileService_ProfileChanged;
                profileService.LocationChanged -= ProfileService_LocationOrHorizonChanged;
                profileService.HorizonChanged -= ProfileService_LocationOrHorizonChanged;
            }
            observerRefreshTimer?.Stop();
            if (observerRefreshTimer is not null) {
                observerRefreshTimer.Tick -= ObserverRefreshTimer_Tick;
            }
            interactionRenderTimer.Stop();
            interactionRenderTimer.Tick -= InteractionRenderTimer_Tick;
            CancelImageLoad();
            imageCache?.Dispose();
            rasterRenderer?.Dispose();
        }

        partial void OnAnnotateConstellationBoundariesChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.AnnotateConstellationBoundaries = newValue;
            }
            UpdateSkyMap();
        }

        partial void OnAnnotateConstellationsChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.AnnotateConstellations = newValue;
            }
            UpdateSkyMap();
        }

        partial void OnAnnotateDSOChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.AnnotateDSO = newValue;
            }
            UpdateSkyMap();
        }

        partial void OnAnnotateGridChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.AnnotateGrid = newValue;
            }
            UpdateSkyMap();
        }

        partial void OnProjectionModeChanged(SkyMapProjectionMode oldValue, SkyMapProjectionMode newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.SkyMapProjectionMode = newValue;
            }
            UpdateSkyMap();
        }

        partial void OnDynamicFoVChanged(bool oldValue, bool newValue) {
            OnPropertyChanged(nameof(EffectiveProjectionMode));
            UpdateSkyMap();
        }

        partial void OnObservationTimeChanged(DateTime? oldValue, DateTime? newValue) {
            DateTime timestamp = newValue?.ToUniversalTime() ?? DateTime.UtcNow;
            if (observerSnapshot is not null
                && newValue is not null
                && timestamp >= observerSnapshot.Timestamp
                && !observerSnapshot.NeedsRefresh(timestamp)) {
                return;
            }

            observerSnapshot = null;
            if (UsesObserver) {
                UpdateSkyMap();
            }
        }

        partial void OnShowHorizonChanged(bool oldValue, bool newValue) {
            if (profileService?.ActiveProfile?.FramingAssistantSettings is { } settings) {
                settings.ShowHorizon = newValue;
            }
            observerSnapshot = null;
            UpdateSkyMap();
        }

        partial void OnUseCachedImagesChanged(bool oldValue, bool newValue) {
            if (!newValue) {
                CancelImageLoad();
            }
            UpdateSkyMap();
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
            suppressRender = true;
            try {
                AnnotateConstellationBoundaries = settings.AnnotateConstellationBoundaries;
                AnnotateConstellations = settings.AnnotateConstellations;
                AnnotateDSO = settings.AnnotateDSO;
                AnnotateGrid = settings.AnnotateGrid;
                ProjectionMode = settings.SkyMapProjectionMode;
                ShowHorizon = settings.ShowHorizon;
            } finally {
                suppressRender = false;
            }
        }

        private SkyMapObserverSnapshot CreateObserverSnapshot() {
            DateTime timestamp = ObservationTime?.ToUniversalTime() ?? DateTime.UtcNow;
            if (observerSnapshot is not null && !observerSnapshot.NeedsRefresh(timestamp)) {
                return observerSnapshot;
            }

            var settings = profileService.ActiveProfile.AstrometrySettings;
            Func<double, double> horizonAltitude = settings.Horizon is null
                ? null
                : settings.Horizon.GetAltitude;
            observerSnapshot = new SkyMapObserverSnapshot(
                settings.Latitude,
                settings.Longitude,
                timestamp,
                horizonAltitude);
            return observerSnapshot;
        }

        private SkyMapViewportProjection GetProjection(SkyMapObserverSnapshot observer, out bool changed) {
            changed = Projection is null
                || !ReferenceEquals(Projection.Viewport, ViewportFoV)
                || Projection.Mode != EffectiveProjectionMode
                || !ReferenceEquals(Projection.Observer, observer);
            if (changed) {
                Projection = new SkyMapViewportProjection(ViewportFoV, EffectiveProjectionMode, observer);
            }
            return Projection;
        }

        private void ObserverRefreshTimer_Tick(object sender, EventArgs e) {
            if (ObservationTime is null
                && UsesObserver
                && (observerSnapshot is null || observerSnapshot.NeedsRefresh(DateTime.UtcNow))) {
                observerSnapshot = null;
                UpdateSkyMap();
            }
        }

        private void InteractionRenderTimer_Tick(object sender, EventArgs e) {
            interactionRenderTimer.Stop();
            if (!isInteracting || !interactionRenderPending || !Initialized) {
                return;
            }
            interactionRenderPending = false;
            RenderCurrentFrame(SkyMapRenderQuality.InteractionPreview);
        }

        private void ProfileService_LocationOrHorizonChanged(object sender, EventArgs e) {
            observerSnapshot = null;
            if (UsesObserver) {
                UpdateSkyMap();
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            observerSnapshot = null;
            LoadSettings();
            UpdateSkyMap();
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
