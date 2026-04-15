# NINA Project Map

Use this reference after reading `AGENTS.md` and the relevant project-local `ARCHITECTURE.md`.

## Layering

- `NINA.Core`: shared utilities, logging, localization, enums, common models, SQLite EF context, and protobuf contracts. Keep dependencies pointing outward from this foundation.
- `NINA.Profile`: persisted profile and typed settings model. Add settings here, not in ad hoc files.
- `NINA.Astrometry`: coordinates, astronomy math, twilight/night calculations, catalog access, and IERS earth-rotation updates.
- `NINA.Image`: image model, file formats, RAW/FITS/XISF/TIFF loading and saving, rendering, statistics, and star analysis.
- `NINA.MGEN`: standalone MGEN2/MGEN3 guider protocol/SDK library. Keep it free of WPF and profile dependencies.
- `NINA.Equipment`: device abstractions and ASCOM, Alpaca, native SDK, file, and utility device adapters.
- `NINA.PlateSolving`: plate solver factory and solve/capture/centering orchestration over solver integrations.
- `NINA.CustomControlLibrary`: reusable WPF custom controls and default themes.
- `NINA.WPF.Base`: shared WPF mediators, dockable/view-model bases, equipment UI support, and sky survey subsystem.
- `NINA.Plugin`: plugin manifests, install/update/remove, compatibility, loading, MEF composition, plugin resources, and extension points.
- `NINA.Sequencer`: advanced sequencer entity model, execution, serialization, target/template storage, expressions, and symbols.
- `NINA.Sequencer.Generators`: Roslyn source generator for expression-backed sequence properties; used as an analyzer by `NINA.Sequencer`.
- `NINA`: executable WPF app, startup, DI composition, shell, app-specific view models/views, runtime assets.
- `NINA.Setup`: WiX MSI packaging; keep install layout aligned with runtime output.
- `NINA.SetupBundle`: WiX Burn bootstrapper, release-note conversion, and outer installer UX.
- `NINA.Test`: NUnit verification layer; folder layout mirrors production areas.

## Common Starting Points

- App startup, shell, DI, app-specific view models: `NINA/App.xaml.cs`, `NINA/CompositionRoot.cs`, `NINA/Utility/IoCBindings.cs`, `NINA/ViewModel/*`.
- New persisted settings or profile behavior: `NINA.Profile/Profile.cs`, `ProfileService.cs`, settings interfaces/classes, `KnownType` declarations, and change-notification registration.
- Localization: `NINA.Core/Locale/Locale.resx` only. Use `Loc.Instance[...]` in code and `{ns:Loc Key}` in XAML.
- Database schema or seed data: `NINA.Core/Database/NINADbContext.cs` plus runtime SQL under `NINA/Database/Initial` or `NINA/Database/Migration`.
- New runtime file: add output copy behavior in `NINA/NINA.csproj`; check `NINA.Setup/Product.wxs`.
- Shared cross-layer UI action: prefer an existing mediator in `NINA.WPF.Base/Mediator`; add a narrow interface/mediator there if needed, then wire in `NINA/Utility/IoCBindings.cs`.
- New device integration: start in `NINA.Equipment/Interfaces`, implementation folders, and ASCOM/Alpaca/native SDK helpers. Use `IProfileService` for settings.
- New image algorithm or file handling: start in `NINA.Image`; route supported formats through the existing image pipeline.
- New plate solver: add under `NINA.Platesolving/Solvers` and register in `PlateSolverFactory`.
- New plugin extension point: update public interfaces and `NINA.Plugin/PluginLoader.cs` composition/import logic.
- New sequence entity: place under `NINA.Sequencer/SequenceItem`, `Conditions`, `Trigger`, or `Container`; add MEF metadata, validation, cloning, parent attachment, and serialization compatibility.
- Expression-backed sequence property: follow the `[UsesExpressions]` and `[IsExpression]` generator pattern from `NINA.Sequencer.Generators`.
- Installer behavior: `NINA.Setup` for MSI contents and actions; `NINA.SetupBundle` for bootstrapper shell and release-note presentation.

## Neighboring Checks

- DI or first-level composition change: check `NINA/Utility/IoCBindings.cs`, `NINA/CompositionRoot.cs`, and relevant view-model consumers.
- Dockable panel or equipment UI change: check `DockManagerVM`, `EquipmentVM`, mediator registration, and docking settings.
- Sequencer palette or plugin-visible entity change: check `PluginLoader`, MEF export metadata, `SequencerFactory`, clone behavior, and `NINA.Sequencer/Serialization/*`.
- Profile setting change: check serialization compatibility, default construction, migrations for moved serialized types, and consumers that cache settings.
- Native dependency change: check output layout, architecture-specific folders, test asset copying, and WiX packaging.
- Astronomy/numerical change: add tests with authoritative reference values, edge cases, and regression coverage in `NINA.Test/AstrometryTest` or the closest matching fixture.
- Image statistics or star detection change: test consumers in autofocus, plate solving, image history, and sequencing where relevant.
- User-facing behavior change: consider `RELEASE_NOTES.md` and docs in the separate `NINA.Docs` submodule/repository.

## Test Commands

Use targeted filters while iterating:

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet test NINA.Test\NINA.Test.csproj --filter 'FullyQualifiedName~NINA.Test.SomeFixture' -v minimal
```

Run the broader CLI sequence from `CONTRIBUTING.md` when risk or blast radius justifies it:

```powershell
dotnet restore NINA.sln
dotnet build NINA\NINA.csproj --configuration Debug --no-restore
dotnet build NINA.Test\NINA.Test.csproj --configuration Debug --no-restore
dotnet test NINA.Test\NINA.Test.csproj --configuration Debug --no-build -p:PlatformTarget=x64
```
