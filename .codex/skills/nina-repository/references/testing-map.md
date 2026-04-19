# NINA Testing Map

Use this after identifying the owning project. Prefer the narrowest meaningful filter first, then broaden when a change touches shared contracts, persisted data, native/runtime assets, serialization, plugin discovery, or UI composition.

## Command Setup

Use these environment variables for local CLI test runs in this checkout:

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
```

Run one fixture, namespace, or group with `FullyQualifiedName~`:

```powershell
dotnet test NINA.Test\NINA.Test.csproj --filter 'FullyQualifiedName~NINA.Test.PlateSolving.ImageSolverTest' -v minimal --no-restore -m:1 -p:UseSharedCompilation=false -p:GeneratePackageOnBuild=false
```

Run multiple groups with `|`:

```powershell
dotnet test NINA.Test\NINA.Test.csproj --filter 'FullyQualifiedName~NINA.Test.Image|FullyQualifiedName~NINA.Test.FITSTest' -v minimal --no-restore -m:1 -p:UseSharedCompilation=false -p:GeneratePackageOnBuild=false
```

Broader CLI flow from `CONTRIBUTING.md`:

```powershell
dotnet restore NINA.sln
dotnet build NINA\NINA.csproj --configuration Debug --no-restore -m:1 -p:UseSharedCompilation=false -p:GeneratePackageOnBuild=false -p:RunPostBuildEvent=Never
dotnet build NINA.Test\NINA.Test.csproj --configuration Debug --no-restore -m:1 -p:UseSharedCompilation=false -p:GeneratePackageOnBuild=false
dotnet test NINA.Test\NINA.Test.csproj --configuration Debug --no-build -p:PlatformTarget=x64
```

Use the solution-root `.runsettings` file for Visual Studio coverage collection. Visual Studio only auto-detects runsettings files with this exact name at the solution root, so keep coverage exclusions there. It excludes third-party/vendor modules such as `Trinet.Core.IO.Ntfs`, `Accord.Imaging`, `OxyPlot`, `ASCOM.Common`, `nikoncswrapper`, and `ASCOM.Alpaca` so reports stay focused on application code. If Visual Studio has an older user-selected runsettings file, clear it or select this file under `Test > Configure Run Settings`.

## Routing Table

| Changed Surface | Primary Filters | Broaden With |
| --- | --- | --- |
| RA/Dec, angles, coordinate transforms | `NINA.Test.CoordinatesTest`, `NINA.Test.AngleTest`, `NINA.Test.AstrometryTest` | `NINA.Test.NighttimeCalculatorTest`, `NINA.Test.NighttimeDataTest`, `NINA.Test.Database.DatabaseInteractionTest` |
| SOFA/NOVAS-backed astronomy calculations | `NINA.Test.AstrometryTest.AstrometryTest`, `NINA.Test.AstrometryTest.WorldCoordinateSystemTest` | root coordinate/nighttime tests and affected sequencer time/altitude providers |
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
| Sequencer conditions | `NINA.Test.Sequencer.Conditions` | astrometry/nighttime tests for altitude/sun/moon conditions |
| Sequencer triggers | `NINA.Test.Sequencer.Trigger` | matching domain tests, sequence-item tests, serialization |
| Sequencer sequence items | `NINA.Test.Sequencer.SequenceItem` | matching equipment/domain tests and serialization |
| Sequencer connect/profile-switch items | `NINA.Test.Sequencer.SequenceItem.Connect.ConnectEquipmentTest` | profile settings, equipment mediator tests, and broader sequence-item tests |
| Sequencer flat-device instruction sets | `NINA.Test.Sequencer.SequenceItem.FlatDevice` | flat-device settings/protocol tests and imaging/camera tests |
| Sequencer camera items | `NINA.Test.Sequencer.SequenceItem.Camera`, `NINATest.Sequencer.SequenceItem.Camera` | camera/equipment and imaging tests; `SetUSBLimitTest` uses the `NINATest...` namespace |
| Sequencer imaging items | `NINA.Test.Sequencer.SequenceItem.Imaging` | image model, capture sequence, camera/equipment tests |
| Sequencer platesolving items/triggers | `NINA.Test.Sequencer.SequenceItem.Platesolving`, `NINA.Test.Sequencer.Trigger.Platesolving` | `NINA.Test.PlateSolving` and rotator/telescope tests |
| Sequencer autofocus items/triggers | `NINA.Test.Sequencer.SequenceItem.Autofocus`, `NINA.Test.Sequencer.Trigger.Autofocus` | `NINA.Test.Autofocus`, focuser, image/star-detection tests |
| Sequencer guider items/triggers | `NINA.Test.Sequencer.SequenceItem.Guider`, `NINA.Test.Sequencer.Trigger.Guider` | MGEN/equipment guider-related tests |
| Sequencer time/date providers | `NINA.Test.Sequencer.Utility.DateTimeProvider` | astrometry/nighttime tests |
| Sequencer expressions/symbols | `NINA.Test.Sequencer.Logic`, `NINA.Test.Sequencer.ExpressionBackedEntityContractTest`, `NINA.Test.Sequencer.SequenceItem.Expressions.UserSymbolInstructionTest` | build `NINA.Sequencer`; add serialization and affected expression-backed entities |
| Sequencer serialization/JSON compatibility | `NINA.Test.Sequencer.Serialization.JsonCreationConverterTest` | plugin discovery, target/template controller behavior, JSON compatibility checks |
| Sequencer drag/drop/view selectors/converters | `NINA.Test.Sequencer.Behaviors`, `NINA.Test.Sequencer.DragDrop`, `NINA.Test.Sequencer.View` | WPF/shared UI tests and app-level view-model checks |
| App/shared view models | `NINA.Test.ViewModel`, plus specific VM fixtures such as `FocuserVMTest`, `DomeVMTest`, `FlatDeviceVMTest`, `RotatorVMTest`, `AutofocusVMTest` | DI registration, mediator consumers, profile-setting tests |
| WPF base mediators | `NINA.Test.Mediator` | affected equipment/view-model tests for the concrete mediator consumers |
| WPF base sky-survey cache/factory | `NINA.Test.SkySurvey` | framing assistant callers and image/file-format tests when image loading behavior changes |
| Installer/runtime file changes | no direct unit-test filter | build `NINA`, inspect output layout, check `NINA.Setup/Product.wxs` and `NINA.SetupBundle` if bundle behavior changed |
| Source generator changes | build `NINA.Sequencer` and affected generated consumers | `NINA.Test.Sequencer.Logic`, serialization, and expression-backed sequence entity tests |

## Sequencer Namespace Shortcuts

Use these when the changed file is under the matching sequencer subtree:

- `NINA.Test.Sequencer.Conditions`
- `NINA.Test.Sequencer.Container`
- `NINA.Test.Sequencer.Container.ExecutionStrategy`
- `NINA.Test.Sequencer.Logic`
- `NINA.Test.Sequencer.Serialization`
- `NINA.Test.Sequencer.SequenceItem.<Area>` where `<Area>` is `Autofocus`, `Camera`, `Dome`, `FilterWheel`, `FlatDevice`, `Focuser`, `Guider`, `Imaging`, `Platesolving`, `Rotator`, `SafetyMonitor`, `Switch`, `Telescope`, or `Utility`
- Target-coordinate inheritance across `CoordinatesInstruction` sequence items: `NINA.Test.Sequencer.SequenceItem.CoordinatesInstructionInheritanceTest`
- `NINA.Test.Sequencer.Trigger.<Area>` where `<Area>` is `Autofocus`, `Dome`, `Guider`, `MeridianFlip`, or `Platesolving`
- `NINA.Test.Sequencer.Utility.DateTimeProvider`
- `NINA.Test.Sequencer.View`, `NINA.Test.Sequencer.View.Converter`, or `NINA.Test.Sequencer.View.MiniSequencer`

## Known Constraints

- `NINA.Test` targets `net10.0-windows` and `x64`; keep x64 for tests that load SOFA, NOVAS, image native libraries, device SDK wrappers, or copied runtime assets.
- `NINA.Test/Usings.cs` preloads `SOFAlib.dll` and `NOVAS31lib.dll` for the test process.
- Fast in-memory `NINA.Test.Image.ImageAnalysis.BayerFilter16bppTests` run by default; only `BayerFilter16bppRealWorldFormats` cases are ignored because they are exhaustive file-backed/resolution checks.
- `BayerFilter16bppRealWorldFormats` test cases are `[NonParallelizable]` when enabled.
- `NINA.Test.Sequencer.Behaviors.DragDropBehaviorTest` uses STA apartments for WPF drag/drop behavior.
- `AutofocusAfterTimeTriggerTest` has an ignored time-dependent test; avoid treating that ignored case as a new regression without inspecting the fixture.
- Hardware/provider code is generally mocked or protocol-level; do not require live hardware unless a specific integration path explicitly needs it.

## When To Add Tests

Default to adding or updating focused unit tests for every testable behavior change. This includes bug fixes, new branches, validation rules, conversions, serialization shape, settings behavior, mediator contracts, clone/reset/validation behavior, parser/format handling, numerical output, and error handling.

For UI or hardware-adjacent changes, test the view model, mediator, protocol parser, adapter behavior, or other unit-testable boundary with mocks instead of requiring live devices or full WPF automation.

Purely mechanical refactors, comments, formatting-only edits, and changes that only move code without behavior may rely on existing coverage, but verify the relevant existing tests still run. If existing coverage is missing around moved behavior, add a small regression test while the code is already in hand.

Always add or update tests when a change affects persisted profile data, sequence JSON shape, database migration/schema behavior, numerical astronomy/image-analysis output, plugin or sequencer discovery metadata, file-format compatibility, or runtime asset layout.

Update this testing map when new tests create a useful new route for future agents: a new fixture namespace, changed filter target, important command variation, ignored/STA/non-parallel constraint, native/runtime dependency, or previously uncovered subsystem. Do not update it for every individual test method when the existing route already covers the change.



