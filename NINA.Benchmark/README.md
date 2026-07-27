# NINA benchmarks

Run the offline sky-map comparison from the repository root in Release mode:

```powershell
dotnet run --project NINA.Benchmark\NINA.Benchmark.csproj -c Release -- --filter *SkyMapRenderingBenchmark*
```

`LegacyFullFrame` models the previous whole-catalogue scan, retained mutable annotations, GDI raster, and full WPF bitmap-copy path. `NewFullFrame` builds one viewport scene and draws it into a reusable WPF surface. The synthetic catalogue sizes mirror the order of magnitude of NINA's checked-in seed data.

Use `--filter *Layer*` to measure constellation/star, DSO, boundary, and equatorial-grid scene generation individually. `NewRasterOnly` isolates final image generation.

Reference result on an AMD Ryzen 9 7950X, .NET 10.0.10, 1200×800 viewport:

| Method | Mean | Allocated |
| --- | ---: | ---: |
| `LegacyFullFrame` | 22.76 ms | 22.47 MB |
| `NewFullFrame` | 11.22 ms | 2.16 MB |

The same run measured constellation/star scene generation at 37 µs, the equatorial grid at 303 µs, constellation boundaries at 468 µs, DSO outlines at 533 µs, and reusable-surface raster generation at 11.10 ms with 64 KB allocated.

BenchmarkDotNet is an MIT-licensed development-only dependency of `NINA.Benchmark`; it is not included in NINA's application or installer output.
