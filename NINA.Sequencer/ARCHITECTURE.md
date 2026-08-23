# NINA.Sequencer Architecture

## Purpose

`NINA.Sequencer` contains the advanced sequencing engine: the sequence tree model, runtime execution pipeline, serialization layer, target/template storage, and the expression/symbol infrastructure used by sequence entities.

Build shape from `NINA.Sequencer.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled
- References `NINA.Sequencer.Generators` as an analyzer/source generator
- Emits compiler-generated files

## Top-Level Structure

- `SequenceItem/`
  Concrete executable items grouped by feature area (`Autofocus`, `Camera`, `FilterWheel`, `Focuser`, `Guider`, `Imaging`, `Platesolving`, `Rotator`, `SafetyMonitor`, `Switch`, `Telescope`, `Utility`, `Dome`, `Connect`, `Expressions`)
- `Container/`
  Sequence containers such as `SequenceRootContainer`, `SequentialContainer`, `ParallelContainer`, `TargetAreaContainer`, `StartAreaContainer`, `EndAreaContainer`
- `Conditions/`
  Loop, altitude, sun/moon, time, and safety-monitor conditions
- `Trigger/`
  Trigger types grouped into areas such as `Autofocus`, `Connect`, `Dome`, `Guider`, `MeridianFlip`, and `Platesolving`
- `Serialization/`
  JSON creation converters and `SequenceJsonConverter`
- `Logic/`
  Expression and symbol infrastructure (`Expression`, `SymbolBroker`, `UserSymbol`, expression controls/converters)
- `View/`
  XAML/data templates for sequence UI surfaces

## Execution Model

The runtime entry point is `Sequencer.cs`:

- `Sequencer.Start(...)` validates the root container, initializes every item/condition/trigger, runs the root container, and then tears the tree down again.
- Validation is recursive and based on the `IValidatable` interface.
- Containers, conditions, and triggers are normal runtime objects, not pure data nodes.

The root node is `Container/SequenceRootContainer.cs`, which adds sequence-wide concerns such as:

- tracking currently running items
- failure events
- reset/clear behavior
- change tracking through `HasChanges`

## Entity Discovery Model

The sequence entity palette is not hard-coded in one place. Instead:

- built-in sequence entities are exported with MEF metadata (`[Export]`, `[ExportMetadata]`)
- `NINA.Plugin.PluginLoader` loads both built-in and plugin-provided entities
- `SequencerFactory` receives the final entity lists and exposes cloneable prototypes for UI/editor use

This is why sequence entities must implement clone behavior and provide metadata consistently.

## Serialization Model

`Serialization/SequenceJsonConverter.cs` is the JSON entry point.

Important characteristics:

- serializes with `TypeNameHandling.All` and `PreserveReferencesHandling.All`
- deserializes through creation converters backed by `ISequencerFactory`
- supports containers, items, conditions, triggers, and date-time providers

The deserialization flow is factory-based, so sequence entities are reconstructed from registered prototypes rather than arbitrary reflection alone.

## Target And Template Storage

Two controllers manage user-authored sequence assets:

- `TargetController`
  Watches the target folder from `ISequenceSettings.SequencerTargetsFolder`, loads `.json` target containers, and can add/delete targets.
- `TemplateController`
  Loads built-in templates from `NINA/Sequencer/Examples` and user templates from `ISequenceSettings.SequencerTemplatesFolder`, storing them as `.template.json`.

Both controllers use `SequenceJsonConverter` and `FileSystemWatcher`, so folder layout and file naming are part of the runtime contract.

## Expression And Symbol Infrastructure

The `Logic/` area is a distinct subsystem:

- `SymbolBroker` owns symbols and functions
- `SymbolController` and `SymbolFunctionController` expose live views of broker data
- `Expression` and related controls support expression-backed properties on sequence entities

This subsystem is the reason the project consumes the `NINA.Sequencer.Generators` analyzer.

NCalc is an internal expression-engine implementation detail. Public and plugin-facing symbol APIs must use NINA-owned contracts such as `ISymbolFunctionArguments`; they must not expose NCalc types. `ISymbolFunctionArguments.Evaluate(int)` intentionally preserves lazy argument evaluation, so conditional functions should evaluate only the branch they select. Keep NCalc version-specific event arguments and parameter access contained in the internal adapter.

## Dependency Position

Project references:

- `NINA.Core`
- `NINA.Astrometry`
- `NINA.Equipment`
- `NINA.Image`
- `NINA.PlateSolving`
- `NINA.Profile`
- `NINA.CustomControlLibrary`
- `NINA.WPF.Base`
- `NINA.Sequencer.Generators` as an analyzer

The project is referenced by the main app, the plugin layer, the installer, and tests. It is both a runtime engine and a plugin extension surface.

## Contribution Notes

- New sequence entities should live under the matching `SequenceItem`, `Conditions`, `Trigger`, or `Container` area and must implement cloning, parent attachment, and validation correctly.
- Add MEF export metadata (`Name`, `Description`, `Icon`, `Category`) for anything that should appear in the sequencer UI or plugin loader registries.
- Keep JSON compatibility in mind; serialization depends on the existing converters and prototype factory model.
- If you use expression-backed properties, follow the generator-based pattern already used in this project instead of hand-writing the same boilerplate.
- Generated expression properties release their symbol consumers automatically. A hand-written `Expression` owner must release the previous value when replacing it and override `ReleaseExpressionConsumers()` so detaching its sequence graph releases the current value.
