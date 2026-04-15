# NINA Project Map

This is a route sheet. Treat `AGENTS.md`, `CONTRIBUTING.md`, and each project's `ARCHITECTURE.md` as the source of truth.

## Owners

| Area | Start Here | Neighboring Checks |
| --- | --- | --- |
| App startup, shell, DI | `NINA/App.xaml.cs`, `NINA/CompositionRoot.cs`, `NINA/Utility/IoCBindings.cs` | `MainWindow`, first-level VMs, service registrations |
| App-specific UI/VMs | `NINA/ViewModel`, `NINA/View` | `IoCBindings.cs`, localization, dock/equipment composition |
| Shared utilities, logging, locale, DB context | `NINA.Core` | `NINA/Database`, `Locale.resx`, generated protobuf consumers |
| Persisted settings and profiles | `NINA.Profile` | `Profile`, `ProfileService`, settings interfaces/classes, serialized compatibility |
| Astronomy math/catalogs | `NINA.Astrometry` | `NINA.Core.Database`, `NINA/Database`, `NINA.Test/AstrometryTest` |
| Image model, formats, statistics, star analysis | `NINA.Image` | native/runtime assets, autofocus, plate solving, image history, sequencing tests |
| Device integrations | `NINA.Equipment` | ASCOM/Alpaca helpers, vendor SDK folders, profile settings, mediators |
| MGEN transport/protocol | `NINA.MGEN` | runtime DLL layout, `NINA.Equipment` guider adapter |
| Plate solving | `NINA.Platesolving` | `PlateSolverFactory`, solver classes, capture/centering orchestration |
| Shared WPF mediators/VM infrastructure | `NINA.WPF.Base` | mediator interfaces, app DI wiring, concrete app handlers |
| Reusable custom controls | `NINA.CustomControlLibrary` | control class, default theme XAML, `Themes/Generic.xaml` |
| Plugin contracts/loading/install | `NINA.Plugin` | public interfaces, `PluginLoader`, manifest model, compatibility/install paths |
| Sequencer entities/runtime/serialization | `NINA.Sequencer` | MEF metadata, clone/parent/validation, `SequencerFactory`, serialization converters |
| Sequencer expression generator | `NINA.Sequencer.Generators` | generated contract, diagnostics, expression-backed entity tests |
| MSI packaging | `NINA.Setup` | runtime output layout, `Product.wxs`, shipped files/directories |
| Burn bootstrapper | `NINA.SetupBundle` | bundle UI/theme, release-note conversion, chained MSI |
| Tests | `NINA.Test` | folder matching production area, shared bootstrap/assets, x64/native dependencies |

## Recurring Checks

- New service, mediator, or first-level VM: update `NINA/Utility/IoCBindings.cs`; check `CompositionRoot.cs` when the shell directly consumes it.
- New setting: update profile interfaces, concrete settings, defaults, known types, change notifications, and compatibility/migration needs.
- New localized label: edit only `NINA.Core/Locale/Locale.resx`.
- New database schema/data requirement: coordinate `NINA.Core.Database.NINADbContext` with SQL under `NINA/Database`; prefer migrations for existing deployments.
- New runtime file/native dependency: update `NINA/NINA.csproj`; check `NINA.Setup` packaging and test output copying when relevant.
- New sequence entity or plugin-visible extension: check MEF export metadata, `PluginLoader`, factory/prototype creation, clone behavior, and JSON compatibility.
- User-facing behavior change: consider `RELEASE_NOTES.md`; handle user documentation separately in the `NINA.Docs` submodule/repository.
- Test selection: use `references/testing-map.md` for actual filters, command templates, and known test constraints.
