# NINA.Core Architecture

## Purpose

`NINA.Core` is the lowest-level shared library in the solution's `NINA.*` stack. It provides common utility code, shared models/enums, localization, logging, database configuration, and generated protocol contracts used by higher layers.

Build shape from `NINA.Core.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled
- Protobuf compilation enabled for `Protos/API/ASCOM/Camera/CameraService.proto`

## Main Areas

- `Utility/`
  Shared infrastructure such as `CoreUtil`, `Logger`, `BaseINPC`, async commands/collections, caching helpers, serial interaction helpers, and `DeviceUpdateTimer`.
- `Database/`
  `NINADbContext.cs` plus EF6/SQLite configuration.
- `Locale/`
  `Loc.cs`, `ILoc.cs`, and the `Locale.*.resx` files used throughout the application.
- `Enum/`
  Shared enums such as `ApplicationTab`, `PlateSolverEnum`, `SequenceEntityStatus`, `LogLevelEnum`, and many equipment/settings enums.
- `Model/`
  Shared data contracts and lightweight model types such as `ApplicationStatus`, `GuideInfo`, `RMS`, `ImagePattern`, and sequence-related exception types.
- `Interfaces/`
  Cross-cutting interfaces like `IAutoCompleteItem`, `IPluggableBehavior`, and `IMyMessageBoxVM`.
- `Protos/`
  The gRPC service definition for the ASCOM camera service used by the SBIG integration.

## Key Infrastructure Types

- `Utility/CoreUtil.cs`
  Defines application-wide paths, version helpers, documentation URLs, file/path helpers, wait/delay helpers, and cleanup utilities.
- `Utility/Logger.cs`
  Configures Serilog output to console and log files under `%LOCALAPPDATA%\\NINA\\Logs`, including header generation and level mapping.
- `Utility/BaseINPC.cs`
  Base observable types used across the solution.
- `Utility/DeviceUpdateTimer.cs`
  Standard polling loop abstraction used by device-facing code to gather and publish state updates.
- `Utility/ApplicationResourceDictionary.cs`
  Minimal adapter over `Application.Current.Resources`.

## Database Role

`Database/NINADbContext.cs` defines the SQLite EF6 context and the mapped entity sets used by astronomy/catalog queries:

- earth rotation parameters
- bright stars
- DSO details
- constellations and constellation boundaries
- visual descriptions/catalogue numbers
- HiPS sky maps

The context also runs initialization/migration SQL from `Database/Initial` and `Database/Migration` under the application base directory. Those SQL files are shipped by the `NINA` executable project, so changes here often require corresponding runtime-file changes there.

## Localization Role

`Locale/Loc.cs` is the runtime accessor for the `Locale.*.resx` resources. Most higher-level projects depend on `Loc.Instance[...]` for user-visible strings, so label keys added here affect the entire application and plugin surface.

Contribution rule from [`../CONTRIBUTING.md`](../CONTRIBUTING.md):

- add or change source labels only in `Locale/Locale.resx`
- do not manually edit the translated `Locale.<culture>.resx` files; those are managed externally via Crowdin

## Protocol Role

`Protos/API/ASCOM/Camera/CameraService.proto` is compiled as part of this project. The generated types are consumed by the equipment layer for the SBIG gRPC/named-pipe camera service implementation.

## Dependency Position

This project has no `NINA.*` project references. Many projects above it depend on it:

- `NINA`
- `NINA.Astrometry`
- `NINA.CustomControlLibrary`
- `NINA.Equipment`
- `NINA.Image`
- `NINA.PlateSolving`
- `NINA.Plugin`
- `NINA.Profile`
- `NINA.Sequencer`
- `NINA.WPF.Base`

That makes `NINA.Core` a poor place for app-specific policy but the correct place for broadly reusable primitives.

## Contribution Notes

- Keep dependencies pointing outward from this project, not back up toward app/UI layers.
- Be conservative with changes to enums, shared models, and `CoreUtil`; those changes fan out across most of the solution.
- Database context changes usually need matching SQL changes in the executable project's `Database` folder.
- Localization keys added here become the canonical labels for both built-in features and plugin metadata translation.
