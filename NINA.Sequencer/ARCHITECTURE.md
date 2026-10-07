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

`LinkedTemplateContainer` reserves its subtree before resolving or executing it. An existing editor delays execution; new editors cannot open during the reservation. Save and Cancel finish persistence/restoration and editor-history cleanup before waking execution. Parent editors cannot close while descendant editors remain open. Startup resolution preserves these subtrees and queued template refreshes recheck whether the sequencer is running.

The wait uses a five-minute monotonic inactivity deadline. Deliberate routed input within the blocking editor resets it, including input in nested editors; focus, pointer movement and background updates do not. Timeout restores saved templates from the innermost open editor outward and warns once. Save has priority once accepted, failed Save restarts the deadline and Stop preserves edits. Restoration failure goes through normal sequencer failure handling without executing unfinished content. Each edit session owns one completion outcome, published after cleanup or on restoration failure. Property notifications only refresh the UI; the countdown is derived from its activity timestamp. The handoff uses the existing structural-edit lock without changing execution strategies or serialized state.

## Expression And Symbol Infrastructure

The `Logic/` area is a distinct subsystem:

- `SymbolBroker` owns symbols and functions
- `SymbolController` and `SymbolFunctionController` expose live views of broker data
- `Expression` and related controls support expression-backed properties on sequence entities

This subsystem is the reason the project consumes the `NINA.Sequencer.Generators` analyzer.

New expression entities should use `[UsesExpressions(GenerateValidation = true)]`. The generator owns the expression list and existing `IValidatable` implementation; optional preparation and domain-validation hooks retain entity-specific rules. Every `[UsesExpressions]` class also receives a private `ValidateOwnExpressions(issues)` helper for its declared expressions. Containers call it from handwritten validation while keeping child validation and its ordering explicit. Read generated scalar properties when executing or checking conditions so live values are evaluated. Proxy attributes synchronize valid values into stored data. Generated-validation entities already bind expression context during creation, replacement and cloning. Attachment/watchdog ordering remains explicit. See [the generator contract](../NINA.Sequencer.Generators/ARCHITECTURE.md) for diagnostics, hooks and the five container exceptions enforced by tests.

NCalc is an internal expression-engine implementation detail. Public and plugin-facing symbol APIs must use NINA-owned contracts such as `ISymbolFunctionArguments`; they must not expose NCalc types. `ISymbolFunctionArguments.Evaluate(int)` intentionally preserves lazy argument evaluation, so conditional functions should evaluate only the branch they select. Keep NCalc version-specific event arguments and parameter access contained in the internal adapter.

Qualified symbols and functions prefer `Provider.Member`; `Provider_Member` remains supported for legacy expressions and plugins. Exact-name lookup still takes precedence. Otherwise the first dot separates provider and member, or the first underscore when no dot exists, preserving each resolver's existing fallback and case behavior. Dotted references allow underscores in both segments, such as `My_Plugin.Sensor_Value`. The public `SymbolBroker.DELIMITER` and event `QualifiedKey` remain underscore-based for compatibility.

`NCalcExpressionAdapter` handles bare dotted names at both expression parsing entry points. It parses temporary identifiers without NCalc's shared cache, then restores the original names in the private expression tree. Expression definitions, references and function callbacks keep the original spelling; NINA still caches the resulting expression instance. Literal text and escaped identifiers are not rewritten.

Releasing an expression's symbol consumers must also discard its cached parameter inputs under the same lock. The next evaluation then resolves symbols in the current scope instead of accepting values from a former scope. Release itself does not evaluate or invoke value-change validators. Single-reference invalidation preserves the other references and their inputs.

Symbols must unregister when an ancestor loses its sequence root and register again when that ancestor returns. Remove empty symbol-cache scopes as well, since the static cache otherwise retains their containers. Replacing a sequence root must detach its items, conditions and triggers so their watchdogs and expression consumers are released. The lifetime tests keep services, live clones and models alive while checking that deleted entities and unloaded editors can be collected.

Cleanup must tolerate repeated parent notifications and symbols whose expression has been cleared. Never-rooted global definitions retain their existing registration behavior, but a removed global must stay unregistered until reattached.

Template and target loaders explicitly release registrations on detached library previews. `ReleaseExpressionConsumers()` also releases a detached symbol's own cache registration, without changing identifiers or removing the graph's children. Root attachment registers the symbols again. Library reload tests keep the controllers and resolver alive while checking collection of the previous graphs. The broker, advanced sequencer view model and sidebar controllers themselves live for the application session.

Sequence and block initialization belong inside their cleanup `try/finally`, including partial initialization failures. Runtime triggers must release external event subscriptions when any ancestor loses its root, even if their immediate parent remains present or has a running status. Initialization and teardown must tolerate repeated calls and root changes without duplicate subscriptions.

InputTarget coordinate subscriptions in deep-sky and linked-template containers use a thread-independent weak listener. Construction, cloning and target replacement can run on library or execution workers, so these subscriptions must not depend on WPF's thread-local weak-event table or synchronously dispatch to the UI. Replacing or clearing a target must remove the old handler while an externally retained target must not keep its former container alive.

## Editor History

`Editing/` owns an in-memory journal for configuration edits, with lifetime managed by `Sequence2VM`. It records field and structural changes through the WPF editor and preserves current runtime inputs during replay. Undo does not reverse equipment actions.

See [editor history architecture](Editing/ARCHITECTURE.md) for capture, ownership, replay, plugin contracts and integration tests. The [plugin integration example](Editing/README.md) and [core editor coverage inventory](Editing/UI-COVERAGE.md) cover extension usage and template coverage.

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
