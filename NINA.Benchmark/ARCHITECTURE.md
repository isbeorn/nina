# NINA.Benchmark Architecture

## Purpose

`NINA.Benchmark` is the solution's BenchmarkDotNet executable for repeatable performance checks. It is developer tooling and is not referenced by, copied into, or packaged with the NINA application.

Build shape:

- Target framework: `net10.0-windows`
- Output type: executable
- Platform target: `x64`
- WPF enabled for measuring production render paths

## Sky-Map Benchmarks

`SkyMapRenderingBenchmark` owns the offline framing map comparison. Its synthetic catalogue cardinalities follow the order of magnitude of NINA's seed data and keep the legacy baseline beside the current implementation so benchmark drift remains visible.

- `LegacyFullFrame` covers the prior catalogue scan, mutable annotations, GDI drawing, and copied `BitmapSource` output.
- `NewFullFrame` covers scene construction and the reusable WPF render surface.
- `NewSceneOnly` and the four `*Layer` cases isolate scene calculation.
- `NewRasterOnly` isolates final image generation.
- `NewAltAzDragFramePreparation` measures scene, cached-image, camera-placement and raster preparation for an alternating drag frame.
- `NewAltAzSoftwareDragPreviewPreparation` forces WPF software rendering and measures preparation of the 50% scratch frame plus publication into the full-viewport interaction surface used by the live UI.
- `NewAltAzSoftwareDragPreviewMaterialized` additionally measures binding and synchronous presentation into a reusable full-viewport render target.
- `NewAltAzSoftwarePresentationOnly` isolates the synthetic full-viewport `RenderTargetBitmap` readback from live frame preparation.
- `NewAltAzSoftwareFinalFrameMaterialized` forces WPF software rendering and measures the full-quality frame emitted when an interaction ends.

Run benchmarks in Release mode and treat results as machine-specific comparisons. Keep functional and numerical assertions in `NINA.Test`; benchmarks prove cost, not correctness.

## Target Crossing Prediction

`TargetPredictionColdBenchmark` and `TargetPredictionUpdateBenchmark` compare the production prediction adapter with the preserved `LegacyTargetPrediction` implementation. They separate cold calculations, cache hits and repeated updates. Workloads are fixed independently of the solver; setup uses isolated EOP data and the production x64 native libraries.

`HorizonFilePredictionBenchmark` accepts optional horizon files and locates reference crossings through the full coordinate transform. Input loading and reference searches are outside measurements. Personal inputs and results stay in ignored artifacts.

See [README.md](README.md#target-crossing-prediction) for commands, measurement scope, performance targets and results. Unresolved predictions must not count as successful optimizations.

## Published Application Startup

`Measure-Startup.ps1` separately benchmarks published applications. Its `StartupProbe` project is excluded from the BenchmarkDotNet executable and loaded only through `DOTNET_STARTUP_HOOKS` in the child process. It redirects NINA's existing storage seam to an isolated directory and substitutes an in-memory settings provider. It records the first `MainWindow.ContentRendered` event, process CPU, JIT counters and memory, then publishes an atomic result so the runner can stop the test process without waiting for native SDK shutdown. It does not add hooks to production startup code. See `README.md` for the controlled workload and limitations.

## Contribution Notes

- Benchmark production code directly where possible instead of maintaining a second optimized implementation in this project.
- Keep legacy baselines self-contained and clearly named; they are comparison fixtures, not production alternatives.
- Update `README.md` when the canonical command or reference workload changes.
- Benchmark-only dependencies do not belong in NINA runtime license manifests because this project is not shipped.
