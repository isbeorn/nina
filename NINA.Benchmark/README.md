# NINA benchmarks

Run the offline sky-map comparison from the repository root in Release mode:

```powershell
dotnet run --project NINA.Benchmark\NINA.Benchmark.csproj -c Release -- --filter *SkyMapRenderingBenchmark*
```

`LegacyFullFrame` models the previous whole-catalogue scan, retained mutable annotations, GDI raster, and full WPF bitmap-copy path. `NewFullFrame` builds one viewport scene and draws it into a reusable WPF surface. The synthetic catalogue sizes mirror the order of magnitude of NINA's checked-in seed data.

Use `--filter *Layer*` to measure constellation/star, DSO, boundary, equatorial-grid, Alt/Az-grid, horizon scene generation, the orientation of 32 cached images and 32 changing camera-rectangle placements individually. `NewRasterOnly` isolates final image generation while `NewAltAzHorizonFullFrame` measures the complete time-dependent view. `NewAltAzDragFramePreparation` measures the normal rendering path's scene generation, cached-image projection and composition, camera-overlay projection, raster generation, grid and horizon. `NewAltAzSoftwareDragPreviewPreparation` forces WPF software rendering and measures the live interaction path that renders a 50% scratch frame and publishes a full-viewport preview surface. `NewAltAzSoftwareDragPreviewMaterialized` additionally forces a synchronous 1200x800 `RenderTargetBitmap` readback. `NewAltAzSoftwarePresentationOnly` isolates that diagnostic readback from frame preparation. `NewAltAzSoftwareFinalFrameMaterialized` measures the full-quality software frame emitted at the end of a drag. Images, bindings and visuals are constructed outside the measured operation.

Reference result from the stable job on an AMD Ryzen 9 7950X, .NET 10.0.10 and a 1200x800 viewport:

| Method | Mean | Allocated |
| --- | ---: | ---: |
| `LegacyFullFrame` | 23.150 ms | 32.25 MB |
| `NewFullFrame` | 12.415 ms | 1.56 MB |
| `NewAltAzHorizonFullFrame` | 3.367 ms | 28.82 KB |
| `NewAltAzDragFramePreparation` | 3.415 ms | 33.88 KB |
| `NewAltAzDragFrameMaterialized` | 20.395 ms | 36.48 KB |

In this comparison `NewFullFrame` is 1.86 times faster than `LegacyFullFrame` and uses 95.2% less managed memory. Preparation and WPF materialization are separate measurements so changes to scene and raster work can be distinguished from presentation cost. The observer snapshot, patterned images, binding, visual and render target are prepared outside each measured operation.

Software-only interaction release gate on the same AMD Ryzen 9 7950X, .NET 10.0.11, .NET SDK 10.0.400 and a 1200x800 viewport:

| Method | Mean | Allocated |
| --- | ---: | ---: |
| Previous retained-WPF materialized frame | 19.512 ms | 36.55 KB |
| `NewAltAzSoftwareDragPreviewPreparation` | 4.161 ms | 37.92 KB |
| `NewAltAzSoftwareDragPreviewMaterialized` | 5.762 ms | 38.31 KB |
| `NewAltAzSoftwarePresentationOnly` | 1.485 ms | 1.45 KB |
| `NewAltAzSoftwareFinalFrameMaterialized` | 5.312 ms | 38.19 KB |

The materialized software drag preview must remain below 16.67 ms on this machine to preserve a 60 FPS interaction budget. Production drag invalidations are coalesced to one preview per 60 Hz interval in both WPF rendering modes so mouse input cannot build a backlog of frames. The preparation and presentation-only cases keep regressions in either half of the path easy to identify.

BenchmarkDotNet is an MIT-licensed development-only dependency of `NINA.Benchmark`; it is not included in NINA's application or installer output.
