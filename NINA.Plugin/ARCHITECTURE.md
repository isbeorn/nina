# NINA.Plugin Architecture

## Purpose

`NINA.Plugin` is the plugin contract and plugin runtime infrastructure. It provides:

- the manifest model and JSON schema used for plugin repositories
- the base class plugin authors derive from
- plugin discovery, loading, compatibility checks, installation, update, and removal
- composition of plugin-provided sequence entities, dockable view models, pluggable behaviors, and equipment providers

Build shape from `NINA.Plugin.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled

## Top-Level Structure

- Root-level runtime services
  `PluginLoader`, `PluginInstaller`, `PluginFetcher`, `PluginBase`, `PluginAssemblyReader`, `PluginCompatibility`, `PluginMergedEntityMap`, `Constants`
- `Interfaces/`
  Plugin-facing contracts such as `IPluginManifest`, `IPluginVersion`, `IPluginDescription`, `IPluginLoader`, `IMessageBroker`
- `ManifestDefinition/`
  Concrete manifest/version/install types and JSON conversion helpers
- `Messaging/`
  Message broker contract implementation support

## Plugin Metadata Model

Two metadata paths exist in the code:

- `PluginBase`
  Reads plugin identity and descriptive metadata from assembly attributes and `AssemblyMetadataAttribute` values. This is the runtime manifest for installed plugin assemblies.
- `ManifestDefinition/PluginManifest.cs`
  Defines the JSON schema used for repository manifests fetched from plugin repositories.

The fetched manifest and the installed assembly manifest are related but not identical concerns:

- repository manifests describe what can be downloaded
- assembly metadata describes what is actually loaded

## Loading Model

`PluginLoader.cs` is the core runtime component.

Important mechanics visible in the source:

- Loads core sequencer and equipment-provider types first through a `TypeCatalog`
- Scans versioned plugin directories under `%LOCALAPPDATA%\\NINA\\Plugins\\<minimum-app-version>`
- Uses a custom `AssemblyLoadContext` to resolve plugin-local dependencies
- Uses MEF (`AssemblyCatalog`, `CompositionContainer`, `[ImportMany]`) to compose:
  - `ISequenceItem`
  - `ISequenceCondition`
  - `ISequenceTrigger`
  - `ISequenceContainer`
  - `ISequenceEntityUpgrader`
  - `ResourceDictionary`
  - `IDockableVM`
  - `IPluggableBehavior`
  - `IEquipmentProvider`
- Injects a large set of application services into the MEF container, including mediators, factories, profile service, image services, sequence mediator, message broker, and symbol broker
- Merges plugin resource dictionaries into `Application.Current.Resources`
- Appends plugin metadata to in-memory registries such as `Items`, `Conditions`, `Triggers`, `Container`, `DockableVMs`, `PluggableBehaviors`, `DeviceProviders`, and `Upgraders`

`AssignSequenceEntity(...)` also applies `ExportMetadata` values (`Name`, `Description`, `Icon`, `Category`) to sequence entities at load time.

## Installation And Update Model

`PluginInstaller.cs` handles the file lifecycle:

- downloads installer payloads
- validates checksums
- supports DLL and archive installs
- stages updates into versioned staging folders
- moves uninstalls into versioned deletion folders

`Constants.cs` defines the version-scoped runtime folders:

- `Plugins`
- `PluginStaging`
- `PluginDeletion`

This version scoping is based on the assembly's `PluginMinimumApplicationVersion` metadata, not the full application revision.

## Compatibility Model

Compatibility checks happen at more than one level:

- repository fetch filters manifests using `PluginVersion.IsPluginCompatible(...)`
- loader validates minimum application version against the running assembly version
- `PluginCompatibilityMap` is consulted to reject deprecated, incompatible, or update-required plugins

## Dependency Position

This project depends on many application libraries because it composes plugin content into them:

- `NINA.Core`
- `NINA.Astrometry`
- `NINA.Equipment`
- `NINA.Image`
- `NINA.PlateSolving`
- `NINA.Profile`
- `NINA.Sequencer`
- `NINA.WPF.Base`

That is expected: the plugin layer is an integration surface, not a low-level primitive library.

## Contribution Notes

- If you add a new plugin extension point, update both the public interfaces and `PluginLoader` composition/import logic.
- New sequence entities intended for plugin discovery must use MEF export metadata consistently, because `PluginLoader` applies labels, icons, and categories from that metadata.
- Keep versioned folder behavior intact; the loader, installer, and updater all rely on the same directory conventions from `Constants.cs`.
- Avoid pushing app-specific logic into plugins directly when the plugin surface can be formalized here.
