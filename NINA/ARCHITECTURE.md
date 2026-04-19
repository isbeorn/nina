# NINA Architecture

## Purpose

`NINA` is the executable WPF application. It owns application startup, dependency composition, the main window shell, and the app-specific view models and views that sit on top of the reusable libraries.

Build shape from `NINA.csproj`:

- Target framework: `net10.0-windows`
- Output type: `WinExe`
- Runtime: `win-x64`, self-contained
- WPF enabled

## Startup And Composition

The startup path is code-driven and easy to trace:

- `App.xaml.cs`
  Loads command-line options, initializes user settings, loads the active profile from the `ProfileService` application resource, configures logging/notifications, optionally shows profile selection, creates the composed main view model, shows `MainWindow`, and starts the `EarthRotationParameterUpdater`.
- `CompositionRoot.cs`
  Creates the dependency graph through `IoCBindings`, eagerly resolves major view models/controllers, initializes the Canon EDSDK wrapper, and builds `MainWindowVM`.
- `Utility/IoCBindings.cs`
  Registers the concrete service graph using `Microsoft.Extensions.DependencyInjection`. This is the central place where mediators, factories, view models, plugin loader, and cross-project services are wired together.
- `MainWindow.xaml(.cs)`
  Hosts the shell window and persists window placement across sessions.

In practice, `NINA` is the composition root for the whole application even though most implementation detail lives in referenced projects.

## Major Subsystems In This Project

- `ViewModel/`
  Contains app-specific orchestration view models such as `ApplicationVM`, `DockManagerVM`, `ImagingVM`, `OptionsVM`, `SkyAtlasVM`, `VersionCheckVM`, `Plugins/PluginsVM`, `Sequencer/SequenceNavigationVM`, `FramingAssistant/FramingAssistantVM`, `FlatWizard/FlatWizardVM`, and `ImageHistory/ImageHistoryVM`.
- `View/`
  Contains the WPF views for the shell and feature areas. The folder is large because most end-user UI is declared here in XAML.
- `Resources/`
  Ships application-level resource dictionaries, styles, icons, and images.
- `Database/`
  Ships `Initial/*.sql` and `Migration/*.sql`. `NINA.Core.Database.NINADbContext` expects these files to exist under the application base directory at runtime.
- `Sequencer/Examples/`
  Ships built-in sequence template JSON files.
- `External/` and `Utility/`
  Ship native DLLs and helper executables such as `dcraw.exe` and `exiftool.exe` that other projects call at runtime.

## Dependency Position

`NINA` depends on almost every library project in the solution:

- Domain and utilities: `NINA.Core`, `NINA.Astrometry`, `NINA.Profile`
- Imaging and equipment: `NINA.Image`, `NINA.Equipment`, `NINA.MGEN`, `NINA.PlateSolving`
- UI support: `NINA.WPF.Base`, `NINA.CustomControlLibrary`
- Extensibility and sequencing: `NINA.Plugin`, `NINA.Sequencer`

This project should not become the home for reusable abstractions. If a type is not shell-specific, the existing codebase usually places it in one of the library projects above.

## Plugin And Docking Integration

Two app-level coordinators are worth knowing before changing UI composition:

- `ViewModel/DockManagerVM.cs`
  Builds the dockable panel list, then waits for `IPluginLoader.Load()` and appends plugin-provided `IDockableVM` instances.
- `ViewModel/Sequencer/SequenceNavigationVM.cs`
  Waits for `IPluginLoader.Load()`, builds a `SequencerFactory` from plugin/core sequence entities, then creates both the simple and advanced sequencer view models.

This means plugin loading affects both the docking layout and the sequencer palette.

## Runtime Assets

`NINA.csproj` is also the place where the application decides which non-code artifacts are copied into the build output:

- database initialization/migration scripts
- native camera/device DLLs under `External/x64`
- localization and documentation assets
- sequencer example templates
- helper executables in `Utility`

If a new feature depends on a runtime file, the code change is usually not enough; the file also has to be added to `NINA.csproj`, and packaging may need updates in `NINA.Setup`.

## Contribution Notes

- Register new services in `Utility/IoCBindings.cs`; keep constructor wiring out of arbitrary call sites.
- Put app shell orchestration here, but move reusable logic down into the library projects when possible.
- If a new feature adds a dockable panel, sequence surface, or plugin-visible service, check both the DI registrations and the consumers in `DockManagerVM` or `SequenceNavigationVM`.
- If a change needs files at runtime, update both `NINA.csproj` and, when installer output matters, the WiX projects.
