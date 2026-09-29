# NINA Testing Map

Choose the route for the changed behavior. Start with focused checks and broaden for shared contracts, persisted data, native assets, plugin discovery or UI composition. The [test architecture](../../../../NINA.Test/ARCHITECTURE.md) explains fixtures and bootstrap; [contribution guidance](../../../../CONTRIBUTING.md#running-auts-from-the-command-line) distinguishes local verification from required CI.

## Command Setup

Run from the repository root on Windows with the SDK from `global.json` and initialized submodules. [Developer prerequisites](../../../../CONTRIBUTING.md#setting-up-the-developer-environment) cover native dependencies and tooling. In the same PowerShell session as the commands below, set:

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
```

Restore on initial setup or when changed dependencies/missing assets require it: `dotnet restore NINA.sln`. Otherwise use `--no-restore`. These local build flags avoid compiler-server and package/post-build side effects; omit `RunPostBuildEvent=Never` when those outputs are what the task needs to verify.

```powershell
dotnet build NINA/NINA.csproj --configuration Debug --no-restore -m:1 -p:UseSharedCompilation=false -p:GeneratePackageOnBuild=false -p:RunPostBuildEvent=Never
dotnet build NINA.Test/NINA.Test.csproj --configuration Debug --no-restore -m:1 -p:UseSharedCompilation=false -p:GeneratePackageOnBuild=false -p:RunPostBuildEvent=Never
```

Build the affected project(s) for the task; the test project references the application. The examples use Debug. Use `--no-build --no-restore` for subsequent test runs only after a build that includes the final source edit.

```powershell
dotnet test NINA.Test/NINA.Test.csproj --configuration Debug --no-build --no-restore --filter 'FullyQualifiedName~NINA.Test.PlateSolving.ImageSolverTest' -v minimal
dotnet test NINA.Test/NINA.Test.csproj --configuration Debug --no-build --no-restore --filter 'FullyQualifiedName~NINA.Test.Image|FullyQualifiedName~NINA.Test.FITSTest' -v minimal -- NUnit.AssemblySelectLimit=10000
```

Each filter in the routing table is a `FullyQualifiedName~` value; combine groups with `|` and exclusions with `&FullyQualifiedName!~`. Keep the whole filter quoted in PowerShell. For large filtered runs, pass `-- NUnit.AssemblySelectLimit=10000` so the NUnit adapter keeps the selected tests instead of falling back to an assembly-wide run when its selection limit is exceeded. Reassess the limit if the selected set grows beyond it.

Add `--list-tests` to a test command to validate its filter without executing tests. `ExpressionGeneratorDiagnosticsTest.cs` is a partial declaration of `ExpressionGeneratorTest`; use the fixture name for discovery and execution. Check for zero matches rather than assuming a successful command selected tests.

Use the solution-root [`.runsettings`](../../../../.runsettings) for Visual Studio coverage collection. It excludes vendor modules such as `Trinet.Core.IO.Ntfs`, `Accord.Imaging`, `OxyPlot`, `ASCOM.Common`, `nikoncswrapper` and `ASCOM.Alpaca`. Visual Studio auto-detects this filename at the solution root; clear any older user-selected file or select this one under `Test > Configure Run Settings`.

## Routing Table

| Changed Surface | Primary Filters | Broaden With |
| --- | --- | --- |
| RA/Dec, angles, coordinate transforms | `NINA.Test.CoordinatesTest`, `NINA.Test.AngleTest`, `NINA.Test.AstrometryTest` | `NINA.Test.NighttimeCalculatorTest`, `NINA.Test.NighttimeDataTest`, `NINA.Test.Database.DatabaseInteractionTest` |
| SOFA/NOVAS-backed astronomy calculations | `NINA.Test.AstrometryTest.AstroUtilTest`, `NINA.Test.AstrometryTest.WorldCoordinateSystemTest` | root coordinate/nighttime tests and affected sequencer time/altitude providers |
| Custom horizon parsing/visibility | `NINA.Test.AstrometryTest.CustomHorizonTest`, `NINA.Test.AstrometryTest.InputCoordinatesTest` | `NINA.Test.Sequencer.Conditions.AboveHorizonConditionTest`, wait-for-altitude sequence items |
| Database/catalog access | `NINA.Test.Database.DatabaseInteractionTest` | astrometry/catalog callers and migration/runtime SQL checks |
| Core utilities/models | `NINA.Test.Utility`, `NINA.Test.Model`, `NINA.Test.RMSTest`, `NINA.Test.GuideStepsHistoryTest` | nearest subsystem tests that consume the changed type |
| Core validation and serial helpers | `NINA.Test.Utility.ValidationRules`, `NINA.Test.Utility.SerialCommunication` | `NINA.Test.Utility` and consumers of the validated setting/protocol |
| CLI option parsing | `NINA.Test.Utility.CommandLineOptionsTest` | app startup/build checks |
| Localization-facing converters/formatting | `NINA.Test.Converters` | affected VM/view tests and `NINA.Test.SerialCommunication` when response text is involved |
| Profile persistence/settings/service | `NINA.Test.ProfileTest` or specific fixtures such as `PluginSettingsTest`, `PluginOptionsAccessorTest`, `ProfilePersistenceTest`, `ProfileServiceBehaviorTest` | plugin tests, profile-switch sequencer tests, and changed consumers of profile settings |
| Image data model/metadata/patterns | `NINA.Test.ImageDataTest`, `NINA.Test.ImageMetaDataTest`, `NINA.Test.FilePatternTest`, `NINA.Test.Image.ExposureDataFactoryTest` | image history, autofocus, plate solving, sequencer imaging items |
| FITS/XISF/file format I/O | `NINA.Test.FITSTest`, `NINA.Test.XISFTest`, `NINA.Test.Image.FileFormat` | `NINA.Test.Image`, runtime native asset/output checks |
| Bayer/debayer/image analysis | `NINA.Test.Image.ImageAnalysis.BayerFilter16bppTests`, `NINA.Test.Image.ImageAnalysis.ImageAnalysisUtilityBehaviorTest` | autofocus and star-detection consumers; slow `BayerFilter16bppRealWorldFormats` cases are ignored by default |
| Star detection measurements | `NINA.Test.Image.StarDetectionMeasurementTest` | `NINA.Test.Autofocus`, plate solving, image history, sequencer imaging items |
| Image history VM | `NINA.Test.ImageHistoryVMTest` | image model/file pattern tests |
| Autofocus fitting/report/VM | `NINA.Test.Autofocus` | sequencer autofocus item/trigger tests and star detection tests |
| Capture sequence/simple sequencer | `NINA.Test.CaptureSequence`, `NINA.Test.SimpleSequencer` | sequencer container/sequence-item tests when advanced sequencer behavior changed |
| Plate solver orchestration | `NINA.Test.PlateSolving` | `NINA.Test.ViewModel.PlateSolvingStatusVMTest`, sequencer platesolving items/triggers, telescope/imaging mediator consumers |
| Focuser core/VM/backlash | `NINA.Test.Focuser` | autofocus tests, sequencer focuser items, autofocus triggers |
| Dome behavior | `NINA.Test.Dome` | sequencer dome items and `NINA.Test.Sequencer.Trigger.Dome` |
| Rotator behavior | `NINA.Test.Rotator.RotatorVMTest` | sequencer rotator item and plate-solving rotate items |
| Flat device protocols/VM/settings | `NINA.Test.FlatDevice` | sequencer flat-device items |
| Camera/equipment SDK providers | `NINA.Test.Equipment.Camera`, `NINA.Test.Equipment.SDK.CameraSDKs` | sequencer camera/imaging items and runtime native dependency checks |
| Planetarium integration | `NINA.Test.Planetarium.StellariumTest` | telescope/framing callers if touched |
| Serial communication protocol/response cache | `NINA.Test.SerialCommunication` | flat-device or equipment tests that use the protocol layer |
| MGEN command protocol | `NINA.Test.MGEN.Commands` | guider sequence items/triggers and equipment guider adapter checks |
| Plugin versions/message broker | `NINA.Test.Plugin` | profile plugin settings, plugin-loader composition, sequencer serialization/discovery if extension surfaces changed |
| Sequencer engine/container strategies | `NINA.Test.Sequencer.SequencerTest`, `NINA.Test.Sequencer.Container`, `NINA.Test.Sequencer.Container.ExecutionStrategy` | sequence item/condition/trigger tests and serialization |
| Sequencer conditions | `NINA.Test.Sequencer.Conditions` | expression contract tests and astrometry/nighttime tests; run compiled-template cases in the WPF group below |
| Sequencer triggers | `NINA.Test.Sequencer.Trigger` | matching domain tests, sequence-item tests, serialization |
| Sequencer sequence items | `NINA.Test.Sequencer.SequenceItem` | matching equipment/domain tests and serialization |
| Sequencer connect/profile-switch items | `NINA.Test.Sequencer.SequenceItem.Connect.ConnectEquipmentTest` | profile settings, equipment mediator tests, and broader sequence-item tests |
| Sequencer flat-device instruction sets | `NINA.Test.Sequencer.SequenceItem.FlatDevice` | flat-device settings/protocol tests and imaging/camera tests |
| Sequencer camera items | `NINA.Test.Sequencer.SequenceItem.Camera` | camera/equipment and imaging tests |
| Sequencer imaging items | `NINA.Test.Sequencer.SequenceItem.Imaging` | image model, capture sequence, camera/equipment tests |
| Sequencer platesolving items/triggers | `NINA.Test.Sequencer.SequenceItem.Platesolving`, `NINA.Test.Sequencer.Trigger.Platesolving` | `NINA.Test.PlateSolving` and rotator/telescope tests |
| Sequencer autofocus items/triggers | `NINA.Test.Sequencer.SequenceItem.Autofocus`, `NINA.Test.Sequencer.Trigger.Autofocus` | `NINA.Test.Autofocus`, focuser, image/star-detection tests |
| Sequencer guider items/triggers | `NINA.Test.Sequencer.SequenceItem.Guider`, `NINA.Test.Sequencer.Trigger.Guider` | MGEN/equipment guider-related tests |
| Sequencer time/date providers | `NINA.Test.Sequencer.Utility.DateTimeProvider` | astrometry/nighttime tests |
| Sequencer expressions/symbols | `NINA.Test.Sequencer.Logic`, `NINA.Test.Sequencer.ExpressionBackedEntityContractTest`, `NINA.Test.Sequencer.SequenceItem.Expressions.UserSymbolInstructionTest`, `NINA.Test.Sequencer.Conditions.AltitudeExpressionLifecycleTest` | generator tests, serialization and affected entities; split compiled-template cases into the WPF group below |
| Sequencer serialization/JSON compatibility | `NINA.Test.Sequencer.Serialization.JsonCreationConverterTest` | plugin discovery, target/template controller behavior, JSON compatibility checks |
| Sequencer drag/drop/view selectors/converters | `NINA.Test.Sequencer.Behaviors`, `NINA.Test.Sequencer.DragDrop`, `NINA.Test.Sequencer.View` | WPF/shared UI and app view-model checks; isolate `SequenceViewCodeBehindTest` in the WPF group below |
| Sequencer editing, undo/redo and field bindings | `NINA.Test.Sequencer.Editing`, `NINA.Test.Sequencer.View.SequenceViewCodeBehindTest` | `CoreEditorHistoryTest` exercises compiled templates through routed WPF input; use the process groups below, then plugin/engine suites for shared contract changes |
| App/shared view models | `NINA.Test.ViewModel`, plus specific VM fixtures such as `FocuserVMTest`, `DomeVMTest`, `FlatDeviceVMTest`, `RotatorVMTest`, `AutofocusVMTest` | DI registration, mediator consumers, profile-setting tests |
| WPF base mediators | `NINA.Test.Mediator` | affected equipment/view-model tests for the concrete mediator consumers |
| WPF base sky-survey cache/factory/offline map | `NINA.Test.SkySurvey` | run `NINA.Benchmark` sky-map comparisons for render-path changes; include framing assistant callers and image/file-format tests when image loading behavior changes |
| Installer/runtime file changes | no direct unit-test filter | build `NINA`, inspect output layout, check `NINA.Setup/Product.wxs` and `NINA.SetupBundle` if bundle behavior changed |
| Expression source generator and plugin diagnostics | `NINA.Test.Sequencer.ExpressionGeneratorTest`, `NINA.Test.Sequencer.ExpressionBackedEntityContractTest` | build `NINA.Sequencer.Generators`, `NINA.Sequencer` and affected consumers including `NINA`; broaden to logic, serialization, entity lifecycle and WPF groups below |

## Sequencer Namespace Shortcuts

Use these when the changed file is under the matching sequencer subtree:

- `NINA.Test.Sequencer.Conditions`
- `NINA.Test.Sequencer.Container`
- `NINA.Test.Sequencer.Container.ExecutionStrategy`
- `NINA.Test.Sequencer.Logic`
- `NINA.Test.Sequencer.Serialization`
- `NINA.Test.Sequencer.SequenceItem.<Area>` where `<Area>` is `Autofocus`, `Camera`, `Dome`, `FilterWheel`, `FlatDevice`, `Focuser`, `Guider`, `Imaging`, `Platesolving`, `Rotator`, `SafetyMonitor`, `Switch`, `Telescope`, or `Utility`
- Target-coordinate inheritance across `CoordinatesInstruction` sequence items: `NINA.Test.Sequencer.SequenceItem.CoordinatesInstructionInheritanceTest`
- `NINA.Test.Sequencer.Trigger.<Area>` where `<Area>` is `Autofocus`, `Dome`, `Guider`, `MeridianFlip`, `Platesolving`, or `Utility`
- `NINA.Test.Sequencer.Utility.DateTimeProvider`
- `NINA.Test.Sequencer.View`, `NINA.Test.Sequencer.View.Converter`, or `NINA.Test.Sequencer.View.MiniSequencer`

## Sequencer And WPF Process Groups

When shared sequencer or expression changes require broad coverage, use these five commands as separate test processes after building. Do not combine their filters into one invocation: the Application/dispatcher and static resource lifetimes can survive fixture teardown even when tests run STA and nonparallel. These are local verification groups; CI configuration is unchanged.

Engine, expressions, generator diagnostics and entity contracts, excluding the WPF fixtures below:

```powershell
dotnet test NINA.Test/NINA.Test.csproj --no-build --no-restore --filter 'FullyQualifiedName~NINA.Test.Sequencer&FullyQualifiedName!~NINA.Test.Sequencer.Editing&FullyQualifiedName!~TemplateControllerTest&FullyQualifiedName!~SequenceViewCodeBehindTest&FullyQualifiedName!~MiniSequencerViewTest&FullyQualifiedName!~LoadImagingLayoutDataTemplateTest&FullyQualifiedName!~AltitudeExpressionLifecycleTest.CompiledTemplate' -v minimal -- NUnit.AssemblySelectLimit=10000
```

Editor history, app sequence views and Sun/Moon expression templates:

```powershell
dotnet test NINA.Test/NINA.Test.csproj --no-build --no-restore --filter 'FullyQualifiedName~NINA.Test.Sequencer.Editing|FullyQualifiedName~SequenceViewCodeBehindTest|FullyQualifiedName~AltitudeExpressionLifecycleTest.CompiledTemplate' -v minimal -- NUnit.AssemblySelectLimit=10000
```

Mini-sequencer view, imaging-layout template and template controller, each in its own process:

```powershell
dotnet test NINA.Test/NINA.Test.csproj --no-build --no-restore --filter 'FullyQualifiedName~MiniSequencerViewTest' -v minimal

dotnet test NINA.Test/NINA.Test.csproj --no-build --no-restore --filter 'FullyQualifiedName~LoadImagingLayoutDataTemplateTest' -v minimal
dotnet test NINA.Test/NINA.Test.csproj --no-build --no-restore --filter 'FullyQualifiedName~NINA.Test.Sequencer.TemplateControllerTest' -v minimal
```

For a focused binding fix, select the fixture exercising that actual template/view first. Build the affected XAML and instantiate the view when practical. The editor's [coverage inventory](../../../../NINA.Sequencer/Editing/UI-COVERAGE.md) and [architecture](../../../../NINA.Sequencer/Editing/ARCHITECTURE.md) describe the integration paths.

## Known Constraints

- `NINA.Test` targets `net10.0-windows` and `x64`; keep x64 for tests that load SOFA, NOVAS, image native libraries, device SDK wrappers, or copied runtime assets.
- `NINA.Test/Usings.cs` preloads `SOFAlib.dll` and `NOVAS31lib.dll` for the test process.
- WPF-facing tests that instantiate `System.Windows.Application`, depend on `Application.Current.Resources`, or construct XAML-backed views should run in STA and generally be marked `[NonParallelizable]` to avoid concurrent access to application resources. STA/nonparallel attributes do not reset the process-wide Application or dispatcher lifetime; use separate processes for the groups above.
- New file-writing tests should prefer `TestContext.CurrentContext.WorkDirectory`, a test-created temp root, or an injected path provider. Avoid shared `%LOCALAPPDATA%` writes unless user-profile storage is the behavior under test.
- In sandboxed agent runs, access-denied failures while writing under `obj`, `bin`, `TestResults`, or user-profile paths need appropriate filesystem access before verification can finish. Inspect the denied path and use the available permission flow for the same scoped command; do not treat an access failure as a product regression or claim it passed.
- Fast in-memory `NINA.Test.Image.ImageAnalysis.BayerFilter16bppTests` run by default; only `BayerFilter16bppRealWorldFormats` cases are ignored because they are exhaustive file-backed/resolution checks.
- `BayerFilter16bppRealWorldFormats` test cases are `[NonParallelizable]` when enabled.
- `NINA.Test.Sequencer.Behaviors.DragDropBehaviorTest` uses STA apartments for WPF drag/drop behavior.
- `AutofocusAfterTimeTriggerTest` has an ignored time-dependent test; avoid treating that ignored case as a new regression without inspecting the fixture.
- Hardware/provider code is generally mocked or protocol-level; do not require live hardware unless a specific integration path explicitly needs it.

## Choosing Coverage

Test at the closest meaningful layer. For an expression lifecycle bug, use production construction, attachment and validation without explicitly evaluating expressions to repair missing production behavior. For a binding/resource bug, exercise the compiled template or view. Use mock equipment/mediators when physical devices are outside the changed behavior.

Add or update focused regression coverage for behavior changes, especially persisted profiles/sequences, database migrations, numerical output, plugin discovery and file-format compatibility. Cover relevant paired operations and boundaries, including clone/reset/reparenting where applicable. Mechanical changes can rely on relevant existing coverage when behavior is preserved.

Prose-only changes need link, anchor, reference and formatting checks rather than application builds or runtime tests. Skill changes also need metadata/discovery checks; test-command changes need discovery checks. After the final source edit, run the relevant checks once more; expand only for new changes, failures or unresolved concerns. Report skipped tests, warnings and environmental limits accurately.

Update this map for useful new fixture routes, changed filter names, command variations or reusable constraints. Individual test methods do not need entries when an existing route already covers them.
