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

## Published Application Startup

Run these commands from the solution root in PowerShell 7 using the repository's [.NET environment setup](../.agents/skills/nina-repository/references/testing-map.md#command-setup). Each publish destination must be empty.

```powershell
./.github/scripts/publish-application.ps1 -Mode Baseline -OutputDirectory TestResults/readytorun/baseline
./.github/scripts/publish-application.ps1 -Mode ReadyToRun -OutputDirectory TestResults/readytorun/readytorun
./.github/scripts/publish-application.ps1 -Mode ReadyToRunComposite -OutputDirectory TestResults/readytorun/composite
./NINA.Benchmark/Measure-Startup.ps1 `
    -BaselineDirectory TestResults/readytorun/baseline `
    -ReadyToRunDirectory TestResults/readytorun/readytorun `
    -CompositeDirectory TestResults/readytorun/composite `
    -SeedDirectory TestResults/readytorun/seed `
    -OutputDirectory TestResults/readytorun/measurements -Iterations 12
```

Prepare the seed directory with a copy of `NINA.sqlite` and a `Profiles` directory containing one benchmark profile with no automatic device connections. Do not point it at live user storage. Each launch receives its own copy of the seed. The probe redirects application storage, uses in-memory application settings, suppresses the update check and moves the 1280x800 window off screen. It exits at the first rendered main window. Background operations may still be running at that point.

The runner warms each variant once, then rotates all six launch orders across the measured rounds. No build or other CPU-heavy work should run during the measured series. `startup.csv` retains every observation, including warm-ups with iteration zero; `summary.json` excludes those warm-ups. Per-launch JSON files, logs and isolated state directories are retained for diagnosis.

This measures new-process startup with warm filesystem caches. It does not measure startup after reboot, hardware connection, third-party plugin workloads or steady-state image processing. JIT compilation time is accumulated across JIT threads and must not be subtracted directly from wall time. Working set and private bytes are snapshots at first render, not peak or steady-state memory. The installed NINA application is not replaced. ReadyToRun is intended to reduce startup JIT work; tiered compilation remains enabled so hot methods can still receive optimized JIT code. See Microsoft's [ReadyToRun documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run).

Reference comparison on 2026-09-30: NINA 3.3.0.1062, AMD Ryzen 9 7950X, Windows 11 Pro 10.0.26200, .NET SDK 10.0.401 and runtime 10.0.12. All three variants used the same optimized Release source, self-contained `win-x64`, one default profile, the same copied database and no external plugins in the timed workload. Twelve measured launches per variant followed one warm-up each.

| Metric | Baseline | ReadyToRun | Composite ReadyToRun |
| --- | ---: | ---: | ---: |
| Median first render | 2667.1 ms | 2020.6 ms | 1782.8 ms |
| Startup reduction | - | 24.2% | 33.2% |
| Observed startup range | 2583.5-2764.6 ms | 1928.5-2119.1 ms | 1724.4-1944.5 ms |
| Median process CPU | 4523.4 ms | 3523.4 ms | 3000.0 ms |
| Median JIT compilation time | 1617.9 ms | 661.1 ms | 432.3 ms |
| Median working set at first render | 340.7 MiB | 337.1 MiB | 338.1 MiB |
| Median private bytes at first render | 381.8 MiB | 384.9 MiB | 384.5 MiB |
| Published bytes excluding PDBs and documentation | 476.4 MiB | 537.2 MiB | 594.6 MiB |
| Compressed unsigned MSI including documentation | 166.7 MiB | 190.3 MiB | 203.2 MiB |

Ordinary ReadyToRun is the release-workflow choice. Composite saves another 237.8 ms in this workload, with another 57.4 MiB of published payload and 12.9 MiB of compressed MSI. Keep composite available for experiments rather than assuming that this machine's warm-cache results apply to cold disk reads or all plugin combinations. Neither result demonstrates a steady-state imaging speedup.

The local check also loaded and initialized a synthetic external MEF plugin in all three variants, built all three MSIs and compared extracted runtime files with their published SHA-256 hashes for ordinary and composite ReadyToRun. This verifies package selection and basic extension loading; it does not replace production certificate signing, installer upgrade tests or hardware testing. Installer builds retain the existing WiX deprecation, file metadata and same-version upgrade warnings. Local raw measurements and package checks are retained under `TestResults/readytorun`.
