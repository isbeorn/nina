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

The shared `Resources/Styles/ProgressBar.xaml` dictionary supplies `LoadingProgressBar` for thin horizontal indeterminate activity and `InitializationLoadingTemplate` for the Imaging, Sequencer and Plugins initialization captions. Loading bars use the active profile's primary brush and stop animating when hidden. Compact spinner controls remain in inline action and sequence-row slots.

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

`DeviceChooserVM` exposes hub configuration through the existing setup command. Hub configuration commands and simple observable properties use CommunityToolkit.Mvvm generators. Partial property hooks manage configuration subscriptions, while mapped and synchronized accessors retain their domain logic. `HubConfigurationVM` manages child choosers and weather field sources using the active profile. The application maps it to `HubConfigurationView` in a standard modal setup window for hub membership and weather field assignments. Safety and Weather equipment panels retain their Settings column and stack the existing child-driver settings templates there. Those templates keep their own connection and editing rules. Equipment panels also show a compact cached child status list while the selected hub has an active session, including faulted sessions. Safety shows each child's safety reading, with shared connection and safety column widths across rows. Weather identifies unmapped children as not used. The Switch list omits the separator used below Safety and Weather details. Source edits notify already constructed status views. Hub session notifications can arrive from equipment worker threads, so command and collection updates return to the UI dispatcher. A profile change leaves an active hub session intact; when that session ends, the chooser synchronously rebuilds its configuration and selection from the active profile before reconnect or gear edits can resume.

`ViewModel/Equipment/Dome/DomeFollower.cs` is a good example of the project's role: it coordinates dome-following behavior using profile settings and equipment mediators, but it still lives in a reusable UI/support layer rather than the app executable.

## Sky Survey Subsystem

The `SkySurvey/` folder is a contained subsystem for retrieving and caching survey imagery. `SkySurveyFactory` selects implementations based on `SkySurveySource`, and the project provides both remote providers and cache/file-backed variants.

This functionality is shared infrastructure for features like the sky atlas and framing workflows.

The offline framing map builds a shared viewport scene, renders annotations and cached imagery and reuses one observer snapshot for coordinate transforms and horizon visibility. See [offline sky-map architecture](SkySurvey/ARCHITECTURE.md) for rendering, cache lifetime, drag behavior and horizon-mask contracts.

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
