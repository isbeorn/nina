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

## Contribution Notes

- Benchmark production code directly where possible instead of maintaining a second optimized implementation in this project.
- Keep legacy baselines self-contained and clearly named; they are comparison fixtures, not production alternatives.
- Update `README.md` when the canonical command or reference workload changes.
- Benchmark-only dependencies do not belong in NINA runtime license manifests because this project is not shipped.
