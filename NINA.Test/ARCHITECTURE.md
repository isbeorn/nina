# NINA.Test Architecture

## Purpose

`NINA.Test` is the NUnit-based test project for the solution. It exercises code across almost every `NINA.*` runtime assembly.

Build shape from `NINA.Test.csproj`:

- Target framework: `net10.0-windows`
- Test framework: NUnit
- Platform target: `x64`
- References the main runtime libraries and the `NINA` executable project

## Test Organization

The folder structure mirrors the production code areas being tested:

- `AstrometryTest/`
- `Autofocus/`
- `Converters/`
- `Database/`
- `Dome/`
- `Equipment/`
- `FlatDevice/`
- `Focuser/`
- `Image/`
- `MGEN/`
- `Planetarium/`
- `PlateSolving/`
- `Plugin/`
- `ProfileTest/`
- `Rotator/`
- `SkySurvey/`
- `Sequencer/`
- `SerialCommunication/`
- `SimpleSequencer/`
- `Utility/`
- `ViewModel/`

The `Sequencer/` subtree is especially deep and mirrors the production sequencer areas (`Behaviors`, `Conditions`, `Container`, `DragDrop`, `Logic`, `SequenceItem`, `Serialization`, `Trigger`, `View`).

## Shared Test Bootstrap

`Usings.cs` contains a `[SetUpFixture]` that preloads native DLLs before test execution:

- `SOFAlib.dll`
- `NOVAS31lib.dll`

That bootstrap is necessary because parts of the production astronomy code depend on native libraries being available in the test process.

## Test Utilities

`ImageDataFactoryTestUtility.cs` is a representative example of how the project builds production objects for tests:

- creates mocked `IProfileService`
- creates mocked pluggable behavior selectors for star detection and annotation
- instantiates real `ImageDataFactory` and `ExposureDataFactory`

The general pattern in this project is:

- mock high-level collaborators
- instantiate real production objects when the behavior under test is inside the object itself

## Test Assets

`NINA.Test.csproj` copies sample files into the output directory, including:

- horizon data files under `AstrometryTest/HorizonData`
- `Autofocus/TestImage_Jelly.xisf`

That makes some tests integration-style rather than purely synthetic.

## Dependency Position

This project references almost every runtime project:

- `NINA`
- `NINA.Astrometry`
- `NINA.Core`
- `NINA.CustomControlLibrary`
- `NINA.Equipment`
- `NINA.Image`
- `NINA.MGEN`
- `NINA.PlateSolving`
- `NINA.Plugin`
- `NINA.Profile`
- `NINA.Sequencer`
- `NINA.WPF.Base`

It is the verification layer, not a reusable library.

## Contribution Notes

- Place new tests under the folder that matches the production area under test; the existing layout makes failures easy to navigate.
- Reuse existing helpers and mocks when possible instead of rebuilding factory/bootstrap code in each fixture.
- For WPF-facing tests that instantiate `System.Windows.Application`, depend on `Application.Current.Resources`, or construct XAML-backed views, keep the fixture in an STA apartment and generally mark it `[NonParallelizable]` to avoid multiple-application races during full-suite execution.
- For startup-reachable XAML views, add a runtime construction test when changing controls or resources. Successful BAML compilation alone does not validate runtime type conversion or static-resource resolution.
- If a production change needs native files or sample data at test time, update the project file so the assets are copied into the test output.
- For astronomical or other numerically sensitive changes, prefer assertions based on documented reference values, authoritative sample data, and edge cases rather than only hand-derived spot checks.
- [AstrometryTest/AstroUtilTest.cs](AstrometryTest/AstroUtilTest.cs) already contains that pattern, including cases annotated with SOFA documented values and other cited reference data; extend those fixtures when the production change touches the same calculation families.
- Keep tests explicit about integration boundaries; some areas here are pure unit tests, while others intentionally exercise file I/O, serialization, or native-library loading.
- `ViewModel/ImageRenderingThreadingTest.cs` checks rendered pixels, history bounds, cancellation and provider thread crossings. Run it and `ViewModel/AberrationSaveCompletionTest.cs` in separate test processes to isolate their application dispatchers. The latter verifies save preparation can finish without UI pumping and that owned rendering dispatchers shut down on success and failure. `ViewModel/ImageSaveControllerLifetimeTest.cs` checks cancellation-source cleanup; `Sequencer/Container/TargetSubscriptionLifetimeTest.cs` covers cross-thread target replacement and weak ownership. The [resource lifetime investigation](../NINA/Diagnostics/2026-10-07-resource-lifetime.md) records the separate native handle measurements.
- `Sequencer/ExpressionGeneratorTest.cs` compiles generator output and diagnostics with Roslyn. It also checks handwritten validation composition with the generated expression helper, mutable generated `Issues` properties, reuse of inherited setters, JSON exclusion and change notifications. `ExpressionBackedEntityContractTest` discovers every annotated property in built-in entities and checks malformed input, correction and repeated validation without manually evaluating expressions, plus initialized mutable issue lists, replacement on validation and notifications for every entity using generated validation. It verifies expression context binding before attachment and through replacement, cloning and reparenting for coordinate-related entities. Scoped-variable cases check actual resolution and consumer subscriptions when moving between parents in both directions, detaching and executing Set Variable. They reject stale inputs after detachment or moving to a scope without the symbol and verify recovery when reattached. `Logic/UserSymbolTest` covers cached-input cleanup on release and disposal, plus preservation of unrelated inputs when one reference is invalidated. `CoordinatesInstructionInheritanceTest` also checks context binding before attachment for all five handwritten coordinate clone implementations. The explicit container exception list prevents new ordinary entities from bypassing generated validation.
- Offline sky-map scene, projection, horizon-snapshot, and reusable-surface regressions live under `SkySurvey/`; pair performance-sensitive changes there with `NINA.Benchmark/SkyMapRenderingBenchmark.cs`.
- `Sequencer/ExpressionGeneratorDiagnosticsTest.cs` extends the generator fixture with plugin-author diagnostics. Tests compile real declarations, run every analyzer from the shipped generator assembly and check severity, source locations, suppression and valid controls. Cases cover inherited and explicit validation contracts, hook mutations, proxy recursion, semantic cache reads, malformed ranges, literal escaping and type-name collisions. Invalid input must not crash the generator or stop unrelated entities from generating.
