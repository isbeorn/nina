# AGENTS.md

This file is a solution-wide navigation guide for humans and coding agents working in `NINA.sln`. It complements the per-project `ARCHITECTURE.md` files and stays focused on boundaries that are backed up by the code.

For repository-wide contribution workflow, branch expectations, release-note updates, and general coding rules, also read [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Scope

This guide covers the projects listed in `NINA.sln`.

## Documentation Boundary

- `NINA.Docs` is a git submodule declared in `.gitmodules` and points to `https://github.com/isbeorn/nina.docs.git`.
- It is not part of the `NINA.sln` project architecture documented below.
- If a code change also requires user-facing documentation updates, handle that separately in the `NINA.Docs` submodule / `nina.docs` repository, following the documentation notes in [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Repository Knowledge

- Treat repository-local, versioned files as the system of record for future contributors and coding agents.
- Keep this file as a map. Put detailed subsystem guidance in the owning `ARCHITECTURE.md`, `CONTRIBUTING.md`, or a focused checked-in reference.
- If a review, bug, or repeated agent mistake reveals a durable rule, update the nearest doc, test, analyzer, script, or route map instead of relying on off-repo memory.

## Code Style And Formatting

- Maintain the existing line-ending style of every touched file; default to CRLF for new files unless the target location dictates LF.
- Treat the root [`.editorconfig`](.editorconfig) as the canonical C# style source. It covers indentation, line endings, namespace style, `using` placement, `var` preferences, naming, and selected analyzer severities.
- Do not introduce new warnings casually. If an existing warning blocks a focused cleanup, keep the fix scoped and document any intentional deferral in the PR.
- For XAML, follow surrounding file style; no repo-wide XAML formatter configuration is checked in.
- Prefer modern C# supported by the target project. For new or refactored MVVM code, prefer `CommunityToolkit.Mvvm` where it fits instead of expanding legacy relay-command patterns.

## Project Architecture Docs

Read the project-local architecture doc before making non-trivial changes in that project:

- [`NINA/ARCHITECTURE.md`](NINA/ARCHITECTURE.md)
- [`NINA.Astrometry/ARCHITECTURE.md`](NINA.Astrometry/ARCHITECTURE.md)
- [`NINA.Core/ARCHITECTURE.md`](NINA.Core/ARCHITECTURE.md)
- [`NINA.CustomControlLibrary/ARCHITECTURE.md`](NINA.CustomControlLibrary/ARCHITECTURE.md)
- [`NINA.Equipment/ARCHITECTURE.md`](NINA.Equipment/ARCHITECTURE.md)
- [`NINA.Image/ARCHITECTURE.md`](NINA.Image/ARCHITECTURE.md)
- [`NINA.MGEN/ARCHITECTURE.md`](NINA.MGEN/ARCHITECTURE.md)
- [`NINA.Platesolving/ARCHITECTURE.md`](NINA.Platesolving/ARCHITECTURE.md)
- [`NINA.Plugin/ARCHITECTURE.md`](NINA.Plugin/ARCHITECTURE.md)
- [`NINA.Profile/ARCHITECTURE.md`](NINA.Profile/ARCHITECTURE.md)
- [`NINA.Sequencer/ARCHITECTURE.md`](NINA.Sequencer/ARCHITECTURE.md)
- [`NINA.Sequencer.Generators/ARCHITECTURE.md`](NINA.Sequencer.Generators/ARCHITECTURE.md)
- [`NINA.Setup/ARCHITECTURE.md`](NINA.Setup/ARCHITECTURE.md)
- [`NINA.SetupBundle/ARCHITECTURE.md`](NINA.SetupBundle/ARCHITECTURE.md)
- [`NINA.Test/ARCHITECTURE.md`](NINA.Test/ARCHITECTURE.md)
- [`NINA.WPF.Base/ARCHITECTURE.md`](NINA.WPF.Base/ARCHITECTURE.md)

Note: the solution project is named `NINA.PlateSolving`, but the folder on disk is `NINA.Platesolving`.

## Solution Map

The solution has a clear layering pattern.

### Foundation

- `NINA.Core`
  Shared utilities, logging, localization, enums, common models, SQLite EF context, and generated protobuf contracts.
- `NINA.Profile`
  Persisted user profile and typed settings model.
- `NINA.Astrometry`
  Astronomy math, coordinate types, night/twilight calculations, catalog queries.

### Runtime Domain Libraries

- `NINA.Image`
  Image model, file formats, analysis, star detection, rendering helpers.
- `NINA.MGEN`
  Standalone MGEN2/MGEN3 transport/protocol library.
- `NINA.Equipment`
  Device abstractions and concrete ASCOM/Alpaca/native adapters.
- `NINA.Platesolving`
  Plate-solver integrations and orchestration.

### Shared UI Infrastructure

- `NINA.CustomControlLibrary`
  Reusable custom WPF controls and themes.
- `NINA.WPF.Base`
  Shared mediators, dockable/view-model base classes, equipment UI support, sky survey subsystem.

### Extensibility And Sequencing

- `NINA.Plugin`
  Plugin manifests, loading, installation, compatibility checks, MEF composition.
- `NINA.Sequencer`
  Advanced sequencer engine, entity model, serialization, target/template storage, expressions/symbols.
- `NINA.Sequencer.Generators`
  Roslyn source generator used by sequencer expression-backed properties.

### App Shell, Packaging, Verification

- `NINA`
  Executable WPF app, DI composition root, main shell, app-specific views/view models, runtime assets.
- `NINA.Setup`
  WiX MSI project.
- `NINA.SetupBundle`
  WiX Burn bootstrapper.
- `NINA.Test`
  NUnit test suite.

### Non-`NINA.*` Solution Projects

These are in `NINA.sln` and matter during dependency tracing, but they do not have `ARCHITECTURE.md` files from the previous documentation pass:

- `Accord.Imaging (NETStandard)`
- `nikoncswrapper`

## Startup And Composition

The executable startup path is code-driven:

1. `NINA/App.xaml.cs`
   Parses command-line options, initializes user settings, loads/selects the active profile, configures logging and notifications, and shows the main window.
2. `NINA/CompositionRoot.cs`
   Builds the application service provider and eagerly resolves major view models/controllers.
3. `NINA/Utility/IoCBindings.cs`
   Registers the DI graph with `Microsoft.Extensions.DependencyInjection`.
4. `NINA/MainWindow.xaml(.cs)`
   Hosts the shell.

If you are unsure where an app-wide service comes from, start with `IoCBindings.cs`.

## Cross-Cutting Runtime Rules

### Profiles And Settings

- `IProfileService` is the central runtime configuration service.
- Typed settings live in `NINA.Profile`, not in scattered ad hoc files.
- A profile change can invalidate cached settings references; many components subscribe to `ProfileChanged` for that reason.

### Database

- The EF6/SQLite context lives in `NINA.Core.Database.NINADbContext`.
- The SQL files it consumes at runtime live under `NINA/Database`.
- A schema or initialization change usually touches both places.

### Localization

- User-visible labels are generally backed by `NINA.Core.Locale.Loc`.
- Sequence/plugin metadata often passes label keys like `Lbl_*` and resolves them at runtime.
- For locale additions or label changes, edit only `NINA.Core/Locale/Locale.resx`, which is the source resource file in the current solution layout.
- Do not manually edit the other `Locale.<culture>.resx` files; those are managed through Crowdin, as noted in [`CONTRIBUTING.md`](CONTRIBUTING.md).

### Third-Party Licenses

- Any third-party package addition, removal, or replacement requires a license metadata check before the change is complete.
- Keep both the in-app list in `NINA/View/About/ThirdPartyLicensesView.xaml` and the bundled text file `NINA/3rd-party-licenses.txt` synchronized with the actual dependency set.
- Remove stale license entries when a package is no longer used.
- If a dependency offers multiple licenses and this project intentionally uses one of them, document the chosen license consistently in both places.

### Native Runtime Assets

Runtime code expects files in the application output layout, especially under:

- `NINA/External`
- `NINA/Utility`
- `NINA/Database`
- `NINA/Sequencer/Examples`

If code starts depending on a new runtime file, the change is not complete until:

- the file is copied by `NINA.csproj`
- the installer packages it when necessary (`NINA.Setup`)

## Plugin And Sequencer Surface

The plugin and sequencer systems are tightly connected.

### Plugin Loading

`NINA.Plugin.PluginLoader`:

- loads built-in sequencer/entity types first
- scans plugin DLLs from versioned folders under `%LOCALAPPDATA%\\NINA`
- composes sequence items, conditions, triggers, containers, dockable view models, pluggable behaviors, and equipment providers through MEF
- merges plugin resource dictionaries into the WPF application resources

### Sequencer Consumption

The main app does not hard-code the final sequencer palette. Instead:

- `NINA.ViewModel.Sequencer.SequenceNavigationVM` waits for plugin loading, then builds `SequencerFactory`
- `SequencerFactory` exposes cloneable entity prototypes to the sequence editor
- `NINA.Sequencer.Serialization.SequenceJsonConverter` deserializes through factory-backed creation converters

If you add a new sequence entity or extension point, check all of:

- the sequencer type itself
- MEF export metadata
- plugin loader composition
- serialization/clone behavior

## Shared UI And Mediators

Lower-level runtime code communicates upward through mediator interfaces rather than directly referencing concrete WPF view models.

Primary mediator implementations live in `NINA.WPF.Base/Mediator`.

The executable wires those mediators to concrete handlers through DI in `NINA/Utility/IoCBindings.cs`.

If a library project needs to trigger UI-side behavior, prefer an existing mediator or add a new interface/mediator pair in `NINA.WPF.Base` instead of reaching into `NINA` directly.

## Scientific And Numerical Correctness

- `NINA.Astrometry` already depends on established astronomy libraries and models through `SOFA` and `NOVAS`, and `NINA.Test` preloads `SOFAlib.dll` plus `NOVAS31lib.dll` during test bootstrap.
- For astronomical calculations and other mathematically sensitive code, prefer algorithms that can be traced to published scientific papers, standards, or official reference documentation for the underlying model instead of ad hoc derivations.
- Validate those changes with extensive automated tests in `NINA.Test`, using reference values, edge cases, and regression coverage rather than only spot-checking results manually.
- Existing tests already follow that pattern in places such as `NINA.Test/AstrometryTest/AstrometryTest.cs`, which includes cases annotated as coming from documented SOFA values and other cited reference data.

## Test Bootstrap Constraints

- WPF-facing tests that instantiate `System.Windows.Application`, depend on `Application.Current.Resources`, or construct XAML-backed views should run STA and generally be `[NonParallelizable]`.
- New file-writing tests should prefer isolated temp paths or injectable path providers over shared user locations such as `%LOCALAPPDATA%`, unless the shared location is the behavior under test.

## Change Discipline

- Keep reusable logic out of `NINA` when a lower library already owns that concern.
- Keep UI shell composition out of `NINA.WPF.Base`; that project provides shared infrastructure, not final app assembly.
- Keep plugin runtime rules centralized in `NINA.Plugin`; do not duplicate plugin scanning or manifest logic in app view models.
- Keep runtime file layout expectations synchronized between code, build output, and installer authoring.
- If a boundary matters enough to document, consider whether a focused unit test, analyzer, or CI check can enforce it.
- For concrete owner and verification routing, use `.codex/skills/nina-repository/references/project-map.md` and `.codex/skills/nina-repository/references/testing-map.md`.
