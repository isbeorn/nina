# NINA benchmarks

Run the offline sky-map comparison from the repository root in Release mode:

```powershell
dotnet run --project NINA.Benchmark\NINA.Benchmark.csproj -c Release -- --filter *SkyMapRenderingBenchmark*
```

`LegacyFullFrame` models the previous whole-catalogue scan, retained mutable annotations, GDI raster, and full WPF bitmap-copy path. `NewFullFrame` builds one viewport scene and draws it into a reusable WPF surface. The synthetic catalogue sizes mirror the order of magnitude of NINA's checked-in seed data.

Use `--filter *Layer*` to measure constellation/star, DSO, boundary, equatorial-grid, Alt/Az-grid, horizon scene generation, the orientation of 32 cached images, and 32 changing camera-rectangle placements individually. `NewRasterOnly` isolates final image generation, while `NewAltAzHorizonFullFrame` measures the complete time-dependent view. `NewAltAzDragFrame` covers a complete alternating drag frame: scene generation, cached-image projection and composition, camera-overlay projection, raster generation, grid, and horizon.

Reference result on an AMD Ryzen 9 7950X, .NET 10.0.10, 1200×800 viewport:

| Method | Mean | Allocated |
| --- | ---: | ---: |
| `LegacyFullFrame` | 21.59 ms | 22.47 MB |
| `NewFullFrame` | 11.37 ms | 2.15 MB |
| `NewAltAzHorizonFullFrame` | 3.04 ms | 277.93 KB |
| `NewAltAzDragFrame` | 2.85 ms | 309.18 KB |

The current comparison makes `NewFullFrame` 1.90 times faster than `LegacyFullFrame` while using 90.4% less managed memory. The same benchmark suite measured constellation/star scene generation at 39.16 µs, the equatorial grid at 314.68 µs, constellation boundaries at 414.99 µs, DSO outlines at 533.85 µs, and reusable-surface raster generation at 10.04 ms with 64.26 KB allocated.

The Alt/Az and local-horizon benchmarks on the same machine measured grid generation at 30.92 µs with 72.84 KB allocated, complete flat-horizon line and mask generation at 16.80 µs with 28.34 KB allocated, and the complete true Alt/Az projection plus horizon frame at 3.04 ms with 277.93 KB allocated. Compared with the first projection-safe implementation, direct horizontal-grid projection is about 12 times faster and allocates 82% less memory; the full frame keeps the same speed while allocating 57% less. The projection-safe 140° Alt/Az horizon-mask regression measures 334.87 µs with 134.94 KB allocated. The integrated alternating drag frame, including all sky layers, cached-image composition, 32 image orientations, 32 camera-overlay placements, grid, horizon, and raster generation, completes in 2.85 ms with 309.18 KB allocated. The observer snapshot is prepared with a deterministic valid sidereal time outside each measured operation and reused for one minute, matching the application drag path without depending on the installed application database.

The cached-image Alt/Az two-axis transform benchmark processes 32 visible images in 6.753 µs with no managed allocations (about 211 ns per image). The camera-overlay benchmark rebinds 32 recalculated framing models and projects their centers and position angles in 5.432 µs with no managed allocations (about 170 ns per rectangle).

BenchmarkDotNet is an MIT-licensed development-only dependency of `NINA.Benchmark`; it is not included in NINA's application or installer output.