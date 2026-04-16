# NINA.Profile Architecture

## Purpose

`NINA.Profile` owns persisted user profiles and the typed settings model behind them. It is the central source of runtime configuration across the application.

Build shape from `NINA.Profile.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled

## Data Model

`Profile.cs` is the aggregate root. It is a `DataContract` containing the concrete settings sections used throughout the application, including:

- application, astrometry, camera, color schema
- dome, filter wheel, focuser, guider, rotator, switch, telescope, weather data
- image, image file, image history
- plate solving, sequencing, flat wizard, flat device
- plugin settings, GNSS, Alpaca, dock layout, safety monitor

The project keeps both interface and implementation types:

- interfaces in `Interfaces/`
- serializable concrete settings classes in the project root

`Settings.cs` is the abstract base for settings sections.

## Profile Lifecycle

`ProfileService.cs` is the operational core.

Responsibilities that are explicit in the code:

- manages the profile directory at `%LOCALAPPDATA%\\NINA\\Profiles`
- loads profiles from `*.profile` files
- creates a default profile when none exists
- tracks available profiles in an `AsyncObservableCollection<ProfileMeta>`
- selects the active profile and publishes `ProfileChanged`, `LocaleChanged`, `LocationChanged`, and `HorizonChanged`
- delays saves through a timer to avoid writing on every property change
- watches the profile directory with `FileSystemWatcher`
- clones and removes profiles
- exposes `Release()` to close the active profile file handle

`ProfileService.TryLoad(...)` and `SelectProfile(...)` are the key entry points for startup and profile switching.

## Serialization And File Locking

`Profile` itself manages its persisted file. The service loads and disposes profiles rather than treating them as detached DTOs. That is why `ProfileService.Release()` and `SelectProfile(...)` explicitly dispose the current profile before replacing it.

This is also why many higher-level components subscribe to `ProfileChanged` instead of caching settings references indefinitely.

## Generated Plugin Settings Support

`PluginSettingsTemplate.tt` generates strongly-typed storage and accessor code for plugin settings:

- `IPluginSettings`
- `IPluginOptionsAccessor`
- `PluginSettings`
- `PluginOptionsAccessor`

The generated code stores plugin-specific values under the active profile keyed by plugin GUID and setting name.

This template is part of the architecture, not just a build artifact: plugin options in the rest of the solution depend on it.

## Single-Profile Activation Signals

`ProfileService` also contains inter-process coordination code:

- `ActivateInstanceOfNinaReferencingProfile(...)`
- `ActivateInstanceWatcher(...)`

These methods use named `EventWaitHandle`s keyed by profile ID so another instance can activate the running window for the same profile.

## Migration Support

`MigrateModularizedSolutionNamespaceChange()` shows that this project is also responsible for profile-file schema migration when serialized type names move between assemblies. That is the correct place for profile compatibility fixes.

## Dependency Position

Project references:

- `NINA.Core`

Many projects above it depend on `IProfileService` and the typed settings interfaces. That makes this project the configuration backbone of the solution.

## Contribution Notes

- Add new persisted settings here, not in ad hoc files or `Properties.Settings`.
- For a new settings section, update the interface list, concrete implementation, `Profile` default construction, `KnownType` declarations, and change-notification registration.
- Keep profile compatibility in mind; moving or renaming serialized types can require migration code.
- If a feature needs plugin-specific persisted values, prefer the existing `PluginSettings`/`PluginOptionsAccessor` path instead of inventing another storage mechanism.
