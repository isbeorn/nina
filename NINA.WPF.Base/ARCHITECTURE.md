# NINA.WPF.Base Architecture

## Purpose

`NINA.WPF.Base` contains shared WPF infrastructure used by the main application and plugin-loaded UI components. It is the bridge between lower-level libraries and app-facing view models for common UI/device interactions.

Build shape from `NINA.WPF.Base.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled

## Top-Level Structure

- `Mediator/`
  Dispatcher-style mediator classes such as `ApplicationMediator`, `ApplicationStatusMediator`, `ImagingMediator`, `ImageSaveMediator`, and device mediators for camera, focuser, telescope, guider, dome, switch, and others
- `ViewModel/`
  Shared view-model base types and reusable view models, including:
  - `DockableVM`
  - equipment panels and choosers under `ViewModel/Equipment/*`
  - autofocus support under `ViewModel/AutoFocus/*`
  - `MeridianFlipVM` and `MeridianFlipVMFactory`
  - `PlateSolvingStatusVM`
- `Interfaces/`
  Contracts for mediators, utility services, and shared view models
- `SkySurvey/`
  Survey providers and cache wrappers such as `NASASkySurvey`, `SkyServerSkySurvey`, `ESOSkySurvey`, `Hips2FitsSurvey`, `FileSkySurvey`, `CacheSkySurvey`, `SkySurveyFactory`
- `Resources/`, `View/`, `Behaviors/`, `Utility/`, `Model/`
  Shared XAML resources, controls, behaviors, and support models

## Mediator Pattern

The mediator classes in `Mediator/` are intentionally simple. A typical mediator:

- stores a single registered handler
- exposes a narrow service surface to non-UI code
- forwards calls/events to the handler

Examples:

- `ApplicationStatusMediator`
  Forwards `ApplicationStatus` updates to the single registered `IApplicationStatusVM`.
- `ImagingMediator`
  Forwards capture/prepare/live-view requests to `IImagingVM`.
- `ImageSaveMediator`
  Forwards enqueue and save-event traffic to `IImageSaveController`.

This keeps non-UI code dependent on interfaces instead of concrete view-model implementations.

## Shared View-Model Layer

`ViewModel/BaseVM.cs` and `ViewModel/DockableVM.cs` are the main shared base classes:

- `BaseVM`
  Carries the active `IProfile` through `IProfileService`.
- `DockableVM`
  Adds title, icon, visibility, settings toggles, and docking-related behavior.

The equipment view models under `ViewModel/Equipment/*` are the reusable UI-facing wrappers around the device abstractions from `NINA.Equipment`.

`ViewModel/Equipment/Dome/DomeFollower.cs` is a good example of the project's role: it coordinates dome-following behavior using profile settings and equipment mediators, but it still lives in a reusable UI/support layer rather than the app executable.

## Sky Survey Subsystem

The `SkySurvey/` folder is a contained subsystem for retrieving and caching survey imagery. `SkySurveyFactory` selects implementations based on `SkySurveySource`, and the project provides both remote providers and cache/file-backed variants.

This functionality is shared infrastructure for features like the sky atlas and framing workflows.

The offline framing map uses a separate frame pipeline inside this subsystem:

- `SkyMapSceneBuilder` projects constellations, stars, DSO outlines, constellation boundaries, equatorial or Alt/Az grid lines, and the local horizon into one per-viewport scene. Catalogue-wide data is indexed or precomputed when the builder is created, and clipped path construction reuses empty point buffers instead of allocating for every rejected sample; do not restore full catalogue scans, per-sample list allocation, or retained mutable annotation view models to the drag path.
- `SkyMapViewportProjection` is the single projection and pan boundary for the scene, cached survey tiles, telescope marker, and framing-camera overlays. Equatorial mode must remain compatible with `ViewportFoV`; Alt/Az mode converts through the shared observer snapshot so every layer, camera position angle, and drag delta follows the horizontal grid. `SkyMapAnnotator.ObservationTime` optionally supplies the shared observation instant; when it is unset, the annotator retains its automatic current-time refresh behavior.
- `SkyMapRasterRenderer` draws that scene into one reusable WPF `WriteableBitmap`. With normal WPF rendering, cached images stay in the retained WPF composition path. When WPF is forced into software-only mode, cached images and annotations are CPU-composited into one surface instead of making WPF transform every tile. Software-only dragging renders into a private 50% scratch bitmap, then expands it with nearest-neighbor sampling into the reusable full-viewport surface before publication. Preview and final frames therefore share the viewport coordinate space used by cached images, framing-camera overlays and pointer input. Nested image and DSO rotations must save and restore the incoming graphics transform so they cannot discard the preview scale used by later annotations. The surfaces are intentionally UI-thread-owned and mutable so dragging does not clone the full viewport bitmap each frame.
- `SkyMapImageCache` parses cached survey metadata once, serializes image decoding and CPU-raster conversion off the UI thread, and composites already-loaded tiles without hiding vector annotations during a drag. Its least-recently-used history is bounded by both image count and estimated pixel memory, while every tile in the active viewport is protected until the view moves. The cache lock also protects a tile raster for the duration of software composition so eviction cannot dispose an image that is being drawn. Keep the history bounds, active-view protection and render lifetime intact when changing the tile-loading path.
- `SkyMapAnnotator` coalesces drag invalidations to a 60 Hz interval in both rendering modes. It does not queue survey-image loads for intermediate previews and always renders a final full-quality frame when dragging ends. Hardware-accelerated rendering retains the normal WPF composition path while software-only rendering uses the reduced scratch frame described above.
- `SkyMapObserverSnapshot` is the time/location boundary for the Alt/Az grid and local-horizon clipping. It converts between celestial and Alt/Az coordinates, implements layer visibility against the configured horizon, and expires after one minute so time-dependent grids and clipping are rebuilt together. Reuse the snapshot throughout its lifetime; constructing one may require astronomical time data and does not belong in the drag path.

When the horizon is enabled, the scene contains both the visible horizon stroke and opaque below-horizon mask areas. The raster renderer draws those mask areas over cached imagery before drawing the visible annotations; filtering only catalogue objects is insufficient because cached survey pixels would otherwise remain visible below the horizon. Build partial masks in viewport space from projection inversion and horizon clearance; projected deep-sky polygons can cross the stereographic discontinuity and cover visible sky at wide fields of view. Emit the visible horizon stroke from the same viewport-space mask intersections instead of independently sampling it in angular coordinates, otherwise the stroke and imagery cutoff diverge at wide fields of view. Refine custom-horizon edge intersections against the actual clearance function so steep profile sections remain stable while panning.

Every celestial layer must be rebuilt from the same viewport and visibility snapshot. This keeps panning planetarium-like and prevents time-dependent horizon filtering from disagreeing between layers.

## Dependency Position

Project references:

- `NINA.Core`
- `NINA.Astrometry`
- `NINA.CustomControlLibrary`
- `NINA.Equipment`
- `NINA.Image`
- `NINA.MGEN`
- `NINA.PlateSolving`
- `NINA.Profile`
- `Accord.Imaging (NETStandard)`

The project is referenced by:

- `NINA`
- `NINA.Plugin`
- `NINA.Sequencer`
- `NINA.Test`

That places it between low-level runtime libraries and the final application shell.

## Contribution Notes

- Put shared WPF infrastructure here when it is not specific to one top-level app screen.
- Use mediators for cross-layer communication instead of reaching directly into concrete view models from lower layers.
- Keep app-shell composition out of this project; DI registration and final screen wiring still belong to `NINA`.
- When adding shared dockable or equipment-facing UI components, define interfaces here and let `NINA` decide how they are composed into the shell.
