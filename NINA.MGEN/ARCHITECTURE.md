# NINA.MGEN Architecture

## Purpose

`NINA.MGEN` is the standalone library for Lacerta MGEN guider hardware. It abstracts both MGEN2 and MGEN3 behind a common `IMGEN` interface.

Build shape from `NINA.MGEN.csproj`:

- Target framework: `netstandard2.0`
- Output type: `Library`

## Top-Level Structure

- Root-level shared contracts and value types
  `IMGEN`, `ImagingParameter`, `GuideState`, `CalibrationStatus`, `StarData`, `DitherAmplitude`, `LEDState`, `FrameInfo`
- `MGEN2/`
  Command-based implementation for MGEN2, including the protocol reference PDF and the concrete `MGEN` class
- `MGEN3/`
  Vendor-SDK-based implementation for MGEN3 (`MGEN3`, `MG3SDK`, `IMG3SDK`, `LoggingMG3SDK`)
- `FTD2XX/`
  FTDI transport helpers
- `Exceptions/`
  Device/protocol-specific exceptions
- `DllLoader.cs`
  Architecture-aware native DLL loading helper

## Interface Boundary

`IMGEN.cs` defines the operations the rest of the system depends on:

- device discovery/open/close
- camera start, guide-star search, calibration, guiding, and dithering
- guide-state polling
- display and LED reading
- parameter read/write

`NINA.Equipment` consumes this interface and turns it into a guider implementation (`MGENGuider`). The UI and sequencing code do not talk to the transport details directly.

## MGEN2 And MGEN3 Split

The two hardware generations are implemented differently:

- `MGEN2/MGEN2.cs`
  Talks to the device through FTDI and explicit command/response classes. It manages timing (`minimumCommandInterval`), locking, button commands, display reads, calibration, and guide-state queries itself.
- `MGEN3/MGEN3.cs`
  Wraps the vendor SDK through `IMG3SDK`/`MG3SDK`, then adds logging and higher-level convenience behavior on top.

The shared `IMGEN` interface is what keeps the rest of the solution insulated from those differences.

## Native Dependency Handling

`DllLoader.cs` loads native DLLs from:

- `External/x86/...`
- `External/x64/...`

based on process architecture. The library locates those DLLs relative to `AppDomain.CurrentDomain.BaseDirectory`, so the executable/runtime packaging project must ship them in the expected layout.

## Dependency Position

This project intentionally has no `NINA.*` project references. It is consumed by:

- `NINA.Equipment`
- `NINA`
- `NINA.Test`
- `NINA.WPF.Base`

That makes it a transport/protocol library, not a feature-composition layer.

## Contribution Notes

- Keep protocol and vendor-SDK details inside this project.
- Maintain the `IMGEN` contract when adding new device capabilities; higher layers already depend on it.
- Avoid adding WPF or profile-setting dependencies here. Existing code keeps this project portable and reusable.
- When adding or renaming native DLL dependencies, coordinate with the runtime packaging in `NINA` and the installers.
