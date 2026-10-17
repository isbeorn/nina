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
  Ship native DLLs and helper executables such as `exiftool.exe` that other projects call at runtime.

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

### Plugin Manager

`ViewModel/Plugins/PluginsVM` reconciles compatible catalog manifests and the loader's installed dictionary into identifier-owned `PluginEntryVM` instances. The dictionary Boolean records load success. Pending install, update and removal state belongs to these app-side entries and survives catalog refresh for the current session. Fetching, compatibility checks and filesystem installation remain in `NINA.Plugin`.

`View/Plugins/PluginsView` reuses list/detail presentation across two distinct sections. Installed is the first tab and initial landing section, even with no local plugins. Available presents catalog entries for discovery. Each section retains its own search, selected tags and plugin selection. Tag choices use OR matching, intersect search and include untagged plugins when no tags are selected. The picker offers alphabetically sorted tags used by at least two distinct compatible catalog identifiers and present in the active section. Whitespace and case differences are normalized for filtering and each plugin contributes at most once to a tag's count. Displayed counts reflect the active section before search or tag filtering: Installed counts local entries and Available counts catalog entries. All original tags remain searchable and visible in plugin details. Installed lists updates first, then sorts alphabetically; Available sorts alphabetically. Refresh retains valid selected tags by name across count changes and removes withdrawn choices. The checked dropdown uses the existing WPF toolkit with NINA resources. Reentrant filter or section changes during list rebinding schedule another presentation rebuild so the active section and displayed entries agree. The manager checks for updates after loading installed plugins so sidebar update badges work before the view opens; the view requests the same coordinated initial fetch for an empty installation.

Plugin details separate Description, Changelog, Showcase, Versions and Info. Changelog appears only when the displayed manifest has a changelog URL and loads on opening. `PluginChangelogVM` owns cancellable display state and `PluginContentCache` stores validated Markdown and its document URL under `%LOCALAPPDATA%\NINA\Cache\PluginChangelogs`, keyed by the full changelog URL and displayed version. Notes remain fresh for seven days; expired notes appear immediately while refreshing and remain available with Retry after a failed refresh. Atomic replacements preserve the previous cache on failure. GitHub blob and Bitbucket source Markdown links resolve to raw documents. HTML and unsupported responses keep a browser link instead of rendering as Markdown; the browser link is always available. Relative Markdown assets resolve against the fetched document URL. Selection, version, URL, tab visibility and unload changes cancel obsolete display work. Installed opens Settings first when an options template exists. Settings templates are resolved by the existing plugin-name resource key and receive the original loaded manifest instance. The settings presenter stays alive when switching detail tabs. Catalog wrappers must never replace that settings instance. `AvailablePluginsView` retains its public view type as a wrapper around the shared browser.

`View/Plugins/PluginImage` loads list/detail icons through `PluginContentCache`. `PluginShowcaseVM` loads screenshots through the same cache when Showcase opens and supplies successfully decoded images in manifest order to the reusable `ImageGallery` control. The gallery accepts any number of images; the current manifest adapter supplies the two existing screenshot fields. The gallery fits a single image to the available pane without scrolling, with bottom selection dots and wrapping previous/next navigation for multiple images. Failed screenshots are omitted. Plugin, version or URL changes reset the gallery and discard obsolete UI completions. The cache centralizes URL/version hashing, seven-day freshness checks and atomic persistence across images and changelogs while retaining their existing formats and separate directories. Decoded images persist under `%LOCALAPPDATA%\NINA\Cache\PluginIcons`, keyed by the full image URL and displayed manifest version and fresh for seven days. A different version fetches its own artwork even when the URL is unchanged. Installed and Available retain independent cache entries for their displayed versions. Expired images refresh on demand; a failed refresh retains that version's cached image without extending its freshness. Concurrent requests share a download, successful replacements are atomic and cache access failures do not prevent displaying downloaded images.

The manager publishes only the newest catalog release per identifier through the existing `AvailablePlugins` contract. Opening Versions requests `/plugins/{identifier}/versions` from every configured repository, preserving the repository base path and standard NINA user agent. History discovery does not depend on the compatibility-filtered catalog: an installed plugin can find older compatible releases when the latest release requires a newer NINA or the catalog request failed. History is cached separately per source and identifier, so it never changes update detection or the offered manifest. Selection changes and refresh cancel obsolete requests. Refresh invalidates history attempts while retaining successful results until replacements arrive. Successful history responses replace the source's previous releases, including empty responses. Unsupported routes fall back to every compatible release in that repository's catalog. Failed sources retain usable releases and expose a Versions-only Retry action.

`PluginVersionVM` rows mark the installed release, pending target and actionable older or newer releases. Installed and pending releases remain visible when absent from the catalog, but only repository-supplied compatible releases can be downloaded. Installing an older or newer selected release uses the same serialized mutation path and restart state as an update. The original loaded manifest and settings remain active until restart. Both catalog and history fetching apply the loader's known plugin-version floor as well as application/API compatibility.

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
