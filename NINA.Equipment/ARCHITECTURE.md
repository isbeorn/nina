# NINA.Equipment Architecture

## Purpose

`NINA.Equipment` is the hardware integration layer. It defines device abstractions and implements concrete adapters for cameras, mounts, focusers, guiders, domes, switches, weather devices, GNSS sources, planetarium integrations, and vendor SDK wrappers.

Build shape from `NINA.Equipment.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled

## Top-Level Structure

- `Interfaces/`
  Device contracts such as `ICamera`, `ITelescope`, `IFocuser`, `IGuider`, `IDome`, `ISwitchHub`, `IWeatherData`, plus factories like `IGnssFactory` and `IPlanetariumFactory`.
- `Equipment/`
  Concrete implementations grouped by device category:
  - `MyCamera`
  - `MyTelescope`
  - `MyFocuser`
  - `MyFilterWheel`
  - `MyGuider`
  - `MyDome`
  - `MySwitch`
  - `MyFlatDevice`
  - `MyWeatherData`
  - `MyGPS`
  - `MyPlanetarium`
  - `MyRotator`
  - `MySafetyMonitor`
- `SDK/`
  Native/vendor interop wrappers for camera, focuser, filter wheel, and flat device SDKs.
- `Utility/`
  Discovery and integration helpers such as `ASCOMInteraction` and `AlpacaInteraction`.
- `Exceptions/` and `Model/`
  Equipment-specific support types.

## Implementation Pattern

The project follows a clear pattern:

- interfaces define the device surface
- category folders contain concrete implementations
- discovery helpers instantiate those implementations
- SDK folders isolate vendor-specific interop

Examples from the code:

- `Utility/ASCOMInteraction.cs`
  Enumerates installed ASCOM drivers and creates `AscomCamera`, `AscomTelescope`, `AscomFilterWheel`, `AscomFocuser`, and related wrappers.
- `Utility/AlpacaInteraction.cs`
  Discovers network devices through `AlpacaDiscovery.GetAscomDevicesAsync(...)`, wraps them in the same adapter types, and also exposes direct Alpaca clients such as `AlpacaDirectCamera`.
- `Equipment/MyCamera/GenericCamera.cs`
  Provides a reusable `ICamera` implementation on top of an `IGenericCameraSDK`.
- `Equipment/MyCamera/FileCamera.cs`
  Provides a non-hardware camera implementation backed by files on disk.
- `Equipment/MyGuider/MGENGuider.cs`
  Adapts the `NINA.MGEN.IMGEN` library into the guider abstraction.
- `Equipment/MyPlanetarium/PlanetariumFactory.cs` and `Equipment/MyGPS/GnssFactory.cs`
  Select concrete external integrations from profile settings.

## ASCOM, Alpaca, And Native SDKs

The code supports multiple backends in parallel:

- ASCOM COM drivers through `ASCOMInteraction`
- Alpaca discovery and direct network clients through `AlpacaInteraction`
- native vendor SDKs under `SDK/*`
- file-based or built-in utility devices like `FileCamera`

The `Equipment/AscomDevice.cs` base class is the shared adapter foundation for many ASCOM/Alpaca implementations.

## Equipment Hubs

The built-in safety, switch and weather hubs expose the existing device interfaces. Shared session handling lives under `Equipment/Hubs`; category adapters own aggregation policy. Safety and switch require all children, while weather connects only explicitly mapped sources and preserves healthy readings when another source fails. Hub configuration is fixed for a connection session. Invalid configurations are rejected before connecting any child and report a localized notification with setup guidance, including when no child rows exist. Rejected initial sessions also report child connection errors before rollback clears cached state; cancellation remains silent.

`IEquipmentHub` exposes cached child statuses and a connection refresh operation used by the existing equipment polling workers. Connected status requires a successful connection owned by the current session; rejected children remain disconnected even when native cleanup fails. Safety evaluations update individual `IsSafe` values from the same readings used for the aggregate result and continue evaluating healthy children after another child disconnects or throws. Full teardown clears cached runtime states. An established faulted session remains active until explicit disconnect.

Composite switch channels carry the original device and channel IDs through `ICompositeSwitchIdentity`. Their aggregate IDs and display names are separate from that identity. Visible labels use the child's configured source number and preserve native channel text. `ExpressionName` qualifies native channel names with the source number and one-based channel number (`native ID + 1`), omitting the driver name; identity implementations that omit it default to their normal switch name. A failed switch session blocks writes until reconnect and previously issued channel objects remain invalid after reconnect. Bound target-value edits validate cached session state only; worker polling and writes retain live connection checks. Identity-based writes are exposed directly by `ISwitchMediator` and `ISwitchVM`. Their default implementations return a localized `NotSupportedException`, allowing existing implementations to omit the operation. The built-in mediator forwards to its handler and the switch VM resolves either the child device identity or an ordinary driver ID with its native channel ID before writing. Index-based `SetSwitchValue` is obsolete but retained for compatibility. `NotSupportedException` from the identity API signals unavailable support; the built-in VM wraps driver failures so they cannot trigger a second write through a legacy fallback.

## Special Integration: SBIG Camera Service

This project also contains the SBIG-specific camera service bridge:

- `Equipment/MyCamera/SBIGCamera.cs`
- `Equipment/MyCamera/SBIGCameraASCOMService.cs`

That code uses the protobuf/gRPC camera contract generated in `NINA.Core` plus `GrpcDotNetNamedPipes`. This is an exception to the otherwise simple adapter model and is worth preserving as equipment-layer code rather than moving into UI projects.

## Dependency Position

Project references:

- `NINA.Core`
- `NINA.Astrometry`
- `NINA.Image`
- `NINA.MGEN`
- `NINA.Profile`
- `nikoncswrapper`

That dependency set is consistent with the code:

- shared models/utilities from `NINA.Core`
- coordinate/math types from `NINA.Astrometry`
- exposure/image conversion types from `NINA.Image`
- guider transport for MGEN from `NINA.MGEN`
- runtime settings from `NINA.Profile`

There is no dependency on the main app shell or plugin loader.

## Contribution Notes

- Put new device interfaces in `Interfaces/` before adding implementations.
- Keep vendor DLL interop and P/Invoke code under `SDK/` or device-specific implementation folders.
- Use the existing discovery helpers for ASCOM/Alpaca-backed devices instead of inventing new scanning paths.
- UI-facing mediators and view models do not belong here; those live in `NINA.WPF.Base` and `NINA`.
- If a device needs persisted settings, wire it through `IProfileService` rather than storing state inside the adapter alone.
