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
