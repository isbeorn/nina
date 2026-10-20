# NINA.PlateSolving Architecture

## Purpose

`NINA.PlateSolving` provides a uniform orchestration layer over multiple plate-solving engines. It does not own image capture or telescope control itself; instead it coordinates those concerns through interfaces and mediators.

Build shape from `NINA.PlateSolving.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled

## Top-Level Structure

- Root-level orchestration types
  `PlateSolverFactory`, `ImageSolver`, `CaptureSolver`, `CenteringSolver`, parameter/result/progress types
- `Interfaces/`
  `IPlateSolver`, `IImageSolver`, `ICaptureSolver`, `ICenteringSolver`, `IPlateSolverFactory`
- `Solvers/`
  Concrete solver integrations:
  - `ASTAPSolver`
  - `AstrometryPlateSolver`
  - `LocalPlateSolver`
  - `Platesolve2Solver`
  - `Platesolve3Solver`
  - `AllSkyPlateSolver`
  - `TheSkyXImageLinkSolver`
  - `Dc3PinPointSolver`
  - base classes such as `BaseSolver` and `CLISolver`

## Factory And Orchestration

`PlateSolverFactory.cs` is the central selection point:

- `GetPlateSolver(...)` maps `PlateSolverEnum` values to concrete solver implementations.
- `GetBlindSolver(...)` maps `BlindSolverEnum` values to the corresponding solver choice.
- `PlateSolverFactoryProxy` also constructs higher-level orchestration objects:
  - `ImageSolver`
  - `CaptureSolver`
  - `CenteringSolver`

Those orchestration types separate concerns:

- `ImageSolver`
  Solves an already prepared image.
- `CaptureSolver`
  Captures through `IImagingMediator`, optionally restores filter state, and retries according to `CaptureSolverParameter`.
- `CenteringSolver`
  Repeatedly solves and slews through imaging, telescope, filter wheel, and dome mediators until the target is centered.

## Solver Base Class

`Solvers/BaseSolver.cs` provides shared mechanics used by the concrete solvers:

- validates solver configuration through `EnsureSolverValid(...)`
- creates `PlateSolveImageProperties`
- manages a shared working directory and failed-solve directory under `%LOCALAPPDATA%\\NINA\\PlateSolver`

Concrete solvers only implement `SolveAsyncImpl(...)`.

## Dependency Position

Project references:

- `NINA.Core`
- `NINA.Astrometry`
- `NINA.Equipment`
- `NINA.Image`
- `NINA.Profile`

That matches the architecture:

- settings come from the profile layer
- image preparation comes from the imaging layer
- telescope/capture/filter/dome actions come from equipment mediators
- solver orchestration and engine-specific parsing live here

## Contribution Notes

- Add new solver integrations under `Solvers/` and register them in `PlateSolverFactory`.
- Keep solver-specific process execution, file parsing, and result translation inside the solver class.
- Keep capture/slew orchestration in `CaptureSolver` or `CenteringSolver`, not inside individual solver implementations.
- Reuse the existing `PlateSolveParameter`, `PlateSolveResult`, and `PlateSolveImageProperties` flow instead of inventing per-solver parameter objects unless the data is truly solver-private.

## Plugin Plate Solver Providers

Plugins export `NINA.PlateSolving.Interfaces.IPlateSolverProvider` through MEF. Each provider has a stable, nonempty `Id`, a display `Name` and explicit primary/blind capabilities. IDs use ordinal comparison. The loader stages exports during composition, retains them only after the owning manifest initializes successfully and publishes one combined snapshot after loading all plugins and merging their resource dictionaries. Invalid exports are skipped. Every provider sharing a duplicate ID is excluded, regardless of load order; conflicts are logged.

`PlateSolverFactory.Providers` exposes a read-only snapshot. `ProvidersChanged` reports snapshot replacement. The static factory owns provider registration and lookup; the existing parameterless `PlateSolverFactoryProxy` uses the same factory methods. Builtin enum values, mappings and fresh concrete solver construction remain unchanged.

`IPlateSolveSettings` exposes `PlateSolverProviderId` and `BlindSolverProviderId`, implemented and persisted by `PlateSolveSettings`. A nonempty provider ID overrides its corresponding legacy enum. Selecting a plugin changes only that ID. Assigning the legacy enum always clears its override, even when the enum value is unchanged. The other role is unaffected. Null or empty IDs use the legacy factory path. The new DataMembers have `Order = 1` so they deserialize after the existing enum members; unknown IDs survive load, cloning and profile switches.

Plugin factory results are lazy adapters. They capture the selected ID and the supplied settings reference when requested, resolve the provider and its capability on first `SolveAsync` and create one solver per adapter with synchronized initialization. A missing or incompatible provider raises a localized exception only when used. There is no fallback to the retained enum. This permits an eagerly constructed but unused blind adapter to remain unavailable without blocking a successful primary solve. ImageSolver's existing unsuccessful-result failover, cloned blind parameter and exception/cancellation propagation remain unchanged.

The Options view lists the existing builtins in their original order, then compatible plugins. An unavailable saved ID remains selectable as an unavailable placeholder. The host uses an actual provider instance as settings content and explicitly looks up its implicit `DataTemplate`; absent templates produce an empty body. Shared provider instances show settings once, while distinct providers with identical names remain separate. Builtin deduplication maps blind `PINPOINT` to primary `PINPONT` rather than comparing enum ordinals.

Compatibility covers plugins consuming the host-provided settings. `IPlateSolveSettings` adds the two provider ID properties; existing members, solver interfaces, enums and factory constructor signatures are unchanged. Older applications can read the retained legacy enum but may drop the new provider ID members when saving a profile. Back up profiles before downgrading.
