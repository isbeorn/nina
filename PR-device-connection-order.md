# PR: Configurable Device Connection Order

## Summary

Adds a user-configurable device connection order to NINA so that "Connect All" connects devices in a sequence the user defines, rather than a fixed hardcoded sequence.

**Motivation:** Devices such as the SV241 SVBony power switch must connect *before* the devices they power (e.g., cameras). The previous hardcoded order connected cameras first, which fails if they are unpowered at connect time.

---

## Files Changed

### `NINA.Profile/Interfaces/IApplicationSettings.cs`
**1 line added** — extends the settings interface with the new property:
```csharp
AsyncObservableCollection<string> DeviceConnectionOrder { get; set; }
```

### `NINA.Profile/ApplicationSettings.cs`
**~30 lines added** — implements the new property:
- Private `AllDevices` constant list defining the canonical set of 11 device names.
- `[DataMember]` property `DeviceConnectionOrder` with backing field and `RaisePropertyChanged`.
- Default initialisation in `SetDefaultValues()` — Switch is placed first so power-switch users get the correct default immediately.
- Migration logic in `OnDeserialized()` — any device names missing from an existing saved profile are appended, so upgrading users never lose a device from the list.

### `NINA/ViewModel/ApplicationDeviceConnectionVM.cs`
**Net change: ~90 lines removed, ~25 lines added** — the only modification to this pre-existing file:
- Added `using System.Linq` (needed for `.ToList()`).
- Replaced the hardcoded 11-block `try/catch` connect sequence inside `ConnectAllDevicesCommand` with a loop over `profileService.ActiveProfile.ApplicationSettings.DeviceConnectionOrder`.
- Added private helper `GetMediatorConnectPair(string)` that maps a device name to its mediator connect delegates (mirrors the pattern already used in `ConnectAllEquipment.cs` in the sequencer).
- Everything else in the file (disconnect logic, `DisconnectEquipment`, `Shutdown`, USB watcher, `AtLeastOneConnected`, all existing commands) is **untouched**.

### `NINA/ViewModel/OptionsVM.cs`
**~15 lines added** — two `[RelayCommand]` methods appended at the end of the existing `partial class`:
- `MoveDeviceConnectionOrderUp(string device)`
- `MoveDeviceConnectionOrderDown(string device)`

These manipulate the ordered collection in the active profile. The `OptionsVM` is the `DataContext` for `EquipmentView`, so this is the natural home for these commands.

### `NINA.Core/Locale/Locale.resx`
**6 lines added** — two new locale entries inserted alphabetically in the `D` section:
- `LblDeviceConnectionOrder` = `"Device Connection Order"`
- `LblDeviceConnectionOrderTooltip` = `"Configure the order in which devices connect when 'Connect All' is used. Use the arrow buttons to reorder."`

### `NINA/View/Options/EquipmentView.xaml`
**~45 lines added** — appended to the existing Equipment options tab:
- One additional `<RowDefinition Height="Auto" />` in the outer grid (making row index 5).
- A full-width `<GroupBox Grid.Row="5" Grid.ColumnSpan="2">` containing an `<ItemsControl>` bound to `ApplicationSettings.DeviceConnectionOrder`.
- Each item row shows the device name and ▲/▼ buttons that call `MoveDeviceConnectionOrderUpCommand` / `MoveDeviceConnectionOrderDownCommand` on `OptionsVM` via the existing `proxy` `BindingProxy` resource already declared on line 28 of that file.

---

## What Was NOT Changed

- `IApplicationDeviceConnectionVM` interface — no new public surface added there.
- `DisconnectEquipment` — disconnect order is unchanged.
- All other settings classes, view models, and views.
- The sequencer `ConnectAllEquipment` item — it already had its own independent ordering; this change does not affect it.

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
