# PR: Configurable Device Connection Order

## Summary

Adds a user-configurable device connection order to NINA so that "Connect All" connects devices in a sequence the user defines, rather than a fixed hardcoded sequence.

**Motivation:** Devices such as the SV241 SVBony power switch must connect *before* the devices they power (e.g., cameras). The previous hardcoded order connected cameras first, which fails if they are unpowered at connect time.

**Related PR:** This change is complemented by a sibling connector-plugin PR in the external repository at https://github.com/ikeysolomon/nina.plugin.connector/tree/ReorderingExperiment. That plugin PR updates the connector plugin’s own `ConnectAllEquipment` implementation to honor the same custom device order.

---

## Files Changed

### `NINA.Profile/Interfaces/IApplicationSettings.cs`
**1 line added** — extends the settings interface with the new property:
```csharp
AsyncObservableCollection<string> DeviceConnectionOrder { get; set; }
```

### `NINA.Profile/ApplicationSettings.cs`
**~30 lines added** — implements the new settings:
- Private `AllDevices` constant list defining the canonical set of 11 device names.
- `[DataMember]` properties `DeviceConnectionOrder` and `UseCustomDeviceConnectionOrder`, each with a proper backing field and `RaisePropertyChanged`.
- Default initialization in `SetDefaultValues()` — Switch is placed first so power-switch users get the correct default immediately.
- Migration logic in `OnDeserialized()` — if the saved list is missing devices, they are appended so upgrading users do not lose supported device types.

### `NINA/ViewModel/ApplicationDeviceConnectionVM.cs`
**Net change: ~90 lines removed, ~25 lines added** — the core toolbar Connect All behavior is changed only here:
- Added `using System.Linq` (needed for `.ToList()`).
- When `UseCustomDeviceConnectionOrder` is enabled, `ConnectAllDevicesCommand` iterates the saved `DeviceConnectionOrder` list and connects devices in that user-defined sequence.
- When custom order is disabled, the original hardcoded default connection sequence is preserved exactly.
- Everything else in the file (disconnect logic, `DisconnectEquipment`, `Shutdown`, USB watcher, `AtLeastOneConnected`, all existing commands) is **untouched**.

### `NINA/ViewModel/OptionsVM.cs`
**~15 lines added** — two `[RelayCommand]` methods appended at the end of the existing `partial class`:
- `MoveDeviceConnectionOrderUp(string device)`
- `MoveDeviceConnectionOrderDown(string device)`

These manipulate the ordered collection in the active profile. The `OptionsVM` is the `DataContext` for `EquipmentView`, so this is the natural home for these commands.

### `NINA.Sequencer/SequenceItem/Connect/ConnectAllEquipment.cs`
**~6 lines changed** — this built-in sequencer item now reads `UseCustomDeviceConnectionOrder` and the saved `DeviceConnectionOrder` list when the option is enabled, ensuring the seq-item connect path stays consistent with the toolbar Connect All behavior.

### `NINA.Core/Locale/Locale.resx`
**10 lines added** — four new locale entries inserted alphabetically in the `D` section:
- `LblDeviceConnectionOrder` = `"Device Connection Order"`
- `LblDeviceConnectionOrderTooltip` = `"Configure the order in which devices connect when 'Connect All' is used. Use the arrow buttons to reorder."`
- `LblUseCustomDeviceConnectionOrder` = `"Use Custom Device Connection Order"`
- `LblUseCustomDeviceConnectionOrderTooltip` = `"When enabled, 'Connect All' will connect devices in the custom order defined below. When disabled, the default connection order is used."`

### `NINA/View/Options/EquipmentView.xaml`
**~45 lines added** — appended to the existing Equipment options tab:
- One additional `<RowDefinition Height="Auto" />` in the outer grid (making row index 5).
- A full-width `<GroupBox Grid.Row="5" Grid.ColumnSpan="2">` containing an `<ItemsControl>` bound to `ApplicationSettings.DeviceConnectionOrder`.
- Each item row shows the device name and ▲/▼ buttons that call `MoveDeviceConnectionOrderUpCommand` / `MoveDeviceConnectionOrderDownCommand` on `OptionsVM` via the existing `proxy` `BindingProxy` resource already declared on line 28 of that file.

---

## What Was NOT Changed

- `IApplicationDeviceConnectionVM` interface — no new public surface added there.
- `DisconnectEquipment` — disconnect order is unchanged.
- All other settings classes, view models, and views not listed above.
- The external connector plugin repository is not modified in this PR; a sibling PR exists at https://github.com/ikeysolomon/nina.plugin.connector/tree/ReorderingExperiment.

---

## Behaviour

| Scenario | Before | After |
|---|---|---|
| First run / new profile | Camera → Filter Wheel → Telescope → … → Switch (hardcoded) | Switch → Camera → Filter Wheel → … (configurable default) |
| Existing profile (upgrade) | Hardcoded order | Saved order loaded; any newly added device types appended |
| User reorders in Options → Equipment | Not possible | Drag order persisted in profile XML |
| "Connect All" button / F5 | Hardcoded | Respects saved order |

---

## Testing Notes

- Open Options → Equipment; a **Device Connection Order** group box appears at the bottom with all 11 devices listed.
- Use ▲/▼ to reorder; the order is saved in the active profile.
- Press F5 / click Connect All; check the log — devices should connect in the listed order.
- Delete or rename the profile file to simulate a fresh install; the default order (Switch first) should appear.
- Load an existing profile that predates this change; all 11 devices should appear (migration appends missing entries).
