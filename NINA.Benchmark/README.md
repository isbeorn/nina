# NINA benchmarks

Run the offline sky-map comparison from the repository root in Release mode:

```powershell
dotnet run --project NINA.Benchmark\NINA.Benchmark.csproj -c Release -- --filter *SkyMapRenderingBenchmark*
```

`LegacyFullFrame` models the previous whole-catalogue scan, retained mutable annotations, GDI raster, and full WPF bitmap-copy path. `NewFullFrame` builds one viewport scene and draws it into a reusable WPF surface. The synthetic catalogue sizes mirror the order of magnitude of NINA's checked-in seed data.

Use `--filter *Layer*` to measure constellation/star, DSO, boundary, equatorial-grid, Alt/Az-grid, horizon scene generation, the orientation of 32 cached images and 32 changing camera-rectangle placements individually. `NewRasterOnly` isolates final image generation while `NewAltAzHorizonFullFrame` measures the complete time-dependent view. `NewAltAzDragFramePreparation` measures scene generation, cached-image projection and composition, camera-overlay projection, raster generation, grid and horizon. `NewAltAzDragFrameMaterialized` adds the real WPF image binding and presentation into a reusable 1200x800 render target. Images, bindings and visuals are constructed outside the measured operation.

Reference result from the stable job on an AMD Ryzen 9 7950X, .NET 10.0.10 and a 1200x800 viewport:

| Method | Mean | Allocated |
| --- | ---: | ---: |
| `LegacyFullFrame` | 23.150 ms | 32.25 MB |
| `NewFullFrame` | 12.415 ms | 1.56 MB |
| `NewAltAzHorizonFullFrame` | 3.367 ms | 28.82 KB |
| `NewAltAzDragFramePreparation` | 3.415 ms | 33.88 KB |
| `NewAltAzDragFrameMaterialized` | 20.395 ms | 36.48 KB |

In this comparison `NewFullFrame` is 1.86 times faster than `LegacyFullFrame` and uses 95.2% less managed memory. Preparation and WPF materialization are separate measurements so changes to scene and raster work can be distinguished from presentation cost. The observer snapshot, patterned images, binding, visual and render target are prepared outside each measured operation.

BenchmarkDotNet is an MIT-licensed development-only dependency of `NINA.Benchmark`; it is not included in NINA's application or installer output.
