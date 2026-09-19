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

## Editor History

`Editing/` owns an in-memory journal for configuration edits. `Sequence2VM` owns its lifetime and replaces it only when the sequence root changes. Runs and view switching preserve the journal. Saving marks a position without overriding the existing `HasChanges` rules. The editor exposes at most 100 transactions plus their baseline state. Undo and Redo live in the bottom action bar. `SequenceSidebar` hosts `SequenceEditHistoryView` in a history tab, using the inherited editor context so it follows root and template-session changes. Built-in entries capture immutable display details: the item path and position, accepted before/after field values and structural source/destination positions. The collapsed row shows a compact location and a separate position or value change; full paths remain in an independent Details expander and tooltip. Expanding details must not navigate history. Structural command boundaries carry the moved entity and operation so captions name the actual action and omit displaced neighbors. Duplicate short container captions use ancestor context, while equal sibling names include their positions. Caption formatting never runs during replay. Field captions follow the editor's existing `IImmutableContainer` convention: the outermost non-root compound instruction supplies the visible name and position, including when implementation children are nested inside mutable containers. Capture and replay still address the actual child configuration. Ordinary containers and the sequence root do not collapse their children into one caption. Caption resolution follows only the parent chain and immediate parent list, without taking a sequence snapshot. History captions reuse dropdown display members and existing enum/entity labels, with readable type names as a fallback. Namespace removal never rewrites literal user text such as expressions or file paths. Custom edits retain their existing description-only contract.

The journal uses one observable collection of retained states, including its baseline. `SequenceEditEntry` keeps edit metadata immutable and updates position/current/saved markers in place, so typing and replay do not rebuild the sidebar. History navigation resolves the selected entry after flushing the field, because that commit can evict an older state. Temporary template-session routing and subscriptions live in `SequenceEditHistory.Templates.cs`; the core journal owns transactions, branching, eviction and replay.

`SequenceEditBehavior` routes WPF input gestures. `SequenceEditBindingResolver` resolves the original binding and owning session. A `PendingSequenceEdit` owns the gesture capture, popup subscriptions and wheel timer and disposes them together. It does not replace bindings or subscribe to model notifications. Text commits at focus departure, Enter or a sequence command. Dropdowns commit after closing and numeric gestures commit at release. Capture checks the visual DataContext first, then the resolved sequence entity or expression owner. Each candidate must belong to the history session and contain the edited configuration object, or explicitly accept it through `ISequenceCustomPropertyEditProvider`. This covers compound editors such as Smart Exposure whose filter and dither fields belong to child entities. Symbol definitions use the owning symbol, not its evaluation scope. Property capture searches configuration objects of that entity only; it never serializes or traverses the sequence tree. Multi-bindings participate when exactly one input writes to the model and the remaining inputs only provide display context, as in camera gain dropdowns. Binding modes and inherited defaults are respected. Runtime controls can inherit `SequenceEditContext.IsRecordingEnabled="False"`.

Structural commands capture list placements around their synchronous model update. Core commands mark their recording boundary; host buttons use explicit `SequenceEditOperation` metadata to wrap plugin overrides only. Host drop targets opt in through `RecordSequenceStructure`, without command-name inference. `CaptureEdit` supplies one nesting and transaction path for structure, target, property and enable/disable actions. History retains the actual entities, uses existing remove/attach paths and resolves nested property owners again during replay because attachment can replace expressions. Runtime linked-template contents are excluded. Explicit contents editing uses `ISequenceEditSessionBoundary` and has a separate temporary journal, disposed on save, cancel or removal. The boundary instance itself belongs to its parent's history. Structural replay also refreshes ownership and disposes sessions that leave the graph. Asynchronous editor operations must start capture after external work finishes, immediately around their model update.

Ordinary scalar and expression-definition fields need no history interface. Entities override capture only for coupled setters, custom mutable values or runtime exclusions through `ISequenceCustomPropertyEditProvider`. The binding recorder does not reference concrete condition or instruction types. Providers supply immutable `ISequenceEditSnapshot` values; the shared replay operation handles conflict checks and compensation for both ordinary properties and provider snapshots. Coordinate snapshots capture the entire coordinate value, epoch and sign. Equatorial instructions, altitude/horizon conditions and Alt/Az instructions restore their expression definitions without allowing component notifications to replace them with numeric literals. Alt/Az replay keeps the current observer location. Generated expression-backed scalar properties journal the definition rather than the current evaluated number. Re-evaluation uses current runtime inputs. Selecting a time provider captures the previous manual clock fields; provider-calculated times remain live.

Control discovery includes dependency properties supplied by templates and styles, with descriptors cached per control type. Composite text editors flush their inner text binding before their model binding. Expression stepper buttons use an explicit command adapter because they write definitions directly. `SequenceEditorGraph` is the single description used by structural capture and owner discovery. It discovers standard collections through interfaces and other children through optional `ISequenceEditorChildProvider`. List descriptors supply their own mutation paths, including replaceable slots. `SequenceTrigger` describes its runner and `CustomTrigger` its source. `ISequenceTriggerEditor` remains supported for additional fixed action sets such as programmable meridian flip. The graph indexes only non-parent ownership edges at session construction and after structural changes. Ordinary field gestures follow parent or indexed ownership edges without traversing the sequence tree. External structural additions trigger a refresh only when the cached path cannot resolve the owner.

`CoreEditorHistoryTest` discovers every built-in entity export, loads the compiled templates and exercises accepted model changes through routed WPF input, undo and redo. It embeds the coverage inventory and asserts the exact export and field lists, so missing bindings cannot silently lower coverage. The catalog commits through focus departure rather than calling the capture helper. It supplements the journal, binding, structure and lifecycle tests. See [the core editor coverage inventory](Editing/UI-COVERAGE.md) when adding or changing an input. New compound controls and command handlers need their own replay checks; a writable-looking binding alone does not prove that all setter side effects are captured.

Ordinary structural edits need no attachment provider. Structural transactions ask `ISequenceAttachmentStateProvider.CaptureAttachmentState()` only for configuration affected by parent hooks: explicit coordinate definitions and rotation when entering/leaving a target context, and symbol identifiers cleared by a scope collision. Restore those values after the normal attachment hooks. Inherited coordinates and current variable values remain live.

Replay checks its expected values or placements and does not move the cursor on conflict. Composites apply in order and undo in reverse order, compensating completed operations if a later operation fails. Property and structural operations share snapshot replay and compensation. A verified rollback preserves the history and cursor for retry. Unverified state or an opaque custom replay failure invalidates history and notifies the user. Undo does not call the runner, reset progress or attempt to reverse equipment actions. Enabled intent uses the existing CREATED/DISABLED semantics, not a saved execution status.

The additive public contracts `ISequenceEditHistory` and `ISequenceEdit` support custom plugin editors. Obtain the context with `SequenceEditContext.GetHistory(editorElement)`, begin a transaction before the synchronous mutation and register an already-applied edit. Implement applicability checks and compensate a failed operation before throwing. Do not retain the context beyond the editing session. No existing entity, mediator, constructor, MEF or JSON contract changes are required.

Automatic plugin coverage includes writable scalar bindings, nested JSON configuration objects and host structural commands. Optional property and attachment snapshot providers let plugins participate in the same path as core entities without a type registry or changes to existing interfaces. Opaque commands, collection editors, converters that write multiple independent values and mutable custom values require explicit integration. Suppress automatic recording on such an editor to avoid duplicate entries. This is compatibility with existing plugins, not universal undo coverage. See [the integration example](Editing/README.md).

Run `NINA.Test.Sequencer.Editing` for journal, binding, live execution, resource retention and lifecycle checks, then the sequencer and plugin regression suites. UI tests run STA and nonparallel. Smart Exposure binding regressions instantiate its real template and the standalone Switch Filter template to cover nested ownership and editable dropdown gestures. The 1,000-entity test runs 100 routed WPF field gestures, undo and redo for ordinary and trigger-owned fields. After initial graph indexing it asserts zero root snapshot reads and reports editing/replay time.

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
