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

## Target Crossing Prediction

Build with the repository's [.NET environment setup](../.agents/skills/nina-repository/references/testing-map.md#command-setup), then run from the solution root:

```powershell
dotnet build NINA.Benchmark/NINA.Benchmark.csproj -c Release --no-restore -m:1 `
    -p:UseSharedCompilation=false -p:GeneratePackageOnBuild=false -p:RunPostBuildEvent=Never
dotnet run --project NINA.Benchmark/NINA.Benchmark.csproj -c Release --no-build --no-restore -- `
    --target-diagnostics TestResults/target-prediction/diagnostics.json
dotnet run --project NINA.Benchmark/NINA.Benchmark.csproj -c Release --no-build --no-restore -- `
    --filter '*TargetPrediction*Benchmark*' --artifacts TestResults/target-prediction/BenchmarkDotNet.Artifacts
```

The job uses one launch, three warm-up iterations and eight measured iterations, with managed-memory diagnostics and a median column. Its generated builds disable compiler sharing, package creation and post-build events. Keep CPU-heavy builds and tests idle during measurement. Raw logs, CSV/JSON reports and the coordinate-evaluation diagnostic remain under ignored `TestResults/target-prediction`.

The legacy fixture is copied from the actual `ItemUtility` at `7359a0be18e234ab056c0029c424b6b5364f103a`. It retains rise/meridian bounds, the 1/5/10 minute sampling intervals, horizon rounding and retained predicted times between updates. Its only behavioral setup differences are a deterministic captured clock, counted coordinate transformations and suppressed logging. The production methods call `WaitLoopData.CalculateTargetExpectedTime` directly. Cold benchmarks bind the private cache writer once during setup; measured resets use that delegate without reflection. Both use the same synthetic zero UT1-UTC seed through the existing EOP database/cache path and packaged x64 native DLLs.

Cold methods clear the predicted event while retaining input construction outside measurement. Flat and ordinary terrain horizons include events five minutes and three hours ahead. Other cases cover 720 terrain segments, a narrow visibility window and a circumpolar target. `CachedUpdate` measures one update five seconds after a primed prediction. `Repeated36` measures 36 updates at five-second intervals, including the first cold result and 35 cache hits within the approved 300-second lifetime. Time and allocation for `Repeated36` are totals for the sequence, not per-update values.

Every start timestamp is pinned to the retained `initial-bounded-solver-diagnostics.json` snapshot, including both ordinary-horizon leads. Narrow-window geometry uses the same pinned flat-crossing reference timestamp and the public full coordinate transform. Workload construction never calls the production crossing solver, so changing its accuracy cannot move a workload or alter the legacy sampling count. Start and returned-event predicates are also checked through the full transform.

The adapter cost excludes generated expression getter/offset synchronization, instruction execution and scheduler overhead. Reading an unchanged offset no longer performs a coordinate transform, so repeated expression updates do not hide another production coordinate sample. Changing an offset still refreshes the horizon immediately. Production evaluation counts come from its returned diagnostic; legacy counts include its current-state, analytic-bound and custom-horizon coordinate transformations. The diagnostics record legacy success, qualifying-predicate status and event timestamps so an unresolved `--` result cannot be treated as a speedup. Functional and first-crossing accuracy tests remain in `NINA.Test`.

The accepted targets are at least 90% fewer coordinate evaluations over repeated ordinary-custom-horizon updates and at most 20% median cold-time regression for both flat and ordinary horizons. The near and far workloads must be assessed separately; a cheap failed legacy prediction is reported as a failure rather than a valid speed comparison.

Final reference comparison on 2026-10-08: AMD Ryzen 9 7950X, Windows 11 10.0.26200.9457, .NET SDK 10.0.401 and runtime 10.0.12, x64. Within its bounded search budget, the production contract allows up to 10 seconds of prediction error and detects qualifying windows lasting at least 10 seconds; budget exhaustion returns `Exhausted`. See the [owning astrometry contract](../NINA.Astrometry/ARCHITECTURE.md#fixed-target-crossings) for its limits. All 22 BenchmarkDotNet cases completed successfully with the unchanged captured workload and approved 300-second cache lifetime.

| Cold workload | Legacy median, us | Production median, us | Median change | Legacy allocated, B | Production allocated, B |
| --- | ---: | ---: | ---: | ---: | ---: |
| Flat, five-minute lead | 170.4 | 176.8 | +3.8% | 1704 | 392 |
| Flat, three-hour lead | 597.5 | 172.7 | -71.1% | 4728 | 392 |
| Ordinary terrain, five-minute lead | 312.9 | 266.9 | -14.7% | 2712 | 456 |
| Ordinary terrain, three-hour lead | 659.5 | 491.8 | -25.4% | 5400 | 616 |

All four flat/ordinary cold workloads pass the at-most-20% median regression target. The percentages above use measured medians; BenchmarkDotNet's default Ratio column compares means.

| Update workload | Legacy median, us | Production median, us | Legacy allocated, B | Production allocated, B |
| --- | ---: | ---: | ---: | ---: |
| Cached update, near | 652.78 | 43.70 | 5368 | 264 |
| Cached update, far | 684.03 | 44.47 | 5344 | 264 |
| 36 updates, near | 19701.86 | 1763.12 | 167280 | 10224 |
| 36 updates, far | 23450.33 | 2065.43 | 192440 | 10472 |

The complete repeated sequences reduce median time by 91.05% near and 91.19% far, with managed allocation reduced by 93.89% and 94.56% respectively. They each include one cold calculation and 35 cache hits; the near cold calculation takes 6 coordinate evaluations and the far calculation takes 11. A cached update takes 1 evaluation versus legacy's 15.

| Repeated workload | Legacy evaluations | Production evaluations | Reduction | Mandatory 90% target |
| --- | ---: | ---: | ---: | --- |
| 36 updates, near | 460 | 41 | 91.09% | Pass |
| 36 updates, far | 540 | 46 | 91.48% | Pass |

With 35 cache hits, these unchanged workloads permit at most 11 evaluations for the initial cold calculation near and 19 far. Both pass the mandatory count target, including their initial calculation.

| Difficult cold workload | Legacy median, us | Production median, us | Legacy allocated, B | Production allocated, B | Legacy / production evaluations |
| --- | ---: | ---: | ---: | ---: | ---: |
| Dense terrain | 321.9 | 1374.7 | 2712 | 936 | 7 / 21 |
| Circumpolar | 3872.0 | 5547.2 | 28920 | 4200 | 85 / 123 |
| Narrow visibility window | 3310.3 | 613.2 | 25584 | 680 | 75 / 13 |

Dense and circumpolar cold calculations are 4.27 and 1.43 times the legacy median respectively, with lower managed allocation. These difficult workloads are separate from the flat/ordinary cold acceptance target. Legacy returns an unresolved result for the narrow window while production returns a qualifying event, so its raw costs do not establish a valid speedup. All seven production scenarios returned qualifying events, checked independently through the full coordinate transform.

The final diagnostic is retained as `TestResults/target-prediction/final-diagnostics.json`; full timing reports and console output remain beside it. The historical `initial-bounded-solver-diagnostics.json` preserves the earlier 60-second-cache results: 69.13% and 68.52% fewer repeated evaluations, both below the mandatory target. Its pinned timestamps and geometry were reused without shifting starts or changing the measurement window for the final comparison.

## Optional Horizon Files

`HorizonFilePredictionBenchmark` measures supplied horizon files separately from the ordinary `*TargetPrediction*Benchmark*` filter. Set `NINA_TARGET_HORIZON_FILES` to absolute paths separated by the platform path separator (a semicolon on Windows). Input files and personal paths are not checked in. Run from the solution root after the normal Release build and .NET environment setup:

```powershell
$env:NINA_TARGET_HORIZON_FILES = (Resolve-Path $firstHorizon).Path + ';' + (Resolve-Path $secondHorizon).Path
dotnet run --project NINA.Benchmark/NINA.Benchmark.csproj -c Release --no-build -- --horizon-file-diagnostics TestResults/target-prediction/glasberg/diagnostics.json
dotnet run --project NINA.Benchmark/NINA.Benchmark.csproj -c Release --no-build -- --filter '*HorizonFilePredictionBenchmark*' --artifacts TestResults/target-prediction/glasberg/BenchmarkDotNet.Artifacts
```

The fixture assumes latitude 47 degrees north, longitude 8 degrees east, elevation 450 m, J2000 RA 20 degrees, declination 20 degrees and zero horizon offset. These are comparison assumptions, not metadata read from a file. Two files produce 16 cases: five-minute and three-hour leads, each with legacy/production cold and 36-update measurements. Updates occur every five seconds with an initially empty cache and the production 300-second cache lifetime.

Setup uses the existing horizon parser, including its comma/space handling and header warning. The first rising transition after the fixed UTC seed is located through public full coordinate transformations sampled every second and refined to one millisecond. Each actual lead start receives another independent first-window scan so an earlier opening is not skipped. Setup rejects inaccurate or nonqualifying production predictions. Parsing, reference scanning and input hashing are outside timing. The scan is a workload reference, not a proof that arbitrary subsecond windows cannot occur.

Diagnostics retain input SHA-256, vertex counts, altitude ranges, steep segments, independently refined window boundaries and every legacy/production event with its qualifying status, error and evaluation count. A separate declination-60 diagnostic exercises different geometry on the first file without expanding the timed case count. Treat timings as a valid cost comparison only when both predictions qualify in the independently identified first window; legacy lateness remains visible. Raw reports, console output and diagnostics stay in ignored `TestResults/target-prediction/glasberg`.

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
