# Custom editor integration

Existing plugins keep their existing binary and JSON contracts. Standard scalar bindings and the host's structural commands participate automatically when their entity belongs to the current advanced sequence. Multi-bindings with one writable input and read-only display context also participate, including camera gain dropdowns. Nested configuration must be reachable through public JSON configuration properties. Evaluated expression results, progress and current variable values are excluded.

Most entities need no history interface. Choose an extension only for behavior that automatic capture cannot restore:

| Editor behavior | Integration |
| --- | --- |
| Ordinary scalar or expression-definition field | None; automatic binding capture handles it |
| One bound field changes several configuration values, or needs a custom copy of a mutable value | `ISequenceCustomPropertyEditProvider` |
| Parent hooks change configuration as well as the entity's placement | `ISequenceAttachmentStateProvider` |
| A trigger owns additional fixed action containers | `ISequenceTriggerEditor` |
| An entity owns other editable child collections or replaceable child slots | `ISequenceEditorChildProvider` |
| A container separates a generated preview from temporary contents editing | `ISequenceEditSessionBoundary` |
| A custom command or editor applies changes outside those binding paths | Register the applied edit through `ISequenceEditHistory` |

Template/style bindings on composite text controls participate too. The recorder discovers standard editor dependency properties on the control type and commits the inner text binding before the outer model binding. This does not make arbitrary command buttons or multi-property setters automatically reversible. The built-in coverage and deliberate exclusions are listed in [UI-COVERAGE.md](UI-COVERAGE.md).

For a bound field whose setter changes several values, the entity can implement `ISequenceCustomPropertyEditProvider`. This is an optional capability, separate from all existing sequence interfaces. `TryCapturePropertyState(source, propertyName, out snapshot)` receives the actual binding source and member before and after the gesture:

- Return `false` for ordinary fields to keep automatic capture.
- Return `true` with an `ISequenceEditSnapshot` for coupled or custom mutable configuration.
- Return `true` with `null` to exclude a runtime-only field.

The snapshot retains immutable copies of only the affected configuration. `Description` provides a compact localized value for the sidebar, or null. `IsCurrent` compares the live configuration with that snapshot. `Restore()` applies all affected values through normal editing paths. Resolve nested objects from the entity each time; parent hooks can replace them. The journal supplies grouping, no-op detection, parent checks, conflict handling, binding refresh and compensation using the opposite snapshot. A failed replay retains its cursor and history when compensation is verified against that snapshot. Unverified state invalidates history and reports the failure. The provider must support capture both before and after the edit and must not execute instructions or capture unrelated progress.

Entities whose parent hooks change configuration can also implement `ISequenceAttachmentStateProvider.CaptureAttachmentState()`. Return the same kind of snapshot, or null if there is no affected configuration. Structural replay restores these values after the normal attachment hooks. Core coordinate entities, symbols and time-provider editors use these capabilities themselves; the journal does not select adapters by concrete instruction or condition type. Existing plugins do not need to adopt either capability for ordinary scalar bindings or host-provided structural actions.

A trigger with additional editable action containers can implement `ISequenceTriggerEditor.GetAdditionalEditorContainers()`. Return the existing containers beyond the standard `TriggerRunner`, not execution-generated copies. This makes their fields and host structural actions discoverable even when their execution parent differs from their editor owner, and preserves temporary linked-template history routing.

Other editor-owned children can be exposed through `ISequenceEditorChildProvider.GetEditorChildLists()`. Each `ISequenceEditorChildList` has a stable name unique to its owner, including the automatically discovered `Items`, `Conditions` and `Triggers`. `Read()` must resolve the current children. For mutable collections or single-child slots, `Insert`, `Remove` and `Move` use the same hooks as manual editing. Set `IsReadOnly` for fixed ownership lists whose child containers can still be edited. The base `SequenceTrigger` supplies its runner automatically; an override must also return its base lists. One shared graph uses these declarations for field ownership, session routing and structural replay.

`ISequenceEditSessionBoundary` is only needed when a container has read-only generated contents and an explicit temporary editing mode. The instance's settings and placement belong to its parent's history. Its contents use a separate journal while `IsEditing` is true. Notify `PropertyChanged` when that mode ends. Saving, cancelling or removing the instance disposes the temporary journal; restoring a removed instance starts a fresh contents session.

Host buttons declare `SequenceEditContext.Operation` alongside `SequenceEditContext.Command`. Host structural drop targets set `DropIntoBehavior.RecordSequenceStructure="True"`. These declarations capture unmodified plugin command overrides without inferring behavior from command member names. Core commands already own their recording boundary, so the host does not record them twice. Custom editor actions can use the same declarations or register their own edits.

Use the editor history context for an opaque command, a collection editor or a converter that writes several independent properties outside single-field capture. Capture immutable before/after values yourself. Register only effective configuration changes, after applying them, on the editor dispatcher. Keep file writes, device actions and background refreshes outside the transaction. For async work, await the external work first.

```csharp
ISequenceEditHistory history = SequenceEditContext.GetHistory(editor);
using (history?.BeginTransaction("Change custom setting")) {
    string before = item.CustomSetting;
    item.CustomSetting = acceptedValue;
    if (before != item.CustomSetting) {
        history?.RecordApplied(new CustomSettingEdit(item, before, item.CustomSetting));
    }
}
```

`CustomSettingEdit` implements `ISequenceEdit`:

- `Description` is a user-facing, localized label.
- `CanUndo` checks the affected entity is still in its expected place and the current setting equals the recorded after value.
- `CanRedo` checks placement and the before value.
- `Undo` restores the before value through the normal setter.
- `Redo` restores the after value through the normal setter.
- A throwing replay operation must compensate its own partial changes before throwing. The journal compensates earlier operations in the same transaction. It invalidates history on unexpected failures.

Leave automatic recording enabled when using a snapshot provider. Set `editing:SequenceEditContext.IsRecordingEnabled="False"` only on a custom editor which registers its own edits through `ISequenceEditHistory`. This prevents its child controls from recording the same gesture twice. Obtain the context afresh for each action, because root replacement and template editing change the session. It resolves the editor's owning entity, independently of the previously focused field or toolbar session. A null context means the view is outside a tracked editor; its command should retain its previous behavior.

History is a bounded editing aid, not a persistent audit log. An edit after undo discards the redo branch. Deleted objects remain alive while referenced by history, then are released when their entries are evicted or the session ends. A plugin remains responsible for its existing attach/detach hooks, subscriptions and expression consumer cleanup.
